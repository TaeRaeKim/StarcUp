using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using StarcUp.Business.InGameDetector;
using StarcUp.Business.Units.Runtime.Services;
using StarcUp.Business.MemoryService;
using StarcUp.Business.GameManager.Extensions;
using StarcUp.Common.Events;
using StarcUp.Business.Units.Types;

namespace StarcUp.Business.Game
{
    public class GameManager : IGameManager, IDisposable
    {
        private readonly IInGameDetector _inGameDetector;
        private readonly IUnitService _unitService;
        private readonly IMemoryService _memoryService;
        private readonly IUnitCountService _unitCountService;
        private readonly System.Timers.Timer _unitDataTimer;
        private readonly System.Timers.Timer _gameDataTimer;
        private readonly System.Timers.Timer _unitCountTimer;
        private readonly UnitUpdateManager _unitUpdateManager;
        private bool _isGameActive;
        private bool _disposed;
        public GameManager(IInGameDetector inGameDetector, IUnitService unitService, IMemoryService memoryService, IUnitCountService unitCountService)
        {
            _inGameDetector = inGameDetector ?? throw new ArgumentNullException(nameof(inGameDetector));
            _unitService = unitService ?? throw new ArgumentNullException(nameof(unitService));
            _memoryService = memoryService ?? throw new ArgumentNullException(nameof(memoryService));
            _unitCountService = unitCountService ?? throw new ArgumentNullException(nameof(unitCountService));
            
            _unitUpdateManager = new UnitUpdateManager(_unitService);
            _unitUpdateManager.UnitsUpdated += OnPlayerUnitsUpdated;
            
            _unitDataTimer = new System.Timers.Timer(100); // 1초에 10번 (100ms 간격)
            _unitDataTimer.Elapsed += OnUnitDataTimerElapsed;
            _unitDataTimer.AutoReset = true;
            
            _gameDataTimer = new System.Timers.Timer(100); // 0.5초마다 (500ms 간격)
            _gameDataTimer.Elapsed += OnGameDataTimerElapsed;
            _gameDataTimer.AutoReset = true;
            
            _inGameDetector.InGameStateChanged += OnInGameStateChanged;
        }

        public LocalGameData LocalGameData { get; private set; } = new LocalGameData();
        public Player[] Players { get; private set; } = new Player[8]
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

            // LocalPlayerIndex 초기 로드
            LoadLocalPlayerIndex();
            
            // 현재 플레이어의 유닛 업데이트 시작
            StartCurrentPlayerUnitUpdates();
            
            // 초기 유닛 데이터 로드
            LoadUnitsData();
            
            // 초기 게임 데이터 로드
            LoadGameData();
            
            // UnitCountService 초기화
            if (_unitCountService.Initialize())
            {
                Console.WriteLine("GameManager: UnitCountService 초기화 성공");
            }
            else
            {
                Console.WriteLine("GameManager: UnitCountService 초기화 실패");
            }
            
            // 타이머 시작
            _isGameActive = true;
            _unitDataTimer.Start();
            _gameDataTimer.Start();
            _unitCountTimer.Start();
            
            Console.WriteLine("GameManager: 게임 초기화 완료");
            Console.WriteLine("  - 유닛 데이터 로딩: 1초에 10번 (100ms 간격)");
            Console.WriteLine("  - 게임 데이터 로딩: 0.5초마다 (500ms 간격)");
            Console.WriteLine("  - 유닛 카운트 출력: 0.1초마다 (100ms 간격)");
            Console.WriteLine($"  - 현재 플레이어({LocalGameData.LocalPlayerIndex}) 유닛 업데이트: 100ms 주기");
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
            
            // UnitCountService 정지
            _unitCountService.Stop();
            Console.WriteLine("GameManager: UnitCountService 정지 완료");
            
            // 현재 플레이어의 유닛 업데이트 중지
            StopCurrentPlayerUnitUpdates();
            
            // 유닛 서비스 캐시 무효화
            _unitService.InvalidateAddressCache();
            
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
                LoadUnitsData();
            }
        }
        
        private void LoadUnitsData()
        {
            try
            {
                if (_unitService.RefreshUnits())
                {
                    var unitCount = _unitService.GetActiveUnitCount();
                    // 필요시 디버그 로그 (너무 빈번하므로 주석 처리)
                    //Console.WriteLine($"GameManager: 유닛 데이터 로드 완료 - 활성 유닛 수: {unitCount}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"GameManager: 유닛 데이터 로드 중 오류 발생 - {ex.Message}");
            }
        }
        
        private void OnGameDataTimerElapsed(object? sender, ElapsedEventArgs e)
        {
            if (_isGameActive && !_disposed)
            {
                LoadGameData();
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
                    LocalGameData = localGameData;
                    Console.WriteLine($"GameManager: LocalPlayerIndex 설정 완료 - {localPlayerIndex}");
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

        /// <summary>
        /// 현재 플레이어의 유닛 업데이트 시작
        /// </summary>
        private void StartCurrentPlayerUnitUpdates()
        {
            var currentPlayerIndex = (byte)LocalGameData.LocalPlayerIndex;
            _unitUpdateManager.StartPlayerUnitUpdates(currentPlayerIndex);
        }

        /// <summary>
        /// 현재 플레이어의 유닛 업데이트 중지
        /// </summary>
        private void StopCurrentPlayerUnitUpdates()
        {
            var currentPlayerIndex = (byte)LocalGameData.LocalPlayerIndex;
            _unitUpdateManager.StopPlayerUnitUpdates(currentPlayerIndex);
        }

        /// <summary>
        /// 플레이어 유닛 업데이트 이벤트 처리
        /// </summary>
        private void OnPlayerUnitsUpdated(object sender, UnitsUpdatedEventArgs e)
        {
            // if (e.PlayerId == LocalGameData.LocalPlayerIndex)
            // {
            //     // 2초마다 로그 출력 (너무 빈번한 로그 방지)
            //     var now = DateTime.Now;
            //     if ((now - _lastUnitLogTime).TotalSeconds >= 2.0)
            //     {
            //         _lastUnitLogTime = now;

            //         // 유닛 타입별로 그룹화하여 개수 계산
            //         var unitGroups = e.Units
            //             .GroupBy(unit => unit.UnitType)
            //             .OrderBy(group => group.Key.ToString())
            //             .ToDictionary(group => group.Key, group => group.Count());

            //         if (unitGroups.Count > 0)
            //         {
            //             Console.WriteLine($"[Player {e.PlayerId}] 보유 유닛 현황 (총 {e.Units.Count}개) - {now:HH:mm:ss}");
            //             foreach (var unitGroup in unitGroups)
            //             {
            //                 var unitType = unitGroup.Key;
            //                 var unitName = unitType.GetUnitName();
            //                 var count = unitGroup.Value;
            //                 Console.WriteLine($"  - {unitName}: {count}개");
            //             }
            //             Console.WriteLine(""); // 빈 줄 추가
            //         }
            //         else
            //         {
            //             Console.WriteLine($"[Player {e.PlayerId}] 보유 유닛 없음 - {now:HH:mm:ss}");
            //         }
            //     }
            // }
        }
        
        public void Dispose()
        {
            if (_disposed)
                return;
                
            GameExit();
            
            _unitDataTimer?.Stop();
            _unitDataTimer?.Dispose();
            
            _gameDataTimer?.Stop();
            _gameDataTimer?.Dispose();
            
            _unitCountTimer?.Stop();
            _unitCountTimer?.Dispose();
            
            // UnitUpdateManager 정리
            if (_unitUpdateManager != null)
            {
                _unitUpdateManager.UnitsUpdated -= OnPlayerUnitsUpdated;
                _unitUpdateManager.Dispose();
            }
            
            if (_inGameDetector != null)
            {
                _inGameDetector.InGameStateChanged -= OnInGameStateChanged;
            }
            
            _disposed = true;
            Console.WriteLine("GameManager: 리소스 정리 완료");
        }
    }
}
