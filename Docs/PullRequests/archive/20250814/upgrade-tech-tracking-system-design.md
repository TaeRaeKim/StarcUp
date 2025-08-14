# 업그레이드/테크 추적 시스템 설계 문서

## 1. 개요

### 1.1 목적
스타크래프트 게임 내 업그레이드와 테크 연구 상태를 실시간으로 추적하고, UI 오버레이에 표시하기 위한 시스템 설계

### 1.2 범위
- 플레이어별 업그레이드 레벨 (0-3) 추적
- 테크 연구 완료 여부 (0-1) 추적
- 연구 진행 중인 항목의 잔여 시간 계산
- UI 프리셋 기반 선택적 데이터 전송
- 상태 변경 이벤트 시스템

### 1.3 주요 요구사항
- 메모리에서 직접 업그레이드/테크 데이터 읽기
- 사용자 정의 카테고리별 그룹화
- 실시간 상태 변경 감지 및 알림
- 진행률, 잔여시간 등 부가 정보 제공

## 2. 시스템 아키텍처

### 2.1 전체 구조
```
┌─────────────────────────────────────────────────────────────┐
│                        StarcUp.UI                           │
│  ┌───────────────────────────────────────────────────────┐ │
│  │         React Components (UpgradeTracker)             │ │
│  └───────────────────────────────────────────────────────┘ │
│  ┌───────────────────────────────────────────────────────┐ │
│  │              IPC Communication Layer                   │ │
│  └───────────────────────────────────────────────────────┘ │
└─────────────────────────┬───────────────────────────────────┘
                          │ Named Pipe
┌─────────────────────────┴───────────────────────────────────┐
│                       StarcUp.Core                          │
│  ┌───────────────────────────────────────────────────────┐ │
│  │              CommunicationService                     │ │
│  └───────────────────────────────────────────────────────┘ │
│  ┌───────────────────────────────────────────────────────┐ │
│  │               UpgradeManager                          │ │
│  │    (Business Logic, Event System & Processing)        │ │
│  └───────────────────────────────────────────────────────┘ │
│  ┌───────────────────────────────────────────────────────┐ │
│  │            UpgradeMemoryAdapter                       │ │
│  │             (Memory Reading Layer)                    │ │
│  └───────────────────────────────────────────────────────┘ │
│  ┌───────────────────────────────────────────────────────┐ │
│  │              MemoryService (Existing)                 │ │
│  └───────────────────────────────────────────────────────┘ │
└──────────────────────────────────────────────────────────────┘
```

### 2.2 계층별 책임

#### Infrastructure Layer (메모리 접근)
- **UpgradeMemoryAdapter**: 메모리 오프셋을 통한 직접 데이터 읽기
- **MemoryService**: 기존 메모리 접근 인프라 활용

#### Business Layer (비즈니스 로직)
- **UpgradeService**: 데이터 가공, 진행 시간 계산, 상태 관리
- **UpgradeManager**: UI 통신, 이벤트 관리, 프리셋 처리

#### Communication Layer (통신)
- **NamedPipeProtocol**: 명령/이벤트 프로토콜 정의
- **CommunicationService**: 메시지 라우팅 및 핸들러

#### UI Layer (표시)
- **React Components**: 업그레이드/테크 상태 표시
- **IPC Handler**: Core와의 양방향 통신

## 3. 데이터 모델

### 3.1 Core 모델

```csharp
// 업그레이드/테크 통계 데이터
public class UpgradeTechStatistics
{
    public List<UpgradeCategoryData> Categories { get; set; }
    public DateTime Timestamp { get; set; }
    public byte PlayerIndex { get; set; }
}

// 카테고리별 데이터
public class UpgradeCategoryData
{
    public string Id { get; set; }
    public string Name { get; set; }
    public List<UpgradeData> Upgrades { get; set; }
    public List<TechData> Techs { get; set; }
}

// 업그레이드 데이터
public class UpgradeData
{
    public UpgradeType Type { get; set; }
    public byte Level { get; set; }           // 0-3
    public int RemainingFrames { get; set; }  // 진행 중일 때
    public int TotalFrames { get; set; }      // 총 소요 프레임
    public bool IsProgressing { get; set; }
}

// 테크 데이터
public class TechData
{
    public TechType Type { get; set; }
    public bool IsCompleted { get; set; }
    public int RemainingFrames { get; set; }
    public int TotalFrames { get; set; }
    public bool IsProgressing { get; set; }
}

// 상태 변경 이벤트
public class UpgradeStateChangedEventArgs : EventArgs
{
    public UpgradeType? UpgradeType { get; set; }
    public TechType? TechType { get; set; }
    public byte OldLevel { get; set; }
    public byte NewLevel { get; set; }
    public bool WasCompleted { get; set; }
    public bool IsCompleted { get; set; }
    public byte PlayerIndex { get; set; }
}
```

### 3.2 UI 모델 (TypeScript)

```typescript
// 업그레이드 설정 (프리셋)
export interface UpgradeSettings {
  categories: UpgradeCategory[];
  showRemainingTime: boolean;
  showProgressPercentage: boolean;
  showProgressBar: boolean;
  upgradeCompletionAlert: boolean;
  upgradeStateTracking: boolean;
}

// 응답 데이터
export interface UpgradeResponse {
  categories: UpgradeCategoryResponse[];
  timestamp: string;
  playerIndex: number;
}

export interface UpgradeCategoryResponse {
  id: string;
  name: string;
  upgrades: UpgradeDataResponse[];
  techs: TechDataResponse[];
}

export interface UpgradeDataResponse {
  type: string;
  level: number;
  remainingFrames?: number;
  totalFrames?: number;
  isProgressing: boolean;
}

export interface TechDataResponse {
  type: string;
  isCompleted: boolean;
  remainingFrames?: number;
  totalFrames?: number;
  isProgressing: boolean;
}
```

## 4. 메모리 매핑

### 4.1 업그레이드 메모리 오프셋
```
기준 주소: ["THREADSTACK0"-00000520]

플레이어 N 업그레이드:
- Index 0-43:  +0xE1C0 + (N * 44) (44 bytes per player)
- Index 47-54: +0x1023D + (N * 8) (8 bytes per player)

예시:
- 플레이어 0: +0xE1C0 (0-43), +0x1023D (47-54)
- 플레이어 1: +0xE1EC (0-43), +0x10245 (47-54)
- 플레이어 2: +0xE218 (0-43), +0x1024D (47-54)
```

### 4.2 테크 메모리 오프셋
```
기준 주소: ["THREADSTACK0"-00000520]

플레이어 N 테크:
- Index 0-23:  +0xDE54 + (N * 24) (24 bytes per player)
- Index 24-43: +0x10050 + (N * 20) (20 bytes per player)

예시:
- 플레이어 0: +0xDE54 (0-23), +0x10050 (24-43)
- 플레이어 1: +0xDE6C (0-23), +0x10064 (24-43)
- 플레이어 2: +0xDE84 (0-23), +0x10078 (24-43)
```

### 4.3 데이터 해석
- **업그레이드**: 
  - 레벨이 있는 업그레이드: 0=없음, 1-3=레벨
  - 단순 업그레이드: 0=미연구, 1=완료
- **테크**: 각 byte 값이 연구 여부 (0=미연구, 1=완료)

## 5. 통신 프로토콜

### 5.1 명령 (UI → Core)

```csharp
// 프리셋 초기화 시 업그레이드 설정 포함
// CommunicationService.HandlePresetInit() 내부에서 처리
public class PresetInitData
{
    public Presets Presets { get; set; }
}

public class Presets 
{
    public PresetItem Upgrade { get; set; }  // 업그레이드 프리셋
    public PresetItem Worker { get; set; }   // 일꾼 프리셋
    public PresetItem Population { get; set; } // 인구수 프리셋
    // ... 기타 프리셋들
}

public class PresetItem
{
    public bool Enabled { get; set; }
    public object Settings { get; set; }  // UpgradeSettings로 역직렬화
}
```

**참고**: 업그레이드 데이터는 설정이 등록되면 Core에서 자동으로 Event 형식으로 전송됨

### 5.2 이벤트 (Core → UI)

```csharp
// 업그레이드 데이터 업데이트
public class UpgradeDataUpdatedEvent : IEvent
{
    public UpgradeTechStatistics Data { get; set; }
}

// 업그레이드 상태 변경
public class UpgradeStateChangedEvent : IEvent
{
    public UpgradeStateChangedEventArgs Args { get; set; }
}

// 업그레이드 완료 알림
public class UpgradeCompletedEvent : IEvent
{
    public string UpgradeName { get; set; }
    public byte Level { get; set; }
}
```

### 5.3 데이터 직렬화 및 역직렬화

#### Event 전송 패턴 (CommunicationService 참조)
```csharp
// 업그레이드 데이터 이벤트 전송
private void SendUpgradeDataEvent(UpgradeTechStatistics statistics)
{
    var eventData = new
    {
        categories = statistics.Categories.Select(c => new
        {
            id = c.Id,
            name = c.Name,
            upgrades = c.Upgrades.Select(u => new
            {
                type = u.Type.ToString(),
                level = u.Level,
                remainingFrames = u.RemainingFrames,
                totalFrames = u.TotalFrames,
                isProgressing = u.IsProgressing
            }),
            techs = c.Techs.Select(t => new
            {
                type = t.Type.ToString(),
                isCompleted = t.IsCompleted,
                remainingFrames = t.RemainingFrames,
                totalFrames = t.TotalFrames,
                isProgressing = t.IsProgressing
            })
        }),
        timestamp = statistics.Timestamp
    };

    _pipeClient.SendEvent(NamedPipeProtocol.Events.UpgradeDataUpdated, eventData);
}
```

#### 프리셋 수신 및 역직렬화 (HandlePresetInit)
```csharp
private void HandleUpgradePreset(PresetItem upgradePreset)
{
    if (!upgradePreset.Enabled)
    {
        LoggerHelper.Warning("⚡ 업그레이드 기능이 비활성화되어 있습니다");
        return;
    }

    // settings 필드에서 UpgradeSettings 객체 파싱
    if (upgradePreset.Settings != null)
    {
        UpgradeSettings upgradeSettings;
        
        if (upgradePreset.Settings is JsonElement element)
        {
            var jsonText = element.GetRawText();
            upgradeSettings = JsonSerializer.Deserialize<UpgradeSettings>(jsonText);
        }
        else
        {
            LoggerHelper.Error($"🛠️ 지원되지 않는 업그레이드 설정 타입: {upgradePreset.Settings.GetType()}");
            return;
        }
        
        // UpgradeManager에 설정 적용
        _upgradeManager.UpdateSettings(upgradeSettings);
        LoggerHelper.Info($"✅ 업그레이드 설정 적용 완료: {upgradeSettings.Categories.Count} 개 카테고리");
    }
}
```

## 6. 구현 세부사항

### 6.1 UpgradeMemoryAdapter

```csharp
public interface IUpgradeMemoryAdapter : IDisposable
{
    // 업그레이드 레벨 읽기 (0-3)
    byte GetUpgradeLevel(UpgradeType type, byte playerIndex);
    
    // 테크 완료 여부 읽기
    bool IsTechCompleted(TechType type, byte playerIndex);
    
    // 진행 중인 업그레이드/테크 정보
    (bool isProgressing, int remainingFrames, int totalFrames) 
        GetProgressInfo(int unitTypeOrBuildingId);
    
    // 전체 업그레이드 상태 읽기
    byte[] ReadAllUpgrades(byte playerIndex);
    
    // 전체 테크 상태 읽기
    byte[] ReadAllTechs(byte playerIndex);
}
```

### 6.2 UpgradeManager

```csharp
public interface IUpgradeManager
{
    // 이벤트
    event EventHandler<UpgradeStateChangedEventArgs> StateChanged;
    event EventHandler<UpgradeCompletedEventArgs> UpgradeCompleted;
    
    // 프로퍼티
    UpgradeTechStatistics CurrentStatistics { get; }
    UpgradeSettings CurrentSettings { get; }
    int LocalPlayerId { get; }
    
    // 초기화
    void Initialize(int localPlayerId);
    
    // 설정 업데이트
    void UpdateSettings(UpgradeSettings settings);
    
    // 업데이트 (GameManager에서 24fps로 호출)
    void Update();
    
    // 내부 메서드 (private in implementation)
    // - 카테고리별 업그레이드/테크 데이터 가져오기
    // - 상태 변경 감지
    // - 진행 시간 계산
}
```

## 7. 데이터 흐름

### 7.1 초기화 시퀀스
```
1. UI → Core: UpdateUpgradeSettingsCommand (프리셋 전송)
2. Core: UpgradeManager 설정 업데이트
3. Core: UpgradeService 초기화
4. Core → UI: UpgradeDataUpdatedEvent (초기 상태)
```

### 7.2 주기적 업데이트 (24fps - 42ms)
```
1. GameManager.OnUpdateTimerElapsed() → UpgradeManager.Update() 호출
2. UpgradeMemoryAdapter로 메모리 읽기
3. UpgradeManager 내부에서 데이터 가공
4. 변경사항 감지
5. Core → UI: UpgradeDataUpdatedEvent
6. (변경 시) Core → UI: UpgradeStateChangedEvent
```

### 7.3 설정 변경
```
1. UI → Core: UpdateUpgradeSettingsCommand
2. Core: 카테고리 재구성
3. Core → UI: UpgradeDataUpdatedEvent (새 구조)
```

## 8. 성능 고려사항

### 8.1 메모리 읽기 최적화
- 배치 읽기: 개별 업그레이드가 아닌 전체 배열 한 번에 읽기
- 캐싱: 42ms 주기로만 메모리 읽기 (24fps)
- 버퍼 풀링: 메모리 할당 최소화

### 8.2 통신 최적화
- 변경사항만 전송: 전체 데이터가 아닌 델타만 전송
- 이벤트 배칭: 여러 변경사항을 하나의 이벤트로 묶기
- 압축: 큰 데이터는 압축하여 전송

### 8.3 UI 렌더링 최적화
- React.memo 사용: 불필요한 리렌더링 방지
- 가상화: 많은 항목 표시 시 가상 스크롤링
- 디바운싱: 빠른 업데이트 시 렌더링 제한

## 9. 에러 처리

### 9.1 메모리 접근 실패
- 재시도 로직 (3회)
- 폴백 값 사용 (이전 상태 유지)
- 에러 로깅 및 사용자 알림

### 9.2 통신 오류
- 재연결 시도
- 메시지 큐잉
- 타임아웃 처리

### 9.3 데이터 무결성
- 체크섬 검증
- 범위 검증 (레벨 0-3, 인덱스 범위)
- 상태 일관성 검증

## 10. 테스트 계획

### 10.1 단위 테스트
- UpgradeMemoryAdapter: 메모리 읽기 정확성
- UpgradeService: 비즈니스 로직 검증
- UpgradeManager: 이벤트 시스템 테스트

### 10.2 통합 테스트
- Core-UI 통신 테스트
- 전체 데이터 흐름 테스트
- 성능 테스트 (100ms 주기 유지)

### 10.3 시나리오 테스트
- 업그레이드 진행 → 완료 시나리오
- 동시 다중 업그레이드
- 설정 변경 시 동작
- 연결 끊김/재연결

## 11. 향후 확장 가능성

### 11.1 기능 확장
- 업그레이드 큐 예측
- 자원 소비 계산
- 업그레이드 추천 시스템
- 히스토리 기록

### 11.2 성능 개선
- 차등 업데이트 알고리즘
- 예측 모델을 통한 프리페칭
- GPU 가속 렌더링

### 11.3 사용성 개선
- 커스텀 알림 사운드
- 시각적 효과 강화
- 단축키 지원
- 다중 플레이어 동시 추적

## 12. 참고 자료

### 12.1 기존 구현 참조
- `WorkerManager.cs`: 통신 패턴 및 이벤트 시스템
- `PopulationManager.cs`: 주기적 업데이트 및 상태 관리
- `WorkerStatistics.cs`: 데이터 모델 구조
- `PopulationStatistics.cs`: 통계 데이터 구조

### 12.2 메모리 오프셋 자료
- `공통.CT`: Cheat Engine 테이블 (업그레이드/테크 오프셋)
- 스타크래프트 메모리 맵 문서

### 12.3 관련 타입 정의
- `UpgradeType`: StarcUp.Core의 업그레이드 열거형
- `TechType`: StarcUp.Core의 테크 열거형

## 13. 구현 일정

### Phase 1: Core Infrastructure (1일)
- [ ] IUpgradeMemoryAdapter 인터페이스 정의
- [ ] UpgradeMemoryAdapter 구현
- [ ] UpgradeTechStatistics 모델 정의

### Phase 2: Business Logic (1일)
- [ ] IUpgradeManager 인터페이스 정의
- [ ] UpgradeManager 구현 (WorkerManager 패턴 참조)
- [ ] GameManager에 UpgradeManager 통합

### Phase 3: Communication (1일)
- [ ] NamedPipeProtocol 업데이트
- [ ] CommunicationService 핸들러 추가
- [ ] ServiceRegistration 업데이트

### Phase 4: UI Integration (1일)
- [ ] TypeScript 타입 정의
- [ ] IPC 핸들러 구현
- [ ] React 컴포넌트 개발

### Phase 5: Testing & Polish (1일)
- [ ] 단위 테스트 작성
- [ ] 통합 테스트
- [ ] 버그 수정 및 최적화

## 14. 위험 요소 및 대응 방안

### 14.1 메모리 오프셋 변경
- **위험**: 게임 패치로 인한 오프셋 변경
- **대응**: 오프셋 설정 파일 외부화, 동적 오프셋 탐색

### 14.2 성능 저하
- **위험**: 잦은 메모리 읽기로 인한 성능 영향
- **대응**: 읽기 주기 조절, 캐싱 강화

### 14.3 동기화 문제
- **위험**: Core-UI 간 상태 불일치
- **대응**: 버전 관리, 체크섬 검증

## 15. 승인 및 검토

### 검토자
- 프로젝트 리드
- UI 팀
- Core 팀

### 승인 기준
- 기존 시스템과의 일관성
- 성능 요구사항 충족
- 확장 가능성 확보

---

**작성일**: 2025-01-14  
**작성자**: StarcUp Development Team  
**버전**: 1.0