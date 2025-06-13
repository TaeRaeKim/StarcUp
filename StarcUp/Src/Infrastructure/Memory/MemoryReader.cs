using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using StarcUp.Business.Models;

namespace StarcUp.Infrastructure.Memory
{
    /// <summary>
    /// Windows API를 사용한 메모리 리더 구현
    /// </summary>
    public class MemoryReader : IMemoryReader
    {
        private Process _process;
        private IntPtr _processHandle;
        private bool _isDisposed;

        public bool IsConnected => _processHandle != IntPtr.Zero && _process != null && !_process.HasExited;
        public int ConnectedProcessId => _process?.Id ?? 0;

        public bool ConnectToProcess(int processId)
        {
            try
            {
                // 기존 연결 해제
                Disconnect();

                _process = Process.GetProcessById(processId);
                _processHandle = MemoryAPI.OpenProcess(
                    MemoryAPI.PROCESS_QUERY_INFORMATION | MemoryAPI.PROCESS_VM_READ,
                    false, processId);

                if (_processHandle != IntPtr.Zero)
                {
                    Console.WriteLine($"프로세스 연결 성공! PID: {processId}");
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
                Console.WriteLine($"프로세스 연결 실패: {ex.Message}");
                return false;
            }
        }

        public void Disconnect()
        {
            if (_processHandle != IntPtr.Zero)
            {
                MemoryAPI.CloseHandle(_processHandle);
                _processHandle = IntPtr.Zero;
            }
            _process = null;
        }

        public List<TebInfo> GetTebAddresses()
        {
            var tebList = new List<TebInfo>();

            if (!IsConnected)
            {
                Console.WriteLine("프로세스에 연결되지 않음");
                return tebList;
            }

            IntPtr snapshot = MemoryAPI.CreateToolhelp32Snapshot(MemoryAPI.TH32CS_SNAPTHREAD, 0);
            if (snapshot == IntPtr.Zero)
            {
                Console.WriteLine("스레드 스냅샷 생성 실패");
                return tebList;
            }

            try
            {
                var threadEntry = new MemoryAPI.THREADENTRY32();
                threadEntry.dwSize = (uint)Marshal.SizeOf(typeof(MemoryAPI.THREADENTRY32));

                int index = 0;
                if (MemoryAPI.Thread32First(snapshot, ref threadEntry))
                {
                    do
                    {
                        if (threadEntry.th32OwnerProcessID == _process.Id)
                        {
                            IntPtr tebAddress = GetTebAddress(threadEntry.th32ThreadID);
                            if (tebAddress != IntPtr.Zero)
                            {
                                var tebInfo = new TebInfo(threadEntry.th32ThreadID, tebAddress, index);
                                tebList.Add(tebInfo);
                                index++;
                            }
                        }
                    }
                    while (MemoryAPI.Thread32Next(snapshot, ref threadEntry));
                }
            }
            finally
            {
                MemoryAPI.CloseHandle(snapshot);
            }

            Console.WriteLine($"총 {tebList.Count}개 TEB 주소 발견");
            return tebList;
        }

        public IntPtr GetStackStart(int threadIndex = 0)
        {
            if (!IsConnected)
            {
                Console.WriteLine("프로세스에 연결되지 않음");
                return IntPtr.Zero;
            }

            // 1단계: kernel32.dll 모듈 정보 가져오기
            if (!GetKernel32ModuleInfo(out var kernel32Info))
            {
                Console.WriteLine("kernel32.dll 모듈 정보를 가져올 수 없습니다.");
                return IntPtr.Zero;
            }

            // 2단계: 지정된 스레드의 StackTop 가져오기
            IntPtr stackTop = GetStackTop(threadIndex);
            if (stackTop == IntPtr.Zero)
            {
                Console.WriteLine("StackTop을 가져올 수 없습니다.");
                return IntPtr.Zero;
            }

            // 3단계: StackTop-4096 영역에서 kernel32 주소 찾기
            IntPtr stackSearchStart = new IntPtr(stackTop.ToInt64() - 4096);
            byte[] stackBuffer = new byte[4096];

            if (!ReadProcessMemory(stackSearchStart, stackBuffer, 4096))
            {
                Console.WriteLine("스택 메모리 읽기 실패");
                return IntPtr.Zero;
            }

            // 4단계: kernel32 범위에 있는 주소 찾기 (64비트)
            int pointerSize = 8;
            int numPointers = 4096 / pointerSize;

            for (int i = numPointers - 1; i >= 0; i--) // 역방향 검색
            {
                long pointerValue = BitConverter.ToInt64(stackBuffer, i * pointerSize);
                IntPtr pointer = new IntPtr(pointerValue);

                if (IsInRange(pointer, kernel32Info.lpBaseOfDll, kernel32Info.SizeOfImage))
                {
                    IntPtr resultAddress = new IntPtr(stackSearchStart.ToInt64() + (i * pointerSize));
                    Console.WriteLine($"StackStart 계산 완료: 0x{resultAddress.ToInt64():X16}");
                    return resultAddress;
                }
            }

            Console.WriteLine("kernel32를 가리키는 스택 엔트리를 찾을 수 없습니다.");
            return IntPtr.Zero;
        }

        public IntPtr GetStackTop(int threadIndex)
        {
            IntPtr snapshot = MemoryAPI.CreateToolhelp32Snapshot(MemoryAPI.TH32CS_SNAPTHREAD, 0);
            if (snapshot == IntPtr.Zero)
                return IntPtr.Zero;

            try
            {
                var threadEntry = new MemoryAPI.THREADENTRY32();
                threadEntry.dwSize = (uint)Marshal.SizeOf(typeof(MemoryAPI.THREADENTRY32));

                int currentIndex = 0;
                if (MemoryAPI.Thread32First(snapshot, ref threadEntry))
                {
                    do
                    {
                        if (threadEntry.th32OwnerProcessID == _process.Id)
                        {
                            if (currentIndex == threadIndex)
                            {
                                IntPtr threadHandle = MemoryAPI.OpenThread(MemoryAPI.THREAD_QUERY_INFORMATION, false, threadEntry.th32ThreadID);
                                if (threadHandle == IntPtr.Zero)
                                    return IntPtr.Zero;

                                try
                                {
                                    if (MemoryAPI.NtQueryInformationThread(
                                        threadHandle,
                                        MemoryAPI.ThreadBasicInformation,
                                        out var tbi,
                                        Marshal.SizeOf(typeof(MemoryAPI.THREAD_BASIC_INFORMATION)),
                                        IntPtr.Zero) == 0)
                                    {
                                        // TEB+8에서 StackTop 읽기
                                        IntPtr stackTopAddress = new IntPtr(tbi.TebBaseAddress.ToInt64() + 8);
                                        return ReadPointer(stackTopAddress);
                                    }
                                }
                                finally
                                {
                                    MemoryAPI.CloseHandle(threadHandle);
                                }
                                break;
                            }
                            currentIndex++;
                        }
                    }
                    while (MemoryAPI.Thread32Next(snapshot, ref threadEntry));
                }
            }
            finally
            {
                MemoryAPI.CloseHandle(snapshot);
            }

            return IntPtr.Zero;
        }

        public IntPtr ReadPointer(IntPtr address)
        {
            byte[] buffer = new byte[8];
            if (ReadProcessMemory(address, buffer, 8))
            {
                long value = BitConverter.ToInt64(buffer, 0);
                return new IntPtr(value);
            }
            return IntPtr.Zero;
        }

        public bool ReadProcessMemory(IntPtr address, byte[] buffer, int size)
        {
            if (!IsConnected || buffer == null || size <= 0)
                return false;

            return MemoryAPI.ReadProcessMemory(_processHandle, address, buffer, size, out _);
        }

        private IntPtr GetTebAddress(uint threadId)
        {
            IntPtr threadHandle = MemoryAPI.OpenThread(MemoryAPI.THREAD_QUERY_INFORMATION, false, threadId);
            if (threadHandle == IntPtr.Zero)
                return IntPtr.Zero;

            try
            {
                if (MemoryAPI.NtQueryInformationThread(
                    threadHandle,
                    MemoryAPI.ThreadBasicInformation,
                    out var basicInfo,
                    Marshal.SizeOf(typeof(MemoryAPI.THREAD_BASIC_INFORMATION)),
                    IntPtr.Zero) == 0)
                {
                    return basicInfo.TebBaseAddress;
                }
            }
            catch { }
            finally
            {
                MemoryAPI.CloseHandle(threadHandle);
            }

            return IntPtr.Zero;
        }

        private bool GetKernel32ModuleInfo(out MemoryAPI.MODULEINFO moduleInfo)
        {
            moduleInfo = new MemoryAPI.MODULEINFO();

            IntPtr[] modules = new IntPtr[1024];
            if (!MemoryAPI.EnumProcessModules(_processHandle, modules, (uint)(modules.Length * IntPtr.Size), out uint needed))
                return false;

            int moduleCount = (int)(needed / IntPtr.Size);
            StringBuilder moduleBaseName = new StringBuilder(256);

            for (int i = 0; i < moduleCount; i++)
            {
                if (modules[i] != IntPtr.Zero)
                {
                    if (MemoryAPI.GetModuleBaseName(_processHandle, modules[i], moduleBaseName, 256) > 0)
                    {
                        string moduleName = moduleBaseName.ToString();
                        if (moduleName.ToLower() == "kernel32.dll")
                        {
                            return MemoryAPI.GetModuleInformation(_processHandle, modules[i], out moduleInfo, (uint)Marshal.SizeOf(typeof(MemoryAPI.MODULEINFO)));
                        }
                    }
                }
            }

            return false;
        }

        private bool IsInRange(IntPtr address, IntPtr baseAddress, uint size)
        {
            long addr = address.ToInt64();
            long baseAddr = baseAddress.ToInt64();
            return addr >= baseAddr && addr < (baseAddr + size);
        }

        public void Dispose()
        {
            if (_isDisposed)
                return;

            Disconnect();
            _isDisposed = true;
        }
    }
}