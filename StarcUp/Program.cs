using StarcUp.Business.GameDetection;
using StarcUp.Business.MemoryService;
using StarcUp.Business.InGameDetector;
using StarcUp.Business.Units.Runtime.Services;
using StarcUp.DependencyInjection;
using StarcUp.Presentation.Forms;
using System;
using System.Windows.Forms;

namespace StarcUp
{
    public static class Program
    {
        private static ServiceContainer _container;
        private static ControlForm _controlForm;

        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            Console.WriteLine("StarcUp - 스타크래프트 메모리 모니터");
            Console.WriteLine("=====================================");

            try
            {
                // 서비스 컨테이너 초기화 및 서비스 등록
                _container = new ServiceContainer();
                RegisterServices();

                // 메인 컨트롤 폼 생성 및 표시
                CreateControlForm();

                Console.WriteLine("프로그램이 시작되었습니다.");
                Console.WriteLine("스타크래프트 프로세스를 자동으로 감지합니다.");

                // 메시지 루프 실행
                Application.Run(_controlForm);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"애플리케이션 시작 실패: {ex.Message}");
                MessageBox.Show($"애플리케이션 시작 실패: {ex.Message}", "오류",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                Cleanup();
            }
        }

        private static void RegisterServices()
        {
            // 서비스 등록은 ServiceContainer에서 처리
            ServiceRegistration.RegisterServices(_container);
        }

        private static void CreateControlForm()
        {
            try
            {
                // 필요한 서비스들 가져오기 (오버레이 관련 제거)
                var gameDetectionService = _container.Resolve<IGameDetector>();
                var memoryService = _container.Resolve<IMemoryService>();
                var inGameDetector = _container.Resolve<IInGameDetector>();
                var unitService = _container.Resolve<IUnitService>();

                // 컨트롤 폼 생성 (단순한 모니터링 전용)
                _controlForm = new ControlForm(gameDetectionService, memoryService, inGameDetector, unitService);

                // 게임 감지 서비스 즉시 시작 (자동 모니터링)
                gameDetectionService.StartDetection();
                Console.WriteLine("게임 감지 서비스 시작됨 - 스타크래프트 프로세스를 모니터링합니다.");

                Console.WriteLine("컨트롤 폼 생성 완료");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"컨트롤 폼 생성 실패: {ex.Message}");
                throw;
            }
        }

        private static void Cleanup()
        {
            try
            {
                Console.WriteLine("애플리케이션 종료 중...");

                _controlForm?.Dispose();
                _container?.Dispose();

                Console.WriteLine("정리 완료.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"정리 중 오류 발생: {ex.Message}");
            }
        }
    }
}