using System;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using StarcUp.Business.Interfaces;
using StarcUp.Business.Models;
using StarcUp.Common.Constants;
using StarcUp.Common.Events;
using StarcUp.Infrastructure.Windows;

namespace StarcUp.Business.Services
{
    /// <summary>
    /// 이벤트 기반 게임 감지 서비스
    /// </summary>
    public class GameDetectionService : IGameDetectionService
    {
        private readonly IWindowManager _windowManager;
        private readonly ProcessEventMonitor _processMonitor;
        private GameInfo _currentGame;
        private bool _isDetecting;
        private bool _isDisposed;

        public event EventHandler<GameEventArgs> HandleFound;
        public event EventHandler<GameEventArgs> HandleLost;
        public event EventHandler<GameEventArgs> HandleChanged;
        public event EventHandler<GameEventArgs> WindowMove;
        public event EventHandler<GameEventArgs> WindowFocusIn;
        public event EventHandler<GameEventArgs> WindowFocusOut;

        public bool IsGameRunning => _currentGame != null;
        public GameInfo CurrentGame => _currentGame;

        public GameDetectionService(IWindowManager windowManager)
        {
            _windowManager = windowManager ?? throw new ArgumentNullException(nameof(windowManager));

            // 프로세스 이벤트 모니터 초기화
            _processMonitor = new ProcessEventMonitor(GameConstants.STARCRAFT_PROCESS_NAMES);
            _processMonitor.ProcessStarted += OnProcessStarted;
            _processMonitor.ProcessStopped += OnProcessStopped;

            // 윈도우 매니저 이벤트 구독
            _windowManager.WindowPositionChanged += OnWindowPositionChanged;
            _windowManager.WindowActivated += OnWindowActivated;
            _windowManager.WindowDeactivated += OnWindowDeactivated;
        }

        public void StartDetection()
        {
            if (_isDetecting)
                return;

            Console.WriteLine("[GameDetectionService] 이벤트 기반 게임 감지 시작...");

            Task.Run(async () =>
            {
                try
                {
                    // 포어그라운드 윈도우 이벤트 후킹 설정
                    _windowManager.SetupForegroundEventHook();

                    // 프로세스 이벤트 모니터링 시작
                    bool success = await _processMonitor.StartMonitoringAsync();

                    if (success)
                    {
                        _isDetecting = true;
                        Console.WriteLine("[GameDetectionService] 이벤트 기반 게임 감지 활성화됨");
                        Console.WriteLine("  - 프로세스 생성/종료 이벤트 감지");
                        Console.WriteLine("  - 윈도우 포커스 이벤트 감지");
                        Console.WriteLine("  - 실시간 모니터링 시작됨");
                    }
                    else
                    {
                        Console.WriteLine("[GameDetectionService] ⚠️ 프로세스 이벤트 모니터링 시작 실패 - 폴백 모드로 전환할 수 있음");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[GameDetectionService] 게임 감지 시작 실패: {ex.Message}");
                }
            });
        }

        public void StopDetection()
        {
            if (!_isDetecting)
                return;

            Console.WriteLine("[GameDetectionService] 이벤트 기반 게임 감지 중지...");

            try
            {
                // 프로세스 이벤트 모니터링 중지
                _processMonitor.StopMonitoring();

                // 윈도우 이벤트 후킹 해제
                _windowManager.RemoveAllHooks();

                if (_currentGame != null)
                {
                    var eventArgs = new GameEventArgs(_currentGame, GameConstants.EventTypes.HANDLE_LOST);
                    HandleLost?.Invoke(this, eventArgs);
                    _currentGame = null;
                }

                _isDetecting = false;
                Console.WriteLine("[GameDetectionService] 이벤트 기반 게임 감지 중지됨");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GameDetectionService] 게임 감지 중지 실패: {ex.Message}");
            }
        }

        private void OnProcessStarted(object sender, ProcessEventArgs e)
        {
            try
            {
                Console.WriteLine($"[GameDetectionService] 🎮 스타크래프트 프로세스 감지: {e.ProcessInfo}");

                // 잠시 대기 후 윈도우 핸들 확인 (프로세스가 완전히 시작될 때까지)
                Task.Delay(1000).ContinueWith(_ =>
                {
                    try
                    {
                        Process process = Process.GetProcessById(e.ProcessInfo.ProcessId);

                        if (process != null && !process.HasExited)
                        {
                            // 메인 윈도우 핸들이 생성될 때까지 대기
                            int attempts = 0;
                            while (process.MainWindowHandle == IntPtr.Zero && attempts < 20)
                            {
                                System.Threading.Thread.Sleep(500);
                                process.Refresh();
                                attempts++;
                            }

                            if (process.MainWindowHandle != IntPtr.Zero)
                            {
                                var newGameInfo = CreateGameInfo(process);

                                if (_currentGame == null)
                                {
                                    // 새 게임 발견
                                    _currentGame = newGameInfo;
                                    SetupWindowEvents();

                                    var eventArgs = new GameEventArgs(_currentGame, GameConstants.EventTypes.HANDLE_FOUND);
                                    HandleFound?.Invoke(this, eventArgs);
                                    Console.WriteLine($"[GameDetectionService] ✅ 게임 연결 완료: {_currentGame}");
                                }
                                else
                                {
                                    // 기존 게임이 있는 경우 (멀티 인스턴스 등)
                                    Console.WriteLine($"[GameDetectionService] ⚠️ 기존 게임이 이미 실행 중: {_currentGame}");
                                }
                            }
                            else
                            {
                                Console.WriteLine($"[GameDetectionService] ⚠️ 메인 윈도우 핸들을 찾을 수 없음: {e.ProcessInfo.ProcessName}");
                            }
                        }

                        process?.Dispose();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[GameDetectionService] 프로세스 시작 처리 실패: {ex.Message}");
                    }
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GameDetectionService] 프로세스 시작 이벤트 처리 실패: {ex.Message}");
            }
        }

        private void OnProcessStopped(object sender, ProcessEventArgs e)
        {
            try
            {
                Console.WriteLine($"[GameDetectionService] 🛑 스타크래프트 프로세스 종료: {e.ProcessInfo}");

                if (_currentGame != null && _currentGame.ProcessId == e.ProcessInfo.ProcessId)
                {
                    var eventArgs = new GameEventArgs(_currentGame, GameConstants.EventTypes.HANDLE_LOST);
                    HandleLost?.Invoke(this, eventArgs);
                    Console.WriteLine($"[GameDetectionService] ❌ 게임 연결 해제: {_currentGame}");
                    _currentGame = null;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GameDetectionService] 프로세스 종료 이벤트 처리 실패: {ex.Message}");
            }
        }

        private GameInfo CreateGameInfo(Process process)
        {
            var gameInfo = new GameInfo(process.Id, process.MainWindowHandle, process.ProcessName);
            UpdateGameInfo(gameInfo, process);
            return gameInfo;
        }

        private void UpdateGameInfo(GameInfo gameInfo, Process process)
        {
            if (gameInfo == null || process == null)
                return;

            try
            {
                var windowInfo = _windowManager.GetWindowInfo(gameInfo.WindowHandle);

                gameInfo.WindowBounds = new Rectangle(
                    windowInfo.WindowRect.Left,
                    windowInfo.WindowRect.Top,
                    windowInfo.WindowRect.Width,
                    windowInfo.WindowRect.Height);

                gameInfo.IsFullscreen = windowInfo.IsFullscreen;
                gameInfo.IsMinimized = windowInfo.IsMinimized;
                gameInfo.IsActive = _windowManager.GetForegroundWindow() == gameInfo.WindowHandle;

                Console.WriteLine($"[GameDetectionService] 게임 정보 업데이트: {gameInfo}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GameDetectionService] 게임 정보 업데이트 중 오류: {ex.Message}");
            }
        }

        private void SetupWindowEvents()
        {
            if (_currentGame != null)
            {
                _windowManager.SetupWindowEventHook(_currentGame.WindowHandle, (uint)_currentGame.ProcessId);
                Console.WriteLine($"[GameDetectionService] 윈도우 이벤트 후킹 설정: Handle=0x{_currentGame.WindowHandle:X}");
            }
        }

        private void OnWindowPositionChanged(IntPtr windowHandle)
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
                    Console.WriteLine($"[GameDetectionService] 윈도우 위치 변경 처리 중 오류: {ex.Message}");
                }
            }
        }

        private void OnWindowActivated(IntPtr windowHandle)
        {
            if (_currentGame?.WindowHandle == windowHandle)
            {
                _currentGame.IsActive = true;
                var eventArgs = new GameEventArgs(_currentGame, GameConstants.EventTypes.WINDOW_FOCUSIN);
                WindowFocusIn?.Invoke(this, eventArgs);
                Console.WriteLine("[GameDetectionService] 🔥 게임 윈도우 활성화됨");
            }
        }

        private void OnWindowDeactivated(IntPtr windowHandle)
        {
            if (_currentGame != null)
            {
                _currentGame.IsActive = false;
                var eventArgs = new GameEventArgs(_currentGame, GameConstants.EventTypes.WINDOW_FOCUSOUT);
                WindowFocusOut?.Invoke(this, eventArgs);
                Console.WriteLine("[GameDetectionService] 🔸 게임 윈도우 비활성화됨");
            }
        }

        public void Dispose()
        {
            if (_isDisposed)
                return;

            StopDetection();
            _processMonitor?.Dispose();
            _windowManager?.Dispose();
            _isDisposed = true;
        }
    }
}