# 📊 StarcUp 오버레이 구현 설계 문서

## 🎯 개요

이 문서는 Prototype/Overlay-Design의 기술과 디자인을 기반으로 StarcUp.UI에 실제 게임 오버레이를 구현하는 설계 계획입니다. 현재 StarcUp.UI의 오버레이는 "Hello World" 표시 단계이며, 이를 완전한 기능을 갖춘 실시간 게임 정보 오버레이로 발전시키는 것이 목표입니다.

## 🏗️ 현재 상태 분석

### 기존 StarcUp.UI 오버레이 (C:\dev\StarcUp\StarcUp.UI\src\overlay\OverlayApp.tsx)
- **현재 기능**: "Hello World" 텍스트 중앙 표시
- **연결 상태**: WorkerManager 이벤트 구독 및 중앙 위치 추적
- **디버그 시스템**: 개발 환경에서 상세한 연결/성능 정보 표시
- **한계**: 실제 게임 정보를 시각적으로 표시하는 컴포넌트 부재

### Prototype/Overlay-Design 참조
- **완성도**: React + TypeScript + Tailwind CSS 기반 완전한 오버레이 시스템
- **주요 컴포넌트**: 8개의 게임 정보 표시 컴포넌트
- **UI 시스템**: shadcn/ui 기반 현대적 디자인 시스템
- **편집 모드**: 드래그 앤 드롭을 통한 실시간 위치 조정

## 🎮 구현할 핵심 컴포넌트

### 1. 일꾼 상태 표시 (WorkerStatus)
```typescript
interface WorkerStatusProps {
  totalWorkers: number
  idleWorkers: number
  activeWorkers: number
  productionWorkers: number
  isVisible: boolean
  position: { x: number, y: number }
}
```

**기능**:
- 실시간 일꾼 수 표시 (총 일꾼 / 유휴 일꾼)
- 일꾼 아이콘 + 숫자 조합
- 유휴 일꾼 수를 오렌지색으로 강조 표시
- 애니메이션: 일꾼 사망 시 빨간색 깜빡임

### 2. 유닛 수 표시 (UnitCountDisplay)
```typescript
interface UnitCountDisplayProps {
  unitCounts: {
    category: 'combat' | 'production' | 'special'
    units: { type: UnitType, count: number, icon: string }[]
  }[]
  isVisible: boolean
  position: { x: number, y: number }
}
```

**기능**:
- 카테고리별 유닛 분류 (전투/생산/특수)
- 유닛 아이콘 + 개수 표시
- 0개인 유닛은 표시하지 않음
- 컴팩트한 세로 레이아웃

### 3. 빌드 오더 가이드 (BuildOrderGuide)
```typescript
interface BuildOrderGuideProps {
  currentPhase: {
    building: BuildingType
    progress: number // 0-100
    targetCount: number
    currentCount: number
  }
  nextPhase: {
    building: BuildingType
    targetCount: number
  }
  isVisible: boolean
  position: { x: number, y: number }
}
```

**기능**:
- 현재 건설 단계와 다음 단계 표시
- 진행도에 따른 아이콘 색상 변화 (회색 → 노랑 → 초록)
- 건물 개수 배지 표시
- 2줄 레이아웃 (현재/다음)

### 4. 업그레이드 진행도 (UpgradeProgress)
```typescript
interface UpgradeProgressProps {
  activeUpgrades: {
    type: UpgradeType
    timeRemaining: number // 초
    totalTime: number // 초
    progress: number // 0-100
  }[]
  isVisible: boolean
  position: { x: number, y: number }
}
```

**기능**:
- 진행 중인 업그레이드 목록
- 남은 시간 표시 (MM:SS 형식)
- 진행률 바와 퍼센트
- 최대 3개까지 표시

### 5. 인구 경고 (PopulationWarning)
```typescript
interface PopulationWarningProps {
  currentPopulation: number
  maxPopulation: number
  warningThreshold: number // 기본 80%
  isVisible: boolean
  position: { x: number, y: number }
}
```

**기능**:
- 인구 한계 근접 시 자동 표시
- 빨간색 배경에 경고 아이콘
- 페이드 인/아웃 애니메이션
- 임계점 도달 시에만 활성화

### 6. 설정 패널 (SettingsPanel)
```typescript
interface SettingsPanelProps {
  isEditMode: boolean
  onToggleVisibility: (component: string) => void
  onOpacityChange: (value: number) => void
  onStyleChange: (style: IconStyle) => void
  onTeamColorChange: (color: TeamColor) => void
  onResetPositions: () => void
}
```

**기능**:
- 편집 모드에서만 표시
- 각 컴포넌트 표시/숨김 토글
- 전체 투명도 조절
- 아이콘 스타일 선택 (Default/White/Yellow/HD)
- 팀 컬러 설정 (HD 모드)

## 🏗️ 아키텍처 설계

### 오버레이 폴더 구조
```
src/overlay/
├── OverlayApp.tsx                    # 메인 오버레이 앱
├── index.html                        # 오버레이 HTML 진입점
├── main.tsx                          # React 앱 부트스트랩
├── overlay.css                       # 기본 오버레이 스타일
├── overlay-env.d.ts                  # 환경 타입 정의
│
├── components/                       # 📦 UI 컴포넌트
│   ├── game-info/                   # 게임 정보 표시 컴포넌트
│   │   ├── WorkerStatus.tsx         # 일꾼 상태
│   │   ├── UnitCountDisplay.tsx     # 유닛 수 표시
│   │   ├── BuildOrderGuide.tsx      # 빌드 오더 가이드
│   │   ├── UpgradeProgress.tsx      # 업그레이드 진행도
│   │   └── PopulationWarning.tsx    # 인구 경고
│   │
│   ├── layout/                      # 레이아웃 관련 컴포넌트
│   │   ├── DraggableOverlay.tsx     # 드래그 가능한 래퍼
│   │   ├── OverlayContainer.tsx     # 오버레이 컨테이너
│   │   └── EditModeHelper.tsx       # 편집 모드 도움말
│   │
│   ├── settings/                    # 설정 관련 컴포넌트
│   │   ├── SettingsPanel.tsx        # 메인 설정 패널
│   │   ├── StyleSelector.tsx        # 아이콘 스타일 선택
│   │   ├── TeamColorPicker.tsx      # 팀 컬러 선택
│   │   ├── OpacitySlider.tsx        # 투명도 조절
│   │   └── PositionReset.tsx        # 위치 리셋 버튼
│   │
│   └── common/                      # 공통 UI 컴포넌트
│       ├── Icon.tsx                 # 범용 아이콘 컴포넌트
│       ├── Badge.tsx                # 배지 컴포넌트
│       ├── ProgressBar.tsx          # 진행률 바
│       └── Tooltip.tsx              # 툴팁
│
├── hooks/                           # 🎣 커스텀 훅
│   ├── useOverlaySettings.ts        # 오버레이 설정 관리
│   ├── usePositionManager.ts        # 위치 관리 (드래그, 저장, 복원)
│   ├── useGameData.ts               # 게임 데이터 구독
│   ├── useEditMode.ts               # 편집 모드 상태 관리
│   ├── useIconStyle.ts              # 아이콘 스타일 관리
│   └── useKeyboardShortcuts.ts      # 키보드 단축키 (Shift + `)
│
├── services/                        # 🔧 서비스 계층
│   ├── IconService.ts               # 아이콘 관리 서비스
│   ├── PositionService.ts           # 위치 저장/복원 서비스
│   ├── SettingsService.ts           # 설정 관리 서비스
│   └── GameDataService.ts           # 게임 데이터 처리 서비스
│
├── stores/                          # 🏪 상태 관리
│   ├── overlayStore.ts              # 메인 오버레이 상태
│   ├── settingsStore.ts             # 설정 상태
│   ├── positionStore.ts             # 위치 상태
│   └── gameDataStore.ts             # 게임 데이터 상태
│
├── types/                           # 📝 타입 정의
│   ├── overlay.ts                   # 오버레이 관련 타입
│   ├── game-data.ts                 # 게임 데이터 타입
│   ├── settings.ts                  # 설정 관련 타입
│   ├── position.ts                  # 위치 관련 타입
│   └── icon.ts                      # 아이콘 시스템 타입
│
├── utils/                           # 🛠️ 유틸리티
│   ├── calculations.ts              # 위치/크기 계산 함수
│   ├── formatters.ts                # 데이터 포맷팅 (시간, 숫자 등)
│   ├── validators.ts                # 데이터 검증 함수
│   └── constants.ts                 # 상수 정의
│
├── styles/                          # 🎨 스타일시트
│   ├── components/                  # 컴포넌트별 스타일
│   │   ├── worker-status.css        # 일꾼 상태 스타일
│   │   ├── unit-count.css          # 유닛 수 스타일
│   │   ├── build-order.css         # 빌드 오더 스타일
│   │   ├── settings-panel.css      # 설정 패널 스타일
│   │   └── common.css              # 공통 컴포넌트 스타일
│   │
│   ├── themes/                      # 테마별 스타일
│   │   ├── default.css             # 기본 테마
│   │   ├── dark.css                # 다크 테마
│   │   └── high-contrast.css       # 고대비 테마
│   │
│   └── icons/                       # 아이콘 스타일
│       ├── icon-styles.css         # 아이콘 기본 스타일
│       ├── icon-filters.css        # CSS 필터 정의
│       └── team-colors.css         # 팀 컬러 정의
│
└── assets/                          # 📁 정적 자산
    ├── icons/                       # 아이콘 파일들
    │   ├── default/                 # 기본 스타일 아이콘
    │   │   ├── protoss/            # 프로토스 아이콘
    │   │   │   ├── zealot.png
    │   │   │   ├── dragoon.png
    │   │   │   └── ...
    │   │   ├── terran/             # 테란 아이콘
    │   │   │   ├── marine.png
    │   │   │   ├── firebat.png
    │   │   │   └── ...
    │   │   └── zerg/               # 저그 아이콘
    │   │       ├── zergling.png
    │   │       ├── hydralisk.png
    │   │       └── ...
    │   │
    │   └── hd/                      # HD 스타일 아이콘 (2파일 세트)
    │       ├── protoss/
    │       │   ├── zealot_diffuse.png      # 기본 텍스처
    │       │   ├── zealot_teamcolor.png    # 팀컬러 마스크
    │       │   ├── dragoon_diffuse.png
    │       │   ├── dragoon_teamcolor.png
    │       │   └── ...
    │       ├── terran/
    │       │   ├── marine_diffuse.png
    │       │   ├── marine_teamcolor.png
    │       │   └── ...
    │       └── zerg/
    │           ├── zergling_diffuse.png
    │           ├── zergling_teamcolor.png
    │           └── ...
    │
    ├── sounds/                      # 사운드 파일 (필요시)
    │   ├── alert.wav               # 경고음
    │   └── notification.wav        # 알림음
    │
    └── fonts/                       # 폰트 파일 (필요시)
        └── starcraft.ttf           # 스타크래프트 스타일 폰트
```

### 폴더별 역할 설명

#### 📦 components/
- **game-info/**: 실제 게임 정보를 표시하는 핵심 컴포넌트들
- **layout/**: 오버레이의 배치와 상호작용을 담당하는 컴포넌트들  
- **settings/**: 사용자 설정을 위한 UI 컴포넌트들
- **common/**: 여러 곳에서 재사용되는 범용 컴포넌트들

#### 🎣 hooks/
- React 함수형 컴포넌트에서 사용할 커스텀 훅들
- 상태 관리, 이벤트 처리, 부수 효과 등을 캡슐화

#### 🔧 services/
- 비즈니스 로직과 외부 시스템과의 연동을 담당
- 컴포넌트로부터 로직을 분리하여 재사용성과 테스트 용이성 확보

#### 🏪 stores/
- 전역 상태 관리 (Zustand, Redux, 또는 Context API 사용)
- 컴포넌트 간 데이터 공유 및 상태 동기화

#### 📝 types/
- TypeScript 타입 정의 파일들
- 도메인별로 분리하여 타입 관리의 복잡성 감소

#### 🛠️ utils/
- 순수 함수들로 구성된 유틸리티 함수들
- 계산, 포맷팅, 검증 등 부수 효과 없는 헬퍼 함수들

#### 🎨 styles/
- CSS 스타일시트들을 기능별, 테마별로 구조화
- 컴포넌트별 스타일과 전역 스타일 분리

#### 📁 assets/
- **icons/**: 아이콘 파일들을 스타일별, 종족별로 체계적 관리
  - `default/`: 기본 스타일 (1개 파일)
  - `hd/`: HD 스타일 (diffuse + teamcolor 2개 파일 세트)
- **sounds/**: 오버레이 관련 사운드 파일
- **fonts/**: 커스텀 폰트 파일

### 데이터 흐름

```
StarcUp.Core → Electron Main → Electron Renderer → React 컴포넌트
     ↓              ↓                  ↓                ↓
   메모리 읽기    IPC 통신        이벤트 구독        상태 업데이트
```

1. **StarcUp.Core**: 메모리에서 게임 데이터 읽기
2. **Electron Main**: Core로부터 데이터 수신 및 Renderer로 전송
3. **Electron Renderer**: IPC 이벤트를 React 상태로 변환
4. **React 컴포넌트**: 상태 변화에 따른 UI 업데이트

## 🎨 디자인 시스템 적용

### Prototype/Overlay-Design 기반 디자인 적용

#### 색상 팔레트
```css
/* Prototype Guidelines.md 기반 */
--overlay-bg: rgba(0, 0, 0, 0.85);
--overlay-border: rgba(255, 255, 255, 0.2);
--text-primary: #FFFFFF;
--text-secondary: #E0E0E0;
--accent-warning: #FFB800;
--accent-success: #00D084;
--accent-error: #FF3B30;
--brand-primary: #0099FF;
```

#### 아이콘 스타일 시스템

**공통 아이콘 인터페이스**
```typescript
interface GameIcon {
  id: string           // 고유 식별자 (예: 'marine', 'zealot', 'forge')
  basePath: string     // 기본 아이콘 파일 경로
  style: IconStyle     // 'default' | 'white' | 'yellow' | 'hd'
  teamColor?: TeamColor // HD 모드용 팀 컬러
}

type IconStyle = 'default' | 'white' | 'yellow' | 'hd'
type TeamColor = 'red' | 'blue' | 'teal' | 'purple' | 'yellow' | 'orange' | 'green' | 'pink'

interface HDIconFiles {
  diffuse: string      // 기본 텍스처 (grayscale)
  teamcolor: string    // 팀 컬러 마스크 (white = 팀컬러 적용 영역)
}
```

**스타일별 아이콘 표현 방식**

##### 1. Default Style
```css
/* 기본 스타일 - 원본 게임 아이콘 색상 유지 */
.icon-default {
  filter: none;
  opacity: 1.0;
}
```
- **용도**: 일반적인 게임 환경
- **특징**: 스타크래프트 원본 아이콘 색상 그대로 사용
- **파일 구조**: `/icons/default/[race]/[unit].png`

##### 2. White Style  
```css
/* 흰색 스타일 - 단색 실루엣 */
.icon-white {
  filter: brightness(0) invert(1);
  opacity: 0.9;
}
```
- **용도**: 어두운 배경이나 미니멀한 환경
- **특징**: 모든 아이콘을 흰색 실루엣으로 변환
- **파일 구조**: 기본 파일 + CSS 필터

##### 3. Yellow Style
```css
/* 노란색 스타일 - 강조 및 가시성 */
.icon-yellow {
  filter: sepia(1) saturate(3) hue-rotate(15deg) brightness(1.2);
  opacity: 1.0;
}
```
- **용도**: 중요한 정보 강조 시
- **특징**: 모든 아이콘을 골드/노란색 톤으로 변환
- **파일 구조**: 기본 파일 + CSS 필터

##### 4. HD Style (Team Color)
```css
/* HD 스타일 - 2파일 합성 시스템 */
.icon-hd-container {
  position: relative;
  width: 24px;
  height: 24px;
}

.icon-hd-diffuse {
  position: absolute;
  top: 0;
  left: 0;
  z-index: 1;
}

.icon-hd-teamcolor {
  position: absolute;
  top: 0;
  left: 0;
  z-index: 2;
  mask: url('./teamcolor-mask.png');
  -webkit-mask: url('./teamcolor-mask.png');
}

/* 팀 컬러별 정의 */
.teamcolor-red { background-color: #ff0000; }
.teamcolor-blue { background-color: #0066ff; }
.teamcolor-teal { background-color: #00ffcc; }
.teamcolor-purple { background-color: #8000ff; }
.teamcolor-yellow { background-color: #ffff00; }
.teamcolor-orange { background-color: #ff8000; }
.teamcolor-green { background-color: #00ff00; }
.teamcolor-pink { background-color: #ff00ff; }
```
- **용도**: 멀티플레이어 환경에서 플레이어 구분
- **특징**: diffuse + teamcolor 2파일 합성으로 고품질 팀 컬러 구현
- **파일 구조**: 
  - `/icons/hd/[race]/[unit]_diffuse.png` (기본 텍스처)
  - `/icons/hd/[race]/[unit]_teamcolor.png` (팀컬러 마스크)

**아이콘 서비스 구현**
```typescript
class IconService {
  private basePath = '/assets/icons'
  
  getIconFiles(type: string, race: Race, style: IconStyle): string | HDIconFiles {
    switch (style) {
      case 'default':
      case 'white':
      case 'yellow':
        return `${this.basePath}/default/${race}/${type}.png`
      case 'hd':
        return {
          diffuse: `${this.basePath}/hd/${race}/${type}_diffuse.png`,
          teamcolor: `${this.basePath}/hd/${race}/${type}_teamcolor.png`
        }
    }
  }
  
  getIconClass(style: IconStyle, teamColor?: TeamColor): string {
    if (style === 'hd' && teamColor) {
      return `icon-hd-container`
    }
    return `icon-${style}`
  }
  
  renderIcon(type: string, race: Race, style: IconStyle, teamColor?: TeamColor): JSX.Element {
    if (style === 'hd') {
      const files = this.getIconFiles(type, race, style) as HDIconFiles
      return (
        <div className="icon-hd-container">
          <img src={files.diffuse} className="icon-hd-diffuse" alt={type} />
          <div 
            className={`icon-hd-teamcolor teamcolor-${teamColor}`}
            style={{ mask: `url(${files.teamcolor})` }}
          />
        </div>
      )
    } else {
      const file = this.getIconFiles(type, race, style) as string
      return (
        <img 
          src={file} 
          className={this.getIconClass(style, teamColor)}
          alt={type}
        />
      )
    }
  }
}
```

#### 컴포넌트 스타일
```css
.overlay-component {
  background: var(--overlay-bg);
  border: 1px solid var(--overlay-border);
  border-radius: 8px;
  padding: 8px 12px;
  backdrop-filter: blur(10px);
  box-shadow: 0 4px 20px rgba(0, 0, 0, 0.3);
}

.worker-icon {
  width: 20px;
  height: 20px;
  color: var(--accent-warning);
}

.unit-count-text {
  font-size: 14px;
  font-weight: 600;
  color: var(--text-primary);
}
```

### 반응형 스케일링
```javascript
// 화면 해상도에 따른 자동 스케일링
const getScaleFactor = (width: number) => {
  if (width <= 1366) return 0.8
  if (width <= 1600) return 0.9
  if (width <= 1920) return 1.0
  return 1.2
}
```

## 🔧 기술적 구현 계획

### 1단계: 기본 컴포넌트 개발 (1-2주)
- [ ] WorkerStatus 컴포넌트 구현
- [ ] UnitCountDisplay 컴포넌트 구현
- [ ] 기본 스타일링 및 애니메이션 적용
- [ ] 기존 OverlayApp.tsx와 통합

### 2단계: 고급 기능 구현 (2-3주)
- [ ] BuildOrderGuide 컴포넌트 구현
- [ ] UpgradeProgress 컴포넌트 구현
- [ ] PopulationWarning 컴포넌트 구현
- [ ] 데이터 바인딩 및 실시간 업데이트

### 3단계: 편집 시스템 구현 (1-2주)
- [ ] DraggableOverlay 컴포넌트 구현
- [ ] SettingsPanel 컴포넌트 구현
- [ ] 위치 저장/복원 시스템
- [ ] 편집 모드 토글 (Shift + ` 키)

### 4단계: 최적화 및 완성 (1주)
- [ ] 성능 최적화 (메모이제이션, 가상화)
- [ ] 크로스 해상도 테스트
- [ ] 접근성 개선
- [ ] 사용자 테스트 및 버그 수정

## 📊 성능 고려사항

### 렌더링 최적화
```typescript
// React.memo를 사용한 불필요한 리렌더링 방지
const WorkerStatus = React.memo(({ totalWorkers, idleWorkers }: WorkerStatusProps) => {
  return (
    <div className="worker-status">
      {/* 컴포넌트 내용 */}
    </div>
  )
})

// useCallback을 사용한 이벤트 핸들러 최적화
const handlePositionChange = useCallback((newPosition: Position) => {
  setPosition(newPosition)
}, [])
```

### 데이터 업데이트 최적화
```typescript
// 디바운싱을 통한 과도한 업데이트 방지
const debouncedUpdatePosition = useMemo(
  () => debounce((position: Position) => {
    updatePosition(position)
  }, 50),
  []
)
```

## 🎮 사용자 경험

### 편집 모드
1. **활성화**: `Shift + `` 키 조합
2. **시각적 피드백**: 편집 가능한 컴포넌트에 점선 테두리
3. **드래그 앤 드롭**: 마우스로 컴포넌트 위치 조정
4. **스냅 가이드**: 정렬을 위한 보조선 표시
5. **설정 패널**: 우상단 톱니바퀴 아이콘으로 접근

### 일반 모드
1. **투명한 상호작용**: 게임 플레이에 방해되지 않는 완전 투명 모드
2. **실시간 업데이트**: 16ms 주기의 부드러운 데이터 갱신
3. **적응형 표시**: 중요한 정보만 자동으로 표시
4. **최소한의 시각적 노이즈**: 깔끔하고 읽기 쉬운 인터페이스

## 🔄 마이그레이션 전략

### 기존 코드 활용
- 현재 OverlayApp.tsx의 연결 로직 유지
- WorkerManager 이벤트 구독 시스템 확장
- 디버그 정보 시스템 개선

### 점진적 구현
1. **병렬 개발**: 기존 "Hello World" 유지하면서 새 컴포넌트 추가
2. **기능별 토글**: 개발 중인 컴포넌트는 개별적으로 활성화/비활성화
3. **A/B 테스트**: 사용자 피드백을 통한 단계적 개선

## 🧪 테스트 계획

### 단위 테스트
- 각 컴포넌트의 렌더링 테스트
- 데이터 바인딩 로직 테스트
- 위치 관리 시스템 테스트

### 통합 테스트
- Electron IPC 통신 테스트
- 실시간 데이터 업데이트 테스트
- 편집 모드 상호작용 테스트

### 성능 테스트
- 장시간 실행 시 메모리 사용량
- 고주사율 모니터에서의 렌더링 성능
- 다중 해상도 환경 테스트

## 📅 구현 일정

### Week 1-2: 기초 작업
- 컴포넌트 구조 설계 완료
- WorkerStatus, UnitCountDisplay 구현
- 기본 스타일링 적용

### Week 3-4: 핵심 기능
- BuildOrderGuide, UpgradeProgress 구현
- PopulationWarning 구현
- 데이터 바인딩 완성

### Week 5-6: 편집 시스템
- DraggableOverlay 구현
- SettingsPanel 구현
- 위치 저장/복원 시스템

### Week 7: 최종 완성
- 성능 최적화
- 버그 수정 및 테스트
- 문서화 완료

## 🎯 성공 지표

1. **기능적 완성도**: 모든 게임 정보가 실시간으로 정확히 표시
2. **사용자 경험**: 편집 모드와 일반 모드 간 원활한 전환
3. **성능**: 60fps 유지 및 1% 미만의 CPU 사용률
4. **안정성**: 장시간 게임 플레이 중 오류 없는 동작
5. **호환성**: 다양한 해상도와 스케일링 환경에서 정상 작동

---

이 설계 문서를 기반으로 StarcUp의 오버레이는 Prototype/Overlay-Design의 우수한 디자인과 기능을 실제 게임 환경에서 활용할 수 있는 완전한 시스템으로 발전할 것입니다.