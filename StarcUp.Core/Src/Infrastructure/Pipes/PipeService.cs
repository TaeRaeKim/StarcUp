using System;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace StarcUp.Infrastructure.Pipes
{
    /// <summary>
    /// Anonymous Pipe를 이용한 프로세스 간 통신 서비스
    /// </summary>
    public class PipeService : IPipeService
    {
        private readonly ICommandHandler _commandHandler;
        private AnonymousPipeClientStream _pipeClientIn;
        private AnonymousPipeClientStream _pipeClientOut;
        private StreamReader _reader;
        private StreamWriter _writer;
        private CancellationTokenSource _cancellationTokenSource;
        private Task _listeningTask;
        private bool _disposed = false;

        /// <summary>
        /// 명령 수신 시 발생하는 이벤트
        /// </summary>
        public event EventHandler<CommandReceivedEventArgs> CommandReceived;

        /// <summary>
        /// PipeService 생성자
        /// </summary>
        /// <param name="commandHandler">명령 처리 핸들러</param>
        public PipeService(ICommandHandler commandHandler)
        {
            _commandHandler = commandHandler ?? throw new ArgumentNullException(nameof(commandHandler));
        }

        /// <summary>
        /// Pipe 클라이언트 시작 (Anonymous Pipes 또는 stdio)
        /// </summary>
        /// <param name="pipeInHandle">부모로부터 받은 입력 핸들 또는 "stdio"</param>
        /// <param name="pipeOutHandle">부모로부터 받은 출력 핸들 또는 "stdio"</param>
        /// <param name="cancellationToken">취소 토큰</param>
        public async Task StartAsync(string pipeInHandle, string pipeOutHandle, CancellationToken cancellationToken = default)
        {
            try
            {
                bool useStdio = pipeInHandle == "stdio" && pipeOutHandle == "stdio";
                
                if (useStdio)
                {
                    Console.WriteLine($"🔗 stdio 통신 시작...");
                    
                    // stdio 스트림 사용
                    _reader = new StreamReader(Console.OpenStandardInput(), Encoding.UTF8);
                    _writer = new StreamWriter(Console.OpenStandardOutput(), Encoding.UTF8) { AutoFlush = true };
                }
                else
                {
                    Console.WriteLine($"🔗 Anonymous Pipe 연결 시작...");
                    Console.WriteLine($"   📥 입력 핸들: {pipeInHandle}");
                    Console.WriteLine($"   📤 출력 핸들: {pipeOutHandle}");

                    // Anonymous Pipe 클라이언트 생성
                    _pipeClientIn = new AnonymousPipeClientStream(PipeDirection.In, pipeInHandle);
                    _pipeClientOut = new AnonymousPipeClientStream(PipeDirection.Out, pipeOutHandle);

                    // 스트림 리더/라이터 생성
                    _reader = new StreamReader(_pipeClientIn, Encoding.UTF8);
                    _writer = new StreamWriter(_pipeClientOut, Encoding.UTF8) { AutoFlush = true };
                }

                // 취소 토큰 소스 생성
                _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

                // 리스닝 태스크 시작
                _listeningTask = ListenForCommandsAsync(_cancellationTokenSource.Token);

                Console.WriteLine($"✅ {(useStdio ? "stdio" : "Anonymous Pipe")} 연결 완료");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Pipe 연결 실패: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 응답 전송
        /// </summary>
        /// <param name="response">응답 메시지</param>
        /// <param name="cancellationToken">취소 토큰</param>
        public async Task SendResponseAsync(string response, CancellationToken cancellationToken = default)
        {
            if (_writer == null)
            {
                throw new InvalidOperationException("Pipe가 초기화되지 않았습니다.");
            }

            try
            {
                await _writer.WriteLineAsync(response);
                Console.WriteLine($"📤 응답 전송: {response}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 응답 전송 실패: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Pipe 서비스 중지
        /// </summary>
        public async Task StopAsync()
        {
            try
            {
                Console.WriteLine("🔌 Pipe 서비스 중지 중...");
                
                _cancellationTokenSource?.Cancel();
                
                if (_listeningTask != null)
                {
                    await _listeningTask;
                }

                Console.WriteLine("✅ Pipe 서비스 중지 완료");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Pipe 서비스 중지 중 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// 명령 수신 대기
        /// </summary>
        private async Task ListenForCommandsAsync(CancellationToken cancellationToken)
        {
            try
            {
                Console.WriteLine("👂 명령 수신 대기 중...");

                while (!cancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        var command = await _reader.ReadLineAsync();
                        if (command == null)
                        {
                            // 부모 프로세스가 파이프를 닫았음
                            Console.WriteLine("📡 부모 프로세스와의 연결이 종료되었습니다.");
                            break;
                        }

                        Console.WriteLine($"📥 명령 수신: {command}");

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
                        // 파이프가 닫혔거나 연결이 끊어짐
                        Console.WriteLine("📡 파이프 연결이 종료되었습니다.");
                        break;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"❌ 명령 수신 중 오류: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 명령 리스닝 중 치명적 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// 명령 처리 및 응답 전송
        /// </summary>
        private async Task HandleCommandAsync(string command, string[] arguments, string commandId)
        {
            try
            {
                Console.WriteLine($"📥 명령 수신: {command} (ID: {commandId})");
                
                // 명령 처리
                var result = await _commandHandler.HandleCommandAsync(command, arguments, commandId);
                
                // 응답 전송
                await SendResponseAsync(result);
                
                Console.WriteLine($"📤 명령 처리 완료: {command} -> {result}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 명령 처리 중 오류: {ex.Message}");
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

        /// <summary>
        /// 리소스 정리
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                try
                {
                    _cancellationTokenSource?.Cancel();
                    _listeningTask?.Wait(TimeSpan.FromSeconds(5));
                    
                    _reader?.Dispose();
                    _writer?.Dispose();
                    _pipeClientIn?.Dispose();
                    _pipeClientOut?.Dispose();
                    _cancellationTokenSource?.Dispose();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"⚠️ PipeService 정리 중 오류: {ex.Message}");
                }
                finally
                {
                    _disposed = true;
                }
            }
        }
    }
}