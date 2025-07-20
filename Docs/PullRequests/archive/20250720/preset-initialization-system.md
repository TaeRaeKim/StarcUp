# 프리셋 초기화 시스템 설계

## 📋 개요

Named Pipe 연결 후 UI에서 Core로 프리셋 정보를 전송하는 시스템을 설계합니다.
일꾼 상세 설정을 비트마스크로 압축하여 효율적으로 전송하고, 향후 다른 기능 프리셋도 확장 가능한 구조로 설계합니다.

## 🎯 목표

1. **프리셋 초기화**: Named Pipe 연결 후 현재 설정된 프리셋을 Core로 전송
2. **비트마스크 압축**: 일꾼 설정 6개 항목을 8bit로 압축하여 전송
3. **확장 가능한 구조**: 향후 다른 기능 프리셋도 추가할 수 있는 메시지 구조
4. **실시간 동기화**: 설정 변경 시 즉시 Core로 전달

## 📊 일꾼 프리셋 비트마스크 설계

### 기존 WorkerPresetEnum 활용
Core에 이미 정의된 `WorkerPresetEnum`을 활용하여 UI 설정과 매핑합니다:

```csharp
[Flags]
public enum WorkerPresetEnum : byte
{
    None = 0b0000_0000,               // 0
    Default = 0b0000_0001,            // 1   - 일꾼 수 출력
    IncludeProduction = 0b0000_0010,  // 2   - 생산 중인 일꾼 수 포함
    Idle = 0b0000_0100,               // 4   - 유휴 일꾼 수 출력
    DetectProduction = 0b0000_1000,   // 8   - 일꾼 생산 감지
    DetectDeath = 0b0001_0000,        // 16  - 일꾼 사망 감지
    CheckGas = 0b0010_0000,           // 32  - 가스 일꾼 체크
}
```

### UI 설정과 WorkerPresetEnum 매핑
```
UI 설정 항목                     → WorkerPresetEnum
일꾼 수 출력                    → Default (1)
생산 중인 일꾼 수 포함           → IncludeProduction (2)
유휴 일꾼 수 출력               → Idle (4)
일꾼 생산 감지                  → DetectProduction (8)
일꾼 사망 감지                  → DetectDeath (16)
가스 일꾼 체크                  → CheckGas (32)
```

### 예시
```
설정 상태:
✅ 일꾼 수 출력          (Default = 1)
❌ 생산 중인 일꾼 수 포함  (IncludeProduction = 2)
✅ 유휴 일꾼 수 출력      (Idle = 4)
✅ 일꾼 생산 감지        (DetectProduction = 8)
✅ 일꾼 사망 감지        (DetectDeath = 16)
✅ 가스 일꾼 체크        (CheckGas = 32)

비트마스크: 1 | 4 | 8 | 16 | 32 = 61 (0b0011_1101)
```

## 🏗️ 메시지 구조 설계

### 1. preset-init 이벤트 구조

```typescript
interface PresetInitMessage {
  type: 'preset-init';
  timestamp: number;
  presets: {
    worker?: WorkerPreset;
    population?: PopulationPreset;  // 향후 확장
    unit?: UnitPreset;              // 향후 확장
    upgrade?: UpgradePreset;        // 향후 확장
    buildOrder?: BuildOrderPreset;  // 향후 확장
  };
}

interface WorkerPreset {
  enabled: boolean;           // 일꾼 기능 전체 활성화 여부
  settingsMask: number;       // 8bit 비트마스크 (0-255)
}
```

### 2. preset-update 이벤트 구조 (설정 변경 시)

```typescript
interface PresetUpdateMessage {
  type: 'preset-update';
  timestamp: number;
  presetType: 'worker' | 'population' | 'unit' | 'upgrade' | 'buildOrder';
  data: WorkerPreset | PopulationPreset | UnitPreset | UpgradePreset | BuildOrderPreset;
}
```

## 🔄 데이터 흐름

### 초기화 시퀀스
1. **Named Pipe 연결 성공**
2. **UI → Core**: `preset-init` 메시지 전송
   - 현재 활성화된 모든 프리셋 정보 포함
   - 일꾼 설정은 비트마스크로 압축하여 전송
3. **Core**: 프리셋 정보 수신 및 적용

### 설정 변경 시퀀스
1. **사용자**: 일꾼 상세 설정에서 "설정 완료" 버튼 클릭
2. **UI**: 비트마스크 계산
3. **UI → Core**: `preset-update` 메시지 전송
4. **Core**: 새로운 설정 적용

## 💻 구현 계획

### Phase 1: 비트마스크 유틸리티 함수
```typescript
// UI/src/utils/presetUtils.ts

// WorkerPresetEnum과 동일한 값들을 사용
export const WorkerPresetFlags = {
  None: 0,                    // 0b0000_0000
  Default: 1,                 // 0b0000_0001 - 일꾼 수 출력
  IncludeProduction: 2,       // 0b0000_0010 - 생산 중인 일꾼 수 포함
  Idle: 4,                    // 0b0000_0100 - 유휴 일꾼 수 출력
  DetectProduction: 8,        // 0b0000_1000 - 일꾼 생산 감지
  DetectDeath: 16,            // 0b0001_0000 - 일꾼 사망 감지
  CheckGas: 32,               // 0b0010_0000 - 가스 일꾼 체크
} as const;

export function calculateWorkerSettingsMask(settings: WorkerSettings): number {
  let mask = 0;
  if (settings.workerCountDisplay) mask |= WorkerPresetFlags.Default;
  if (settings.includeProducingWorkers) mask |= WorkerPresetFlags.IncludeProduction;
  if (settings.idleWorkerDisplay) mask |= WorkerPresetFlags.Idle;
  if (settings.workerProductionDetection) mask |= WorkerPresetFlags.DetectProduction;
  if (settings.workerDeathDetection) mask |= WorkerPresetFlags.DetectDeath;
  if (settings.gasWorkerCheck) mask |= WorkerPresetFlags.CheckGas;
  return mask;
}
```

### Phase 2: Named Pipe 연결 후 초기화
```typescript
// App.tsx에서 Named Pipe 연결 성공 시
const sendPresetInit = async () => {
  const workerMask = calculateWorkerSettingsMask(currentWorkerSettings);
  const message: PresetInitMessage = {
    type: 'preset-init',
    timestamp: Date.now(),
    presets: {
      worker: {
        enabled: currentPreset.featureStates[0], // 일꾼 기능 활성화 여부
        settingsMask: workerMask
      }
    }
  };
  
  await window.coreAPI?.sendPresetInit(message);
};
```

### Phase 3: 설정 변경 시 실시간 전송
```typescript
// WorkerDetailSettings.tsx의 handleSave에서
const handleSave = async () => {
  const workerMask = calculateWorkerSettingsMask({
    workerCountDisplay,
    includeProducingWorkers,
    idleWorkerDisplay,
    workerProductionDetection,
    workerDeathDetection,
    gasWorkerCheck
  });
  
  const message: PresetUpdateMessage = {
    type: 'preset-update',
    timestamp: Date.now(),
    presetType: 'worker',
    data: {
      enabled: true,
      settingsMask: workerMask
    }
  };
  
  await window.coreAPI?.sendPresetUpdate(message);
  onClose();
};
```

## 🔧 Core 측 구현 계획

### 메시지 처리
```csharp
// Named Pipe 메시지 핸들러에서
public void HandlePresetInit(PresetInitMessage message)
{
    if (message.Presets.Worker != null)
    {
        var workerPreset = (WorkerPresetEnum)message.Presets.Worker.SettingsMask;
        _workerManager.WorkerPreset = workerPreset;
        Console.WriteLine($"일꾼 프리셋 초기화 완료: {workerPreset} (0x{(int)workerPreset:X2})");
    }
    // 다른 프리셋들도 처리...
}

public void HandlePresetUpdate(PresetUpdateMessage message)
{
    if (message.PresetType == "worker" && message.Data is WorkerPreset workerData)
    {
        var workerPreset = (WorkerPresetEnum)workerData.SettingsMask;
        _workerManager.WorkerPreset = workerPreset;
        Console.WriteLine($"일꾼 프리셋 업데이트: {workerPreset} (0x{(int)workerPreset:X2})");
    }
}
```

### WorkerManager에서의 활용
기존 WorkerManager는 이미 WorkerPresetEnum을 사용하고 있어 추가 수정이 최소화됩니다:

```csharp
// 기존 코드 활용 - WorkerManager.cs:121
var calculatedTotal = IsWorkerStateSet(WorkerPresetEnum.IncludeProduction)
    ? totalWorkers
    : totalWorkers - productionWorkers;
```

## 📝 테스트 시나리오

### 1. 비트마스크 계산 테스트
- 모든 설정 ON: 0b0011_1111 = 63
- 모든 설정 OFF: 0b0000_0000 = 0
- 혼합 설정: 예시 케이스들 검증

### 2. 메시지 전송 테스트
- Named Pipe 연결 후 초기화 메시지 전송
- 설정 변경 후 업데이트 메시지 전송
- Core에서 올바른 설정 파싱 검증

### 3. 확장성 테스트
- 새로운 프리셋 타입 추가 시 기존 코드 영향 없음 확인

## 🚀 향후 확장 계획

1. **Population 프리셋**: 인구수 관련 설정들
2. **Unit 프리셋**: 유닛 관련 설정들
3. **Upgrade 프리셋**: 업그레이드 관련 설정들
4. **BuildOrder 프리셋**: 빌드오더 관련 설정들

각 프리셋마다 고유한 비트마스크 구조를 가지며, `preset-init` 메시지에 선택적으로 포함될 수 있습니다.

## 🔍 검토 포인트

- [ ] 비트마스크 계산 로직 정확성
- [ ] 메시지 구조의 확장 가능성
- [ ] Named Pipe 통신 안정성
- [ ] Core 측 파싱 로직 정확성
- [ ] 에러 처리 및 예외 상황 대응