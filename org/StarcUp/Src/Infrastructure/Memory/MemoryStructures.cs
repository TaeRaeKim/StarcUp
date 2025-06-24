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
        public static extern IntPtr OpenProcess(uint processAccess, bool inheritHandle, int processId);

        [DllImport("kernel32.dll")]
        public static extern bool CloseHandle(IntPtr handle);

        [DllImport("kernel32.dll")]
        public static extern bool ReadProcessMemory(IntPtr processHandle, IntPtr baseAddress,
            byte[] buffer, int bufferSize, out IntPtr bytesRead);

        [DllImport("kernel32.dll")]
        public static extern IntPtr CreateToolhelp32Snapshot(uint flags, uint processId);

        [DllImport("kernel32.dll")]
        public static extern bool Thread32First(IntPtr snapshot, ref THREADENTRY32 threadEntry);

        [DllImport("kernel32.dll")]
        public static extern bool Thread32Next(IntPtr snapshot, ref THREADENTRY32 threadEntry);

        [DllImport("kernel32.dll")]
        public static extern IntPtr OpenThread(uint desiredAccess, bool inheritHandle, uint threadId);

        [DllImport("ntdll.dll")]
        public static extern int NtQueryInformationThread(IntPtr threadHandle, int threadInformationClass,
            out THREAD_BASIC_INFORMATION threadInformation, int threadInformationLength, IntPtr returnLength);

        [DllImport("psapi.dll")]
        public static extern bool EnumProcessModules(IntPtr processHandle, IntPtr[] modules,
            uint size, out uint needed);

        [DllImport("psapi.dll")]
        public static extern uint GetModuleBaseName(IntPtr processHandle, IntPtr module,
            StringBuilder baseName, uint size);

        [DllImport("psapi.dll")]
        public static extern bool GetModuleInformation(IntPtr processHandle, IntPtr module,
            out MODULEINFO moduleInfo, uint size);

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
            public IntPtr TebBaseAddress;
            public CLIENT_ID ClientId;
            public IntPtr AffinityMask;
            public int Priority;
            public int BasePriority;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct CLIENT_ID
        {
            public IntPtr UniqueProcess;
            public IntPtr UniqueThread;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MODULEINFO
        {
            public IntPtr lpBaseOfDll;
            public uint SizeOfImage;
            public IntPtr EntryPoint;
        }
    }
}