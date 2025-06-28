using StarcUp.Business.Game;
using StarcUp.Business.Units.Runtime.Models;
using StarcUp.Business.Units.Runtime.Services;
using StarcUp.Business.Units.Types;

namespace StarcUp.Business.GameManager.Extensions
{
    /// <summary>
    /// Player 구조체에 대한 확장 메서드들
    /// </summary>
    public static class PlayerExtensions
    {
        private static IUnitCountService? _unitCountService;

        /// <summary>
        /// UnitCountService 인스턴스 설정 (DI 컨테이너에서 호출)
        /// </summary>
        public static void SetUnitCountService(IUnitCountService unitCountService)
        {
            _unitCountService = unitCountService;
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
                Console.WriteLine("[PlayerExtensions] UnitCountService가 설정되지 않음");
                return 0;
            }

            try
            {
                return _unitCountService.GetUnitCount(unitType, (byte)player.PlayerIndex, includeProduction);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[PlayerExtensions] GetUnitCount 오류: {ex.Message}");
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
                Console.WriteLine("[PlayerExtensions] UnitCountService가 설정되지 않음");
                return new List<UnitCount>();
            }

            try
            {
                return _unitCountService.GetAllUnitCounts((byte)player.PlayerIndex, includeProduction);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[PlayerExtensions] GetAllUnitCounts 오류: {ex.Message}");
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
                Console.WriteLine("[PlayerExtensions] UnitCountService가 설정되지 않음");
                return new List<UnitCount>();
            }

            try
            {
                return _unitCountService.GetUnitCountsByCategory((byte)player.PlayerIndex, category, includeProduction);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[PlayerExtensions] GetUnitCountsByCategory 오류: {ex.Message}");
                return new List<UnitCount>();
            }
        }

        /// <summary>
        /// 프로토스 건물 카운트 조회 (편의 메서드)
        /// </summary>
        public static int GetUnitCount(this Player player, UnitType unitType, IncludeProduction includeProduction)
        {
            return player.GetUnitCount(unitType, includeProduction == IncludeProduction.Yes);
        }
    }

    /// <summary>
    /// 생산중 유닛 포함 여부를 나타내는 열거형
    /// </summary>
    public enum IncludeProduction
    {
        No = 0,
        Yes = 1
    }

    /// <summary>
    /// 편의를 위한 상수 클래스 (사용자 요청 스타일)
    /// </summary>
    public static class ProductionConstants
    {
        public const IncludeProduction INCLUDE_PRODUCTION = IncludeProduction.Yes;
        public const IncludeProduction EXCLUDE_PRODUCTION = IncludeProduction.No;
    }
}