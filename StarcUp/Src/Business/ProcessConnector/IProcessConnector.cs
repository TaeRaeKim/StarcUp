using System;
using System.Collections.Generic;

namespace StarcUp.Business.Memory
{

    /// <summary>
    /// 메모리 서비스 인터페이스
    /// </summary>
    public interface IProcessConnector : IDisposable
    {
        bool ConnectToProcess(int processId);
        void Disconnect();
        IntPtr GetStackStart(int threadIndex = 0);
        List<TebInfo> GetTebAddresses();
        bool IsConnected { get; }
        int ConnectedProcessId { get; }
    }


}