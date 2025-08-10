using System;
using System.Diagnostics;
using System.Threading;
using StarcUp.Common.Constants;
using StarcUp.Common.Events;
using StarcUp.Common.Logging;
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

        #endregion

        #region Properties

        public bool IsGameRunning => _currentGame != null;
        public GameInfo CurrentGame => _currentGame;
        public bool IsPollingMode => _isPollingMode;

        #endregion

        #region Public Methods

        public void StartDetection()
        {
            if (_isDetecting)
                return;

            LoggerHelper.Info("스타크래프트 감지 시작");
            try
            {
                StartPollingMode();
                _isDetecting = true;
            }
            catch (Exception ex)
            {
                LoggerHelper.Error($"감지 시작 실패: {ex.Message}");
            }
        }

        public void StopDetection()
        {
            if (!_isDetecting)
                return;

            try
            {
                StopPollingMode();
                StopEventMode();

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
            }
            catch (Exception ex)
            {
                LoggerHelper.Error($"감지 중지 실패: {ex.Message}");
            }
        }

        #endregion


        private void StartPollingMode()
        {
            if (_isPollingMode)
                return;

            lock (_lockObject)
            {
                _pollingTimer = new Timer(PollingCallback, null, 0, 2000); // 즉시 시작, 2초 간격
                _isPollingMode = true;
            }
        }

        private void StopPollingMode()
        {
            if (!_isPollingMode)
                return;

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
                LoggerHelper.Error($"폴링 중 오류: {ex.Message}");
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
                    LoggerHelper.Error($"프로세스 확인 실패 ({processName}): {ex.Message}");
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
                LoggerHelper.Info($"게임 프로세스 발견: {processName} (PID: {process.Id})");   
                try
                {
                    _currentGame = new GameInfo(process.Id, processName)
                    {
                        DetectedAt = DateTime.Now,
                    };

                    StopPollingMode();
                    RegistProcessExitedEvent(process);

                    var eventArgs = new GameEventArgs(_currentGame, GameConstants.EventTypes.HANDLE_FOUND);
                    HandleFound?.Invoke(this, eventArgs);
                }
                catch (Exception ex)
                {
                    LoggerHelper.Error($"게임 정보 생성 실패: {ex.Message}");
                }
            }
        }
        private void RegistProcessExitedEvent(Process process)
        {
            try
            {
                _currentGameProcess = Process.GetProcessById(process.Id);
                _currentGameProcess.EnableRaisingEvents = true;
                _currentGameProcess.Exited += OnGameProcessExited;
            }
            catch (Exception ex)
            {
                LoggerHelper.Error($"RegistProcessExitedEvent 실패: {ex.Message}");
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
                }
            }
            catch (Exception ex)
            {
                LoggerHelper.Error($"이벤트 모드 중지 실패: {ex.Message}");
            }
        }

        private void OnGameProcessExited(object sender, EventArgs e)
        {
            try
            {
                lock (_lockObject)
                {
                    if (_currentGame != null)
                    {
                        var eventArgs = new GameEventArgs(_currentGame, GameConstants.EventTypes.HANDLE_LOST);
                        HandleLost?.Invoke(this, eventArgs);
                        _currentGame = null;
                    }

                    StopEventMode();

                    if (_isDetecting)
                    {
                        StartPollingMode();
                    }
                }
            }
            catch (Exception ex)
            {
                LoggerHelper.Error($"게임 종료 처리 실패: {ex.Message}");
            }
        }

        public void Dispose()
        {
            if (_isDisposed)
                return;

            StopDetection();
            _isDisposed = true;
        }
    }
}