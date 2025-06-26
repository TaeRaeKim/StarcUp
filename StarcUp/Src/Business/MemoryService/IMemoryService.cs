using StarcUp.Common.Events;
using System;
using System.Collections.Generic;

namespace StarcUp.Business.Memory
{

    /// <summary>
    /// 메모리 서비스 인터페이스
    /// </summary>
    public interface IMemoryService : IDisposable
    {
        event EventHandler<ProcessEventArgs> ProcessConnect;
        event EventHandler<ProcessEventArgs> ProcessDisconnect;

        bool ConnectToProcess(int processId);
        void Disconnect();
        nint GetStackStart(int threadIndex = 0);

        nint GetPebAddress();
        nint GetUser32Address();

        List<TebInfo> GetTebAddresses();
        bool IsConnected { get; }
        int ConnectedProcessId { get; }
    }


}