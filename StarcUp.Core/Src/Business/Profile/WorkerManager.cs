using System;
using System.Collections.Generic;
using System.Linq;
using StarcUp.Business.Profile.Models;
using StarcUp.Business.Units.Runtime.Models;
using StarcUp.Business.Units.Types;
using StarcUp.Common.Events;

namespace StarcUp.Business.Profile
{
    // 1. Flags Enum을 사용한 상태 관리
    [Flags]
    public enum WorkerPresetEnum : byte
    {
        None = 0b0000_0000,        // 0
        Default = 0b0000_0001,        // 1
        IncludeProduction = 0b0000_0010,  // 2
        Idle = 0b0000_0100,       // 4
        DetectProduction = 0b0000_1000,     // 8
        DetectDeath = 0b0001_0000,      // 16
        CheckGas = 0b0010_0000,       // 32
    }

    public class WorkerManager : IWorkerManager
    {
        private WorkerStatistics _currentStats;
        private WorkerStatistics _previousStats;
        private readonly Dictionary<int, GasBuildingState> _gasBuildingStates;
        private readonly object _lock = new object();

        // 5가지 이벤트
        public event EventHandler<WorkerEventArgs> TotalCountChanged;
        public event EventHandler<WorkerEventArgs> ProductionCompleted;
        public event EventHandler<WorkerEventArgs> WorkerDied;
        public event EventHandler<WorkerEventArgs> IdleCountChanged;
        public event EventHandler<GasBuildingEventArgs> GasBuildingAlert;

        public WorkerPresetEnum WorkerPreset { get; set; }
        public WorkerStatistics CurrentStatistics => _currentStats;
        public int LocalPlayerId { get; private set; }

        public WorkerManager()
        {
            _currentStats = new WorkerStatistics();
            _previousStats = new WorkerStatistics();
            _gasBuildingStates = new Dictionary<int, GasBuildingState>();
        }

        public void Initialize(int localPlayerId)
        {
            LocalPlayerId = localPlayerId;
            _currentStats = new WorkerStatistics();
            _previousStats = new WorkerStatistics();
            _gasBuildingStates.Clear();
        }

        public void UpdateWorkerData(IEnumerable<Unit> units)
        {
            if (units == null) return;

            var newStats = CalculateWorkerStatistics(units);

            lock (_lock)
            {
                _previousStats = _currentStats.Clone();
                _currentStats = newStats;

                // 각 이벤트 조건 확인 및 발생
                CheckAndRaiseEvents();
            }
        }

        public void UpdateGasBuildings(IEnumerable<Unit> gasBuildings)
        {
            if (gasBuildings == null) return;

            var gasBuildingList = gasBuildings.ToList();
            
            foreach (var building in gasBuildingList.Where(b => b.IsBuilding))
            {
                // 위치 기반 고유 식별자 사용 (건물은 이동하지 않음) - 개선 여부 O
                var unitId = HashCode.Combine(building.UnitType, building.CurrentX, building.CurrentY, building.PlayerIndex);
                var actionIndex = (ActionIndex)building.ActionIndex;

                if (_gasBuildingStates.TryGetValue(unitId, out var existingState))
                {
                    existingState.UpdateState(actionIndex, building.GatheringState);

                    if (existingState.ShouldTriggerAlert())
                    {
                        RaiseGasBuildingAlert(existingState);
                        // 알림 발생 후 타이머 초기화하여 중복 알림 방지
                        existingState.StateStartTime = DateTime.Now;
                    }
                }
                else
                {
                    Console.WriteLine($"새로운 가스 건물 등록: UnitId {unitId}, 위치: ({building.CurrentX}, {building.CurrentY})");
                    _gasBuildingStates[unitId] = new GasBuildingState
                    {
                        UnitId = unitId,
                        ActionIndex = actionIndex,
                        GatheringState = building.GatheringState,
                        StateStartTime = DateTime.Now,
                        LastChecked = DateTime.Now
                    };
                }
            }
        }

        private WorkerStatistics CalculateWorkerStatistics(IEnumerable<Unit> units)
        {
            var workers = units.Where(u => u.IsWorker).ToList();
            
            var totalWorkers = workers.Count;
            var idleWorkers = workers.Count(w => ((ActionIndex)w.ActionIndex).IsIdle());
            var productionWorkers = workers.Count(w => w.ActionIndex == 23); // 생산 중
            var activeWorkers = workers.Count(w => !((ActionIndex)w.ActionIndex).IsIdle());

            // 프리셋 적용하여 계산된 총 개수 구하기
            var calculatedTotal = IsWorkerStateSet(WorkerPresetEnum.IncludeProduction)
                ? totalWorkers
                : totalWorkers - productionWorkers;

            return new WorkerStatistics
            {
                TotalWorkers = totalWorkers,
                IdleWorkers = idleWorkers,
                ProductionWorkers = productionWorkers,
                ActiveWorkers = activeWorkers,
                CalculatedTotalWorkers = calculatedTotal,
                LastUpdated = DateTime.Now
            };
        }

        private void CheckAndRaiseEvents()
        {
            var current = _currentStats;
            var previous = _previousStats;

            // 1. 총 일꾼 개수 변경 (프리셋 고려)
            if (current.HasTotalCountChanged(previous))
            {
                RaiseWorkerEvent(WorkerEventType.TotalCountChanged, current, previous);
            }

            // 2. 생산 완료
            if (current.IsProductionCompleted(previous))
            {
                RaiseWorkerEvent(WorkerEventType.ProductionCompleted, current, previous);
            }

            // 3. 일꾼 사망
            if (current.IsWorkerDied(previous))
            {
                RaiseWorkerEvent(WorkerEventType.WorkerDied, current, previous);
            }

            // 4. 유휴 일꾼 개수 변경
            if (current.HasIdleCountChanged(previous))
            {
                RaiseWorkerEvent(WorkerEventType.IdleCountChanged, current, previous);
            }
        }

        private void RaiseWorkerEvent(WorkerEventType eventType, WorkerStatistics current, WorkerStatistics previous)
        {
            var eventArgs = new WorkerEventArgs
            {
                PlayerId = LocalPlayerId,
                TotalWorkers = current.TotalWorkers,
                PreviousTotalWorkers = previous.TotalWorkers,
                CalculatedTotalWorkers = current.CalculatedTotalWorkers,
                PreviousCalculatedWorkers = previous.CalculatedTotalWorkers,
                IdleWorkers = current.IdleWorkers,
                PreviousIdleWorkers = previous.IdleWorkers,
                ProductionWorkers = current.ProductionWorkers,
                PreviousProductionWorkers = previous.ProductionWorkers,
                ActiveWorkers = current.ActiveWorkers,
                Timestamp = DateTime.Now,
                EventType = eventType
            };

            switch (eventType)
            {
                case WorkerEventType.TotalCountChanged:
                    TotalCountChanged?.Invoke(this, eventArgs);
                    break;
                case WorkerEventType.ProductionCompleted:
                    ProductionCompleted?.Invoke(this, eventArgs);
                    break;
                case WorkerEventType.WorkerDied:
                    WorkerDied?.Invoke(this, eventArgs);
                    break;
                case WorkerEventType.IdleCountChanged:
                    IdleCountChanged?.Invoke(this, eventArgs);
                    break;
            }
        }

        private void RaiseGasBuildingAlert(GasBuildingState state)
        {
            var eventArgs = new GasBuildingEventArgs
            {
                PlayerId = LocalPlayerId,
                GasBuildingUnitId = state.UnitId,
                ActionIndex = state.ActionIndex,
                GatheringState = state.GatheringState,
                Duration = state.StateDuration,
                Timestamp = DateTime.Now
            };

            Console.WriteLine($"🚨 가스 건물 채취 중단 알림! - UnitId: {state.UnitId}, 지속시간: {state.StateDuration.TotalMilliseconds:F0}ms");
            GasBuildingAlert?.Invoke(this, eventArgs);
        }

        // 프리셋 관리
        public void SetWorkerState(WorkerPresetEnum state)
        {
            WorkerPreset |= state;
        }

        public void ClearWorkerState(WorkerPresetEnum state)
        {
            WorkerPreset &= ~state;
        }

        public bool IsWorkerStateSet(WorkerPresetEnum state)
        {
            return (WorkerPreset & state) == state;
        }

        // 통계 메서드
        public double GetWorkerEfficiency()
        {
            return _currentStats.TotalWorkers > 0 
                ? (double)_currentStats.ActiveWorkers / _currentStats.TotalWorkers 
                : 0;
        }

        public TimeSpan GetAverageIdleTime()
        {
            // 향후 구현 예정
            return TimeSpan.Zero;
        }

        public int GetCalculatedTotalWorkers()
        {
            return _currentStats.CalculatedTotalWorkers;
        }

        public void Dispose()
        {
            TotalCountChanged = null;
            ProductionCompleted = null;
            WorkerDied = null;
            IdleCountChanged = null;
            GasBuildingAlert = null;
            _gasBuildingStates?.Clear();
        }
    }
}