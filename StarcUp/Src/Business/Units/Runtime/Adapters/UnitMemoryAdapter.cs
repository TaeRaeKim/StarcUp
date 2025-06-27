using StarcUp.Business.Units.Runtime.Models;
using StarcUp.Infrastructure.Memory;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace StarcUp.Business.Units.Runtime.Adapters
{
    public class UnitMemoryAdapter : IUnitMemoryAdapter
    {
        private readonly IMemoryReader _memoryReader;
        private readonly int _maxUnits;
        private readonly int _unitSize;

        private UnitRaw[] _units;
        private UnitRaw[] _previousUnits;

        private nint _unitArrayBaseAddress;
        private int _currentUnitCount;
        private bool _disposed;

        // 동적 주소 계산 및 캐싱
        private nint _cachedUnitArrayAddress;
        private bool _isAddressCached;

        // 플레이어별 유닛 포인터 주소들
        private readonly Dictionary<byte, nint> _playerUnitPointers = new Dictionary<byte, nint>();
        private nint _starcraftBaseAddress;

        public UnitMemoryAdapter(IMemoryReader memoryReader)
        {
            _memoryReader = memoryReader ?? throw new ArgumentNullException(nameof(memoryReader));

            _maxUnits = 3400;
            _unitSize = Marshal.SizeOf<UnitRaw>();

            _units = new UnitRaw[_maxUnits];
            _previousUnits = new UnitRaw[_maxUnits];

            Console.WriteLine($"UnitMemoryAdapter 초기화 완료 - 최대 유닛 수: {_maxUnits}, 유닛 크기: {_unitSize}바이트");
        }

        public void SetUnitArrayBaseAddress(nint baseAddress)
        {
            _unitArrayBaseAddress = baseAddress;
            Console.WriteLine($"[UnitMemoryAdapter] 유닛 배열 베이스 주소 설정: 0x{baseAddress:X}");
        }

        public bool InitializeUnitArrayAddress()
        {
            return InitializeUnitArrayAddress(true);
        }

        private bool InitializeUnitArrayAddress(bool loadDataAfterInit)
        {
            try
            {
                // 캐싱된 주소가 있으면 사용
                if (_isAddressCached && _cachedUnitArrayAddress != 0)
                {
                    Console.WriteLine($"[UnitMemoryAdapter] 캐싱된 유닛 배열 주소 사용: 0x{_cachedUnitArrayAddress:X}");
                    _unitArrayBaseAddress = _cachedUnitArrayAddress;
                    
                    if (loadDataAfterInit)
                    {
                        // 캐싱된 주소로 유닛 데이터 로드
                        Console.WriteLine("[UnitMemoryAdapter] 캐싱된 주소로 유닛 데이터 로드 중...");
                        bool loadSuccess = LoadAllUnits(false); // 재귀 방지
                        if (!loadSuccess)
                        {
                            Console.WriteLine("[UnitMemoryAdapter] ⚠️ 캐싱된 주소로 유닛 데이터 로드 실패");
                        }
                        return loadSuccess;
                    }
                    else
                    {
                        Console.WriteLine("[UnitMemoryAdapter] 주소만 설정 (데이터 로드 생략)");
                        return true;
                    }
                }

                Console.WriteLine("[UnitMemoryAdapter] 유닛 배열 주소 계산 중...");

                // StarCraft.exe 모듈 찾기
                var starcraftModule = FindStarCraftModule();
                if (starcraftModule == null)
                {
                    Console.WriteLine("[UnitMemoryAdapter] ❌ StarCraft.exe 모듈을 찾을 수 없습니다.");
                    return false;
                }

                Console.WriteLine($"[UnitMemoryAdapter] StarCraft.exe 베이스 주소: 0x{starcraftModule.Value.modBaseAddr:X}");

                // StarCraft 베이스 주소 저장
                _starcraftBaseAddress = starcraftModule.Value.modBaseAddr;
                
                // 플레이어별 유닛 포인터 주소 초기화
                InitializePlayerUnitPointers();

                // Step 1: 포인터 주소 계산
                nint pointerAddress = starcraftModule.Value.modBaseAddr + 0xE77FE0 + 0x80;
                
                // Step 2: 실제 유닛 배열 주소 읽기
                nint finalUnitArrayAddress = _memoryReader.ReadPointer(pointerAddress);

                Console.WriteLine($"[UnitMemoryAdapter] 올바른 주소 계산:");
                Console.WriteLine($"  - StarCraft.exe BaseAddr: 0x{starcraftModule.Value.modBaseAddr:X}");
                Console.WriteLine($"  - 포인터 주소 (Base + 0xE77FE0 + 0x80): 0x{pointerAddress:X}");
                Console.WriteLine($"  - 실제 유닛 배열 주소 (포인터에서 읽은 값): 0x{finalUnitArrayAddress:X}");

                // 유효성 검사
                if (finalUnitArrayAddress == 0)
                {
                    Console.WriteLine("[UnitMemoryAdapter] ❌ 포인터에서 읽은 유닛 배열 주소가 0입니다.");
                    return false;
                }

                // 주소 캐싱
                _cachedUnitArrayAddress = finalUnitArrayAddress;
                _isAddressCached = true;
                _unitArrayBaseAddress = finalUnitArrayAddress;

                Console.WriteLine("[UnitMemoryAdapter] ✅ 유닛 배열 주소 초기화 성공");
                
                if (loadDataAfterInit)
                {
                    // 주소 설정 후 유닛 데이터 로드
                    Console.WriteLine("[UnitMemoryAdapter] 유닛 데이터 로드 중...");
                    bool loadSuccess = LoadAllUnits(false); // 재귀 방지
                    if (!loadSuccess)
                    {
                        Console.WriteLine("[UnitMemoryAdapter] ⚠️ 유닛 데이터 로드 실패");
                    }
                    return loadSuccess;
                }
                else
                {
                    Console.WriteLine("[UnitMemoryAdapter] 주소만 설정 (데이터 로드 생략)");
                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[UnitMemoryAdapter] 유닛 배열 주소 초기화 실패: {ex.Message}");
                _isAddressCached = false;
                return false;
            }
        }

        private MemoryAPI.MODULEENTRY32? FindStarCraftModule()
        {
            // 가능한 모듈명 (정리된 버전)
            string[] possibleNames = {
                "StarCraft.exe",
                "StarCraft"
            };

            Console.WriteLine("[UnitMemoryAdapter] 모듈 검색 시도...");

            // PSAPI 방식 (EnumProcessModules) - 기존 성공 방식
            foreach (string targetName in possibleNames)
            {
                var module = FindModuleByPSAPI(targetName);
                if (module.HasValue)
                {
                    Console.WriteLine($"[UnitMemoryAdapter] PSAPI로 모듈 발견: {module.Value.szModule}");
                    return module;
                }
            }

            Console.WriteLine("[UnitMemoryAdapter] StarCraft 모듈을 찾을 수 없음");
            return null;
        }

        private MemoryAPI.MODULEENTRY32? FindModuleByPSAPI(string targetName)
        {
            try
            {
                nint processHandle = _memoryReader.GetProcessHandle();
                IntPtr[] moduleHandles = new IntPtr[1024];
                uint bytesNeeded = 0;

                if (!MemoryAPI.EnumProcessModules(processHandle, moduleHandles, 
                    (uint)(moduleHandles.Length * IntPtr.Size), out bytesNeeded))
                {
                    return null;
                }

                int moduleCount = (int)(bytesNeeded / IntPtr.Size);
                
                for (int i = 0; i < moduleCount; i++)
                {
                    var moduleHandle = moduleHandles[i];
                    if (moduleHandle == IntPtr.Zero) continue;

                    var fileName = new StringBuilder(256);
                    uint result = MemoryAPI.GetModuleFileNameEx(processHandle, moduleHandle, fileName, 256);
                    
                    if (result > 0)
                    {
                        string moduleName = System.IO.Path.GetFileName(fileName.ToString());
                        
                        if (moduleName.IndexOf(targetName, StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            // MODULEENTRY32 구조로 변환
                            if (_memoryReader.GetModuleInformation(moduleHandle, out var moduleInfo))
                            {
                                var moduleEntry = new MemoryAPI.MODULEENTRY32
                                {
                                    modBaseAddr = moduleInfo.lpBaseOfDll,
                                    modBaseSize = moduleInfo.SizeOfImage,
                                    szModule = moduleName,
                                    szExePath = fileName.ToString()
                                };
                                return moduleEntry;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[UnitMemoryAdapter] PSAPI 모듈 검색 오류: {ex.Message}");
            }

            return null;
        }

        public bool LoadAllUnits()
        {
            return LoadAllUnits(true);
        }

        private bool LoadAllUnits(bool allowAutoInitialize)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(UnitMemoryAdapter));

            // 주소가 설정되지 않았으면 자동으로 초기화 시도 (무한 재귀 방지)
            if (_unitArrayBaseAddress == 0 && allowAutoInitialize)
            {
                Console.WriteLine("[UnitMemoryAdapter] 주소가 설정되지 않음, 자동 초기화 시도...");
                if (!InitializeUnitArrayAddress())
                {
                    throw new InvalidOperationException("Unit array base address initialization failed");
                }
            }

            var stopwatch = Stopwatch.StartNew();

            try
            {
                Array.Copy(_units, _previousUnits, _maxUnits);

                
                bool success = _memoryReader.ReadStructureArrayIntoBuffer<UnitRaw>(
                    _unitArrayBaseAddress, _units, _maxUnits);

                if (!success)
                {
                    Console.WriteLine("[UnitMemoryAdapter] ❌ 유닛 배열 읽기 실패");
                    return false;
                }

                
                // 0번 인덱스 유닛만 자세히 출력 (디버깅용)
                var unit0 = _units[0];
                
                // nextPointer 계산 테스트
                if (unit0.nextPointer != 0)
                {
                    long addressOffset = unit0.nextPointer - _unitArrayBaseAddress;
                    long nextIndex = addressOffset / _unitSize;
                    
                    // 다음 인덱스가 유효 범위인지 확인
                    if (nextIndex >= 0 && nextIndex < _maxUnits)
                    {
                        var nextUnit = _units[nextIndex];
                    }
                    else
                    {
                        Console.WriteLine($"  ❌ 계산된 인덱스가 유효 범위를 벗어남 (0-{_maxUnits-1})");
                    }
                }

                _currentUnitCount = CountActiveUnits();

                stopwatch.Stop();

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"유닛 로드 중 오류: {ex.Message}");
                return false;
            }
        }

        public bool RefreshUnits()
        {
            return LoadAllUnits();
        }

        public IEnumerable<UnitRaw> GetAllRawUnits()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(UnitMemoryAdapter));

            try
            {
                // 연결 리스트 방식으로 활성 유닛들만 효율적으로 반환
                var activeUnits = GetActiveUnitsFromLinkedList();
                Console.WriteLine($"[UnitMemoryAdapter] GetAllRawUnits: 연결 리스트에서 {activeUnits.Count}개 유닛 반환");
                return activeUnits.Select(x => x.unit);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[UnitMemoryAdapter] GetAllRawUnits 연결 리스트 방식 실패: {ex.Message}");
                // fallback to old method
                return _units
                    .Take(_currentUnitCount)
                    .Where(IsRawUnitValid);
            }
        }

        public IEnumerable<UnitRaw> GetPlayerRawUnits(byte playerId)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(UnitMemoryAdapter));

            try
            {
                // nextAllyPointer를 사용한 효율적인 플레이어 유닛 조회
                var playerUnits = GetPlayerUnits(playerId).ToList();
                
                if (playerUnits.Count > 0)
                {
                    return playerUnits;
                }
                
                // nextAllyPointer 방식이 실패하면 기존 방식으로 fallback
                var activeUnits = GetActiveUnitsFromLinkedList();
                var fallbackUnits = activeUnits.Where(x => x.unit.playerIndex == playerId).Select(x => x.unit).ToList();
                
                return fallbackUnits;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[UnitMemoryAdapter] GetPlayerRawUnits 실패: {ex.Message}");
                // 마지막 fallback - 전체 스캔
                return _units
                    .Take(_currentUnitCount)
                    .Where(unit => unit.playerIndex == playerId && IsRawUnitValid(unit));
            }
        }

        public IEnumerable<UnitRaw> GetRawUnitsByType(ushort unitType)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(UnitMemoryAdapter));

            return _units
                .Take(_currentUnitCount)
                .Where(unit => unit.unitType == unitType && IsRawUnitValid(unit));
        }

        public IEnumerable<UnitRaw> GetRawUnitsNearPosition(ushort x, ushort y, int radius)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(UnitMemoryAdapter));

            int radiusSquared = radius * radius;

            return _units
                .Take(_currentUnitCount)
                .Where(unit => IsRawUnitValid(unit) &&
                              GetDistanceSquared(unit.currentX, unit.currentY, x, y) <= radiusSquared);
        }

        public IEnumerable<(int Index, UnitRaw Current, UnitRaw Previous)> GetChangedRawUnits()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(UnitMemoryAdapter));

            for (int i = 0; i < _currentUnitCount; i++)
            {
                if (!_units[i].Equals(_previousUnits[i]))
                {
                    yield return (i, _units[i], _previousUnits[i]);
                }
            }
        }

        public int GetActiveUnitCount()
        {
            return _currentUnitCount;
        }

        public bool IsRawUnitValid(UnitRaw unit)
        {
            return unit.unitType != 0 &&
                   unit.playerIndex < 12 &&
                   unit.health > 0;
        }

        /// <summary>
        /// nextPointer를 따라가며 활성 유닛 리스트 반환
        /// </summary>
        private List<(int index, UnitRaw unit)> GetActiveUnitsFromLinkedList()
        {
            var activeUnits = new List<(int index, UnitRaw unit)>();
            
            // 0번 인덱스부터 시작 (일반적으로 연결 리스트의 헤드)
            int currentIndex = 0;
            int iterations = 0;

            while (currentIndex >= 0 && currentIndex < _maxUnits && iterations < _maxUnits)
            {
                iterations++;
                var currentUnit = _units[currentIndex];
                
                // 유효한 유닛이면 리스트에 추가
                if (IsRawUnitValid(currentUnit))
                {
                    activeUnits.Add((currentIndex, currentUnit));
                }

                // nextPointer가 0이면 연결 리스트 끝
                if (currentUnit.nextPointer == 0)
                {
                    break;
                }

                // 다음 유닛 인덱스 계산: (nextPointer - baseAddr) / unitSize
                long addressOffset = currentUnit.nextPointer - _unitArrayBaseAddress;
                
                // 주소 오프셋 유효성 검사
                if (addressOffset < 0)
                {
                    Console.WriteLine($"[UnitMemoryAdapter] ❌ nextPointer가 base address보다 작음: offset={addressOffset}");
                    break;
                }
                
                if (addressOffset % _unitSize != 0)
                {
                    Console.WriteLine($"[UnitMemoryAdapter] ❌ nextPointer 주소가 유닛 크기에 맞지 않음: offset={addressOffset}, unitSize={_unitSize}");
                    break;
                }

                int nextIndex = (int)(addressOffset / _unitSize);
                
                // 다음 인덱스 유효성 검사
                if (nextIndex < 0 || nextIndex >= _maxUnits)
                {
                    Console.WriteLine($"[UnitMemoryAdapter] ❌ 계산된 다음 인덱스가 범위를 벗어남: {nextIndex} (범위: 0-{_maxUnits-1})");
                    break;
                }
                                                
                currentIndex = nextIndex;
            }

            
            
            return activeUnits;
        }

        /// <summary>
        /// 기존 전체 스캔 방식 (fallback용)
        /// </summary>
        private int CountActiveUnitsFullScan()
        {
            int count = 0;
            
            for (int i = 0; i < _maxUnits; i++)
            {
                if (IsRawUnitValid(_units[i]))
                {
                    count++;
                }
            }
            
            Console.WriteLine($"[UnitMemoryAdapter] 전체 스캔 방식 카운팅 완료: {count}개");
            return count;
        }

        /// <summary>
        /// 두 방식의 성능을 비교하고 더 나은 결과 반환
        /// </summary>
        private int CountActiveUnits()
        {
            // 연결 리스트 방식으로 효율적인 카운팅
            try
            {
                var activeUnits = GetActiveUnitsFromLinkedList();
                int linkedListCount = activeUnits.Count;
                
                return linkedListCount;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[UnitMemoryAdapter] 연결 리스트 방식 실패: {ex.Message}, 전체 스캔으로 fallback");
                return CountActiveUnitsFullScan();
            }
        }

        private static int GetDistanceSquared(ushort x1, ushort y1, ushort x2, ushort y2)
        {
            int dx = x1 - x2;
            int dy = y1 - y2;
            return dx * dx + dy * dy;
        }

        /// <summary>
        /// 플레이어별 유닛 포인터 주소 초기화
        /// </summary>
        private void InitializePlayerUnitPointers()
        {
            if (_starcraftBaseAddress == 0)
            {
                Console.WriteLine("[UnitMemoryAdapter] ❌ StarCraft 베이스 주소가 설정되지 않음");
                return;
            }

            // 플레이어별 유닛 포인터 주소 계산
            // 0번 플레이어: StarCraft.exe + E77FE0 + 20 = 0x20 오프셋
            // 1번 플레이어: StarCraft.exe + E77FE0 + 28 = 0x28 오프셋 (0x20 + 8)
            // 2번 플레이어: StarCraft.exe + E77FE0 + 30 = 0x30 오프셋 (0x20 + 16)
            // 3번 플레이어: StarCraft.exe + E77FE0 + 38 = 0x38 오프셋 (0x20 + 24)
            
            _playerUnitPointers.Clear();
            
            for (byte playerId = 0; playerId < 8; playerId++) // 최대 8명 플레이어
            {
                nint playerPointerAddress = _starcraftBaseAddress + 0xE77FE0 + 0x20 + (playerId * 8);
                _playerUnitPointers[playerId] = playerPointerAddress;
                
            }
        }

        /// <summary>
        /// 특정 플레이어의 유닛만 nextAllyPointer를 통해 순회하여 반환 (IEnumerable 방식)
        /// </summary>
        /// <param name="playerId">플레이어 ID (0-7)</param>
        /// <returns>해당 플레이어의 유닛 목록</returns>
        public IEnumerable<UnitRaw> GetPlayerUnits(byte playerId)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(UnitMemoryAdapter));

            if (!_playerUnitPointers.ContainsKey(playerId))
            {
                return Enumerable.Empty<UnitRaw>();
            }

            var playerUnits = new List<UnitRaw>();
            
            try
            {
                // 플레이어의 첫 번째 유닛 포인터 읽기
                nint playerPointerAddress = _playerUnitPointers[playerId];
                nint firstUnitPointer = _memoryReader.ReadPointer(playerPointerAddress);
                
                if (firstUnitPointer == 0)
                {
                    return playerUnits;
                }

                // nextAllyPointer를 따라가며 해당 플레이어의 유닛들 순회
                nint currentUnitPointer = firstUnitPointer;
                int iterations = 0;
                const int maxIterations = 200; // 무한 루프 방지

                while (currentUnitPointer != 0 && iterations < maxIterations)
                {
                    iterations++;

                    // 포인터 주소가 유효한 유닛 배열 범위 내인지 확인
                    if (currentUnitPointer < _unitArrayBaseAddress || 
                        currentUnitPointer >= _unitArrayBaseAddress + (_maxUnits * _unitSize))
                    {
                        break;
                    }

                    // 유닛 인덱스 계산
                    long unitOffset = currentUnitPointer - _unitArrayBaseAddress;
                    if (unitOffset % _unitSize != 0)
                    {
                        break;
                    }

                    int unitIndex = (int)(unitOffset / _unitSize);
                    if (unitIndex < 0 || unitIndex >= _maxUnits)
                    {
                        break;
                    }

                    // 유닛 데이터 가져오기
                    var currentUnit = _units[unitIndex];
                    
                    // 유닛이 유효하고 해당 플레이어의 유닛인지 확인
                    if (IsRawUnitValid(currentUnit) && currentUnit.playerIndex == playerId)
                    {
                        playerUnits.Add(currentUnit);
                    }

                    // 다음 아군 포인터로 이동
                    currentUnitPointer = currentUnit.nextAllyPointer;
                }

                return playerUnits;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[UnitMemoryAdapter] Player {playerId} 유닛 순회 중 오류: {ex.Message}");
                return Enumerable.Empty<UnitRaw>();
            }
        }

        /// <summary>
        /// 특정 플레이어의 유닛을 Unit 배열에 직접 복사 (nextAllyPointer 사용, 최고 효율성)
        /// </summary>
        /// <param name="playerId">플레이어 ID (0-7)</param>
        /// <param name="buffer">결과를 저장할 Unit 배열</param>
        /// <param name="maxCount">버퍼의 최대 크기</param>
        /// <returns>실제로 복사된 유닛 수</returns>
        public int GetPlayerUnitsToBuffer(byte playerId, Unit[] buffer, int maxCount)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(UnitMemoryAdapter));

            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));

            if (maxCount <= 0 || maxCount > buffer.Length)
                throw new ArgumentException("Invalid maxCount", nameof(maxCount));

            if (!_playerUnitPointers.ContainsKey(playerId))
            {
                return 0; // 플레이어 포인터가 설정되지 않음
            }

            int unitCount = 0;
            
            try
            {
                // 플레이어의 첫 번째 유닛 포인터 읽기
                nint playerPointerAddress = _playerUnitPointers[playerId];
                nint firstUnitPointer = _memoryReader.ReadPointer(playerPointerAddress);
                
                if (firstUnitPointer == 0)
                {
                    return 0; // 유닛 없음
                }

                // nextAllyPointer를 따라가며 해당 플레이어의 유닛들을 Unit으로 변환하여 버퍼에 직접 복사
                nint currentUnitPointer = firstUnitPointer;
                int iterations = 0;
                const int maxIterations = 200; // 무한 루프 방지

                while (currentUnitPointer != 0 && iterations < maxIterations && unitCount < maxCount)
                {
                    iterations++;

                    // 포인터 주소가 유효한 유닛 배열 범위 내인지 확인
                    if (currentUnitPointer < _unitArrayBaseAddress || 
                        currentUnitPointer >= _unitArrayBaseAddress + (_maxUnits * _unitSize))
                    {
                        break;
                    }

                    // 유닛 인덱스 계산
                    long unitOffset = currentUnitPointer - _unitArrayBaseAddress;
                    if (unitOffset % _unitSize != 0)
                    {
                        break;
                    }

                    int unitIndex = (int)(unitOffset / _unitSize);
                    if (unitIndex < 0 || unitIndex >= _maxUnits)
                    {
                        break;
                    }

                    // 유닛 데이터 가져오기
                    var currentUnitRaw = _units[unitIndex];
                    
                    // 유닛이 유효하고 해당 플레이어의 유닛인지 확인
                    if (IsRawUnitValid(currentUnitRaw) && currentUnitRaw.playerIndex == playerId)
                    {
                        // 기존 Unit 인스턴스에 UnitRaw 데이터를 직접 파싱 (메모리 재활용)
                        buffer[unitCount].ParseRaw(currentUnitRaw);
                        
                        // Unit 레벨에서도 유효성 검사
                        if (buffer[unitCount].IsValid)
                        {
                            unitCount++;
                        }
                    }

                    // 다음 아군 포인터로 이동
                    currentUnitPointer = currentUnitRaw.nextAllyPointer;
                }

                return unitCount;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[UnitMemoryAdapter] Player {playerId} 유닛 버퍼 복사 중 오류: {ex.Message}");
                return unitCount; // 지금까지 복사된 개수 반환
            }
        }

        public void InvalidateAddressCache()
        {
            Console.WriteLine("[UnitMemoryAdapter] 주소 캐시 무효화");
            _isAddressCached = false;
            _cachedUnitArrayAddress = 0;
            _unitArrayBaseAddress = 0;
            _starcraftBaseAddress = 0;
            _playerUnitPointers.Clear();
        }

        public void Dispose()
        {
            if (_disposed) return;

            _units = null;
            _previousUnits = null;
            
            // 캐시 정리
            _isAddressCached = false;
            _cachedUnitArrayAddress = 0;
            _unitArrayBaseAddress = 0;
            _starcraftBaseAddress = 0;
            _playerUnitPointers.Clear();

            _disposed = true;
            Console.WriteLine("UnitMemoryAdapter 리소스 정리 완료");
        }
    }
}