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
        private readonly IInGameDetector _inGameDetector;
        private readonly IUnitService _unitService;
        private readonly IMemoryService _memoryService;
        private readonly IUnitCountService _unitCountService;
        private readonly IWorkerManager _workerManager;
        private readonly IPopulationManager _populationManager;
        private readonly Timer _unitDataTimer;
        private readonly Timer _gameDataTimer;
        private readonly Timer _unitCountTimer;
        private readonly Timer _populationTimer;
        private bool _isGameActive;
        private bool _disposed;
        public GameManager(IInGameDetector inGameDetector, IUnitService unitService, IMemoryService memoryService, IUnitCountService unitCountService, IWorkerManager workerManager, IPopulationManager populationManager)
        {
            _inGameDetector = inGameDetector ?? throw new ArgumentNullException(nameof(inGameDetector));
            _unitService = unitService ?? throw new ArgumentNullException(nameof(unitService));
            _memoryService = memoryService ?? throw new ArgumentNullException(nameof(memoryService));
            _unitCountService = unitCountService ?? throw new ArgumentNullException(nameof(unitCountService));
            _workerManager = workerManager ?? throw new ArgumentNullException(nameof(workerManager));
            _populationManager = populationManager ?? throw new ArgumentNullException(nameof(populationManager));
                        
            _unitDataTimer = new Timer(100); // 1초에 10번 (100ms 간격)
            _unitDataTimer.Elapsed += OnUnitDataTimerElapsed;
            _unitDataTimer.AutoReset = true;
            
            _gameDataTimer = new Timer(100); // 0.5초마다 (500ms 간격)
            _gameDataTimer.Elapsed += OnGameDataTimerElapsed;
            _gameDataTimer.AutoReset = true;
            
            _unitCountTimer = new Timer(500); // 0.5초마다 (500ms 간격)
            _unitCountTimer.Elapsed += OnUnitCountTimerElapsed;
            _unitCountTimer.AutoReset = true;
            
            _populationTimer = new Timer(200); // 0.2초마다 (200ms 간격)
            _populationTimer.Elapsed += OnPopulationTimerElapsed;
            _populationTimer.AutoReset = true;
            
            _inGameDetector.InGameStateChanged += OnInGameStateChanged;
        }

        public LocalGameData LocalGameData { get; private set; } = new LocalGameData();
        public Player[] Players { get; } = new Player[8]
        {
            new Player { PlayerIndex = 0 },
            new Player { PlayerIndex = 1 },
            new Player { PlayerIndex = 2 },
            new Player { PlayerIndex = 3 },
            new Player { PlayerIndex = 4 },
            new Player { PlayerIndex = 5 },
            new Player { PlayerIndex = 6 },
            new Player { PlayerIndex = 7 }
        };
        public nint[] StartUnitAddressFromIndex { get; private set; } = Array.Empty<nint>();
        public nint StartUnitAddress { get; private set; } = 0;
        public void GameInit()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(GameManager));

            Console.WriteLine("GameManager: 게임 초기화 시작");

            // 객체 초기화
            Array.ForEach(Players, player => player.Init());

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

            // 타이머 시작
            _isGameActive = true;
            _unitDataTimer.Start();
            _gameDataTimer.Start();
            _unitCountTimer.Start();
            _populationTimer.Start();
            Console.WriteLine("GameManager: 게임 초기화 완료");            
        }
        public void GameExit()
        {
            if (_disposed)
                return;
                
            Console.WriteLine("GameManager: 게임 종료 시작");
            
            _isGameActive = false;
            _unitDataTimer.Stop();
            _gameDataTimer.Stop();
            _unitCountTimer.Stop();
            _populationTimer.Stop();
            
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
        
        private void OnUnitDataTimerElapsed(object? sender, ElapsedEventArgs e)
        {
            if (_isGameActive && !_disposed)
            {
                LoadUnitsData(); // 전체 Unit을 Buffer 방식으로 로드
                UpdateUnitData(); // Unit Count를 Buffer 방식으로 로드
            }
        }
        
        private void LoadUnitsData()
        {
            try
            {
                _unitService.RefreshUnits();
                var units = _unitService.GetPlayerUnits(LocalGameData.LocalPlayerIndex);
                
                // WorkerManager에 일꾼 데이터 업데이트
                var workers = units.Where(u => u.IsWorker);
                _workerManager.UpdateWorkerData(workers);
                
                // WorkerManager에 가스 건물 데이터 업데이트 (기존 타이머 활용)
                var gasBuildings = units.Where(u => u.IsGasBuilding).ToList();                
                _workerManager.UpdateGasBuildings(gasBuildings);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"GameManager: 유닛 데이터 로드 중 오류 발생 - {ex.Message}");
            }
        }

        private void UpdateUnitData()
        {
            try
            {
                // UnitCountService 데이터 새로고침
                if (!_unitCountService.RefreshData())
                {
                    Console.WriteLine("GameManager: UnitCountService 데이터 새로고침 실패");
                    return;
                }

                // Players[LocalGameData.LocalPlayerIndex].UpdateUnits();
                // Console.WriteLine($"GameManager: 플레이어 {LocalGameData.LocalPlayerIndex} 유닛 데이터 업데이트 완료");
                // Players[LocalGameData.LocalPlayerIndex].GetAllUnitCounts(true).ForEach(unitCount =>
                // {
                //     Console.WriteLine($"GameManager: 플레이어 {LocalGameData.LocalPlayerIndex} 유닛 타입 {unitCount.UnitType}의 개수: {unitCount.TotalCount}");
                // });
                // Console.WriteLine($"GameManager: {Players[LocalGameData.LocalPlayerIndex].GetUnitCount(UnitType.AllUnits, true)} 유닛이 업데이트되었습니다.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"GameManager: 유닛 데이터 업데이트 중 오류 발생 - {ex.Message}");
            }
        }
        
        private void OnGameDataTimerElapsed(object? sender, ElapsedEventArgs e)
        {
            if (_isGameActive && !_disposed)
            {
                LoadGameData();
            }
        }
        
        private void OnUnitCountTimerElapsed(object? sender, ElapsedEventArgs e)
        {
            if (_isGameActive && !_disposed)
            {
                // UnitCount 관련 로직이 필요하면 여기에 추가
            }
        }
        
        private void OnPopulationTimerElapsed(object? sender, ElapsedEventArgs e)
        {
            if (_isGameActive && !_disposed)
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
                
            _unitDataTimer?.Stop();
            _unitDataTimer?.Dispose();
            
            _gameDataTimer?.Stop();
            _gameDataTimer?.Dispose();
            
            _unitCountTimer?.Stop();
            _unitCountTimer?.Dispose();
            
            _populationTimer?.Stop();
            _populationTimer?.Dispose();
            
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
