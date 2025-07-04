using StarcUp.DependencyInjection;
using System;

namespace StarcUp
{
    public static class Program
    {
        private static ServiceContainer _container;
        [STAThread]
        static void Main()
        {

            Console.WriteLine("StarcUp - 스타크래프트 메모리 모니터");
            Console.WriteLine("=====================================");

            try
            {
                _container = new ServiceContainer();
                RegisterServices();

            }
            catch (Exception ex)
            {
                Console.WriteLine($"애플리케이션 시작 실패: {ex.Message}");
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