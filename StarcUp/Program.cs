using StarcUp.Business.Interfaces;
using StarcUp.DependencyInjection;
using StarcUp.Presentation.Forms;
using System;
using System.ComponentModel.Design;
using System.Windows.Forms;
using ServiceContainer = StarcUp.DependencyInjection.ServiceContainer;

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

            Console.WriteLine("StarcUp - 스타크래프트 오버레이 컨트롤 프로그램");
            Console.WriteLine("================================================");

            try
            {
                // 서비스 컨테이너 초기화 및 서비스 등록
                _container = new ServiceContainer();
                RegisterServices();

                // 메인 컨트롤 폼 생성 및 표시
                CreateControlForm();

                Console.WriteLine("컨트롤 폼이 준비되었습니다.");
                Console.WriteLine("'추적 시작' 버튼을 클릭하여 오버레이를 활성화하세요.");

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
                // 필요한 서비스들 가져오기
                var overlayService = _container.Resolve<IOverlayService>();
                var gameDetectionService = _container.Resolve<IGameDetectionService>();
                var pointerMonitorService = _container.Resolve<IPointerMonitorService>();

                // 컨트롤 폼 생성
                _controlForm = new ControlForm(overlayService, gameDetectionService, pointerMonitorService);

                // 게임 감지 서비스는 즉시 시작 (게임 상태 모니터링)
                gameDetectionService.StartDetection();
                Console.WriteLine("게임 감지 서비스 시작됨 - 게임 상태를 실시간으로 모니터링합니다.");

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