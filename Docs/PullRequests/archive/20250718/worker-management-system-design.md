# StarcUp 일꾼 관리 시스템 설계

## 📋 현재 상황 분석

### 현재 구현 상태
- **GameManager.cs:149-173줄**: 일꾼 개수 계산 로직이 임시로 구현됨
- **WorkerManager.cs**: 기본적인 워커 프리셋 상태 관리 + 개수 필드 추가
- **문제점**: 일꾼 개수 변화 시 이벤트 발생 없음, 체계적인 관리 부재

### 기존 아키텍처 분석
- **이벤트 시스템**: `Common/Events/` 폴더에 이벤트 아키텍처 존재
- **Unit 시스템**: `IUnitService`를 통한 유닛 관리 시스템
- **DI 컨테이너**: `ServiceRegistration.cs`를 통한 의존성 주입
- **로컬 플레이어 중심**: 현재 `LocalGameData.LocalPlayerIndex`만 관리

## 🎯 설계 목표

1. **단일 플레이어 중심 관리**: 로컬 플레이어의 일꾼만 관리 (성능 최적화)
2. **3가지 핵심 이벤트**: 총 개수 변경, 생산 완료, 일꾼 사망 이벤트
3. **지능적 이벤트 감지**: 생산 완료 vs 사망 구분하여 정확한 이벤트 발생
4. **기존 시스템 통합**: 현재 아키텍처와 자연스럽게 통합

## 🏗️ 설계 구조

### 1. 이벤트 시스템

#### WorkerEventArgs.cs
```csharp
using StarcUp.Business.Units.Types;

namespace StarcUp.Common.Events
{
    public class WorkerEventArgs : EventArgs
    {
        public int PlayerId { get; set; }
        public int TotalWorkers { get; set; }
        public int PreviousTotalWorkers { get; set; }
        public int CalculatedTotalWorkers { get; set; }      // 프리셋 적용된 총 일꾼 수
        public int PreviousCalculatedWorkers { get; set; }   // 이전 계산된 총 일꾼 수
        public int IdleWorkers { get; set; }
        public int PreviousIdleWorkers { get; set; }
        public int ProductionWorkers { get; set; }
        public int PreviousProductionWorkers { get; set; }
        public int ActiveWorkers { get; set; }
        public DateTime Timestamp { get; set; }
        public WorkerEventType EventType { get; set; }
    }

    public class GasBuildingEventArgs : EventArgs
    {
        public int PlayerId { get; set; }
        public int GasBuildingUnitId { get; set; }
        public ActionIndex ActionIndex { get; set; }
        public byte GatheringState { get; set; }
        public TimeSpan Duration { get; set; }              // 상태 지속 시간
        public DateTime Timestamp { get; set; }
    }

    public enum WorkerEventType
    {
        TotalCountChanged,    // 총 일꾼 개수 변경 (프리셋 고려)
        ProductionCompleted,  // 일꾼 생산 완료
        WorkerDied,          // 일꾼 사망
        IdleCountChanged,    // 유휴 일꾼 개수 변경
        GasBuildingAlert     // 가스 건물 채취 중단 알림
    }
}
```

#### 이벤트 발생 로직
```csharp
// 1. 총 일꾼 수 이벤트 (프리셋 고려)
if (IncludeProduction 프리셋 ON)
    계산된총개수 = 전체일꾼수
else
    계산된총개수 = 전체일꾼수 - 생산중일꾼수

if (계산된총개수 != 이전계산된총개수)
    -> TotalCountChanged 이벤트

// 2. 생산 완료 감지
if (총개수 == 이전총개수 && 생산Worker수 < 이전생산Worker수)
    -> ProductionCompleted 이벤트

// 3. 일꾼 사망 감지  
if (총개수 < 이전총개수 && !(생산Worker수 < 이전생산Worker수))
    -> WorkerDied 이벤트

// 4. 유휴 일꾼 개수 변경
if (유휴Worker수 != 이전유휴Worker수)
    -> IdleCountChanged 이벤트

// 5. 가스 건물 체크 (별도 타이머로 0.5초마다)
foreach (가스건물 in 플레이어가스건물들)
{
    if (가스건물.ActionIndex == 23 && 가스건물.GatheringState == 0 && 지속시간 >= 0.5초)
        -> GasBuildingAlert 이벤트
}
```

### 2. 일꾼 데이터 모델

#### WorkerStatistics.cs
```csharp
namespace StarcUp.Business.Profile.Models
{
    public class WorkerStatistics
    {
        public int TotalWorkers { get; set; }
        public int IdleWorkers { get; set; }
        public int ProductionWorkers { get; set; }
        public int ActiveWorkers { get; set; }
        public int CalculatedTotalWorkers { get; set; }  // 프리셋 적용된 총 개수
        public DateTime LastUpdated { get; set; }

        public bool HasChanged(WorkerStatistics other)
        {
            return TotalWorkers != other.TotalWorkers ||
                   IdleWorkers != other.IdleWorkers ||
                   ProductionWorkers != other.ProductionWorkers ||
                   CalculatedTotalWorkers != other.CalculatedTotalWorkers;
        }

        public bool HasTotalCountChanged(WorkerStatistics other)
        {
            return CalculatedTotalWorkers != other.CalculatedTotalWorkers;
        }

        public bool HasIdleCountChanged(WorkerStatistics other)
        {
            return IdleWorkers != other.IdleWorkers;
        }

        public bool IsProductionCompleted(WorkerStatistics other)
        {
            return TotalWorkers == other.TotalWorkers && 
                   ProductionWorkers < other.ProductionWorkers;
        }

        public bool IsWorkerDied(WorkerStatistics other)
        {
            return TotalWorkers < other.TotalWorkers && 
                   !(ProductionWorkers < other.ProductionWorkers);
        }
    }

    public class GasBuildingState
    {
        public int UnitId { get; set; }
        public ActionIndex ActionIndex { get; set; }
        public byte GatheringState { get; set; }
        public DateTime StateStartTime { get; set; }
        public DateTime LastChecked { get; set; }

        public TimeSpan StateDuration => DateTime.Now - StateStartTime;
        
        public bool ShouldTriggerAlert()
        {
            return ActionIndex == ActionIndex.GatheringGas && 
                   GatheringState == 0 && 
                   StateDuration >= TimeSpan.FromMilliseconds(500);
        }
    }
}
```

### 3. 일꾼 관리자 인터페이스

#### IWorkerManager.cs
```csharp
namespace StarcUp.Business.Profile
{
    public interface IWorkerManager : IDisposable
    {
        // 5가지 이벤트
        event EventHandler<WorkerEventArgs> TotalCountChanged;      // 총 일꾼 개수 변경
        event EventHandler<WorkerEventArgs> ProductionCompleted;    // 생산 완료
        event EventHandler<WorkerEventArgs> WorkerDied;            // 일꾼 사망
        event EventHandler<WorkerEventArgs> IdleCountChanged;      // 유휴 일꾼 개수 변경
        event EventHandler<GasBuildingEventArgs> GasBuildingAlert; // 가스 건물 알림

        // 속성
        WorkerPresetEnum WorkerPreset { get; set; }
        WorkerStatistics CurrentStatistics { get; }
        int LocalPlayerId { get; }
        
        // 메서드
        void Initialize(int localPlayerId);
        void UpdateWorkerData(IEnumerable<Unit> units);
        void UpdateGasBuildings(IEnumerable<Unit> gasBuildings);
        
        // 프리셋 관리
        void SetWorkerState(WorkerPresetEnum state);
        void ClearWorkerState(WorkerPresetEnum state);
        bool IsWorkerStateSet(WorkerPresetEnum state);
        
        // 통계 메서드
        double GetWorkerEfficiency();
        TimeSpan GetAverageIdleTime();
        int GetCalculatedTotalWorkers(); // 프리셋 적용된 총 개수
    }
}
```

### 4. 일꾼 관리자 구현

#### WorkerManager.cs (완전 재설계)
```csharp
using StarcUp.Business.Units.Types;
using StarcUp.Common.Events;

namespace StarcUp.Business.Profile
{
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
                _previousStats = new WorkerStatistics
                {
                    TotalWorkers = _currentStats.TotalWorkers,
                    IdleWorkers = _currentStats.IdleWorkers,
                    ProductionWorkers = _currentStats.ProductionWorkers,
                    ActiveWorkers = _currentStats.ActiveWorkers,
                    CalculatedTotalWorkers = _currentStats.CalculatedTotalWorkers,
                    LastUpdated = _currentStats.LastUpdated
                };

                _currentStats = newStats;

                // 각 이벤트 조건 확인 및 발생
                CheckAndRaiseEvents();
            }
        }

        public void UpdateGasBuildings(IEnumerable<Unit> gasBuildings)
        {
            if (gasBuildings == null) return;

            foreach (var building in gasBuildings.Where(b => b.IsBuilding))
            {
                var unitId = building.GetHashCode(); // 유닛 고유 ID 생성
                var actionIndex = (ActionIndex)building.ActionIndex;

                if (_gasBuildingStates.TryGetValue(unitId, out var existingState))
                {
                    // 상태 변경 확인
                    if (existingState.ActionIndex != actionIndex || 
                        existingState.GatheringState != building.GatheringState)
                    {
                        existingState.ActionIndex = actionIndex;
                        existingState.GatheringState = building.GatheringState;
                        existingState.StateStartTime = DateTime.Now;
                    }

                    existingState.LastChecked = DateTime.Now;

                    // 알림 조건 확인
                    if (existingState.ShouldTriggerAlert())
                    {
                        RaiseGasBuildingAlert(existingState);
                    }
                }
                else
                {
                    // 새로운 가스 건물 추가
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
```

### 5. GameManager 통합

#### GameManager.cs 수정사항
```csharp
public class GameManager : IGameManager, IDisposable
{
    private readonly IWorkerManager _workerManager;
    
    public GameManager(IInGameDetector inGameDetector, IUnitService unitService, 
                      IMemoryService memoryService, IUnitCountService unitCountService, 
                      IWorkerManager workerManager)
    {
        // 기존 코드...
        _workerManager = workerManager ?? throw new ArgumentNullException(nameof(workerManager));
        
        // 이벤트 구독
        _workerManager.WorkerCountChanged += OnWorkerCountChanged;
        _workerManager.WorkerStateChanged += OnWorkerStateChanged;
    }

    private void LoadUnitsData()
    {
        try
        {
            _unitService.RefreshUnits();
            var units = _unitService.GetPlayerUnits(LocalGameData.LocalPlayerIndex);
            
            // WorkerManager에 데이터 전달
            _workerManager.UpdateWorkerData(units);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"GameManager: 유닛 데이터 로드 중 오류 발생 - {ex.Message}");
        }
    }

    private void OnWorkerCountChanged(object sender, WorkerEventArgs e)
    {
        Console.WriteLine($"GameManager: 일꾼 개수 변화 - 이전: {e.PreviousWorkers}, 현재: {e.TotalWorkers}");
    }

    private void OnWorkerStateChanged(object sender, WorkerEventArgs e)
    {
        Console.WriteLine($"GameManager: 일꾼 상태 변화 - 유휴: {e.IdleWorkers}, 생산: {e.ProductionWorkers}");
    }
}
```

### 6. 서비스 등록

#### ServiceRegistration.cs 수정
```csharp
public static class ServiceRegistration
{
    public static ServiceContainer RegisterServices(ServiceContainer container)
    {
        // 기존 서비스들...
        container.RegisterSingleton<IWorkerManager, WorkerManager>();
        
        return container;
    }
}
```

## 📊 성능 최적화

### 1. 데이터 변화 감지
- `WorkerStatistics.HasChanged()` 메서드로 불필요한 이벤트 발생 방지
- 락을 사용한 스레드 안전성 보장

### 2. 메모리 관리
- 딕셔너리 기반 플레이어별 데이터 관리
- IDisposable 패턴으로 적절한 리소스 정리

### 3. 이벤트 최적화
- 개수 변화와 상태 변화를 분리하여 필요한 이벤트만 발생
- 이벤트 핸들러의 null 체크로 안전성 확보

## 🚀 확장 가능성

### 1. 추가 가능한 기능들
- 일꾼 생산 효율성 분석
- 일꾼 유휴 시간 추적
- 일꾼 작업 패턴 분석
- 자동 일꾼 관리 기능

### 2. 다른 유닛 타입으로의 확장
- 같은 패턴을 사용하여 전투 유닛, 건물 등 관리 가능
- 범용적인 `UnitManager<T>` 제네릭 클래스로 발전 가능

## 🔧 구현 순서

1. **이벤트 클래스 생성**: `WorkerEventArgs.cs` 작성
2. **데이터 모델 생성**: `WorkerStatistics.cs` 작성
3. **인터페이스 정의**: `IWorkerManager.cs` 작성
4. **구현 클래스 작성**: `WorkerManager.cs` 리팩토링
5. **GameManager 통합**: 기존 코드 수정
6. **서비스 등록**: DI 컨테이너에 등록
7. **테스트 작성**: 단위 테스트 및 통합 테스트

이 설계는 현재 StarcUp의 아키텍처와 자연스럽게 통합되며, 확장 가능하고 성능 최적화된 일꾼 관리 시스템을 제공합니다.