using System;
using System.Diagnostics;
using System.Threading;
using StarcUp.Common.Constants;
using StarcUp.Common.Events;
using StarcUp.Infrastructure.Windows;
using Timer = System.Threading.Timer;

namespace StarcUp.Business.GameDetection
{
    /// <summary>
    /// 하이브리드 스타크래프트 감지 서비스
    /// - 게임 시작: 2초 간격 폴링
    /// - 게임 종료: Process.Exited 이벤트
    /// - 게임 종료 후: 다시 폴링 모드로 전환
    /// </summary>
    public class GameDetector : IGameDetector
    {
        #region Private Fields

        private readonly IWindowManager _windowManager;
        private Timer _pollingTimer;
        private Process _currentGameProcess;
        private GameInfo _currentGame;
        private bool _isPollingMode;
        private bool _isDetecting;
        private bool _isDisposed;
        private readonly object _lockObject = new();

        #endregion

        #region Events

        public event EventHandler<GameEventArgs> HandleFound;
        public event EventHandler<GameEventArgs> HandleLost;
        public event EventHandler<GameEventArgs> HandleChanged;
        public event EventHandler<GameEventArgs> WindowMove;
        public event EventHandler<GameEventArgs> WindowFocusIn;
        public event EventHandler<GameEventArgs> WindowFocusOut;

        #endregion

        #region Properties

        public bool IsGameRunning => _currentGame != null;
        public GameInfo CurrentGame => _currentGame;
        public bool IsPollingMode => _isPollingMode;

        #endregion

        #region Constructor

        public GameDetector(IWindowManager windowManager)
        {
            _windowManager = windowManager ?? throw new ArgumentNullException(nameof(windowManager));

            // 윈도우 매니저 이벤트 구독
            _windowManager.WindowPositionChanged += OnWindowPositionChanged;
            _windowManager.WindowActivated += OnWindowActivated;
            _windowManager.WindowDeactivated += OnWindowDeactivated;
        }

        #endregion

        #region Public Methods

        public void StartDetection()
        {
            if (_isDetecting)
                return;

            Console.WriteLine("[HybridStarcraftDetector] 🚀 하이브리드 스타크래프트 감지 시작");
            Console.WriteLine("  📊 모드: 폴링 방식 (2초 간격)");
            Console.WriteLine("  🎯 대상: StarCraft, StarCraft_BW, StarCraft Remastered");
            Console.WriteLine("  ⚡ 전략: 시작=폴링, 종료=이벤트");

            try
            {
                // 윈도우 이벤트 후킹 설정
                _windowManager.SetupForegroundEventHook();

                // 폴링 모드로 시작
                StartPollingMode();

                _isDetecting = true;
                Console.WriteLine("[HybridStarcraftDetector] ✅ 감지 시스템 활성화됨");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[HybridStarcraftDetector] ❌ 감지 시작 실패: {ex.Message}");
            }
        }

        public void StopDetection()
        {
            if (!_isDetecting)
                return;

            Console.WriteLine("[HybridStarcraftDetector] 🛑 하이브리드 감지 중지...");

            try
            {
                // 폴링 중지
                StopPollingMode();

                // 게임 프로세스 이벤트 해제
                StopEventMode();

                // 윈도우 이벤트 후킹 해제
                _windowManager.RemoveAllHooks();

                lock (_lockObject)
                {
                    if (_currentGame != null)
                    {
                        var eventArgs = new GameEventArgs(_currentGame, GameConstants.EventTypes.HANDLE_LOST);
                        HandleLost?.Invoke(this, eventArgs);
                        _currentGame = null;
                    }
                }

                _isDetecting = false;
                Console.WriteLine("[HybridStarcraftDetector] ✅ 감지 시스템 중지됨");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[HybridStarcraftDetector] ❌ 감지 중지 실패: {ex.Message}");
            }
        }

        #endregion

        #region Polling Mode (게임 시작 감지)

        private void StartPollingMode()
        {
            if (_isPollingMode)
                return;

            Console.WriteLine("[HybridStarcraftDetector] 🔍 폴링 모드 시작 (2초 간격)");

            lock (_lockObject)
            {
                _pollingTimer = new Timer(PollingCallback, null, 0, 2000); // 즉시 시작, 2초 간격
                _isPollingMode = true;
            }

            Console.WriteLine("  - 방식: Process.GetProcessesByName()");
            Console.WriteLine("  - 간격: 2초");
            Console.WriteLine("  - CPU 영향: 최소");
        }

        private void StopPollingMode()
        {
            if (!_isPollingMode)
                return;

            Console.WriteLine("[HybridStarcraftDetector] ⏹️ 폴링 모드 중지");

            lock (_lockObject)
            {
                _pollingTimer?.Dispose();
                _pollingTimer = null;
                _isPollingMode = false;
            }
        }

        private void PollingCallback(object state)
        {
            if (_isDisposed || !_isPollingMode)
                return;

            try
            {
                CheckForStarcraftProcess();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[HybridStarcraftDetector] 폴링 중 오류: {ex.Message}");
            }
        }

        private void CheckForStarcraftProcess()
        {
            if (_currentGame != null)
                return; // 이미 게임이 실행 중이면 확인 안함

            Process foundProcess = null;
            string foundProcessName = null;

            // 스타크래프트 프로세스 검색
            foreach (string processName in GameConstants.STARCRAFT_PROCESS_NAMES)
            {
                try
                {
                    Process[] processes = Process.GetProcessesByName(processName);
                    if (processes.Length > 0)
                    {
                        foundProcess = processes[0];
                        foundProcessName = processName;

                        // 나머지 프로세스들 정리
                        for (int i = 1; i < processes.Length; i++)
                        {
                            processes[i].Dispose();
                        }
                        break;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[HybridStarcraftDetector] 프로세스 확인 실패 ({processName}): {ex.Message}");
                }
            }

            if (foundProcess != null)
            {
                OnGameProcessFound(foundProcess, foundProcessName);
                foundProcess.Dispose();
            }
        }

        private void OnGameProcessFound(Process process, string processName)
        {
            lock (_lockObject)
            {
                Console.WriteLine($"[HybridStarcraftDetector] 🎮 게임 프로세스 발견: {processName} (PID: {process.Id})");

                // 메인 윈도우 핸들 확인 (잠시 대기)
                if (process.MainWindowHandle == 0)
                {
                    Console.WriteLine("  ⏳ 메인 윈도우 생성 대기 중...");

                    // 별도 스레드에서 윈도우 핸들 대기
                    var waitProcess = Process.GetProcessById(process.Id);
                    System.Threading.Tasks.Task.Run(() => WaitForMainWindow(waitProcess, processName));
                    return;
                }

                // 즉시 게임 정보 생성
                CreateGameInfo(process, processName);
            }
        }

        private void WaitForMainWindow(Process process, string processName)
        {
            try
            {
                int attempts = 0;
                while (attempts < 20) // 최대 10초 대기
                {
                    Thread.Sleep(500);
                    process.Refresh();

                    if (process.HasExited)
                    {
                        Console.WriteLine("  ❌ 프로세스가 예상보다 빨리 종료됨");
                        return;
                    }

                    if (process.MainWindowHandle != 0)
                    {
                        Console.WriteLine("  ✅ 메인 윈도우 핸들 확인됨");

                        lock (_lockObject)
                        {
                            CreateGameInfo(process, processName);
                        }
                        return;
                    }

                    attempts++;
                }

                Console.WriteLine("  ⚠️ 메인 윈도우 핸들을 찾을 수 없음 (백그라운드 프로세스일 가능성)");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  ❌ 윈도우 대기 중 오류: {ex.Message}");
            }
            finally
            {
                process.Dispose();
            }
        }

        private void CreateGameInfo(Process process, string processName)
        {
            try
            {
                _currentGame = new GameInfo(process.Id, process.MainWindowHandle, processName)
                {
                    DetectedAt = DateTime.Now,
                    IsActive = false
                };

                // 게임 정보 업데이트
                UpdateGameInfo(_currentGame, process);

                // 윈도우 이벤트 설정
                SetupWindowEvents();

                // 폴링 모드 중지하고 이벤트 모드로 전환
                StopPollingMode();
                StartEventMode(process);

                // 게임 발견 이벤트 발생
                var eventArgs = new GameEventArgs(_currentGame, GameConstants.EventTypes.HANDLE_FOUND);
                HandleFound?.Invoke(this, eventArgs);

                Console.WriteLine($"[HybridStarcraftDetector] ✅ 게임 연결 완료: {_currentGame}");
                Console.WriteLine("[HybridStarcraftDetector] 🔄 이벤트 모드로 전환 완료");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[HybridStarcraftDetector] 게임 정보 생성 실패: {ex.Message}");
            }
        }

        #endregion

        #region Event Mode (게임 종료 감지)

        private void StartEventMode(Process process)
        {
            try
            {
                Console.WriteLine("[HybridStarcraftDetector] 🎯 이벤트 모드 시작");
                Console.WriteLine("  - 방식: Process.Exited 이벤트");
                Console.WriteLine("  - CPU 영향: 0% (이벤트 대기)");

                // 새로운 Process 객체로 이벤트 모니터링
                _currentGameProcess = Process.GetProcessById(process.Id);
                _currentGameProcess.EnableRaisingEvents = true;
                _currentGameProcess.Exited += OnGameProcessExited;

                Console.WriteLine($"  ✅ 프로세스 종료 감지 활성화: PID {process.Id}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[HybridStarcraftDetector] ❌ 이벤트 모드 시작 실패: {ex.Message}");

                // 이벤트 모드 실패 시 백업 폴링으로 전환
                Console.WriteLine("  🔄 백업: 1초 간격 폴링으로 대체");
                StartBackupPolling();
            }
        }

        private void StopEventMode()
        {
            try
            {
                if (_currentGameProcess != null)
                {
                    _currentGameProcess.Exited -= OnGameProcessExited;
                    _currentGameProcess.Dispose();
                    _currentGameProcess = null;

                    Console.WriteLine("[HybridStarcraftDetector] ⏹️ 이벤트 모드 중지");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[HybridStarcraftDetector] 이벤트 모드 중지 실패: {ex.Message}");
            }
        }

        private void OnGameProcessExited(object sender, EventArgs e)
        {
            try
            {
                Console.WriteLine("[HybridStarcraftDetector] 🛑 게임 종료 이벤트 감지");

                lock (_lockObject)
                {
                    if (_currentGame != null)
                    {
                        var eventArgs = new GameEventArgs(_currentGame, GameConstants.EventTypes.HANDLE_LOST);
                        HandleLost?.Invoke(this, eventArgs);

                        Console.WriteLine($"[HybridStarcraftDetector] ❌ 게임 연결 해제: {_currentGame}");
                        _currentGame = null;
                    }

                    // 이벤트 모드 정리
                    StopEventMode();

                    // 폴링 모드로 다시 전환
                    if (_isDetecting)
                    {
                        Console.WriteLine("[HybridStarcraftDetector] 🔄 폴링 모드로 재전환");
                        StartPollingMode();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[HybridStarcraftDetector] 게임 종료 처리 실패: {ex.Message}");
            }
        }

        private void StartBackupPolling()
        {
            // Process.Exited 이벤트가 실패한 경우의 백업 폴링 (1초 간격)
            Timer backupTimer = null;
            backupTimer = new Timer(_ =>
            {
                try
                {
                    if (_currentGameProcess?.HasExited == true)
                    {
                        OnGameProcessExited(null, EventArgs.Empty);
                        backupTimer?.Dispose();
                    }
                }
                catch
                {
                    backupTimer?.Dispose();
                }
            }, null, 1000, 1000);
        }

        #endregion

        #region Helper Methods

        private void UpdateGameInfo(GameInfo gameInfo, Process process)
        {
            if (gameInfo == null || process == null)
                return;

            try
            {
                var windowInfo = _windowManager.GetWindowInfo(gameInfo.WindowHandle);

                gameInfo.WindowBounds = new System.Drawing.Rectangle(
                    windowInfo.WindowRect.Left,
                    windowInfo.WindowRect.Top,
                    windowInfo.WindowRect.Width,
                    windowInfo.WindowRect.Height);

                gameInfo.IsFullscreen = windowInfo.IsFullscreen;
                gameInfo.IsMinimized = windowInfo.IsMinimized;
                gameInfo.IsActive = _windowManager.GetForegroundWindow() == gameInfo.WindowHandle;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[HybridStarcraftDetector] 게임 정보 업데이트 실패: {ex.Message}");
            }
        }

        private void SetupWindowEvents()
        {
            if (_currentGame != null)
            {
                _windowManager.SetupWindowEventHook(_currentGame.WindowHandle, (uint)_currentGame.ProcessId);
                Console.WriteLine($"  🪟 윈도우 이벤트 후킹 설정: Handle=0x{_currentGame.WindowHandle:X}");
            }
        }

        #endregion

        #region Window Event Handlers

        private void OnWindowPositionChanged(nint windowHandle)
        {
            if (_currentGame?.WindowHandle == windowHandle)
            {
                try
                {
                    var process = Process.GetProcessById(_currentGame.ProcessId);
                    UpdateGameInfo(_currentGame, process);

                    var eventArgs = new GameEventArgs(_currentGame, GameConstants.EventTypes.WINDOW_MOVE);
                    WindowMove?.Invoke(this, eventArgs);
                    process.Dispose();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[HybridStarcraftDetector] 윈도우 위치 변경 처리 실패: {ex.Message}");
                }
            }
        }

        private void OnWindowActivated(nint windowHandle)
        {
            if (_currentGame?.WindowHandle == windowHandle)
            {
                _currentGame.IsActive = true;
                var eventArgs = new GameEventArgs(_currentGame, GameConstants.EventTypes.WINDOW_FOCUSIN);
                WindowFocusIn?.Invoke(this, eventArgs);
                Console.WriteLine("[HybridStarcraftDetector] 🔥 게임 윈도우 활성화됨");
            }
        }

        private void OnWindowDeactivated(nint windowHandle)
        {
            if (_currentGame != null)
            {
                _currentGame.IsActive = false;
                var eventArgs = new GameEventArgs(_currentGame, GameConstants.EventTypes.WINDOW_FOCUSOUT);
                WindowFocusOut?.Invoke(this, eventArgs);
                Console.WriteLine("[HybridStarcraftDetector] 🔸 게임 윈도우 비활성화됨");
            }
        }

        #endregion

        #region Status and Diagnostics

        /// <summary>
        /// 현재 감지 시스템 상태 정보
        /// </summary>
        public string GetStatusInfo()
        {
            lock (_lockObject)
            {
                return $@"
[HybridStarcraftDetector] 상태 정보:
- 감지 활성화: {_isDetecting}
- 현재 모드: {(_isPollingMode ? "폴링 모드 (2초 간격)" : "이벤트 모드 (Process.Exited)")}
- 게임 실행 중: {IsGameRunning}
- 현재 게임: {CurrentGame?.ProcessName ?? "없음"}
- 게임 PID: {CurrentGame?.ProcessId ?? 0}
- 게임 활성화: {CurrentGame?.IsActive ?? false}
- 성능 영향: {(_isPollingMode ? "최소 (2초 폴링)" : "없음 (이벤트 대기)")}
";
            }
        }

        #endregion

        #region Dispose

        public void Dispose()
        {
            if (_isDisposed)
                return;

            StopDetection();
            _windowManager?.Dispose();
            _isDisposed = true;
        }

        #endregion
    }
}