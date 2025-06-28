# StarcUp 프로젝트 일일 변경사항 보고서
**날짜**: 2025년 6월 27일  
**주요 테마**: TebAddress 버그 수정 및 유닛 메모리 구조 대폭 개선

---

## 📋 개요

이번 업데이트는 **유닛 메모리 읽기 구조의 대대적인 개선**이 주요 내용임. 특히 **TebAddress 계산 오류 수정**과 **연결 리스트 기반 유닛 탐색** 구현을 통해 안정성과 성능을 크게 향상시킴.

---

## 🔧 주요 변경사항

### 1. **TebAddress 버그 수정** 🐛➡️✅

#### 문제점
- 기존 하드코딩된 메모리 주소 방식의 불안정성
- 게임 재시작 시 주소 변경으로 인한 메모리 읽기 실패

#### 해결 방안
- **동적 주소 계산**: `StarCraft.exe` 모듈 베이스 주소 + 오프셋으로 계산함
- **주소 캐싱 시스템**: 계산된 주소를 캐시하여 성능 향상시킴
- **PSAPI 기반 모듈 검색**: 안정적인 모듈 찾기 구현함
- **캐시 무효화 메커니즘**: 프로세스 변경 시 자동 캐시 초기화함

```csharp
// 핵심 주소 계산 로직
nint pointerAddress = starcraftModule.Value.modBaseAddr + 0xE77FE0 + 0x80;
nint finalUnitArrayAddress = _memoryReader.ReadPointer(pointerAddress);
```

### 2. **성능 최적화 - 연결 리스트 기반 유닛 탐색** ⚡

#### 기존 방식
- 전체 3400개 슬롯을 순차 스캔 → **O(n) 시간 복잡도**

#### 개선된 방식  
- `nextPointer`를 따라가는 연결 리스트 순회 → **O(active units) 시간 복잡도**
- 실제 활성 유닛만 처리하여 대폭적인 성능 향상시킴

### 3. **완전한 유닛 메모리 구조 매핑** 🗺️

- **488바이트** 완전한 StarCraft 유닛 메모리 구조 구현함
- 연결 리스트 포인터(`prevPointer`, `nextPointer`) 추가함
- 정확한 메모리 오프셋 주석 명시함
- `unsafe` 구조체로 메모리 레이아웃 정확성 보장함

---

## 📁 변경된 파일별 상세 내용

### **Core 비즈니스 로직**

#### `UnitMemoryAdapter.cs` - 핵심 메모리 어댑터
- ✅ **TebAddress 동적 계산 시스템** 구현함
- ✅ **주소 캐싱 메커니즘** (`_cachedUnitArrayAddress`, `_isAddressCached`) 추가함
- ✅ **연결 리스트 기반 유닛 탐색** (`GetActiveUnitsFromLinkedList()`) 구현함
- ✅ **PSAPI 기반 모듈 검색** (`FindModuleByPSAPI()`) 구현함

#### `IUnitMemoryAdapter.cs` - 인터페이스 확장
- ✅ `InvalidateAddressCache()` 메서드 추가함
- ✅ 주소 캐시 무효화 인터페이스 제공함

#### `UnitRaw.cs` - 메모리 구조 완전 매핑
- ✅ **488바이트 완전한 StarCraft 유닛 구조체** 구현함
- ✅ 연결 리스트 포인터 (`prevPointer`, `nextPointer`) 추가함
- ✅ 정확한 메모리 오프셋 주석 명시함
- ✅ `unsafe` 고정 배열로 메모리 정확성 보장함

#### `Unit.cs` - 모델 호환성 개선
- ✅ `FromRaw()` 메서드 `unsafe` 블록 처리함
- ✅ `ProductionQueueIndex`, `ProductionQueue` 속성 추가함

#### `UnitService.cs` & `IUnitService.cs` - 서비스 레이어
- ✅ `InvalidateAddressCache()` 기능 추가함
- ✅ 상위 서비스에서 주소 캐시 관리함

### **정적 데이터 관리**

#### `UnitInfo.cs`, `UnitInfoDto.cs`, `UnitsContainer.cs`
- ✅ `record` 타입으로 불변 데이터 구조 구현함
- ✅ JSON 직렬화/역직렬화 최적화함
- ✅ 다양한 유닛 검색 메서드 제공함 (`GetCheapestUnits`, `GetStrongestUnits`)

#### `UnitInfoRepository.cs` & `IUnitInfoRepository.cs`
- ✅ 효율적인 유닛 정적 데이터 관리함
- ✅ 다양한 검색 및 필터링 기능 제공함

#### `UnitTypeExtensions.cs` - 유닛 타입 시스템
- ✅ **완전한 BWAPI 호환 UnitType enum** (235개 유닛 타입) 구현함
- ✅ 상세한 카테고리 분류 시스템 구축함
- ✅ 종족별, 타입별 유닛 분류 메서드 확장함

### **사용자 인터페이스**

#### `ControlForm.cs` - 개발자 도구 UI
- ✅ **InGame 전용 유닛 테스트 도구** 추가함
- ✅ 플레이어별 유닛 검색 기능 구현함
- ✅ 실시간 유닛 데이터 갱신 기능 추가함
- ✅ 메모리 연결 상태에 따른 UI 제어 구현함

### **프로젝트 관리**

#### `commit-guidelines.md` - 협업 가이드라인
- ✅ 체계적인 Git 커밋 가이드라인 문서 작성함
- ✅ Prefix 기반 커밋 메시지 규칙 정립함

### **테스트 코드**

#### `UnitServiceTest.cs` & `UnitInfoRepositoryTest.cs`
- ✅ 새로운 기능에 대한 단위 테스트 추가함
- ✅ 메모리 구조 변경에 따른 테스트 코드 업데이트함

---

## 📊 성능 및 안정성 개선 효과

### **안정성 향상** 🛡️
- ✅ **TebAddress 동적 계산**으로 게임 재시작 내구성 확보함
- ✅ **메모리 구조 정확성**으로 데이터 신뢰성 향상시킴
- ✅ **주소 캐싱**으로 메모리 접근 안정성 증대함

### **성능 최적화** 🚀
- ✅ **연결 리스트 방식**으로 유닛 검색 속도 대폭 향상시킴
  - 기존: 3400개 슬롯 전체 스캔
  - 개선: 실제 활성 유닛 수만큼만 처리
- ✅ **주소 캐싱**으로 반복 계산 오버헤드 제거함
- ✅ **PSAPI 기반 모듈 검색**으로 효율적인 주소 찾기 구현함

### **개발 편의성** 🛠️
- ✅ **유닛 테스트 UI**로 실시간 디버깅 가능함
- ✅ **체계적인 커밋 가이드라인**으로 협업 효율성 증대함
- ✅ **완전한 메모리 구조 문서화**로 개발 생산성 향상시킴

---

## 🔮 향후 계획

1. **메모리 읽기 최적화**: 추가적인 캐싱 전략 도입
2. **UI 개선**: 유닛 테스트 도구 기능 확장
3. **테스트 커버리지**: 새로운 기능에 대한 포괄적 테스트 작성
4. **문서화**: API 문서 및 사용자 가이드 작성

---

## 📝 기술적 하이라이트

### 핵심 기술 스택
- **메모리 관리**: `unsafe` 코드, 포인터 연산
- **성능 최적화**: 연결 리스트, 캐싱 시스템
- **Windows API**: PSAPI를 통한 프로세스 모듈 검색
- **아키텍처**: Clean Architecture, 의존성 주입

### 코드 품질
- **불변 데이터**: `record` 타입 활용
- **타입 안전성**: 강타입 enum 및 확장 메서드
- **메모리 안전성**: `unsafe` 블록 최소화
- **테스트 가능성**: 인터페이스 기반 설계

---

**결론**: 이번 업데이트는 **메모리 읽기 안정성과 성능 모두를 크게 개선**한 중요한 리팩토링임. 특히 TebAddress 버그 수정과 연결 리스트 기반 유닛 탐색은 프로젝트의 핵심 기능 안정성을 한층 높임.