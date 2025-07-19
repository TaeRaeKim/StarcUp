# WindowManager 이벤트훅 메시지 루프 문제 분석

## 📅 작성일
2025년 7월 19일

## 🔍 문제 상황

### 배경
- **StarcUp Legacy (Windows Forms)**: WindowManager의 SetWinEventHook이 정상 동작
- **StarcUp.Core (콘솔/라이브러리)**: 동일한 WindowManager 코드가 이벤트훅 콜백 실행 안 됨

### 증상
- SetWinEventHook 호출은 성공 (반환값 확인)
- 윈도우 이벤트 콜백 함수가 전혀 실행되지 않음
- 메모리 누수나 크래시는 없음

## 🔬 원인 분석

### 1. 핵심 원인: Windows 메시지 루프 부재

**Windows Forms 환경**:
```csharp
Application.Run(); // 자동 메시지 루프 실행
```

**콘솔/라이브러리 환경**:
```csharp
// 메시지 루프 없음 - 이벤트 콜백이 실행되지 않음
Console.ReadLine(); // 단순 대기만
```

### 2. SetWinEventHook 동작 원리

SetWinEventHook은 **Windows 메시지 시스템**을 통해 콜백을 전달합니다:

1. 시스템에서 윈도우 이벤트 발생
2. 해당 이벤트를 등록된 훅의 스레드 메시지 큐에 전달
3. **메시지 루프가 메시지를 처리하며 콜백 함수 호출**
4. ❌ 메시지 루프가 없으면 메시지가 처리되지 않음

### 3. 환경별 차이점 비교

| 구분 | StarcUp Legacy | StarcUp.Core |
|------|----------------|--------------|
| **UI 프레임워크** | Windows Forms | 콘솔/라이브러리 |
| **메시지 루프** | ✅ `Application.Run()` | ❌ 없음 |
| **스레드 컨텍스트** | UI 메시지 펌프 | 메인 스레드 블로킹 |
| **이벤트 처리** | ✅ 자동 처리 | ❌ 처리되지 않음 |

## 💡 해결책

### 1. Windows 메시지 루프 구현

```csharp
// Windows API 추가
[DllImport("user32.dll")]
private static extern bool GetMessage(out MSG lpMsg, IntPtr hWnd, uint wMsgFilterMin, uint wMsgFilterMax);

[DllImport("user32.dll")]
private static extern bool TranslateMessage(ref MSG lpMsg);

[DllImport("user32.dll")]
private static extern IntPtr DispatchMessage(ref MSG lpMsg);

[DllImport("user32.dll")]
private static extern void PostQuitMessage(int nExitCode);

// 메시지 구조체
[StructLayout(LayoutKind.Sequential)]
public struct MSG
{
    public IntPtr hwnd;
    public uint message;
    public IntPtr wParam;
    public IntPtr lParam;
    public uint time;
    public POINT pt;
}

// 메시지 루프 실행
private static void RunMessageLoop()
{
    MSG msg;
    while (isRunning && GetMessage(out msg, IntPtr.Zero, 0, 0))
    {
        TranslateMessage(ref msg);
        DispatchMessage(ref msg);
    }
}
```

### 2. 적용 전후 비교

**적용 전**:
```csharp
// 이벤트훅 설정
hookHandle = SetWinEventHook(...);

// 단순 대기 - 메시지 처리 안 됨
Console.ReadLine();
```

**적용 후**:
```csharp
// 이벤트훅 설정
hookHandle = SetWinEventHook(...);

// 별도 스레드에서 사용자 입력 처리
Task.Run(() => {
    Console.ReadLine();
    isRunning = false;
    PostQuitMessage(0);
});

// 메시지 루프 실행 - 이벤트 처리됨
RunMessageLoop();
```

### 3. 권장 구현 패턴

#### A. 라이브러리 내부에서 처리
```csharp
public class WindowManager : IDisposable
{
    private bool _isRunning;
    private Task _messageLoopTask;
    
    public void StartEventHook()
    {
        // 훅 설정
        SetupHook();
        
        // 전용 스레드에서 메시지 루프 실행
        _messageLoopTask = Task.Run(RunMessageLoop);
    }
    
    private void RunMessageLoop()
    {
        // Windows 메시지 루프 구현
    }
}
```

#### B. 사용자 코드에서 처리
```csharp
// 사용자가 직접 메시지 루프 실행
var windowManager = new WindowManager();
windowManager.StartEventHook();

// 애플리케이션 메시지 루프
Application.Run(); // 또는 사용자 정의 메시지 루프
```

## 🎯 StarcUp.Core 적용 방안

### 1. WindowManager 개선안

```csharp
public interface IWindowManager : IDisposable
{
    bool StartEventMonitoring();
    void StopEventMonitoring();
    event EventHandler<WindowEventArgs> WindowChanged;
}

public class WindowManager : IWindowManager
{
    private CancellationTokenSource _cancellationTokenSource;
    private Task _messageLoopTask;
    
    public bool StartEventMonitoring()
    {
        if (!SetupHook()) return false;
        
        _cancellationTokenSource = new CancellationTokenSource();
        _messageLoopTask = Task.Run(() => RunMessageLoop(_cancellationTokenSource.Token));
        
        return true;
    }
    
    private void RunMessageLoop(CancellationToken cancellationToken)
    {
        MSG msg;
        while (!cancellationToken.IsCancellationRequested && 
               GetMessage(out msg, IntPtr.Zero, 0, 0))
        {
            TranslateMessage(ref msg);
            DispatchMessage(ref msg);
        }
    }
}
```

### 2. 의존성 주입 업데이트

```csharp
// ServiceRegistration.cs
public static void RegisterServices(IServiceContainer container)
{
    // WindowManager를 싱글톤으로 등록하고 자동 시작
    container.RegisterSingleton<IWindowManager>(() => {
        var manager = new WindowManager();
        manager.StartEventMonitoring(); // 자동 시작
        return manager;
    });
}
```

## 🔧 테스트 및 검증

### 1. 단위 테스트
```csharp
[Test]
public async Task WindowManager_ShouldDetectWindowEvents()
{
    // Given
    var windowManager = new WindowManager();
    var eventReceived = false;
    
    windowManager.WindowChanged += (s, e) => eventReceived = true;
    
    // When
    windowManager.StartEventMonitoring();
    
    // 윈도우 변경 시뮬레이션
    await SimulateWindowChange();
    
    // Then
    Assert.IsTrue(eventReceived);
}
```

### 2. 통합 테스트
- StarcUp.Test 프로젝트에서 실제 윈도우 이벤트 감지 테스트
- 다양한 실행 환경에서의 동작 확인

## 📚 참고 자료

### Windows API 문서
- [SetWinEventHook function](https://docs.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-setwineventhook)
- [GetMessage function](https://docs.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-getmessage)
- [Windows Message Loop](https://docs.microsoft.com/en-us/windows/win32/winmsg/using-messages-and-message-queues)

### 관련 이슈
- Windows.Practice 프로젝트의 ForegroundWindowTracker와 WindowEventTracker 비교 분석 결과
- 콘솔 애플리케이션에서 SetWinEventHook 동작 실험

## 📝 결론

**StarcUp Legacy에서 StarcUp.Core로 전환 시 WindowManager 이벤트훅이 동작하지 않던 근본 원인은 Windows 메시지 루프의 부재였습니다.**

이 문제는 다음과 같이 해결해야 합니다:

1. **즉시 해결**: WindowManager 내부에 전용 메시지 루프 스레드 구현
2. **장기 계획**: StarcUp.Core의 모든 Windows API 기반 컴포넌트에 대한 메시지 루프 전략 수립
3. **문서화**: 향후 유사한 문제 방지를 위한 개발 가이드라인 작성

이를 통해 StarcUp.Core가 Windows Forms 환경과 동일한 수준의 시스템 이벤트 감지 기능을 제공할 수 있습니다.