using StarcUp.Common.Events;
using System;

namespace StarcUp.Business.InGameStateMonitor
{
    /// <summary>
    /// 포인터 모니터링 서비스 인터페이스
    /// </summary>
    public interface IInGameStateMonitor : IDisposable
    {
        bool IsInGame { get; }
        event EventHandler<InGameStateEventArgs> InGameStateChanged;

        void StartMonitoring(int processId);
        void StopMonitoring();
    }
}