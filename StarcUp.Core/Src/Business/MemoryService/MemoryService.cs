using StarcUp.Common.Events;
using StarcUp.Infrastructure.Memory;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace StarcUp.Business.MemoryService
{
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
        private ModuleInfo _cachedStarCraftModule;

        public bool IsConnected => _memoryReader?.IsConnected ?? false;
        public int ConnectedProcessId => _memoryReader?.ConnectedProcessId ?? 0;

        public MemoryService(IMemoryReader memoryReader)
        {
            _memoryReader = memoryReader ?? throw new ArgumentNullException(nameof(memoryReader));
        }

        public bool ConnectToProcess(int processId)
        {
            if (_isDisposed)
            {
                Console.WriteLine("[MemoryService] ConnectToProcess: 서비스가 이미 해제됨");
                return false;
            }

            if (processId <= 0)
            {
                Console.WriteLine("[MemoryService] ConnectToProcess: 잘못된 프로세스 ID");
                return false;
            }

            try
            {
                bool wasConnected = IsConnected;
                int previousProcessId = ConnectedProcessId;

                if (wasConnected)
                {
                    Console.WriteLine("[MemoryService] 기존 연결 해제 중...");
                    Disconnect();
                }

                Console.WriteLine($"[MemoryService] 프로세스 {processId}에 연결 시도...");

                if (_memoryReader.ConnectToProcess(processId))
                {
                    RefreshAllCache();
                    Console.WriteLine($"[MemoryService] 프로세스 {processId} 연결 성공");
                    ProcessConnect?.Invoke(this, new ProcessEventArgs(processId));
                    return true;
                }
                else
                {
                    Console.WriteLine($"[MemoryService] 프로세스 {processId} 연결 실패");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MemoryService] ConnectToProcess 예외: {ex.Message}");
                return false;
            }
        }

        public void Disconnect()
        {
            if (!IsConnected) return;

            try
            {
                int processId = ConnectedProcessId;
                _memoryReader.Disconnect();
                RefreshAllCache();
                Console.WriteLine($"[MemoryService] 프로세스 {processId} 연결 해제됨");
                ProcessDisconnect?.Invoke(this, new ProcessEventArgs(processId));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MemoryService] Disconnect 예외: {ex.Message}");
            }
        }

        public int ReadInt(nint address)
        {
            if (!IsConnected)
            {
                Console.WriteLine("[MemoryService] ReadInt: 프로세스에 연결되지 않음");
                return 0;
            }

            if (!IsValidAddress(address))
            {
                Console.WriteLine($"[MemoryService] ReadInt: 잘못된 주소 0x{address:X}");
                return 0;
            }

            try
            {
                return _memoryReader.ReadInt(address);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MemoryService] ReadInt 오류: {ex.Message}");
                return 0;
            }
        }

        public float ReadFloat(nint address)
        {
            if (!IsConnected)
            {
                Console.WriteLine("[MemoryService] ReadFloat: 프로세스에 연결되지 않음");
                return 0f;
            }

            if (!IsValidAddress(address))
            {
                Console.WriteLine($"[MemoryService] ReadFloat: 잘못된 주소 0x{address:X}");
                return 0f;
            }

            try
            {
                return _memoryReader.ReadFloat(address);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MemoryService] ReadFloat 오류: {ex.Message}");
                return 0f;
            }
        }

        public double ReadDouble(nint address)
        {
            if (!IsConnected)
            {
                Console.WriteLine("[MemoryService] ReadDouble: 프로세스에 연결되지 않음");
                return 0.0;
            }

            if (!IsValidAddress(address))
            {
                Console.WriteLine($"[MemoryService] ReadDouble: 잘못된 주소 0x{address:X}");
                return 0.0;
            }

            try
            {
                return _memoryReader.ReadDouble(address);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MemoryService] ReadDouble 오류: {ex.Message}");
                return 0.0;
            }
        }

        public byte ReadByte(nint address)
        {
            if (!IsConnected)
            {
                Console.WriteLine("[MemoryService] ReadByte: 프로세스에 연결되지 않음");
                return 0;
            }

            if (!IsValidAddress(address))
            {
                Console.WriteLine($"[MemoryService] ReadByte: 잘못된 주소 0x{address:X}");
                return 0;
            }

            try
            {
                return _memoryReader.ReadByte(address);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MemoryService] ReadByte 오류: {ex.Message}");
                return 0;
            }
        }

        public short ReadShort(nint address)
        {
            if (!IsConnected)
            {
                Console.WriteLine("[MemoryService] ReadShort: 프로세스에 연결되지 않음");
                return 0;
            }

            if (!IsValidAddress(address))
            {
                Console.WriteLine($"[MemoryService] ReadShort: 잘못된 주소 0x{address:X}");
                return 0;
            }

            try
            {
                return _memoryReader.ReadShort(address);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MemoryService] ReadShort 오류: {ex.Message}");
                return 0;
            }
        }

        public long ReadLong(nint address)
        {
            if (!IsConnected)
            {
                Console.WriteLine("[MemoryService] ReadLong: 프로세스에 연결되지 않음");
                return 0L;
            }

            if (!IsValidAddress(address))
            {
                Console.WriteLine($"[MemoryService] ReadLong: 잘못된 주소 0x{address:X}");
                return 0L;
            }

            try
            {
                return _memoryReader.ReadLong(address);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MemoryService] ReadLong 오류: {ex.Message}");
                return 0L;
            }
        }

        public bool ReadBool(nint address)
        {
            if (!IsConnected)
            {
                Console.WriteLine("[MemoryService] ReadBool: 프로세스에 연결되지 않음");
                return false;
            }

            if (!IsValidAddress(address))
            {
                Console.WriteLine($"[MemoryService] ReadBool: 잘못된 주소 0x{address:X}");
                return false;
            }

            try
            {
                return _memoryReader.ReadBool(address);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MemoryService] ReadBool 오류: {ex.Message}");
                return false;
            }
        }

        public nint ReadPointer(nint address)
        {
            if (!IsConnected)
            {
                Console.WriteLine("[MemoryService] ReadPointer: 프로세스에 연결되지 않음");
                return 0;
            }

            if (!IsValidAddress(address))
            {
                Console.WriteLine($"[MemoryService] ReadPointer: 잘못된 주소 0x{address:X}");
                return 0;
            }

            try
            {
                return _memoryReader.ReadPointer(address);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MemoryService] ReadPointer 오류: {ex.Message}");
                return 0;
            }
        }

        public string ReadString(nint address, int maxLength = 256, Encoding encoding = null)
        {
            if (!IsConnected)
            {
                Console.WriteLine("[MemoryService] ReadString: 프로세스에 연결되지 않음");
                return string.Empty;
            }

            if (!IsValidAddress(address))
            {
                Console.WriteLine($"[MemoryService] ReadString: 잘못된 주소 0x{address:X}");
                return string.Empty;
            }

            if (maxLength <= 0)
            {
                Console.WriteLine("[MemoryService] ReadString: 잘못된 최대 길이");
                return string.Empty;
            }

            try
            {
                return _memoryReader.ReadString(address, maxLength, encoding);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MemoryService] ReadString 오류: {ex.Message}");
                return string.Empty;
            }
        }

        public T ReadStructure<T>(nint address) where T : struct
        {
            if (!IsConnected)
            {
                Console.WriteLine("[MemoryService] ReadStructure: 프로세스에 연결되지 않음");
                return default(T);
            }

            if (!IsValidAddress(address))
            {
                Console.WriteLine($"[MemoryService] ReadStructure: 잘못된 주소 0x{address:X}");
                return default(T);
            }

            try
            {
                return _memoryReader.ReadStructure<T>(address);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MemoryService] ReadStructure 오류: {ex.Message}");
                return default(T);
            }
        }

        public T[] ReadStructureArray<T>(nint address, int count) where T : struct
        {
            if (!IsConnected)
            {
                Console.WriteLine("[MemoryService] ReadStructureArray: 프로세스에 연결되지 않음");
                return new T[0];
            }

            if (!IsValidAddress(address))
            {
                Console.WriteLine($"[MemoryService] ReadStructureArray: 잘못된 주소 0x{address:X}");
                return new T[0];
            }

            if (count <= 0)
            {
                Console.WriteLine("[MemoryService] ReadStructureArray: 잘못된 카운트");
                return new T[0];
            }

            try
            {
                return _memoryReader.ReadStructureArray<T>(address, count);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MemoryService] ReadStructureArray 오류: {ex.Message}");
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
                    Console.WriteLine($"[MemoryService] GetPebAddress: NtQueryInformationProcess 실패, 상태: {status}");
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
            lock (_lockObject)
            {
                if (_cachedTebList != null)
                {
                    return new List<TebInfo>(_cachedTebList);
                }

                if (!IsConnected)
                {
                    Console.WriteLine("[MemoryService] GetTebAddresses: 프로세스에 연결되지 않음");
                    return new List<TebInfo>();
                }

                var tebList = new List<TebInfo>();

                try
                {
                    nint snapshot = _memoryReader.CreateThreadSnapshot();
                    if (snapshot == 0)
                    {
                        Console.WriteLine("[MemoryService] 스레드 스냅샷 생성 실패");
                        return tebList;
                    }

                    try
                    {
                        if (_memoryReader.GetFirstThread(snapshot, out var threadEntry))
                        {
                            int threadIndex = 0;
                            do
                            {
                                if (threadEntry.th32OwnerProcessID == ConnectedProcessId)
                                {
                                    nint tebAddress = _memoryReader.GetThread(threadEntry.th32ThreadID);
                                    if (tebAddress != 0)
                                    {
                                        tebList.Add(new TebInfo(threadEntry.th32ThreadID, tebAddress, threadIndex));
                                        Console.WriteLine($"[MemoryService] 스레드 {threadEntry.th32ThreadID}: TEB=0x{tebAddress:X}");
                                        threadIndex++;
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

                    _cachedTebList = new List<TebInfo>(tebList);
                    Console.WriteLine($"[MemoryService] TEB 캐시 생성: {tebList.Count}개");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[MemoryService] GetTebAddresses 오류: {ex.Message}");
                }

                return tebList;
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
                nint stackTopPtr = tebAddress + 0x08;
                nint stackTop = ReadPointer(stackTopPtr);
                return stackTop;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MemoryService] GetStackTop 오류: {ex.Message}");
                return 0;
            }
        }

        public nint GetStackBottom(int threadIndex = 0)
        {
            var tebList = GetTebAddresses();
            if (threadIndex < 0 || threadIndex >= tebList.Count)
            {
                Console.WriteLine($"[MemoryService] GetStackBottom: 잘못된 스레드 인덱스 {threadIndex} (최대: {tebList.Count - 1})");
                return 0;
            }

            try
            {
                nint tebAddress = tebList[threadIndex].TebAddress;
                nint stackBottomPtr = tebAddress + 0x10;
                nint stackBottom = ReadPointer(stackBottomPtr);
                Console.WriteLine($"[MemoryService] 스레드 {threadIndex} 스택 하단: 0x{stackBottom:X}");
                return stackBottom;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MemoryService] GetStackBottom 오류: {ex.Message}");
                return 0;
            }
        }

        public nint GetThreadStackAddress(int threadIndex = 0)
        {
            if (!IsConnected)
            {
                Console.WriteLine("[MemoryService] GetThreadStackAddress: 프로세스에 연결되지 않음");
                return 0;
            }

            try
            {
                var kernel32Module = GetKernel32Module();
                if (kernel32Module == null)
                {
                    Console.WriteLine("[MemoryService] GetThreadStackAddress: kernel32.dll 모듈 정보를 가져올 수 없음");
                    return 0;
                }

                nint stackTop = GetStackTop(threadIndex);
                if (stackTop == 0)
                {
                    Console.WriteLine("[MemoryService] GetThreadStackAddress: StackTop을 가져올 수 없음");
                    return 0;
                }

                nint stackSearchStart = stackTop - 4096;
                byte[] stackBuffer = new byte[4096];

                if (!ReadMemoryIntoBuffer(stackSearchStart, stackBuffer, 4096))
                {
                    Console.WriteLine("[MemoryService] GetThreadStackAddress: 스택 메모리 읽기 실패");
                    return 0;
                }

                int pointerSize = 8;
                int numPointers = 4096 / pointerSize;

                for (int i = numPointers - 1; i >= 0; i--)
                {
                    nint pointer = (nint)BitConverter.ToInt64(stackBuffer, i * pointerSize);

                    if (IsInKernel32Range(pointer, kernel32Module))
                    {
                        nint resultAddress = stackSearchStart + (i * pointerSize);
                                return resultAddress;
                    }
                }

                Console.WriteLine("[MemoryService] GetThreadStackAddress: kernel32를 가리키는 스택 엔트리를 찾을 수 없음");
                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MemoryService] GetThreadStackAddress 오류: {ex.Message}");
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

            var result = FindModule(moduleName);
            if (result != null)
            {
                moduleInfo = result;
                return true;
            }

            return false;
        }

        public ModuleInfo GetKernel32Module()
        {
            if (_cachedKernel32Module != null)
            {
                return _cachedKernel32Module;
            }

            string[] possibleNames = { "kernel32.dll", "KERNEL32.DLL", "kernel32", "KERNEL32" };
            
            foreach (string name in possibleNames)
            {
                Console.WriteLine($"[MemoryService] {name} 검색 시도...");
                if (FindModule(name, out var moduleInfo))
                {
                    _cachedKernel32Module = moduleInfo;
                    Console.WriteLine($"[MemoryService] kernel32 모듈 발견: {name}");
                    return moduleInfo;
                }
            }
            
            Console.WriteLine("[MemoryService] kernel32 모듈을 찾지 못했습니다.");
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
        
        public ModuleInfo GetStarCraftModule()
        {
            if (_cachedStarCraftModule != null)
            {
                return _cachedStarCraftModule;
            }

            string[] possibleNames = {"StarCraft.exe", "StarCraft"};
            
            foreach (string name in possibleNames)
            {
                Console.WriteLine($"[MemoryService] {name} 검색 시도...");
                if (FindModule(name, out var moduleInfo))
                {
                    _cachedStarCraftModule = moduleInfo;
                    Console.WriteLine($"[MemoryService] kernel32 모듈 발견: {name}");
                    return moduleInfo;
                }
            }
            
            Console.WriteLine("[MemoryService] starcraft 모듈을 찾지 못했습니다.");
            return null;
        }

        public bool IsValidAddress(nint address)
        {
            if (Environment.Is64BitProcess)
            {
                return address != 0 && (long)address > 0x10000 && (long)address < 0x7FFFFFFFFFFF;
            }
            else
            {
                return address != 0 && (int)address > 0x10000 && (int)address < 0x7FFFFFFF;
            }
        }

        public bool IsInModuleRange(nint address, string moduleName)
        {
            if (!IsValidAddress(address) || string.IsNullOrWhiteSpace(moduleName))
                return false;

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
                Console.WriteLine("[MemoryService] TEB 캐시 무효화됨");
            }
        }

        public void RefreshModuleCache()
        {
            lock (_lockObject)
            {
                _cachedKernel32Module = null;
                _cachedUser32Module = null;
                Console.WriteLine("[MemoryService] 모듈 캐시 무효화됨");
            }
        }

        public void RefreshAllCache()
        {
            lock (_lockObject)
            {
                RefreshTebCache();
                RefreshModuleCache();
                Console.WriteLine("[MemoryService] 모든 캐시 무효화됨");
            }
        }

        public bool ReadMemoryIntoBuffer(nint address, byte[] buffer, int size)
        {
            if (!IsConnected)
            {
                Console.WriteLine("[MemoryService] ReadMemoryIntoBuffer: 프로세스에 연결되지 않음");
                return false;
            }

            if (!IsValidAddress(address))
            {
                Console.WriteLine($"[MemoryService] ReadMemoryIntoBuffer: 잘못된 주소 0x{address:X}");
                return false;
            }

            if (buffer == null || size <= 0 || size > buffer.Length)
            {
                Console.WriteLine("[MemoryService] ReadMemoryIntoBuffer: 잘못된 버퍼 또는 크기");
                return false;
            }

            try
            {
                return _memoryReader.ReadMemoryIntoBuffer(address, buffer, size);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MemoryService] ReadMemoryIntoBuffer 오류: {ex.Message}");
                return false;
            }
        }

        public bool ReadStructureArrayIntoBuffer<T>(nint address, T[] buffer, int count) where T : unmanaged
        {
            if (!IsConnected)
            {
                Console.WriteLine("[MemoryService] ReadStructureArrayIntoBuffer: 프로세스에 연결되지 않음");
                return false;
            }

            if (!IsValidAddress(address))
            {
                Console.WriteLine($"[MemoryService] ReadStructureArrayIntoBuffer: 잘못된 주소 0x{address:X}");
                return false;
            }

            if (buffer == null || count <= 0 || count > buffer.Length)
            {
                Console.WriteLine("[MemoryService] ReadStructureArrayIntoBuffer: 잘못된 버퍼 또는 카운트");
                return false;
            }

            try
            {
                return _memoryReader.ReadStructureArrayIntoBuffer<T>(address, buffer, count);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MemoryService] ReadStructureArrayIntoBuffer 오류: {ex.Message}");
                return false;
            }
        }

        public void DebugAllModules()
        {
            if (!IsConnected)
            {
                Console.WriteLine("[MemoryService] DebugAllModules: 프로세스에 연결되지 않음");
                return;
            }

            Console.WriteLine($"[MemoryService] === PID {ConnectedProcessId} 모든 모듈 목록 (치트엔진 방식) ===");

            try
            {
                nint processHandle = _memoryReader.GetProcessHandle();
                if (processHandle == IntPtr.Zero)
                {
                    Console.WriteLine("[MemoryService] 프로세스 핸들을 가져올 수 없음");
                    return;
                }

                IntPtr[] moduleHandles = new IntPtr[1024];
                uint bytesNeeded = 0;

                if (!Infrastructure.Memory.MemoryAPI.EnumProcessModules(
                    processHandle,
                    moduleHandles,
                    (uint)(moduleHandles.Length * IntPtr.Size),
                    out bytesNeeded))
                {
                    int error = Marshal.GetLastWin32Error();
                    Console.WriteLine($"[MemoryService] EnumProcessModules 실패, 오류 코드: {error}");
                    return;
                }

                int moduleCount = (int)(bytesNeeded / IntPtr.Size);
                Console.WriteLine($"[MemoryService] 발견된 모듈 수: {moduleCount}");

                for (int i = 0; i < moduleCount && i < moduleHandles.Length; i++)
                {
                    if (moduleHandles[i] == IntPtr.Zero) continue;

                    try
                    {
                        var moduleNameBuilder = new StringBuilder(260);
                        uint nameLength = Infrastructure.Memory.MemoryAPI.GetModuleFileNameEx(
                            processHandle,
                            moduleHandles[i],
                            moduleNameBuilder,
                            (uint)moduleNameBuilder.Capacity);

                        if (nameLength > 0)
                        {
                            string fullPath = moduleNameBuilder.ToString();
                            string moduleName = Path.GetFileName(fullPath);

                            if (Infrastructure.Memory.MemoryAPI.GetModuleInformation(
                                processHandle,
                                moduleHandles[i],
                                out Infrastructure.Memory.MemoryAPI.MODULEINFO moduleInfo,
                                (uint)Marshal.SizeOf<Infrastructure.Memory.MemoryAPI.MODULEINFO>()))
                            {
                                Console.WriteLine($"[{i + 1:D2}] 모듈명: {moduleName}");
                                Console.WriteLine($"     베이스주소: 0x{moduleInfo.lpBaseOfDll:X}");
                                Console.WriteLine($"     크기: 0x{moduleInfo.SizeOfImage:X} ({moduleInfo.SizeOfImage:N0} bytes)");
                                Console.WriteLine($"     전체경로: {fullPath}");
                                Console.WriteLine();

                                if (moduleName.ToLower().Contains("starcraft") ||
                                    moduleName.ToLower().Contains("star") ||
                                    fullPath.ToLower().Contains("starcraft"))
                                {
                                    Console.WriteLine($"★ StarCraft 관련 모듈 발견: {moduleName}");
                                    Console.WriteLine();
                                }
                            }
                        }
                        else
                        {
                            Console.WriteLine($"[{i + 1:D2}] 모듈명을 가져올 수 없음 (핸들: 0x{moduleHandles[i]:X})");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[MemoryService] 모듈 {i} 정보 읽기 실패: {ex.Message}");
                    }
                }

                Console.WriteLine($"[MemoryService] === 총 {moduleCount}개 모듈 처리 완료 ===");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MemoryService] DebugAllModules 오류: {ex.Message}");
                Console.WriteLine($"[MemoryService] 스택 트레이스: {ex.StackTrace}");
            }
        }

        public void DebugAllModulesCheatEngineStyle()
        {
            if (!IsConnected)
            {
                Console.WriteLine("[MemoryService] 프로세스에 연결되지 않음");
                return;
            }

            Console.WriteLine($"[MemoryService] === PID {ConnectedProcessId} 모든 모듈 목록 (치트엔진 스타일) ===");

            try
            {
                nint processHandle = _memoryReader.GetProcessHandle();
                if (processHandle == IntPtr.Zero)
                {
                    Console.WriteLine("[MemoryService] 프로세스 핸들을 가져올 수 없음");
                    return;
                }

                IntPtr[] moduleHandles = new IntPtr[1024];
                uint bytesNeeded = 0;

                if (!Infrastructure.Memory.MemoryAPI.EnumProcessModules(
                    processHandle,
                    moduleHandles,
                    (uint)(moduleHandles.Length * IntPtr.Size),
                    out bytesNeeded))
                {
                    int error = Marshal.GetLastWin32Error();
                    Console.WriteLine($"[MemoryService] EnumProcessModules 실패, 오류 코드: {error}");
                    return;
                }

                int moduleCount = (int)(bytesNeeded / IntPtr.Size);
                Console.WriteLine($"[MemoryService] 발견된 모듈 수: {moduleCount}");

                for (int i = 0; i < moduleCount && i < moduleHandles.Length; i++)
                {
                    if (moduleHandles[i] == IntPtr.Zero) continue;

                    try
                    {
                        var moduleNameBuilder = new StringBuilder(260);
                        uint nameLength = Infrastructure.Memory.MemoryAPI.GetModuleFileNameEx(
                            processHandle,
                            moduleHandles[i],
                            moduleNameBuilder,
                            (uint)moduleNameBuilder.Capacity);

                        if (nameLength > 0)
                        {
                            string fullPath = moduleNameBuilder.ToString();
                            string moduleName = Path.GetFileName(fullPath);

                            if (Infrastructure.Memory.MemoryAPI.GetModuleInformation(
                                processHandle,
                                moduleHandles[i],
                                out Infrastructure.Memory.MemoryAPI.MODULEINFO moduleInfo,
                                (uint)Marshal.SizeOf<Infrastructure.Memory.MemoryAPI.MODULEINFO>()))
                            {
                                Console.WriteLine($"[{i + 1:D2}] 모듈명: {moduleName}");
                                Console.WriteLine($"     베이스주소: 0x{moduleInfo.lpBaseOfDll:X}");
                                Console.WriteLine($"     크기: 0x{moduleInfo.SizeOfImage:X} ({moduleInfo.SizeOfImage:N0} bytes)");
                                Console.WriteLine($"     전체경로: {fullPath}");
                                Console.WriteLine();

                                if (moduleName.ToLower().Contains("starcraft") ||
                                    moduleName.ToLower().Contains("star") ||
                                    fullPath.ToLower().Contains("starcraft"))
                                {
                                    Console.WriteLine($"★ StarCraft 관련 모듈 발견: {moduleName}");
                                    Console.WriteLine();
                                }
                            }
                        }
                        else
                        {
                            Console.WriteLine($"[{i + 1:D2}] 모듈명을 가져올 수 없음 (핸들: 0x{moduleHandles[i]:X})");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[MemoryService] 모듈 {i} 정보 읽기 실패: {ex.Message}");
                    }
                }

                Console.WriteLine($"[MemoryService] === 총 {moduleCount}개 모듈 처리 완료 ===");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MemoryService] DebugAllModulesCheatEngineStyle 오류: {ex.Message}");
                Console.WriteLine($"[MemoryService] 스택 트레이스: {ex.StackTrace}");
            }
        }

        public ModuleInfo FindModule(string targetModuleName)
        {
            if (!IsConnected)
            {
                Console.WriteLine("[MemoryService] 프로세스에 연결되지 않음");
                return null;
            }

            if (string.IsNullOrWhiteSpace(targetModuleName))
            {
                Console.WriteLine("[MemoryService] 모듈명이 비어있음");
                return null;
            }

            try
            {
                nint processHandle = _memoryReader.GetProcessHandle();
                IntPtr[] moduleHandles = new IntPtr[1024];
                uint bytesNeeded = 0;

                if (!Infrastructure.Memory.MemoryAPI.EnumProcessModules(
                    processHandle,
                    moduleHandles,
                    (uint)(moduleHandles.Length * IntPtr.Size),
                    out bytesNeeded))
                {
                    Console.WriteLine($"[MemoryService] EnumProcessModules 실패");
                    return null;
                }

                int moduleCount = (int)(bytesNeeded / IntPtr.Size);

                for (int i = 0; i < moduleCount && i < moduleHandles.Length; i++)
                {
                    if (moduleHandles[i] == IntPtr.Zero) continue;

                    try
                    {
                        var moduleNameBuilder = new StringBuilder(260);
                        uint nameLength = Infrastructure.Memory.MemoryAPI.GetModuleFileNameEx(
                            processHandle,
                            moduleHandles[i],
                            moduleNameBuilder,
                            (uint)moduleNameBuilder.Capacity);

                        if (nameLength > 0)
                        {
                            string fullPath = moduleNameBuilder.ToString();
                            string moduleName = Path.GetFileName(fullPath);

                            if (string.Equals(moduleName, targetModuleName, StringComparison.OrdinalIgnoreCase))
                            {
                                if (Infrastructure.Memory.MemoryAPI.GetModuleInformation(
                                    processHandle,
                                    moduleHandles[i],
                                    out Infrastructure.Memory.MemoryAPI.MODULEINFO moduleInfo,
                                    (uint)Marshal.SizeOf<Infrastructure.Memory.MemoryAPI.MODULEINFO>()))
                                {
                                    var result = new ModuleInfo(
                                        moduleName,
                                        moduleInfo.lpBaseOfDll,
                                        moduleInfo.SizeOfImage,
                                        fullPath
                                    );

                                    Console.WriteLine($"[MemoryService] 모듈 발견 (치트엔진 스타일): {result}");
                                    return result;
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[MemoryService] 모듈 {i} 검사 중 오류: {ex.Message}");
                    }
                }

                Console.WriteLine($"[MemoryService] 모듈 '{targetModuleName}'을 찾을 수 없음 (치트엔진 스타일)");
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MemoryService] FindModuleCheatEngineStyle 오류: {ex.Message}");
                return null;
            }
        }

        public void FindModulesByPattern(string searchPattern)
        {
            if (!IsConnected)
            {
                Console.WriteLine("[MemoryService] FindModulesByPattern: 프로세스에 연결되지 않음");
                return;
            }

            if (string.IsNullOrWhiteSpace(searchPattern))
            {
                Console.WriteLine("[MemoryService] FindModulesByPattern: 검색 패턴이 비어있음");
                return;
            }

            Console.WriteLine($"[MemoryService] === '{searchPattern}' 패턴으로 모듈 검색 ===");

            try
            {
                nint processHandle = _memoryReader.GetProcessHandle();
                IntPtr[] moduleHandles = new IntPtr[1024];
                uint bytesNeeded = 0;

                if (!Infrastructure.Memory.MemoryAPI.EnumProcessModules(
                    processHandle,
                    moduleHandles,
                    (uint)(moduleHandles.Length * IntPtr.Size),
                    out bytesNeeded))
                {
                    Console.WriteLine("[MemoryService] EnumProcessModules 실패");
                    return;
                }

                int moduleCount = (int)(bytesNeeded / IntPtr.Size);
                int foundCount = 0;

                for (int i = 0; i < moduleCount && i < moduleHandles.Length; i++)
                {
                    if (moduleHandles[i] == IntPtr.Zero) continue;

                    try
                    {
                        var moduleNameBuilder = new StringBuilder(260);
                        uint nameLength = Infrastructure.Memory.MemoryAPI.GetModuleFileNameEx(
                            processHandle,
                            moduleHandles[i],
                            moduleNameBuilder,
                            (uint)moduleNameBuilder.Capacity);

                        if (nameLength > 0)
                        {
                            string fullPath = moduleNameBuilder.ToString();
                            string moduleName = Path.GetFileName(fullPath);

                            if (moduleName.IndexOf(searchPattern, StringComparison.OrdinalIgnoreCase) >= 0)
                            {
                                foundCount++;

                                if (Infrastructure.Memory.MemoryAPI.GetModuleInformation(
                                    processHandle,
                                    moduleHandles[i],
                                    out Infrastructure.Memory.MemoryAPI.MODULEINFO moduleInfo,
                                    (uint)Marshal.SizeOf<Infrastructure.Memory.MemoryAPI.MODULEINFO>()))
                                {
                                    Console.WriteLine($"[발견] {moduleName}");
                                    Console.WriteLine($"       베이스: 0x{moduleInfo.lpBaseOfDll:X}, 크기: 0x{moduleInfo.SizeOfImage:X}");
                                    Console.WriteLine($"       경로: {fullPath}");
                                    Console.WriteLine();
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[MemoryService] 모듈 {i} 검사 중 오류: {ex.Message}");
                    }
                }

                if (foundCount == 0)
                {
                    Console.WriteLine($"[MemoryService] '{searchPattern}' 패턴과 일치하는 모듈을 찾을 수 없음");
                }
                else
                {
                    Console.WriteLine($"[MemoryService] === '{searchPattern}' 패턴으로 {foundCount}개 모듈 발견 ===");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MemoryService] FindModulesByPattern 오류: {ex.Message}");
            }
        }

        private string SafeGetString(string input)
        {
            if (string.IsNullOrEmpty(input))
                return "[비어있음]";

            var cleanChars = input.Where(c => !char.IsControl(c) && c >= 32 && c <= 126).ToArray();

            if (cleanChars.Length == 0)
                return "[읽기불가]";

            return new string(cleanChars);
        }

        private bool IsInKernel32Range(nint pointer, ModuleInfo kernel32Module)
        {
            if (kernel32Module == null || pointer == 0)
                return false;

            return pointer >= kernel32Module.BaseAddress && 
                   pointer < (kernel32Module.BaseAddress + (nint)kernel32Module.Size);
        }

        public int ReadLocalPlayerIndex()
        {
            if (!IsConnected)
            {
                Console.WriteLine("[MemoryService] ReadLocalPlayerIndex: 프로세스에 연결되지 않음");
                return -1;
            }

            try
            {
                var starcraftModule = GetStarCraftModule();
                if (starcraftModule == null)
                {
                    Console.WriteLine("[MemoryService] ReadLocalPlayerIndex: StarCraft 모듈을 찾을 수 없음");
                    return -1;
                }

                nint localPlayerIndexAddress = starcraftModule.BaseAddress + 0xDD5B5C;
                int localPlayerIndex = ReadByte(localPlayerIndexAddress);
                
                Console.WriteLine($"[MemoryService] LocalPlayerIndex 읽기 성공: {localPlayerIndex}");
                return localPlayerIndex;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MemoryService] ReadLocalPlayerIndex 예외: {ex.Message}");
                return -1;
            }
        }

        public int ReadGameTime()
        {
            if (!IsConnected)
            {
                Console.WriteLine("[MemoryService] ReadGameTime: 프로세스에 연결되지 않음");
                return -1;
            }

            try
            {
                nint threadStackAddress = GetThreadStackAddress(0);
                if (threadStackAddress == 0)
                {
                    Console.WriteLine("[MemoryService] ReadGameTime: ThreadStack 주소를 가져올 수 없음");
                    return -1;
                }


                nint baseAddress = threadStackAddress - 0x520;
                
                nint pointerAddress = ReadPointer(baseAddress);
                
                if (pointerAddress == 0)
                {
                    Console.WriteLine("[MemoryService] ReadGameTime: 포인터 읽기 실패");
                    return -1;
                }

                nint gameTimeAddress = pointerAddress + 0x14C;
                
                int gameTimeFrames = ReadInt(gameTimeAddress);
                
                int gameTimeSeconds = gameTimeFrames / 24;
                
                return gameTimeSeconds;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MemoryService] ReadGameTime 예외: {ex.Message}");
                return -1;
            }
        }

        public void Dispose()
        {
            if (_isDisposed) return;

            try
            {
                Disconnect();
                _isDisposed = true;
                Console.WriteLine("[MemoryService] 서비스 해제됨");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MemoryService] Dispose 오류: {ex.Message}");
            }
        }
    }
}