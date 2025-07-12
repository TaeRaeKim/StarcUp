using StarcUp.Common.Events;
using System;

namespace StarcUp.Business.GameDetection
{
    /// <summary>
    /// 게임 감지 서비스 인터페이스
    /// </summary>
    public interface IGameDetector : IDisposable
    {
        event EventHandler<GameEventArgs> HandleFound;
        event EventHandler<GameEventArgs> HandleLost;

        bool IsGameRunning { get; }
        GameInfo CurrentGame { get; }

        void StartDetection();
        void StopDetection();
    }
}