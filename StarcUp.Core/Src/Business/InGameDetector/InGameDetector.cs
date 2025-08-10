using StarcUp.Business.GameDetection;
using StarcUp.Business.MemoryService;
using StarcUp.Business.Units.Runtime.Repositories;
using StarcUp.Common.Constants;
using StarcUp.Common.Events;
using StarcUp.Common.Logging;
using System;

namespace StarcUp.Business.InGameDetector
{
    public class InGameDetector : IInGameDetector
    {
        private readonly IMemoryService _memoryService;
        private readonly UnitOffsetRepository _offsetRepository;
        private bool _isMonitoring;
        private System.Timers.Timer _monitorTimer;

        private nint _cachedInGameAddress; // 캐싱된 InGame 상태 주소
        private bool _isAddressCached; // 주소 캐싱 여부

        public event EventHandler<InGameEventArgs> InGameStateChanged;

        public bool IsInGame { get; private set; }

        public InGameDetector(IMemoryService memoryService, UnitOffsetRepository offsetRepository)
        {
            _memoryService = memoryService ?? throw new ArgumentNullException(nameof(memoryService));
            _offsetRepository = offsetRepository ?? throw new ArgumentNullException(nameof(offsetRepository));
            _memoryService.ProcessConnect += OnProcessConnect;
            _memoryService.ProcessDisconnect += OnProcessDisconnect;
        }


        public void StartMonitoring(int processId)
        {
            if (_isMonitoring)
                return;

            LoggerHelper.Info($"InGame 상태 모니터링 시작: PID {processId}");

            try
            {
                if (GetIsAddress() == 0)
                {
                    LoggerHelper.Error("InGame 상태 주소를 캐싱할 수 없습니다. 모니터링을 중지합니다.");
                    return;
                }

                _monitorTimer = new System.Timers.Timer(500);
                _monitorTimer.Elapsed += CheckInGameState;
                _monitorTimer.Start();

                _isMonitoring = true;
                LoggerHelper.Info("InGame 상태 모니터링 활성화됨");
            }
            catch (Exception ex)
            {
                LoggerHelper.Error("InGame 모니터링 시작 실패", ex);
                StopMonitoring();
            }
        }

        public void StopMonitoring()
        {
            if (!_isMonitoring)
                return;

            LoggerHelper.Info("InGame 상태 모니터링 중지...");

            _monitorTimer?.Stop();
            _monitorTimer?.Dispose();
            _monitorTimer = null;

            IsInGame = false;

            _isMonitoring = false;
            LoggerHelper.Info("InGame 상태 모니터링 중지됨");
        }

        private void CheckInGameState(object sender, System.Timers.ElapsedEventArgs e)
        {
            try
            {
                if (!_memoryService.IsConnected)
                {
                    StopMonitoring();
                    return;
                }

                bool newInGameState = ReadInGameState();

                if (newInGameState != IsInGame)
                {
                    IsInGame = newInGameState;
                    LoggerHelper.Info($"InGame 상태 변경: {IsInGame}");

                    var eventArgs = new InGameEventArgs(IsInGame);
                    InGameStateChanged?.Invoke(this, eventArgs);
                }
            }
            catch (Exception ex)
            {
                LoggerHelper.Error("InGame 상태 확인 중 오류", ex);
            }
        }

        private bool ReadInGameState()
        {
            try
            {
                var finalAddress = GetIsAddress(); ;

                nint inGamePointer = _memoryService.ReadPointer(finalAddress);

                nint baseAddress = _memoryService.ReadPointer(_memoryService.GetThreadStackAddress(0) - _offsetRepository.GetBaseOffset());
                
                nint mapFileNameAddress = baseAddress + _offsetRepository.GetMapNameOffset();
                string mapFileName =_memoryService.IsValidAddress(mapFileNameAddress)
                    ? _memoryService.ReadString(baseAddress + _offsetRepository.GetMapNameOffset(), 256)
                    : string.Empty;

                bool isInGame = mapFileName.ToLower().EndsWith(".scx") || mapFileName.ToLower().EndsWith(".scm");

                return (inGamePointer != 0) && isInGame;
            }
            catch (Exception ex)
            {
                LoggerHelper.Error("InGame 상태 읽기 실패", ex);
                _isAddressCached = false; // 오류 발생 시 캐시 무효화
                return false;
            }
        }

        private nint GetIsAddress()
        {
            if (_isAddressCached && _cachedInGameAddress != 0)
            {
                return _cachedInGameAddress;
            }

            LoggerHelper.Debug("InGame 주소 계산 중...");

            nint BaseAddress = _memoryService.GetStarCraftModule()?.BaseAddress ?? 0;
            if (BaseAddress == 0)
            {
                LoggerHelper.Error("StarCraft 모듈을 찾을 수 없습니다.");
                return 0;
            }
            nint finalAddress = BaseAddress + 0x10A1878;

            _cachedInGameAddress = finalAddress;
            _isAddressCached = true;

            return finalAddress;
        }

        private void OnProcessConnect(object sender, ProcessEventArgs e)
        {
            LoggerHelper.Info($"프로세스 연결됨 (PID: {e.ProcessId})");
            StartMonitoring(e.ProcessId);
        }

        private void OnProcessDisconnect(object sender, ProcessEventArgs e)
        {
            LoggerHelper.Info("프로세스 연결 해제됨");
            _isAddressCached = false; // 캐시 무효화
            _cachedInGameAddress = 0;
            var eventArgs = new InGameEventArgs(false);
            InGameStateChanged?.Invoke(this, eventArgs);
            StopMonitoring();
        }

        public void Dispose()
        {
            StopMonitoring();
            _cachedInGameAddress = 0;
            _isAddressCached = false;

            if (_memoryService != null)
            {
                _memoryService.ProcessConnect -= OnProcessConnect;
                _memoryService.ProcessDisconnect -= OnProcessDisconnect;
            }
        }
    }
}