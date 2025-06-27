using System;
using System.Buffers;
using System.Text;

namespace StarcUp.Infrastructure.Memory
{
    /// <summary>
    /// 통합된 메모리 리더 인터페이스
    /// - 기본 메모리 읽기 기능과 최적화된 기능을 모두 포함
    /// - Windows API를 직접 사용하는 저수준 메모리 리더 인터페이스
    /// </summary>
    public interface IMemoryReader : IDisposable
    {
        bool IsConnected { get; }
        int ConnectedProcessId { get; }

        bool ConnectToProcess(int processId);
        void Disconnect();

        // 기본 메모리 읽기 메서드들
        byte[] ReadMemoryRaw(nint address, int size);

        int ReadInt(nint address);
        float ReadFloat(nint address);
        double ReadDouble(nint address);
        byte ReadByte(nint address);
        short ReadShort(nint address);
        long ReadLong(nint address);
        bool ReadBool(nint address);
        nint ReadPointer(nint address);
        string ReadString(nint address, int maxLength = 256, Encoding encoding = null);

        T ReadStructure<T>(nint address) where T : struct;
        T[] ReadStructureArray<T>(nint address, int count) where T : struct;

        // 최적화된 메모리 읽기 메서드들 (버퍼 재사용)
        bool ReadMemoryIntoBuffer(nint address, byte[] buffer, int size);
        unsafe bool ReadStructureDirect<T>(nint address, out T result) where T : unmanaged;
        unsafe bool ReadStructureArrayIntoBuffer<T>(nint address, T[] buffer, int count) where T : unmanaged;

        // 스레드 관련 메서드들
        nint CreateThreadSnapshot();
        bool GetFirstThread(nint snapshot, out MemoryAPI.THREADENTRY32 threadEntry);
        bool GetNextThread(nint snapshot, ref MemoryAPI.THREADENTRY32 threadEntry);
        nint GetThread(uint threadId);

        // 모듈 관련 메서드들
        nint CreateModuleSnapshot();
        bool GetFirstModule(nint snapshot, out MemoryAPI.MODULEENTRY32 moduleEntry);
        bool GetNextModule(nint snapshot, ref MemoryAPI.MODULEENTRY32 moduleEntry);
        bool GetModuleInformation(nint moduleBase, out MemoryAPI.MODULEINFO moduleInfo);

        // 프로세스 관련 메서드들
        int QueryProcessInformation(out MemoryAPI.PROCESS_BASIC_INFORMATION processInfo);
        nint GetProcessHandle();

        // 리소스 관리
        void CloseHandle(nint handle);
    }
}