# UnitCount 시스템 개발 완료

## 📋 개요

StarCraft 메모리에서 유닛 개수(완성된 유닛 + 생산중인 유닛)를 실시간으로 추적하는 시스템을 구현했습니다.

## 🎯 주요 기능

### ✅ 구현된 기능
- **실시간 유닛 카운트 추적**: 완성된 유닛과 생산중인 유닛을 구분하여 추적
- **JSON 기반 설정**: 하드코딩 없는 유연한 메모리 오프셋 관리
- **버퍼 기반 메모리 읽기**: 단일 API 호출로 모든 종족 데이터 로드 (19232 bytes)
- **Player 확장 메서드**: `player.GetUnitCount(UnitType.ProtossNexus, true)` 형태의 직관적 API
- **GameManager 통합**: 0.1초마다 localPlayer의 유닛 현황 자동 출력

## 🏗️ 아키텍처

### 핵심 컴포넌트

#### 1. UnitCount 클래스 (`Models/UnitCount.cs`)
```csharp
public class UnitCount
{
    public UnitType UnitType { get; set; }
    public byte PlayerIndex { get; set; }
    public int CompletedCount { get; set; }    // 완성된 유닛
    public int ProductionCount { get; set; }   // 생산중인 유닛
    public int TotalCount => CompletedCount + ProductionCount;
}
```

#### 2. UnitCountAdapter (`Adapters/UnitCountAdapter.cs`)
- **메모리 주소 계산**: ThreadStack0 + baseOffset을 통한 포인터 체인
- **버퍼 기반 읽기**: 19232 bytes 단일 읽기로 모든 종족 데이터 로드
- **오프셋 관리**: JSON 설정 파일 기반 동적 오프셋 관리

#### 3. UnitCountService (`Services/UnitCountService.cs`)
- **100ms 간격 업데이트**: 1초에 10번 자동 데이터 새로고침
- **8개 플레이어 캐시**: 모든 플레이어의 유닛 카운트 캐싱
- **Thread-safe**: lock을 사용한 안전한 동시 접근

#### 4. Player 확장 메서드 (`Extensions/PlayerExtensions.cs`)
```csharp
// 기본 사용법
int nexusCount = player.GetUnitCount(UnitType.ProtossNexus);

// 생산중 포함
int totalNexus = player.GetUnitCount(UnitType.ProtossNexus, true);

// 모든 유닛 조회
var allUnits = player.GetAllUnitCounts(true);
```

## 🔧 메모리 구조

### 메모리 주소 계산
```
ThreadStack0 + baseOffset(-1312) → 포인터 값 읽기 → 실제 베이스 주소
실제 베이스 주소 + unitOffset + (playerIndex * 4) → 완성된 유닛 개수
완성된 주소 + productionOffset(-10944) → 생산중 포함 개수
```

### 버퍼 구조
- **시작 오프셋**: 0x3234 (12852)
- **종료 오프셋**: 0x7D34 (32052)  
- **버퍼 크기**: 19232 bytes
- **포함 데이터**: 모든 종족의 완성된 + 생산중 유닛 카운트

## 📁 파일 구조

```
StarcUp/
├── Data/
│   └── all_race_unit_offsets.json          # 메모리 오프셋 설정
├── Src/Business/Units/Runtime/
│   ├── Models/
│   │   ├── UnitCount.cs                    # 유닛 카운트 데이터 모델
│   │   ├── UnitCountDto.cs                 # DTO 클래스들
│   │   ├── UnitCountRaw.cs                 # Raw 구조체
│   │   └── UnitOffsetConfigDto.cs          # 설정 DTO
│   ├── Adapters/
│   │   ├── IUnitCountAdapter.cs            # 어댑터 인터페이스
│   │   └── UnitCountAdapter.cs             # 메모리 접근 어댑터
│   ├── Services/
│   │   ├── IUnitCountService.cs            # 서비스 인터페이스
│   │   └── UnitCountService.cs             # 비즈니스 로직 서비스
│   └── Repositories/
│       └── UnitOffsetRepository.cs         # JSON 설정 관리
├── Src/Business/GameManager/
│   ├── Extensions/
│   │   └── PlayerExtensions.cs             # Player 확장 메서드
│   └── GameManager.cs                      # 게임 매니저 (통합)
└── Docs/Examples/
    └── UnitCountUsageExample.cs            # 사용 예시 코드
```

## ⚡ 성능 특징

- **단일 메모리 읽기**: 19232 bytes를 한 번에 읽어 모든 데이터 확보
- **효율적인 캐싱**: 100ms마다 업데이트되는 캐시 시스템
- **최소한의 메모리 접근**: 버퍼 기반으로 반복적인 메모리 읽기 방지

## 🔄 의존성 주입

ServiceRegistration.cs에서 모든 서비스가 자동 등록됩니다:
```csharp
container.RegisterSingleton<UnitOffsetRepository>();
container.RegisterSingleton<IUnitCountAdapter>();
container.RegisterSingleton<IUnitCountService>();
PlayerExtensions.SetUnitCountService(unitCountService);
```

## 📊 실제 사용 예시

### GameManager 통합
```csharp
// 0.1초마다 실행되어 1초마다 출력
[Player 0] 유닛 현황: 완성 45개 + 생산중 3개 = 총 48개 - 14:30:25
  주요 유닛:
    - 프로브: 12개 (+1개 생산중)
    - 질럿: 8개
    - 드라군: 6개 (+2개 생산중)
```

### 프로그래밍 인터페이스
```csharp
var localPlayer = Players[localPlayerIndex];

// 특정 유닛 개수
int zealots = localPlayer.GetUnitCount(UnitType.ProtossZealot, true);

// 전체 유닛 목록
var allUnits = localPlayer.GetAllUnitCounts(true);
foreach (var unit in allUnits.Where(u => u.TotalCount > 0))
{
    Console.WriteLine($"{unit.UnitType}: {unit.CompletedCount}개 (+{unit.ProductionCount}개 생산중)");
}
```

## 🛠️ 기술적 세부사항

### JSON 설정 파일 구조
```json
{
  "baseOffset": -1312,
  "productionOffset": -10944,
  "bufferInfo": {
    "minOffset": 12852,
    "maxOffset": 32052,
    "bufferSize": 19232
  },
  "races": {
    "protoss": {
      "units": [
        { "unitType": "ProtossProbe", "unitId": 64, "completedOffset": 13108 }
      ]
    }
  }
}
```

### 인터페이스 우선 설계
모든 주요 컴포넌트가 인터페이스를 통해 정의되어 테스트 가능하고 확장 가능한 구조를 제공합니다.

## ✅ 완료된 작업 목록

1. ✅ UnitCount 클래스 설계 및 구현
2. ✅ JSON 기반 오프셋 설정 시스템
3. ✅ 버퍼 기반 메모리 어댑터 구현
4. ✅ 100ms 간격 자동 업데이트 서비스
5. ✅ Player 확장 메서드 API
6. ✅ GameManager 통합 및 실시간 로그
7. ✅ 의존성 주입 컨테이너 설정
8. ✅ 포인터 체인 메모리 주소 계산
9. ✅ 전체 시스템 테스트 및 문서화

## 🚀 향후 확장 가능성

- **다른 종족 지원**: Terran, Zerg 유닛도 동일한 구조로 확장 가능
- **실시간 알림**: 특정 유닛 수 도달 시 알림 기능
- **통계 분석**: 시간별 유닛 생산 추이 분석
- **외부 API**: RESTful API로 외부 도구와 연동