using StarcUp.Infrastructure.Memory;
using StarcUp.Src.Infrastructure.Memory;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace StarcUp.Src.Business.UnitManager
{ 
    public class UnitManager : IUnitManager, IDisposable
    {
        private readonly IMemoryReader _memoryReader;
        private readonly IOptimizedMemoryReader _optimizedMemoryReader; // 최적화된 리더 추가
        private readonly int _maxUnits;
        private readonly int _unitSize;
        private readonly int _totalMemorySize;

        // ArrayPool을 사용한 임시 버퍼들
        private readonly ArrayPool<byte> _byteArrayPool;

        // 고정된 배열들 (재할당 방지)
        private Unit[] _units;
        private Unit[] _previousUnits; // 변경 감지용

        private nint _unitArrayBaseAddress;
        private int _currentUnitCount;
        private bool _disposed;
        private readonly bool _useOptimizedReader; // 최적화된 리더 사용 여부

        public UnitManager(IMemoryReader memoryReader, bool useDoubleBuffering = true)
        {
            _memoryReader = memoryReader;

            // IOptimizedMemoryReader 사용 가능한지 확인
            _optimizedMemoryReader = memoryReader as IOptimizedMemoryReader;
            _useOptimizedReader = _optimizedMemoryReader != null;

            _maxUnits = 3400;
            _unitSize = Marshal.SizeOf<Unit>();
            _totalMemorySize = _maxUnits * _unitSize;

            // ArrayPool 인스턴스 생성 (공유 풀 사용)
            _byteArrayPool = ArrayPool<byte>.Shared;

            // 유닛 배열 초기화 (재할당 방지)
            _units = new Unit[_maxUnits];
            _previousUnits = new Unit[_maxUnits];

            Console.WriteLine($"최적화된 리더 사용: {_useOptimizedReader}");
        }


        public void SetUnitArrayBaseAddress(nint baseAddress)
        {
            _unitArrayBaseAddress = baseAddress;
            Console.WriteLine($"유닛 배열 베이스 주소 설정: 0x{baseAddress:X}");
        }

        /// <summary>
        /// 최적화된 유닛 로드 - 버퍼 재사용
        /// </summary>
        public bool LoadAllUnits()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(UnitManager));

            if (_unitArrayBaseAddress == 0)
                throw new InvalidOperationException("Unit array base address not set");

            var stopwatch = Stopwatch.StartNew();

            try
            {
                //// 현재 사용할 버퍼 선택 (더블 버퍼링)
                //byte[] currentBuffer = GetCurrentBuffer();

                //// 1. 메모리 읽기 - 기존 버퍼 재사용
                //if (!ReadMemoryToBuffer(currentBuffer))
                //{
                //    return false;
                //}

                // 2. 이전 데이터 백업 (변경 감지용)
                Array.Copy(_units, _previousUnits, _maxUnits);

                _optimizedMemoryReader.ReadStructureArrayIntoBuffer<Unit>(_unitArrayBaseAddress, _units, _maxUnits);

                //// 3. 인플레이스 변환 (새로운 배열 할당 없음)
                //ConvertBufferToUnitsInPlace(currentBuffer);

                // 4. 활성 유닛 개수 업데이트
                _currentUnitCount = CountActiveUnits();

                stopwatch.Stop();

                //// 버퍼 교체 (더블 버퍼링)
                //SwapBuffers();

                Console.WriteLine($"로드 완료: {_currentUnitCount}개 유닛, 소요시간: {stopwatch.ElapsedMilliseconds}ms");

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"유닛 로드 실패: {ex.Message}");
                return false;
            }
        }


        /// <summary>
        /// 변경된 유닛들만 탐지 (델타 업데이트)
        /// </summary>
        public IEnumerable<(int Index, Unit Current, Unit Previous)> GetChangedUnits()
        {
            for (int i = 0; i < _maxUnits; i++)
            {
                if (!UnitsEqual(_units[i], _previousUnits[i]))
                {
                    yield return (i, _units[i], _previousUnits[i]);
                }
            }
        }

        /// <summary>
        /// 유닛 동등성 비교 (주요 필드만)
        /// </summary>
        private bool UnitsEqual(Unit a, Unit b)
        {
            return a.unitType == b.unitType &&
                   a.health == b.health &&
                   a.currentX == b.currentX &&
                   a.currentY == b.currentY &&
                   a.playerIndex == b.playerIndex &&
                   a.actionIndex == b.actionIndex;
        }

        /// <summary>
        /// 활성 유닛 개수 계산
        /// </summary>
        public int CountActiveUnits()
        {
            int count = 0;
            for (int i = 0; i < _maxUnits; i++)
            {
                if (_units[i].unitType != 0)
                    count++;
            }
            return count;
        }

        // 기존 인터페이스 유지
        public IEnumerable<Unit> GetAllUnits()
        {
            for (int i = 0; i < _maxUnits; i++)
            {
                if (_units[i].unitType != 0)
                    yield return _units[i];
            }
        }

        public IEnumerable<Unit> GetPlayerUnits(byte playerId)
        {
            for (int i = 0; i < _maxUnits; i++)
            {
                if (_units[i].unitType != 0 && _units[i].playerIndex == playerId)
                    yield return _units[i];
            }
        }

        public IEnumerable<Unit> GetUnitsByType(ushort unitType)
        {
            for (int i = 0; i < _maxUnits; i++)
            {
                if (_units[i].unitType == unitType)
                    yield return _units[i];
            }
        }

        public IEnumerable<Unit> GetUnitsNearPosition(ushort x, ushort y, int radius)
        {
            int radiusSquared = radius * radius;

            for (int i = 0; i < _maxUnits; i++)
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
        public bool RefreshUnits() => LoadAllUnits();

        public void Dispose()
        {
            if (_disposed)
                return;
            _disposed = true;
        }
    }
}