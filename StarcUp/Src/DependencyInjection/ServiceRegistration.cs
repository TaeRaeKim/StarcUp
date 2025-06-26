using StarcUp.Business.GameDetection;
using StarcUp.Business.InGameStateMonitor;
using StarcUp.Business.MemoryService;
using StarcUp.Infrastructure.Memory;
using StarcUp.Infrastructure.Windows;

namespace StarcUp.DependencyInjection
{    /// <summary>
     /// 하이브리드 감지 시스템 서비스 등록
     /// </summary>
    public static class ServiceRegistration
    {
        public static void RegisterServices(ServiceContainer container)
        {
            Console.WriteLine("🚀 싱글톤 서비스 등록 중...");
            Console.WriteLine();

            // Infrastructure Services
            container.RegisterSingleton<IMemoryReader>(
                c => new MemoryReader());
            container.RegisterSingleton<IWindowManager>(
                c => new WindowManager());

            // Business Services
            container.RegisterSingleton<IMemoryService>(
                c => new MemoryService(
                    c.Resolve<IMemoryReader>()));

            // Game Detection Services
            container.RegisterSingleton<IGameDetector>(
                c => new GameDetector(
                    c.Resolve<IWindowManager>()));

            // InGameStateMonitor
            container.RegisterSingleton<IInGameStateMonitor>(
                c => new InGameStateMonitor(
                    c.Resolve<IMemoryService>()));

            //container.RegisterSingleton<IGameManager>(
            //    c => new GameManager(
            //        c.Resolve<IPointerMonitorService>()));

            Console.WriteLine("✅ 서비스 등록 완료:");
            Console.WriteLine("   📖 MemoryReader - 통합된 메모리 읽기 서비스");
            Console.WriteLine("   🪟 WindowManager - 윈도우 관리 서비스");
            Console.WriteLine("   🧠 MemoryService - 메모리 비즈니스 로직");
            Console.WriteLine("   🎮 GameDetector - 게임 감지 서비스");
            Console.WriteLine("   📊 InGameStateMonitor - 게임 상태 모니터링 서비스");
        }
    }
}
