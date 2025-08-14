using StarcUp.Business.Units.Types;

namespace StarcUp.Infrastructure.Memory
{
    /// <summary>
    /// 게임 메모리 오프셋 저장소 인터페이스
    /// </summary>
    public interface IGameOffsetRepository
    {
        // 기본 오프셋
        int GetBaseOffset();
        int GetMapNameOffset();
        int GetProductionOffset();
        
        // 유닛 관련 오프셋
        int GetUnitCompletedOffset(UnitType unitType);
        int GetUnitProductionOffset(UnitType unitType);
        int GetUnitPlayerOffset(UnitType unitType, byte playerIndex, bool includeProduction = false);
        Dictionary<UnitType, int> GetAllUnitCompletedOffsets();
        
        // 인구수 관련 오프셋
        int GetTerranSupplyUsedOffset();
        int GetTerranSupplyMaxOffset();
        int GetZergSupplyUsedOffset();
        int GetZergSupplyMaxOffset();
        int GetProtossSupplyUsedOffset();
        int GetProtossSupplyMaxOffset();
        
        // 업그레이드 관련 오프셋
        int GetUpgradeOffset1();      // 0-43 업그레이드
        int GetUpgradeOffset2();      // 47-54 업그레이드
        int GetTechOffset1();         // 0-23 테크
        int GetTechOffset2();         // 24-43 테크
        
        // 버퍼 관련
        int GetBufferSize();
        int GetMinBufferOffset();
        int GetBufferOffset(int absoluteOffset);
        
        // 캐시 관리
        void ClearCache();
    }
}