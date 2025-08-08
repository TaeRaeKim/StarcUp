# 프리셋 데이터 중앙 관리 시스템 설계 문서

## 📋 개요

StarcUp 프로젝트에서 프리셋 데이터의 중앙 집중식 관리를 통해 데이터 일관성과 유지보수성을 향상시키기 위한 시스템 설계 문서입니다.

### 🎯 목표

1. **중앙 집중식 프리셋 관리**: Electron 메인 프로세스에서 모든 프리셋 데이터를 중앙 관리
2. **이벤트 기반 동기화**: 프리셋 변경 시 모든 관련 컴포넌트로 자동 업데이트
3. **자동화된 저장**: 프리셋 변경 시 DataStorage를 통한 자동 저장
4. **실시간 동기화**: Core, Overlay, Main-Page 간 실시간 프리셋 상태 동기화

---

## 🔍 현재 구현 분석

### 현재 아키텍처의 문제점

#### 1. **분산된 프리셋 관리**
```typescript
// 현재: MainPage에서 직접 프리셋 관리
const [presets, setPresets] = useState<Preset[]>([]);
const [currentPresetIndex, setCurrentPresetIndex] = useState(0);

// 문제: 각 컴포넌트가 독립적으로 프리셋 상태를 관리
```

#### 2. **수동적인 동기화**
```typescript
// 현재: MainPage에서 수동으로 Core에 전송
const sendPresetToCore = useCallback(async () => {
  if (presetsLoaded && presets.length > 0) {
    // 개별 프리셋 타입별로 수동 전송
    await window.coreAPI.sendPresetUpdate(workerPresetData);
  }
}, [presets, currentPresetIndex, presetsLoaded]);
```

#### 3. **복잡한 상태 동기화**
```typescript
// 현재: 프리셋 변경 시 다중 단계 업데이트
useEffect(() => {
  sendPresetToCore(); // 1. Core로 전송
  // 2. DataStorage 저장은 별도 로직
  // 3. Overlay 업데이트는 별도 IPC 호출
}, [currentPresetIndex]);
```

### 현재 데이터 흐름

```
MainPage (상태 관리)
    ↓ 수동 호출
┌─ DataStorageService (개별 저장)
└─ CoreCommunicationService (수동 전송)
    ↓ 별도 IPC
  OverlayApp (독립적 상태)
```

---

## 🏗️ 새로운 아키텍처 설계

### 1. 시스템 구조

```
┌─────────────────────────────────────────────────────────────┐
│                   Electron Main Process                     │
│  ┌─────────────────────────────────────────────────────────┐│
│  │              ServiceContainer                            ││
│  │  ┌─────────────────────┐  ┌─────────────────────────────┐││
│  │  │  PresetStateManager │  │   DataStorageService        │││
│  │  │   (중앙 상태 관리)     │  │    (자동 저장)              │││
│  │  └─────────────────────┘  └─────────────────────────────┘││
│  │              │                         │                ││
│  │              │ 이벤트 기반               │ 자동 저장          ││
│  │              │                         │                ││
│  │  ┌─────────────────────┐  ┌─────────────────────────────┐││
│  │  │ CoreCommunicationSvc │  │      IPCService             │││
│  │  │  (자동 전송)          │  │   (이벤트 브로드캐스트)        │││
│  │  └─────────────────────┘  └─────────────────────────────┘││
│  └─────────────────────────────────────────────────────────┘│
└─────────────────────────────────────────────────────────────┘
           │                            │
           │ 자동 Named Pipe 전송         │ IPC 이벤트
           │                            │ 
           │                            │
 ┌─────────────────┐         ┌─────────────────────────────────┐
 │  StarcUp.Core   │         │     Renderer Processes          │
 │                 │         │  ┌─────────────────────────────┐ │
 │  즉시 반영       │         │  │       Main Page             │ │
 │                 │         │  │   (이벤트 수신자)            │ │
 └─────────────────┘         │  └─────────────────────────────┘ │
                             │  ┌─────────────────────────────┐ │
                             │  │      Overlay Window         │ │
                             │  │  (기본 기능만 수신)          │ │
                             │  └─────────────────────────────┘ │
                             └─────────────────────────────────┘
```

### 2. 핵심 컴포넌트

#### 2.1 PresetStateManager (신규)

**역할**: 프리셋 상태의 중앙 관리 및 이벤트 발행
**위치**: `StarcUp.UI/electron/src/services/preset/PresetStateManager.ts`

```typescript
export interface IPresetStateManager {
  // 상태 조회
  getCurrentPreset(): IPreset | null
  getPresetState(): IPresetState
  getAllPresets(): IPreset[]
  
  // 프리셋 관리
  switchPreset(presetId: string): Promise<void>
  updatePresetSettings(presetType: string, settings: any): Promise<void>
  toggleFeature(featureIndex: number, enabled: boolean): Promise<void>
  
  // 이벤트 관리
  onStateChanged(callback: (event: IPresetChangeEvent) => void): () => void
  
  // 초기화 및 정리
  initialize(): Promise<void>
  dispose(): Promise<void>
}

export interface IPresetChangeEvent {
  type: 'preset-switched' | 'settings-updated' | 'feature-toggled'
  presetId: string
  changes: {
    featureStates?: boolean[]
    settings?: Record<string, any>
    toggledFeature?: { index: number; enabled: boolean }
  }
  timestamp: Date
}
```

#### 2.2 개선된 데이터 흐름

```typescript
// 새로운 흐름: 이벤트 기반 자동화
MainPage (프리셋 설정 변경)
    ↓ IPC: preset:update-settings
PresetStateManager (중앙 상태 업데이트 + 이벤트 발행)
    ↓ 자동 트리거
┌─ DataStorageService (자동 저장)
├─ CoreCommunicationService (자동 전송)
└─ All Renderer Processes (이벤트 브로드캐스트)
    ↓ 실시간 업데이트
  MainPage & Overlay (UI 자동 업데이트)
```

---

## 🔗 IPC 채널 설계

### 기본 프리셋 관리 채널

```typescript
export interface IPresetIPCChannels {
  // 상태 조회
  'preset:get-current': { 
    request: void; 
    response: { preset: IPreset | null } 
  }
  
  'preset:get-all': { 
    request: void; 
    response: { presets: IPreset[] } 
  }
  
  // 프리셋 관리
  'preset:switch': { 
    request: { presetId: string }; 
    response: { success: boolean } 
  }
  
  'preset:update-settings': { 
    request: { presetType: string; settings: any }; 
    response: { success: boolean } 
  }
  
  'preset:toggle-feature': { 
    request: { featureIndex: number; enabled: boolean }; 
    response: { success: boolean } 
  }
}
```

### 이벤트 채널 (Renderer 수신용)

```typescript
export interface IPresetEventChannels {
  // 프리셋 변경 이벤트
  'preset:state-changed': { 
    request: never; 
    response: { 
      preset: IPreset; 
      changeType: 'switched' | 'settings-updated' | 'feature-toggled';
      changes: any;
    } 
  }
  
  // Overlay용 기본 기능 상태만 전송
  'preset:features-changed': { 
    request: never; 
    response: { 
      featureStates: boolean[]; // [worker, population, unit, upgrade, buildOrder]
    } 
  }
}
```

---

## 📁 파일 구조

### 신규 파일

```
StarcUp.UI/electron/src/services/preset/
├── PresetStateManager.ts          # 중앙 상태 관리자
├── interfaces.ts                  # 타입 정의
├── PresetEventEmitter.ts          # 이벤트 시스템
└── index.ts                       # Export 관리
```

### 수정될 기존 파일

```
StarcUp.UI/electron/src/services/
├── ServiceContainer.ts            # PresetStateManager 등록
├── ipc/ChannelHandlers.ts         # 프리셋 IPC 채널 추가
└── core/CoreCommunicationService.ts # 자동 전송 로직 개선

StarcUp.UI/
├── preload.ts                     # 프리셋 IPC API 추가
├── src/vite-env.d.ts              # 타입 정의 확장
├── src/main-page/App.tsx          # 이벤트 기반 수신으로 변경
└── src/overlay/OverlayApp.tsx     # 프리셋 기능 상태 수신
```

---

## 🔄 데이터 변환 및 호환성

### Core 통신을 위한 데이터 변환

현재 Core는 `presetUtils.ts`의 프로토콜을 사용하므로, PresetStateManager에서 자동 변환을 수행합니다.

```typescript
// PresetStateManager 내부 변환 로직
private convertToCore(preset: IPreset): PresetInitMessage {
  return {
    type: 'preset-init',
    timestamp: Date.now(),
    presets: {
      worker: {
        enabled: preset.featureStates[0],
        settingsMask: calculateWorkerSettingsMask(preset.workerSettings)
      },
      population: {
        enabled: preset.featureStates[1],
        settingsMask: 0 // TODO: 추후 구현
      },
      // ... 다른 기능들
    }
  };
}
```

### Overlay를 위한 단순화된 데이터

```typescript
// Overlay는 기본 기능 On/Off만 필요
interface OverlayPresetData {
  featureStates: boolean[]; // [worker, population, unit, upgrade, buildOrder]
}
```

---

## 🛠️ 구현 단계별 계획

### Phase 1: PresetStateManager 구현
1. **PresetStateManager 클래스** 생성
   - 기본 상태 관리 로직
   - 이벤트 시스템 구현
   - FilePresetRepository와 연동

2. **ServiceContainer 등록**
   - 의존성 주입 설정
   - 생명주기 관리 연동

### Phase 2: IPC 채널 확장
1. **새로운 IPC 채널** 추가
   - ChannelHandlers.ts에 프리셋 핸들러 추가
   - 이벤트 브로드캐스트 로직 구현

2. **Preload API 확장**
   - preload.ts에 새로운 IPC API 추가
   - TypeScript 타입 정의 업데이트

### Phase 3: 자동 동기화 구현
1. **PresetStateManager 이벤트 시스템**
   - 상태 변경 시 이벤트 자동 발행
   - 여러 리스너에게 동시 브로드캐스트

2. **Core 통신 자동화**
   - CoreCommunicationService와 연동
   - 프리셋 변경 시 자동 전송

3. **DataStorage 자동 저장**
   - 프리셋 변경 시 자동 저장 트리거
   - 성능 최적화를 위한 debouncing

### Phase 4: UI 컴포넌트 연동
1. **MainPage 리팩토링**
   - 기존 useState를 이벤트 수신으로 변경
   - 프리셋 변경 로직을 IPC 호출로 단순화

2. **OverlayApp 연동**
   - 프리셋 기능 상태 이벤트 수신 추가
   - 실시간 UI 업데이트 구현

---

## 🔧 현재 구현의 개선 방안

### 1. MainPage App.tsx 개선

#### 현재 문제점
```typescript
// 현재: 복잡한 수동 상태 관리
const [presets, setPresets] = useState<Preset[]>([]);
const [currentPresetIndex, setCurrentPresetIndex] = useState(0);

// 복잡한 프리셋 전송 로직
const sendPresetToCore = useCallback(async () => {
  // 복잡한 수동 변환 및 전송 로직
}, [presets, currentPresetIndex, presetsLoaded]);
```

#### 개선 후
```typescript
// 개선: 이벤트 기반 단순화
const [preset, setPreset] = useState<IPreset | null>(null);

// 단순화된 프리셋 변경
const handlePresetChange = async (presetId: string) => {
  await window.presetAPI.switchPreset(presetId);
  // 나머지는 이벤트로 자동 처리
};

// 이벤트 수신으로 자동 동기화
useEffect(() => {
  const unsubscribe = window.presetAPI.onStateChanged((event) => {
    setPreset(event.preset);
    // UI 자동 업데이트
  });
  return unsubscribe;
}, []);
```

### 2. OverlayApp 개선

#### 현재 문제점
```typescript
// 현재: 프리셋 기능 상태를 받지 못함
const [overlaySettings, setOverlaySettings] = useState<OverlaySettings>({
  showWorkerStatus: true,  // 하드코딩된 기본값
  showBuildOrder: false,
  // ...
});
```

#### 개선 후
```typescript
// 개선: 프리셋 기능 상태 자동 동기화
const [presetFeatures, setPresetFeatures] = useState<boolean[]>([]);

useEffect(() => {
  const unsubscribe = window.presetAPI.onFeaturesChanged((data) => {
    setPresetFeatures(data.featureStates);
    // 기능별 오버레이 표시 자동 조정
    setOverlaySettings(prev => ({
      ...prev,
      showWorkerStatus: data.featureStates[0],
      showPopulationWarning: data.featureStates[1],
      showUnitCount: data.featureStates[2],
      showUpgradeProgress: data.featureStates[3],
      showBuildOrder: data.featureStates[4],
    }));
  });
  return unsubscribe;
}, []);
```

### 3. CoreCommunicationService 개선

#### 현재 문제점
```typescript
// 현재: 수동적인 프리셋 전송
async sendPresetInit(message: any): Promise<ICoreResponse> {
  return await this.sendCommand({ 
    type: 'preset:init', 
    payload: message 
  })
}
```

#### 개선 후
```typescript
// 개선: PresetStateManager와 연동하여 자동 전송
constructor(presetStateManager: IPresetStateManager) {
  // 프리셋 변경 이벤트 자동 구독
  presetStateManager.onStateChanged(async (event) => {
    const coreMessage = this.convertPresetForCore(event.preset);
    await this.sendCommand({
      type: 'preset:update',
      payload: coreMessage
    });
  });
}
```

---

## ⚡ 성능 최적화

### 1. 이벤트 Debouncing
```typescript
// 빈번한 설정 변경 시 성능 최적화
private debouncedSave = debounce(async (preset: IPreset) => {
  await this.dataStorageService.savePreset(preset);
}, 300);
```

### 2. 메모리 캐싱
```typescript
// 자주 사용하는 프리셋 데이터 캐싱
private presetCache = new Map<string, IPreset>();
```

### 3. 선택적 업데이트
```typescript
// Overlay는 기본 기능 상태만 수신 (상세 설정 제외)
if (changeEvent.type === 'feature-toggled') {
  this.broadcastToOverlay({ featureStates: preset.featureStates });
}
```

---

## 🧪 테스트 전략

### 1. 단위 테스트
- PresetStateManager 상태 관리 로직
- 이벤트 발행/수신 메커니즘
- 데이터 변환 함수

### 2. 통합 테스트
- IPC 채널 통신
- Core 통신 프로토콜
- DataStorage 자동 저장

### 3. E2E 테스트
- MainPage에서 프리셋 변경 → Overlay 즉시 반영
- 프리셋 설정 변경 → Core 즉시 적용
- 앱 재시작 시 마지막 상태 복원

---

## 📝 마이그레이션 계획

### 1. 기존 코드 호환성
- 기존 IPC 채널은 deprecated로 표시하되 유지
- 단계적 마이그레이션으로 안정성 확보

### 2. 데이터 마이그레이션
- 기존 FilePresetRepository 데이터 형식 유지
- 새로운 필드는 기본값으로 초기화

### 3. 점진적 전환
1. PresetStateManager 구현 및 테스트
2. 새로운 IPC 채널 추가 (기존과 병행)
3. MainPage 부분적 마이그레이션
4. 전체 시스템 검증 후 기존 코드 제거

---

## 🎯 예상 효과

### 1. 개발 생산성 향상
- **코드 중복 제거**: 프리셋 관리 로직 중앙화
- **디버깅 용이성**: 단일 지점에서 모든 프리셋 상태 추적
- **유지보수성**: 인터페이스 기반 설계로 확장성 확보

### 2. 사용자 경험 개선
- **실시간 동기화**: 설정 변경 즉시 모든 UI에 반영
- **일관성 보장**: 모든 컴포넌트 간 데이터 일관성 유지
- **안정성 향상**: 중앙 집중식 관리로 오류 가능성 감소

### 3. 시스템 안정성
- **자동화된 저장**: 데이터 손실 방지
- **오류 복구**: 중앙 집중식 오류 처리
- **성능 최적화**: 불필요한 중복 처리 제거

---

이 설계를 통해 StarcUp의 프리셋 시스템은 더욱 견고하고 유지보수하기 쉬운 구조로 발전할 것입니다.