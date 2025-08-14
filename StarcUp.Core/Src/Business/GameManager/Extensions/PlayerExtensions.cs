using StarcUp.Business.Game;
using StarcUp.Business.Units.Runtime.Models;
using StarcUp.Business.Units.Runtime.Services;
using StarcUp.Business.Units.Types;
using StarcUp.Common.Logging;

namespace StarcUp.Business.GameManager.Extensions
{
    /// <summary>
    /// Player 구조체에 대한 확장 메서드들
    /// </summary>
    public static class PlayerExtensions
    {
        private static IUnitCountService? _unitCountService;
        private static IUnitService? _unitService;

        /// <summary>
        /// UnitCountService 인스턴스 설정 (DI 컨테이너에서 호출)
        /// </summary>
        public static void SetUnitCountService(IUnitCountService unitCountService)
        {
            _unitCountService = unitCountService;
        }

        /// <summary>
        /// UnitService 인스턴스 설정 (DI 컨테이너에서 호출)
        /// </summary>
        public static void SetUnitService(IUnitService unitService)
        {
            _unitService = unitService;
        }

        /// <summary>
        /// 특정 유닛의 카운트 조회
        /// </summary>
        /// <param name="player">플레이어</param>
        /// <param name="unitType">유닛 타입</param>
        /// <param name="includeProduction">생산중인 유닛 포함 여부</param>
        /// <returns>유닛 개수</returns>
        public static int GetUnitCount(this Player player, UnitType unitType, bool includeProduction = false)
        {
            if (_unitCountService == null)
            {
                LoggerHelper.Warning("[PlayerExtensions] UnitCountService가 설정되지 않음");
                return 0;
            }

            try
            {
                // UnitType.AllUnits인 경우 모든 유닛의 총합 반환
                if (unitType == UnitType.AllUnits)
                {
                    var allCounts = _unitCountService.GetAllUnitCounts(player.PlayerIndex, includeProduction);
                    return allCounts.Sum(uc => includeProduction ? uc.TotalCount : uc.CompletedCount);
                }

                return _unitCountService.GetUnitCount(unitType, player.PlayerIndex, includeProduction);
            }
            catch (Exception ex)
            {
                LoggerHelper.Warning($"[PlayerExtensions] GetUnitCount 오류: {ex.Message}");
                return 0;
            }
        }

        /// <summary>
        /// 모든 유닛 카운트 조회
        /// </summary>
        /// <param name="player">플레이어</param>
        /// <param name="includeProduction">생산중인 유닛 포함 여부</param>
        /// <returns>UnitCount 객체 리스트</returns>
        public static List<UnitCount> GetAllUnitCounts(this Player player, bool includeProduction = false)
        {
            if (_unitCountService == null)
            {
                LoggerHelper.Warning("[PlayerExtensions] UnitCountService가 설정되지 않음");
                return new List<UnitCount>();
            }

            try
            {
                return _unitCountService.GetAllUnitCounts(player.PlayerIndex, includeProduction);
            }
            catch (Exception ex)
            {
                LoggerHelper.Warning($"[PlayerExtensions] GetAllUnitCounts 오류: {ex.Message}");
                return new List<UnitCount>();
            }
        }

        /// <summary>
        /// 특정 카테고리의 유닛 카운트 조회
        /// </summary>
        /// <param name="player">플레이어</param>
        /// <param name="category">유닛 카테고리</param>
        /// <param name="includeProduction">생산중인 유닛 포함 여부</param>
        /// <returns>UnitCount 객체 리스트</returns>
        public static List<UnitCount> GetUnitCountsByCategory(this Player player, UnitCategory category, bool includeProduction = false)
        {
            if (_unitCountService == null)
            {
                LoggerHelper.Warning("[PlayerExtensions] UnitCountService가 설정되지 않음");
                return new List<UnitCount>();
            }

            try
            {
                return _unitCountService.GetUnitCountsByCategory(player.PlayerIndex, category, includeProduction);
            }
            catch (Exception ex)
            {
                LoggerHelper.Warning($"[PlayerExtensions] GetUnitCountsByCategory 오류: {ex.Message}");
                return new List<UnitCount>();
            }
        }

        /// <summary>
        /// 플레이어의 유닛 정보를 업데이트합니다
        /// </summary>
        /// <param name="player">플레이어</param>
        public static void UpdateUnits(this Player player)
        {

            if (_unitService == null)
            {
                LoggerHelper.Warning("[PlayerExtensions] UnitService가 설정되지 않음");
                return;
            }

            try
            {
                player.SetUnitCount(
                    _unitService.GetPlayerUnitsToBuffer(
                        player.PlayerIndex,
                        player.GetPlayerUnits(),
                        player.GetMaxUnits()
                        )
                    );
            }
            catch (Exception ex)
            {
                LoggerHelper.Warning($"[PlayerExtensions] Player {player.PlayerIndex} 유닛 업데이트 중 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// 실제로 활성화된 유닛들만 반환 (_unitCount개만 순회, 메모리 할당 없음)
        /// 기존 GetPlayerUnits()는 3400개 전체를 반환하지만, 이 메서드는 실제 유닛 수만큼만 반환
        /// </summary>
        /// <param name="player">플레이어</param>
        /// <returns>활성화된 유닛들의 ReadOnlySpan</returns>
        public static ReadOnlySpan<Unit> GetActiveUnits(this Player player)
        {
            return player.GetPlayerUnits().AsSpan(0, player.UnitCount);
        }

        /// <summary>
        /// 워커 유닛들만 효율적으로 조회 (yield return 방식, 지연 실행)
        /// </summary>
        /// <param name="player">플레이어</param>
        /// <returns>워커 유닛들의 IEnumerable</returns>
        public static IEnumerable<Unit> GetWorkers(this Player player)
        {
            var units = player.GetPlayerUnits();
            var unitCount = player.UnitCount;
            
            for (int i = 0; i < unitCount; i++)
            {
                if (units[i].IsWorker)
                    yield return units[i];
            }
        }

        /// <summary>
        /// 가스 건물들만 효율적으로 조회
        /// </summary>
        /// <param name="player">플레이어</param>
        /// <returns>가스 건물들의 IEnumerable</returns>
        public static IEnumerable<Unit> GetGasBuildings(this Player player)
        {
            var units = player.GetPlayerUnits();
            var unitCount = player.UnitCount;
            
            for (int i = 0; i < unitCount; i++)
            {
                if (units[i].IsGasBuilding)
                    yield return units[i];
            }
        }

        /// <summary>
        /// 모든 건물들을 효율적으로 조회
        /// </summary>
        /// <param name="player">플레이어</param>
        /// <returns>건물들의 IEnumerable</returns>
        public static IEnumerable<Unit> GetBuildings(this Player player)
        {
            var units = player.GetPlayerUnits();
            var unitCount = player.UnitCount;
            
            for (int i = 0; i < unitCount; i++)
            {
                if (units[i].IsBuilding)
                    yield return units[i];
            }
        }

    }
}