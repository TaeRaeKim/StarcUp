using StarcUp.Business.Units.Runtime.Adapters;
using StarcUp.Business.Units.Runtime.Models;
using StarcUp.Business.Units.Types;

namespace StarcUp.Business.Units.Runtime.Services
{
    public class UnitCountService : IUnitCountService
    {
        private readonly IUnitCountAdapter _unitCountAdapter;
        private readonly object _lockObject = new object();
        
        private bool _disposed;
        private bool _isInitialized;
        private DateTime _lastUpdateTime;

        // 캐시된 데이터 (최근 업데이트된 유닛 카운트들)
        private readonly Dictionary<byte, Dictionary<UnitType, UnitCount>> _cachedCompletedCounts = new();
        private readonly Dictionary<byte, Dictionary<UnitType, UnitCount>> _cachedProductionCounts = new();

        public bool IsRunning => _isInitialized;
        public DateTime LastUpdateTime => _lastUpdateTime;

        public UnitCountService(IUnitCountAdapter unitCountAdapter)
        {
            _unitCountAdapter = unitCountAdapter ?? throw new ArgumentNullException(nameof(unitCountAdapter));

            Console.WriteLine("[UnitCountService] 초기화 완료 - 수동 업데이트 방식");
        }

        public bool Initialize()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(UnitCountService));

            try
            {
                // 어댑터 초기화
                if (!_unitCountAdapter.InitializeBaseAddress())
                {
                    Console.WriteLine("[UnitCountService] ❌ UnitCountAdapter 초기화 실패");
                    return false;
                }

                // 초기 데이터 로드
                if (!RefreshData())
                {
                    Console.WriteLine("[UnitCountService] ❌ 초기 데이터 로드 실패");
                    return false;
                }

                _isInitialized = true;

                Console.WriteLine("[UnitCountService] ✅ 서비스 초기화 완료");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[UnitCountService] 초기화 중 오류: {ex.Message}");
                return false;
            }
        }

        public void Stop()
        {
            if (_disposed)
                return;

            _isInitialized = false;
            Console.WriteLine("[UnitCountService] 서비스 중지됨");
        }

        public bool RefreshData()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(UnitCountService));

            lock (_lockObject)
            {
                try
                {
                    // 어댑터에서 데이터 로드
                    if (!_unitCountAdapter.LoadAllUnitCounts())
                    {
                        return false;
                    }

                    // 모든 플레이어의 데이터 캐시 업데이트
                    for (byte playerId = 0; playerId < 8; playerId++)
                    {
                        UpdatePlayerCache(playerId);
                    }

                    _lastUpdateTime = DateTime.Now;
                    return true;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[UnitCountService] 데이터 새로고침 중 오류: {ex.Message}");
                    return false;
                }
            }
        }

        private void UpdatePlayerCache(byte playerId)
        {
            // 완성된 유닛 카운트 업데이트
            var completedCounts = _unitCountAdapter.GetAllUnitCountsToBuffer(playerId, false);
            _cachedCompletedCounts[playerId] = completedCounts.ToDictionary(uc => uc.UnitType, uc => uc);

            // 생산중 포함 유닛 카운트 업데이트
            var productionCounts = _unitCountAdapter.GetAllUnitCountsToBuffer(playerId, true);
            _cachedProductionCounts[playerId] = productionCounts.ToDictionary(uc => uc.UnitType, uc => uc);
        }

        public int GetUnitCount(UnitType unitType, int playerIndex, bool includeProduction = false)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(UnitCountService));

            if (playerIndex < 0 || playerIndex >= 8)
                throw new ArgumentOutOfRangeException(nameof(playerIndex), "플레이어 인덱스는 0-7 범위여야 합니다.");

            lock (_lockObject)
            {
                var cache = includeProduction ? _cachedProductionCounts : _cachedCompletedCounts;
                
                if (cache.TryGetValue((byte)playerIndex, out var playerCounts) &&
                    playerCounts.TryGetValue(unitType, out var unitCount))
                {
                    return includeProduction ? unitCount.TotalCount : unitCount.CompletedCount;
                }

                return 0;
            }
        }

        public List<UnitCount> GetAllUnitCounts(int playerIndex, bool includeProduction = false)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(UnitCountService));

            if (playerIndex < 0 || playerIndex >= 8)
                throw new ArgumentOutOfRangeException(nameof(playerIndex), "플레이어 인덱스는 0-7 범위여야 합니다.");

            lock (_lockObject)
            {
                var cache = includeProduction ? _cachedProductionCounts : _cachedCompletedCounts;
                
                if (cache.TryGetValue((byte)playerIndex, out var playerCounts))
                {
                    return playerCounts.Values.Where(uc => uc.IsValid).ToList();
                }

                return new List<UnitCount>();
            }
        }

        public List<UnitCount> GetUnitCountsByCategory(int playerIndex, UnitCategory category, bool includeProduction = false)
        {
            var allCounts = GetAllUnitCounts(playerIndex, includeProduction);
            return allCounts.Where(uc => uc.UnitType.GetCategory() == category).ToList();
        }


        public void Dispose()
        {
            if (_disposed)
                return;

            Stop();
            _unitCountAdapter?.Dispose();

            _cachedCompletedCounts.Clear();
            _cachedProductionCounts.Clear();

            _disposed = true;
            Console.WriteLine("[UnitCountService] 리소스 정리 완료");
        }
    }
}