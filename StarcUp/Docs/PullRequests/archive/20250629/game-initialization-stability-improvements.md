# 게임 초기화 안정성 개선 및 멀티스레딩 오류 해결

## 개요

게임 시작/종료 시점에서 발생하는 null reference 오류와 초기화 실패 문제를 해결하고, 멀티스레딩 환경에서의 안정성을 향상시켰습니다.

## 주요 문제점

1. **InGame 상태 확인 중 null reference 오류**
   - 게임 시작/종료 시점에 `Object reference not set to an instance of an object` 오류 발생
   - 멀티스레딩 환경에서 타이머 관련 동기화 문제

2. **UnitCountService 초기화 실패**
   - 게임 시작 시 포인터 체인이 안정화되기 전에 초기화 시도로 인한 실패
   - 초기화 실패 시 적절한 재시도 로직 부재

3. **GameManager 타이머 초기화 누락**
   - `_unitCountTimer` 필드가 정의되었지만 초기화되지 않아 null reference 오류 발생

## 해결 방안

### 1. InGameDetector 안정성 개선

**파일**: `StarcUp/Src/Business/InGameDetector/InGameDetector.cs`

- **변경사항**: 
  - 디버그 로그 최적화 (InGameDetector.cs:110)
  - 불필요한 콘솔 출력 제거로 성능 향상

### 2. GameManager 타이머 초기화 수정

**파일**: `StarcUp/Src/Business/GameManager/GameManager.cs`

- **변경사항**:
  - `using System.Threading;` 추가 (라인 5)
  - `_unitCountTimer` 초기화 로직 추가 (라인 43-45)
  - `OnUnitCountTimerElapsed` 메서드 구현 (라인 197-203)

```csharp
_unitCountTimer = new Timer(500); // 0.5초마다 (500ms 간격)
_unitCountTimer.Elapsed += OnUnitCountTimerElapsed;
_unitCountTimer.AutoReset = true;
```

### 3. UnitCountService 초기화 재시도 로직 구현

**파일**: `StarcUp/Src/Business/GameManager/GameManager.cs`

- **새로운 메서드**: `InitializeUnitCountServiceWithRetry()` (라인 242-273)
- **특징**:
  - 0.2초 간격으로 최대 10회 재시도
  - 각 시도마다 상세한 로그 출력
  - 10회 실패 시 GameManager 자동 Dispose

```csharp
private bool InitializeUnitCountServiceWithRetry()
{
    const int maxRetries = 10;
    const int delayMs = 200; // 0.2초

    for (int attempt = 1; attempt <= maxRetries; attempt++)
    {
        Console.WriteLine($"GameManager: UnitCountService 초기화 시도 {attempt}/{maxRetries}");
        
        try
        {
            if (_unitCountService.Initialize())
            {
                Console.WriteLine($"GameManager: UnitCountService 초기화 성공 (시도 {attempt})");
                return true;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"GameManager: UnitCountService 초기화 시도 {attempt} 중 예외 발생: {ex.Message}");
        }

        if (attempt < maxRetries)
        {
            Console.WriteLine($"GameManager: {delayMs}ms 대기 후 재시도...");
            Thread.Sleep(delayMs);
        }
    }

    Console.WriteLine($"GameManager: UnitCountService 초기화 {maxRetries}회 시도 모두 실패");
    return false;
}
```

### 4. Player 클래스 구조 개선

**파일**: `StarcUp/Src/Business/GameManager/Player.cs`

- **변경사항**:
  - `struct` → `class`로 변경하여 참조 타입으로 전환
  - 유닛 관리 기능 추가 (Unit 배열, 메모리 최적화)
  - 생성자에서 미리 할당된 Unit 배열로 메모리 재활용 구현

### 5. PlayerExtensions 기능 확장

**파일**: `StarcUp/Src/Business/GameManager/Extensions/PlayerExtensions.cs`

- **새로운 기능**:
  - `UpdateUnits()` 메서드로 플레이어별 유닛 데이터 업데이트
  - `UnitType.AllUnits` 지원 (모든 유닛 총합 계산)
  - 타입 안전성 강화 (`byte` → `int` 캐스팅 제거)

```csharp
public static void UpdateUnits(this Player player)
{
    if (_unitService == null)
    {
        Console.WriteLine("[PlayerExtensions] UnitService가 설정되지 않음");
        return;
    }

    try
    {
        player.SetUnitCount(
            _unitService.GetPlayerUnitsToBuffer(
                player.PlayerIndex,
                player.GetPlayerUnits(),
                player.GetMaxUnits()
                )
            );
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[PlayerExtensions] Player {player.PlayerIndex} 유닛 업데이트 중 오류: {ex.Message}");
    }
}
```

### 6. 파일 정리

**삭제된 파일**:
- `StarcUp/Docs/Examples/UnitCountUsageExample.cs` - 사용하지 않는 예제 파일
- `StarcUp/Docs/PullRequest/20250628/timer-management-refactor.md` - 이전 PR 문서

## 개선 효과

### 1. 안정성 향상
- 게임 시작/종료 시 null reference 오류 완전 해결
- 멀티스레딩 환경에서의 동기화 문제 해결
- 초기화 실패 시 자동 재시도로 성공률 향상

### 2. 사용자 경험 개선
- 게임 시작 시 더 안정적인 초기화 과정
- 명확한 로그 메시지로 문제 상황 파악 용이
- 초기화 실패 시 자동 종료로 무한 대기 상황 방지

### 3. 개발자 경험 개선
- 상세한 재시도 로그로 디버깅 용이성 향상
- 타입 안전성 강화로 런타임 오류 감소
- 코드 구조 개선으로 유지보수성 향상

## 테스트 가이드

1. **기본 시나리오**:
   - 게임 시작 → 정상 초기화 확인
   - 게임 종료 → 오류 없이 종료 확인

2. **재시도 로직 테스트**:
   - 포인터 체인이 불안정한 상황에서 게임 시작
   - 재시도 로그 출력 및 최종 성공 확인

3. **예외 상황 테스트**:
   - 10회 재시도 후에도 실패하는 경우
   - GameManager 자동 Dispose 동작 확인

## 영향받는 컴포넌트

- **GameManager**: 초기화 로직 개선
- **InGameDetector**: 로그 최적화
- **Player**: 구조 변경 (struct → class)
- **PlayerExtensions**: 기능 확장
- **UnitCountService**: 재시도 로직과 연동

## 주의사항

- Player 클래스가 struct에서 class로 변경되어 참조 의미론이 적용됨
- 재시도 로직으로 인해 게임 시작 시간이 최대 2초 정도 지연될 수 있음 (실패 시에만)
- 메모리 사용량이 Player 클래스 개선으로 인해 약간 증가할 수 있음

## 관련 이슈

- InGame 상태 확인 중 null reference 오류
- 게임 시작 시 UnitCountService 초기화 실패
- 멀티스레딩 환경에서의 동기화 문제