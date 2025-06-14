using System;
using System.Runtime.InteropServices;

namespace StarcUp.Infrastructure.Windows
{
    /// <summary>
    /// 윈도우 정보 구조체
    /// </summary>
    public struct WindowInfo
    {
        public WindowsAPI.RECT WindowRect;
        public WindowsAPI.RECT ClientRect;
        public bool IsMinimized;
        public bool IsMaximized;
        public bool IsFullscreen;
        public int TitleBarHeight;
    }

    /// <summary>
    /// Windows API를 사용한 윈도우 매니저 구현
    /// </summary>
    public class WindowManager : IWindowManager
    {
        private IntPtr _winEventHook;
        private IntPtr _foregroundEventHook;
        private IntPtr _lastActiveWindow = IntPtr.Zero;
        private WindowsAPI.WinEventDelegate _winEventDelegate;
        private WindowsAPI.WinEventDelegate _foregroundEventDelegate;
        private bool _isDisposed;

        public event Action<IntPtr> WindowPositionChanged;
        public event Action<IntPtr> WindowActivated;
        public event Action<IntPtr> WindowDeactivated;

        public WindowManager()
        {
            _winEventDelegate = new WindowsAPI.WinEventDelegate(WinEventProc);
            _foregroundEventDelegate = new WindowsAPI.WinEventDelegate(ForegroundEventProc);
        }

        public bool SetupWindowEventHook(IntPtr windowHandle, uint processId)
        {
            try
            {
                // 기존 후킹 해제
                if (_winEventHook != IntPtr.Zero)
                {
                    WindowsAPI.UnhookWinEvent(_winEventHook);
                    _winEventHook = IntPtr.Zero;
                }

                // 윈도우 위치 변경 및 최소화 이벤트 후킹
                _winEventHook = WindowsAPI.SetWinEventHook(
                    WindowsAPI.EVENT_SYSTEM_MINIMIZESTART,
                    WindowsAPI.EVENT_OBJECT_LOCATIONCHANGE,
                    IntPtr.Zero,
                    _winEventDelegate,
                    processId,
                    0,
                    WindowsAPI.WINEVENT_OUTOFCONTEXT);

                Console.WriteLine($"윈도우 이벤트 후킹 설정: {(_winEventHook != IntPtr.Zero ? "성공" : "실패")}");
                return _winEventHook != IntPtr.Zero;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"윈도우 이벤트 후킹 설정 실패: {ex.Message}");
                return false;
            }
        }

        public bool SetupForegroundEventHook()
        {
            try
            {
                // 기존 후킹 해제
                if (_foregroundEventHook != IntPtr.Zero)
                {
                    WindowsAPI.UnhookWinEvent(_foregroundEventHook);
                    _foregroundEventHook = IntPtr.Zero;
                }

                // 전역 포어그라운드 윈도우 변경 이벤트 후킹
                _foregroundEventHook = WindowsAPI.SetWinEventHook(
                    WindowsAPI.EVENT_SYSTEM_FOREGROUND,
                    WindowsAPI.EVENT_SYSTEM_FOREGROUND,
                    IntPtr.Zero,
                    _foregroundEventDelegate,
                    0, // 모든 프로세스
                    0, // 모든 쓰레드
                    WindowsAPI.WINEVENT_OUTOFCONTEXT);

                Console.WriteLine($"포어그라운드 이벤트 후킹 설정: {(_foregroundEventHook != IntPtr.Zero ? "성공" : "실패")}");
                return _foregroundEventHook != IntPtr.Zero;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"포어그라운드 이벤트 후킹 설정 실패: {ex.Message}");
                return false;
            }
        }

        public void RemoveAllHooks()
        {
            try
            {
                if (_winEventHook != IntPtr.Zero)
                {
                    WindowsAPI.UnhookWinEvent(_winEventHook);
                    _winEventHook = IntPtr.Zero;
                }

                if (_foregroundEventHook != IntPtr.Zero)
                {
                    WindowsAPI.UnhookWinEvent(_foregroundEventHook);
                    _foregroundEventHook = IntPtr.Zero;
                }

                Console.WriteLine("모든 윈도우 이벤트 후킹 해제 완료");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"윈도우 이벤트 후킹 해제 실패: {ex.Message}");
            }
        }

        public WindowInfo GetWindowInfo(IntPtr windowHandle)
        {
            var windowInfo = new WindowInfo();

            if (windowHandle == IntPtr.Zero)
                return windowInfo;

            // 윈도우와 클라이언트 영역 정보 가져오기
            bool hasWindowRect = WindowsAPI.GetWindowRect(windowHandle, out windowInfo.WindowRect);
            bool hasClientRect = WindowsAPI.GetClientRect(windowHandle, out windowInfo.ClientRect);

            if (hasWindowRect && hasClientRect)
            {
                // 최소화/최대화 상태 확인
                windowInfo.IsMinimized = WindowsAPI.IsIconic(windowHandle);
                windowInfo.IsMaximized = WindowsAPI.IsZoomed(windowHandle);

                // 전체화면 여부 판단
                int windowWidth = windowInfo.WindowRect.Right - windowInfo.WindowRect.Left;
                int windowHeight = windowInfo.WindowRect.Bottom - windowInfo.WindowRect.Top;
                int screenWidth = System.Windows.Forms.Screen.PrimaryScreen.Bounds.Width;
                int screenHeight = System.Windows.Forms.Screen.PrimaryScreen.Bounds.Height;

                windowInfo.IsFullscreen = windowInfo.IsMaximized ||
                    (windowWidth >= screenWidth && windowHeight >= screenHeight) ||
                    (windowInfo.WindowRect.Left <= 0 && windowInfo.WindowRect.Top <= 0 && windowWidth >= screenWidth - 20);

                // 타이틀바 높이 계산
                windowInfo.TitleBarHeight = CalculateTitleBarHeight(windowInfo.WindowRect, windowInfo.ClientRect);
            }

            return windowInfo;
        }

        public bool IsWindowMinimized(IntPtr windowHandle)
        {
            return WindowsAPI.IsIconic(windowHandle);
        }

        public bool IsWindowMaximized(IntPtr windowHandle)
        {
            return WindowsAPI.IsZoomed(windowHandle);
        }

        public IntPtr GetForegroundWindow()
        {
            return WindowsAPI.GetForegroundWindow();
        }

        private void WinEventProc(IntPtr hWinEventHook, uint eventType, IntPtr hwnd,
            int idObject, int idChild, uint dwEventThread, uint dwmsEventTime)
        {
            if (hwnd == IntPtr.Zero)
                return;

            switch (eventType)
            {
                case WindowsAPI.EVENT_OBJECT_LOCATIONCHANGE:
                    WindowPositionChanged?.Invoke(hwnd);
                    break;

                case WindowsAPI.EVENT_SYSTEM_MINIMIZESTART:
                case WindowsAPI.EVENT_SYSTEM_MINIMIZEEND:
                    WindowPositionChanged?.Invoke(hwnd);
                    break;
            }
        }

        private void ForegroundEventProc(IntPtr hWinEventHook, uint eventType, IntPtr hwnd,
            int idObject, int idChild, uint dwEventThread, uint dwmsEventTime)
        {
            if (eventType == WindowsAPI.EVENT_SYSTEM_FOREGROUND && hwnd != IntPtr.Zero)
            {
                // 자기 자신(오버레이 프로그램) 무시
                try
                {
                    uint processId;
                    WindowsAPI.GetWindowThreadProcessId(hwnd, out processId);
                    string processName = System.Diagnostics.Process.GetProcessById((int)processId).ProcessName;

                    if (processName.Contains("StarcUp"))
                        return;
                }
                catch
                {
                    // 프로세스 정보를 가져올 수 없는 경우 무시
                    return;
                }

                WindowDeactivated?.Invoke(_lastActiveWindow);
                WindowActivated?.Invoke(hwnd);
                _lastActiveWindow = hwnd;
            }
        }

        private int CalculateTitleBarHeight(WindowsAPI.RECT windowRect, WindowsAPI.RECT clientRect)
        {
            int totalBorderHeight = (windowRect.Bottom - windowRect.Top) - (clientRect.Bottom - clientRect.Top);
            return Math.Max(20, (int)(totalBorderHeight * 0.8));
        }

        public void Dispose()
        {
            if (_isDisposed)
                return;

            RemoveAllHooks();
            _isDisposed = true;
        }
    }
}