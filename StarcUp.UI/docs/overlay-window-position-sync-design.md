# 오버레이 윈도우 위치 동기화 시스템 설계

## 📋 개요

StarcUp.Core에서 전송되는 스타크래프트 윈도우 위치 정보를 받아서 오버레이 윈도우의 위치와 크기를 실시간으로 동기화하는 시스템을 설계합니다.

## 🏗️ 현재 아키텍처 분석

### 기존 구현 상태 ✅

#### Core 측 (완료)
- **WindowPositionData**: 윈도우 위치/크기/클라이언트 영역 정보 모델
- **CommunicationService**: Named Pipe를 통한 윈도우 위치 이벤트 전송
- **실시간 감지**: 스타크래프트 게임 감지 시 자동으로 윈도우 모니터링 시작
- **성능 최적화**: 50ms Throttling, 5px 중복 이벤트 필터링

#### UI 측 (기존 구조)
- **CoreCommunicationService**: Named Pipe 수신 및 이벤트 처리
- **OverlayAutoManager**: 오버레이 표시/숨김 자동 관리
- **WindowManager**: Electron 윈도우 위치/크기 제어
- **이벤트 시스템**: 게임 감지, 인게임 상태 변경 이벤트 처리

### 현재 데이터 흐름

```
[StarcUp.Core] 스타크래프트 윈도우 변경
    ↓ Named Pipe
[StarcUp.UI] CoreCommunicationService → 이벤트 수신
    ↓
[OverlayAutoManager] 표시/숨김 결정
    ↓
[WindowManager] 오버레이 윈도우 제어
```

## 🎯 설계 목표

1. **기존 아키텍처 활용**: 현재 CoreCommunicationService와 OverlayAutoManager 확장
2. **실시간 동기화**: 스타크래프트 윈도우 변경 시 즉시 오버레이 위치 업데이트
3. **클라이언트 영역 기반**: 게임 화면 영역에 정확히 맞춤
4. **성능 최적화**: 불필요한 업데이트 방지
5. **오류 복구**: 스타크래프트 윈도우 손실 시 적절한 처리

## 🔧 구현 계획

### 1단계: Named Pipe 수신부 확장

#### WindowPositionData 인터페이스 (신규)
```typescript
interface WindowPositionData {
  x: number
  y: number
  width: number
  height: number
  clientX: number
  clientY: number
  clientWidth: number
  clientHeight: number
  isMinimized: boolean
  isMaximized: boolean
  isVisible: boolean
  timestamp: string
}

interface WindowPositionEvent {
  eventType: 'window-position-changed' | 'window-size-changed'
  windowPosition: WindowPositionData
}
```

#### CoreCommunicationService 확장
```typescript
export class CoreCommunicationService {
  private windowPositionChangedCallback: ((data: WindowPositionData) => void) | null = null
  
  // 기존 setupEventHandlers()에 추가
  private setupEventHandlers(): void {
    // ... 기존 이벤트 핸들러들
    
    // 윈도우 위치 변경 이벤트 핸들러
    this.namedPipeService.onEvent('window-position-changed', (data: WindowPositionEvent) => {
      console.log('🪟 스타크래프트 윈도우 위치 변경:', data.windowPosition)
      if (this.windowPositionChangedCallback) {
        this.windowPositionChangedCallback(data.windowPosition)
      }
    })
    
    this.namedPipeService.onEvent('window-size-changed', (data: WindowPositionEvent) => {
      console.log('🪟 스타크래프트 윈도우 크기 변경:', data.windowPosition)
      if (this.windowPositionChangedCallback) {
        this.windowPositionChangedCallback(data.windowPosition)
      }
    })
  }
  
  // 윈도우 위치 변경 콜백 등록
  onWindowPositionChanged(callback: (data: WindowPositionData) => void): void {
    this.windowPositionChangedCallback = callback
  }
  
  // 윈도우 위치 변경 콜백 제거
  offWindowPositionChanged(): void {
    this.windowPositionChangedCallback = null
  }
}
```

### 2단계: OverlayAutoManager 확장

#### 위치 동기화 기능 추가
```typescript
export class OverlayAutoManager implements IOverlayAutoManager {
  private windowManager: IWindowManager
  private currentStarCraftPosition: WindowPositionData | null = null
  
  // 윈도우 위치 동기화 설정
  private enablePositionSync = true
  private lastSyncTime = 0
  private readonly syncThrottleMs = 16 // 60fps (16ms)
  
  constructor(windowManager: IWindowManager) {
    this.windowManager = windowManager
  }
  
  /**
   * 스타크래프트 윈도우 위치 변경 처리
   */
  updateStarCraftWindowPosition(position: WindowPositionData): void {
    this.currentStarCraftPosition = position
    
    console.log(`🪟 스타크래프트 윈도우 업데이트: ${position.clientX},${position.clientY} ${position.clientWidth}x${position.clientHeight}`)
    
    // 오버레이가 표시 중이고 위치 동기화가 활성화된 경우에만 동기화
    if (this.shouldSyncPosition()) {
      this.syncOverlayPosition(position)
    }
  }
  
  /**
   * 위치 동기화 여부 결정
   */
  private shouldSyncPosition(): boolean {
    if (!this.enablePositionSync) return false
    if (!this.isAutoModeEnabled) return false
    if (!this.windowManager.isOverlayWindowVisible()) return false
    if (!this.currentStarCraftPosition) return false
    
    // 스타크래프트가 최소화되었거나 보이지 않으면 동기화 안함
    if (this.currentStarCraftPosition.isMinimized || !this.currentStarCraftPosition.isVisible) {
      return false
    }
    
    // Throttling 체크
    const now = Date.now()
    if (now - this.lastSyncTime < this.syncThrottleMs) {
      return false
    }
    
    return true
  }
  
  /**
   * 오버레이 위치 동기화 실행
   */
  private syncOverlayPosition(position: WindowPositionData): void {
    try {
      // 클라이언트 영역 기준으로 오버레이 위치 설정
      this.windowManager.setOverlayPosition(position.clientX, position.clientY)
      this.windowManager.setOverlaySize(position.clientWidth, position.clientHeight)
      
      this.lastSyncTime = Date.now()
      
      console.log(`✅ 오버레이 동기화 완료: [${position.clientX}, ${position.clientY}] ${position.clientWidth}x${position.clientHeight}`)
      
      // 오버레이 내부 React 컴포넌트에 중앙 위치 정보 전달
      const centerX = position.clientWidth / 2
      const centerY = position.clientHeight / 2
      
      this.windowManager.sendToOverlayWindow('update-center-position', {
        x: centerX,
        y: centerY,
        gameAreaBounds: {
          x: position.clientX,
          y: position.clientY,
          width: position.clientWidth,
          height: position.clientHeight
        }
      })
      
    } catch (error) {
      console.error('❌ 오버레이 위치 동기화 실패:', error)
    }
  }
  
  /**
   * 위치 동기화 활성화/비활성화
   */
  enablePositionSync(): void {
    this.enablePositionSync = true
    console.log('🎯 오버레이 위치 동기화 활성화')
    
    // 현재 스타크래프트 위치가 있으면 즉시 동기화
    if (this.currentStarCraftPosition && this.shouldSyncPosition()) {
      this.syncOverlayPosition(this.currentStarCraftPosition)
    }
  }
  
  disablePositionSync(): void {
    this.enablePositionSync = false
    console.log('🎯 오버레이 위치 동기화 비활성화')
  }
  
  /**
   * 기존 updateOverlayVisibility 확장
   */
  private updateOverlayVisibility(): void {
    if (!this.isAutoModeEnabled) {
      return
    }

    const shouldShowOverlay = this.isInGame && this.isStarcraftInForeground

    if (shouldShowOverlay) {
      console.log('✅ 조건 만족 - overlay 표시 (InGame + Foreground)')
      this.windowManager.showOverlay()
      
      // 오버레이 표시 시 현재 스타크래프트 위치로 동기화
      if (this.currentStarCraftPosition && this.enablePositionSync) {
        this.syncOverlayPosition(this.currentStarCraftPosition)
      }
    } else {
      console.log(`❌ 조건 불만족 - overlay 숨김 (InGame: ${this.isInGame}, Foreground: ${this.isStarcraftInForeground})`)
      this.windowManager.hideOverlay()
    }
  }
  
  /**
   * 현재 스타크래프트 윈도우 정보 조회
   */
  getCurrentStarCraftPosition(): WindowPositionData | null {
    return this.currentStarCraftPosition ? { ...this.currentStarCraftPosition } : null
  }
  
  /**
   * 리소스 정리
   */
  dispose(): void {
    this.disableAutoMode()
    this.currentStarCraftPosition = null
    console.log('🧹 OverlayAutoManager 정리 완료')
  }
}
```

### 3단계: 서비스 통합

#### ServiceContainer 업데이트
```typescript
// electron/src/services/ServiceContainer.ts
export class ServiceContainer {
  // ... 기존 서비스들
  
  async initializeServices(): Promise<void> {
    try {
      // ... 기존 초기화
      
      // 윈도우 위치 동기화 이벤트 연결
      this.coreCommunicationService.onWindowPositionChanged((position) => {
        this.overlayAutoManager.updateStarCraftWindowPosition(position)
      })
      
      console.log('✅ 윈도우 위치 동기화 시스템 초기화 완료')
    } catch (error) {
      console.error('❌ 서비스 초기화 실패:', error)
      throw error
    }
  }
}
```

### 4단계: React 오버레이 컴포넌트 확장

#### 오버레이 컴포넌트에서 중앙 위치 정보 활용
```typescript
// src/overlay/OverlayApp.tsx
interface CenterPositionData {
  x: number
  y: number
  gameAreaBounds: {
    x: number
    y: number
    width: number
    height: number
  }
}

export function OverlayApp() {
  const [centerPosition, setCenterPosition] = useState<CenterPositionData | null>(null)
  
  useEffect(() => {
    // Electron 메인 프로세스로부터 중앙 위치 정보 수신
    const unsubscribe = window.electronAPI.onUpdateCenterPosition((data: CenterPositionData) => {
      setCenterPosition(data)
    })
    
    return unsubscribe
  }, [])
  
  return (
    <div className="overlay-container">
      {centerPosition && (
        <div 
          className="hello-world"
          style={{
            position: 'absolute',
            left: `${centerPosition.x}px`,
            top: `${centerPosition.y}px`,
            transform: 'translate(-50%, -50%)',
            color: 'white',
            fontSize: '24px',
            fontWeight: 'bold',
            textShadow: '2px 2px 4px rgba(0,0,0,0.8)',
            zIndex: 9999,
            pointerEvents: 'none'
          }}
        >
          Hello World
        </div>
      )}
      
      {/* 디버그 정보 (개발 환경에서만) */}
      {process.env.NODE_ENV === 'development' && centerPosition && (
        <div className="debug-info" style={{
          position: 'absolute',
          top: '10px',
          left: '10px',
          background: 'rgba(0,0,0,0.7)',
          color: 'white',
          padding: '10px',
          fontSize: '12px',
          borderRadius: '4px'
        }}>
          <div>Center: {centerPosition.x}, {centerPosition.y}</div>
          <div>Game Area: {centerPosition.gameAreaBounds.width}x{centerPosition.gameAreaBounds.height}</div>
          <div>Position: {centerPosition.gameAreaBounds.x}, {centerPosition.gameAreaBounds.y}</div>
        </div>
      )}
    </div>
  )
}
```

#### preload.ts 확장
```typescript
// electron/preload.ts
const electronAPI = {
  // ... 기존 API들
  
  onUpdateCenterPosition: (callback: (data: any) => void) => 
    ipcRenderer.on('update-center-position', (_event, data) => callback(data)),
    
  removeUpdateCenterPositionListener: () => 
    ipcRenderer.removeAllListeners('update-center-position')
}

contextBridge.exposeInMainWorld('electronAPI', electronAPI)
```

## 🔄 전체 데이터 흐름

```
1. 스타크래프트 윈도우 변경
   ↓
2. [StarcUp.Core] WindowManager → 이벤트 감지
   ↓
3. [StarcUp.Core] CommunicationService → Named Pipe 전송
   ↓
4. [StarcUp.UI] CoreCommunicationService → 이벤트 수신
   ↓
5. [StarcUp.UI] OverlayAutoManager → 위치 동기화 로직
   ↓
6. [StarcUp.UI] WindowManager → 오버레이 윈도우 이동/크기 변경
   ↓
7. [StarcUp.UI] React Overlay → 중앙 위치 계산 및 "Hello World" 배치
```

## 🎯 성능 최적화

### 1. Throttling
- **Core**: 50ms 제한으로 과도한 이벤트 전송 방지
- **UI**: 16ms (60fps) 제한으로 부드러운 동기화

### 2. 중복 방지
- **Core**: 5픽셀 이하 변경 무시
- **UI**: 동일한 위치로의 중복 설정 방지

### 3. 조건부 동기화
```typescript
private shouldSyncPosition(): boolean {
  return this.enablePositionSync && 
         this.isAutoModeEnabled && 
         this.windowManager.isOverlayWindowVisible() && 
         this.currentStarCraftPosition && 
         !this.currentStarCraftPosition.isMinimized && 
         this.currentStarCraftPosition.isVisible &&
         (Date.now() - this.lastSyncTime >= this.syncThrottleMs)
}
```

### 4. 메모리 효율성
- WindowPositionData 객체 재사용
- 불필요한 객체 생성 최소화

## 🧪 테스트 시나리오

### 기본 기능 테스트
1. **스타크래프트 윈도우 이동**: 오버레이 즉시 추적
2. **스타크래프트 윈도우 크기 변경**: 오버레이 크기 동기화
3. **스타크래프트 최소화/복원**: 오버레이 숨김/표시
4. **스타크래프트 최대화**: 전체 화면 오버레이

### 성능 테스트
1. **빠른 연속 이동**: Throttling 동작 확인
2. **미세한 움직임**: 중복 이벤트 필터링 확인
3. **장시간 사용**: 메모리 누수 없음 확인

### 오류 복구 테스트
1. **스타크래프트 강제 종료**: 오버레이 적절히 숨김
2. **Named Pipe 연결 끊김**: 재연결 시 위치 재동기화
3. **오버레이 윈도우 오류**: 복구 메커니즘 동작

## 📋 구현 우선순위

### Phase 1 (높음)
1. WindowPositionData 타입 정의
2. CoreCommunicationService 이벤트 핸들러 추가
3. OverlayAutoManager 위치 동기화 로직 구현

### Phase 2 (중간)
1. React 오버레이 컴포넌트 중앙 배치 구현
2. 성능 최적화 (Throttling, 중복 방지)
3. 디버그 정보 표시

### Phase 3 (낮음)
1. 고급 오류 처리 및 복구
2. 사용자 설정 (동기화 활성화/비활성화)
3. 추가 테스트 및 최적화

## 🔧 설정 옵션

### 사용자 설정 가능 항목
```typescript
interface OverlaySyncSettings {
  enablePositionSync: boolean      // 위치 동기화 활성화
  syncThrottleMs: number          // 동기화 Throttling 간격
  offsetX: number                 // X축 오프셋
  offsetY: number                 // Y축 오프셋
  scaleX: number                  // X축 스케일 (1.0 = 100%)
  scaleY: number                  // Y축 스케일 (1.0 = 100%)
}
```

이 설계를 통해 스타크래프트 윈도우와 완벽하게 동기화되는 오버레이 시스템을 구현할 수 있습니다!