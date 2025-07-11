using System;
using System.Threading.Tasks;

namespace StarcUp.Core.Src.Infrastructure.Communication
{
    /// <summary>
    /// StarcUp.UI와의 통신을 관리하는 서비스
    /// </summary>
    public class CommunicationService : ICommunicationService
    {
        private readonly INamedPipeClient _pipeClient;
        private bool _disposed = false;

        public bool IsConnected => _pipeClient.IsConnected;

        public event EventHandler<bool> ConnectionStateChanged;

        public CommunicationService(INamedPipeClient pipeClient)
        {
            _pipeClient = pipeClient ?? throw new ArgumentNullException(nameof(pipeClient));
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

                // 메시지 수신 이벤트 구독
                _pipeClient.MessageReceived += OnMessageReceived;

                // 자동 재연결 시작 (3초 간격, 최대 10회 재시도)
                _pipeClient.StartAutoReconnect(pipeName, 3000, 10);

                // 연결 시도 (재연결 간격에 맞춘 짧은 타임아웃)
                var connected = await _pipeClient.ConnectAsync(pipeName, 2000);
                
                if (connected)
                {
                    Console.WriteLine("✅ StarcUp.UI 서버에 연결 성공");
                    // ping은 OnConnectionStateChanged에서 자동으로 전송됨
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

        public async Task StopAsync()
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
                    await _pipeClient.SendEventAsync("core-shutdown");
                }

                // 연결 해제
                _pipeClient.Disconnect();

                // 이벤트 구독 해제
                _pipeClient.ConnectionStateChanged -= OnConnectionStateChanged;
                _pipeClient.MessageReceived -= OnMessageReceived;

                Console.WriteLine("✅ 통신 서비스 중지 완료");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 통신 서비스 중지 오류: {ex.Message}");
            }
        }

        public async Task NotifyGameStatusAsync(object gameStatus)
        {
            if (_disposed || !_pipeClient.IsConnected)
                return;

            try
            {
                await _pipeClient.SendEventAsync("game-status-changed", gameStatus);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 게임 상태 알림 전송 실패: {ex.Message}");
            }
        }

        public async Task NotifyUnitDataAsync(object unitData)
        {
            if (_disposed || !_pipeClient.IsConnected)
                return;

            try
            {
                await _pipeClient.SendEventAsync("unit-data-changed", unitData);
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

        private void OnMessageReceived(object sender, string message)
        {
            Console.WriteLine($"📨 메시지 수신: {message}");
            // 필요에 따라 메시지 처리 로직 추가
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            
            try
            {
                StopAsync().Wait();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 통신 서비스 종료 오류: {ex.Message}");
            }

            ConnectionStateChanged = null;
        }
    }
}