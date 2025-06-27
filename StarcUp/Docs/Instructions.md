# Instructions.md

This file provides guidance when working with code in this repository.

## 빌드 및 개발 명령어

### 프로젝트 빌드
```bash
# 전체 솔루션 빌드
dotnet build StarcUp.sln

# 특정 프로젝트 빌드
dotnet build StarcUp/StarcUp.csproj
dotnet build StarcUp.Test/StarcUp.Test.csproj
```

### 테스트 실행
```bash
# 모든 테스트 실행
dotnet test StarcUp.Test/StarcUp.Test.csproj

# 상세 출력으로 테스트 실행
dotnet test StarcUp.Test/StarcUp.Test.csproj --verbosity normal

# 특정 테스트 클래스 실행
dotnet test StarcUp.Test/StarcUp.Test.csproj --filter "ClassName=UnitServiceTest"
```

### 애플리케이션 실행
```bash
# 메인 애플리케이션 실행
dotnet run --project StarcUp/StarcUp.csproj
```

## 아키텍처 개요

StarcUp은 스타크래프트의 메모리를 실시간으로 모니터링하는 Windows Forms 애플리케이션입니다. 의존성 주입을 사용한 계층형 아키텍처를 따릅니다.

### 주요 컴포넌트

#### Infrastructure 계층 (`Src/Infrastructure/`)
- **MemoryReader**: Windows API 기반의 핵심 메모리 읽기 기능 (최적화 포함)
- **WindowManager**: 스타크래프트 프로세스의 윈도우 감지 및 관리
- **ProcessEventMonitor**: 프로세스 생명주기 이벤트 모니터링

#### Business 계층 (`Src/Business/`)
- **GameDetector**: 스타크래프트 프로세스 감지 및 게임 상태 확인
- **InGameDetector**: 메모리 분석을 통한 인게임 상태 모니터링
- **MemoryService**: 고수준 메모리 접근 추상화
- **Units**: 스타크래프트 유닛 관리 도메인 로직
  - Runtime: 메모리에서 읽어온 실시간 유닛 데이터
  - StaticData: 유닛 메타데이터 및 설정 정보

#### Presentation 계층 (`Src/Presentation/`)
- **ControlForm**: 테스트 및 모니터링용 메인 UI
- **OverlayNotificationForm**: 게임 오버레이 기능

### 메모리 아키텍처
- 성능이 중요한 메모리 작업을 위해 unsafe 코드 블록 사용
- GC 압박을 줄이기 위한 바이트 배열 객체 풀링 구현
- 기본/최적화 메모리 읽기 경로 모두 지원
- 적절한 리소스 관리를 통한 스레드 안전 메모리 작업

### 유닛 시스템
- **UnitRaw**: 게임 유닛의 직접적인 메모리 표현
- **Unit**: 계산된 속성을 포함한 비즈니스 도메인 모델
- **UnitInfo**: 유닛 타입에 대한 정적 메타데이터
- **UnitService**: 고수준 유닛 작업 및 필터링

### 의존성 주입
`ServiceRegistration.cs`에서 커스텀 `ServiceContainer`를 사용하여 서비스 등록:
- 모든 서비스는 싱글톤으로 등록
- 생성자 주입을 통한 의존성 해결
- 리소스 정리를 위한 적절한 Disposal 체인

## 테스트 프레임워크
- xUnit을 사용한 단위 테스트
- 읽기 쉬운 테스트 어설션을 위한 FluentAssertions
- 추가 어설션 기능을 위한 Shouldly
- 테스트 데이터는 `StarcUp.Test/data/`에 위치

## 커밋 가이드라인
`StarcUp/Docs/commit-guidelines.md`에 정의된 프로젝트 커밋 메시지 형식을 따라주세요:
- `[Feat]`, `[Fix]`, `[Perf]`, `[Refactor]`, `[Test]`, `[Docs]` 등의 접두사 사용
- 영어로 간결한 제목 작성 (50자 이내)
- 현재형 및 명령형 어조 사용

## 개발 시 주의사항

### 메모리 안전성
- 성능이 중요한 메모리 작업을 위해 `AllowUnsafeBlocks=true` 사용
- 메모리 주소 읽기 전 항상 유효성 검증
- 비관리 리소스에 대한 적절한 disposal 패턴 사용

### 게임 프로세스 상호작용
- 스타크래프트 프로세스의 메모리 읽기를 위해 관리자 권한 필요
- 프로세스 생명주기 이벤트 처리 (시작/종료/크래시)
- 성능을 위해 자주 접근하는 메모리 주소 캐싱

### 유닛 데이터 플로우
1. **감지**: GameDetector가 스타크래프트 프로세스 발견
2. **메모리 접근**: MemoryService가 연결 설정
3. **주소 해결**: InGameDetector가 유닛 배열 베이스 주소 찾기
4. **데이터 로딩**: UnitMemoryAdapter가 원시 유닛 데이터 읽기
5. **비즈니스 로직**: UnitService가 필터링되고 타입이 지정된 유닛 접근 제공