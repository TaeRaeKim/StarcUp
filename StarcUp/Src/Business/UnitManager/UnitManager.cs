using StarcUp.Business.UnitManager;
using StarcUp.Infrastructure.Memory;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;

namespace StarcUp.Business.UnitManager
{
    public class UnitManager : IUnitManager, IDisposable
    {
        private readonly IMemoryReader _memoryReader;
        private readonly int _maxUnits;
        private readonly int _unitSize;

        // 고정된 배열들 (재할당 방지)
        private Unit[] _units;
        private Unit[] _previousUnits; // 변경 감지용

        private nint _unitArrayBaseAddress;
        private int _currentUnitCount;
        private bool _disposed;

        public UnitManager(IMemoryReader memoryReader)
        {
            _memoryReader = memoryReader ?? throw new ArgumentNullException(nameof(memoryReader));

            _maxUnits = 3400;
            _unitSize = Marshal.SizeOf<Unit>();

            // 유닛 배열 초기화 (재할당 방지)
            _units = new Unit[_maxUnits];
            _previousUnits = new Unit[_maxUnits];

            Console.WriteLine($"UnitManager 초기화 완료 - 최대 유닛 수: {_maxUnits}");
        }

        public void SetUnitArrayBaseAddress(nint baseAddress)
        {
            _unitArrayBaseAddress = baseAddress;
            Console.WriteLine($"유닛 배열 베이스 주소 설정: 0x{baseAddress:X}");
        }

        public bool LoadAllUnits()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(UnitManager));

            if (_unitArrayBaseAddress == 0)
                throw new InvalidOperationException("Unit array base address not set");

            var stopwatch = Stopwatch.StartNew();

            try
            {
                // 1. 이전 데이터 백업 (변경 감지용)
                Array.Copy(_units, _previousUnits, _maxUnits);

                // 2. 통합된 MemoryReader의 최적화된 메서드 사용
                bool success = _memoryReader.ReadStructureArrayIntoBuffer<Unit>(
                    _unitArrayBaseAddress, _units, _maxUnits);

                if (!success)
                {
                    Console.WriteLine("유닛 배열 읽기 실패");
                    return false;
                }

                // 3. 유효한 유닛 수 계산
                _currentUnitCount = CountActiveUnits();

                stopwatch.Stop();
                Console.WriteLine($"유닛 로드 완료: {_currentUnitCount}개 유닛, {stopwatch.ElapsedMilliseconds}ms");

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

        public IEnumerable<Unit> GetAllUnits()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(UnitManager));

            return _units
                .Take(_currentUnitCount)
                .Where(IsUnitValid);
        }

        public IEnumerable<Unit> GetPlayerUnits(byte playerId)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(UnitManager));

            return _units
                .Take(_currentUnitCount)
                .Where(unit => unit.playerIndex == playerId && IsUnitValid(unit));
        }

        public IEnumerable<Unit> GetUnitsByType(ushort unitType)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(UnitManager));

            return _units
                .Take(_currentUnitCount)
                .Where(unit => unit.unitType == unitType && IsUnitValid(unit));
        }

        public IEnumerable<Unit> GetUnitsNearPosition(ushort x, ushort y, int radius)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(UnitManager));

            int radiusSquared = radius * radius;

            return _units
                .Take(_currentUnitCount)
                .Where(unit => IsUnitValid(unit) &&
                              GetDistanceSquared(unit.currentX, unit.currentY, x, y) <= radiusSquared);
        }

        public IEnumerable<(int Index, Unit Current, Unit Previous)> GetChangedUnits()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(UnitManager));

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

        public bool IsUnitValid(Unit unit)
        {
            return unit.unitType != 0 &&
                   unit.playerIndex < 12 &&
                   unit.health > 0;
        }

        private int CountActiveUnits()
        {
            int count = 0;
            for (int i = 0; i < _maxUnits; i++)
            {
                if (_units[i].unitType != 0)
                {
                    count = i + 1; // 마지막 유효한 인덱스 + 1
                }
                else if (_units[i].unitType == 0)
                {
                    // 빈 슬롯을 만나면 종료 (연속된 배열 가정)
                    break;
                }
            }
            return count;
        }

        private static int GetDistanceSquared(ushort x1, ushort y1, ushort x2, ushort y2)
        {
            int dx = x1 - x2;
            int dy = y1 - y2;
            return dx * dx + dy * dy;
        }

        public void Dispose()
        {
            if (_disposed) return;

            // 메모리 리더는 외부에서 관리하므로 Dispose하지 않음
            _units = null;
            _previousUnits = null;

            _disposed = true;
            Console.WriteLine("UnitManager 리소스 정리 완료");
        }
    }
}