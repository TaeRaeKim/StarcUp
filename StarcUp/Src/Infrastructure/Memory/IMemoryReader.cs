using System;
using System.Collections.Generic;
using StarcUp.Business.Memory;

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

        nint GetPebAddress();
        bool GetUser32ModuleInfo(out MemoryAPI.MODULEENTRY32 moduleInfo);
        List<TebInfo> GetTebAddresses();
        nint GetStackStart(int threadIndex = 0);
        nint GetStackTop(int threadIndex);
        nint ReadPointer(nint address);
        bool ReadProcessMemory(nint address, byte[] buffer, int size);

        byte[] ReadMemory(nint address, int size);
        T ReadStructure<T>(nint address) where T : struct;
        T[] ReadStructureArray<T>(nint address, int count) where T : struct;
    }
}