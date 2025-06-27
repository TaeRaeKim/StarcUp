using StarcUp.Business.Units.Runtime.Models;
using StarcUp.Infrastructure.Memory;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;

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

        public UnitMemoryAdapter(IMemoryReader memoryReader)
        {
            _memoryReader = memoryReader ?? throw new ArgumentNullException(nameof(memoryReader));

            _maxUnits = 3400;
            _unitSize = Marshal.SizeOf<UnitRaw>();

            _units = new UnitRaw[_maxUnits];
            _previousUnits = new UnitRaw[_maxUnits];

            Console.WriteLine($"UnitMemoryAdapter 초기화 완료 - 최대 유닛 수: {_maxUnits}");
        }

        public void SetUnitArrayBaseAddress(nint baseAddress)
        {
            _unitArrayBaseAddress = baseAddress;
            Console.WriteLine($"유닛 배열 베이스 주소 설정: 0x{baseAddress:X}");
        }

        public bool LoadAllUnits()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(UnitMemoryAdapter));

            if (_unitArrayBaseAddress == 0)
                throw new InvalidOperationException("Unit array base address not set");

            var stopwatch = Stopwatch.StartNew();

            try
            {
                Array.Copy(_units, _previousUnits, _maxUnits);

                bool success = _memoryReader.ReadStructureArrayIntoBuffer<UnitRaw>(
                    _unitArrayBaseAddress, _units, _maxUnits);

                if (!success)
                {
                    Console.WriteLine("유닛 배열 읽기 실패");
                    return false;
                }

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

        public IEnumerable<UnitRaw> GetAllRawUnits()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(UnitMemoryAdapter));

            return _units
                .Take(_currentUnitCount)
                .Where(IsRawUnitValid);
        }

        public IEnumerable<UnitRaw> GetPlayerRawUnits(byte playerId)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(UnitMemoryAdapter));

            return _units
                .Take(_currentUnitCount)
                .Where(unit => unit.playerIndex == playerId && IsRawUnitValid(unit));
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

        private int CountActiveUnits()
        {
            int count = 0;
            for (int i = 0; i < _maxUnits; i++)
            {
                if (_units[i].unitType != 0)
                {
                    count = i + 1;
                }
                else if (_units[i].unitType == 0)
                {
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

            _units = null;
            _previousUnits = null;

            _disposed = true;
            Console.WriteLine("UnitMemoryAdapter 리소스 정리 완료");
        }
    }
}