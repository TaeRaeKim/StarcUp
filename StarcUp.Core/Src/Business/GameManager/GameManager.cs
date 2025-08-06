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
using Timer = System.Timers.Timer;
using StarcUp.Business.Units.Types;
using System.Security.Principal;

namespace StarcUp.Business.Game
{
    public class GameManager : IGameManager, IDisposable
    {
        private const int MAX_PLAYERS = 8;
        public GameManager(IInGameDetector inGameDetector, IUnitService unitService, IMemoryService memoryService, IUnitCountService unitCountService, IWorkerManager workerManager, IPopulationManager populationManager)
        {
            _inGameDetector = inGameDetector ?? throw new ArgumentNullException(nameof(inGameDetector));
            _unitService = unitService ?? throw new ArgumentNullException(nameof(unitService));
            _memoryService = memoryService ?? throw new ArgumentNullException(nameof(memoryService));
            _unitCountService = unitCountService ?? throw new ArgumentNullException(nameof(unitCountService));
            _workerManager = workerManager ?? throw new ArgumentNullException(nameof(workerManager));
            _populationManager = populationManager ?? throw new ArgumentNullException(nameof(populationManager));
            
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

            Console.WriteLine("GameManager: 게임 초기화 시작");

            for( int i = 0; i < Players.Length; i++)
            {
                Players[i].Init();
            }

            // 유닛 서비스 초기화
            if (_unitService.InitializeUnitArrayAddress())
            {
                Console.WriteLine("GameManager: 유닛 배열 주소 초기화 성공");
            }
            else
            {
                Console.WriteLine("GameManager: 유닛 배열 주소 초기화 실패");
            }

            LoadLocalPlayerIndex();
            LoadGameData();

            // UnitCountService 초기화 (재시도 로직 포함)
            if (!InitializeUnitCountServiceWithRetry())
            {
                Console.WriteLine("GameManager: UnitCountService 초기화 최종 실패 - GameManager를 종료합니다.");
                Dispose();
                return;
            }

            // WorkerManager 초기화
            _workerManager.Initialize(LocalGameData.LocalPlayerIndex);

            // PopulationManager 초기화
            _populationManager.Initialize(LocalGameData.LocalPlayerIndex, LocalGameData.LocalPlayerRace);
            
            // 테스트용 기본 설정 적용
            InitializeTestPopulationSettings();
            Console.WriteLine("GameManager: PopulationManager 초기화 완료");

            // 통합 타이머 시작
            _isGameActive = true;
            _updateTimer.Start();
            Console.WriteLine("GameManager: 게임 초기화 완료");            
        }
        public void GameExit()
        {
            if (_disposed)
                return;
                
            Console.WriteLine("GameManager: 게임 종료 시작");
            
            _isGameActive = false;
            _updateTimer.Stop();
            
            // UnitCountService 정지
            _unitCountService?.Stop();
            Console.WriteLine("GameManager: UnitCountService 정지 완료");
            
            // 유닛 서비스 캐시 무효화
            _unitService?.InvalidateAddressCache();
            
            Console.WriteLine("GameManager: 게임 종료 완료");
        }
        
        private void OnInGameStateChanged(object? sender, InGameEventArgs e)
        {
            if (e.IsInGame && !_isGameActive)
            {
                Console.WriteLine($"GameManager: 인게임 상태 감지됨 - {e.Timestamp}");
                GameInit();
            }
            else if (!e.IsInGame && _isGameActive)
            {
                Console.WriteLine($"GameManager: 게임 종료 상태 감지됨 - {e.Timestamp}");
                GameExit();
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
                Console.WriteLine($"GameManager: Update 중 오류 발생 - {ex.Message}");
            }
        }
        
        private void UpdateUnitService()
        {
            try
            {
                var localPlayer = Players[LocalGameData.LocalPlayerIndex];

                _unitService.UpdateUnits();
                localPlayer.UpdateUnits();
                
                var workers =  localPlayer.GetWorkers();
                _workerManager.UpdateWorkerData(workers);
                
                var gasBuildings =  localPlayer.GetGasBuildings().ToList();                
                _workerManager.UpdateGasBuildings(gasBuildings);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"GameManager: 유닛 데이터 로드 중 오류 발생 - {ex.Message}");
            }
        }

        private void UpdateUnitCountService()
        {
            try
            {
                // UnitCountService 데이터 새로고침
                if (!_unitCountService.UpdateData())
                {
                    Console.WriteLine("GameManager: UnitCountService 데이터 새로고침 실패");
                    return;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"GameManager: 유닛 데이터 업데이트 중 오류 발생 - {ex.Message}");
            }
        }
        
        /// <summary>
        /// PopulationManager 업데이트 래퍼 함수
        /// </summary>
        private void UpdatePopulationData()
        {
            try
            {
                _populationManager.UpdatePopulationData();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"GameManager: PopulationManager 업데이트 중 오류 발생 - {ex.Message}");
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
                    Console.WriteLine($"GameManager: LocalPlayerIndex 설정 완료 - {localPlayerIndex}, 종족: {localGameData.LocalPlayerRace}");
                }
                else
                {
                    Console.WriteLine($"GameManager: 잘못된 LocalPlayerIndex 값 - {localPlayerIndex}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"GameManager: LocalPlayerIndex 로드 중 오류 발생 - {ex.Message}");
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
                Console.WriteLine($"GameManager: GameTime 로드 중 오류 발생 - {ex.Message}");
            }
        }

        private bool InitializeUnitCountServiceWithRetry()
        {
            const int maxRetries = 10;
            const int delayMs = 200; // 0.2초

            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                Console.WriteLine($"GameManager: UnitCountService 초기화 시도 {attempt}/{maxRetries}");
                
                try
                {
                    if (_unitCountService.Initialize())
                    {
                        Console.WriteLine($"GameManager: UnitCountService 초기화 성공 (시도 {attempt})");
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"GameManager: UnitCountService 초기화 시도 {attempt} 중 예외 발생: {ex.Message}");
                }

                if (attempt < maxRetries)
                {
                    Console.WriteLine($"GameManager: {delayMs}ms 대기 후 재시도...");
                    Thread.Sleep(delayMs);
                }
            }

            Console.WriteLine($"GameManager: UnitCountService 초기화 {maxRetries}회 시도 모두 실패");
            return false;
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
            
            _disposed = true;
            Console.WriteLine("GameManager: 리소스 정리 완료");
        }

        /// <summary>
        /// 테스트용 PopulationManager 기본 설정 초기화
        /// 모드 A와 모드 B 모두 테스트할 수 있도록 설정
        /// </summary>
        private void InitializeTestPopulationSettings()
        {
            // 모드 A (고정값 기반) 테스트 설정
            var fixedModeSettings = new StarcUp.Business.Profile.Models.PopulationSettings
            {
                Mode = StarcUp.Business.Profile.Models.PopulationMode.Fixed,
                FixedSettings = new StarcUp.Business.Profile.Models.FixedModeSettings
                {
                    ThresholdValue = 2, // 여유분이 4 이하일 때 경고
                    TimeLimit = new StarcUp.Business.Profile.Models.TimeLimitSettings
                    {
                        Enabled = true,
                        Minutes = 0,
                        Seconds = 0 // 2분 후부터 경고 활성화
                    }
                }
            };

            // 필요시 모드 B로 전환할 수 있도록 모드 B 설정도 준비
            var buildingModeSettings = new StarcUp.Business.Profile.Models.PopulationSettings
            {
                Mode = StarcUp.Business.Profile.Models.PopulationMode.Building,
                BuildingSettings = new StarcUp.Business.Profile.Models.BuildingModeSettings
                {
                    Race = LocalGameData.LocalPlayerRace,
                    TrackedBuildings = GetDefaultTrackedBuildings(LocalGameData.LocalPlayerRace)
                }
            };
            
            _populationManager.InitializePopulationSettings(buildingModeSettings);
            Console.WriteLine("GameManager: 모드 A (고정값 기반) 테스트 설정 적용됨 - 기준값: 4, 시간제한: 2분");


            Console.WriteLine($"GameManager: 모드 B (건물 기반) 설정도 준비됨 - 종족: {LocalGameData.LocalPlayerRace}");
        }

        /// <summary>
        /// 종족별 기본 추적 건물 설정 생성
        /// </summary>
        private List<StarcUp.Business.Profile.Models.TrackedBuilding> GetDefaultTrackedBuildings(RaceType race)
        {
            var buildings = new List<StarcUp.Business.Profile.Models.TrackedBuilding>();

            switch (race)
            {
                case RaceType.Terran:
                    buildings.Add(new StarcUp.Business.Profile.Models.TrackedBuilding
                    {
                        BuildingType = UnitType.TerranBarracks,
                        Name = "배럭",
                        Multiplier = 1,
                        Enabled = true
                    });
                    buildings.Add(new StarcUp.Business.Profile.Models.TrackedBuilding
                    {
                        BuildingType = UnitType.TerranFactory,
                        Name = "팩토리",
                        Multiplier = 6,
                        Enabled = false
                    });
                    buildings.Add(new StarcUp.Business.Profile.Models.TrackedBuilding
                    {
                        BuildingType = UnitType.TerranStarport,
                        Name = "스타포트",
                        Multiplier = 4,
                        Enabled = false
                    });
                    break;

                case RaceType.Protoss:
                    buildings.Add(new StarcUp.Business.Profile.Models.TrackedBuilding
                    {
                        BuildingType = UnitType.ProtossGateway,
                        Name = "게이트웨이",
                        Multiplier = 2,
                        Enabled = true
                    });
                    buildings.Add(new StarcUp.Business.Profile.Models.TrackedBuilding
                    {
                        BuildingType = UnitType.ProtossRoboticsFacility,
                        Name = "로보틱스 퍼실리티",
                        Multiplier = 6,
                        Enabled = false
                    });
                    buildings.Add(new StarcUp.Business.Profile.Models.TrackedBuilding
                    {
                        BuildingType = UnitType.ProtossStargate,
                        Name = "스타게이트",
                        Multiplier = 4,
                        Enabled = false
                    });
                    break;

                case RaceType.Zerg:
                    buildings.Add(new StarcUp.Business.Profile.Models.TrackedBuilding
                    {
                        BuildingType = UnitType.ZergHatchery,
                        Name = "해처리",
                        Multiplier = 1,
                        Enabled = true
                    });
                    break;
            }

            return buildings;
        }
    }
}
