using StarcUp.Business.Models;
using StarcUp.Common.Events;
using System;

namespace StarcUp.Business.Interfaces
{
    /// <summary>
    /// 게임 감지 서비스 인터페이스
    /// </summary>
    public interface IGameDetectionService : IDisposable
    {
        event EventHandler<GameEventArgs> GameFound;
        event EventHandler<GameEventArgs> GameLost;
        event EventHandler<GameEventArgs> WindowChanged;
        event EventHandler<GameEventArgs> WindowActivated;
        event EventHandler<GameEventArgs> WindowDeactivated;

        bool IsGameRunning { get; }
        GameInfo CurrentGame { get; }

        void StartDetection();
        void StopDetection();
    }
}