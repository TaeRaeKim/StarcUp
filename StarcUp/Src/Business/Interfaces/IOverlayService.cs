using StarcUp.Common.Events;
using System;

namespace StarcUp.Business.Interfaces
{
    /// <summary>
    /// 오버레이 서비스 인터페이스 (메인 컨트롤러)
    /// </summary>
    public interface IOverlayService : IDisposable
    {
        event EventHandler<PointerEventArgs> PointerValueChanged;

        bool IsRunning { get; }

        void Start();
        void Stop();
    }
}