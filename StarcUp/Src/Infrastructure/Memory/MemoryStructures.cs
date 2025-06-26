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
        // 기본 Windows API 함수들
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

        [DllImport("ntdll.dll")]
        public static extern int NtQueryInformationProcess(
            nint processHandle,
            int processInformationClass,
            ref PROCESS_BASIC_INFORMATION processInformation,
            int processInformationLength,
            ref int returnLength);

        // PSAPI 함수들 (치트엔진 스타일 모듈 열거용)
        [DllImport("psapi.dll", SetLastError = true)]
        public static extern bool EnumProcessModules(
            nint hProcess,
            [Out] IntPtr[] lphModule,
            uint cb,
            out uint lpcbNeeded);

        [DllImport("psapi.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern uint GetModuleFileNameEx(
            nint hProcess,
            IntPtr hModule,
            StringBuilder lpBaseName,
            uint nSize);

        [DllImport("psapi.dll", SetLastError = true)]
        public static extern bool GetModuleInformation(
            nint hProcess,
            IntPtr hModule,
            out MODULEINFO lpmodinfo,
            uint cb);

        // 기존 ToolHelp32 모듈 관련 함수들
        [DllImport("kernel32.dll")]
        public static extern bool Module32First(nint snapshot, ref MODULEENTRY32 moduleEntry);

        [DllImport("kernel32.dll")]
        public static extern bool Module32Next(nint snapshot, ref MODULEENTRY32 moduleEntry);

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

        // 프로세스 정보 타입 상수
        public const int ProcessBasicInformation = 0;

        [StructLayout(LayoutKind.Sequential)]
        public struct PROCESS_BASIC_INFORMATION
        {
            public nint Reserved1;
            public nint PebBaseAddress;      // PEB 주소
            public nint Reserved2_0;
            public nint Reserved2_1;
            public nint UniqueProcessId;     // 프로세스 ID
            public nint Reserved3;
        }

        /// <summary>
        /// 모듈 정보를 담는 구조체 (CreateToolhelp32Snapshot 사용)
        /// </summary>
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public struct MODULEENTRY32
        {
            public uint dwSize;
            public uint th32ModuleID;
            public uint th32ProcessID;
            public uint GlblcntUsage;
            public uint ProccntUsage;
            public nint modBaseAddr;
            public uint modBaseSize;
            public nint hModule;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
            public string szModule;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string szExePath;
        }

        /// <summary>
        /// MODULEENTRY32 구조체 초기화 헬퍼
        /// </summary>
        public static MODULEENTRY32 CreateModuleEntry32()
        {
            var entry = new MODULEENTRY32();
            entry.dwSize = (uint)Marshal.SizeOf(typeof(MODULEENTRY32));
            return entry;
        }

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

        // 모듈 스냅샷을 위한 추가 상수
        public const uint TH32CS_SNAPMODULE = 0x00000008;
        public const uint TH32CS_SNAPMODULE32 = 0x00000010;
    }
}