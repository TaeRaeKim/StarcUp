using System;
using System.Collections.Concurrent;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace StarcUp.Core.Src.Infrastructure.Communication
{
    /// <summary>
    /// Named Pipes 클라이언트 구현
    /// StarcUp.UI의 Named Pipes Server와 통신
    /// </summary>
    public class NamedPipeClient : INamedPipeClient
    {
        private NamedPipeClientStream _pipeClient;
        private StreamReader _reader;
        private StreamWriter _writer;
        private CancellationTokenSource _cancellationTokenSource;
        private Task _readTask;
        private Task _reconnectTask;
        
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
        public event EventHandler<string> MessageReceived;

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

            var commandId = GenerateCommandId();
            var message = new
            {
                id = commandId,
                command = command,
                args = args ?? new string[0],
                timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            };

            var tcs = new TaskCompletionSource<NamedPipeResponse>();
            _pendingCommands.TryAdd(commandId, tcs);

            try
            {
                var jsonMessage = JsonSerializer.Serialize(message);
                Console.WriteLine($"📤 명령 전송: {command}");
                
                lock (_lockObject)
                {
                    if (_writer != null && _isConnected)
                    {
                        _writer.WriteLine(jsonMessage);
                    }
                    else
                    {
                        _pendingCommands.TryRemove(commandId, out _);
                        return NamedPipeResponse.CreateError(commandId, "연결이 끊어졌습니다.");
                    }
                }

                // 15초 타임아웃
                var timeoutTask = Task.Delay(15000);
                var completedTask = await Task.WhenAny(tcs.Task, timeoutTask);
                
                if (completedTask == timeoutTask)
                {
                    _pendingCommands.TryRemove(commandId, out _);
                    return NamedPipeResponse.CreateError(commandId, $"명령 실행 시간 초과: {command}");
                }

                return await tcs.Task;
            }
            catch (Exception ex)
            {
                _pendingCommands.TryRemove(commandId, out _);
                Console.WriteLine($"❌ 명령 전송 실패: {ex.Message}");
                return NamedPipeResponse.CreateError(commandId, ex.Message);
            }
        }

        public async Task<bool> SendEventAsync(string eventType, object data = null)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(NamedPipeClient));

            if (!_isConnected || _writer == null)
                return false;

            try
            {
                var eventMessage = new
                {
                    type = "event",
                    eventType = eventType,
                    data = data,
                    timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                };

                var jsonMessage = JsonSerializer.Serialize(eventMessage);
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
                        break;

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

                // 명령 응답 처리
                if (root.TryGetProperty("id", out var idElement))
                {
                    var responseId = idElement.GetString();
                    if (_pendingCommands.TryRemove(responseId, out var tcs))
                    {
                        var response = new NamedPipeResponse
                        {
                            Id = responseId,
                            Success = root.TryGetProperty("success", out var successElement) ? successElement.GetBoolean() : false,
                            Data = root.TryGetProperty("data", out var dataElement) ? dataElement.GetRawText() : null,
                            Error = root.TryGetProperty("error", out var errorElement) ? errorElement.GetString() : null,
                            Timestamp = root.TryGetProperty("timestamp", out var timestampElement) ? timestampElement.GetInt64() : 0
                        };

                        tcs.SetResult(response);
                    }
                }
                // 이벤트 또는 기타 메시지 처리
                else
                {
                    MessageReceived?.Invoke(this, message);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 메시지 처리 오류: {ex.Message}");
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
            MessageReceived = null;
        }
    }
}