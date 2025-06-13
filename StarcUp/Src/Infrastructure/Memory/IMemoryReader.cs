using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using StarcUp.Business.Models;

namespace StarcUp.Infrastructure.Memory
{
    /// <summary>
    /// 메모리 리더 인터페이스
    /// </summary>
    public interface IMemoryReader : IDisposable
    {
        bool ConnectToProcess(int processId);
        void Disconnect();
        bool IsConnected { get; }
        int ConnectedProcessId { get; }

        List<TebInfo> GetTebAddresses();
        IntPtr GetStackStart(int threadIndex = 0);
        IntPtr GetStackTop(int threadIndex);
        IntPtr ReadPointer(IntPtr address);
        bool ReadProcessMemory(IntPtr address, byte[] buffer, int size);
    }
}