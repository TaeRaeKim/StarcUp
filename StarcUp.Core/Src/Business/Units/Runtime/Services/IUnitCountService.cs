using StarcUp.Business.Units.Runtime.Models;
using StarcUp.Business.Units.Types;

namespace StarcUp.Business.Units.Runtime.Services
{
    /// <summary>
    /// 유닛 카운트 데이터를 관리하는 서비스 인터페이스
    /// - 1초에 10번 데이터 새로고침
    /// - 플레이어별 유닛 카운트 조회 제공
    /// - 완성된/생산중 유닛 구분 지원
    /// </summary>
    public interface IUnitCountService : IDisposable
    {
        /// <summary>
        /// 서비스 초기화 및 타이머 시작
        /// </summary>
        /// <returns>초기화 성공 여부</returns>
        bool Initialize();

        /// <summary>
        /// 서비스 중지
        /// </summary>
        void Stop();

        /// <summary>
        /// 수동으로 데이터 새로고침
        /// </summary>
        /// <returns>새로고침 성공 여부</returns>
        bool UpdateData();

        /// <summary>
        /// 특정 유닛의 카운트 조회
        /// </summary>
        /// <param name="unitType">유닛 타입</param>
        /// <param name="playerIndex">플레이어 인덱스 (0-7)</param>
        /// <param name="includeProduction">생산중인 유닛 포함 여부</param>
        /// <returns>유닛 개수</returns>
        int GetUnitCount(UnitType unitType, int playerIndex, bool includeProduction = false);

        /// <summary>
        /// 특정 플레이어의 모든 유닛 카운트 조회
        /// </summary>
        /// <param name="playerIndex">플레이어 인덱스 (0-7)</param>
        /// <param name="includeProduction">생산중인 유닛 포함 여부</param>
        /// <returns>UnitCount 객체 리스트</returns>
        List<UnitCount> GetAllUnitCounts(int playerIndex, bool includeProduction = false);

        /// <summary>
        /// 특정 플레이어의 특정 카테고리 유닛 카운트 조회
        /// </summary>
        /// <param name="playerIndex">플레이어 인덱스 (0-7)</param>
        /// <param name="category">유닛 카테고리</param>
        /// <param name="includeProduction">생산중인 유닛 포함 여부</param>
        /// <returns>UnitCount 객체 리스트</returns>
        List<UnitCount> GetUnitCountsByCategory(int playerIndex, UnitCategory category, bool includeProduction = false);

        /// <summary>
        /// 서비스 실행 상태
        /// </summary>
        bool IsRunning { get; }

        /// <summary>
        /// 마지막 업데이트 시간
        /// </summary>
        DateTime LastUpdateTime { get; }
    }
}