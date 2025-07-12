using StarcUp.Business.GameDetection;
using StarcUp.Business.InGameDetector;
using StarcUp.Business.MemoryService;
using StarcUp.Business.Units.Runtime.Adapters;
using StarcUp.Business.Units.Runtime.Services;
using StarcUp.Business.Units.Runtime.Repositories;
using StarcUp.Business.Units.StaticData.Repositories;
using StarcUp.Business.Game;
using StarcUp.Business.GameManager.Extensions;
using StarcUp.Infrastructure.Memory;
using StarcUp.Infrastructure.Windows;
using StarcUp.Infrastructure.Communication;
using StarcUp.Business.Communication;

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

            // Offset Repository
            container.RegisterSingleton(
                c => new UnitOffsetRepository("Data"));

            // Game Detection Services
            container.RegisterSingleton<IGameDetector>(
                c => new GameDetector());

            // Business Services
            container.RegisterSingleton<IMemoryService>(
                c => new MemoryService(
                    c.Resolve<IGameDetector>(),
                    c.Resolve<IMemoryReader>()));

            // InGameStateMonitor
            container.RegisterSingleton<IInGameDetector>(
                c => new InGameDetector(
                    c.Resolve<IMemoryService>(),
                    c.Resolve<UnitOffsetRepository>()));

            // Unit Services
            container.RegisterSingleton<IUnitInfoRepository>(
                c => new UnitInfoRepository());
            container.RegisterSingleton<IUnitMemoryAdapter>(
                c => new UnitMemoryAdapter(
                    c.Resolve<IMemoryService>()));
            container.RegisterSingleton<IUnitService>(
                c => new UnitService(
                    c.Resolve<IUnitMemoryAdapter>()));

            // Unit Count Services
            container.RegisterSingleton<IUnitCountAdapter>(
                c => new UnitCountAdapter(
                    c.Resolve<IMemoryService>(),
                    c.Resolve<UnitOffsetRepository>()));
            container.RegisterSingleton<IUnitCountService>(
                c => new UnitCountService(
                    c.Resolve<IUnitCountAdapter>()));

            // Communication Services
            container.RegisterSingleton<INamedPipeClient>(
                c => new NamedPipeClient());
            container.RegisterSingleton<ICommunicationService>(
                c => new CommunicationService(
                    c.Resolve<INamedPipeClient>(),
                    c.Resolve<IGameDetector>(),
                    c.Resolve<IInGameDetector>()));

            PlayerExtensions.SetUnitCountService(container.Resolve<IUnitCountService>());
            PlayerExtensions.SetUnitService(container.Resolve<IUnitService>());

            container.RegisterSingleton<IGameManager>(
                c => new GameManager(
                    c.Resolve<IInGameDetector>(),
                    c.Resolve<IUnitService>(),
                    c.Resolve<IMemoryService>(),
                    c.Resolve<IUnitCountService>()));

            Console.WriteLine("✅ 서비스 등록 완료:");
            Console.WriteLine("   📖 MemoryReader - 통합된 메모리 읽기 서비스");
            Console.WriteLine("   🪟 WindowManager - 윈도우 관리 서비스");
            Console.WriteLine("   🧠 MemoryService - 메모리 비즈니스 로직");
            Console.WriteLine("   🎮 GameDetector - 게임 감지 서비스");
            Console.WriteLine("   📊 InGameStateMonitor - 게임 상태 모니터링 서비스");
            Console.WriteLine("   🏗️ UnitInfoRepository - 유닛 정적 데이터 저장소");
            Console.WriteLine("   🔗 UnitMemoryAdapter - 유닛 메모리 접근 어댑터");
            Console.WriteLine("   ⚙️ UnitService - 유닛 비즈니스 로직 서비스");
            Console.WriteLine("   🏗️ UnitOffsetRepository - 유닛 오프셋 설정 저장소");
            Console.WriteLine("   🔢 UnitCountAdapter - 유닛 카운트 메모리 어댑터");
            Console.WriteLine("   📊 UnitCountService - 유닛 카운트 관리 서비스");
            Console.WriteLine("   🔗 NamedPipeClient - Named Pipe 통신 클라이언트");
            Console.WriteLine("   📡 CommunicationService - UI 통신 관리 서비스");
            Console.WriteLine("   🎯 GameManager - 게임 관리 서비스 (자동 유닛 데이터 로딩)");
        }
    }
}
