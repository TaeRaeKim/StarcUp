using StarcUp.Common.Events;
using System;

namespace StarcUp.Business.InGameDetector
{
    /// <summary>
    /// 포인터 모니터링 서비스 인터페이스
    /// </summary>
    public interface IInGameDetector : IDisposable
    {
        bool IsInGame { get; }
        event EventHandler<InGameEventArgs> InGameStateChanged;

        void StartMonitoring(int processId);
        void StopMonitoring();
    }
}