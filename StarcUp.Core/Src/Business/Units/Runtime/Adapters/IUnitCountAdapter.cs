using StarcUp.Business.Units.Runtime.Models;
using StarcUp.Business.Units.Types;

namespace StarcUp.Business.Units.Runtime.Adapters
{
    /// <summary>
    /// 유닛 카운트 데이터를 메모리에서 읽어오는 어댑터 인터페이스
    /// - 한번의 메모리 읽기로 모든 종족의 유닛/건물 카운트를 로드
    /// - 완성된 유닛과 생산중인 유닛을 구분하여 제공
    /// - 플레이어별 유닛 카운트 조회 지원
    /// </summary>
    public interface IUnitCountAdapter : IDisposable
    {
        /// <summary>
        /// 베이스 주소 초기화 (THREADSTACK0 기반)
        /// </summary>
        /// <returns>초기화 성공 여부</returns>
        bool InitializeBaseAddress();

        /// <summary>
        /// 모든 유닛 카운트 데이터를 메모리에서 로드
        /// </summary>
        /// <returns>로드 성공 여부</returns>
        bool LoadAllUnitCounts();

        /// <summary>
        /// 특정 유닛의 카운트 조회
        /// </summary>
        /// <param name="unitType">유닛 타입</param>
        /// <param name="playerIndex">플레이어 인덱스 (0-7)</param>
        /// <param name="includeProduction">생산중인 유닛 포함 여부</param>
        /// <returns>유닛 개수</returns>
        int GetUnitCount(UnitType unitType, int playerIndex, bool includeProduction = false);

        /// <summary>
        /// 특정 플레이어의 모든 유닛 카운트 조회 (Dictionary 형태)
        /// </summary>
        /// <param name="playerIndex">플레이어 인덱스 (0-7)</param>
        /// <param name="includeProduction">생산중인 유닛 포함 여부</param>
        /// <returns>유닛 타입별 카운트</returns>
        Dictionary<UnitType, int> GetAllUnitCounts(int playerIndex, bool includeProduction = false);

        /// <summary>
        /// 특정 플레이어의 모든 유닛 카운트 조회 (UnitCount 리스트 형태)
        /// </summary>
        /// <param name="playerIndex">플레이어 인덱스 (0-7)</param>
        /// <param name="includeProduction">생산중인 유닛 포함 여부</param>
        /// <returns>UnitCount 객체 리스트</returns>
        List<UnitCount> GetAllUnitCountsToBuffer(int playerIndex, bool includeProduction = false);

        /// <summary>
        /// 유닛 카운트 데이터 새로고침
        /// </summary>
        /// <returns>새로고침 성공 여부</returns>
        bool RefreshUnitCounts();

        /// <summary>
        /// 캐시된 주소 무효화
        /// </summary>
        void InvalidateCache();
    }
}