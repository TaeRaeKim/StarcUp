using StarcUp.Business.Models;
using System;
using System.Collections.Generic;

namespace StarcUp.Business.Interfaces
{

    /// <summary>
    /// 메모리 서비스 인터페이스
    /// </summary>
    public interface IMemoryService : IDisposable
    {
        bool ConnectToProcess(int processId);
        void Disconnect();
        IntPtr GetStackStart(int threadIndex = 0);
        List<TebInfo> GetTebAddresses();
        bool IsConnected { get; }
        int ConnectedProcessId { get; }
    }


}