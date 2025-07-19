# 스타크래프트 윈도우 위치 동기화 시스템 설계

## 📋 개요

기존 StarcUp.Core의 윈도우 인프라스트럭처를 활용하여 스타크래프트 윈도우의 위치/크기 변경을 감지하고, Named Pipe를 통해 StarcUp.UI에 실시간으로 전달하는 시스템을 설계합니다.

## 🏗️ 현재 아키텍처 분석

### 기존 구현 상태 ✅
- **WindowManager.cs**: 윈도우 이벤트 후킹 및 감지 완료
- **WindowInfo.cs**: 윈도우 상태 정보 구조체 완비
- **WindowsAPI.cs**: Windows API 래퍼 완성
- **Named Pipe 통신**: 기존 GameDetector → UI 통신 채널 존재

### 활용 가능한 이벤트
- `WindowPositionChanged`: 위치/크기 변경 감지
- `WindowActivated/Deactivated`: 포어그라운드 상태 변경
- `EVENT_OBJECT_LOCATIONCHANGE`: 실시간 위치 변경

## 🎯 설계 목표

1. **기존 아키텍처 재활용**: 현재 WindowManager 시스템 활용
2. **Named Pipe 확장**: 기존 통신 채널에 윈도우 이벤트 추가
3. **최소 코드 변경**: 기존 구조를 최대한 유지하며 확장
4. **성능 최적화**: 불필요한 이벤트 필터링

## 🔧 구현 계획

### 1단계: Core 측 윈도우 추적 서비스 확장

#### `IWindowPositionService` 인터페이스 (신규)
```csharp
public interface IWindowPositionService : IDisposable
{
    event Action<WindowPositionData> WindowPositionChanged;
    void StartTracking(IntPtr windowHandle);
    void StopTracking();
    bool IsTracking { get; }
    WindowPositionData GetCurrentPosition();
}
```

#### `WindowPositionData` 데이터 모델 (신규)
```csharp
public class WindowPositionData
{
    public int X { get; set; }
    public int Y { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public int ClientX { get; set; }
    public int ClientY { get; set; }
    public int ClientWidth { get; set; }
    public int ClientHeight { get; set; }
    public bool IsMinimized { get; set; }
    public bool IsMaximized { get; set; }
    public bool IsVisible { get; set; }
    public DateTime Timestamp { get; set; }
}
```

#### `WindowPositionService` 구현 (신규)
```csharp
public class WindowPositionService : IWindowPositionService
{
    private readonly IWindowManager _windowManager;
    private readonly ILogger<WindowPositionService> _logger;
    private IntPtr _trackingWindowHandle;
    private WindowPositionData _lastPosition;
    
    public event Action<WindowPositionData> WindowPositionChanged;
    
    public WindowPositionService(IWindowManager windowManager, ILogger<WindowPositionService> logger)
    {
        _windowManager = windowManager;
        _logger = logger;
        
        // 기존 WindowManager 이벤트 구독
        _windowManager.WindowPositionChanged += OnWindowPositionChanged;
    }
    
    private void OnWindowPositionChanged(IntPtr windowHandle)
    {
        // 추적 중인 윈도우인지 확인
        if (_trackingWindowHandle != windowHandle) return;
        
        var windowInfo = _windowManager.GetWindowInfo(windowHandle);
        var positionData = ConvertToPositionData(windowInfo);
        
        // 중복 이벤트 필터링
        if (!HasPositionChanged(positionData)) return;
        
        _lastPosition = positionData;
        WindowPositionChanged?.Invoke(positionData);
        
        _logger.LogDebug("스타크래프트 윈도우 위치 변경: {Position}", positionData);
    }
}
```

### 2단계: Named Pipe 메시지 확장

#### 기존 통신 구조 확장
```csharp
// 기존: GameStatusMessage
public class GameStatusMessage
{
    public bool IsGameRunning { get; set; }
    public bool IsInGame { get; set; }
    // 기존 필드들...
}

// 신규: WindowPositionMessage 추가
public class WindowPositionMessage
{
    public string MessageType => "WindowPosition";
    public WindowPositionData Position { get; set; }
    public DateTime Timestamp { get; set; }
}

// 통합: 기존 메시지 시스템에 새 타입 추가
public enum MessageType
{
    GameStatus,
    WindowPosition // 신규
}
```

#### Named Pipe 송신 로직 확장
```csharp
public class GameCommunicationService // 기존 클래스 확장
{
    private readonly IWindowPositionService _windowPositionService;
    
    public GameCommunicationService(
        // 기존 의존성들...,
        IWindowPositionService windowPositionService)
    {
        _windowPositionService = windowPositionService;
        
        // 윈도우 위치 변경 이벤트 구독
        _windowPositionService.WindowPositionChanged += OnWindowPositionChanged;
    }
    
    private async void OnWindowPositionChanged(WindowPositionData position)
    {
        var message = new WindowPositionMessage 
        { 
            Position = position,
            Timestamp = DateTime.UtcNow
        };
        
        await SendMessageToPipe(message);
    }
}
```

### 3단계: UI 측 메시지 수신 및 처리

#### Named Pipe 수신 확장 (StarcUp.UI)
```typescript
// 기존: GameStatusReceiver 확장
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

interface WindowPositionMessage {
  messageType: 'WindowPosition'
  position: WindowPositionData
  timestamp: string
}

class GameDataReceiver {
  private windowPositionCallbacks: ((data: WindowPositionData) => void)[] = []
  
  onWindowPositionChanged(callback: (data: WindowPositionData) => void): void {
    this.windowPositionCallbacks.push(callback)
  }
  
  private handleMessage(message: any): void {
    switch (message.messageType) {
      case 'GameStatus':
        // 기존 로직
        break
      case 'WindowPosition':
        this.handleWindowPositionMessage(message as WindowPositionMessage)
        break
    }
  }
  
  private handleWindowPositionMessage(message: WindowPositionMessage): void {
    this.windowPositionCallbacks.forEach(callback => {
      callback(message.position)
    })
  }
}
```

### 4단계: OverlayAutoManager 통합

#### 윈도우 위치 동기화 로직
```typescript
export class OverlayAutoManager implements IOverlayAutoManager {
  private gameDataReceiver: GameDataReceiver
  private currentStarCraftBounds: WindowPositionData | null = null
  
  constructor(
    windowManager: IWindowManager,
    gameDataReceiver: GameDataReceiver
  ) {
    this.windowManager = windowManager
    this.gameDataReceiver = gameDataReceiver
    
    // 윈도우 위치 변경 이벤트 구독
    this.gameDataReceiver.onWindowPositionChanged(this.onStarCraftWindowChanged.bind(this))
  }
  
  private onStarCraftWindowChanged(position: WindowPositionData): void {
    this.currentStarCraftBounds = position
    
    if (this.shouldShowOverlay()) {
      this.syncOverlayWithStarCraft(position)
    }
  }
  
  private syncOverlayWithStarCraft(position: WindowPositionData): void {
    // 클라이언트 영역 기준으로 오버레이 위치 조정
    this.windowManager.setOverlayPosition(position.clientX, position.clientY)
    this.windowManager.setOverlaySize(position.clientWidth, position.clientHeight)
    
    // Hello World 중앙 위치 계산
    const centerX = position.clientWidth / 2
    const centerY = position.clientHeight / 2
    
    // 오버레이에 중앙 위치 정보 전달
    this.windowManager.sendToOverlayWindow('update-center-position', {
      x: centerX,
      y: centerY
    })
    
    console.log(`🎯 오버레이 동기화: ${position.clientX},${position.clientY} ${position.clientWidth}x${position.clientHeight}`)
  }
}
```

## 🔄 전체 데이터 흐름

```
1. 스타크래프트 윈도우 변경
   ↓
2. WindowManager (Core) → 이벤트 감지
   ↓
3. WindowPositionService → 데이터 변환
   ↓
4. GameCommunicationService → Named Pipe 송신
   ↓
5. GameDataReceiver (UI) → 메시지 수신
   ↓
6. OverlayAutoManager → 오버레이 위치 계산
   ↓
7. WindowManager (UI) → 오버레이 윈도우 조정
   ↓
8. Overlay React Component → Hello World 중앙 배치
```

## 🎯 성능 최적화

### 이벤트 필터링
```csharp
private bool HasPositionChanged(WindowPositionData newPosition)
{
    if (_lastPosition == null) return true;
    
    const int threshold = 5; // 5픽셀 이하 변경은 무시
    
    return Math.Abs(newPosition.X - _lastPosition.X) > threshold ||
           Math.Abs(newPosition.Y - _lastPosition.Y) > threshold ||
           Math.Abs(newPosition.Width - _lastPosition.Width) > threshold ||
           Math.Abs(newPosition.Height - _lastPosition.Height) > threshold ||
           newPosition.IsMinimized != _lastPosition.IsMinimized ||
           newPosition.IsMaximized != _lastPosition.IsMaximized;
}
```

### Throttling
```csharp
private readonly SemaphoreSlim _throttleSemaphore = new(1, 1);
private DateTime _lastSentTime = DateTime.MinValue;
private const int ThrottleMs = 50; // 50ms 제한

private async void OnWindowPositionChanged(WindowPositionData position)
{
    if (!await _throttleSemaphore.WaitAsync(0)) return; // 이미 처리 중이면 무시
    
    try
    {
        var now = DateTime.UtcNow;
        if ((now - _lastSentTime).TotalMilliseconds < ThrottleMs) return;
        
        await SendWindowPositionMessage(position);
        _lastSentTime = now;
    }
    finally
    {
        _throttleSemaphore.Release();
    }
}
```

## 🧪 테스트 전략

### 단위 테스트
```csharp
[Test]
public void WindowPositionService_ShouldFilterDuplicateEvents()
{
    // Given
    var service = new WindowPositionService(_mockWindowManager, _mockLogger);
    var callCount = 0;
    service.WindowPositionChanged += _ => callCount++;
    
    // When
    // 동일한 위치로 두 번 변경
    _mockWindowManager.TriggerPositionChanged(samePosition);
    _mockWindowManager.TriggerPositionChanged(samePosition);
    
    // Then
    callCount.Should().Be(1);
}
```

### 통합 테스트
- 실제 스타크래프트 윈도우 이동 시나리오
- Named Pipe 통신 지연 시간 측정
- 오버레이 동기화 정확도 검증

## 📋 구현 순서

1. **WindowPositionService 구현** (Core)
2. **Named Pipe 메시지 확장** (Core)
3. **UI 수신 로직 구현** (UI)
4. **OverlayAutoManager 통합** (UI)
5. **Hello World 중앙 배치** (UI)
6. **성능 최적화 및 테스트**

## 🔧 DI 컨테이너 등록

### Core (ServiceRegistration.cs)
```csharp
services.AddSingleton<IWindowPositionService, WindowPositionService>();
```

### UI (의존성 주입)
```typescript
// GameDataReceiver를 OverlayAutoManager에 주입
const gameDataReceiver = container.get<GameDataReceiver>();
const overlayAutoManager = new OverlayAutoManager(windowManager, gameDataReceiver);
```

이 설계는 기존 아키텍처를 최대한 활용하면서 새로운 기능을 깔끔하게 추가할 수 있는 방법입니다. 어떤 부분부터 구현해보시겠습니까?