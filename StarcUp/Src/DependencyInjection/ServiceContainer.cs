using System;
using System.Collections.Generic;
using StarcUp.Business.GameDetection;
using StarcUp.Business.InGameStateMonitor;
using StarcUp.Business.MemoryService;
using StarcUp.Infrastructure.Memory;
using StarcUp.Infrastructure.Windows;
using StarcUp.Src.Infrastructure.Memory;

namespace StarcUp.DependencyInjection
{
    public class ServiceContainer
    {
        private readonly Dictionary<Type, object> _singletonServices = new Dictionary<Type, object>();
        private readonly Dictionary<Type, Func<ServiceContainer, object>> _transientFactories = new Dictionary<Type, Func<ServiceContainer, object>>();

        /// <summary>
        /// 싱글톤 서비스 등록 (인스턴스 직접 등록)
        /// </summary>
        public void RegisterSingleton<TInterface, TImplementation>(TImplementation instance)
            where TImplementation : class, TInterface
        {
            if (instance == null)
                throw new ArgumentNullException(nameof(instance));

            _singletonServices[typeof(TInterface)] = instance;
        }

        /// <summary>
        /// 싱글톤 서비스 등록 (팩토리 함수로 등록)
        /// </summary>
        public void RegisterSingleton<TInterface>(Func<ServiceContainer, TInterface> factory)
        {
            if (factory == null)
                throw new ArgumentNullException(nameof(factory));

            var instance = factory(this);
            _singletonServices[typeof(TInterface)] = instance;
        }

        /// <summary>
        /// 트랜지언트 서비스 등록
        /// </summary>
        public void RegisterTransient<TInterface, TImplementation>()
            where TImplementation : class, TInterface, new()
        {
            _transientFactories[typeof(TInterface)] = container => new TImplementation();
        }

        /// <summary>
        /// 트랜지언트 서비스 등록 (팩토리 함수로)
        /// </summary>
        public void RegisterTransient<TInterface>(Func<ServiceContainer, TInterface> factory)
        {
            if (factory == null)
                throw new ArgumentNullException(nameof(factory));

            _transientFactories[typeof(TInterface)] = container => factory(container);
        }

        /// <summary>
        /// 서비스 해결
        /// </summary>
        public T Resolve<T>()
        {
            var serviceType = typeof(T);

            // 싱글톤 서비스 확인
            if (_singletonServices.TryGetValue(serviceType, out var singletonService))
            {
                return (T)singletonService;
            }

            // 트랜지언트 서비스 확인
            if (_transientFactories.TryGetValue(serviceType, out var factory))
            {
                return (T)factory(this);
            }

            throw new InvalidOperationException($"Service of type {serviceType.Name} is not registered.");
        }

        /// <summary>
        /// 서비스가 등록되어 있는지 확인
        /// </summary>
        public bool IsRegistered<T>()
        {
            var serviceType = typeof(T);
            return _singletonServices.ContainsKey(serviceType) || _transientFactories.ContainsKey(serviceType);
        }

        /// <summary>
        /// 모든 IDisposable 서비스 정리
        /// </summary>
        public void Dispose()
        {
            foreach (var service in _singletonServices.Values)
            {
                if (service is IDisposable disposable)
                {
                    try
                    {
                        disposable.Dispose();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"서비스 정리 중 오류: {ex.Message}");
                    }
                }
            }

            _singletonServices.Clear();
            _transientFactories.Clear();
        }
    }

    /// <summary>
    /// 하이브리드 감지 시스템 서비스 등록
    /// </summary>
    public static class ServiceRegistration
    {
        public static void RegisterServices(ServiceContainer container)
        {
            Console.WriteLine("🚀 하이브리드 감지 시스템 서비스 등록 중...");
            Console.WriteLine();

            // Infrastructure Services
            container.RegisterSingleton<IMemoryReader>(
                c => new OptimizedMemoryReader());

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
            Console.WriteLine("   📖 MemoryReader - 메모리 읽기 서비스");
            Console.WriteLine("   🪟 WindowManager - 윈도우 관리 서비스");
            Console.WriteLine("   🧠 MemoryService - 메모리 비즈니스 로직");
            Console.WriteLine("   🎮 HybridStarcraftDetector - 하이브리드 게임 감지");
            Console.WriteLine();
            Console.WriteLine("🎯 하이브리드 감지 시스템 특징:");
            Console.WriteLine("   ⚡ 게임 시작: 2초 간격 폴링 (안정적)");
            Console.WriteLine("   🎯 게임 종료: Process.Exited 이벤트 (즉시)");
            Console.WriteLine("   🔄 자동 모드 전환: 폴링 ↔ 이벤트");
            Console.WriteLine("   📊 성능 최적화: CPU 사용량 최소화");
            Console.WriteLine("   🛡️ 관리자 권한: 완전히 불필요");
            Console.WriteLine();
        }
    }
}