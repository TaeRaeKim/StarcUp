using System;
using System.Collections.Concurrent;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using StarcUp.Common.Events;

namespace StarcUp.Infrastructure.Communication
{
    /// <summary>
    /// Named Pipes 클라이언트 구현
    /// StarcUp.UI의 Named Pipes Server와 통신
    /// </summary>
    public class NamedPipeClient : INamedPipeClient
    {
        private static readonly JsonSerializerOptions LogOptions = new()
        {
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        private static readonly JsonSerializerOptions SerializeOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        private NamedPipeClientStream _pipeClient;
        private StreamReader _reader;
        private StreamWriter _writer;
        private CancellationTokenSource _cancellationTokenSource;
        private Task _readTask;
        private readonly ConcurrentDictionary<string, TaskCompletionSource<NamedPipeResponse>> _pendingCommands;
        private readonly object _lockObject = new object();
        
        private bool _disposed = false;
        private bool _isConnected = false;
        private bool _isReconnecting = false;
        private bool _autoReconnectEnabled = false;
        
        private string _currentPipeName = "StarcUp";
        private int _reconnectInterval = 3000;
        private int _maxRetries = 0;
        private int _retryCount = 0;

        public bool IsConnected => _isConnected;
        public bool IsReconnecting => _isReconnecting;

        public event EventHandler<bool> ConnectionStateChanged;
        public event EventHandler<CommandRequestEventArgs> CommandRequestReceived;

        public NamedPipeClient()
        {
            _pendingCommands = new ConcurrentDictionary<string, TaskCompletionSource<NamedPipeResponse>>();
        }

        public async Task<bool> ConnectAsync(string pipeName = "StarcUp", int timeout = 5000)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(NamedPipeClient));

            if (_isConnected)
                return true;

            try
            {
                // 기존 연결이 있다면 먼저 정리
                if (_pipeClient != null)
                {
                    await CleanupConnectionAsync();
                }

                _currentPipeName = pipeName;
                var pipePath = $@"\\.\pipe\{pipeName}";
                
                Console.WriteLine($"🔌 Named Pipe 서버에 연결 시도: {pipePath}");
                
                _pipeClient = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
                
                var connectTask = _pipeClient.ConnectAsync();
                var timeoutTask = Task.Delay(timeout);
                
                var completedTask = await Task.WhenAny(connectTask, timeoutTask);
                
                if (completedTask == timeoutTask)
                {
                    Console.WriteLine($"❌ Named Pipe 연결 타임아웃: {timeout}ms");
                    _pipeClient = null;
                    return false;
                }
                
                if (!_pipeClient.IsConnected)
                {
                    Console.WriteLine("❌ Named Pipe 연결 실패");
                    _pipeClient = null;
                    return false;
                }

                _reader = new StreamReader(_pipeClient, Encoding.UTF8);
                _writer = new StreamWriter(_pipeClient, Encoding.UTF8) { AutoFlush = true };
                
                _cancellationTokenSource = new CancellationTokenSource();
                _readTask = Task.Run(ReadLoop, _cancellationTokenSource.Token);
                
                _isConnected = true;
                _retryCount = 0;
                
                Console.WriteLine($"✅ Named Pipe 서버에 연결 성공: {pipePath}");
                ConnectionStateChanged?.Invoke(this, true);
                
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Named Pipe 연결 오류: {ex.Message}");
                await CleanupConnectionAsync();
                return false;
            }
        }

        public void Disconnect()
        {
            if (!_isConnected)
                return;

            Console.WriteLine("🔌 Named Pipe 연결 종료");
            CleanupConnectionAsync().Wait();
        }

        public async Task<NamedPipeResponse> SendCommandAsync(string command, string[] args = null)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(NamedPipeClient));

            if (!_isConnected || _writer == null)
                return NamedPipeResponse.CreateError("", "Named Pipe 서버에 연결되지 않았습니다.");

            // 새로운 프로토콜을 사용하여 RequestMessage 생성
            var payload = args != null && args.Length > 0 ? new { args } : null;
            var request = new NamedPipeProtocol.RequestMessage(command, payload);

            var tcs = new TaskCompletionSource<NamedPipeResponse>();
            _pendingCommands.TryAdd(request.Id, tcs);

            try
            {
                var jsonMessage = JsonSerializer.Serialize(request, SerializeOptions);
                Console.WriteLine($"📤 Request: {{ type: \"{NamedPipeProtocol.MessageType.Request}\", command: \"{command}\", id: \"{request.Id}\", timestamp: {request.Timestamp} }}");
                
                lock (_lockObject)
                {
                    if (_writer != null && _isConnected)
                    {
                        _writer.WriteLine(jsonMessage);
                    }
                    else
                    {
                        _pendingCommands.TryRemove(request.Id, out _);
                        return NamedPipeResponse.CreateError(request.Id, "연결이 끊어졌습니다.");
                    }
                }

                // 15초 타임아웃
                var timeoutTask = Task.Delay(15000);
                var completedTask = await Task.WhenAny(tcs.Task, timeoutTask);
                
                if (completedTask == timeoutTask)
                {
                    _pendingCommands.TryRemove(request.Id, out _);
                    return NamedPipeResponse.CreateError(request.Id, $"명령 실행 시간 초과: {command}");
                }

                return await tcs.Task;
            }
            catch (Exception ex)
            {
                _pendingCommands.TryRemove(request.Id, out _);
                Console.WriteLine($"❌ [SendCommandAsync] 명령 전송 실패: {ex.Message}");
                return NamedPipeResponse.CreateError(request.Id, ex.Message);
            }
        }

        public bool SendEvent(string eventType, object data = null)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(NamedPipeClient));

            if (!_isConnected || _writer == null)
                return false;

            try
            {
                var eventMessage = new NamedPipeProtocol.EventMessage(eventType, data);
                var jsonMessage = JsonSerializer.Serialize(eventMessage, SerializeOptions);
                Console.WriteLine($"📡 이벤트 전송: {eventType}");
                
                lock (_lockObject)
                {
                    if (_writer != null && _isConnected)
                    {
                        _writer.WriteLine(jsonMessage);
                        return true;
                    }
                }
                
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 이벤트 전송 실패: {ex.Message}");
                return false;
            }
        }

        public void StartAutoReconnect(string pipeName = "StarcUp", int reconnectInterval = 3000, int maxRetries = 0)
        {
            _currentPipeName = pipeName;
            _reconnectInterval = reconnectInterval;
            _maxRetries = maxRetries;
            _autoReconnectEnabled = true;
            
            Console.WriteLine($"🔄 자동 재연결 시작: {pipeName} (간격: {reconnectInterval}ms, 최대 재시도: {maxRetries})");
        }

        public void StopAutoReconnect()
        {
            _autoReconnectEnabled = false;
            Console.WriteLine("🔄 자동 재연결 중지");
        }

        public void TriggerReconnect()
        {
            if (_autoReconnectEnabled && !_isConnected && !_isReconnecting && !_disposed)
            {
                Console.WriteLine("🔄 수동 재연결 트리거");
                _isReconnecting = true;
                _ = Task.Run(ReconnectLoop);
            }
        }

        private async Task ReadLoop()
        {
            try
            {
                while (!_cancellationTokenSource.Token.IsCancellationRequested && _isConnected)
                {
                    var message = await _reader.ReadLineAsync();
                    
                    if (message == null)
                    {
                        break;
                    }

                    await ProcessIncomingMessage(message);
                }
            }
            catch (Exception ex) when (!_cancellationTokenSource.Token.IsCancellationRequested)
            {
                Console.WriteLine($"❌ 메시지 읽기 오류: {ex.Message}");
            }
            finally
            {
                if (_isConnected)
                {
                    await HandleDisconnection();
                }
            }
        }

        private async Task ProcessIncomingMessage(string message)
        {
            try
            {
                var jsonDocument = JsonDocument.Parse(message);
                var root = jsonDocument.RootElement;

                // 메시지 타입 확인
                if (!root.TryGetProperty("type", out var typeElement))
                {
                    Console.WriteLine($"⚠️ 메시지 타입이 없음 - 무시");
                    return;
                }

                var messageTypeValue = typeElement.GetInt32();
                var messageType = (NamedPipeProtocol.MessageType)messageTypeValue;

                switch (messageType)
                {
                    case NamedPipeProtocol.MessageType.Request:
                        await HandleIncomingRequest(root);
                        break;
                    case NamedPipeProtocol.MessageType.Response:
                        HandleIncomingResponse(root);
                        break;
                    case NamedPipeProtocol.MessageType.Event:
                        HandleIncomingEvent(root);
                        break;
                    default:
                        Console.WriteLine($"⚠️ 알 수 없는 메시지 타입: {messageType} ({messageTypeValue})");
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 메시지 처리 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// UI로부터 온 요청 처리 - 순수한 메시지 전달만 담당
        /// </summary>
        private async Task HandleIncomingRequest(JsonElement root)
        {
            try
            {
                var requestId = root.GetProperty("id").GetString();
                var command = root.GetProperty("command").GetString();
                
                Console.WriteLine($"📨 Request: {{ type: \"{NamedPipeProtocol.MessageType.Request}\", command: \"{command}\", id: \"{requestId}\" }}");
                
                // 페이로드 추출 (있는 경우)
                object payload = null;
                if (root.TryGetProperty("payload", out var payloadElement))
                {
                    payload = payloadElement.GetRawText();
                }
                
                // CommandRequestReceived 이벤트 발생 - Business 계층에서 처리하도록 위임
                var eventArgs = new CommandRequestEventArgs(requestId, command, payload);
                CommandRequestReceived?.Invoke(this, eventArgs);
                
                // Infrastructure 계층에서는 요청 접수 확인만 응답
                var response = new NamedPipeProtocol.ResponseMessage(requestId, true, new { message = $"명령 '{command}' 요청이 접수되었습니다" });
                await SendResponseAsync(response);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 요청 처리 오류: {ex.Message}");
                
                // 오류 발생 시 에러 응답 전송
                try
                {
                    var requestId = root.TryGetProperty("id", out var idElement) ? idElement.GetString() : "unknown";
                    var errorResponse = new NamedPipeProtocol.ResponseMessage(requestId, false, null, $"요청 처리 중 오류 발생: {ex.Message}");
                    await SendResponseAsync(errorResponse);
                }
                catch
                {
                    Console.WriteLine("❌ 에러 응답 전송 실패");
                }
            }
        }

        /// <summary>
        /// UI로부터 온 응답 처리
        /// </summary>
        private void HandleIncomingResponse(JsonElement root)
        {
            try
            {
                var requestId = root.GetProperty("requestId").GetString();
                var success = root.TryGetProperty("success", out var successElement) ? successElement.GetBoolean() : false;
                var dataText = root.TryGetProperty("data", out var dataElement) ? dataElement.GetRawText() : "null";
                
                var responseId = root.TryGetProperty("id", out var idElement) ? idElement.GetString() : "unknown";
                var timestamp = root.TryGetProperty("timestamp", out var timestampElement) ? timestampElement.GetInt64() : 0;
                Console.WriteLine($"📥 Response: {{ type: \"{NamedPipeProtocol.MessageType.Response}\", id: \"{responseId}\", requestId: \"{requestId}\", success: {success.ToString().ToLower()}, timestamp: {timestamp}, data: {dataText} }}");
                
                if (_pendingCommands.TryRemove(requestId, out var tcs))
                {
                    var response = new NamedPipeResponse
                    {
                        Id = requestId,
                        Success = success,
                        Data = dataText,
                        Error = root.TryGetProperty("error", out var errorElement) ? errorElement.GetString() : null,
                        Timestamp = timestamp
                    };

                    tcs.SetResult(response);
                }
                else
                {
                    Console.WriteLine($"⚠️ 대기중인 명령을 찾을 수 없음 - RequestID: {requestId}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 응답 처리 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// UI로부터 온 이벤트 처리
        /// </summary>
        private void HandleIncomingEvent(JsonElement root)
        {
            try
            {
                var eventName = root.GetProperty("event").GetString();
                Console.WriteLine($"📢 [HandleIncomingEvent] 이벤트 처리: {eventName}");
                
                // Event 메시지는 현재 특별한 처리 없이 로그만 출력
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ [HandleIncomingEvent] 이벤트 처리 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// 응답 메시지 전송
        /// </summary>
        private async Task SendResponseAsync(NamedPipeProtocol.ResponseMessage response)
        {
            try
            {
                // 직렬화 전에 로그 출력 (한글이 정상적으로 보임)
                var dataText = response.Data != null ? JsonSerializer.Serialize(response.Data, LogOptions) : "null";
                Console.WriteLine($"📤 Response: {{ type: \"{NamedPipeProtocol.MessageType.Response}\", id: \"{response.Id}\", requestId: \"{response.RequestId}\", success: {response.Success.ToString().ToLower()}, timestamp: {response.Timestamp}, data: {dataText} }}");
                
                var responseJson = JsonSerializer.Serialize(response, SerializeOptions);
                await _writer.WriteLineAsync(responseJson);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 응답 전송 실패: {ex.Message}");
            }
        }

        private async Task HandleDisconnection()
        {
            Console.WriteLine("🔌 Named Pipe 연결이 끊어졌습니다");
            
            await CleanupConnectionAsync();
            
            if (_autoReconnectEnabled && !_disposed)
            {
                _ = Task.Run(ReconnectLoop);
            }
        }

        private async Task ReconnectLoop()
        {
            _isReconnecting = true;
            Console.WriteLine("🔄 자동 재연결 시작");
            
            while (_autoReconnectEnabled && !_disposed && !_isConnected)
            {
                _retryCount++;
                
                if (_maxRetries > 0 && _retryCount > _maxRetries)
                {
                    Console.WriteLine($"❌ 최대 재연결 횟수 초과: {_maxRetries}");
                    break;
                }

                Console.WriteLine($"🔄 재연결 시도 #{_retryCount}");
                
                var success = await ConnectAsync(_currentPipeName, 2000);
                if (success)
                {
                    Console.WriteLine("✅ 재연결 성공");
                    _isReconnecting = false;
                    return;
                }

                await Task.Delay(_reconnectInterval);
            }
            
            _isReconnecting = false;
            Console.WriteLine("🔄 자동 재연결 중지");
        }

        private async Task CleanupConnectionAsync()
        {
            _isConnected = false;
            
            // 진행 중인 명령들에 오류 응답
            foreach (var kvp in _pendingCommands)
            {
                kvp.Value.SetResult(NamedPipeResponse.CreateError(kvp.Key, "연결이 끊어졌습니다"));
            }
            _pendingCommands.Clear();

            // 읽기 작업 취소
            _cancellationTokenSource?.Cancel();
            
            try
            {
                if (_readTask != null && !_readTask.IsCompleted)
                {
                    await _readTask;
                }
            }
            catch (OperationCanceledException)
            {
                // 예상된 취소 예외 무시
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 읽기 작업 종료 오류: {ex.Message}");
            }

            // 리소스 정리
            try
            {
                _writer?.Dispose();
                _reader?.Dispose();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 리소스 정리 오류: {ex.Message}");
            }
            finally
            {
                _writer = null;
                _reader = null;
                _pipeClient = null;
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;
                _readTask = null;
            }

            ConnectionStateChanged?.Invoke(this, false);
        }

        private string GenerateCommandId()
        {
            return $"cmd_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}_{Guid.NewGuid().ToString("N")[..8]}";
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            _autoReconnectEnabled = false;
            
            Console.WriteLine("🗑️ Named Pipe 클라이언트 종료");
            
            CleanupConnectionAsync().Wait();
            
            ConnectionStateChanged = null;
            CommandRequestReceived = null;
        }
    }
}