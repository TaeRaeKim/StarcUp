using StarcUp.Business.GameDetection;
using StarcUp.Business.Units.Runtime.Repositories;
using StarcUp.Business.Units.Types;
using StarcUp.Common.Events;
using StarcUp.Common.Logging;
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

        private readonly IGameDetector _gameDetector;
        private readonly IMemoryReader _memoryReader;
        private readonly UnitOffsetRepository _unitOffsetRepository;
        private readonly object _lockObject = new object();
        private bool _isDisposed;

        private List<TebInfo> _cachedTebList;
        private ModuleInfo _cachedKernel32Module;
        private ModuleInfo _cachedUser32Module;
        private ModuleInfo _cachedStarCraftModule;
        private nint _cachedThreadStackBasePointer;

        public bool IsConnected => _memoryReader?.IsConnected ?? false;
        public int ConnectedProcessId => _memoryReader?.ConnectedProcessId ?? 0;

        public MemoryService(IGameDetector gameDetector, IMemoryReader memoryReader, UnitOffsetRepository unitOffsetRepository)
        {
            _gameDetector = gameDetector ?? throw new ArgumentNullException(nameof(gameDetector));
            _memoryReader = memoryReader ?? throw new ArgumentNullException(nameof(memoryReader));
            _unitOffsetRepository = unitOffsetRepository ?? throw new ArgumentNullException(nameof(unitOffsetRepository));

            _gameDetector.HandleFound += (object sender, GameEventArgs e) => ConnectToProcess(e.GameInfo.ProcessId);
            _gameDetector.HandleLost += (object sender, GameEventArgs e) => Disconnect();

            LoggerHelper.Info("초기화 완료");
            RefreshAllCache();
        }
        public bool ConnectToProcess(int processId)
        {
            if (_isDisposed)
            {
                LoggerHelper.Warning("ConnectToProcess: 서비스가 이미 해제됨");
                return false;
            }

            if (processId <= 0)
            {
                LoggerHelper.Error("ConnectToProcess: 잘못된 프로세스 ID");
                return false;
            }

            try
            {
                bool wasConnected = IsConnected;
                int previousProcessId = ConnectedProcessId;

                if (wasConnected)
                {
                    LoggerHelper.Info("기존 연결 해제 중...");
                    Disconnect();
                }

                LoggerHelper.Info($"프로세스 {processId}에 연결 시도...");

                if (_memoryReader.ConnectToProcess(processId))
                {
                    RefreshAllCache();
                    InitializeBasePointer();
                    LoggerHelper.Info($"프로세스 {processId} 연결 성공");
                    ProcessConnect?.Invoke(this, new ProcessEventArgs(processId));
                    return true;
                }
                else
                {
                    LoggerHelper.Error($"프로세스 {processId} 연결 실패");
                    return false;
                }
            }
            catch (Exception ex)
            {
                LoggerHelper.Error($"ConnectToProcess 예외: {ex.Message}");
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
                _cachedThreadStackBasePointer = 0;
                LoggerHelper.Info($"프로세스 {processId} 연결 해제됨");
                ProcessDisconnect?.Invoke(this, new ProcessEventArgs(processId));
            }
            catch (Exception ex)
            {
                LoggerHelper.Error($"Disconnect 예외: {ex.Message}");
            }
        }

        public int ReadInt(nint address)
        {
            if (!IsConnected)
            {
                LoggerHelper.Debug("ReadInt: 프로세스에 연결되지 않음");
                return 0;
            }

            if (!IsValidAddress(address))
            {
                LoggerHelper.Debug($"ReadInt: 잘못된 주소 0x{address:X}");
                return 0;
            }

            try
            {
                return _memoryReader.ReadInt(address);
            }
            catch (Exception ex)
            {
                LoggerHelper.Error($"ReadInt 오류: {ex.Message}");
                return 0;
            }
        }

        public float ReadFloat(nint address)
        {
            if (!IsConnected)
            {
                LoggerHelper.Debug("ReadFloat: 프로세스에 연결되지 않음");
                return 0f;
            }

            if (!IsValidAddress(address))
            {
                LoggerHelper.Debug($"ReadFloat: 잘못된 주소 0x{address:X}");
                return 0f;
            }

            try
            {
                return _memoryReader.ReadFloat(address);
            }
            catch (Exception ex)
            {
                LoggerHelper.Error($"ReadFloat 오류: {ex.Message}");
                return 0f;
            }
        }

        public double ReadDouble(nint address)
        {
            if (!IsConnected)
            {
                LoggerHelper.Debug("ReadDouble: 프로세스에 연결되지 않음");
                return 0.0;
            }

            if (!IsValidAddress(address))
            {
                LoggerHelper.Debug($"ReadDouble: 잘못된 주소 0x{address:X}");
                return 0.0;
            }

            try
            {
                return _memoryReader.ReadDouble(address);
            }
            catch (Exception ex)
            {
                LoggerHelper.Error($"ReadDouble 오류: {ex.Message}");
                return 0.0;
            }
        }

        public byte ReadByte(nint address)
        {
            if (!IsConnected)
            {
                LoggerHelper.Debug("ReadByte: 프로세스에 연결되지 않음");
                return 0;
            }

            if (!IsValidAddress(address))
            {
                LoggerHelper.Debug($"ReadByte: 잘못된 주소 0x{address:X}");
                return 0;
            }

            try
            {
                return _memoryReader.ReadByte(address);
            }
            catch (Exception ex)
            {
                LoggerHelper.Error($"ReadByte 오류: {ex.Message}");
                return 0;
            }
        }

        public short ReadShort(nint address)
        {
            if (!IsConnected)
            {
                LoggerHelper.Debug("ReadShort: 프로세스에 연결되지 않음");
                return 0;
            }

            if (!IsValidAddress(address))
            {
                LoggerHelper.Debug($"ReadShort: 잘못된 주소 0x{address:X}");
                return 0;
            }

            try
            {
                return _memoryReader.ReadShort(address);
            }
            catch (Exception ex)
            {
                LoggerHelper.Error($"ReadShort 오류: {ex.Message}");
                return 0;
            }
        }

        public long ReadLong(nint address)
        {
            if (!IsConnected)
            {
                LoggerHelper.Debug("ReadLong: 프로세스에 연결되지 않음");
                return 0L;
            }

            if (!IsValidAddress(address))
            {
                LoggerHelper.Debug($"ReadLong: 잘못된 주소 0x{address:X}");
                return 0L;
            }

            try
            {
                return _memoryReader.ReadLong(address);
            }
            catch (Exception ex)
            {
                LoggerHelper.Error($"ReadLong 오류: {ex.Message}");
                return 0L;
            }
        }

        public bool ReadBool(nint address)
        {
            if (!IsConnected)
            {
                LoggerHelper.Debug("ReadBool: 프로세스에 연결되지 않음");
                return false;
            }

            if (!IsValidAddress(address))
            {
                LoggerHelper.Debug($"ReadBool: 잘못된 주소 0x{address:X}");
                return false;
            }

            try
            {
                return _memoryReader.ReadBool(address);
            }
            catch (Exception ex)
            {
                LoggerHelper.Error($"ReadBool 오류: {ex.Message}");
                return false;
            }
        }

        public nint ReadPointer(nint address)
        {
            if (!IsConnected)
            {
                LoggerHelper.Debug("ReadPointer: 프로세스에 연결되지 않음");
                return 0;
            }

            if (!IsValidAddress(address))
            {
                LoggerHelper.Debug($"ReadPointer: 잘못된 주소 0x{address:X}");
                return 0;
            }

            try
            {
                return _memoryReader.ReadPointer(address);
            }
            catch (Exception ex)
            {
                LoggerHelper.Error($"ReadPointer 오류: {ex.Message}");
                return 0;
            }
        }

        public string ReadString(nint address, int maxLength = 256, Encoding encoding = null)
        {
            if (!IsConnected)
            {
                LoggerHelper.Debug("ReadString: 프로세스에 연결되지 않음");
                return string.Empty;
            }

            if (!IsValidAddress(address))
            {
                LoggerHelper.Debug($"ReadString: 잘못된 주소 0x{address:X}");
                return string.Empty;
            }

            if (maxLength <= 0)
            {
                LoggerHelper.Debug("ReadString: 잘못된 최대 길이");
                return string.Empty;
            }

            try
            {
                return _memoryReader.ReadString(address, maxLength, encoding);
            }
            catch (Exception ex)
            {
                LoggerHelper.Error($"ReadString 오류: {ex.Message}");
                return string.Empty;
            }
        }

        public T ReadStructure<T>(nint address) where T : struct
        {
            if (!IsConnected)
            {
                LoggerHelper.Debug("ReadStructure: 프로세스에 연결되지 않음");
                return default(T);
            }

            if (!IsValidAddress(address))
            {
                LoggerHelper.Debug($"ReadStructure: 잘못된 주소 0x{address:X}");
                return default(T);
            }

            try
            {
                return _memoryReader.ReadStructure<T>(address);
            }
            catch (Exception ex)
            {
                LoggerHelper.Error($"ReadStructure 오류: {ex.Message}");
                return default(T);
            }
        }

        public T[] ReadStructureArray<T>(nint address, int count) where T : struct
        {
            if (!IsConnected)
            {
                LoggerHelper.Debug("ReadStructureArray: 프로세스에 연결되지 않음");
                return new T[0];
            }

            if (!IsValidAddress(address))
            {
                LoggerHelper.Debug($"ReadStructureArray: 잘못된 주소 0x{address:X}");
                return new T[0];
            }

            if (count <= 0)
            {
                LoggerHelper.Debug("ReadStructureArray: 잘못된 카운트");
                return new T[0];
            }

            try
            {
                return _memoryReader.ReadStructureArray<T>(address, count);
            }
            catch (Exception ex)
            {
                LoggerHelper.Error($"ReadStructureArray 오류: {ex.Message}");
                return new T[0];
            }
        }

        public nint GetPebAddress()
        {
            if (!IsConnected)
            {
                LoggerHelper.Debug("GetPebAddress: 프로세스에 연결되지 않음");
                return 0;
            }

            try
            {
                int status = _memoryReader.QueryProcessInformation(out var processInfo);
                if (status == 0)
                {
                    LoggerHelper.Debug($"PEB 주소: 0x{processInfo.PebBaseAddress:X}");
                    return processInfo.PebBaseAddress;
                }
                else
                {
                    LoggerHelper.Error($"GetPebAddress: NtQueryInformationProcess 실패, 상태: {status}");
                    return 0;
                }
            }
            catch (Exception ex)
            {
                LoggerHelper.Error($"GetPebAddress 오류: {ex.Message}");
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
                    LoggerHelper.Debug("GetTebAddresses: 프로세스에 연결되지 않음");
                    return new List<TebInfo>();
                }

                var tebList = new List<TebInfo>();

                try
                {
                    nint snapshot = _memoryReader.CreateThreadSnapshot();
                    if (snapshot == 0)
                    {
                        LoggerHelper.Error("스레드 스냅샷 생성 실패");
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
                    LoggerHelper.Debug($"TEB 캐시 생성: {tebList.Count}개");
                }
                catch (Exception ex)
                {
                    LoggerHelper.Error($"GetTebAddresses 오류: {ex.Message}");
                }

                return tebList;
            }
        }

        public nint GetStackTop(int threadIndex = 0)
        {
            var tebList = GetTebAddresses();
            if (threadIndex < 0 || threadIndex >= tebList.Count)
            {
                LoggerHelper.Error($"GetStackTop: 잘못된 스레드 인덱스 {threadIndex} (최대: {tebList.Count - 1})");
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
                LoggerHelper.Error($"GetStackTop 오류: {ex.Message}");
                return 0;
            }
        }

        public nint GetStackBottom(int threadIndex = 0)
        {
            var tebList = GetTebAddresses();
            if (threadIndex < 0 || threadIndex >= tebList.Count)
            {
                LoggerHelper.Error($"GetStackBottom: 잘못된 스레드 인덱스 {threadIndex} (최대: {tebList.Count - 1})");
                return 0;
            }

            try
            {
                nint tebAddress = tebList[threadIndex].TebAddress;
                nint stackBottomPtr = tebAddress + 0x10;
                nint stackBottom = ReadPointer(stackBottomPtr);
                LoggerHelper.Debug($"스레드 {threadIndex} 스택 하단: 0x{stackBottom:X}");
                return stackBottom;
            }
            catch (Exception ex)
            {
                LoggerHelper.Error($"GetStackBottom 오류: {ex.Message}");
                return 0;
            }
        }

        public nint GetThreadStackAddress(int threadIndex = 0)
        {
            if (!IsConnected)
            {
                LoggerHelper.Debug("GetThreadStackAddress: 프로세스에 연결되지 않음");
                return 0;
            }

            try
            {
                var kernel32Module = GetKernel32Module();
                if (kernel32Module == null)
                {
                    LoggerHelper.Error("GetThreadStackAddress: kernel32.dll 모듈 정보를 가져올 수 없음");
                    return 0;
                }

                nint stackTop = GetStackTop(threadIndex);
                if (stackTop == 0)
                {
                    LoggerHelper.Error("GetThreadStackAddress: StackTop을 가져올 수 없음");
                    return 0;
                }

                nint stackSearchStart = stackTop - 4096;
                byte[] stackBuffer = new byte[4096];

                if (!ReadMemoryIntoBuffer(stackSearchStart, stackBuffer, 4096))
                {
                    LoggerHelper.Error("GetThreadStackAddress: 스택 메모리 읽기 실패");
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

                LoggerHelper.Error("GetThreadStackAddress: kernel32를 가리키는 스택 엔트리를 찾을 수 없음");
                return 0;
            }
            catch (Exception ex)
            {
                LoggerHelper.Error($"GetThreadStackAddress 오류: {ex.Message}");
                return 0;
            }
        }

        public bool FindModule(string moduleName, out ModuleInfo moduleInfo)
        {
            moduleInfo = null;
            if (!IsConnected)
            {
                LoggerHelper.Debug("FindModule: 프로세스에 연결되지 않음");
                return false;
            }

            if (string.IsNullOrWhiteSpace(moduleName))
            {
                LoggerHelper.Debug("FindModule: 모듈명이 비어있음");
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
                LoggerHelper.Debug($"{name} 검색 시도...");
                if (FindModule(name, out var moduleInfo))
                {
                    _cachedKernel32Module = moduleInfo;
                    LoggerHelper.Info($"kernel32 모듈 발견: {name}");
                    return moduleInfo;
                }
            }
            
            LoggerHelper.Warning("kernel32 모듈을 찾지 못했습니다.");
            return null;
        }

        public ModuleInfo GetUser32Module()
        {
            if (_cachedUser32Module != null)
            {
                LoggerHelper.Debug("user32 캐시 사용");
                return _cachedUser32Module;
            }

            if (FindModule("user32.dll", out var moduleInfo))
            {
                _cachedUser32Module = moduleInfo;
                LoggerHelper.Debug("user32 캐시 생성");
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
                LoggerHelper.Debug($"{name} 검색 시도...");
                if (FindModule(name, out var moduleInfo))
                {
                    _cachedStarCraftModule = moduleInfo;
                    LoggerHelper.Info($"kernel32 모듈 발견: {name}");
                    return moduleInfo;
                }
            }
            
            LoggerHelper.Warning("StarCraft 모듈을 찾지 못했습니다.");
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
                LoggerHelper.Debug("TEB 캐시 무효화됨");
            }
        }

        public void RefreshModuleCache()
        {
            lock (_lockObject)
            {
                _cachedKernel32Module = null;
                _cachedUser32Module = null;
                LoggerHelper.Debug("모듈 캐시 무효화됨");
            }
        }

        public void RefreshAllCache()
        {
            lock (_lockObject)
            {
                RefreshTebCache();
                RefreshModuleCache();
                LoggerHelper.Debug("모든 캐시 무효화됨");
            }
        }

        public void InitializeBasePointer()
        {
            try
            {
                // ThreadStack 주소 가져오기
                nint threadStackAddress = GetThreadStackAddress(0);
                if (threadStackAddress == 0)
                {
                    LoggerHelper.Warning("InitializeBasePointer: ThreadStack 주소를 찾을 수 없습니다.");
                    _cachedThreadStackBasePointer = 0;
                    return;
                }

                // BaseOffset 가져오기
                int baseOffset = _unitOffsetRepository.GetBaseOffset();
                
                // ThreadStack - baseOffset 위치에서 포인터 읽기
                nint baseAddress = threadStackAddress - baseOffset;
                _cachedThreadStackBasePointer = ReadPointer(baseAddress);
            }
            catch (Exception ex)
            {
                LoggerHelper.Error($"InitializeBasePointer 오류: {ex.Message}");
                _cachedThreadStackBasePointer = 0;
            }
        }

        public nint GetBasePointer()
        {
            return _cachedThreadStackBasePointer;
        }

        public bool ReadMemoryIntoBuffer(nint address, byte[] buffer, int size)
        {
            if (!IsConnected)
            {
                LoggerHelper.Debug("ReadMemoryIntoBuffer: 프로세스에 연결되지 않음");
                return false;
            }

            if (!IsValidAddress(address))
            {
                LoggerHelper.Debug($"ReadMemoryIntoBuffer: 잘못된 주소 0x{address:X}");
                return false;
            }

            if (buffer == null || size <= 0 || size > buffer.Length)
            {
                LoggerHelper.Debug("ReadMemoryIntoBuffer: 잘못된 버퍼 또는 크기");
                return false;
            }

            try
            {
                return _memoryReader.ReadMemoryIntoBuffer(address, buffer, size);
            }
            catch (Exception ex)
            {
                LoggerHelper.Error($"ReadMemoryIntoBuffer 오류: {ex.Message}");
                return false;
            }
        }

        public bool ReadStructureArrayIntoBuffer<T>(nint address, T[] buffer, int count) where T : unmanaged
        {
            if (!IsConnected)
            {
                LoggerHelper.Debug("ReadStructureArrayIntoBuffer: 프로세스에 연결되지 않음");
                return false;
            }

            if (!IsValidAddress(address))
            {
                LoggerHelper.Debug($"ReadStructureArrayIntoBuffer: 잘못된 주소 0x{address:X}");
                return false;
            }

            if (buffer == null || count <= 0 || count > buffer.Length)
            {
                LoggerHelper.Debug("ReadStructureArrayIntoBuffer: 잘못된 버퍼 또는 카운트");
                return false;
            }

            try
            {
                return _memoryReader.ReadStructureArrayIntoBuffer<T>(address, buffer, count);
            }
            catch (Exception ex)
            {
                LoggerHelper.Error($"ReadStructureArrayIntoBuffer 오류: {ex.Message}");
                return false;
            }
        }

        public void DebugAllModules()
        {
            if (!IsConnected)
            {
                LoggerHelper.Debug("DebugAllModules: 프로세스에 연결되지 않음");
                return;
            }

            LoggerHelper.Info($"=== PID {ConnectedProcessId} 모든 모듈 목록 (치트엔진 방식) ===");

            try
            {
                nint processHandle = _memoryReader.GetProcessHandle();
                if (processHandle == IntPtr.Zero)
                {
                    LoggerHelper.Debug(" 프로세스 핸들을 가져올 수 없음");
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
                    LoggerHelper.Debug($" EnumProcessModules 실패, 오류 코드: {error}");
                    return;
                }

                int moduleCount = (int)(bytesNeeded / IntPtr.Size);
                LoggerHelper.Debug($" 발견된 모듈 수: {moduleCount}");

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
                                LoggerHelper.Debug($"[{i + 1:D2}] 모듈명: {moduleName}");
                                LoggerHelper.Debug($"     베이스주소: 0x{moduleInfo.lpBaseOfDll:X}");
                                LoggerHelper.Debug($"     크기: 0x{moduleInfo.SizeOfImage:X} ({moduleInfo.SizeOfImage:N0} bytes)");
                                LoggerHelper.Debug($"     전체경로: {fullPath}");
                                LoggerHelper.Debug("");

                                if (moduleName.ToLower().Contains("starcraft") ||
                                    moduleName.ToLower().Contains("star") ||
                                    fullPath.ToLower().Contains("starcraft"))
                                {
                                    LoggerHelper.Info($"★ StarCraft 관련 모듈 발견: {moduleName}");
                                    LoggerHelper.Debug("");
                                }
                            }
                        }
                        else
                        {
                            LoggerHelper.Debug($"[{i + 1:D2}] 모듈명을 가져올 수 없음 (핸들: 0x{moduleHandles[i]:X})");
                        }
                    }
                    catch (Exception ex)
                    {
                        LoggerHelper.Debug($" 모듈 {i} 정보 읽기 실패: {ex.Message}");
                    }
                }

                LoggerHelper.Debug($" === 총 {moduleCount}개 모듈 처리 완료 ===");
            }
            catch (Exception ex)
            {
                LoggerHelper.Debug($" DebugAllModules 오류: {ex.Message}");
                LoggerHelper.Debug($" 스택 트레이스: {ex.StackTrace}");
            }
        }

        public void DebugAllModulesCheatEngineStyle()
        {
            if (!IsConnected)
            {
                LoggerHelper.Debug(" 프로세스에 연결되지 않음");
                return;
            }

            LoggerHelper.Debug($" === PID {ConnectedProcessId} 모든 모듈 목록 (치트엔진 스타일) ===");

            try
            {
                nint processHandle = _memoryReader.GetProcessHandle();
                if (processHandle == IntPtr.Zero)
                {
                    LoggerHelper.Debug(" 프로세스 핸들을 가져올 수 없음");
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
                    LoggerHelper.Debug($" EnumProcessModules 실패, 오류 코드: {error}");
                    return;
                }

                int moduleCount = (int)(bytesNeeded / IntPtr.Size);
                LoggerHelper.Debug($" 발견된 모듈 수: {moduleCount}");

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
                                LoggerHelper.Debug($"[{i + 1:D2}] 모듈명: {moduleName}");
                                LoggerHelper.Debug($"     베이스주소: 0x{moduleInfo.lpBaseOfDll:X}");
                                LoggerHelper.Debug($"     크기: 0x{moduleInfo.SizeOfImage:X} ({moduleInfo.SizeOfImage:N0} bytes)");
                                LoggerHelper.Debug($"     전체경로: {fullPath}");
                                LoggerHelper.Debug("");

                                if (moduleName.ToLower().Contains("starcraft") ||
                                    moduleName.ToLower().Contains("star") ||
                                    fullPath.ToLower().Contains("starcraft"))
                                {
                                    LoggerHelper.Info($"★ StarCraft 관련 모듈 발견: {moduleName}");
                                    LoggerHelper.Debug("");
                                }
                            }
                        }
                        else
                        {
                            LoggerHelper.Debug($"[{i + 1:D2}] 모듈명을 가져올 수 없음 (핸들: 0x{moduleHandles[i]:X})");
                        }
                    }
                    catch (Exception ex)
                    {
                        LoggerHelper.Debug($" 모듈 {i} 정보 읽기 실패: {ex.Message}");
                    }
                }

                LoggerHelper.Debug($" === 총 {moduleCount}개 모듈 처리 완료 ===");
            }
            catch (Exception ex)
            {
                LoggerHelper.Debug($" DebugAllModulesCheatEngineStyle 오류: {ex.Message}");
                LoggerHelper.Debug($" 스택 트레이스: {ex.StackTrace}");
            }
        }

        public ModuleInfo FindModule(string targetModuleName)
        {
            if (!IsConnected)
            {
                LoggerHelper.Debug(" 프로세스에 연결되지 않음");
                return null;
            }

            if (string.IsNullOrWhiteSpace(targetModuleName))
            {
                LoggerHelper.Debug(" 모듈명이 비어있음");
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
                    LoggerHelper.Debug($" EnumProcessModules 실패");
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

                                    LoggerHelper.Debug($" 모듈 발견 (치트엔진 스타일): {result}");
                                    return result;
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        LoggerHelper.Debug($" 모듈 {i} 검사 중 오류: {ex.Message}");
                    }
                }

                LoggerHelper.Debug($" 모듈 '{targetModuleName}'을 찾을 수 없음 (치트엔진 스타일)");
                return null;
            }
            catch (Exception ex)
            {
                LoggerHelper.Debug($" FindModuleCheatEngineStyle 오류: {ex.Message}");
                return null;
            }
        }

        public void FindModulesByPattern(string searchPattern)
        {
            if (!IsConnected)
            {
                LoggerHelper.Debug(" FindModulesByPattern: 프로세스에 연결되지 않음");
                return;
            }

            if (string.IsNullOrWhiteSpace(searchPattern))
            {
                LoggerHelper.Debug(" FindModulesByPattern: 검색 패턴이 비어있음");
                return;
            }

            LoggerHelper.Debug($" === '{searchPattern}' 패턴으로 모듈 검색 ===");

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
                    LoggerHelper.Debug(" EnumProcessModules 실패");
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
                                    LoggerHelper.Debug($"[발견] {moduleName}");
                                    LoggerHelper.Debug($"       베이스: 0x{moduleInfo.lpBaseOfDll:X}, 크기: 0x{moduleInfo.SizeOfImage:X}");
                                    LoggerHelper.Debug($"       경로: {fullPath}");
                                    LoggerHelper.Debug("");
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        LoggerHelper.Debug($" 모듈 {i} 검사 중 오류: {ex.Message}");
                    }
                }

                if (foundCount == 0)
                {
                    LoggerHelper.Debug($" '{searchPattern}' 패턴과 일치하는 모듈을 찾을 수 없음");
                }
                else
                {
                    LoggerHelper.Debug($" === '{searchPattern}' 패턴으로 {foundCount}개 모듈 발견 ===");
                }
            }
            catch (Exception ex)
            {
                LoggerHelper.Debug($" FindModulesByPattern 오류: {ex.Message}");
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
                LoggerHelper.Debug(" ReadLocalPlayerIndex: 프로세스에 연결되지 않음");
                return -1;
            }

            try
            {
                var starcraftModule = GetStarCraftModule();
                if (starcraftModule == null)
                {
                    LoggerHelper.Debug(" ReadLocalPlayerIndex: StarCraft 모듈을 찾을 수 없음");
                    return -1;
                }

                nint localPlayerIndexAddress = starcraftModule.BaseAddress + 0xDD5B5C;
                int localPlayerIndex = ReadByte(localPlayerIndexAddress);
                
                LoggerHelper.Debug($" LocalPlayerIndex 읽기 성공: {localPlayerIndex}");
                return localPlayerIndex;
            }
            catch (Exception ex)
            {
                LoggerHelper.Debug($" ReadLocalPlayerIndex 예외: {ex.Message}");
                return -1;
            }
        }

        public int ReadGameTime()
        {
            if (!IsConnected)
            {
                LoggerHelper.Debug(" ReadGameTime: 프로세스에 연결되지 않음");
                return -1;
            }

            try
            {
                nint threadStackAddress = GetThreadStackAddress(0);
                if (threadStackAddress == 0)
                {
                    LoggerHelper.Debug(" ReadGameTime: ThreadStack 주소를 가져올 수 없음");
                    return -1;
                }


                // GetBasePointer()는 이미 ThreadStack - baseOffset에서 ReadPointer한 값
                nint pointerAddress = GetBasePointer();
                
                if (pointerAddress == 0)
                {
                    LoggerHelper.Debug(" ReadGameTime: 포인터 읽기 실패");
                    return -1;
                }

                nint gameTimeAddress = pointerAddress + 0x14C;
                
                int gameTimeFrames = ReadInt(gameTimeAddress);
                
                int gameTimeSeconds = gameTimeFrames / 24;
                
                return gameTimeSeconds;
            }
            catch (Exception ex)
            {
                LoggerHelper.Debug($" ReadGameTime 예외: {ex.Message}");
                return -1;
            }
        }

        public RaceType ReadPlayerRace(int playerIndex)
        {
            if (!IsConnected)
            {
                LoggerHelper.Debug(" ReadPlayerRace: 프로세스에 연결되지 않음");
                return RaceType.Zerg;
            }

            try
            {
                var starcraftModule = GetStarCraftModule();
                if (starcraftModule == null)
                {
                    LoggerHelper.Debug(" ReadPlayerRace: StarCraft 모듈을 찾을 수 없음");
                    return RaceType.Zerg;
                }

                // 어셈블리 계산 구현
                // mov rdi, 3CDB939D3F07AAA4
                nint rdi = unchecked((nint)0x3CDB939D3F07AAA4);
                
                // sub rdi,[StarCraft.exe+E42AFC]
                nint addr1 = starcraftModule.BaseAddress + 0xE42AFC;
                nint value1 = ReadPointer(addr1);
                rdi = rdi - value1;
                
                // xor rdi,[StarCraft.exe+1091F60]
                nint addr2 = starcraftModule.BaseAddress + 0x1091F60;
                nint value2 = ReadPointer(addr2);
                rdi = rdi ^ value2;

                // rdi + (playerIndex + playerIndex * 8) * 4 + 09
                nint playerRaceAddress = rdi + (playerIndex + playerIndex * 8) * 4 + 0x09;
                byte race = ReadByte(playerRaceAddress);
                
                LoggerHelper.Debug($" 플레이어 {playerIndex} 종족 읽기 성공: {race}");
                return (RaceType)race;
            }
            catch (Exception ex)
            {
                LoggerHelper.Debug($" ReadPlayerRace 예외: {ex.Message}");
                return RaceType.Zerg;
            }
        }

        public int ReadSupplyUsed(int playerIndex, RaceType race)
        {
            if (!IsConnected)
            {
                LoggerHelper.Debug(" ReadSupplyUsed: 프로세스에 연결되지 않음");
                return 0;
            }

            try
            {
                nint threadStackAddress = GetThreadStackAddress(0);
                if (threadStackAddress == 0)
                {
                    LoggerHelper.Debug(" ReadSupplyUsed: ThreadStack 주소를 가져올 수 없음");
                    return 0;
                }

                // 포인터 체인: threadStackAddress - baseOffset
                nint baseAddress = threadStackAddress - _unitOffsetRepository.GetBaseOffset();
                nint pointerAddress = ReadPointer(baseAddress);
                
                if (pointerAddress == 0)
                {
                    LoggerHelper.Debug(" ReadSupplyUsed: 포인터 읽기 실패");
                    return 0;
                }

                // 종족별 오프셋 가져오기
                int supplyUsedOffset = race switch
                {
                    RaceType.Zerg => _unitOffsetRepository.GetZergSupplyUsedOffset(),
                    RaceType.Terran => _unitOffsetRepository.GetTerranSupplyUsedOffset(),
                    RaceType.Protoss => _unitOffsetRepository.GetProtossSupplyUsedOffset(),
                    _ => throw new ArgumentException($"잘못된 종족 값: {race}")
                };

                // 플레이어별 주소 계산
                nint supplyUsedAddress = pointerAddress + supplyUsedOffset + (playerIndex * 4);
                int supplyUsed = ReadInt(supplyUsedAddress);
                
                return supplyUsed;
            }
            catch (Exception ex)
            {
                LoggerHelper.Debug($" ReadSupplyUsed 예외: {ex.Message}");
                return 0;
            }
        }

        public int ReadSupplyMax(int playerIndex, RaceType race)
        {
            if (!IsConnected)
            {
                LoggerHelper.Debug(" ReadSupplyMax: 프로세스에 연결되지 않음");
                return 0;
            }

            try
            {
                nint threadStackAddress = GetThreadStackAddress(0);
                if (threadStackAddress == 0)
                {
                    LoggerHelper.Debug(" ReadSupplyMax: ThreadStack 주소를 가져올 수 없음");
                    return 0;
                }

                // 포인터 체인: threadStackAddress - baseOffset
                nint baseAddress = threadStackAddress - _unitOffsetRepository.GetBaseOffset();
                nint pointerAddress = ReadPointer(baseAddress);
                
                if (pointerAddress == 0)
                {
                    LoggerHelper.Debug(" ReadSupplyMax: 포인터 읽기 실패");
                    return 0;
                }

                // 종족별 오프셋 가져오기
                int supplyMaxOffset = race switch
                {
                    RaceType.Zerg => _unitOffsetRepository.GetZergSupplyMaxOffset(),
                    RaceType.Terran => _unitOffsetRepository.GetTerranSupplyMaxOffset(),
                    RaceType.Protoss => _unitOffsetRepository.GetProtossSupplyMaxOffset(),
                    _ => throw new ArgumentException($"잘못된 종족 값: {race}")
                };

                // 플레이어별 주소 계산
                nint supplyMaxAddress = pointerAddress + supplyMaxOffset + (playerIndex * 4);
                int supplyMax = ReadInt(supplyMaxAddress);
                
                return supplyMax;
            }
            catch (Exception ex)
            {
                LoggerHelper.Debug($" ReadSupplyMax 예외: {ex.Message}");
                return 0;
            }
        }
        public void Dispose()
        {
            if (_isDisposed) return;

            try
            {
                Disconnect();
                _isDisposed = true;
                LoggerHelper.Debug(" 서비스 해제됨");
            }
            catch (Exception ex)
            {
                LoggerHelper.Debug($" Dispose 오류: {ex.Message}");
            }
        }
    }
}