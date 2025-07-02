# Core/UI 프로젝트 분리 및 WinUI 3 마이그레이션 시작

**날짜**: 2025-07-02  
**작업 유형**: 프로젝트 구조 개선, WinUI 3 마이그레이션

## 📋 작업 개요

기존 Windows Forms 프로젝트를 Core/UI로 분리하고 WinUI 3 마이그레이션을 위한 기반을 구축했습니다.

## 🏗️ 새로운 프로젝트 구조

### 프로젝트 분리 결과
```
StarcUp.sln
├── StarcUp (기존 Windows Forms)
├── StarcUp.Core (새로 생성 - 비즈니스 로직)
├── StarcUp.UI (새로 생성 - WinUI 3)
└── StarcUp.Test (기존 테스트)
```

### 1. StarcUp.Core (Class Library)
- **타겟 프레임워크**: .NET 8.0-windows
- **특징**: `AllowUnsafeBlocks=true`, `System.Management` 패키지 포함
- **역할**: 플랫폼 독립적 비즈니스 로직

#### 이동된 폴더 구조
```
StarcUp.Core/Src/
├── Business/
│   ├── GameDetector/
│   ├── GameManager/
│   ├── InGameDetector/
│   ├── MemoryService/
│   └── Units/
├── Common/
│   ├── Constants/
│   └── Events/
├── DependencyInjection/
└── Infrastructure/
```

#### 포함된 핵심 컴포넌트
- **GameDetector**: 하이브리드 스타크래프트 감지 시스템
- **MemoryService**: Windows API 기반 메모리 읽기/쓰기
- **InGameDetector**: 게임 내 상태 감지
- **UnitService**: 유닛 관련 비즈니스 로직
- **ServiceContainer**: 커스텀 DI 컨테이너

### 2. StarcUp.UI (WinUI 3)
- **타겟 프레임워크**: .NET 8.0-windows (WinUI 3)
- **역할**: 모던 사용자 인터페이스
- **참조**: StarcUp.Core 프로젝트 참조 설정 완료

#### 프로젝트 구조
```
StarcUp.UI/
├── Views/
│   ├── MainPage.xaml
│   └── MainPage.xaml.cs
├── Assets/
├── Properties/
├── App.xaml
├── App.xaml.cs
├── Package.appxmanifest
└── StarcUp.UI.csproj
```

## 🔧 수행한 작업

### 1. WinUI 3 템플릿 설치 및 프로젝트 생성
```bash
# SSH를 통한 템플릿 설치
ssh Taerae@main "dotnet new install VijayAnand.WinUITemplates"

# WinUI 3 프로젝트 생성
ssh Taerae@main "cd StarcUp && dotnet new winui -n StarcUp.UI"
```

### 2. 파일 구조 정리
- 최상위 폴더에 생성된 WinUI 3 파일들을 `StarcUp.UI/` 폴더로 이동
- 중복 솔루션 파일 정리
- 메인 솔루션에 새 프로젝트들 추가

### 3. Core 프로젝트 설정
```xml
<PropertyGroup>
  <TargetFramework>net8.0-windows</TargetFramework>
  <AllowUnsafeBlocks>true</AllowUnsafeBlocks>
</PropertyGroup>
<ItemGroup>
  <PackageReference Include="System.Management" Version="9.0.6" />
</ItemGroup>
```

### 4. 비즈니스 로직 이동
- `cp -r StarcUp/Src/Business StarcUp.Core/Src/`
- `cp -r StarcUp/Src/Common StarcUp.Core/Src/`
- `cp -r StarcUp/Src/DependencyInjection StarcUp.Core/Src/`
- `cp -r StarcUp/Src/Infrastructure StarcUp.Core/Src/`

### 5. 프로젝트 참조 설정
```bash
# 솔루션에 프로젝트 추가
ssh Taerae@main "cd StarcUp && dotnet sln add StarcUp.UI/StarcUp.UI.csproj"
ssh Taerae@main "cd StarcUp && dotnet sln add StarcUp.Core/StarcUp.Core.csproj"

# UI에서 Core 참조
ssh Taerae@main "cd StarcUp && dotnet add StarcUp.UI/StarcUp.UI.csproj reference StarcUp.Core/StarcUp.Core.csproj"
```

## ✅ 빌드 검증

### StarcUp.Core 빌드 성공
```
StarcUp.Core -> C:\Users\Taerae\StarcUp\StarcUp.Core\bin\Debug\net8.0-windows\StarcUp.Core.dll
경고 2개, 오류 0개
```

### 솔루션 프로젝트 목록
```
StarcUp.Core\StarcUp.Core.csproj
StarcUp.Test\StarcUp.Test.csproj
StarcUp.UI\StarcUp.UI.csproj
StarcUp\StarcUp.csproj
```

## 🎨 WinUI 3 UI 변환 시작

### MainPage.xaml 구조 설계
기존 ControlForm의 5개 그룹을 WinUI 3 레이아웃으로 변환:

1. **감지 상태 그룹** → `Border` + `StackPanel`
2. **게임 모니터링 그룹** → `Border` + `Grid` 레이아웃
3. **오버레이 상태 그룹** → `Border` + 버튼 그룹
4. **유닛 테스트 그룹** → `Border` + `NumberBox`, `ComboBox`
5. **메모리 정보 그룹** → `Border` + `ListBox`

### 주요 개선사항
- **모던 레이아웃**: `Border`의 `CornerRadius` 사용
- **반응형 디자인**: `Grid`와 `StackPanel` 조합
- **스크롤 지원**: `ScrollViewer`로 전체 래핑
- **Fluent Design**: WinUI 3 네이티브 스타일링

## 🚀 다음 단계

1. **MainPage.xaml.cs 구현**
   - 기존 ControlForm 로직을 WinUI 3에 맞게 이식
   - 이벤트 핸들러 구현
   - StarcUp.Core 서비스 의존성 주입

2. **오버레이 시스템 개선**
   - Win2D 통합 검토
   - GPU 가속 렌더링 적용

3. **테스트 및 검증**
   - 기존 기능 동등성 확인
   - 성능 비교 테스트

## 📈 기대 효과

- **코드 재사용성**: Core 로직을 여러 UI에서 공유 가능
- **유지보수성**: 비즈니스 로직과 UI 관심사 분리
- **모던 UX**: WinUI 3의 Fluent Design 적용
- **성능 향상**: GPU 가속 렌더링 기반

## 🔧 기술적 개선사항

### SSH 명령 가이드라인 강화
CLAUDE.md에 SSH 사용 지침을 추가:
- **🚨 중요**: 모든 dotnet 명령은 SSH 필수
- **⚠️ 절대 금지**: 로컬에서 dotnet 명령 실행

### 프로젝트 구조 최적화
- **단일 책임 원칙**: 각 프로젝트의 명확한 역할 분담
- **의존성 방향**: UI → Core (단방향 참조)
- **빌드 독립성**: Core 프로젝트 단독 빌드 가능

이번 작업으로 StarcUp 프로젝트의 WinUI 3 마이그레이션을 위한 견고한 기반이 마련되었습니다.