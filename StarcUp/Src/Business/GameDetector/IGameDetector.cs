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
        event EventHandler<GameEventArgs> HandleChanged;
        event EventHandler<GameEventArgs> WindowMove;
        event EventHandler<GameEventArgs> WindowFocusIn;
        event EventHandler<GameEventArgs> WindowFocusOut;

        bool IsGameRunning { get; }
        GameInfo CurrentGame { get; }

        void StartDetection();
        void StopDetection();
    }
}