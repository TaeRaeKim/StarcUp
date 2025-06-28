# 타이머 관리 시스템 개선

## 개요

GameManager에서 통합적으로 업데이트 주기를 관리하도록 타이머 시스템을 개선했습니다. 기존에 각 서비스가 자체 타이머를 가지고 있던 구조에서 GameManager가 중앙에서 모든 업데이트를 제어하는 구조로 변경했습니다.

## 변경 사항

### 1. UnitUpdateService 변경

**기존:**
- 자체 `System.Threading.Timer`를 사용하여 100ms 주기로 자동 업데이트
- `Start()`, `Stop()` 메서드로 타이머 제어
- `UpdateUnits(object state)` private 메서드

**변경 후:**
- 타이머 제거, 수동 업데이트 방식으로 변경
- `UpdateUnits()` public 메서드로 변경하여 외부에서 호출 가능
- `Start()`, `Stop()` 메서드는 초기화/정리 용도로만 사용

### 2. UnitUpdateManager 변경

**추가된 메서드:**
- `UpdatePlayerUnits(byte playerId)`: 특정 플레이어 업데이트
- `UpdateAllPlayerUnits()`: 모든 등록된 플레이어 업데이트

**제거된 메서드:**
- `StartCurrentPlayerUpdates()`: 0번 플레이어 자동 시작 (불필요)
- `GetCurrentPlayerUnits()`: 특정 플레이어 지정 방식으로 통일

### 3. UnitCountService 변경

**기존:**
- 자체 `System.Timers.Timer`를 사용하여 100ms 주기로 자동 업데이트
- `OnTimerElapsed` 이벤트 핸들러에서 자동 업데이트

**변경 후:**
- 타이머 제거, 수동 업데이트 방식으로 변경
- `RefreshData()` 메서드를 외부에서 호출하여 업데이트
- `_isRunning` 필드를 `_isInitialized`로 변경하여 의미 명확화

### 4. GameManager 변경

**타이머 구조:**
- `_unitDataTimer`: 유닛 데이터 로딩 (100ms)
- `_gameDataTimer`: 게임 데이터 로딩 (100ms)  
- `_unitCountTimer`: 유닛 카운트 업데이트 (100ms) - 새로 추가
- `_unitUpdateTimer`: 유닛 업데이트 관리 (100ms) - 새로 추가

**새로운 이벤트 핸들러:**
- `OnUnitCountTimerElapsed`: UnitCountService.RefreshData() 호출
- `OnUnitUpdateTimerElapsed`: UnitUpdateManager.UpdateAllPlayerUnits() 호출

## 장점

1. **중앙 집중식 관리**: 모든 업데이트 주기를 GameManager에서 통합 관리
2. **독립적인 주기 설정**: 각 기능별로 서로 다른 업데이트 주기 설정 가능
3. **메모리 효율성**: 불필요한 타이머 객체 제거로 메모리 사용량 감소
4. **디버깅 용이성**: 업데이트 로직이 중앙화되어 문제 추적이 쉬워짐
5. **확장성**: 새로운 업데이트 기능 추가 시 일관된 패턴 적용 가능

## 주의사항

- 각 타이머는 독립적으로 동작하므로 서로 다른 주기로 설정 가능
- 예외 처리가 각 이벤트 핸들러에 포함되어 있어 한 기능의 오류가 다른 기능에 영향을 주지 않음
- UnitUpdateService와 UnitCountService는 더 이상 자체 타이머를 갖지 않으므로 `Initialize()`/`Stop()` 호출 시 주의

## 테스트 파일 수정

- `UnitUpdateManagerTest.cs`: `StartCurrentPlayerUpdates()` 호출을 `StartPlayerUnitUpdates(0)` 호출로 변경
- `GetCurrentPlayerUnits()` 호출을 `GetLatestPlayerUnits(0)` 호출로 변경

## 파일 목록

### 수정된 파일:
- `StarcUp/Src/Business/Units/Runtime/Services/UnitUpdateService.cs`
- `StarcUp/Src/Business/Units/Runtime/Services/UnitUpdateManager.cs`
- `StarcUp/Src/Business/Units/Runtime/Services/UnitCountService.cs`
- `StarcUp/Src/Business/GameManager/GameManager.cs`
- `StarcUp.Test/Src/Business/Units/Runtime/Services/UnitUpdateManagerTest.cs`

### 생성된 파일:
- `StarcUp/Docs/PullRequest/20250628/timer-management-refactor.md` (이 문서)