# 메모리 계층 개선 사항 정리

## 🔧 MemoryReader (Infrastructure Layer)

### 기존 코드 제거
- ❌ `GetTebAddress()` (private 메소드였음)
- ❌ `GetKernel32ModuleInfo()` (복합 로직)
- ❌ `GetTebAddresses()` (비즈니스 로직)
- ❌ `GetStackStart()` / `GetStackTop()` (복합 로직)
- ❌ `GetPebAddress()` (복합 로직)
- ❌ `GetModuleInfo()` (복합 로직)
- ❌ `IsInRange()` (사용되지 않던 유틸 메소드)

### 인터페이스와 정확히 일치하는 메소드들
- ✅ **연결 관리**: `ConnectToProcess()`, `Disconnect()`
- ✅ **타입별 읽기**: `ReadInt()`, `ReadFloat()`, `ReadDouble()` 등
- ✅ **스레드 관련**: `CreateThreadSnapshot()`, `GetFirstThread()`, `GetNextThread()`, `GetThread()`
- ✅ **모듈 관련**: `CreateModuleSnapshot()`, `GetFirstModule()`, `GetNextModule()`, `GetModuleInformation()`
- ✅ **프로세스 관련**: `QueryProcessInformation()`
- ✅ **리소스 관리**: `CloseHandle()`

### 역할
**Windows API를 직접 사용하는 저수준 클래스**
- Windows API와의 직접적인 접촉만 담당
- 에러 처리나 유효성 검사는 최소한으로만 수행
- 단순한 Windows API 래퍼 메소드들만 제공

---

## 🎯 MemoryService (Business Layer)

### 1. 미들웨어 기능
- ✅ **에러 처리**: 모든 메소드에서 null 체크, 예외 처리, 로깅
- ✅ **캐싱**: TEB 목록, 모듈 정보 캐싱으로 성능 향상
- ✅ **유효성 검사**: 연결 상태, 주소 유효성, 매개변수 검증
- ✅ **스레드 안전성**: lock 사용으로 동시 접근 보호

### 2. 고수준 API
- ✅ **복합 로직**: `GetTebAddresses()` - 스레드 순회 + TebInfo 생성
- ✅ **편의 메소드**: `GetKernel32Module()`, `GetUser32Module()`
- ✅ **스택 관리**: `GetStackStart()`, `GetStackTop()` - TEB + 오프셋 계산

### 3. 캐싱 전략
- ~~✅ **TEB 캐시**: 30초 유효 (스레드 정보는 자주 변하지 않음)~~
- ~~✅ **모듈 캐시**: 5분 유효 (모듈 정보는 거의 변하지 않음)~~
- **수동 캐시**: 필요 시 캐시
- ✅ **자동 캐시 무효화**: 연결 해제 시 모든 캐시 정리

### 4. 이벤트 시스템
- ✅ **ProcessConnect** / **ProcessDisconnect** 이벤트
- ✅ **상위 서비스들이 구독 가능**

### 역할
**메모리 작업의 미들웨어 역할을 하는 서비스**
- MemoryReader를 사용하여 복합적인 비즈니스 로직 제공
- null/error 체크, 로깅, 캐싱, 재시도 등의 미들웨어 기능
- 상위 레벨 서비스들이 사용하기 쉬운 고수준 API 제공

---

## 🔄 계층별 역할 분담

### MemoryReader (저수준)
```csharp
// 단순한 Windows API 래퍼만
ReadInt() → 바이트 읽기 + BitConverter
CreateThreadSnapshot() → Windows API 직접 호출
QueryProcessInformation() → raw 상태 코드 반환
```

### MemoryService (미들웨어)
```csharp  
// 비즈니스 로직 + 에러 처리 + 캐싱
ReadInt() → 연결 확인 + 주소 검증 + 로깅 + MemoryReader 호출
GetTebAddresses() → 캐시 확인 → 스레드 순회 → TebInfo 생성 → 캐싱
GetPebAddress() → QueryProcessInformation + 상태 검사 + 로깅
```

---

## 📈 개선 효과

### Before (기존)
- **MemoryReader**: 복합 로직 + Windows API 혼재
- **MemoryService**: 단순한 래퍼 역할만 수행
- **테스트 어려움**: 강한 결합, 복잡한 의존성

### After (개선)
- **MemoryReader**: 순수한 Windows API 래퍼
- **MemoryService**: 진정한 미들웨어 역할
- **완전한 관심사 분리**: 각 계층이 명확한 책임
- **테스트 용이**: 인터페이스 기반 모킹 가능
- **성능 향상**: 캐싱, 유효성 검사
- **안정성 향상**: 에러 처리, 로깅, 스레드 안전성

---

## 🎯 다음 단계

1. **ModuleInfo 클래스** 별도 파일로 분리 완료 ✅
2. **IMemoryService 인터페이스** 개선 완료 ✅  
3. **MemoryService 구현** 완료 ✅
4. **다른 서비스들도 동일한 패턴으로 개선** (진행 예정)

---

**작성일**: 2024년 6월 26일  
**프로젝트**: StarcUp (스타크래프트 오버레이)  
**버전**: v2.0 (메모리 계층 개선)