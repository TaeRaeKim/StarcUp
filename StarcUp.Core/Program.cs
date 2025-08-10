using StarcUp.DependencyInjection;
using StarcUp.Infrastructure.Communication;
using StarcUp.Business.Communication;
using StarcUp.Common.Logging;

namespace StarcUp
{
    public static class Program
    {
        private static ServiceContainer _container;
        private static CancellationTokenSource _cancellationTokenSource;
        
        [STAThread]
        static async Task Main(string[] args)
        {
            // 초기 시작 메시지는 Console로 출력 (Logger 초기화 전)
            Console.WriteLine("StarcUp.Core - 스타크래프트 메모리 모니터 (자식 프로세스)");
            Console.WriteLine("=========================================================");

            try
            {
                // 환경 정보 출력
                NamedPipeConfig.PrintEnvironmentInfo();
                // Logger 초기화 전이므로 Console 사용

                // 명령줄 인자 확인 - Named Pipe 모드만 지원
                var pipeName = args.Length > 0 ? args[0] : null;
                if (!string.IsNullOrEmpty(pipeName))
                {
                    // Logger 초기화 전이므로 Console 사용
                    Console.WriteLine($"📡 사용자 지정 파이프 이름: {pipeName}");
                }

                // 서비스 컨테이너 초기화
                _container = new ServiceContainer();
                RegisterServices();
                
                // LoggerHelper 초기화
                var loggerFactory = _container.Resolve<ILoggerFactory>();
                LoggerHelper.Initialize(loggerFactory);

                // 통신 서비스 시작
                var communicationService = _container.Resolve<ICommunicationService>();
                await communicationService.StartAsync(pipeName);

                // 애플리케이션 대기
                LoggerHelper.Info("🚀 StarcUp.Core 시작 완료. 'q' 입력 시 종료");
                _cancellationTokenSource = new CancellationTokenSource();
                
                // 백그라운드에서 키 입력 대기
                _ = Task.Run(() => WaitForExitCommand(_cancellationTokenSource.Token));
                
                Task.Delay(Timeout.Infinite, _cancellationTokenSource.Token).Wait();

            }
            catch (OperationCanceledException)
            {
                LoggerHelper.Info("📱 종료 신호 수신");
            }
            catch (Exception ex)
            {
                LoggerHelper.Error("❌ 애플리케이션 시작 실패", ex);
            }
            finally
            {
                Cleanup();
                Environment.Exit(1);
            }
        }

        private static void RegisterServices()
        {
            // 서비스 등록은 ServiceContainer에서 처리
            ServiceRegistration.RegisterServices(_container);
        }

        private static void WaitForExitCommand(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var key = Console.ReadKey(true);
                if (key.KeyChar == 'q' || key.KeyChar == 'Q')
                {
                    LoggerHelper.Info("🛑 종료 명령 수신");
                    _cancellationTokenSource?.Cancel();
                    break;
                }
            }
        }

        /// <summary>
        /// 리소스 정리
        /// </summary>
        private static void Cleanup()
        {
            try
            {
                LoggerHelper.Info("🧹 애플리케이션 종료 중...");
                _container?.Dispose();
                LoggerHelper.Info("✅ 정리 완료");
            }
            catch (Exception ex)
            {
                LoggerHelper.Error("⚠️ 정리 중 오류 발생", ex);
            }
        }
    }
}