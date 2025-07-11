using System;
using System.Threading.Tasks;
using StarcUp.Infrastructure.Communication;
using StarcUp.Common.Events;
using StarcUp.Business.GameDetection;

namespace StarcUp.Business.Communication
{
    /// <summary>
    /// StarcUp.UI와의 통신을 관리하는 비즈니스 서비스
    /// </summary>
    public class CommunicationService : ICommunicationService
    {
        private readonly INamedPipeClient _pipeClient;
        private readonly IGameDetector _gameDetector;
        private bool _disposed = false;

        public bool IsConnected => _pipeClient.IsConnected;

        public event EventHandler<bool> ConnectionStateChanged;

        public CommunicationService(INamedPipeClient pipeClient, IGameDetector gameDetector)
        {
            _pipeClient = pipeClient ?? throw new ArgumentNullException(nameof(pipeClient));
            _gameDetector = gameDetector ?? throw new ArgumentNullException(nameof(gameDetector));
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
                _gameDetector.HandleFound += OnGameFound;
                _gameDetector.HandleLost += OnGameLost;

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
                _gameDetector.HandleFound -= OnGameFound;
                _gameDetector.HandleLost -= OnGameLost;

                Console.WriteLine("✅ 통신 서비스 중지 완료");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 통신 서비스 중지 오류: {ex.Message}");
            }
        }

        public void NotifyGameStatus(object gameStatus)
        {
            if (_disposed || !_pipeClient.IsConnected)
                return;

            try
            {
                _pipeClient.SendEvent("game-status-changed", gameStatus);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 게임 상태 알림 전송 실패: {ex.Message}");
            }
        }

        public void NotifyUnitData(object unitData)
        {
            if (_disposed || !_pipeClient.IsConnected)
                return;

            try
            {
                _pipeClient.SendEvent("unit-data-changed", unitData);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 유닛 데이터 알림 전송 실패: {ex.Message}");
            }
        }

        private async void OnConnectionStateChanged(object sender, bool isConnected)
        {
            if (isConnected)
            {
                Console.WriteLine("✅ StarcUp.UI 서버에 연결되었습니다");
                
                // 연결 상태 변경 시 ping 전송 (재연결이든 첫 연결이든 통일)
                try
                {
                    var pingResponse = await _pipeClient.SendCommandAsync("ping", new[] { "core-ready" });
                    if (pingResponse.Success)
                    {
                        Console.WriteLine($"📡 서버 연결 확인 완료");
                    }
                    else
                    {
                        Console.WriteLine($"📡 서버 연결 확인 실패: {pingResponse.Error}");
                    }
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

        private void OnCommandRequestReceived(object sender, CommandRequestEventArgs e)
        {
            Console.WriteLine($"🎯 명령 요청 처리: {e.Command} (RequestId: {e.RequestId})");
            
            try
            {
                switch (e.Command)
                {
                    case NamedPipeProtocol.Commands.StartGameDetect:
                        Console.WriteLine("🚀 게임 감지 시작 요청 처리");
                        _gameDetector.StartDetection();
                        Console.WriteLine("✅ 게임 감지 시작됨");
                        break;
                        
                    case NamedPipeProtocol.Commands.StopGameDetect:
                        Console.WriteLine("🛑 게임 감지 중지 요청 처리");
                        _gameDetector.StopDetection();
                        Console.WriteLine("✅ 게임 감지 중지됨");
                        break;
                        
                    case NamedPipeProtocol.Commands.GetGameStatus:
                        Console.WriteLine("📊 게임 상태 조회 요청 처리");
                        var gameStatus = _gameDetector.IsGameRunning ? "GAME_RUNNING" : "NOT_RUNNING";
                        Console.WriteLine($"📊 현재 게임 상태: {gameStatus}");
                        // 필요하면 상태를 UI로 알림 전송
                        NotifyGameStatus(new { status = gameStatus });
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

        private void OnGameFound(object sender, GameEventArgs e)
        {
            Console.WriteLine($"🎮 게임 발견 이벤트: {e.GameInfo.ProcessName} (PID: {e.GameInfo.ProcessId})");
            
            try
            {
                var eventData = new
                {
                    eventType = "game-found",
                    gameInfo = new
                    {
                        processId = e.GameInfo.ProcessId,
                        processName = e.GameInfo.ProcessName,
                        windowHandle = e.GameInfo.WindowHandle.ToString(),
                        detectedAt = e.GameInfo.DetectedAt,
                        isActive = e.GameInfo.IsActive,
                        isFullscreen = e.GameInfo.IsFullscreen,
                        isMinimized = e.GameInfo.IsMinimized,
                        windowBounds = new
                        {
                            x = e.GameInfo.WindowBounds.X,
                            y = e.GameInfo.WindowBounds.Y,
                            width = e.GameInfo.WindowBounds.Width,
                            height = e.GameInfo.WindowBounds.Height
                        }
                    }
                };

                _pipeClient.SendEvent("game-found", eventData);
                Console.WriteLine("📡 게임 발견 이벤트를 UI에 전송했습니다");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 게임 발견 이벤트 전송 실패: {ex.Message}");
            }
        }

        private void OnGameLost(object sender, GameEventArgs e)
        {
            Console.WriteLine($"🛑 게임 종료 이벤트: {e.GameInfo.ProcessName} (PID: {e.GameInfo.ProcessId})");
            
            try
            {
                var eventData = new
                {
                    eventType = "game-lost",
                    gameInfo = new
                    {
                        processId = e.GameInfo.ProcessId,
                        processName = e.GameInfo.ProcessName,
                        windowHandle = e.GameInfo.WindowHandle.ToString(),
                        detectedAt = e.GameInfo.DetectedAt,
                        lostAt = DateTime.Now
                    }
                };

                _pipeClient.SendEvent("game-lost", eventData);
                Console.WriteLine("📡 게임 종료 이벤트를 UI에 전송했습니다");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 게임 종료 이벤트 전송 실패: {ex.Message}");
            }
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