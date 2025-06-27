using StarcUp.Business.GameDetection;
using StarcUp.Business.InGameDetector;
using StarcUp.Business.MemoryService;
using StarcUp.Business.Units.Runtime.Adapters;
using StarcUp.Business.Units.Runtime.Services;
using StarcUp.Business.Units.StaticData.Repositories;
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
            container.RegisterSingleton<IInGameDetector>(
                c => new InGameDetector(
                    c.Resolve<IMemoryService>()));

            // Unit Services
            container.RegisterSingleton<IUnitInfoRepository>(
                c => new UnitInfoRepository());
            container.RegisterSingleton<IUnitMemoryAdapter>(
                c => new UnitMemoryAdapter(
                    c.Resolve<IMemoryReader>()));
            container.RegisterSingleton<IUnitService>(
                c => new UnitService(
                    c.Resolve<IUnitMemoryAdapter>()));

            //container.RegisterSingleton<IGameManager>(
            //    c => new GameManager(
            //        c.Resolve<IPointerMonitorService>()));

            Console.WriteLine("✅ 서비스 등록 완료:");
            Console.WriteLine("   📖 MemoryReader - 통합된 메모리 읽기 서비스");
            Console.WriteLine("   🪟 WindowManager - 윈도우 관리 서비스");
            Console.WriteLine("   🧠 MemoryService - 메모리 비즈니스 로직");
            Console.WriteLine("   🎮 GameDetector - 게임 감지 서비스");
            Console.WriteLine("   📊 InGameStateMonitor - 게임 상태 모니터링 서비스");
            Console.WriteLine("   🏗️ UnitInfoRepository - 유닛 정적 데이터 저장소");
            Console.WriteLine("   🔗 UnitMemoryAdapter - 유닛 메모리 접근 어댑터");
            Console.WriteLine("   ⚙️ UnitService - 유닛 비즈니스 로직 서비스");
        }
    }
}
