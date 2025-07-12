using System;
using System.Threading.Tasks;
using StarcUp.Common.Events;

namespace StarcUp.Business.Communication
{
    /// <summary>
    /// StarcUp.UI와의 통신을 관리하는 비즈니스 서비스 인터페이스
    /// </summary>
    public interface ICommunicationService : IDisposable
    {
        Task<bool> StartAsync(string pipeName = "StarcUp.Dev");
        void Stop();
        
        bool IsConnected { get; }
        event EventHandler<bool> ConnectionStateChanged;
    }
}