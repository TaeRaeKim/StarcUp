using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace StarcUp
{
    public class MemoryReader
    {
        // Windows API 함수들
        [DllImport("kernel32.dll")]
        private static extern IntPtr OpenProcess(uint processAccess, bool inheritHandle, int processId);

        [DllImport("kernel32.dll")]
        private static extern bool CloseHandle(IntPtr handle);

        [DllImport("kernel32.dll")]
        private static extern bool ReadProcessMemory(IntPtr processHandle, IntPtr baseAddress,
            byte[] buffer, int bufferSize, out IntPtr bytesRead);

        [DllImport("kernel32.dll")]
        private static extern IntPtr CreateToolhelp32Snapshot(uint flags, uint processId);

        [DllImport("kernel32.dll")]
        private static extern bool Thread32First(IntPtr snapshot, ref THREADENTRY32 threadEntry);

        [DllImport("kernel32.dll")]
        private static extern bool Thread32Next(IntPtr snapshot, ref THREADENTRY32 threadEntry);

        [DllImport("kernel32.dll")]
        private static extern IntPtr OpenThread(uint desiredAccess, bool inheritHandle, uint threadId);

        [DllImport("ntdll.dll")]
        private static extern int NtQueryInformationThread(IntPtr threadHandle, int threadInformationClass,
            out THREAD_BASIC_INFORMATION threadInformation, int threadInformationLength, IntPtr returnLength);

        [DllImport("psapi.dll")]
        private static extern bool EnumProcessModules(IntPtr processHandle, IntPtr[] modules,
            uint size, out uint needed);

        [DllImport("psapi.dll")]
        private static extern uint GetModuleBaseName(IntPtr processHandle, IntPtr module,
            StringBuilder baseName, uint size);

        [DllImport("psapi.dll")]
        private static extern bool GetModuleInformation(IntPtr processHandle, IntPtr module,
            out MODULEINFO moduleInfo, uint size);

        // 상수들
        private const uint PROCESS_QUERY_INFORMATION = 0x0400;
        private const uint PROCESS_VM_READ = 0x0010;
        private const uint TH32CS_SNAPTHREAD = 0x00000004;
        private const uint THREAD_QUERY_INFORMATION = 0x0040;
        private const int ThreadBasicInformation = 0;

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

        public struct TebInfo
        {
            public uint ThreadId;
            public IntPtr TebAddress;
            public int Index;
        }

        // 멤버 변수들
        private Process starcraftProcess;
        private IntPtr processHandle;

        // 스타크래프트에 연결
        public bool ConnectToStarcraft()
        {
            try
            {
                Process[] processes = Process.GetProcessesByName("StarCraft");
                if (processes.Length == 0)
                {
                    Console.WriteLine("스타크래프트 프로세스를 찾을 수 없습니다.");
                    return false;
                }

                starcraftProcess = processes[0];
                processHandle = OpenProcess(PROCESS_QUERY_INFORMATION | PROCESS_VM_READ, false, starcraftProcess.Id);

                if (processHandle != IntPtr.Zero)
                {
                    Console.WriteLine($"스타크래프트 연결 성공! PID: {starcraftProcess.Id}");
                    return true;
                }
                else
                {
                    Console.WriteLine("프로세스 핸들 열기 실패 (관리자 권한 필요)");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"연결 실패: {ex.Message}");
                return false;
            }
        }

        // TEB 주소들 가져오기
        public List<TebInfo> GetTebAddresses()
        {
            var tebList = new List<TebInfo>();

            if (starcraftProcess == null || processHandle == IntPtr.Zero)
            {
                Console.WriteLine("스타크래프트에 연결되지 않음");
                return tebList;
            }

            IntPtr snapshot = CreateToolhelp32Snapshot(TH32CS_SNAPTHREAD, 0);
            if (snapshot == IntPtr.Zero)
            {
                Console.WriteLine("스레드 스냅샷 생성 실패");
                return tebList;
            }

            try
            {
                THREADENTRY32 threadEntry = new THREADENTRY32();
                threadEntry.dwSize = (uint)Marshal.SizeOf(typeof(THREADENTRY32));

                int index = 0;
                if (Thread32First(snapshot, ref threadEntry))
                {
                    do
                    {
                        if (threadEntry.th32OwnerProcessID == starcraftProcess.Id)
                        {
                            IntPtr tebAddress = GetTebAddress(threadEntry.th32ThreadID);
                            if (tebAddress != IntPtr.Zero)
                            {
                                var tebInfo = new TebInfo
                                {
                                    ThreadId = threadEntry.th32ThreadID,
                                    TebAddress = tebAddress,
                                    Index = index
                                };
                                tebList.Add(tebInfo);

                                Console.WriteLine($"TEB{index}: 스레드={threadEntry.th32ThreadID}, 주소=0x{tebAddress.ToInt64():X16}");
                                index++;
                            }
                        }
                    }
                    while (Thread32Next(snapshot, ref threadEntry));
                }
            }
            finally
            {
                CloseHandle(snapshot);
            }

            Console.WriteLine($"총 {tebList.Count}개 TEB 주소 발견");
            return tebList;
        }

        // Pascal 코드의 GetStackStart 함수 구현
        public IntPtr GetStackStart(int threadIndex = 0)
        {
            if (starcraftProcess == null || processHandle == IntPtr.Zero)
            {
                Console.WriteLine("스타크래프트에 연결되지 않음");
                return IntPtr.Zero;
            }

            // 1단계: kernel32.dll 모듈 정보 가져오기
            MODULEINFO kernel32Info;
            if (!GetKernel32ModuleInfo(out kernel32Info))
            {
                Console.WriteLine("kernel32.dll 모듈 정보를 가져올 수 없습니다.");
                return IntPtr.Zero;
            }

            Console.WriteLine($"kernel32.dll: 베이스=0x{kernel32Info.lpBaseOfDll.ToInt64():X16}, 크기=0x{kernel32Info.SizeOfImage:X}");

            // 2단계: 지정된 스레드의 StackTop 가져오기
            IntPtr stackTop = GetStackTop(threadIndex);
            if (stackTop == IntPtr.Zero)
            {
                Console.WriteLine("StackTop을 가져올 수 없습니다.");
                return IntPtr.Zero;
            }

            Console.WriteLine($"StackTop: 0x{stackTop.ToInt64():X16}");

            // 3단계: StackTop-4096 영역에서 4096바이트 읽기
            IntPtr stackSearchStart = new IntPtr(stackTop.ToInt64() - 4096);
            byte[] stackBuffer = new byte[4096];
            IntPtr bytesRead;

            if (!ReadProcessMemory(processHandle, stackSearchStart, stackBuffer, 4096, out bytesRead))
            {
                Console.WriteLine("스택 메모리 읽기 실패");
                return IntPtr.Zero;
            }

            Console.WriteLine($"스택 검색 시작: 0x{stackSearchStart.ToInt64():X16}, 읽은 바이트: {bytesRead.ToInt32()}");

            // 4단계: kernel32 범위에 있는 주소 찾기 (64비트)
            IntPtr resultAddress = IntPtr.Zero;
            int pointerSize = 8; // 64비트
            int numPointers = 4096 / pointerSize;

            Console.WriteLine("kernel32 주소를 가리키는 스택 엔트리 검색 중...");

            for (int i = numPointers - 1; i >= 0; i--) // 역방향 검색
            {
                // 8바이트씩 읽어서 포인터 값 확인
                long pointerValue = BitConverter.ToInt64(stackBuffer, i * pointerSize);
                IntPtr pointer = new IntPtr(pointerValue);

                // kernel32 범위 내에 있는지 확인
                if (IsInRange(pointer, kernel32Info.lpBaseOfDll, kernel32Info.SizeOfImage))
                {
                    resultAddress = new IntPtr(stackSearchStart.ToInt64() + (i * pointerSize));
                    Console.WriteLine($"kernel32 주소 발견: 스택 위치=0x{resultAddress.ToInt64():X16}, 가리키는 주소=0x{pointerValue:X16}");
                    break;
                }
            }

            if (resultAddress != IntPtr.Zero)
            {
                Console.WriteLine($"최종 StackStart: 0x{resultAddress.ToInt64():X16}");
                return resultAddress;
            }
            else
            {
                Console.WriteLine("kernel32를 가리키는 스택 엔트리를 찾을 수 없습니다.");
                return IntPtr.Zero;
            }
        }

        // TEB+8에서 StackTop 읽기
        public IntPtr GetStackTop(int threadIndex)
        {
            IntPtr snapshot = CreateToolhelp32Snapshot(TH32CS_SNAPTHREAD, 0);
            if (snapshot == IntPtr.Zero)
            {
                return IntPtr.Zero;
            }

            try
            {
                THREADENTRY32 threadEntry = new THREADENTRY32();
                threadEntry.dwSize = (uint)Marshal.SizeOf(typeof(THREADENTRY32));

                int currentIndex = 0;
                if (Thread32First(snapshot, ref threadEntry))
                {
                    do
                    {
                        if (threadEntry.th32OwnerProcessID == starcraftProcess.Id)
                        {
                            if (currentIndex == threadIndex)
                            {
                                IntPtr threadHandle = OpenThread(THREAD_QUERY_INFORMATION, false, threadEntry.th32ThreadID);
                                if (threadHandle == IntPtr.Zero)
                                {
                                    return IntPtr.Zero;
                                }

                                try
                                {
                                    THREAD_BASIC_INFORMATION tbi = new THREAD_BASIC_INFORMATION();
                                    int result = NtQueryInformationThread(
                                        threadHandle,
                                        ThreadBasicInformation,
                                        out tbi,
                                        Marshal.SizeOf(typeof(THREAD_BASIC_INFORMATION)),
                                        IntPtr.Zero);

                                    if (result == 0) // 성공
                                    {
                                        // TEB+8에서 StackTop 읽기
                                        IntPtr stackTopAddress = new IntPtr(tbi.TebBaseAddress.ToInt64() + 8);
                                        IntPtr stackTop = ReadPointer(stackTopAddress);
                                        return stackTop;
                                    }
                                }
                                finally
                                {
                                    CloseHandle(threadHandle);
                                }
                                break;
                            }
                            currentIndex++;
                        }
                    }
                    while (Thread32Next(snapshot, ref threadEntry));
                }
            }
            finally
            {
                CloseHandle(snapshot);
            }

            return IntPtr.Zero;
        }

        // 개별 TEB 주소 가져오기
        private IntPtr GetTebAddress(uint threadId)
        {
            IntPtr threadHandle = OpenThread(THREAD_QUERY_INFORMATION, false, threadId);
            if (threadHandle == IntPtr.Zero)
                return IntPtr.Zero;

            try
            {
                THREAD_BASIC_INFORMATION basicInfo = new THREAD_BASIC_INFORMATION();
                int result = NtQueryInformationThread(
                    threadHandle,
                    ThreadBasicInformation,
                    out basicInfo,
                    Marshal.SizeOf(typeof(THREAD_BASIC_INFORMATION)),
                    IntPtr.Zero);

                if (result == 0) // 성공
                {
                    return basicInfo.TebBaseAddress;
                }
            }
            catch
            {
                // 조용히 실패
            }
            finally
            {
                CloseHandle(threadHandle);
            }

            return IntPtr.Zero;
        }

        // kernel32.dll 모듈 정보 가져오기
        private bool GetKernel32ModuleInfo(out MODULEINFO moduleInfo)
        {
            moduleInfo = new MODULEINFO();

            IntPtr[] modules = new IntPtr[1024];
            uint needed;

            if (!EnumProcessModules(processHandle, modules, (uint)(modules.Length * IntPtr.Size), out needed))
            {
                return false;
            }

            int moduleCount = (int)(needed / IntPtr.Size);
            StringBuilder moduleBaseName = new StringBuilder(256);

            for (int i = 0; i < moduleCount; i++)
            {
                if (modules[i] != IntPtr.Zero)
                {
                    if (GetModuleBaseName(processHandle, modules[i], moduleBaseName, 256) > 0)
                    {
                        string moduleName = moduleBaseName.ToString();
                        if (moduleName.ToLower() == "kernel32.dll")
                        {
                            return GetModuleInformation(processHandle, modules[i], out moduleInfo, (uint)Marshal.SizeOf(typeof(MODULEINFO)));
                        }
                    }
                }
            }

            return false;
        }

        // 메모리에서 8바이트 포인터 읽기
        private IntPtr ReadPointer(IntPtr address)
        {
            byte[] buffer = new byte[8];
            IntPtr bytesRead;

            if (ReadProcessMemory(processHandle, address, buffer, 8, out bytesRead))
            {
                if (bytesRead.ToInt32() == 8)
                {
                    long value = BitConverter.ToInt64(buffer, 0);
                    return new IntPtr(value);
                }
            }

            return IntPtr.Zero;
        }

        // 주소가 지정된 범위 내에 있는지 확인
        private bool IsInRange(IntPtr address, IntPtr baseAddress, uint size)
        {
            long addr = address.ToInt64();
            long baseAddr = baseAddress.ToInt64();

            return addr >= baseAddr && addr < (baseAddr + size);
        }

        // 연결 해제
        public void Disconnect()
        {
            if (processHandle != IntPtr.Zero)
            {
                CloseHandle(processHandle);
                processHandle = IntPtr.Zero;
            }
            starcraftProcess = null;
        }
    }
}