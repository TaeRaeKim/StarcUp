using StarcUp.Infrastructure.Memory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace StarcUp.Src.Business.UnitManager
{
    public class UnitManager : IUnitManager
    {
        private readonly IMemoryReader _memoryReader;
        private readonly int _maxUnits;
        private readonly int _unitSize;
        private readonly int _totalMemorySize;

        // 스타크래프트 유닛 배열의 베이스 주소 (게임에서 찾아야 함)
        private nint _unitArrayBaseAddress;

        // 현재 로드된 유닛들
        private Unit[] _units;
        private int _currentUnitCount;

        public UnitManager(IMemoryReader memoryReader)
        {
            _memoryReader = memoryReader;
            _maxUnits = 3400;
            _unitSize = Marshal.SizeOf<Unit>(); // 488 바이트 확인
            _totalMemorySize = _maxUnits * _unitSize;
            _units = new Unit[_maxUnits];
            _currentUnitCount = 0;
        }

        /// <summary>
        /// 스타크래프트 프로세스에서 유닛 배열의 베이스 주소를 설정
        /// </summary>
        public void SetUnitArrayBaseAddress(nint baseAddress)
        {
            _unitArrayBaseAddress = baseAddress;
        }

        /// <summary>
        /// 메모리에서 모든 유닛 정보를 한 번에 로드
        /// </summary>
        public bool LoadAllUnits()
        {
            if (_unitArrayBaseAddress == nint.Zero)
            {
                throw new InvalidOperationException("Unit array base address not set");
            }

            try
            {
                // 전체 유닛 배열을 한 번에 읽기 (1.6MB)
                byte[] unitMemoryBlock = _memoryReader.ReadMemory(_unitArrayBaseAddress, _totalMemorySize);

                if (unitMemoryBlock == null || unitMemoryBlock.Length != _totalMemorySize)
                {
                    return false;
                }

                // 바이트 배열을 Unit 구조체 배열로 변환
                _units = ByteArrayToUnitArray(unitMemoryBlock);

                // 실제 사용 중인 유닛 개수 계산 (연결 리스트 포인터가 0이 아닌 것들)
                _currentUnitCount = CountActiveUnits();

                return true;
            }
            catch (Exception ex)
            {
                // 로깅 처리
                Console.WriteLine($"Failed to load units: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 특정 범위의 유닛만 로드 (성능 최적화용)
        /// </summary>
        public bool LoadUnitsRange(int startIndex, int count)
        {
            if (_unitArrayBaseAddress == nint.Zero)
            {
                throw new InvalidOperationException("Unit array base address not set");
            }

            if (startIndex < 0 || startIndex >= _maxUnits || count <= 0)
            {
                return false;
            }

            int actualCount = Math.Min(count, _maxUnits - startIndex);
            int rangeSize = actualCount * _unitSize;
            nint rangeAddress = _unitArrayBaseAddress + (startIndex * _unitSize);

            try
            {
                byte[] unitMemoryBlock = _memoryReader.ReadMemory(rangeAddress, rangeSize);

                if (unitMemoryBlock == null || unitMemoryBlock.Length != rangeSize)
                {
                    return false;
                }

                // 특정 범위만 업데이트
                Unit[] rangeUnits = ByteArrayToUnitArray(unitMemoryBlock);
                Array.Copy(rangeUnits, 0, _units, startIndex, actualCount);

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to load unit range: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 바이트 배열을 Unit 구조체 배열로 변환
        /// </summary>
        private Unit[] ByteArrayToUnitArray(byte[] bytes)
        {
            int unitCount = bytes.Length / _unitSize;
            Unit[] units = new Unit[unitCount];

            for (int i = 0; i < unitCount; i++)
            {
                int offset = i * _unitSize;
                byte[] unitBytes = new byte[_unitSize];
                Array.Copy(bytes, offset, unitBytes, 0, _unitSize);

                // 바이트 배열을 구조체로 변환
                GCHandle handle = GCHandle.Alloc(unitBytes, GCHandleType.Pinned);
                try
                {
                    units[i] = Marshal.PtrToStructure<Unit>(handle.AddrOfPinnedObject());
                }
                finally
                {
                    handle.Free();
                }
            }

            return units;
        }

        /// <summary>
        /// 활성화된 유닛 개수 계산
        /// </summary>
        private int CountActiveUnits()
        {
            int count = 0;
            for (int i = 0; i < _units.Length; i++)
            {
                // 유닛이 활성화되어 있는지 확인 (예: 유닛 타입이 0이 아님)
                if (_units[i].unitType != 0 && (_units[i].nextPointer != 0 || _units[i].prevPointer != 0))
                {
                    count++;
                }
            }
            return count;
        }

        /// <summary>
        /// 모든 유닛 목록 반환
        /// </summary>
        public IEnumerable<Unit> GetAllUnits()
        {
            for (int i = 0; i < _currentUnitCount && i < _units.Length; i++)
            {
                if (_units[i].unitType != 0)
                {
                    yield return _units[i];
                }
            }
        }

        /// <summary>
        /// 특정 플레이어의 유닛들만 반환
        /// </summary>
        public IEnumerable<Unit> GetPlayerUnits(byte playerId)
        {
            for (int i = 0; i < _units.Length; i++)
            {
                if (_units[i].unitType != 0 && _units[i].playerIndex == playerId)
                {
                    yield return _units[i];
                }
            }
        }

        /// <summary>
        /// 특정 유닛 타입의 유닛들만 반환
        /// </summary>
        public IEnumerable<Unit> GetUnitsByType(ushort unitType)
        {
            for (int i = 0; i < _units.Length; i++)
            {
                if (_units[i].unitType == unitType)
                {
                    yield return _units[i];
                }
            }
        }

        /// <summary>
        /// 특정 위치 근처의 유닛들 반환
        /// </summary>
        public IEnumerable<Unit> GetUnitsNearPosition(ushort x, ushort y, int radius)
        {
            int radiusSquared = radius * radius;

            for (int i = 0; i < _units.Length; i++)
            {
                if (_units[i].unitType != 0)
                {
                    int dx = _units[i].currentX - x;
                    int dy = _units[i].currentY - y;
                    int distanceSquared = dx * dx + dy * dy;

                    if (distanceSquared <= radiusSquared)
                    {
                        yield return _units[i];
                    }
                }
            }
        }

        public int CurrentUnitCount => _currentUnitCount;

        public int MaxUnits => _maxUnits;

        public bool RefreshUnits()
        {
            return LoadAllUnits();
        }
    }
}
