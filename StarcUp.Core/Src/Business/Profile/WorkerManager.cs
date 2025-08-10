using System;
using System.Collections.Generic;
using System.Linq;
using StarcUp.Business.Profile.Models;
using StarcUp.Business.Units.Runtime.Models;
using StarcUp.Business.Units.Types;
using StarcUp.Common.Events;
using StarcUp.Common.Logging;

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
        
        private List<Unit> _cachedWorkers;

        public event EventHandler<WorkerEventArgs> WorkerStatusChanged;
        public event EventHandler<WorkerEventArgs> ProductionStarted;
        public event EventHandler<WorkerEventArgs> ProductionCompleted;
        public event EventHandler<WorkerEventArgs> ProductionCanceled;
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

            var newStats = CalculateWorkerStatisticsFromUnits(units);

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
                    LoggerHelper.Debug($"새로운 가스 건물 등록: UnitId {unitId}, 위치: ({building.CurrentX}, {building.CurrentY})");
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

        private WorkerStatistics CalculateWorkerStatisticsFromUnits(IEnumerable<Unit> units)
        {
            var workers = units.Where(u => u.IsWorker).ToList();
            
            // worker 데이터 캐시 (프리셋 변경 시 재계산용)
            _cachedWorkers = workers;
            
            return CalculateWorkerStatistics(workers);
        }

        private WorkerStatistics CalculateWorkerStatistics(List<Unit> workers)
        {
            var totalWorkers = workers.Count;
            var idleWorkers = workers.Count(w => ((ActionIndex)w.ActionIndex).IsIdle());
            var productionWorkers = workers.Count(w => ((ActionIndex)w.ActionIndex).IsInProduction());

            // 프리셋 적용하여 계산된 총 개수 구하기
            var calculatedTotal = IsWorkerStateSet(WorkerPresetEnum.IncludeProduction)
                ? totalWorkers
                : totalWorkers - productionWorkers;

            return new WorkerStatistics
            {
                TotalWorkers = totalWorkers,
                IdleWorkers = idleWorkers,
                ProductionWorkers = productionWorkers,
                CalculatedTotalWorkers = calculatedTotal,
                LastUpdated = DateTime.Now
            };
        }

        private void CheckAndRaiseEvents()
        {
            var current = _currentStats;
            var previous = _previousStats;

            // 1. 생산 완료 감지
            if (current.IsProductionCompleted(previous))
            {
                // 프리셋에 따라 이벤트 타입 결정
                var eventType = IsWorkerStateSet(WorkerPresetEnum.DetectProduction)
                    ? WorkerEventType.ProductionCompleted
                    : WorkerEventType.WorkerStatusChanged;

                RaiseWorkerEvent(eventType, current, previous);
            }
            // 2. 생산 취소 감지
            else if (current.IsProductionCanceled(previous))
            {
                RaiseWorkerEvent(WorkerEventType.ProductionCanceled, current, previous);
            }
            // 3. 첫 생산 시작 감지 (예약생성은 불리지 않음)
            else if (current.IsProductionStarted(previous))
            {
                RaiseWorkerEvent(WorkerEventType.ProductionStarted, current, previous);
            }
            // 4. 단순 증가 감지 (게임시작, 마인드컨트롤?)
            else if (current.IsSimpleIncrease(previous))
            {
                RaiseWorkerEvent(WorkerEventType.WorkerStatusChanged, current, previous);
            }
            // 5. 일꾼 사망 감지
            else if (current.IsWorkerDied(previous))
            {
                // 프리셋에 따라 이벤트 타입 결정
                var eventType = IsWorkerStateSet(WorkerPresetEnum.DetectDeath)
                    ? WorkerEventType.WorkerDied
                    : WorkerEventType.WorkerStatusChanged;

                RaiseWorkerEvent(eventType, current, previous);
            }

            // 6. 유휴 일꾼 개수 변경 (기존 로직 유지)
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
                Timestamp = DateTime.Now,
                EventType = eventType
            };

            switch (eventType)
            {
                case WorkerEventType.WorkerStatusChanged:
                    WorkerStatusChanged?.Invoke(this, eventArgs);
                    break;
                case WorkerEventType.ProductionStarted:
                    ProductionStarted?.Invoke(this, eventArgs);
                    break;
                case WorkerEventType.ProductionCompleted:
                    ProductionCompleted?.Invoke(this, eventArgs);
                    break;
                case WorkerEventType.ProductionCanceled:
                    ProductionCanceled?.Invoke(this, eventArgs);
                    break;
                case WorkerEventType.WorkerDied:
                    WorkerDied?.Invoke(this, eventArgs);
                    break;
                case WorkerEventType.IdleCountChanged:
                    if (IsWorkerStateSet(WorkerPresetEnum.Idle))
                    {
                        IdleCountChanged?.Invoke(this, eventArgs);
                    }
                    break;
            }
        }

        private void RaiseGasBuildingAlert(GasBuildingState state)
        {
            if (!IsWorkerStateSet(WorkerPresetEnum.CheckGas))
            {
                return;
            }

            var eventArgs = new GasBuildingEventArgs
            {
                PlayerId = LocalPlayerId,
                GasBuildingUnitId = state.UnitId,
                ActionIndex = state.ActionIndex,
                GatheringState = state.GatheringState,
                Duration = state.StateDuration,
                Timestamp = DateTime.Now
            };

            LoggerHelper.Warning($"가스 건물 채취 중단 알림! - UnitId: {state.UnitId}, 지속시간: {state.StateDuration.TotalMilliseconds:F0}ms");
            GasBuildingAlert?.Invoke(this, eventArgs);
        }

        public bool IsWorkerStateSet(WorkerPresetEnum state)
        {
            return (WorkerPreset & state) == state;
        }

        public void InitializeWorkerPreset(WorkerPresetEnum preset)
        {
            LoggerHelper.Info($"일꾼 프리셋 초기화: {preset}");
            WorkerPreset = preset;
            OnPresetChanged();
        }

        public WorkerPresetEnum UpdateWorkerPreset(WorkerPresetEnum newPreset)
        {
            var previousPreset = WorkerPreset;
            LoggerHelper.Info($"일꾼 프리셋 업데이트: {previousPreset} → {newPreset}");
            WorkerPreset = newPreset;
            OnPresetChanged();
            return previousPreset;
        }
        
        private void OnPresetChanged()
        {
            lock (_lock)
            {
                if (_cachedWorkers == null || _cachedWorkers.Count == 0) return;

                _previousStats = _currentStats.Clone();
                _currentStats = CalculateWorkerStatistics(_cachedWorkers);

                RaiseWorkerEvent(WorkerEventType.WorkerStatusChanged, _currentStats, _previousStats);
            }
        }

        public void Dispose()
        {
            WorkerStatusChanged = null;
            ProductionStarted = null;
            ProductionCompleted = null;
            ProductionCanceled = null;
            WorkerDied = null;
            IdleCountChanged = null;
            GasBuildingAlert = null;
            _gasBuildingStates?.Clear();
        }
    }
}