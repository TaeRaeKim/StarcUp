using StarcUp.Business.Interfaces;
using StarcUp.DependencyInjection;
using System;
using System.ComponentModel.Design;
using System.Windows.Forms;
using ServiceContainer = StarcUp.DependencyInjection.ServiceContainer;

namespace StarcUp
{
    public static class Program
    {
        private static ServiceContainer _container;
        private static IOverlayService _overlayService;

        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            Console.WriteLine("스타크래프트 오버레이 프로그램 시작");
            Console.WriteLine("=================================");

            try
            {
                // 서비스 컨테이너 초기화 및 서비스 등록
                _container = new ServiceContainer();
                RegisterServices();

                // 메인 오버레이 서비스 시작
                _overlayService = _container.Resolve<IOverlayService>();
                _overlayService.Start();

                Console.WriteLine("서비스 초기화 완료. 스타크래프트를 실행해주세요.");

                // 메시지 루프 실행
                Application.Run();
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

        private static void Cleanup()
        {
            try
            {
                Console.WriteLine("애플리케이션 종료 중...");
                _overlayService?.Stop();
                _overlayService?.Dispose();
                Console.WriteLine("정리 완료.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"정리 중 오류 발생: {ex.Message}");
            }
        }
    }
}