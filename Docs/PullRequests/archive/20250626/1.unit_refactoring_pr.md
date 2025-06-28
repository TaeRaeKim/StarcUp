# Unit 관련 구조 개선: 도메인 중심 구조 및 계층 분리

## 📋 개요

현재 UnitManager와 UnitInfoUtil이 별도의 폴더로 분리되어 있는 구조를 개선하여, 도메인 중심의 명확한 계층 구조로 리팩토링. 

## 🤔 문제 인식

### 1. 분산된 폴더 구조
- `UnitManager/`와 `UnitInfoUtil/`이 별도 위치에 존재
- 유닛 관련 기능임에도 불구하고 물리적으로 분리됨
- 개발자가 유닛 관련 코드를 찾기 위해 여러 위치를 확인해야 함

### 2. 메모리 구조체와 비즈니스 로직의 혼재
- 현재 `Unit.cs`는 메모리 레이아웃에 특화된 구조체
- `unsafe` 포인터, `fixed byte` 배열, 패딩 등으로 비즈니스 로직에서 사용하기 부적절
- 코드 가독성 저하 및 유지보수의 어려움

### 3. 단일 서비스의 과도한 책임
- UnitManager가 메모리 접근, 데이터 변환, 비즈니스 로직을 모두 담당
- 단일 책임 원칙(SRP) 위반
- 테스트 및 확장이 어려운 구조

## 💡 해결 방향

### 1. 도메인 중심 구조로 통합
유닛 관련 모든 기능을 `Business/Units/` 하위로 통합하여 응집도를 높이고, 기능별로 명확히 분리:

```
Business/Units/
├── Runtime/      # 실시간 데이터
├── StaticData/   # 정적 데이터  
└── Types/        # 공통 타입
```

### 2. 메모리 구조체와 비즈니스 모델 분리
메모리에서 직접 읽어오는 구조체와 비즈니스에서 사용하는 클래스를 명확히 분리:

- `UnitRaw.cs`: 메모리 레이아웃 전용 (기존 Unit.cs)
- `Unit.cs`: 비즈니스 로직 전용 (새로 생성)

### 3. 계층별 책임 분리
단일 서비스가 가진 과도한 책임을 계층별로 분리:

- **Adapter**: 메모리 접근 및 Raw 데이터 관리
- **Service**: 비즈니스 로직 및 고수준 API 제공
- **Repository**: 정적 데이터 접근

## 🏗️ 설계 결정

### 네이밍 컨벤션
여러 후보를 검토한 결과:

**메모리 구조체**: `UnitRaw` vs `RawUnit`
- ✅ `UnitRaw`: 직관적이며 Unit이 메인 개념임을 명확히 표현
- ❌ `RawUnit`: Prefix 패턴이지만 Raw가 부각됨

**서비스 계층**: `UnitService` vs `UnitRuntimeService`  
- ✅ `UnitService`: 간결하며 메인 서비스임을 표현
- ❌ `UnitRuntimeService`: 의미는 명확하지만 이름이 길음

**정적 데이터**: `UnitInfoService` vs `UnitInfoRepository`
- ✅ `UnitInfoRepository`: Repository 패턴에 적합 (단순 조회 기능)
- ❌ `UnitInfoService`: Service는 비즈니스 로직이 있을 때 적합

### 계층 분리 접근
메모리 접근과 비즈니스 로직을 분리하기 위해 여러 방안을 검토:

**Option 1**: Infrastructure로 승격
- 문제: 전역 Infrastructure와 혼동 가능

**Option 2**: Manager 폴더 유지
- 문제: 여전히 단일 클래스가 과도한 책임

**Option 3**: Adapter 패턴 적용 ⭐
- 장점: MemoryService를 통한 올바른 의존성 체인
- 장점: 단일 책임 원칙 준수
- 선택 이유: `UnitService → UnitMemoryAdapter → MemoryService → MemoryReader`

## 🎯 구현 결과

### 최종 폴더 구조
```
Business/Units/
├── Runtime/
│   ├── Models/
│   │   ├── UnitRaw.cs              # 메모리 구조체
│   │   └── Unit.cs                 # 비즈니스 클래스
│   ├── Adapters/
│   │   ├── UnitMemoryAdapter.cs    # 메모리 접근 어댑터
│   │   └── IUnitMemoryAdapter.cs
│   └── Services/
│       ├── UnitService.cs          # 비즈니스 로직
│       └── IUnitService.cs
├── StaticData/
│   ├── Models/
│   │   ├── UnitInfo.cs            # UnitDto.cs에서 분리
│   │   ├── UnitInfoDto.cs
│   │   └── UnitsContainer.cs
│   └── Repositories/
│       ├── UnitInfoRepository.cs   # 정적 데이터 접근
│       └── IUnitInfoRepository.cs
└── Types/
    ├── UnitType.cs                 # enum 정의
    └── UnitTypeExtensions.cs       # 확장 메서드
```

### 의존성 체인
```
UnitService 
    ↓ 의존
UnitMemoryAdapter 
    ↓ 의존  
MemoryService (Infrastructure)
    ↓ 의존
MemoryReader (Infrastructure)
```

### 주요 변경사항

**파일명 변경**:
- `Unit.cs` → `UnitRaw.cs` (메모리 구조체)
- `GameUnit.cs` → `Unit.cs` (비즈니스 클래스)
- `UnitManager.cs` → `UnitService.cs`
- `UnitInfoUtils.cs` → `UnitInfoRepository.cs`

**새로 추가**:
- `UnitMemoryAdapter.cs` - 메모리 접근 전담
- `IUnitMemoryAdapter.cs` - 어댑터 인터페이스

**분리된 파일**:
- `UnitDto.cs` → 3개 파일로 분리 (UnitInfo, UnitInfoDto, UnitsContainer)

## ✅ 개선 효과

### 1. 관심사 분리
- **UnitMemoryAdapter**: 메모리 접근만 담당
- **UnitService**: 비즈니스 로직만 담당  
- **UnitInfoRepository**: 정적 데이터 접근만 담당

### 2. 코드 가독성 향상
```csharp
// Before: 복잡한 메모리 구조체 사용
var units = unitManager.GetAllUnits(); // Unit 구조체
foreach (var unit in units)
{
    if (unit.health > 0 && unit.playerIndex == 0)
        Console.WriteLine($"Unit {unit.unitType}");
}

// After: 깔끔한 비즈니스 객체 사용  
var units = unitService.GetPlayerUnits(0).Where(u => u.IsAlive);
foreach (var unit in units)
{
    Console.WriteLine($"{unit.DisplayName} - {unit.HealthPercentage:P0}");
}
```

### 3. 테스트 용이성
- 각 계층을 독립적으로 테스트 가능
- Mock 객체를 통한 단위 테스트 작성 용이
- 의존성 주입을 통한 느슨한 결합

### 4. 확장성
- 새로운 어댑터 추가 용이 (UnitDatabaseAdapter 등)
- 비즈니스 로직 확장 시 다른 계층에 영향 없음
- 다른 도메인에서도 유사한 패턴 적용 가능

## 🔄 마이그레이션 가이드

### 기존 코드 호환성
기존 코드는 대부분 그대로 동작하며, 점진적으로 새로운 API로 마이그레이션 가능:

```csharp
// 기존 방식 (여전히 지원)
var unitManager = container.Resolve<IUnitService>();
unitManager.LoadAllUnits();

// 새로운 방식 (권장)
var unitService = container.Resolve<IUnitService>();
var aliveUnits = unitService.GetAliveUnits();
```

### DI 등록 변경
```csharp
// 새로운 등록 순서
container.RegisterSingleton<IUnitMemoryAdapter, UnitMemoryAdapter>();
container.RegisterSingleton<IUnitInfoRepository, UnitInfoRepository>();
container.RegisterSingleton<IUnitService, UnitService>();
```

## 🎯 결론

이번 리팩토링을 통해:

1. **유닛 관련 모든 기능을 한 곳에 통합**하여 응집도 향상
2. **메모리 구조와 비즈니스 모델을 분리**하여 코드 가독성 향상  
3. **계층별 책임을 명확히 분리**하여 유지보수성 향상
4. **Adapter 패턴 도입**으로 올바른 의존성 체인 구축

향후 다른 도메인(Player, Building 등)에도 동일한 패턴을 적용하여 일관성 있는 아키텍처를 구축할 수 있는 기반을 마련.