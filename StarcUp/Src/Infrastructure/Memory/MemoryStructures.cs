using System;
using System.Runtime.InteropServices;
using System.Text;

namespace StarcUp.Infrastructure.Memory
{
    /// <summary>
    /// 메모리 관련 Windows API 및 구조체
    /// </summary>
    public static class MemoryAPI
    {
        // Windows API 함수들
        [DllImport("kernel32.dll")]
        public static extern nint OpenProcess(uint processAccess, bool inheritHandle, int processId);

        [DllImport("kernel32.dll")]
        public static extern bool CloseHandle(nint handle);

        [DllImport("kernel32.dll")]
        public static extern bool ReadProcessMemory(nint processHandle, nint baseAddress,
            byte[] buffer, int bufferSize, out nint bytesRead);

        [DllImport("kernel32.dll")]
        public static extern nint CreateToolhelp32Snapshot(uint flags, uint processId);

        [DllImport("kernel32.dll")]
        public static extern bool Thread32First(nint snapshot, ref THREADENTRY32 threadEntry);

        [DllImport("kernel32.dll")]
        public static extern bool Thread32Next(nint snapshot, ref THREADENTRY32 threadEntry);

        [DllImport("kernel32.dll")]
        public static extern nint OpenThread(uint desiredAccess, bool inheritHandle, uint threadId);

        [DllImport("ntdll.dll")]
        public static extern int NtQueryInformationThread(nint threadHandle, int threadInformationClass,
            out THREAD_BASIC_INFORMATION threadInformation, int threadInformationLength, nint returnLength);

        [DllImport("psapi.dll")]
        public static extern bool EnumProcessModules(nint processHandle, nint[] modules,
            uint size, out uint needed);

        [DllImport("psapi.dll")]
        public static extern uint GetModuleBaseName(nint processHandle, nint module,
            StringBuilder baseName, uint size);

        [DllImport("psapi.dll")]
        public static extern bool GetModuleInformation(nint processHandle, nint module,
            out MODULEINFO moduleInfo, uint size);

        /// <summary>
        /// nint 버퍼를 받는 ReadProcessMemory 오버로드
        /// </summary>
        [DllImport("kernel32.dll", EntryPoint = "ReadProcessMemory")]
        public static extern bool ReadProcessMemory(
            nint hProcess,
            nint lpBaseAddress,
            nint lpBuffer,
            int dwSize,
            out nint lpNumberOfBytesRead);

        /// <summary>
        /// 안전한 Unsafe 메모리 읽기 래퍼
        /// </summary>
        public static unsafe bool ReadProcessMemory(
            nint processHandle,
            nint address,
            void* buffer,
            int size)
        {
            return ReadProcessMemory(processHandle, address, (nint)buffer, size, out _);
        }

        // 상수들
        public const uint PROCESS_QUERY_INFORMATION = 0x0400;
        public const uint PROCESS_VM_READ = 0x0010;
        public const uint TH32CS_SNAPTHREAD = 0x00000004;
        public const uint THREAD_QUERY_INFORMATION = 0x0040;
        public const int ThreadBasicInformation = 0;

        // 구조체들
        [StructLayout(LayoutKind.Sequential)]
        public struct THREADENTRY32
        {
            public uint dwSize;
            public uint cntUsage;
            public uint th32ThreadID;
            public uint th32OwnerProcessID;
            public int tpBasePri;
            public int tpDeltaPri;
            public uint dwFlags;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct THREAD_BASIC_INFORMATION
        {
            public uint ExitStatus;
            public uint Padding1;
            public nint TebBaseAddress;
            public CLIENT_ID ClientId;
            public nint AffinityMask;
            public int Priority;
            public int BasePriority;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct CLIENT_ID
        {
            public nint UniqueProcess;
            public nint UniqueThread;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MODULEINFO
        {
            public nint lpBaseOfDll;
            public uint SizeOfImage;
            public nint EntryPoint;
        }
    }
}