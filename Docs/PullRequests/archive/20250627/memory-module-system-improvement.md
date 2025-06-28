# 메모리 모듈 시스템 개선 및 GameManager 구현

## 📋 개요

StarcUp 프로젝트의 메모리 모듈 검색 시스템을 치트엔진 방식으로 통일하고, GameManager를 통한 자동 게임 데이터 로딩 시스템을 구현했습니다.

## 🚨 기존 문제점

### 1. 이중 모듈 검색 시스템
- **스냅샷 방식**: `CreateModuleSnapshot` + `GetFirstModule`/`GetNextModule`
- **치트엔진 방식**: `EnumProcessModules` (더 안정적)
- **InGameDetector**: 이미 치트엔진 방식 사용
- **MemoryService**: 스냅샷 방식 사용으로 모듈 검색 실패

### 2. GameTime/LocalPlayerIndex 읽기 실패
- kernel32.dll 모듈을 찾지 못해 GameTime 읽기 실패
- StarCraft.exe 모듈을 찾지 못해 LocalPlayerIndex 읽기 실패

## ✅ 해결 방안

### 1. 모듈 검색 시스템 통일

**FindModule 메서드를 치트엔진 방식으로 완전 교체:**

```csharp
// 기존 (스냅샷 방식)
public bool FindModule(string moduleName, out ModuleInfo moduleInfo)
{
    // CreateModuleSnapshot 사용
    // GetFirstModule/GetNextModule 사용
    // 불안정하고 모듈을 찾지 못함
}

// 개선 (치트엔진 방식)
public bool FindModule(string moduleName, out ModuleInfo moduleInfo)
{
    var result = FindModuleCheatEngineStyle(moduleName);
    if (result != null)
    {
        moduleInfo = result;
        return true;
    }
    return false;
}
```

### 2. GameManager 구현

**자동 게임 데이터 관리 시스템:**

```csharp
public class GameManager : IGameManager, IDisposable
{
    private readonly IInGameDetector _inGameDetector;
    private readonly IUnitService _unitService;
    private readonly IMemoryService _memoryService;
    private readonly Timer _unitDataTimer;    // 100ms (1초에 10번)
    private readonly Timer _gameDataTimer;    // 500ms (0.5초마다)
}
```

**주요 기능:**
- **자동 초기화**: InGame 감지 시 GameManager 자동 시작
- **LocalPlayerIndex**: 게임 시작 시 1회 로드
- **GameTime**: 0.5초마다 지속 업데이트 (프레임 → 초 변환)
- **Units Data**: 1초에 10번 갱신 (기존 유지)

### 3. 스택 메서드 명확화

**TEB 기반 스택 주소 메서드 이름 수정:**

| 기존 메서드 | 새 메서드 | TEB 오프셋 | 설명 |
|------------|-----------|-----------|------|
| `GetStackStart()` | `GetStackTop()` | TEB + 0x08 | 스택 상단 (높은 주소) |
| `GetStackTop()` | `GetStackBottom()` | TEB + 0x10 | 스택 하단 (낮은 주소) |

**새로운 치트엔진 방식 스택 주소:**
```csharp
GetThreadStackAddress(int threadIndex)  // kernel32 기반 GameTime용
```

## 🏗️ 구현 세부사항

### 1. 메모리 서비스 개선

**모든 모듈 검색이 치트엔진 방식 사용:**
- `GetKernel32Module()`: kernel32.dll 검색
- `GetUser32Module()`: user32.dll 검색
- `ReadLocalPlayerIndex()`: StarCraft.exe 검색
- `ReadGameTime()`: GetThreadStackAddress() 사용

### 2. GameManager 동작 흐름

```
InGame 감지 → GameManager.GameInit()
    ├── LocalPlayerIndex 로드 (1회)
    ├── UnitService 초기화
    ├── 유닛 데이터 타이머 시작 (100ms)
    └── 게임 데이터 타이머 시작 (500ms)

게임 종료 감지 → GameManager.GameExit()
    ├── 모든 타이머 중지
    └── 캐시 무효화
```

### 3. 메모리 읽기 구조

**LocalPlayerIndex 읽기:**
```
StarCraft.exe + 0xDD5B5C → Int32 값
```

**GameTime 읽기:**
```
GetThreadStackAddress(0) → THREADSTACK0
THREADSTACK0 - 0x520 → BaseAddress
[BaseAddress] → PointerAddress  
[PointerAddress + 0x14C] → GameTime (프레임)
GameTime / 24 → 초 단위 변환
```

## 🎯 개선 결과

### 1. 안정성 향상
- ✅ 모든 모듈 검색이 치트엔진 방식으로 통일
- ✅ kernel32.dll, user32.dll, StarCraft.exe 안정적 검색
- ✅ GameTime, LocalPlayerIndex 정상 읽기

### 2. 자동화 구현  
- ✅ InGame 감지 시 자동 초기화
- ✅ 실시간 게임 데이터 업데이트
- ✅ 게임 종료 시 자동 정리

### 3. 코드 일관성
- ✅ 모든 모듈 검색 로직 통일
- ✅ 메서드 이름과 실제 동작 일치
- ✅ 명확한 책임 분리

## 📊 변경된 파일

### 핵심 파일
- `IMemoryService.cs`: 인터페이스 메서드 문서화
- `MemoryService.cs`: FindModule 치트엔진 방식 교체
- `GameManager.cs`: 완전 재구현
- `IGameManager.cs`: IDisposable 추가
- `ServiceRegistration.cs`: GameManager 의존성 주입 업데이트

### 문서
- `stack-methods-refactoring.md`: 스택 메서드 리팩토링 문서
- `memory-module-system-improvement.md`: 이 문서

## 🧪 테스트 결과

### 정상 동작 확인
- ✅ InGame 감지 → GameManager 자동 시작
- ✅ LocalPlayerIndex 정상 읽기
- ✅ GameTime 0.5초마다 정상 업데이트
- ✅ Units Data 1초에 10번 정상 갱신
- ✅ 게임 종료 시 자동 정리

### 성능 최적화
- ✅ 모듈 캐싱으로 반복 검색 방지
- ✅ 주소 캐싱으로 포인터 계산 최적화
- ✅ 타이머 분리로 적절한 갱신 주기

## 🔧 기술적 세부사항

### Windows API 사용
- `EnumProcessModules`: 안정적인 모듈 열거
- `GetModuleFileNameEx`: 모듈 이름 추출
- `GetModuleInformation`: 모듈 크기 정보

### TEB (Thread Environment Block) 활용
- TEB + 0x08: StackBase (스택 상단)
- TEB + 0x10: StackLimit (스택 하단)
- kernel32 기반 치트엔진 방식 스택 주소 계산

### 메모리 안전성
- ObjectDisposed 예외 처리
- 리소스 자동 정리 (IDisposable)
- 캐시 무효화를 통한 메모리 누수 방지

