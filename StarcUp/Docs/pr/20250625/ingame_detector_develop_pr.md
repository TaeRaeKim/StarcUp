# InGameDetector 구현 및 네이밍 개선: 게임 내부 상태 감지 시스템

## 📋 개요

스타크래프트 게임 내부 상태(로비 vs 실제 게임 플레이)를 실시간으로 감지하는 `InGameDetector` 서비스를 구현하고, 기존 네이밍 일관성 문제를 해결하여 더 직관적이고 간결한 클래스명으로 개선.

## 🤔 문제 인식

### 1. 게임 상태 감지의 한계
기존 `GameDetector`는 스타크래프트 **프로세스의 존재**만 감지할 수 있었음:
- ✅ 게임이 실행 중인지 확인
- ✅ 윈도우 포커스 상태 확인
- ❌ **실제 게임 플레이 중인지** vs **로비/메뉴 상태인지** 구분 불가

### 2. 네이밍 일관성 부족
```csharp
// 기존 구조의 문제점
GameDetector           // 게임 프로세스 감지
InGameStateMonitor     // 게임 내부 상태 감지 (이름이 길고 조잡함)

// 문제점:
// - "InGameState"와 "Monitor"가 중복적
// - "Monitor"가 너무 일반적인 용어
// - GameDetector와 네이밍 패턴이 다름
```

### 3. 복잡한 메모리 주소 계산 미구현
스타크래프트의 InGame 상태 확인을 위해서는 복잡한 포인터 체인이 필요:
```
[[PEB Address + 0x828] + [B] + 0xD8] + 0xB4

B 계산 로직:
1. rcx = StarCraft.exe+10B1838
2. movzx edx, cx 
3. edx = edx * [USER32.dll+D52B0]
4. B = edx + [USER32.dll+D52A8]
```

## 💡 해결 방향

### 1. 명확한 역할 분담
```csharp
GameDetector    // 게임 프로세스 감지 (기존)
├── 스타크래프트 프로세스 실행 여부
├── 윈도우 포커스 상태 변화
└── 윈도우 위치 변화

InGameDetector  // 게임 내부 상태 감지 (신규)
├── 로비 상태 vs 게임 중 상태 구분
├── 복잡한 메모리 포인터 체인 처리
└── 실시간 InGame 상태 모니터링
```

### 2. 네이밍 일관성 확보
```csharp
// 개선된 구조
GameDetector     // 게임 프로세스 감지
InGameDetector   // 게임 내부 상태 감지

// 장점:
// - "Detector" 패턴으로 통일
// - 간결하고 직관적
// - 역할이 명확히 구분됨
```

### 3. 어셈블리 로직의 정확한 C# 구현
어셈블리 명령어를 C#으로 정확히 변환:
```csharp
// movzx edx, cx → C# 구현
ushort cx = (ushort)_memoryService.ReadShort(rcx);  // 2바이트 직접 읽기
uint edx = cx;  // zero-extend to 32bit

// 메모리 타입별 정확한 읽기
int factorValue = _memoryService.ReadInt(user32Factor);      // 4바이트 정수
nint offsetValue = _memoryService.ReadPointer(user32Offset); // 8바이트 포인터
```

## 🏗️ 설계 결정

### InGameDetector 아키텍처
현재 프로젝트의 레이어드 아키텍처에 완벽하게 통합:

```
InGameDetector (Business Layer)
    ↓ 의존
MemoryService (Business Layer - 미들웨어)
    ↓ 의존
MemoryReader (Infrastructure Layer)
    ↓ 의존
Windows API
```

### 메모리 읽기 최적화
메모리 접근 시 타입별 최적화 적용:

```csharp
// ❌ 비효율적: 4바이트 읽고 2바이트 사용
int rcxValue = ReadInt(rcx);
ushort cx = (ushort)rcxValue;

// ✅ 효율적: 필요한 2바이트만 읽기
ushort cx = (ushort)ReadShort(rcx);
```

### 이벤트 기반 설계
기존 아키텍처와 일관성 있는 이벤트 기반 구조:

```csharp
public class InGameDetector
{
    public event EventHandler<InGameEventArgs> InGameStateChanged;
    
    // MemoryService의 ProcessConnect 이벤트 구독
    _memoryService.ProcessConnect += OnProcessConnect;
    _memoryService.ProcessDisconnect += OnProcessDisconnect;
}
```

## 🎯 구현 결과

### 파일 구조
```
Business/Services/
├── InGameDetector.cs           # 게임 내부 상태 감지 (신규)
└── IInGameDetector.cs          # 인터페이스 (신규)

Common/Events/
└── InGameEventArgs.cs          # 이벤트 아규먼트 (신규)
```

### 핵심 기능

#### 1. 복잡한 B값 계산
```csharp
private nint CalculateBValue()
{
    // 1. StarCraft.exe+10B1838에서 값 읽기
    nint rcx = _starcraftModule.BaseAddress + 0x10B1838;
    ushort cx = (ushort)_memoryService.ReadShort(rcx);  // 최적화: 2바이트만 읽기
    uint edx = cx;  // zero-extend
    
    // 2. USER32.dll 기반 계산
    int factorValue = _memoryService.ReadInt(_user32Module.BaseAddress + 0xD52B0);
    nint offsetValue = _memoryService.ReadPointer(_user32Module.BaseAddress + 0xD52A8);
    
    // 3. 최종 B값 계산
    return edx * factorValue + offsetValue;
}
```

#### 2. InGame 상태 확인
```csharp
private bool ReadInGameState()
{
    // 1. PEB 주소 가져오기
    nint pebAddress = _memoryService.GetPebAddress();
    
    // 2. B값 계산
    nint bValue = CalculateBValue();
    
    // 3. 복잡한 포인터 체인: [[PEB + 0x828] + [B] + 0xD8] + 0xB4
    nint step1 = _memoryService.ReadPointer(pebAddress + 0x828);
    nint step2 = _memoryService.ReadPointer(step1 + bValue + 0xD8);
    nint finalAddress = step2 + 0xB4;
    
    // 4. 최종 InGame 상태 읽기
    int inGameValue = _memoryService.ReadInt(finalAddress);
    return inGameValue == 1;  // 1이면 게임 중, 0이면 로비
}
```

#### 3. 실시간 모니터링
```csharp
private void CheckInGameState(object sender, ElapsedEventArgs e)
{
    bool newInGameState = ReadInGameState();
    
    if (newInGameState != IsInGame)
    {
        IsInGame = newInGameState;
        InGameStateChanged?.Invoke(this, new InGameEventArgs(IsInGame));
    }
}
```

### 네이밍 개선

#### 클래스명 변경
```csharp
// Before
InGameStateMonitor      → InGameDetector     ✅
IInGameStateMonitor     → IInGameDetector    ✅
InGameStateEventArgs    → InGameEventArgs    ✅
```

#### 변수명 정리
```csharp
// Before
_pointerMonitorService  → _inGameDetector    ✅

// Before (로깅)
"InGame 상태 모니터링"   → "InGame 상태 감지"  ✅
```

### 사용 예시
```csharp
// 게임 발견 시 InGame 감지 시작
gameDetector.GameFound += (s, e) => {
    inGameDetector.StartMonitoring(e.Game.ProcessId);
};

// InGame 상태 변화 감지
inGameDetector.InGameStateChanged += (s, e) => {
    if (e.IsInGame) {
        Console.WriteLine("🎮 게임에 진입했습니다!");
        ShowGameOverlay();
    } else {
        Console.WriteLine("📋 메뉴로 돌아갔습니다.");
        HideGameOverlay();
    }
};
```

## ✅ 개선 효과

### 1. 명확한 역할 분담
```csharp
// 프로세스 레벨 감지
GameDetector.GameFound      // "스타크래프트가 실행됨"

// 게임 내부 상태 감지  
InGameDetector.InGameStateChanged  // "실제 게임이 시작됨"
```

### 2. 네이밍 일관성 확보
- **패턴 통일**: `GameDetector` + `InGameDetector`
- **간결성**: `InGameStateMonitor` → `InGameDetector` (6글자 단축)
- **직관성**: "Detector"로 감지 역할 명확화

### 3. 메모리 접근 최적화
- **정확한 타입 사용**: `ReadShort` vs `ReadInt` vs `ReadPointer`
- **성능 향상**: 필요한 크기만 읽어서 전송량 50% 감소
- **어셈블리 일치**: `movzx edx, cx` 로직 정확히 구현

### 4. 아키텍처 일관성
- **기존 패턴 준수**: 이벤트 기반, 의존성 주입, 레이어 분리
- **확장성**: 다른 게임 상태 감지 기능 추가 용이
- **테스트 가능**: 인터페이스 기반 모킹 지원

## 🔄 마이그레이션 가이드

### DI 등록 변경
```csharp
// Before
container.RegisterSingleton<IInGameStateMonitor, InGameStateMonitor>();

// After
container.RegisterSingleton<IInGameDetector, InGameDetector>();
```

### 코드 업데이트
```csharp
// Before
private readonly IInGameStateMonitor _pointerMonitorService;
_pointerMonitorService.InGameStateChanged += OnInGameStateChanged;

// After
private readonly IInGameDetector _inGameDetector;
_inGameDetector.InGameStateChanged += OnInGameStateChanged;
```

### 이벤트 구독 변경
```csharp
// Before
void OnInGameStateChanged(object sender, InGameStateEventArgs e)

// After  
void OnInGameStateChanged(object sender, InGameEventArgs e)
```

## 🚀 향후 개선 여지

### 1. 하드코딩된 오프셋 문제 해결
현재 구현에서 가장 큰 개선 여지는 **하드코딩된 메모리 오프셋**:

```csharp
// ❌ 현재: 하드코딩된 오프셋들
nint rcx = _starcraftModule.BaseAddress + 0x10B1838;  // 고정 오프셋
nint user32Factor = _user32Module.BaseAddress + 0xD52B0;  // 고정 오프셋  
nint user32Offset = _user32Module.BaseAddress + 0xD52A8;  // 고정 오프셋
nint step1 = _memoryService.ReadPointer(pebAddress + 0x828);  // 고정 오프셋
```

**문제점**:
- 스타크래프트 업데이트 시 오프셋 변경 가능
- USER32.dll 버전별 차이 발생 가능
- 하드코딩으로 인한 유지보수 어려움

### 개선 방향

#### Option 1: 설정 파일 기반 오프셋 관리
```csharp
// GameOffsets.json
{
  "starcraft": {
    "version": "1.16.1",
    "gameStateOffset": "0x10B1838",
    "pebOffset": "0x828",
    "finalOffset": "0xB4"
  },
  "user32": {
    "factorOffset": "0xD52B0", 
    "baseOffset": "0xD52A8"
  }
}

// 코드에서 사용
var offsets = OffsetConfig.Load("GameOffsets.json");
nint rcx = _starcraftModule.BaseAddress + offsets.Starcraft.GameStateOffset;
```

#### Option 2: 패턴 스캐닝을 통한 동적 오프셋 탐지
```csharp
public class OffsetScanner
{
    // 바이트 패턴을 통해 동적으로 오프셋 찾기
    public nint FindGameStateOffset(nint moduleBase)
    {
        byte[] pattern = { 0x8B, 0x0D, 0x??, 0x??, 0x??, 0x?? }; // 특정 어셈블리 패턴
        return PatternScan(moduleBase, pattern);
    }
    
    // 시그니처 기반 검색
    public nint FindBySignature(string signature) 
    {
        // "48 8B 0D ?? ?? ?? ?? 0F B7 C1" 같은 시그니처로 검색
    }
}
```

#### Option 3: 버전별 오프셋 테이블
```csharp
public static class OffsetTable
{
    private static readonly Dictionary<string, GameOffsets> VersionOffsets = new()
    {
        ["1.16.1"] = new GameOffsets 
        {
            GameState = 0x10B1838,
            PebOffset = 0x828,
            // ...
        },
        ["1.22.0"] = new GameOffsets 
        {
            GameState = 0x10B2000,  // 업데이트된 오프셋
            PebOffset = 0x828,
            // ...
        }
    };
    
    public static GameOffsets GetOffsets(string version) => VersionOffsets[version];
}
```

### 2. 자동 버전 감지 시스템
```csharp
public class GameVersionDetector
{
    public string DetectStarcraftVersion(nint moduleBase)
    {
        // PE 헤더에서 버전 정보 읽기
        // 또는 특정 바이트 시퀀스로 버전 판별
        return "1.16.1";
    }
}
```

### 3. 오프셋 검증 시스템
```csharp
public class OffsetValidator
{
    public bool ValidateOffsets(GameOffsets offsets)
    {
        // 계산된 주소가 유효한 메모리 범위인지 확인
        // 읽어온 값이 예상 범위 내인지 검증
        // 여러 번 읽어서 일관성 있는 값인지 확인
        return IsValidAddress(finalAddress) && IsReasonableValue(inGameValue);
    }
}
```

## 🎯 결론

이번 구현을 통해:

1. **게임 내부 상태 감지** 기능을 완전히 구현하여 로비와 실제 게임 플레이를 정확히 구분
2. **복잡한 메모리 포인터 체인**을 C#으로 정확히 구현하여 스타크래프트 메모리 구조 대응
3. **네이밍 일관성**을 확보하여 코드 가독성과 유지보수성 향상
4. **메모리 접근 최적화**를 통해 성능과 정확성 동시 확보
5. **기존 아키텍처와의 완벽한 통합**으로 확장 가능한 구조 구축

### 다음 단계 로드맵
1. **오프셋 관리 시스템** 구축 (설정 파일 + 패턴 스캐닝)
2. **게임 버전 자동 감지** 기능 추가
3. **오프셋 검증 및 자동 복구** 시스템 구현
4. **다른 게임 상태 감지** 기능 확장 (자원, 유닛 선택 등)

**핵심 성과**: 단순한 네이밍 개선을 넘어서, 실제 게임 해킹에 필요한 복잡한 메모리 로직을 완전히 구현하고, 향후 확장을 위한 명확한 개선 방향을 제시.

---

**작성일**: 2025년 6월 26일  
**프로젝트**: StarcUp (스타크래프트 오버레이)  
**버전**: v3.0 (InGame 상태 감지 시스템)