using ElectronCgi.DotNet;
using StarcUp.Business.GameDetection;
using System;
using System.Threading.Tasks;

namespace StarcUp.Core.Business.CgiService
{
    public class CgiService : ICgiService, IDisposable
    {
        private readonly IGameDetector _gameDetector;
        private bool _isGameDetectionRunning = false;
        private bool _disposed = false;

        public CgiService(IGameDetector gameDetector)
        {
            _gameDetector = gameDetector ?? throw new ArgumentNullException(nameof(gameDetector));
        }

        public async Task StartGameDetectionAsync()
        {
            if (_isGameDetectionRunning)
                return;

            _gameDetector.StartDetection();
            _isGameDetectionRunning = true;
            
            await Task.CompletedTask;
        }

        public async Task StopGameDetectionAsync()
        {
            if (!_isGameDetectionRunning)
                return;

            _gameDetector.StopDetection();
            _isGameDetectionRunning = false;
            
            await Task.CompletedTask;
        }

        public async Task<bool> IsGameDetectionRunningAsync()
        {
            return await Task.FromResult(_isGameDetectionRunning);
        }

        public async Task<string> GetGameStatusAsync()
        {
            var status = new
            {
                IsRunning = _isGameDetectionRunning,
                IsGameDetected = _gameDetector.IsGameRunning,
                CurrentGame = _gameDetector.CurrentGame?.ProcessName ?? "None"
            };

            return await Task.FromResult(System.Text.Json.JsonSerializer.Serialize(status));
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _gameDetector?.Dispose();
                _disposed = true;
            }
        }
    }
}