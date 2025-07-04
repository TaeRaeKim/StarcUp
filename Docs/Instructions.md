# Instructions.md

## 프로젝트 개요

StarcUp은 스타크래프트의 메모리를 실시간으로 모니터링하는 애플리케이션입니다. Core/UI 분리 아키텍처를 통해 비즈니스 로직과 UI를 분리하고, WinUI 3과 Windows Forms를 모두 지원합니다.

## 프로젝트 구조

### 4개 프로젝트 구성
- **StarcUp.Core** (.NET 8.0-windows): 비즈니스 로직 및 인프라스트럭처
- **StarcUp.UI** (.NET 9.0-windows): 신규 WinUI 3 애플리케이션  
- **StarcUp** (.NET 8.0-windows): 기존 Windows Forms 애플리케이션
- **StarcUp.Test** (.NET 8.0-windows): 단위 테스트 프로젝트

### 계층형 아키텍처 (StarcUp.Core 기준)
의존성 주입을 사용한 계층형 아키텍처를 따릅니다.

### 주요 컴포넌트

#### Infrastructure 계층 (`Src/Infrastructure/`)
- **Memory**
  - **MemoryReader**: Windows API 기반의 핵심 메모리 읽기 기능 (최적화 포함)
  - **MemoryStructures**: 메모리 구조체 정의
- **Windows** 
  - **WindowManager**: 스타크래프트 프로세스의 윈도우 감지 및 관리
  - **WindowInfo**: 윈도우 정보 모델
  - **WindowsAPI**: Windows API 래퍼
- **Process**
  - **ProcessEventMonitor**: 프로세스 생명주기 이벤트 모니터링

#### Business 계층 (`Src/Business/`)
- **GameDetector**: 스타크래프트 프로세스 감지 및 게임 상태 확인
  - **GameInfo**: 게임 정보 모델
- **GameManager**: 게임 상태 및 플레이어 정보 관리
  - **Player**: 플레이어 정보 모델
  - **LocalGameData**: 로컬 게임 데이터
  - **PlayerExtensions**: 플레이어 관련 확장 메서드
- **InGameDetector**: 메모리 분석을 통한 인게임 상태 모니터링
- **MemoryService**: 고수준 메모리 접근 추상화
  - **ModuleInfo**: 모듈 정보 모델
  - **TebInfo**: Thread Environment Block 정보
- **Units**: 스타크래프트 유닛 관리 도메인 로직
  - **Runtime**: 메모리에서 읽어온 실시간 유닛 데이터
    - **Adapters**: UnitMemoryAdapter, UnitCountAdapter - 메모리 접근 추상화
    - **Models**: Unit, UnitRaw, UnitCount, UnitCountRaw 등 유닛 모델
    - **Services**: UnitService, UnitCountService - 고수준 유닛 작업
    - **Repositories**: UnitOffsetRepository - 오프셋 데이터 관리
  - **StaticData**: 유닛 메타데이터 및 설정 정보
    - **Models**: UnitInfo, UnitsContainer - 정적 유닛 정보
    - **Repositories**: UnitInfoRepository - 유닛 메타데이터 관리
  - **Types**: UnitTypeExtensions - 유닛 타입 확장

#### Common 계층 (`Src/Common/`)
- **Constants**: GameConstants - 게임 관련 상수
- **Events**: 이벤트 모델 (GameEventArgs, InGameEventArgs, ProcessEventArgs)

#### Presentation 계층 (`Src/Presentation/`)
- **Forms**
  - **ControlForm**: 테스트 및 모니터링용 메인 UI
  - **OverlayNotificationForm**: 게임 오버레이 기능

#### Dependency Injection (`Src/DependencyInjection/`)
- **ServiceContainer**: 커스텀 DI 컨테이너
- **ServiceRegistration**: 서비스 등록 관리

### 메모리 아키텍처
- 성능이 중요한 메모리 작업을 위해 unsafe 코드 블록 사용
- GC 압박을 줄이기 위한 바이트 배열 객체 풀링 구현
- 기본/최적화 메모리 읽기 경로 모두 지원
- 적절한 리소스 관리를 통한 스레드 안전 메모리 작업

### 유닛 시스템
- **UnitRaw**: 게임 유닛의 직접적인 메모리 표현
- **Unit**: 계산된 속성을 포함한 비즈니스 도메인 모델
- **UnitInfo**: 유닛 타입에 대한 정적 메타데이터
- **UnitCount/UnitCountRaw**: 유닛 카운트 정보 (원시/가공)
- **UnitService**: 고수준 유닛 작업 및 필터링
- **UnitCountService**: 유닛 카운트 관리 및 재시도 로직
- **UnitMemoryAdapter**: 메모리에서 유닛 데이터 읽기 추상화
- **UnitCountAdapter**: 메모리에서 유닛 카운트 읽기 추상화
- **UnitOffsetRepository**: 유닛 오프셋 설정 관리

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

## 플랫폼 요구사항

### 개발 환경
- **.NET 8.0-windows**: StarcUp.Core, StarcUp, StarcUp.Test
- **.NET 9.0-windows**: StarcUp.UI (WinUI 3)
- **Windows 전용**: Linux/macOS에서는 실행 불가
- **관리자 권한 필수**: 다른 프로세스 메모리 읽기를 위해 필요
- **스타크래프트 의존성**: 실행 중인 스타크래프트 프로세스 필요

### 주요 NuGet 패키지
- **System.Management**: WMI를 통한 프로세스 모니터링
- **Microsoft.WindowsAppSDK**: WinUI 3 지원 (StarcUp.UI)
- **Microsoft.Web.WebView2**: 웹 뷰 지원
- **xUnit + FluentAssertions + Shouldly + Moq**: 테스트 프레임워크

## 개발 가이드라인

### 인터페이스 우선 설계
**모든 서비스와 어댑터 클래스는 반드시 인터페이스를 먼저 정의하고 구현해야 합니다.**

```csharp
// 1. 인터페이스 먼저 정의
public interface IUnitCountAdapter : IDisposable
{
    bool LoadAllUnitCounts();
    int GetUnitCount(UnitType unitType, byte playerIndex);
}

// 2. 구현 클래스
public class UnitCountAdapter : IUnitCountAdapter
{
    // 구현...
}
```

### 의존성 주입 시스템
**커스텀 ServiceContainer 사용 (표준 Microsoft DI가 아님)**

- 모든 서비스는 싱글톤으로 등록됨
- `ServiceRegistration.cs`에서 서비스 등록 관리
- 생성자 주입을 통한 의존성 해결
- 적절한 Disposal 체인을 통한 리소스 정리

### 메모리 안전성 가이드라인
```csharp
// unsafe 블록 사용 예시
unsafe
{
    fixed (byte* ptr = buffer)
    {
        // 직접 메모리 접근
        var value = *(int*)ptr;
    }
}

// 바이트 배열 객체 풀링
using var pooledBuffer = _bufferPool.Get();
var buffer = pooledBuffer.Array;
```

### 개발 시 주의사항

#### 메모리 안전성
- 성능이 중요한 메모리 작업을 위해 `AllowUnsafeBlocks=true` 사용
- 메모리 주소 읽기 전 항상 유효성 검증
- 비관리 리소스에 대한 적절한 disposal 패턴 사용

#### 게임 프로세스 상호작용
- 스타크래프트 프로세스의 메모리 읽기를 위해 관리자 권한 필요
- 프로세스 생명주기 이벤트 처리 (시작/종료/크래시)
- 성능을 위해 자주 접근하는 메모리 주소 캐싱

#### UI 프레임워크 선택
- **새로운 기능**: WinUI 3 (StarcUp.UI) 우선 개발
- **레거시 유지보수**: Windows Forms (StarcUp) 기존 기능 유지
- **공통 로직**: StarcUp.Core에서 비즈니스 로직 구현

### 유닛 데이터 플로우
1. **게임 감지**: GameDetector가 스타크래프트 프로세스 발견
2. **게임 관리**: GameManager가 게임 상태 및 플레이어 정보 관리
3. **메모리 접근**: MemoryService가 연결 설정
4. **인게임 감지**: InGameDetector가 유닛 배열 베이스 주소 찾기
5. **데이터 어댑터**: 
   - UnitMemoryAdapter가 원시 유닛 데이터 읽기
   - UnitCountAdapter가 유닛 카운트 데이터 읽기
6. **비즈니스 서비스**: 
   - UnitService가 필터링되고 타입이 지정된 유닛 접근 제공
   - UnitCountService가 유닛 카운트 관리 및 재시도 로직 처리
7. **정적 데이터**: UnitInfoRepository가 유닛 메타데이터 제공

## 빌드 및 실행 명령어

### Makefile을 통한 빌드 (권장)

#### 전체 빌드
```bash
make build          # 전체 솔루션 빌드
```

#### 특정 프로젝트 빌드
```bash
make build-core     # StarcUp.Core 프로젝트만 빌드
make build-ui       # StarcUp.UI 프로젝트만 빌드  
make build-legacy   # StarcUp (Windows Forms) 프로젝트만 빌드
make build-test     # StarcUp.Test 프로젝트만 빌드
```

#### 테스트 및 유지보수
```bash
make test           # 테스트 실행
make clean          # 빌드 출력 정리
make restore        # NuGet 패키지 복원
make dev-setup      # 개발 환경 설정
```

#### 실행 명령어 (참조용)
```bash
make run-ui         # WinUI 3 앱 실행 명령어 출력
make run-legacy     # Windows Forms 앱 실행 명령어 출력
```

### 직접 dotnet 명령어

#### 빌드
```bash
# 전체 솔루션 빌드
dotnet build StarcUp.sln

# 특정 프로젝트 빌드
dotnet build StarcUp.Core/StarcUp.Core.csproj
dotnet build StarcUp.UI/StarcUp.UI.csproj
dotnet build StarcUp/StarcUp.csproj
dotnet build StarcUp.Test/StarcUp.Test.csproj

# Release 모드 빌드
dotnet build StarcUp.sln --configuration Release
```

#### 테스트
```bash
# 모든 테스트 실행
dotnet test StarcUp.Test/StarcUp.Test.csproj

# 상세 출력으로 테스트 실행
dotnet test StarcUp.Test/StarcUp.Test.csproj --verbosity normal

# 특정 테스트 클래스 실행
dotnet test StarcUp.Test/StarcUp.Test.csproj --filter "ClassName=UnitServiceTest"

# 특정 테스트 메서드 실행
dotnet test StarcUp.Test/StarcUp.Test.csproj --filter "MethodName=ShouldLoadUnits"
```

#### 애플리케이션 실행
```bash
# WinUI 3 앱 실행
dotnet run --project StarcUp.UI/StarcUp.UI.csproj

# Windows Forms 앱 실행 (관리자 권한 필요)
dotnet run --project StarcUp/StarcUp.csproj
```

### Claude Code 환경에서 빌드
```bash
# SSH를 통한 Makefile 사용 (권장)
ssh Taerae@main "cd StarcUp && make build"
ssh Taerae@main "cd StarcUp && make build-core"
ssh Taerae@main "cd StarcUp && make test"

# SSH를 통한 직접 dotnet 명령어
ssh Taerae@main "cd StarcUp && dotnet build StarcUp.sln"
ssh Taerae@main "cd StarcUp && dotnet test StarcUp.Test/StarcUp.Test.csproj"
```

## UI 프레임워크

### WinUI 3 (StarcUp.UI)
- **최신 Windows 애플리케이션 플랫폼**: XAML 기반 현대적 UI
- **Discord 스타일 다크 테마**: 완전한 다크 모드 지원
- **MSIX 패키징**: Windows Store 배포 지원
- **주요 페이지**:
  - **MainPage**: 메인 애플리케이션 페이지
  - **ControlTestPage**: 테스트 및 디버깅용 페이지

### Windows Forms (StarcUp)
- **레거시 지원**: 기존 기능 유지보수
- **빠른 개발**: 프로토타이핑 및 테스트용
- **주요 폼**:
  - **ControlForm**: 메인 모니터링 UI
  - **OverlayNotificationForm**: 게임 오버레이 기능

## 데이터 파일 구조

### JSON 설정 파일 (`StarcUp/Data/`)
- **starcraft_units.json**: 유닛 메타데이터
- **all_race_unit_offsets.json**: 유닛 오프셋 설정
- **protoss_building_offsets.json**: 프로토스 건물 오프셋

### CT 파일 (Cheat Engine)
- **TerranOffset.CT**: 테란 유닛 메모리 주소
- **ProtossOffset.CT**: 프로토스 유닛 메모리 주소  
- **ZergOffset.CT**: 저그 유닛 메모리 주소

### 리소스 파일
- **images/**: 종족별 유닛 이미지 (Terran, Protoss, Zerg)
- **cmdicons/**: 유닛 아이콘 파일들

### 테스트 데이터 (`StarcUp.Test/data/`)
- **starcraft_units.json**: 테스트용 유닛 데이터

## Windows에서 Make 설치 및 빌드 시스템 가이드

빌드 시스템 사용법과 Windows에서 Make 설치 방법에 대한 상세한 내용은 다음 문서를 참조하세요:

📄 **[빌드 시스템 개선 및 Make 설치 가이드](PullRequests/archive/20250702/build-system-improvements.md)**

이 문서에는 다음 내용이 포함되어 있습니다:
- Makefile 명령어 체계 개선사항
- 특정 프로젝트 빌드 방법
- Chocolatey를 통한 Make 설치 가이드
- WSL Claude 환경과 로컬 Windows 환경별 사용법