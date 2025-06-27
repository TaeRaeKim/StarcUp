using StarcUp.Business.Units.Runtime.Models;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using Timer = System.Threading.Timer;

namespace StarcUp.Business.Units.Runtime.Services
{
    /// <summary>
    /// 특정 플레이어의 유닛 정보를 100ms 주기로 업데이트하는 서비스
    /// </summary>
    public class UnitUpdateService : IDisposable
    {
        private readonly IUnitService _unitService;
        private readonly Timer _updateTimer;
        private readonly byte _playerId;
        private readonly Unit[] _currentPlayerUnits;
        private readonly int _maxUnits;
        private bool _disposed;
        private bool _isRunning;
        private int _currentUnitCount;

        public event EventHandler<UnitsUpdatedEventArgs> UnitsUpdated;

        public UnitUpdateService(IUnitService unitService, byte playerId = 0)
        {
            _unitService = unitService ?? throw new ArgumentNullException(nameof(unitService));
            _playerId = playerId;
            _maxUnits = 3400; // 최대 유닛 수

            // 미리 할당된 배열로 메모리 재활용 (모든 Unit 인스턴스 미리 생성)
            _currentPlayerUnits = new Unit[_maxUnits];
            for (int i = 0; i < _maxUnits; i++)
            {
                _currentPlayerUnits[i] = new Unit();
            }
            _currentUnitCount = 0;

            // 100ms 주기로 업데이트하는 타이머 생성
            _updateTimer = new Timer(UpdateUnits, null, Timeout.Infinite, Timeout.Infinite);

            Console.WriteLine($"UnitUpdateService 초기화 완료 (Player {playerId}, 최대 유닛 수: {_maxUnits})");
        }

        public void Start()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(UnitUpdateService));

            if (_isRunning)
            {
                Console.WriteLine($"[UnitUpdateService] Player {_playerId} 업데이트 서비스가 이미 실행 중입니다.");
                return;
            }

            _isRunning = true;

            // 100ms 주기로 타이머 시작
            _updateTimer.Change(0, 100);

        }

        public void Stop()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(UnitUpdateService));

            if (!_isRunning)
                return;

            _isRunning = false;
            _updateTimer.Change(Timeout.Infinite, Timeout.Infinite);

        }

        private void UpdateUnits(object state)
        {
            if (_disposed || !_isRunning)
                return;

            try
            {
                // 미리 할당된 배열에 유닛 데이터 직접 복사 (메모리 재활용)
                _currentUnitCount = _unitService.GetPlayerUnitsToBuffer(_playerId, _currentPlayerUnits, _maxUnits);

                // 이벤트 발생 - 필요한 부분만 복사하여 전달
                var currentUnits = new List<Unit>(_currentUnitCount);
                for (int i = 0; i < _currentUnitCount; i++)
                {
                    currentUnits.Add(_currentPlayerUnits[i]);
                }

                UnitsUpdated?.Invoke(this, new UnitsUpdatedEventArgs
                {
                    PlayerId = _playerId,
                    Units = currentUnits,
                    Timestamp = DateTime.Now
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[UnitUpdateService] Player {_playerId} 유닛 업데이트 중 오류: {ex.Message}");

                // 오류 발생 시 일반 방식으로 fallback
                try
                {
                    var fallbackUnits = _unitService.GetPlayerUnits(_playerId).ToList();
                    UnitsUpdated?.Invoke(this, new UnitsUpdatedEventArgs
                    {
                        PlayerId = _playerId,
                        Units = fallbackUnits,
                        Timestamp = DateTime.Now
                    });
                }
                catch (Exception fallbackEx)
                {
                    Console.WriteLine($"[UnitUpdateService] Player {_playerId} fallback 업데이트도 실패: {fallbackEx.Message}");
                }
            }
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            Stop();
            _updateTimer?.Dispose();
            _disposed = true;

        }
    }

    public class UnitsUpdatedEventArgs : EventArgs
    {
        public byte PlayerId { get; set; }
        public List<Unit> Units { get; set; }
        public DateTime Timestamp { get; set; }
    }
}