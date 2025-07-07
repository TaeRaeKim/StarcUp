using StarcUp.DependencyInjection;
using StarcUp.Infrastructure.Pipes;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace StarcUp
{
    public static class Program
    {
        private static ServiceContainer _container;
        private static IPipeService _pipeService;
        private static CancellationTokenSource _cancellationTokenSource;
        
        [STAThread]
        static async Task Main(string[] args)
        {
            Console.WriteLine("StarcUp.Core - 스타크래프트 메모리 모니터 (자식 프로세스)");
            Console.WriteLine("=========================================================");

            try
            {
                // 명령줄 인자 확인
                bool useStdio = args.Length >= 2 && args[0] == "stdio" && args[1] == "stdio";
                
                if (!useStdio && args.Length < 2)
                {
                    Console.WriteLine("❌ 사용법:");
                    Console.WriteLine("   StarcUp.Core.exe <pipeInHandle> <pipeOutHandle>  - Anonymous Pipes 모드");
                    Console.WriteLine("   StarcUp.Core.exe stdio stdio                    - stdio 모드");
                    Environment.Exit(1);
                    return;
                }

                string pipeInHandle, pipeOutHandle;
                
                if (useStdio)
                {
                    Console.WriteLine("📡 stdio 모드로 실행");
                    pipeInHandle = "stdio";
                    pipeOutHandle = "stdio";
                }
                else
                {
                    pipeInHandle = args[0];
                    pipeOutHandle = args[1];
                    Console.WriteLine($"📡 부모 프로세스로부터 파이프 핸들 수신:");
                    Console.WriteLine($"   📥 입력: {pipeInHandle}");
                    Console.WriteLine($"   📤 출력: {pipeOutHandle}");
                }

                // 서비스 컨테이너 초기화
                _container = new ServiceContainer();
                RegisterServices();

                // 취소 토큰 소스 생성
                _cancellationTokenSource = new CancellationTokenSource();

                // Pipe 서비스 초기화 및 시작
                _pipeService = _container.Resolve<IPipeService>();
                await _pipeService.StartAsync(pipeInHandle, pipeOutHandle, _cancellationTokenSource.Token);

                Console.WriteLine("🚀 StarcUp.Core 준비 완료! 명령 대기 중...");
                Console.WriteLine("종료하려면 Ctrl+C를 누르거나 부모 프로세스에서 종료 신호를 보내세요.");

                // 종료 신호 대기
                Console.CancelKeyPress += (sender, e) => {
                    e.Cancel = true;
                    _cancellationTokenSource.Cancel();
                };

                // 무한 대기 (명령 처리)
                await WaitForShutdownAsync(_cancellationTokenSource.Token);

            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 애플리케이션 시작 실패: {ex.Message}");
                Environment.Exit(1);
            }
            finally
            {
                await Cleanup();
            }
        }

        private static void RegisterServices()
        {
            // 서비스 등록은 ServiceContainer에서 처리
            ServiceRegistration.RegisterServices(_container);
        }

        /// <summary>
        /// 종료 신호 대기
        /// </summary>
        private static async Task WaitForShutdownAsync(CancellationToken cancellationToken)
        {
            try
            {
                // 취소 토큰이 신호될 때까지 대기
                await Task.Delay(Timeout.Infinite, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("🔌 종료 신호 수신됨");
            }
        }

        /// <summary>
        /// 리소스 정리
        /// </summary>
        private static async Task Cleanup()
        {
            try
            {
                Console.WriteLine("🧹 애플리케이션 종료 중...");
                
                // Pipe 서비스 정리
                if (_pipeService != null)
                {
                    await _pipeService.StopAsync();
                    _pipeService.Dispose();
                }
                
                // 취소 토큰 소스 정리
                _cancellationTokenSource?.Dispose();
                
                // 서비스 컨테이너 정리
                _container?.Dispose();
                
                Console.WriteLine("✅ 정리 완료");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ 정리 중 오류 발생: {ex.Message}");
            }
        }
    }
}