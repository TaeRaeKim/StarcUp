using System;
using System.Text;

namespace StarcUp.Infrastructure.Memory
{
    /// <summary>
    /// Windows API를 직접 사용하는 저수준 메모리 리더 인터페이스
    /// - 단순한 Windows API 래퍼 메소드들만 정의
    /// - 복합적인 비즈니스 로직은 상위 MemoryService에서 처리
    /// </summary>
    public interface IMemoryReader : IDisposable
    {
        bool IsConnected { get; }
        int ConnectedProcessId { get; }

        bool ConnectToProcess(int processId);
        void Disconnect();

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

        nint CreateThreadSnapshot();
        bool GetFirstThread(nint snapshot, out MemoryAPI.THREADENTRY32 threadEntry);
        bool GetNextThread(nint snapshot, ref MemoryAPI.THREADENTRY32 threadEntry);
        nint GetThread(uint threadId);

        nint CreateModuleSnapshot();
        bool GetFirstModule(nint snapshot, out MemoryAPI.MODULEENTRY32 moduleEntry);
        bool GetNextModule(nint snapshot, ref MemoryAPI.MODULEENTRY32 moduleEntry);
        bool GetModuleInformation(nint moduleBase, out MemoryAPI.MODULEINFO moduleInfo);

        int QueryProcessInformation(out MemoryAPI.PROCESS_BASIC_INFORMATION processInfo);

        void CloseHandle(nint handle);
    }
}