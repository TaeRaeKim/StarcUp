using System;
using System.Diagnostics;
using System.Windows.Forms;

namespace StarcUp
{
    public class GameDetector
    {
        private Timer gameCheckTimer;
        private IntPtr starcraftWindow;
        private IntPtr winEventHook;
        private IntPtr foregroundEventHook;
        private WindowsAPI.WinEventDelegate winEventDelegate;
        private WindowsAPI.WinEventDelegate foregroundEventDelegate;

        public event Action<IntPtr> GameFound;
        public event Action GameLost;
        public event Action<IntPtr> WindowPositionChanged;
        public event Action<IntPtr> WindowActivated;
        public event Action<IntPtr> WindowDeactivated;

        public IntPtr StarcraftWindow => starcraftWindow;

        public void StartDetection()
        {
            gameCheckTimer = new Timer();
            gameCheckTimer.Interval = 1000; // 1초마다 체크
            gameCheckTimer.Tick += CheckForStarcraft;
            gameCheckTimer.Start();

            // 윈도우 이벤트 후킹 설정
            winEventDelegate = new WindowsAPI.WinEventDelegate(WinEventProc);
            foregroundEventDelegate = new WindowsAPI.WinEventDelegate(ForegroundEventProc);

            // 전역 포어그라운드 이벤트 후킹 설정
            SetupForegroundEventHook();
        }

        public void StopDetection()
        {
            gameCheckTimer?.Stop();
            gameCheckTimer?.Dispose();

            // 윈도우 이벤트 후킹 해제
            if (winEventHook != IntPtr.Zero)
            {
                WindowsAPI.UnhookWinEvent(winEventHook);
                winEventHook = IntPtr.Zero;
            }

            // 포어그라운드 이벤트 후킹 해제
            if (foregroundEventHook != IntPtr.Zero)
            {
                WindowsAPI.UnhookWinEvent(foregroundEventHook);
                foregroundEventHook = IntPtr.Zero;
            }
        }

        private void SetupWindowEventHook()
        {
            // 기존 후킹이 있다면 해제
            if (winEventHook != IntPtr.Zero)
            {
                WindowsAPI.UnhookWinEvent(winEventHook);
            }

            // 스타크래프트 프로세스 ID 가져오기
            uint processId;
            WindowsAPI.GetWindowThreadProcessId(starcraftWindow, out processId);

            // 윈도우 위치 변경 및 최소화 이벤트 후킹
            winEventHook = WindowsAPI.SetWinEventHook(
                WindowsAPI.EVENT_SYSTEM_MINIMIZESTART,
                WindowsAPI.EVENT_OBJECT_LOCATIONCHANGE,
                IntPtr.Zero,
                winEventDelegate,
                processId,
                0,
                WindowsAPI.WINEVENT_OUTOFCONTEXT);
        }

        private void SetupForegroundEventHook()
        {
            // 전역 포어그라운드 윈도우 변경 이벤트 후킹
            foregroundEventHook = WindowsAPI.SetWinEventHook(
                WindowsAPI.EVENT_SYSTEM_FOREGROUND,
                WindowsAPI.EVENT_SYSTEM_FOREGROUND,
                IntPtr.Zero,
                foregroundEventDelegate,
                0, // 모든 프로세스
                0, // 모든 쓰레드
                WindowsAPI.WINEVENT_OUTOFCONTEXT);
        }

        private void ForegroundEventProc(IntPtr hWinEventHook, uint eventType, IntPtr hwnd,
            int idObject, int idChild, uint dwEventThread, uint dwmsEventTime)
        {
            if (eventType == WindowsAPI.EVENT_SYSTEM_FOREGROUND && starcraftWindow != IntPtr.Zero)
            {
                // 오버레이 자체는 무시 (OverlayForm에서 Handle을 전달받아야 함)
                // 임시로 프로세스 이름으로 필터링
                try
                {
                    uint processId;
                    WindowsAPI.GetWindowThreadProcessId(hwnd, out processId);
                    string processName = System.Diagnostics.Process.GetProcessById((int)processId).ProcessName;

                    // 자기 자신(오버레이 프로그램)은 무시
                    if (processName.Contains("StarcUp") || processName.Contains("YourProgramName"))
                    {
                        return;
                    }
                }
                catch
                {
                    // 프로세스 정보를 가져올 수 없는 경우 무시
                }

                if (hwnd == starcraftWindow)
                {
                    // 스타크래프트가 활성화됨
                    WindowActivated?.Invoke(starcraftWindow);
                }
                else
                {
                    // 다른 윈도우가 활성화됨
                    WindowDeactivated?.Invoke(hwnd);
                }
            }
        }

        private void WinEventProc(IntPtr hWinEventHook, uint eventType, IntPtr hwnd,
            int idObject, int idChild, uint dwEventThread, uint dwmsEventTime)
        {
            // 스타크래프트 윈도우의 이벤트만 처리
            if (hwnd == starcraftWindow)
            {
                if (eventType == WindowsAPI.EVENT_OBJECT_LOCATIONCHANGE)
                {
                    // 위치 변경 시
                    WindowPositionChanged?.Invoke(starcraftWindow);
                }
                else if (eventType == WindowsAPI.EVENT_SYSTEM_MINIMIZESTART)
                {
                    // 최소화 시작 시 오버레이 숨기기
                    GameLost?.Invoke();
                }
                else if (eventType == WindowsAPI.EVENT_SYSTEM_MINIMIZEEND)
                {
                    // 최소화 해제 시 오버레이 다시 표시
                    GameFound?.Invoke(starcraftWindow);
                }
            }
        }

        private void CheckForStarcraft(object sender, EventArgs e)
        {
            // 스타크래프트 프로세스 찾기
            Process[] processes = Process.GetProcessesByName("StarCraft");
            if (processes.Length == 0)
            {
                // 스타크래프트 브루드워도 체크
                processes = Process.GetProcessesByName("StarCraft_BW");
            }
            if (processes.Length == 0)
            {
                // 리마스터 버전도 체크
                processes = Process.GetProcessesByName("StarCraft Remastered");
            }

            if (processes.Length > 0)
            {
                IntPtr newWindow = processes[0].MainWindowHandle;
                if (newWindow != IntPtr.Zero)
                {
                    if (starcraftWindow == IntPtr.Zero)
                    {
                        starcraftWindow = newWindow;
                        SetupWindowEventHook(); // 윈도우 이벤트 후킹 설정
                        GameFound?.Invoke(starcraftWindow);
                        Console.WriteLine("스타크래프트 감지됨 - 오버레이 활성화");
                    }
                    else
                    {
                        starcraftWindow = newWindow;
                    }
                }
            }
            else
            {
                if (starcraftWindow != IntPtr.Zero)
                {
                    // 윈도우 이벤트 후킹 해제
                    if (winEventHook != IntPtr.Zero)
                    {
                        WindowsAPI.UnhookWinEvent(winEventHook);
                        winEventHook = IntPtr.Zero;
                    }

                    starcraftWindow = IntPtr.Zero;
                    GameLost?.Invoke();
                    Console.WriteLine("스타크래프트 종료됨 - 오버레이 비활성화");
                }
            }
        }
    }
}