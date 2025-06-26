using StarcUp.Common.Events;
using StarcUp.Infrastructure.Memory;
using System;
using System.Collections.Generic;
using System.Text;

namespace StarcUp.Business.MemoryService
{
    /// <summary>
    /// 메모리 작업의 미들웨어 역할을 하는 서비스
    /// - MemoryReader를 사용하여 복합적인 비즈니스 로직 제공
    /// - null/error 체크, 로깅, 수동 캐싱 등의 미들웨어 기능
    /// </summary>
    public class MemoryService : IMemoryService
    {
        public event EventHandler<ProcessEventArgs> ProcessConnect;
        public event EventHandler<ProcessEventArgs> ProcessDisconnect;

        private readonly IMemoryReader _memoryReader;
        private readonly object _lockObject = new object();
        private bool _isDisposed;

        private List<TebInfo> _cachedTebList;
        private ModuleInfo _cachedKernel32Module;
        private ModuleInfo _cachedUser32Module;

        public bool IsConnected => _memoryReader?.IsConnected ?? false;
        public int ConnectedProcessId => _memoryReader?.ConnectedProcessId ?? 0;

        public MemoryService(IMemoryReader memoryReader)
        {
            _memoryReader = memoryReader ?? throw new ArgumentNullException(nameof(memoryReader));
        }

        public bool ConnectToProcess(int processId)
        {
            lock (_lockObject)
            {
                try
                {
                    if (processId <= 0)
                    {
                        Console.WriteLine($"[MemoryService] 잘못된 프로세스 ID: {processId}");
                        return false;
                    }

                    if (IsConnected && ConnectedProcessId == processId)
                    {
                        Console.WriteLine($"[MemoryService] 이미 연결된 프로세스: {processId}");
                        return true;
                    }

                    Console.WriteLine($"[MemoryService] 프로세스 연결 시도: PID {processId}");
                    bool success = _memoryReader.ConnectToProcess(processId);

                    if (success)
                    {
                        ClearAllCache();
                        Console.WriteLine($"[MemoryService] 프로세스 연결 성공: PID {processId}");
                        ProcessConnect?.Invoke(this, new ProcessEventArgs(processId));
                    }
                    else
                    {
                        Console.WriteLine($"[MemoryService] 프로세스 연결 실패: PID {processId}");
                    }

                    return success;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[MemoryService] 프로세스 연결 중 오류: {ex.Message}");
                    return false;
                }
            }
        }

        public void Disconnect()
        {
            lock (_lockObject)
            {
                try
                {
                    int oldProcessId = ConnectedProcessId;
                    ProcessDisconnect?.Invoke(this, new ProcessEventArgs(oldProcessId, "ProcessDisConnect"));
                    _memoryReader.Disconnect();
                    ClearAllCache();
                    Console.WriteLine("[MemoryService] 프로세스 연결 해제됨");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[MemoryService] 프로세스 연결 해제 중 오류: {ex.Message}");
                }
            }
        }

        public int ReadInt(nint address)
        {
            if (!IsValidConnectionAndAddress(address, "ReadInt")) return 0;
            return _memoryReader.ReadInt(address);
        }

        public float ReadFloat(nint address)
        {
            if (!IsValidConnectionAndAddress(address, "ReadFloat")) return 0f;
            return _memoryReader.ReadFloat(address);
        }

        public double ReadDouble(nint address)
        {
            if (!IsValidConnectionAndAddress(address, "ReadDouble")) return 0.0;
            return _memoryReader.ReadDouble(address);
        }

        public byte ReadByte(nint address)
        {
            if (!IsValidConnectionAndAddress(address, "ReadByte")) return 0;
            return _memoryReader.ReadByte(address);
        }

        public short ReadShort(nint address)
        {
            if (!IsValidConnectionAndAddress(address, "ReadShort")) return 0;
            return _memoryReader.ReadShort(address);
        }

        public long ReadLong(nint address)
        {
            if (!IsValidConnectionAndAddress(address, "ReadLong")) return 0L;
            return _memoryReader.ReadLong(address);
        }

        public bool ReadBool(nint address)
        {
            if (!IsValidConnectionAndAddress(address, "ReadBool")) return false;
            return _memoryReader.ReadBool(address);
        }

        public nint ReadPointer(nint address)
        {
            if (!IsValidConnectionAndAddress(address, "ReadPointer")) return 0;
            return _memoryReader.ReadPointer(address);
        }

        public string ReadString(nint address, int maxLength = 256, Encoding encoding = null)
        {
            if (!IsValidConnectionAndAddress(address, "ReadString")) return string.Empty;
            if (maxLength <= 0) return string.Empty;
            return _memoryReader.ReadString(address, maxLength, encoding);
        }

        public T ReadStructure<T>(nint address) where T : struct
        {
            if (!IsValidConnectionAndAddress(address, $"ReadStructure<{typeof(T).Name}>")) return default(T);
            try
            {
                return _memoryReader.ReadStructure<T>(address);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MemoryService] ReadStructure<{typeof(T).Name}> 실패: {ex.Message}");
                return default(T);
            }
        }

        public T[] ReadStructureArray<T>(nint address, int count) where T : struct
        {
            if (!IsValidConnectionAndAddress(address, $"ReadStructureArray<{typeof(T).Name}>")) return new T[0];
            if (count <= 0) return new T[0];
            try
            {
                return _memoryReader.ReadStructureArray<T>(address, count);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MemoryService] ReadStructureArray<{typeof(T).Name}> 실패: {ex.Message}");
                return new T[0];
            }
        }

        public nint GetPebAddress()
        {
            if (!IsConnected)
            {
                Console.WriteLine("[MemoryService] GetPebAddress: 프로세스에 연결되지 않음");
                return 0;
            }

            try
            {
                int status = _memoryReader.QueryProcessInformation(out var processInfo);
                if (status == 0)
                {
                    Console.WriteLine($"[MemoryService] PEB 주소: 0x{processInfo.PebBaseAddress:X}");
                    return processInfo.PebBaseAddress;
                }
                else
                {
                    Console.WriteLine($"[MemoryService] PEB 주소 가져오기 실패: NTSTATUS 0x{status:X}");
                    return 0;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MemoryService] GetPebAddress 오류: {ex.Message}");
                return 0;
            }
        }

        public List<TebInfo> GetTebAddresses()
        {
            if (!IsConnected)
            {
                Console.WriteLine("[MemoryService] GetTebAddresses: 프로세스에 연결되지 않음");
                return new List<TebInfo>();
            }

            if (_cachedTebList != null)
            {
                Console.WriteLine($"[MemoryService] TEB 캐시 사용 ({_cachedTebList.Count}개)");
                return _cachedTebList;
            }

            try
            {
                var tebList = new List<TebInfo>();
                nint snapshot = _memoryReader.CreateThreadSnapshot();
                if (snapshot == 0)
                {
                    Console.WriteLine("[MemoryService] 스레드 스냅샷 생성 실패");
                    return tebList;
                }

                try
                {
                    int index = 0;
                    if (_memoryReader.GetFirstThread(snapshot, out var threadEntry))
                    {
                        do
                        {
                            if (threadEntry.th32OwnerProcessID == ConnectedProcessId)
                            {
                                nint tebAddress = _memoryReader.GetThread(threadEntry.th32ThreadID);
                                if (tebAddress != 0)
                                {
                                    var tebInfo = new TebInfo(threadEntry.th32ThreadID, tebAddress, index);
                                    tebList.Add(tebInfo);
                                    index++;
                                }
                            }
                        }
                        while (_memoryReader.GetNextThread(snapshot, ref threadEntry));
                    }
                }
                finally
                {
                    _memoryReader.CloseHandle(snapshot);
                }

                _cachedTebList = tebList;
                Console.WriteLine($"[MemoryService] TEB 캐시 생성: {tebList.Count}개");
                return tebList;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MemoryService] GetTebAddresses 오류: {ex.Message}");
                return new List<TebInfo>();
            }
        }

        public nint GetStackStart(int threadIndex = 0)
        {
            var tebList = GetTebAddresses();
            if (threadIndex < 0 || threadIndex >= tebList.Count)
            {
                Console.WriteLine($"[MemoryService] GetStackStart: 잘못된 스레드 인덱스 {threadIndex} (최대: {tebList.Count - 1})");
                return 0;
            }

            try
            {
                nint tebAddress = tebList[threadIndex].TebAddress;
                nint stackTopPtr = tebAddress + 0x08;
                nint stackStart = ReadPointer(stackTopPtr);
                Console.WriteLine($"[MemoryService] 스레드 {threadIndex} 스택 시작: 0x{stackStart:X}");
                return stackStart;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MemoryService] GetStackStart 오류: {ex.Message}");
                return 0;
            }
        }

        public nint GetStackTop(int threadIndex = 0)
        {
            var tebList = GetTebAddresses();
            if (threadIndex < 0 || threadIndex >= tebList.Count)
            {
                Console.WriteLine($"[MemoryService] GetStackTop: 잘못된 스레드 인덱스 {threadIndex} (최대: {tebList.Count - 1})");
                return 0;
            }

            try
            {
                nint tebAddress = tebList[threadIndex].TebAddress;
                nint stackBottomPtr = tebAddress + 0x10;
                nint stackTop = ReadPointer(stackBottomPtr);
                Console.WriteLine($"[MemoryService] 스레드 {threadIndex} 스택 끝: 0x{stackTop:X}");
                return stackTop;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MemoryService] GetStackTop 오류: {ex.Message}");
                return 0;
            }
        }

        public bool FindModule(string moduleName, out ModuleInfo moduleInfo)
        {
            moduleInfo = null;
            if (!IsConnected)
            {
                Console.WriteLine("[MemoryService] FindModule: 프로세스에 연결되지 않음");
                return false;
            }

            if (string.IsNullOrWhiteSpace(moduleName))
            {
                Console.WriteLine("[MemoryService] FindModule: 모듈명이 비어있음");
                return false;
            }

            try
            {
                nint snapshot = _memoryReader.CreateModuleSnapshot();
                if (snapshot == 0)
                {
                    Console.WriteLine("[MemoryService] 모듈 스냅샷 생성 실패");
                    return false;
                }

                try
                {
                    if (_memoryReader.GetFirstModule(snapshot, out var moduleEntry))
                    {
                        do
                        {
                            if (string.Equals(moduleEntry.szModule, moduleName, StringComparison.OrdinalIgnoreCase))
                            {
                                moduleInfo = new ModuleInfo(
                                    moduleEntry.szModule,
                                    moduleEntry.modBaseAddr,
                                    moduleEntry.modBaseSize,
                                    moduleEntry.szExePath
                                );
                                Console.WriteLine($"[MemoryService] 모듈 발견: {moduleInfo}");
                                return true;
                            }
                        }
                        while (_memoryReader.GetNextModule(snapshot, ref moduleEntry));
                    }
                }
                finally
                {
                    _memoryReader.CloseHandle(snapshot);
                }

                Console.WriteLine($"[MemoryService] 모듈 '{moduleName}'을 찾을 수 없음");
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MemoryService] FindModule 오류: {ex.Message}");
                return false;
            }
        }

        public ModuleInfo GetKernel32Module()
        {
            if (_cachedKernel32Module != null)
            {
                Console.WriteLine("[MemoryService] kernel32 캐시 사용");
                return _cachedKernel32Module;
            }

            if (FindModule("kernel32.dll", out var moduleInfo))
            {
                _cachedKernel32Module = moduleInfo;
                Console.WriteLine("[MemoryService] kernel32 캐시 생성");
                return moduleInfo;
            }
            return null;
        }

        public ModuleInfo GetUser32Module()
        {
            if (_cachedUser32Module != null)
            {
                Console.WriteLine("[MemoryService] user32 캐시 사용");
                return _cachedUser32Module;
            }

            if (FindModule("user32.dll", out var moduleInfo))
            {
                _cachedUser32Module = moduleInfo;
                Console.WriteLine("[MemoryService] user32 캐시 생성");
                return moduleInfo;
            }
            return null;
        }

        public bool IsValidAddress(nint address)
        {
            return address != 0;
        }

        public bool IsInModuleRange(nint address, string moduleName)
        {
            if (!IsValidAddress(address)) return false;
            if (FindModule(moduleName, out var moduleInfo))
            {
                return moduleInfo.IsInRange(address);
            }
            return false;
        }

        public void RefreshTebCache()
        {
            lock (_lockObject)
            {
                _cachedTebList = null;
                Console.WriteLine("[MemoryService] TEB 캐시 수동 무효화");
            }
        }

        public void RefreshModuleCache()
        {
            lock (_lockObject)
            {
                _cachedKernel32Module = null;
                _cachedUser32Module = null;
                Console.WriteLine("[MemoryService] 모듈 캐시 수동 무효화");
            }
        }

        public void RefreshAllCache()
        {
            lock (_lockObject)
            {
                ClearAllCache();
                Console.WriteLine("[MemoryService] 모든 캐시 수동 무효화");
            }
        }

        private bool IsValidConnectionAndAddress(nint address, string operation)
        {
            if (!IsConnected)
            {
                Console.WriteLine($"[MemoryService] {operation}: 프로세스에 연결되지 않음");
                return false;
            }
            if (!IsValidAddress(address))
            {
                Console.WriteLine($"[MemoryService] {operation}: 잘못된 주소 0x{address:X}");
                return false;
            }
            return true;
        }

        private void ClearAllCache()
        {
            _cachedTebList = null;
            _cachedKernel32Module = null;
            _cachedUser32Module = null;
        }

        public void Dispose()
        {
            if (_isDisposed) return;
            Disconnect();
            _isDisposed = true;
        }
    }
}