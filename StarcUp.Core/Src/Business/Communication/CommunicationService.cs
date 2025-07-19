using System;
using System.Threading.Tasks;
using StarcUp.Infrastructure.Communication;
using StarcUp.Infrastructure.Windows;
using StarcUp.Common.Events;
using StarcUp.Business.GameDetection;
using StarcUp.Business.InGameDetector;

namespace StarcUp.Business.Communication
{
    /// <summary>
    /// StarcUp.UI와의 통신을 관리하는 비즈니스 서비스
    /// </summary>
    public class CommunicationService : ICommunicationService
    {
        private readonly INamedPipeClient _pipeClient;
        private readonly IGameDetector _gameDetector;
        private readonly IInGameDetector _inGameDetector;
        private readonly IWindowManager _windowManager;
        private bool _disposed = false;
        
        // 윈도우 위치 변경 관련 필드
        private WindowPositionData _lastWindowPosition;
        private DateTime _lastPositionSentTime = DateTime.MinValue;
        private const int ThrottleMs = 50; // 50ms 제한

        public bool IsConnected => _pipeClient.IsConnected;

        public event EventHandler<bool> ConnectionStateChanged;

        public CommunicationService(INamedPipeClient pipeClient, IGameDetector gameDetector, IInGameDetector inGameDetector, IWindowManager windowManager)
        {
            _pipeClient = pipeClient ?? throw new ArgumentNullException(nameof(pipeClient));
            _gameDetector = gameDetector ?? throw new ArgumentNullException(nameof(gameDetector));
            _inGameDetector = inGameDetector ?? throw new ArgumentNullException(nameof(inGameDetector));
            _windowManager = windowManager ?? throw new ArgumentNullException(nameof(windowManager));
        }

        public async Task<bool> StartAsync(string pipeName = "StarcUp.Dev")
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(CommunicationService));

            try
            {
                // 파이프 이름이 null이거나 기본값이라면 환경에 맞는 이름 사용
                if (string.IsNullOrEmpty(pipeName) || pipeName == "StarcUp.Dev")
                {
                    pipeName = NamedPipeConfig.GetPipeNameForCurrentEnvironment();
                }

                Console.WriteLine($"🚀 통신 서비스 시작: {pipeName}");

                // 연결 상태 변경 이벤트 구독
                _pipeClient.ConnectionStateChanged += OnConnectionStateChanged;

                // 명령 요청 이벤트 구독
                _pipeClient.CommandRequestReceived += OnCommandRequestReceived;

                // 게임 감지 이벤트 구독
                _gameDetector.HandleFound += OnGameDetected;
                _gameDetector.HandleLost += OnGameEnded;

                // 인 게임 감지 이벤트 구독
                _inGameDetector.InGameStateChanged += OnInGameStatus;

                // 윈도우 위치 변경 이벤트 구독
                _windowManager.WindowPositionChanged += OnWindowPositionChanged;
                _windowManager.WindowSizeChanged += OnWindowSizeChanged;


                // 자동 재연결 시작 (3초 간격, 최대 10회 재시도)
                _pipeClient.StartAutoReconnect(pipeName, 3000, 10);

                // 연결 시도 (재연결 간격에 맞춘 짧은 타임아웃)
                var connected = await _pipeClient.ConnectAsync(pipeName, 2000);
                
                if (connected)
                {
                    Console.WriteLine("✅ StarcUp.UI 서버에 연결 성공");
                }
                else
                {
                    Console.WriteLine("❌ StarcUp.UI 서버 연결 실패 - 자동 재연결 시작");
                    
                    // 첫 연결 실패 시 즉시 재연결 루프 시작
                    if (_pipeClient.IsReconnecting == false)
                    {
                        _pipeClient.TriggerReconnect();
                    }
                }

                return connected;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 통신 서비스 시작 실패: {ex.Message}");
                return false;
            }
        }

        public void Stop()
        {
            if (_disposed)
                return;

            try
            {
                Console.WriteLine("🛑 통신 서비스 중지");

                // 자동 재연결 중지
                _pipeClient.StopAutoReconnect();

                // 종료 알림 전송
                if (_pipeClient.IsConnected)
                {
                    _pipeClient.SendEvent("core-shutdown");
                }

                // 연결 해제
                _pipeClient.Disconnect();

                // 이벤트 구독 해제
                _pipeClient.ConnectionStateChanged -= OnConnectionStateChanged;
                _pipeClient.CommandRequestReceived -= OnCommandRequestReceived;
                _gameDetector.HandleFound -= OnGameDetected;
                _gameDetector.HandleLost -= OnGameEnded;
                _inGameDetector.InGameStateChanged -= OnInGameStatus;
                _windowManager.WindowPositionChanged -= OnWindowPositionChanged;
                _windowManager.WindowSizeChanged -= OnWindowSizeChanged;

                Console.WriteLine("✅ 통신 서비스 중지 완료");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 통신 서비스 중지 오류: {ex.Message}");
            }
        }

        private void OnCommandRequestReceived(object sender, CommandRequestEventArgs e)
        {            
            try
            {
                switch (e.Command)
                {
                    case NamedPipeProtocol.Commands.StartGameDetect:
                        _gameDetector.StartDetection();
                        break;
                        
                    case NamedPipeProtocol.Commands.StopGameDetect:
                        _gameDetector.StopDetection();
                        break;
                    default:
                        Console.WriteLine($"⚠️ 알 수 없는 명령: {e.Command}");
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 명령 처리 중 오류 발생: {e.Command} - {ex.Message}");
            }
        }
        private void OnInGameStatus(object sender, InGameEventArgs e){
            try
            {
                var eventData = new
                {
                    eventType = "in-game-status",
                    inGameInfo = new
                    {
                        isInGame = e.IsInGame,
                        timestamp = e.Timestamp.ToString("HH:mm:ss.fff")
                    }
                };
                _pipeClient.SendEvent(eventData.eventType, eventData);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 게임 중 이벤트 전송 실패: {ex.Message}");
            }
        }

        private void OnWindowPositionChanged(object sender, WindowChangedEventArgs e)
        {
            OnWindowChanged(e, "window-position-changed");
        }

        private void OnWindowSizeChanged(object sender, WindowChangedEventArgs e)
        {
            OnWindowChanged(e, "window-size-changed");
        }

        private void OnWindowChanged(WindowChangedEventArgs e, string eventType)
        {
            try
            {
                // Throttling 체크
                var now = DateTime.UtcNow;
                if ((now - _lastPositionSentTime).TotalMilliseconds < ThrottleMs)
                {
                    return;
                }

                // 현재 윈도우 정보를 WindowPositionData로 변환
                var positionData = e.CurrentWindowInfo?.ToPositionData();
                if (positionData == null)
                {
                    return;
                }

                // 중복 이벤트 필터링 (5픽셀 이하 변경은 무시)
                if (_lastWindowPosition != null && !positionData.HasPositionChanged(_lastWindowPosition, 5))
                {
                    return;
                }

                var eventData = new
                {
                    eventType = eventType,
                    windowPosition = new
                    {
                        x = positionData.X,
                        y = positionData.Y,
                        width = positionData.Width,
                        height = positionData.Height,
                        clientX = positionData.ClientX,
                        clientY = positionData.ClientY,
                        clientWidth = positionData.ClientWidth,
                        clientHeight = positionData.ClientHeight,
                        isMinimized = positionData.IsMinimized,
                        isMaximized = positionData.IsMaximized,
                        isVisible = positionData.IsVisible,
                        timestamp = positionData.Timestamp.ToString("HH:mm:ss.fff")
                    }
                };

                _pipeClient.SendEvent(eventData.eventType, eventData);
                
                _lastWindowPosition = positionData.Clone();
                _lastPositionSentTime = now;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 윈도우 위치 변경 이벤트 전송 실패: {ex.Message}");
            }
        }
        private void OnGameDetected(object sender, GameEventArgs e)
        {
            try
            {
                var eventData = new
                {
                    eventType = "game-detected",
                    gameInfo = new
                    {
                        processId = e.GameInfo.ProcessId,
                        processName = e.GameInfo.ProcessName,
                        detectedAt = e.GameInfo.DetectedAt
                    }
                };

                _pipeClient.SendEvent(eventData.eventType, eventData);

                // 스타크래프트 윈도우 모니터링 시작
                if (_windowManager.StartMonitoring(e.GameInfo.ProcessId))
                {
                    Console.WriteLine($"🪟 스타크래프트 윈도우 모니터링 시작 (PID: {e.GameInfo.ProcessId})");
                }
                else
                {
                    Console.WriteLine($"❌ 스타크래프트 윈도우 모니터링 시작 실패 (PID: {e.GameInfo.ProcessId})");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 게임 발견 이벤트 전송 실패: {ex.Message}");
            }
        }
        private void OnGameEnded(object sender, GameEventArgs e)
        {
            try
            {
                var eventData = new
                {
                    eventType = "game-ended",
                    gameInfo = new
                    {
                        processId = e.GameInfo.ProcessId,
                        processName = e.GameInfo.ProcessName,
                        detectedAt = e.GameInfo.DetectedAt,
                        lostAt = DateTime.Now
                    }
                };

                _pipeClient.SendEvent(eventData.eventType, eventData);

                // 스타크래프트 윈도우 모니터링 중지
                _windowManager.StopMonitoring();
                Console.WriteLine($"🪟 스타크래프트 윈도우 모니터링 중지 (PID: {e.GameInfo.ProcessId})");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 게임 종료 이벤트 전송 실패: {ex.Message}");
            }
        }
        private async void OnConnectionStateChanged(object sender, bool isConnected)
        {
            if (isConnected)
            {
                Console.WriteLine("✅ StarcUp.UI 서버에 연결되었습니다");
                try
                {
                    var pingResponse = await _pipeClient.SendCommandAsync("ping", new[] { "core-ready" });
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ 핑 전송 실패: {ex.Message}");
                }
            }
            else
            {
                Console.WriteLine("❌ StarcUp.UI 서버와의 연결이 끊어졌습니다");
            }

            ConnectionStateChanged?.Invoke(this, isConnected);
        }


        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            
            try
            {
                Stop();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 통신 서비스 종료 오류: {ex.Message}");
            }

            ConnectionStateChanged = null;
        }
    }
}