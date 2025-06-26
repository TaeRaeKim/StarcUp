using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using StarcUp.Business.Memory;

namespace StarcUp.Infrastructure.Memory
{
    /// <summary>
    /// Windows API를 사용한 메모리 리더 구현
    /// </summary>
    public class MemoryReader : IMemoryReader
    {
        private Process _process;
        protected nint _processHandle;
        private bool _isDisposed;

        public bool IsConnected => _processHandle != 0 && _process != null && !_process.HasExited;
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

                if (_processHandle != 0)
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
            if (_processHandle != 0)
            {
                MemoryAPI.CloseHandle(_processHandle);
                _processHandle = 0;
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

            nint snapshot = MemoryAPI.CreateToolhelp32Snapshot(MemoryAPI.TH32CS_SNAPTHREAD, 0);
            if (snapshot == 0)
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
                            nint tebAddress = GetTebAddress(threadEntry.th32ThreadID);
                            if (tebAddress != 0)
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

        public nint GetStackStart(int threadIndex = 0)
        {
            if (!IsConnected)
            {
                Console.WriteLine("프로세스에 연결되지 않음");
                return 0;
            }

            // 1단계: kernel32.dll 모듈 정보 가져오기 (새로운 구현 사용)
            if (!GetKernel32ModuleInfo(out var kernel32Info))
            {
                Console.WriteLine("kernel32.dll 모듈 정보를 가져올 수 없습니다.");
                return 0;
            }

            // 2단계: 지정된 스레드의 StackTop 가져오기
            nint stackTop = GetStackTop(threadIndex);
            if (stackTop == 0)
            {
                Console.WriteLine("StackTop을 가져올 수 없습니다.");
                return 0;
            }

            // 3단계: StackTop-4096 영역에서 kernel32 주소 찾기
            nint stackSearchStart = stackTop - 4096;
            byte[] stackBuffer = new byte[4096];

            if (!ReadProcessMemory(stackSearchStart, stackBuffer, 4096))
            {
                Console.WriteLine("스택 메모리 읽기 실패");
                return 0;
            }

            // 4단계: kernel32 범위에 있는 주소 찾기 (64비트)
            int pointerSize = 8;
            int numPointers = 4096 / pointerSize;

            for (int i = numPointers - 1; i >= 0; i--) // 역방향 검색
            {
                nint pointer = (nint)BitConverter.ToInt64(stackBuffer, i * pointerSize);

                // MODULEENTRY32 구조체 사용으로 변경
                if (IsInRange(pointer, kernel32Info.modBaseAddr, kernel32Info.modBaseSize))
                {
                    nint resultAddress = stackSearchStart + (i * pointerSize);
                    Console.WriteLine($"StackStart 계산 완료: 0x{resultAddress:X16}");
                    return resultAddress;
                }
            }

            Console.WriteLine("kernel32를 가리키는 스택 엔트리를 찾을 수 없습니다.");
            return 0;
        }


        public nint GetStackTop(int threadIndex)
        {
            nint snapshot = MemoryAPI.CreateToolhelp32Snapshot(MemoryAPI.TH32CS_SNAPTHREAD, 0);
            if (snapshot == 0)
                return 0;

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
                                nint threadHandle = MemoryAPI.OpenThread(MemoryAPI.THREAD_QUERY_INFORMATION, false, threadEntry.th32ThreadID);
                                if (threadHandle == 0)
                                    return 0;

                                try
                                {
                                    if (MemoryAPI.NtQueryInformationThread(
                                        threadHandle,
                                        MemoryAPI.ThreadBasicInformation,
                                        out var tbi,
                                        Marshal.SizeOf(typeof(MemoryAPI.THREAD_BASIC_INFORMATION)),
                                        0) == 0)
                                    {
                                        // TEB+8에서 StackTop 읽기
                                        nint stackTopAddress = tbi.TebBaseAddress + 8;
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

            return 0;
        }

        public nint ReadPointer(nint address)
        {
            byte[] buffer = new byte[8];
            if (ReadProcessMemory(address, buffer, 8))
            {
                nint value = (nint)BitConverter.ToInt64(buffer, 0);
                return value;
            }
            return 0;
        }

        public bool ReadProcessMemory(nint address, byte[] buffer, int size)
        {
            if (!IsConnected || buffer == null || size <= 0)
                return false;

            return MemoryAPI.ReadProcessMemory(_processHandle, address, buffer, size, out _);
        }

        private nint GetTebAddress(uint threadId)
        {
            nint threadHandle = MemoryAPI.OpenThread(MemoryAPI.THREAD_QUERY_INFORMATION, false, threadId);
            if (threadHandle == 0)
                return 0;

            try
            {
                if (MemoryAPI.NtQueryInformationThread(
                    threadHandle,
                    MemoryAPI.ThreadBasicInformation,
                    out var basicInfo,
                    Marshal.SizeOf(typeof(MemoryAPI.THREAD_BASIC_INFORMATION)),
                    0) == 0)
                {
                    return basicInfo.TebBaseAddress;
                }
            }
            catch { }
            finally
            {
                MemoryAPI.CloseHandle(threadHandle);
            }

            return 0;
        }

        /// <summary>
        /// 메모리에서 지정된 크기만큼 데이터를 읽어 바이트 배열로 반환
        /// </summary>
        public byte[] ReadMemory(nint address, int size)
        {
            if (!IsConnected || size <= 0)
                return null;

            byte[] buffer = new byte[size];

            if (ReadProcessMemory(address, buffer, size))
            {
                return buffer;
            }

            return null;
        }

        /// <summary>
        /// 메모리에서 구조체를 직접 읽기
        /// </summary>
        public T ReadStructure<T>(nint address) where T : struct
        {
            int size = Marshal.SizeOf<T>();
            byte[] buffer = ReadMemory(address, size);

            if (buffer == null)
                throw new InvalidOperationException($"Failed to read memory at address 0x{address:X}");

            GCHandle handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
            try
            {
                return Marshal.PtrToStructure<T>(handle.AddrOfPinnedObject());
            }
            finally
            {
                handle.Free();
            }
        }

        /// <summary>
        /// 메모리에서 구조체 배열을 읽기
        /// </summary>
        public T[] ReadStructureArray<T>(nint address, int count) where T : struct
        {
            if (count <= 0)
                return new T[0];

            int structSize = Marshal.SizeOf<T>();
            int totalSize = structSize * count;

            byte[] buffer = ReadMemory(address, totalSize);
            if (buffer == null)
                throw new InvalidOperationException($"Failed to read memory array at address 0x{address:X}");

            T[] result = new T[count];

            for (int i = 0; i < count; i++)
            {
                byte[] structBytes = new byte[structSize];
                Array.Copy(buffer, i * structSize, structBytes, 0, structSize);

                GCHandle handle = GCHandle.Alloc(structBytes, GCHandleType.Pinned);
                try
                {
                    result[i] = Marshal.PtrToStructure<T>(handle.AddrOfPinnedObject());
                }
                finally
                {
                    handle.Free();
                }
            }

            return result;
        }

        private bool IsInRange(nint address, nint baseAddress, uint size)
        {
            long addr = address;
            long baseAddr = baseAddress;
            return addr >= baseAddr && addr < (baseAddr + size);
        }

        public void Dispose()
        {
            if (_isDisposed)
                return;

            Disconnect();
            _isDisposed = true;
        }

        /// <summary>
        /// 프로세스의 PEB(Process Environment Block) 주소를 가져옵니다.
        /// </summary>
        /// <returns>PEB 주소 (실패 시 0)</returns>
        public nint GetPebAddress()
        {
            if (!IsConnected)
            {
                Console.WriteLine("프로세스에 연결되지 않음");
                return 0;
            }

            try
            {
                var processInfo = new MemoryAPI.PROCESS_BASIC_INFORMATION();
                int returnLength = 0;

                int status = MemoryAPI.NtQueryInformationProcess(
                    _processHandle,
                    MemoryAPI.ProcessBasicInformation,
                    ref processInfo,
                    Marshal.SizeOf(typeof(MemoryAPI.PROCESS_BASIC_INFORMATION)),
                    ref returnLength
                );

                if (status == 0) // STATUS_SUCCESS
                {
                    Console.WriteLine($"PEB 주소: 0x{processInfo.PebBaseAddress:X16}");
                    return processInfo.PebBaseAddress;
                }
                else
                {
                    Console.WriteLine($"NtQueryInformationProcess 실패: NTSTATUS 0x{status:X}");
                    return 0;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"PEB 주소 가져오기 실패: {ex.Message}");
                return 0;
            }
        }
        /// <summary>
        /// 지정된 모듈의 정보를 가져옵니다
        /// </summary>
        public bool GetModuleInfo(string moduleName, out MemoryAPI.MODULEENTRY32 moduleInfo)
        {
            moduleInfo = new MemoryAPI.MODULEENTRY32();

            if (!IsConnected)
            {
                Console.WriteLine("프로세스에 연결되지 않음");
                return false;
            }

            nint snapshot = MemoryAPI.CreateToolhelp32Snapshot(
                MemoryAPI.TH32CS_SNAPMODULE | MemoryAPI.TH32CS_SNAPMODULE32,
                (uint)_process.Id);

            if (snapshot == 0)
            {
                Console.WriteLine("모듈 스냅샷 생성 실패");
                return false;
            }

            try
            {
                var modEntry = MemoryAPI.CreateModuleEntry32();

                if (MemoryAPI.Module32First(snapshot, ref modEntry))
                {
                    do
                    {
                        if (string.Equals(modEntry.szModule, moduleName, StringComparison.OrdinalIgnoreCase))
                        {
                            moduleInfo = modEntry;
                            Console.WriteLine($"{moduleName} 발견: 베이스주소=0x{modEntry.modBaseAddr.ToInt64():X16}, 크기={modEntry.modBaseSize}");
                            return true;
                        }
                    }
                    while (MemoryAPI.Module32Next(snapshot, ref modEntry));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"모듈 열거 중 오류: {ex.Message}");
            }
            finally
            {
                MemoryAPI.CloseHandle(snapshot);
            }

            Console.WriteLine($"{moduleName} 모듈을 찾을 수 없음");
            return false;
        }

        /// <summary>
        /// User32.dll 모듈 정보를 가져옵니다
        /// </summary>
        public bool GetUser32ModuleInfo(out MemoryAPI.MODULEENTRY32 moduleInfo)
        {
            return GetModuleInfo("user32.dll", out moduleInfo);
        }

        /// <summary>
        /// Kernel32.dll 모듈 정보를 가져옵니다 (기존 구현을 대체)
        /// </summary>
        public bool GetKernel32ModuleInfo(out MemoryAPI.MODULEENTRY32 moduleInfo)
        {
            return GetModuleInfo("kernel32.dll", out moduleInfo);
        }

        /// <summary>
        /// 모든 로드된 모듈 정보를 가져옵니다
        /// </summary>
        public Dictionary<string, MemoryAPI.MODULEENTRY32> GetAllModuleInfo()
        {
            var modules = new Dictionary<string, MemoryAPI.MODULEENTRY32>(StringComparer.OrdinalIgnoreCase);

            if (!IsConnected)
            {
                Console.WriteLine("프로세스에 연결되지 않음");
                return modules;
            }

            nint snapshot = MemoryAPI.CreateToolhelp32Snapshot(
                MemoryAPI.TH32CS_SNAPMODULE | MemoryAPI.TH32CS_SNAPMODULE32,
                (uint)_process.Id);

            if (snapshot == 0)
            {
                Console.WriteLine("모듈 스냅샷 생성 실패");
                return modules;
            }

            try
            {
                var modEntry = MemoryAPI.CreateModuleEntry32();

                if (MemoryAPI.Module32First(snapshot, ref modEntry))
                {
                    do
                    {
                        string moduleName = modEntry.szModule;
                        modules[moduleName] = modEntry;

                        Console.WriteLine($"모듈 발견: {moduleName} - 베이스주소=0x{modEntry.modBaseAddr.ToInt64():X16}, 크기={modEntry.modBaseSize}");
                    }
                    while (MemoryAPI.Module32Next(snapshot, ref modEntry));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"모듈 열거 중 오류: {ex.Message}");
            }
            finally
            {
                MemoryAPI.CloseHandle(snapshot);
            }

            Console.WriteLine($"총 {modules.Count}개 모듈 발견");
            return modules;
        }

        /// <summary>
        /// 특정 DLL의 베이스 주소만 빠르게 가져옵니다
        /// </summary>
        public nint GetModuleBaseAddress(string moduleName)
        {
            if (GetModuleInfo(moduleName, out var moduleInfo))
            {
                return moduleInfo.modBaseAddr;
            }
            return 0;
        }

        /// <summary>
        /// User32.dll의 베이스 주소를 가져옵니다
        /// </summary>
        public nint GetUser32BaseAddress()
        {
            return GetModuleBaseAddress("user32.dll");
        }
    }
}