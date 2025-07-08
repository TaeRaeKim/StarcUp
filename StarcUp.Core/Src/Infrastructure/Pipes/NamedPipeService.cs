using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace StarcUp.Infrastructure.Pipes
{
    /// <summary>
    /// Named Pipe를 이용한 프로세스 간 통신 서비스
    /// </summary>
    public class NamedPipeService : INamedPipeService
    {
        private readonly ICommandHandler _commandHandler;
        private readonly ConcurrentDictionary<string, PipeClient> _clients = new();
        private CancellationTokenSource _cancellationTokenSource;
        private Task _serverTask;
        private string _pipeName;
        private bool _disposed = false;

        /// <summary>
        /// 명령 수신 시 발생하는 이벤트
        /// </summary>
        public event EventHandler<CommandReceivedEventArgs> CommandReceived;

        /// <summary>
        /// 클라이언트 연결 시 발생하는 이벤트
        /// </summary>
        public event EventHandler<ClientConnectedEventArgs> ClientConnected;

        /// <summary>
        /// 클라이언트 연결 해제 시 발생하는 이벤트
        /// </summary>
        public event EventHandler<ClientDisconnectedEventArgs> ClientDisconnected;

        /// <summary>
        /// 연결된 클라이언트 수
        /// </summary>
        public int ConnectedClientCount => _clients.Count;

        /// <summary>
        /// NamedPipeService 생성자
        /// </summary>
        /// <param name="commandHandler">명령 처리 핸들러</param>
        public NamedPipeService(ICommandHandler commandHandler)
        {
            _commandHandler = commandHandler ?? throw new ArgumentNullException(nameof(commandHandler));
        }

        /// <summary>
        /// Named Pipe 서버 시작
        /// </summary>
        /// <param name="pipeName">파이프 이름</param>
        /// <param name="cancellationToken">취소 토큰</param>
        public async Task StartAsync(string pipeName, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(pipeName))
                throw new ArgumentException("파이프 이름은 필수입니다.", nameof(pipeName));

            try
            {
                _pipeName = pipeName;
                _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

                Console.WriteLine($"🔗 Named Pipe 서버 시작: {pipeName}");

                // 서버 태스크 시작
                _serverTask = AcceptClientsAsync(_cancellationTokenSource.Token);

                Console.WriteLine($"✅ Named Pipe 서버 준비 완료: {pipeName}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Named Pipe 서버 시작 실패: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 응답 전송
        /// </summary>
        /// <param name="response">응답 메시지</param>
        /// <param name="clientId">클라이언트 ID (null인 경우 모든 클라이언트에게 전송)</param>
        /// <param name="cancellationToken">취소 토큰</param>
        public async Task SendResponseAsync(string response, string clientId = null, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(response))
                throw new ArgumentException("응답 메시지는 필수입니다.", nameof(response));

            if (clientId != null)
            {
                // 특정 클라이언트에게만 전송
                if (_clients.TryGetValue(clientId, out var client))
                {
                    await client.SendResponseAsync(response, cancellationToken);
                }
                else
                {
                    Console.WriteLine($"⚠️ 클라이언트를 찾을 수 없습니다: {clientId}");
                }
            }
            else
            {
                // 모든 클라이언트에게 전송
                var tasks = new List<Task>();
                foreach (var client in _clients.Values)
                {
                    tasks.Add(client.SendResponseAsync(response, cancellationToken));
                }

                if (tasks.Count > 0)
                {
                    await Task.WhenAll(tasks);
                    Console.WriteLine($"📤 모든 클라이언트에게 응답 전송 완료: {response}");
                }
            }
        }

        /// <summary>
        /// Named Pipe 서비스 중지
        /// </summary>
        public async Task StopAsync()
        {
            try
            {
                Console.WriteLine("🔌 Named Pipe 서비스 중지 중...");

                _cancellationTokenSource?.Cancel();

                // 모든 클라이언트 연결 해제
                foreach (var client in _clients.Values)
                {
                    await client.DisconnectAsync();
                }
                _clients.Clear();

                // 서버 태스크 대기
                if (_serverTask != null)
                {
                    await _serverTask;
                }

                Console.WriteLine("✅ Named Pipe 서비스 중지 완료");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Named Pipe 서비스 중지 중 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// 클라이언트 연결 수락
        /// </summary>
        private async Task AcceptClientsAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var pipeServer = new NamedPipeServerStream(
                        _pipeName,
                        PipeDirection.InOut,
                        NamedPipeServerStream.MaxAllowedServerInstances,
                        PipeTransmissionMode.Byte,
                        PipeOptions.Asynchronous);

                    Console.WriteLine($"👂 클라이언트 연결 대기 중... (파이프: {_pipeName})");

                    // 클라이언트 연결 대기
                    await pipeServer.WaitForConnectionAsync(cancellationToken);

                    var clientId = Guid.NewGuid().ToString();
                    var client = new PipeClient(clientId, pipeServer, _commandHandler);

                    // 클라이언트 이벤트 핸들러 등록
                    client.CommandReceived += (sender, args) => CommandReceived?.Invoke(sender, args);
                    client.Disconnected += OnClientDisconnected;

                    // 클라이언트 등록
                    _clients.TryAdd(clientId, client);

                    Console.WriteLine($"✅ 클라이언트 연결됨: {clientId}");
                    ClientConnected?.Invoke(this, new ClientConnectedEventArgs(clientId));

                    // 클라이언트 통신 시작
                    _ = Task.Run(async () => await client.StartAsync(cancellationToken), cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    // 정상적인 취소
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ 클라이언트 연결 수락 중 오류: {ex.Message}");
                    await Task.Delay(1000, cancellationToken); // 재시도 지연
                }
            }
        }

        /// <summary>
        /// 클라이언트 연결 해제 처리
        /// </summary>
        private void OnClientDisconnected(object sender, ClientDisconnectedEventArgs e)
        {
            _clients.TryRemove(e.ClientId, out _);
            Console.WriteLine($"🔌 클라이언트 연결 해제: {e.ClientId} ({e.Reason})");
            ClientDisconnected?.Invoke(this, e);
        }

        /// <summary>
        /// 리소스 정리
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                try
                {
                    StopAsync().Wait(TimeSpan.FromSeconds(5));
                    _cancellationTokenSource?.Dispose();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"⚠️ NamedPipeService 정리 중 오류: {ex.Message}");
                }
                finally
                {
                    _disposed = true;
                }
            }
        }
    }

    /// <summary>
    /// 파이프 클라이언트 래퍼
    /// </summary>
    internal class PipeClient
    {
        private readonly string _clientId;
        private readonly NamedPipeServerStream _pipeStream;
        private readonly ICommandHandler _commandHandler;
        private readonly StreamReader _reader;
        private readonly StreamWriter _writer;
        private bool _disposed = false;

        public event EventHandler<CommandReceivedEventArgs> CommandReceived;
        public event EventHandler<ClientDisconnectedEventArgs> Disconnected;

        public PipeClient(string clientId, NamedPipeServerStream pipeStream, ICommandHandler commandHandler)
        {
            _clientId = clientId;
            _pipeStream = pipeStream;
            _commandHandler = commandHandler;

            _reader = new StreamReader(pipeStream, Encoding.UTF8);
            _writer = new StreamWriter(pipeStream, Encoding.UTF8) { AutoFlush = true };
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            try
            {
                await ListenForCommandsAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 클라이언트 {_clientId} 통신 중 오류: {ex.Message}");
                Disconnected?.Invoke(this, new ClientDisconnectedEventArgs(_clientId, ex.Message));
            }
        }

        public async Task SendResponseAsync(string response, CancellationToken cancellationToken = default)
        {
            if (_disposed || !_pipeStream.IsConnected)
                return;

            try
            {
                await _writer.WriteLineAsync(response);
                Console.WriteLine($"📤 클라이언트 {_clientId}에게 응답 전송: {response}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 클라이언트 {_clientId} 응답 전송 실패: {ex.Message}");
                throw;
            }
        }

        public async Task DisconnectAsync()
        {
            if (!_disposed)
            {
                try
                {
                    _pipeStream?.Close();
                    _reader?.Dispose();
                    _writer?.Dispose();
                    _pipeStream?.Dispose();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"⚠️ 클라이언트 {_clientId} 연결 해제 중 오류: {ex.Message}");
                }
                finally
                {
                    _disposed = true;
                }
            }
        }

        private async Task ListenForCommandsAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested && _pipeStream.IsConnected)
            {
                try
                {
                    var command = await _reader.ReadLineAsync();
                    if (command == null)
                    {
                        // 클라이언트가 연결을 끊었음
                        break;
                    }

                    Console.WriteLine($"📥 클라이언트 {_clientId}로부터 명령 수신: {command}");

                    // 명령 파싱
                    var parts = command.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length > 0)
                    {
                        var commandName = parts[0];
                        var arguments = parts.Length > 1 ? parts[1..] : Array.Empty<string>();
                        var commandId = Guid.NewGuid().ToString();

                        // 명령 처리 및 응답 전송
                        await HandleCommandAsync(commandName, arguments, commandId);
                    }
                }
                catch (IOException)
                {
                    // 연결이 끊어짐
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ 클라이언트 {_clientId} 명령 수신 중 오류: {ex.Message}");
                }
            }

            Disconnected?.Invoke(this, new ClientDisconnectedEventArgs(_clientId, "Connection closed"));
        }

        private async Task HandleCommandAsync(string command, string[] arguments, string commandId)
        {
            try
            {
                // 명령 처리
                var result = await _commandHandler.HandleCommandAsync(command, arguments, commandId);

                // 응답 전송
                await SendResponseAsync(result);

                Console.WriteLine($"📤 클라이언트 {_clientId} 명령 처리 완료: {command} -> {result}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 클라이언트 {_clientId} 명령 처리 중 오류: {ex.Message}");
                try
                {
                    await SendResponseAsync($"ERROR:{ex.Message}");
                }
                catch
                {
                    // 응답 전송 실패는 무시
                }
            }
        }
    }
}