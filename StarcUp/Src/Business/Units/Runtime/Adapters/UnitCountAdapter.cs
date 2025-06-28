using StarcUp.Business.Units.Runtime.Models;
using StarcUp.Business.Units.Runtime.Repositories;
using StarcUp.Business.Units.Types;
using StarcUp.Business.MemoryService;
using System.Diagnostics;

namespace StarcUp.Business.Units.Runtime.Adapters
{
    public class UnitCountAdapter : IUnitCountAdapter
    {
        private readonly IMemoryService _memoryService;
        private readonly UnitOffsetRepository _offsetRepository;
        
        private byte[] _buffer;
        private nint _baseAddress;
        private bool _disposed;

        // 캐싱된 데이터
        private readonly Dictionary<UnitType, int> _completedOffsets;
        private readonly int _bufferSize;
        private readonly int _minBufferOffset;
        private readonly int _baseOffset;

        public UnitCountAdapter(IMemoryService memoryService, UnitOffsetRepository offsetRepository)
        {
            _memoryService = memoryService ?? throw new ArgumentNullException(nameof(memoryService));
            _offsetRepository = offsetRepository ?? throw new ArgumentNullException(nameof(offsetRepository));

            // 설정 로드 및 캐싱
            _completedOffsets = _offsetRepository.GetAllCompletedOffsets();
            _bufferSize = _offsetRepository.GetBufferSize();
            _minBufferOffset = _offsetRepository.GetMinBufferOffset();
            _baseOffset = _offsetRepository.GetBaseOffset();

            // 버퍼 초기화
            _buffer = new byte[_bufferSize];

            Console.WriteLine($"[UnitCountAdapter] 초기화 완료 - 버퍼 크기: {_bufferSize} bytes, 지원 유닛 수: {_completedOffsets.Count}");
        }

        public bool InitializeBaseAddress()
        {
            try
            {
                if (!_memoryService.IsConnected)
                {
                    Console.WriteLine("[UnitCountAdapter] ❌ MemoryService가 연결되지 않음");
                    return false;
                }

                // MemoryService의 GetThreadStackAddress 사용
                var threadStackAddress = _memoryService.GetThreadStackAddress(0);
                if (threadStackAddress == 0)
                {
                    Console.WriteLine("[UnitCountAdapter] ❌ THREADSTACK0 주소를 찾을 수 없습니다.");
                    return false;
                }

                // ThreadStack0 + _baseOffset( -0x520)으로 포인터 읽기
                nint pointerAddress = _memoryService.ReadPointer(threadStackAddress + _baseOffset);
                if (pointerAddress == 0)
                {
                    Console.WriteLine("[UnitCountAdapter] ❌ 포인터 읽기 실패");
                    return false;
                }

                _baseAddress = pointerAddress;
                Console.WriteLine($"[UnitCountAdapter] ✅ 베이스 주소 설정: 0x{_baseAddress:X}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[UnitCountAdapter] 베이스 주소 초기화 실패: {ex.Message}");
                return false;
            }
        }

        public bool LoadAllUnitCounts()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(UnitCountAdapter));

            if (!_memoryService.IsConnected)
            {
                Console.WriteLine("[UnitCountAdapter] ❌ MemoryService가 연결되지 않음");
                return false;
            }

            if (_baseAddress == 0)
            {
                Console.WriteLine("[UnitCountAdapter] 베이스 주소가 설정되지 않음, 자동 초기화 시도...");
                if (!InitializeBaseAddress())
                {
                    throw new InvalidOperationException("Base address initialization failed");
                }
            }

            try
            {
                // 계산된 시작 주소 (베이스 주소 + 최소 오프셋)
                nint startAddress = _baseAddress + _minBufferOffset;

                // MemoryService의 ReadMemoryIntoBuffer를 사용하여 한번에 모든 데이터 로드
                bool success = _memoryService.ReadMemoryIntoBuffer(startAddress, _buffer, _bufferSize);

                if (!success)
                {
                    Console.WriteLine("[UnitCountAdapter] ❌ 유닛 카운트 버퍼 읽기 실패");
                    return false;
                }
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[UnitCountAdapter] 유닛 카운트 로드 중 오류: {ex.Message}");
                return false;
            }
        }

        public int GetUnitCount(UnitType unitType, byte playerIndex, bool includeProduction = false)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(UnitCountAdapter));

            if (!_completedOffsets.TryGetValue(unitType, out var completedOffset))
            {
                return 0; // 지원하지 않는 유닛 타입
            }

            try
            {
                // 오프셋 계산
                var targetOffset = includeProduction 
                    ? _offsetRepository.GetProductionOffset(unitType)
                    : completedOffset;
                
                var playerOffset = targetOffset + (playerIndex * 4);
                var bufferOffset = _offsetRepository.GetBufferOffset(playerOffset);

                // 버퍼 범위 검사
                if (bufferOffset < 0 || bufferOffset + 4 > _bufferSize)
                {
                    Console.WriteLine($"[UnitCountAdapter] ❌ 버퍼 범위 초과: offset={bufferOffset}, bufferSize={_bufferSize}");
                    return 0;
                }

                // 버퍼에서 4바이트 int 값 읽기
                var count = BitConverter.ToInt32(_buffer, bufferOffset);
                return Math.Max(0, count); // 음수 방지
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[UnitCountAdapter] GetUnitCount 오류 ({unitType}, Player {playerIndex}): {ex.Message}");
                return 0;
            }
        }

        public Dictionary<UnitType, int> GetAllUnitCounts(byte playerIndex, bool includeProduction = false)
        {
            var result = new Dictionary<UnitType, int>();

            foreach (var unitType in _completedOffsets.Keys)
            {
                var count = GetUnitCount(unitType, playerIndex, includeProduction);
                if (count > 0)
                {
                    result[unitType] = count;
                }
            }

            return result;
        }

        public List<UnitCount> GetAllUnitCountsToBuffer(byte playerIndex, bool includeProduction = false)
        {
            var result = new List<UnitCount>();

            foreach (var unitType in _completedOffsets.Keys)
            {
                var completedCount = GetUnitCount(unitType, playerIndex, false);
                var productionCount = includeProduction ? 
                    GetUnitCount(unitType, playerIndex, true) - completedCount : 0;

                if (completedCount > 0 || productionCount > 0)
                {
                    result.Add(new UnitCount(unitType, playerIndex, completedCount, productionCount));
                }
            }

            return result;
        }

        public bool RefreshUnitCounts()
        {
            return LoadAllUnitCounts();
        }

        public void InvalidateCache()
        {
            Console.WriteLine("[UnitCountAdapter] 캐시 무효화");
            _baseAddress = 0;
        }

        public void Dispose()
        {
            if (_disposed) return;

            _buffer = null;
            _baseAddress = 0;
            _disposed = true;

            Console.WriteLine("[UnitCountAdapter] 리소스 정리 완료");
        }
    }
}