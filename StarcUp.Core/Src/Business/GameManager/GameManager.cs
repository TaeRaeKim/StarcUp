using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using StarcUp.Business.InGameDetector;
using StarcUp.Business.Units.Runtime.Services;
using StarcUp.Business.MemoryService;
using StarcUp.Business.GameManager.Extensions;
using StarcUp.Business.Profile;
using StarcUp.Common.Events;
using StarcUp.Core.Common.Events;
using Timer = System.Timers.Timer;
using StarcUp.Business.Units.Types;
using System.Security.Principal;
using StarcUp.Common.Logging;

namespace StarcUp.Business.Game
{
    public class GameManager : IGameManager, IDisposable
    {
        private const int MAX_PLAYERS = 8;
        
        public GameManager(IInGameDetector inGameDetector, IUnitService unitService, IMemoryService memoryService, IUnitCountService unitCountService, IWorkerManager workerManager, IPopulationManager populationManager, IUpgradeManager upgradeManager)
        {
            _inGameDetector = inGameDetector ?? throw new ArgumentNullException(nameof(inGameDetector));
            _unitService = unitService ?? throw new ArgumentNullException(nameof(unitService));
            _memoryService = memoryService ?? throw new ArgumentNullException(nameof(memoryService));
            _unitCountService = unitCountService ?? throw new ArgumentNullException(nameof(unitCountService));
            _workerManager = workerManager ?? throw new ArgumentNullException(nameof(workerManager));
            _populationManager = populationManager ?? throw new ArgumentNullException(nameof(populationManager));
            _upgradeManager = upgradeManager ?? throw new ArgumentNullException(nameof(upgradeManager));

            // Player 배열 초기화
            Players = new Player[MAX_PLAYERS];
            for (int i = 0; i < MAX_PLAYERS; i++)
            {
                Players[i] = new Player { PlayerIndex = i };
            }

            // 24fps = 약 41.67ms, 안전하게 42ms로 설정
            _updateTimer = new Timer(42); // 24fps (42ms 간격)
            _updateTimer.Elapsed += OnUpdateTimerElapsed;
            _updateTimer.AutoReset = true;

            _inGameDetector.InGameStateChanged += OnInGameStateChanged;
        }

        private readonly IInGameDetector _inGameDetector;
        private readonly IUnitService _unitService;
        private readonly IMemoryService _memoryService;
        private readonly IUnitCountService _unitCountService;
        private readonly IWorkerManager _workerManager;
        private readonly IPopulationManager _populationManager;
        private readonly IUpgradeManager _upgradeManager;
        private readonly Timer _updateTimer;
        private bool _isGameActive;
        private bool _disposed;
        public LocalGameData LocalGameData { get; private set; } = new LocalGameData();
        public Player[] Players { get; }

        public nint[] StartUnitAddressFromIndex { get; private set; } = Array.Empty<nint>();
        public nint StartUnitAddress { get; private set; } = 0;
        public void GameInit()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(GameManager));

            LoggerHelper.Info("게임 초기화 시작");

            for (int i = 0; i < Players.Length; i++)
            {
                Players[i].Init();
            }

            //추후 비동기로 개선 가능
            _unitService.InitializeUnitArrayAddress();
            _unitCountService.Initialize();

            LoadLocalPlayerIndex();
            LoadGameData();
            
            var localPlayer = Players[LocalGameData.LocalPlayerIndex];

            _unitService.UpdateUnits();
            localPlayer.UpdateUnits();

            _workerManager.Initialize(LocalGameData.LocalPlayerIndex);
            _populationManager.Initialize(LocalGameData.LocalPlayerIndex, LocalGameData.LocalPlayerRace);

            // UpgradeManager 초기화
            _upgradeManager.Initialize(LocalGameData.LocalPlayerIndex);

            LoggerHelper.Info("UpgradeManager 초기화 완료");

            // 통합 타이머 시작
            _isGameActive = true;
            _updateTimer.Start();
            LoggerHelper.Info("게임 초기화 완료");
        }
        public void GameExit()
        {
            if (_disposed)
                return;

            LoggerHelper.Info("게임 종료 시작");

            _isGameActive = false;
            _updateTimer.Stop();

            // UnitCountService 정지
            _unitCountService?.Stop();
            LoggerHelper.Info("UnitCountService 정지 완료");

            // 유닛 서비스 캐시 무효화
            _unitService?.InvalidateAddressCache();

            LoggerHelper.Info("게임 종료 완료");
        }

        private void OnInGameStateChanged(object? sender, InGameEventArgs e)
        {
            switch (e.State)
            {
                case InGameState.Started:
                    if (!_isGameActive)
                    {
                        LoggerHelper.Info($"인게임 상태 감지됨 - {e.Timestamp}");
                        GameInit();
                    }
                    break;

                case InGameState.Restarted:
                    LoggerHelper.Info($"게임 재시작 감지됨 - {e.Timestamp}");
                    // 게임 재시작 시: 기존 게임 종료 후 다시 초기화
                    GameExit();
                    Thread.Sleep(100); // 짧은 대기 후 재초기화
                    GameInit();
                    break;

                case InGameState.Stopped:
                    if (_isGameActive)
                    {
                        LoggerHelper.Info($"게임 종료 상태 감지됨 - {e.Timestamp}");
                        GameExit();
                    }
                    break;
            }
        }

        /// <summary>
        /// 통합 업데이트 함수 - 24fps (42ms 간격)로 모든 작업 실행
        /// </summary>
        private void OnUpdateTimerElapsed(object? sender, ElapsedEventArgs e)
        {
            if (!_isGameActive || _disposed) return;

            try
            {
                // 모든 작업을 24fps로 실행
                UpdateUnitService();
                UpdateUnitCountService();
                LoadGameData();
                UpdatePopulationData();
            }
            catch (Exception ex)
            {
                LoggerHelper.Error("Update 중 오류 발생", ex);
            }
        }

        private void UpdateUnitService()
        {
            try
            {
                var localPlayer = Players[LocalGameData.LocalPlayerIndex];

                _unitService.UpdateUnits();
                localPlayer.UpdateUnits();

                var workers = localPlayer.GetWorkers();
                _workerManager.UpdateWorkerData(workers);

                var gasBuildings = localPlayer.GetGasBuildings();
                _workerManager.UpdateGasBuildings(gasBuildings);


                var buildings = localPlayer.GetBuildings();
                _upgradeManager.Update(buildings);
            }
            catch (Exception ex)
            {
                LoggerHelper.Error("유닛 데이터 로드 중 오류 발생", ex);
            }
        }

        private void UpdateUnitCountService()
        {
            try
            {
                // UnitCountService 데이터 새로고침
                if (!_unitCountService.UpdateData())
                {
                    LoggerHelper.Warning("UnitCountService 데이터 새로고침 실패");
                    return;
                }
            }
            catch (Exception ex)
            {
                LoggerHelper.Error("유닛 데이터 업데이트 중 오류 발생", ex);
            }
        }

        /// <summary>
        /// PopulationManager 업데이트 래퍼 함수
        /// </summary>
        private void UpdatePopulationData()
        {
            try
            {
                // 게임 시간을 PopulationManager에 전달
                _populationManager.UpdateGameTime(TimeSpan.FromSeconds(LocalGameData.GameTime));
                _populationManager.UpdatePopulationData();
            }
            catch (Exception ex)
            {
                LoggerHelper.Error("PopulationManager 업데이트 중 오류 발생", ex);
            }
        }

        private void LoadLocalPlayerIndex()
        {
            try
            {
                int localPlayerIndex = _memoryService.ReadLocalPlayerIndex();
                if (localPlayerIndex >= 0 && localPlayerIndex < 8)
                {
                    var localGameData = LocalGameData;
                    localGameData.LocalPlayerIndex = localPlayerIndex;

                    // 종족 정보도 함께 로드
                    localGameData.LocalPlayerRace = _memoryService.ReadPlayerRace(localPlayerIndex);

                    LocalGameData = localGameData;
                    LoggerHelper.Info($"LocalPlayerIndex 설정 완료 - {localPlayerIndex}, 종족: {localGameData.LocalPlayerRace}");
                }
                else
                {
                    LoggerHelper.Warning($"잘못된 LocalPlayerIndex 값 - {localPlayerIndex}");
                }
            }
            catch (Exception ex)
            {
                LoggerHelper.Error("LocalPlayerIndex 로드 중 오류 발생", ex);
            }
        }


        private void LoadGameData()
        {
            try
            {
                int gameTime = _memoryService.ReadGameTime();
                if (gameTime >= 0)
                {
                    var localGameData = LocalGameData;
                    localGameData.GameTime = gameTime;
                    LocalGameData = localGameData;
                }
            }
            catch (Exception ex)
            {
                LoggerHelper.Error("GameTime 로드 중 오류 발생", ex);
            }
        }
        public void Dispose()
        {
            if (_disposed)
                return;

            _updateTimer?.Stop();
            _updateTimer?.Dispose();

            if (_inGameDetector != null)
            {
                _inGameDetector.InGameStateChanged -= OnInGameStateChanged;
            }

            if (_workerManager != null)
            {
                _workerManager.Dispose();
            }

            if (_populationManager != null)
            {
                _populationManager.Dispose();
            }

            if (_upgradeManager != null)
            {
                _upgradeManager.Dispose();
            }

            _disposed = true;
            LoggerHelper.Info("리소스 정리 완료");
        }
    }
}
