using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using StarcUp.Business.InGameDetector;
using StarcUp.Business.Units.Runtime.Services;
using StarcUp.Business.MemoryService;
using StarcUp.Common.Events;

namespace StarcUp.Business.Game
{
    public class GameManager : IGameManager, IDisposable
    {
        private readonly IInGameDetector _inGameDetector;
        private readonly IUnitService _unitService;
        private readonly IMemoryService _memoryService;
        private readonly System.Timers.Timer _unitDataTimer;
        private readonly System.Timers.Timer _gameDataTimer;
        private bool _isGameActive;
        private bool _disposed;

        public GameManager(IInGameDetector inGameDetector, IUnitService unitService, IMemoryService memoryService)
        {
            _inGameDetector = inGameDetector ?? throw new ArgumentNullException(nameof(inGameDetector));
            _unitService = unitService ?? throw new ArgumentNullException(nameof(unitService));
            _memoryService = memoryService ?? throw new ArgumentNullException(nameof(memoryService));
            
            _unitDataTimer = new System.Timers.Timer(100); // 1초에 10번 (100ms 간격)
            _unitDataTimer.Elapsed += OnUnitDataTimerElapsed;
            _unitDataTimer.AutoReset = true;
            
            _gameDataTimer = new System.Timers.Timer(500); // 0.5초마다 (500ms 간격)
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
            
            // 초기 유닛 데이터 로드
            LoadUnitsData();
            
            // 초기 게임 데이터 로드
            LoadGameData();
            
            // 타이머 시작
            _isGameActive = true;
            _unitDataTimer.Start();
            _gameDataTimer.Start();
            
            Console.WriteLine("GameManager: 게임 초기화 완료");
            Console.WriteLine("  - 유닛 데이터 로딩: 1초에 10번 (100ms 간격)");
            Console.WriteLine("  - 게임 데이터 로딩: 0.5초마다 (500ms 간격)");
        }
        public void GameExit()
        {
            if (_disposed)
                return;
                
            Console.WriteLine("GameManager: 게임 종료 시작");
            
            _isGameActive = false;
            _unitDataTimer.Stop();
            _gameDataTimer.Stop();
            
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
        
        public void Dispose()
        {
            if (_disposed)
                return;
                
            GameExit();
            
            _unitDataTimer?.Stop();
            _unitDataTimer?.Dispose();
            
            _gameDataTimer?.Stop();
            _gameDataTimer?.Dispose();
            
            if (_inGameDetector != null)
            {
                _inGameDetector.InGameStateChanged -= OnInGameStateChanged;
            }
            
            _disposed = true;
            Console.WriteLine("GameManager: 리소스 정리 완료");
        }
    }
}
