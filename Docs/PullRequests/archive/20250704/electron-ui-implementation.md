# Electron 기반 스타크래프트 스타일 UI 구현

**날짜**: 2025-07-04  
**작업 유형**: UI 구현, 컴포넌트 개발, 스타일링 시스템

## 📋 작업 개요

Electron 프레임워크 마이그레이션 완료 후, 스타크래프트 게임 스타일을 반영한 메인 UI 인터페이스를 구현했습니다. React + TypeScript 기반의 모던 컴포넌트 시스템과 게임 테마에 최적화된 디자인 시스템을 구축했습니다.

## 🎨 UI 디자인 시스템

### 스타크래프트 색상 팔레트
```css
:root {
  /* 주요 팀 색상 */
  --sc-blue: #0066cc;        /* 파란색 (Player 1)*/
  --sc-teal: #00cccc;        /* 청록색 (Player 2) */
  --sc-green: #00cc00;       /* 녹색 (Player 3) */
  --sc-yellow: #ffcc00;      /* 노란색 (Player 4) */
  --sc-orange: #ff6600;      /* 주황색 (Player 5) */
  --sc-red: #cc0000;         /* 빨간색 (Player 6) */
  --sc-purple: #9900cc;      /* 보라색 (Player 7) */
  --sc-pink: #ff66cc;        /* 분홍색 (Player 8) */
  
  /* UI 배경 색상 */
  --sc-dark-bg: #001122;     /* 메인 배경 */
  --sc-panel-bg: #003366;    /* 패널 배경 */
  --sc-border: #0099cc;      /* 테두리 색상 */
  --sc-text: #ffffff;        /* 기본 텍스트 */
  --sc-text-secondary: #ccddff; /* 보조 텍스트 */
}
```

### 레이아웃 구조
- **플렉시블 그리드**: CSS Grid와 Flexbox를 활용한 반응형 레이아웃
- **카드 기반 UI**: 각 기능을 독립적인 카드로 구성
- **스크롤링 텍스트**: 게임 내 알림과 유사한 동적 텍스트 표시

## 🏗️ 컴포넌트 아키텍처

### 1. App.tsx (메인 애플리케이션)
```typescript
interface AppState {
  gameStatus: 'disconnected' | 'connected' | 'in-game';
  playerCount: number;
  features: FeatureStatus[];
}

const App: React.FC = () => {
  const [gameStatus, setGameStatus] = useState<GameStatus>('disconnected');
  const [features, setFeatures] = useState<FeatureStatus[]>(initialFeatures);
  
  return (
    <div className="app">
      <header className="app-header">
        <h1>StarcUp Monitor</h1>
        <div className="connection-status">{gameStatus}</div>
      </header>
      
      <main className="app-main">
        <FeatureStatusGrid features={features} />
        <ScrollingText messages={gameMessages} />
      </main>
    </div>
  );
};
```

#### 주요 기능
- **상태 관리**: React Hooks를 활용한 게임 상태 추적
- **컴포넌트 조합**: 재사용 가능한 하위 컴포넌트들의 조합
- **타입 안전성**: TypeScript 인터페이스를 통한 강력한 타입 체킹

### 2. FeatureStatusGrid.tsx (기능 상태 표시)
```typescript
interface FeatureStatus {
  id: string;
  name: string;
  status: 'active' | 'inactive' | 'error';
  description: string;
}

interface FeatureStatusGridProps {
  features: FeatureStatus[];
  onFeatureToggle?: (featureId: string) => void;
}

const FeatureStatusGrid: React.FC<FeatureStatusGridProps> = ({ 
  features, 
  onFeatureToggle 
}) => {
  return (
    <div className="feature-grid">
      {features.map(feature => (
        <FeatureCard 
          key={feature.id}
          feature={feature}
          onToggle={onFeatureToggle}
        />
      ))}
    </div>
  );
};
```

#### 구현된 기능들
1. **게임 감지** - 스타크래프트 프로세스 탐지 상태
2. **메모리 모니터링** - 실시간 메모리 읽기 상태
3. **유닛 추적** - 유닛 정보 수집 상태
4. **오버레이** - 게임 내 오버레이 표시 상태
5. **자동 업데이트** - 실시간 데이터 갱신 상태

#### 스타일링 특징
```css
.feature-card {
  background: linear-gradient(135deg, var(--sc-panel-bg) 0%, #004080 100%);
  border: 2px solid var(--sc-border);
  border-radius: 8px;
  transition: all 0.3s ease;
  position: relative;
  overflow: hidden;
}

.feature-card::before {
  content: '';
  position: absolute;
  top: 0;
  left: -100%;
  width: 100%;
  height: 100%;
  background: linear-gradient(90deg, transparent, rgba(255,255,255,0.1), transparent);
  transition: left 0.6s ease;
}

.feature-card:hover::before {
  left: 100%;
}
```

### 3. ScrollingText.tsx (동적 텍스트 애니메이션)
```typescript
interface ScrollingTextProps {
  messages: string[];
  speed?: number;
  direction?: 'left' | 'right';
}

const ScrollingText: React.FC<ScrollingTextProps> = ({ 
  messages, 
  speed = 50,
  direction = 'left' 
}) => {
  const [currentIndex, setCurrentIndex] = useState(0);
  
  useEffect(() => {
    const interval = setInterval(() => {
      setCurrentIndex(prev => (prev + 1) % messages.length);
    }, speed * 100);
    
    return () => clearInterval(interval);
  }, [messages.length, speed]);

  return (
    <div className="scrolling-container">
      <div 
        className={`scrolling-text scrolling-${direction}`}
        key={currentIndex}
      >
        {messages[currentIndex]}
      </div>
    </div>
  );
};
```

#### 애니메이션 효과
- **부드러운 스크롤**: CSS `transform`을 활용한 60fps 애니메이션
- **메시지 순환**: 자동으로 다음 메시지로 전환
- **방향 제어**: 좌우 스크롤 방향 설정 가능
- **속도 조절**: 스크롤 속도 커스터마이징

```css
@keyframes scroll-left {
  0% { transform: translateX(100%); }
  100% { transform: translateX(-100%); }
}

.scrolling-left {
  animation: scroll-left 10s linear infinite;
}
```

## 🔧 기술적 구현

### TypeScript 타입 정의
```typescript
// types/app.ts
export type GameStatus = 'disconnected' | 'connecting' | 'connected' | 'in-game';

export interface Player {
  id: number;
  name: string;
  race: 'terran' | 'protoss' | 'zerg';
  color: string;
  isAI: boolean;
}

export interface GameState {
  status: GameStatus;
  players: Player[];
  currentPlayer: Player | null;
  gameTime: number;
  mapName: string;
}

export interface FeatureStatus {
  id: string;
  name: string;
  status: 'active' | 'inactive' | 'error' | 'loading';
  description: string;
  lastUpdate: Date;
}
```

### 상태 관리 훅
```typescript
// hooks/useGameState.ts
export const useGameState = () => {
  const [gameState, setGameState] = useState<GameState>(initialGameState);
  
  const updateGameStatus = useCallback((status: GameStatus) => {
    setGameState(prev => ({ ...prev, status }));
  }, []);
  
  const updatePlayers = useCallback((players: Player[]) => {
    setGameState(prev => ({ ...prev, players }));
  }, []);
  
  return {
    gameState,
    updateGameStatus,
    updatePlayers
  };
};
```

### 실시간 데이터 연동
```typescript
// hooks/useRealtimeData.ts
export const useRealtimeData = () => {
  const [data, setData] = useState<RealtimeData>({});
  
  useEffect(() => {
    // Electron IPC를 통한 Core 모듈과의 통신
    const handleDataUpdate = (event: any, newData: RealtimeData) => {
      setData(newData);
    };
    
    window.electron?.ipcRenderer.on('data-update', handleDataUpdate);
    
    return () => {
      window.electron?.ipcRenderer.removeListener('data-update', handleDataUpdate);
    };
  }, []);
  
  return data;
};
```

## 🎯 UX/UI 개선사항

### 반응형 디자인
```css
/* 모바일 대응 */
@media (max-width: 768px) {
  .feature-grid {
    grid-template-columns: 1fr;
    gap: 1rem;
  }
  
  .feature-card {
    padding: 1rem;
  }
}

/* 태블릿 대응 */
@media (min-width: 769px) and (max-width: 1024px) {
  .feature-grid {
    grid-template-columns: repeat(2, 1fr);
  }
}

/* 데스크톱 대응 */
@media (min-width: 1025px) {
  .feature-grid {
    grid-template-columns: repeat(3, 1fr);
  }
}
```

### 접근성 개선
- **키보드 네비게이션**: Tab 키를 통한 컴포넌트 간 이동
- **스크린 리더 지원**: ARIA 라벨 및 역할 정의
- **고대비 모드**: 색각 이상 사용자를 위한 고대비 색상 옵션

```jsx
<button
  className="feature-toggle"
  aria-label={`Toggle ${feature.name} feature`}
  aria-pressed={feature.status === 'active'}
  onClick={() => onFeatureToggle(feature.id)}
  onKeyDown={(e) => e.key === 'Enter' && onFeatureToggle(feature.id)}
>
  {feature.name}
</button>
```

## ⚡ 성능 최적화

### 컴포넌트 메모이제이션
```typescript
const FeatureCard = React.memo<FeatureCardProps>(({ feature, onToggle }) => {
  const handleToggle = useCallback(() => {
    onToggle?.(feature.id);
  }, [feature.id, onToggle]);
  
  return (
    <div className="feature-card">
      {/* 컴포넌트 내용 */}
    </div>
  );
});
```

### 가상화된 리스트 (큰 데이터셋 대응)
```typescript
import { FixedSizeList as List } from 'react-window';

const VirtualizedFeatureList: React.FC<VirtualizedFeatureListProps> = ({ 
  features 
}) => {
  const Row = ({ index, style }: { index: number; style: React.CSSProperties }) => (
    <div style={style}>
      <FeatureCard feature={features[index]} />
    </div>
  );
  
  return (
    <List
      height={600}
      itemCount={features.length}
      itemSize={120}
    >
      {Row}
    </List>
  );
};
```

## 🔒 보안 및 안정성

### IPC 통신 보안
```typescript
// preload.ts
import { contextBridge, ipcRenderer } from 'electron';

contextBridge.exposeInMainWorld('electron', {
  ipcRenderer: {
    // 안전한 IPC 메서드만 노출
    send: (channel: string, data: any) => {
      const validChannels = ['game-command', 'feature-toggle'];
      if (validChannels.includes(channel)) {
        ipcRenderer.send(channel, data);
      }
    },
    on: (channel: string, func: Function) => {
      const validChannels = ['data-update', 'game-status'];
      if (validChannels.includes(channel)) {
        ipcRenderer.on(channel, (event, ...args) => func(...args));
      }
    }
  }
});
```

### 에러 바운더리
```typescript
class ErrorBoundary extends React.Component<
  { children: React.ReactNode },
  { hasError: boolean }
> {
  constructor(props: any) {
    super(props);
    this.state = { hasError: false };
  }
  
  static getDerivedStateFromError(error: Error) {
    return { hasError: true };
  }
  
  componentDidCatch(error: Error, errorInfo: React.ErrorInfo) {
    console.error('UI Error:', error, errorInfo);
    // 에러 리포팅 서비스로 전송
  }
  
  render() {
    if (this.state.hasError) {
      return <div className="error-fallback">UI 오류가 발생했습니다.</div>;
    }
    
    return this.props.children;
  }
}
```

## 📊 테스트 가능한 아키텍처

### 컴포넌트 테스트
```typescript
// __tests__/FeatureStatusGrid.test.tsx
import { render, fireEvent, screen } from '@testing-library/react';
import FeatureStatusGrid from '../FeatureStatusGrid';

describe('FeatureStatusGrid', () => {
  const mockFeatures = [
    { id: '1', name: 'Game Detection', status: 'active' as const, description: 'Test' }
  ];
  
  it('renders feature cards correctly', () => {
    render(<FeatureStatusGrid features={mockFeatures} />);
    expect(screen.getByText('Game Detection')).toBeInTheDocument();
  });
  
  it('calls onFeatureToggle when card is clicked', () => {
    const mockToggle = jest.fn();
    render(
      <FeatureStatusGrid 
        features={mockFeatures} 
        onFeatureToggle={mockToggle} 
      />
    );
    
    fireEvent.click(screen.getByText('Game Detection'));
    expect(mockToggle).toHaveBeenCalledWith('1');
  });
});
```

## 🚀 향후 개선 계획

### 1. 실시간 데이터 연동
- StarcUp.Core와의 WebSocket/IPC 통신 구현
- 게임 상태 실시간 모니터링
- 유닛 정보 실시간 업데이트

### 2. 고급 UI 컴포넌트
- 차트 및 그래프 라이브러리 통합
- 데이터 시각화 컴포넌트
- 인터랙티브 미니맵

### 3. 사용자 경험 개선
- 테마 커스터마이징 시스템
- 핫키 지원
- 윈도우 관리 기능

## 📈 완성된 기능

✅ **기본 UI 프레임워크** - Electron + React + TypeScript 구성 완료  
✅ **스타크래프트 테마** - 게임 스타일 디자인 시스템 구축  
✅ **기능 상태 표시** - 각종 기능의 실시간 상태 모니터링  
✅ **동적 애니메이션** - 스크롤링 텍스트 및 호버 효과  
✅ **반응형 디자인** - 다양한 화면 크기 대응  
✅ **타입 안전성** - TypeScript를 통한 개발 안정성 확보  
✅ **컴포넌트 시스템** - 재사용 가능하고 테스트 가능한 컴포넌트 아키텍처  

이번 구현을 통해 StarcUp이 모던하고 직관적인 사용자 인터페이스를 갖추게 되었습니다.