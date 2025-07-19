using System;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace StarcUp.Infrastructure.Windows
{
    public class WindowManager : IWindowManager
    {
        private WindowInfo _currentWindowInfo;
        private WindowInfo _previousWindowInfo;
        private IntPtr _targetWindowHandle;
        private int _targetProcessId;
        private string _targetProcessName;
        private bool _isMonitoring;
        private bool _isDisposed;
        private readonly object _lockObject = new();

        private IntPtr _winEventLocationHook;
        private IntPtr _winEventSizeHook;
        private IntPtr _winEventMoveStartHook;
        private IntPtr _winEventMoveEndHook;
        private WindowsAPI.WinEventDelegate _winEventDelegate;

        // 메시지 루프 관련 필드
        private readonly IMessageLoopRunner _messageLoopRunner;

        public event EventHandler<WindowChangedEventArgs> WindowPositionChanged;
        public event EventHandler<WindowChangedEventArgs> WindowSizeChanged;
        public event EventHandler<WindowChangedEventArgs> WindowLost;

        public bool IsMonitoring => _isMonitoring;
        public bool IsWindowValid => _targetWindowHandle != IntPtr.Zero && WindowsAPI.IsWindow(_targetWindowHandle);
        public bool IsMessageLoopRunning => _messageLoopRunner.IsRunning;
        public uint MessageLoopThreadId => _messageLoopRunner.ThreadId;

        public WindowManager(IMessageLoopRunner messageLoopRunner = null)
        {
            _messageLoopRunner = messageLoopRunner ?? new MessageLoopRunner();
            _winEventDelegate = WinEventProc;
        }

        public bool StartMonitoring(int processId)
        {
            if (_isDisposed) return false;

            lock (_lockObject)
            {
                if (_isMonitoring)
                {
                    StopMonitoring();
                }

                _targetProcessId = processId;
                
                try
                {
                    var process = Process.GetProcessById(processId);
                    _targetProcessName = process.ProcessName;
                    
                    var windowHandle = FindMainWindow(processId);
                    if (windowHandle == IntPtr.Zero)
                    {
                        Console.WriteLine($"[WindowManager] 프로세스 ID {processId}의 메인 윈도우를 찾을 수 없습니다.");
                        return false;
                    }

                    _targetWindowHandle = windowHandle;
                    _currentWindowInfo = GetWindowInfo(windowHandle);
                    _previousWindowInfo = _currentWindowInfo?.Clone();

                    StartEventBasedMonitoring();
                    _isMonitoring = true;

                    Console.WriteLine($"[WindowManager] 윈도우 모니터링 시작: {_currentWindowInfo}");
                    return true;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[WindowManager] 모니터링 시작 실패 (PID: {processId}): {ex.Message}");
                    return false;
                }
            }
        }

        public bool StartMonitoring(string processName)
        {
            if (_isDisposed) return false;

            try
            {
                var processes = Process.GetProcessesByName(processName);
                if (processes.Length == 0)
                {
                    Console.WriteLine($"[WindowManager] 프로세스 '{processName}'을 찾을 수 없습니다.");
                    return false;
                }

                var success = StartMonitoring(processes[0].Id);
                
                for (int i = 1; i < processes.Length; i++)
                {
                    processes[i].Dispose();
                }
                processes[0].Dispose();

                return success;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[WindowManager] 모니터링 시작 실패 (프로세스명: {processName}): {ex.Message}");
                return false;
            }
        }

        public void StopMonitoring()
        {
            lock (_lockObject)
            {
                if (!_isMonitoring) return;

                StopEventBasedMonitoring();
                
                _targetWindowHandle = IntPtr.Zero;
                _targetProcessId = 0;
                _targetProcessName = null;
                _currentWindowInfo = null;
                _previousWindowInfo = null;
                _isMonitoring = false;

                Console.WriteLine("[WindowManager] 윈도우 모니터링 중지");
            }
        }

        public WindowInfo GetCurrentWindowInfo()
        {
            lock (_lockObject)
            {
                return _currentWindowInfo?.Clone();
            }
        }


        private void CheckWindowChanges()
        {
            lock (_lockObject)
            {
                if (!IsWindowValid)
                {
                    OnWindowLost();
                    return;
                }

                var newWindowInfo = GetWindowInfo(_targetWindowHandle);
                if (newWindowInfo == null)
                {
                    OnWindowLost();
                    return;
                }

                if (_currentWindowInfo != null && newWindowInfo.HasSizeOrPositionChanged(_currentWindowInfo))
                {
                    _previousWindowInfo = _currentWindowInfo.Clone();
                    _currentWindowInfo = newWindowInfo;

                    DetermineAndFireChangeEvent();
                }
                else if (_currentWindowInfo == null)
                {
                    _currentWindowInfo = newWindowInfo;
                }
            }
        }

        private void DetermineAndFireChangeEvent()
        {
            if (_previousWindowInfo == null || _currentWindowInfo == null)
                return;

            bool positionChanged = _currentWindowInfo.X != _previousWindowInfo.X || _currentWindowInfo.Y != _previousWindowInfo.Y;
            bool sizeChanged = _currentWindowInfo.Width != _previousWindowInfo.Width || _currentWindowInfo.Height != _previousWindowInfo.Height;

            WindowChangeType changeType;
            if (positionChanged && sizeChanged)
            {
                changeType = WindowChangeType.BothChanged;
                WindowPositionChanged?.Invoke(this, new WindowChangedEventArgs(_previousWindowInfo, _currentWindowInfo, changeType));
                WindowSizeChanged?.Invoke(this, new WindowChangedEventArgs(_previousWindowInfo, _currentWindowInfo, changeType));
            }
            else if (positionChanged)
            {
                changeType = WindowChangeType.PositionChanged;
                WindowPositionChanged?.Invoke(this, new WindowChangedEventArgs(_previousWindowInfo, _currentWindowInfo, changeType));
            }
            else if (sizeChanged)
            {
                changeType = WindowChangeType.SizeChanged;
                WindowSizeChanged?.Invoke(this, new WindowChangedEventArgs(_previousWindowInfo, _currentWindowInfo, changeType));
            }
        }

        private void OnWindowLost()
        {
            if (_currentWindowInfo != null)
            {
                Console.WriteLine("[WindowManager] 대상 윈도우가 손실되었습니다.");
                WindowLost?.Invoke(this, new WindowChangedEventArgs(_currentWindowInfo, null, WindowChangeType.WindowLost));
            }
            
            StopMonitoring();
        }

        private IntPtr FindMainWindow(int processId)
        {
            IntPtr mainWindow = IntPtr.Zero;
            
            WindowsAPI.EnumWindows((hWnd, lParam) =>
            {
                WindowsAPI.GetWindowThreadProcessId(hWnd, out uint windowProcessId);
                
                if (windowProcessId == processId && WindowsAPI.IsWindowVisible(hWnd))
                {
                    var windowInfo = GetWindowInfo(hWnd);
                    if (windowInfo != null && !string.IsNullOrEmpty(windowInfo.Title))
                    {
                        mainWindow = hWnd;
                        return false;
                    }
                }
                
                return true;
            }, IntPtr.Zero);

            return mainWindow;
        }

        private WindowInfo GetWindowInfo(IntPtr hWnd)
        {
            if (hWnd == IntPtr.Zero || !WindowsAPI.IsWindow(hWnd))
                return null;

            try
            {
                WindowsAPI.GetWindowThreadProcessId(hWnd, out uint processId);
                
                if (!WindowsAPI.GetWindowRect(hWnd, out WindowsAPI.RECT rect))
                    return null;

                int titleLength = WindowsAPI.GetWindowTextLength(hWnd);
                StringBuilder titleBuilder = new StringBuilder(titleLength + 1);
                WindowsAPI.GetWindowText(hWnd, titleBuilder, titleBuilder.Capacity);

                string processName = GetProcessName((int)processId);

                return new WindowInfo(
                    hWnd,
                    titleBuilder.ToString(),
                    processName,
                    (int)processId,
                    rect.Left,
                    rect.Top,
                    rect.Width,
                    rect.Height
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[WindowManager] 윈도우 정보 가져오기 실패: {ex.Message}");
                return null;
            }
        }

        private string GetProcessName(int processId)
        {
            try
            {
                using var process = Process.GetProcessById(processId);
                return process.ProcessName;
            }
            catch
            {
                return "Unknown";
            }
        }

        private async void StartEventBasedMonitoring()
        {
            if (_winEventDelegate == null) return;

            // 메시지 루프를 시작하고, 그 스레드에서 훅 설정
            await _messageLoopRunner.StartAsync(() => SetupEventHooks());
        }

        private void StopEventBasedMonitoring()
        {
            int unhookCount = 0;

            if (_winEventLocationHook != IntPtr.Zero)
            {
                WindowsAPI.UnhookWinEvent(_winEventLocationHook);
                _winEventLocationHook = IntPtr.Zero;
                unhookCount++;
            }

            if (_winEventSizeHook != IntPtr.Zero)
            {
                WindowsAPI.UnhookWinEvent(_winEventSizeHook);
                _winEventSizeHook = IntPtr.Zero;
                unhookCount++;
            }

            if (_winEventMoveStartHook != IntPtr.Zero)
            {
                WindowsAPI.UnhookWinEvent(_winEventMoveStartHook);
                _winEventMoveStartHook = IntPtr.Zero;
                unhookCount++;
            }

            if (_winEventMoveEndHook != IntPtr.Zero)
            {
                WindowsAPI.UnhookWinEvent(_winEventMoveEndHook);
                _winEventMoveEndHook = IntPtr.Zero;
                unhookCount++;
            }

            if (unhookCount > 0)
            {
                Console.WriteLine($"[WindowManager] 이벤트 기반 모니터링 중지 ({unhookCount}개 훅 해제)");
            }
            
            // 메시지 루프 중지
            _messageLoopRunner.Stop();
        }

        private void WinEventProc(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime)
        {
            if (_isDisposed || !_isMonitoring)
                return;

            if (hwnd != _targetWindowHandle)
                return;

            try
            {
                string eventName = GetEventTypeName(eventType);
                uint currentThreadId = WindowsAPI.GetCurrentThreadId();
                Console.WriteLine($"[WindowManager] ✅ 이벤트 콜백 실행: {eventName} (PID: {_targetProcessId}, Thread: {currentThreadId}, MessageLoopThread: {_messageLoopRunner.ThreadId})");

                if (eventType == WindowsAPI.EVENT_OBJECT_LOCATIONCHANGE ||
                    eventType == WindowsAPI.EVENT_OBJECT_SIZECHANGE ||
                    eventType == WindowsAPI.EVENT_SYSTEM_MOVESIZEEND)
                {
                    Console.WriteLine($"[WindowManager] 윈도우 변경 확인 중...");
                    CheckWindowChanges();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[WindowManager] 이벤트 처리 중 오류: {ex.Message}");
            }
        }

        private string GetEventTypeName(uint eventType)
        {
            return eventType switch
            {
                WindowsAPI.EVENT_OBJECT_LOCATIONCHANGE => "위치 변경",
                WindowsAPI.EVENT_OBJECT_SIZECHANGE => "크기 변경",
                WindowsAPI.EVENT_SYSTEM_MOVESIZESTART => "이동/크기 변경 시작",
                WindowsAPI.EVENT_SYSTEM_MOVESIZEEND => "이동/크기 변경 완료",
                _ => $"알 수 없는 이벤트 (0x{eventType:X})"
            };
        }

        private void SetupEventHooks()
        {
            uint hookFlags = WindowsAPI.WINEVENT_OUTOFCONTEXT | WindowsAPI.WINEVENT_SKIPOWNPROCESS;
            uint processId = (uint)_targetProcessId;
            uint currentThreadId = WindowsAPI.GetCurrentThreadId();

            Console.WriteLine($"[WindowManager] 훅 설정 시작 (스레드 ID: {currentThreadId}, 대상 PID: {processId})");

            _winEventLocationHook = WindowsAPI.SetWinEventHook(
                WindowsAPI.EVENT_OBJECT_LOCATIONCHANGE,
                WindowsAPI.EVENT_OBJECT_LOCATIONCHANGE,
                IntPtr.Zero,
                _winEventDelegate,
                processId,
                0,
                hookFlags);

            // _winEventSizeHook = WindowsAPI.SetWinEventHook(
            //     WindowsAPI.EVENT_OBJECT_SIZECHANGE,
            //     WindowsAPI.EVENT_OBJECT_SIZECHANGE,
            //     IntPtr.Zero,
            //     _winEventDelegate,
            //     processId,
            //     0,
            //     hookFlags);

            // _winEventMoveStartHook = WindowsAPI.SetWinEventHook(
            //     WindowsAPI.EVENT_SYSTEM_MOVESIZESTART,
            //     WindowsAPI.EVENT_SYSTEM_MOVESIZESTART,
            //     IntPtr.Zero,
            //     _winEventDelegate,
            //     processId,
            //     0,
            //     hookFlags);

            // _winEventMoveEndHook = WindowsAPI.SetWinEventHook(
            //     WindowsAPI.EVENT_SYSTEM_MOVESIZEEND,
            //     WindowsAPI.EVENT_SYSTEM_MOVESIZEEND,
            //     IntPtr.Zero,
            //     _winEventDelegate,
            //     processId,
            //     0,
            //     hookFlags);

            int hookCount = 0;
            if (_winEventLocationHook != IntPtr.Zero) hookCount++;
            if (_winEventSizeHook != IntPtr.Zero) hookCount++;
            if (_winEventMoveStartHook != IntPtr.Zero) hookCount++;
            if (_winEventMoveEndHook != IntPtr.Zero) hookCount++;

            Console.WriteLine($"[WindowManager] 이벤트 훅 설정 완료 ({hookCount}/4 훅 등록 성공)");
        }


        public void Dispose()
        {
            if (_isDisposed) return;

            StopMonitoring();
            _messageLoopRunner?.Dispose();
            _isDisposed = true;
        }
    }
}