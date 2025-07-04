using ElectronCgi.DotNet;
using System;
using System.Threading.Tasks;

namespace StarcUp.Core.Business.CgiService
{
    public class CgiHost : IDisposable
    {
        private readonly Connection _connection;
        private readonly ICgiService _cgiService;
        private bool _disposed = false;

        public CgiHost(ICgiService cgiService)
        {
            _cgiService = cgiService ?? throw new ArgumentNullException(nameof(cgiService));
            _connection = new ConnectionBuilder()
                .WithLogging()
                .Build();
        }

        public async Task StartAsync()
        {
            // GameDetector 시작 명령 등록
            _connection.On<string, bool>("start-game-detection", async (arg) =>
            {
                await _cgiService.StartGameDetectionAsync();
                return true;
            });

            // GameDetector 중지 명령 등록
            _connection.On<string, bool>("stop-game-detection", async (arg) =>
            {
                await _cgiService.StopGameDetectionAsync();
                return true;
            });

            // 게임 감지 상태 조회
            _connection.On<string, bool>("is-game-detection-running", async (arg) =>
            {
                return await _cgiService.IsGameDetectionRunningAsync();
            });

            // 게임 상태 조회
            _connection.On<string, string>("get-game-status", async (arg) =>
            {
                return await _cgiService.GetGameStatusAsync();
            });

            // 연결 시작
            await _connection.ListenAsync();
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _connection?.Dispose();
                _cgiService?.Dispose();
                _disposed = true;
            }
        }
    }
}