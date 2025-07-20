# WorkerManager 이벤트 전송 시스템 설계 문서

## 📋 개요

WorkerManager에서 발생하는 일꾼 관련 이벤트를 StarcUp.UI로 실시간 전송하는 시스템을 설계합니다.
Core의 NamedPipeProtocol을 활용하여 Event 타입 메시지로 전송하며, UI-Core 간 이벤트 명세를 동기화합니다.

## 🎯 목표

1. **실시간 일꾼 상태 모니터링**: UI에서 일꾼 개수, 유휴 상태, 생산 상태 실시간 확인
2. **가스 건물 알림**: 가스 채취 중단 시 즉시 알림
3. **프리셋 기반 필터링**: 사용자 설정에 따른 선택적 이벤트 수신
4. **성능 최적화**: 불필요한 이벤트 전송 최소화

## 🏗️ 아키텍처 설계

### 이벤트 흐름
```
WorkerManager → CommunicationService → NamedPipe → UI EventHandler
```

### 메시지 타입
- **MessageType**: `Event` (타입 2)
- **프로토콜**: Core `NamedPipeProtocol.EventMessage` 사용

## 📡 이벤트 명세 정의

### 1. 일꾼 상태 변경 이벤트

#### 이벤트명: `worker-status-changed`

```json
{
  "id": "msg_1642435200000_abc12345",
  "type": 2,
  "timestamp": 1642435200000,
  "event": "worker-status-changed",
  "data": {
    "totalWorkers": 45,
    "calculatedTotal": 42,
    "idleWorkers": 3,
    "productionWorkers": 2,
    "activeWorkers": 40
  }
}
```

### 2. 가스 건물 알림 이벤트

#### 이벤트명: `gas-building-alert`

```json
{
  "id": "msg_1642435201000_def67890",
  "type": 2,
  "timestamp": 1642435201000,
  "event": "gas-building-alert",
  "data": {}
}
```

### 3. 일꾼 프리셋 변경 이벤트

#### 이벤트명: `worker-preset-changed`

```json
{
  "id": "msg_1642435202000_ghi34567",
  "type": 2,
  "timestamp": 1642435202000,
  "event": "worker-preset-changed",
  "data": {
    "success": true,
    "previousPreset": {
      "mask": 15,
      "flags": ["Default", "IncludeProduction", "Idle", "DetectProduction"]
    },
    "currentPreset": {
      "mask": 47,
      "flags": ["Default", "IncludeProduction", "Idle", "DetectProduction", "DetectDeath", "CheckGas"]
    }
  }
}
```

## 💻 Core 구현 사항

### 1. CommunicationService 확장

```csharp
// WorkerManager 이벤트 구독 (StartAsync에 추가)
_workerManager.TotalCountChanged += OnWorkerTotalCountChanged;
_workerManager.ProductionCompleted += OnWorkerProductionCompleted;
_workerManager.WorkerDied += OnWorkerDied;
_workerManager.IdleCountChanged += OnWorkerIdleCountChanged;
_workerManager.GasBuildingAlert += OnGasBuildingAlert;

// 프리셋 변경 이벤트도 구독 (WorkerManager에 추가 필요)
_workerManager.PresetChanged += OnWorkerPresetChanged;
```

### 2. NamedPipeProtocol 이벤트 확장

```csharp
public static class Events
{
    // 기존 이벤트들
    public const string GameDetected = "game-detected";
    public const string GameEnded = "game-ended";
    public const string GameStatusChanged = "game-status-changed";
    
    // WorkerManager 이벤트들 추가
    public const string WorkerStatusChanged = "worker-status-changed";
    public const string GasBuildingAlert = "gas-building-alert";
    public const string WorkerPresetChanged = "worker-preset-changed";
}
```

## 🎨 UI 구현 사항

### 1. TypeScript 타입 정의

```typescript
// WorkerManager 이벤트 타입 정의
export interface WorkerStatusChangedEvent {
  playerId: number;
  eventType: WorkerEventType;
  timestamp: string;
  current: WorkerStats;
  previous: WorkerStats;
  preset: WorkerPresetFlags;
}

export interface GasBuildingAlertEvent {
  playerId: number;
  buildingId: number;
  duration: number;
  timestamp: string;
  alert: {
    type: 'gathering-stopped';
    severity: 'warning' | 'error';
  };
}

export interface WorkerPresetChangedEvent {
  playerId: number;
  previousPreset: PresetInfo;
  currentPreset: PresetInfo;
  timestamp: string;
}

export type WorkerEventType = 
  | 'TotalCountChanged'
  | 'ProductionCompleted' 
  | 'WorkerDied'
  | 'IdleCountChanged';

export interface WorkerStats {
  totalWorkers: number;
  calculatedTotal: number;
  idleWorkers: number;
  productionWorkers: number;
  activeWorkers: number;
}

export interface WorkerPresetFlags {
  includeProduction: boolean;
  detectIdle: boolean;
  detectProduction: boolean;
  detectDeath: boolean;
  checkGas: boolean;
}

export interface PresetInfo {
  mask: number;
  flags: string[];
}
```

### 2. NamedPipeProtocol 이벤트 확장

```typescript
export const Events = {
  // 기존 이벤트들
  GameDetected: 'game-detected',
  GameEnded: 'game-ended',
  GameStatusChanged: 'game-status-changed',
  
  // WorkerManager 이벤트들 추가
  WorkerStatusChanged: 'worker-status-changed',
  GasBuildingAlert: 'gas-building-alert',
  WorkerPresetChanged: 'worker-preset-changed'
} as const
```

### 3. CoreCommunicationService 이벤트 핸들러

```typescript
// WorkerManager 이벤트 핸들러 추가
this.namedPipeService.onEvent('worker-status-changed', (data: WorkerStatusChangedEvent) => {
  console.log(`👷 일꾼 상태 변경: ${data.eventType}`, data);
  if (this.workerStatusChangedCallback) {
    this.workerStatusChangedCallback(data);
  }
});

this.namedPipeService.onEvent('gas-building-alert', (data: GasBuildingAlertEvent) => {
  console.log(`⛽ 가스 건물 알림: ${data.duration}ms 채취 중단`, data);
  if (this.gasBuildingAlertCallback) {
    this.gasBuildingAlertCallback(data);
  }
});

this.namedPipeService.onEvent('worker-preset-changed', (data: WorkerPresetChangedEvent) => {
  console.log(`⚙️ 일꾼 프리셋 변경:`, data.currentPreset.flags);
  if (this.workerPresetChangedCallback) {
    this.workerPresetChangedCallback(data);
  }
});
```

## 🎛️ 이벤트 필터링 및 성능 최적화

### 1. 프리셋 기반 필터링

```csharp
// Core에서 프리셋에 따른 이벤트 전송 여부 결정
private void OnWorkerTotalCountChanged(object sender, WorkerEventArgs e)
{
    // Default 프리셋이 설정된 경우에만 전송
    if (_workerManager.IsWorkerStateSet(WorkerPresetEnum.Default))
    {
        SendWorkerStatusEvent(e, "TotalCountChanged");
    }
}
```

### 2. 배치 처리 (선택사항)

```csharp
// 짧은 시간 내 여러 이벤트 발생 시 배치로 전송
private readonly Timer _batchTimer;
private readonly List<WorkerEventArgs> _pendingEvents = new();

private void StartBatchProcessing()
{
    _batchTimer = new Timer(ProcessPendingEvents, null, 100, 100); // 100ms 간격
}
```

## 🔧 구현 우선순위

### Phase 1: 핵심 이벤트 (높은 우선순위)
1. `worker-status-changed` (TotalCountChanged, IdleCountChanged)
2. `gas-building-alert`

### Phase 2: 확장 이벤트 (중간 우선순위)
1. `worker-status-changed` (ProductionCompleted, WorkerDied)
2. `worker-preset-changed`

### Phase 3: 최적화 (낮은 우선순위)
1. 이벤트 배치 처리
2. UI 필터링 옵션
3. 성능 모니터링

## 🧪 테스트 시나리오

### 1. 기본 이벤트 테스트
- 일꾼 생산 시 TotalCountChanged 이벤트 발생 확인
- 일꾼 유휴 상태 변경 시 IdleCountChanged 이벤트 발생 확인

### 2. 프리셋 연동 테스트
- 프리셋 비활성화 시 해당 이벤트 미전송 확인
- 프리셋 변경 시 실시간 이벤트 활성화/비활성화 확인

### 3. 성능 테스트
- 대량 일꾼 상태 변경 시 UI 반응성 확인
- 메모리 사용량 및 이벤트 처리 지연시간 측정

## 📝 구현 후 문서 업데이트

1. **README 업데이트**: WorkerManager 이벤트 시스템 사용법 추가
2. **API 문서**: 새로운 이벤트 타입 및 데이터 구조 문서화
3. **예제 코드**: UI에서 WorkerManager 이벤트 구독 및 처리 예제

---

이 설계를 바탕으로 단계별로 구현하여 실시간 일꾼 관리 시스템을 완성할 수 있습니다.