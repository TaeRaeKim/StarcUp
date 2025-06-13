using System;
using System.Diagnostics;
using System.Drawing;
using System.Timers;
using StarcUp.Business.Interfaces;
using StarcUp.Business.Models;
using StarcUp.Common.Constants;
using StarcUp.Common.Events;
using StarcUp.Infrastructure.Windows;

namespace StarcUp.Business.Services
{
    /// <summary>
    /// 게임 감지 서비스
    /// </summary>
    public class GameDetectionService : IGameDetectionService
    {
        private readonly IWindowManager _windowManager;
        private Timer _gameCheckTimer;
        private GameInfo _currentGame;
        private bool _isDetecting;
        private bool _isDisposed;

        public event EventHandler<GameEventArgs> GameFound;
        public event EventHandler<GameEventArgs> GameLost;
        public event EventHandler<GameEventArgs> WindowChanged;
        public event EventHandler<GameEventArgs> WindowActivated;
        public event EventHandler<GameEventArgs> WindowDeactivated;

        public bool IsGameRunning => _currentGame != null;
        public GameInfo CurrentGame => _currentGame;

        public GameDetectionService(IWindowManager windowManager)
        {
            _windowManager = windowManager ?? throw new ArgumentNullException(nameof(windowManager));

            // 윈도우 매니저 이벤트 구독
            _windowManager.WindowPositionChanged += OnWindowPositionChanged;
            _windowManager.WindowActivated += OnWindowActivated;
            _windowManager.WindowDeactivated += OnWindowDeactivated;
        }

        public void StartDetection()
        {
            if (_isDetecting)
                return;

            Console.WriteLine("게임 감지 시작...");

            // 게임 프로세스 체크 타이머 설정
            _gameCheckTimer = new Timer(GameConstants.GAME_CHECK_INTERVAL_MS);
            _gameCheckTimer.Elapsed += CheckForStarcraft;
            _gameCheckTimer.Start();

            // 포어그라운드 윈도우 이벤트 후킹 설정
            _windowManager.SetupForegroundEventHook();

            _isDetecting = true;
            Console.WriteLine("게임 감지 활성화됨");
        }

        public void StopDetection()
        {
            if (!_isDetecting)
                return;

            Console.WriteLine("게임 감지 중지...");

            _gameCheckTimer?.Stop();
            _gameCheckTimer?.Dispose();
            _gameCheckTimer = null;

            _windowManager.RemoveAllHooks();

            if (_currentGame != null)
            {
                var eventArgs = new GameEventArgs(_currentGame, GameConstants.EventTypes.GAME_LOST);
                GameLost?.Invoke(this, eventArgs);
                _currentGame = null;
            }

            _isDetecting = false;
            Console.WriteLine("게임 감지 중지됨");
        }

        private void CheckForStarcraft(object sender, ElapsedEventArgs e)
        {
            try
            {
                Process[] processes = null;

                // 여러 스타크래프트 프로세스 이름 확인
                foreach (string processName in GameConstants.STARCRAFT_PROCESS_NAMES)
                {
                    processes = Process.GetProcessesByName(processName);
                    if (processes.Length > 0)
                        break;
                }

                if (processes != null && processes.Length > 0)
                {
                    var process = processes[0];
                    if (process.MainWindowHandle != IntPtr.Zero)
                    {
                        // 새로운 게임 발견 또는 기존 게임 업데이트
                        var newGameInfo = CreateGameInfo(process);

                        if (_currentGame == null)
                        {
                            // 새 게임 발견
                            _currentGame = newGameInfo;
                            SetupWindowEvents();

                            var eventArgs = new GameEventArgs(_currentGame, GameConstants.EventTypes.GAME_FOUND);
                            GameFound?.Invoke(this, eventArgs);
                            Console.WriteLine($"게임 발견: {_currentGame}");
                        }
                        else if (_currentGame.WindowHandle != newGameInfo.WindowHandle)
                        {
                            // 윈도우 핸들이 변경됨
                            _currentGame = newGameInfo;
                            SetupWindowEvents();

                            var eventArgs = new GameEventArgs(_currentGame, GameConstants.EventTypes.WINDOW_CHANGED);
                            WindowChanged?.Invoke(this, eventArgs);
                            Console.WriteLine($"윈도우 변경: {_currentGame}");
                        }
                        else
                        {
                            // 기존 게임 정보 업데이트
                            UpdateGameInfo(_currentGame, process);
                        }
                    }
                }
                else
                {
                    // 게임이 종료됨
                    if (_currentGame != null)
                    {
                        var eventArgs = new GameEventArgs(_currentGame, GameConstants.EventTypes.GAME_LOST);
                        GameLost?.Invoke(this, eventArgs);
                        Console.WriteLine($"게임 종료: {_currentGame}");
                        _currentGame = null;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"게임 감지 중 오류: {ex.Message}");
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
            }
            catch (Exception ex)
            {
                Console.WriteLine($"게임 정보 업데이트 중 오류: {ex.Message}");
            }
        }

        private void SetupWindowEvents()
        {
            if (_currentGame != null)
            {
                _windowManager.SetupWindowEventHook(_currentGame.WindowHandle, (uint)_currentGame.ProcessId);
            }
        }

        private void OnWindowPositionChanged(IntPtr windowHandle)
        {
            if (_currentGame?.WindowHandle == windowHandle)
            {
                // 게임 윈도우 정보 업데이트
                try
                {
                    var process = Process.GetProcessById(_currentGame.ProcessId);
                    UpdateGameInfo(_currentGame, process);

                    var eventArgs = new GameEventArgs(_currentGame, "WindowPositionChanged");
                    WindowChanged?.Invoke(this, eventArgs);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"윈도우 위치 변경 처리 중 오류: {ex.Message}");
                }
            }
        }

        private void OnWindowActivated(IntPtr windowHandle)
        {
            if (_currentGame?.WindowHandle == windowHandle)
            {
                _currentGame.IsActive = true;
                var eventArgs = new GameEventArgs(_currentGame, GameConstants.EventTypes.WINDOW_ACTIVATED);
                WindowActivated?.Invoke(this, eventArgs);
                Console.WriteLine("게임 윈도우 활성화됨");
            }
        }

        private void OnWindowDeactivated(IntPtr windowHandle)
        {
            if (_currentGame != null)
            {
                _currentGame.IsActive = false;
                var eventArgs = new GameEventArgs(_currentGame, GameConstants.EventTypes.WINDOW_DEACTIVATED);
                WindowDeactivated?.Invoke(this, eventArgs);
                Console.WriteLine("게임 윈도우 비활성화됨");
            }
        }

        public void Dispose()
        {
            if (_isDisposed)
                return;

            StopDetection();
            _windowManager?.Dispose();
            _isDisposed = true;
        }
    }
}