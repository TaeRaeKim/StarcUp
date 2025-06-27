namespace StarcUp.Src.Business.UnitInfoUtil
{
    public interface IUnitInfoUtils
    {
        // JSON에서 유닛 로딩
        extern private static  List<UnitInfo> LoadUnitsFromJson(string fileName);


        // 유닛 검색 메서드들
        UnitInfo? Get(UnitType unitType);
        UnitInfo? GetById(int id);
        UnitInfo? GetByName(string name);
        bool TryGetById(int id, out UnitInfo? unit);
        bool TryGetByName(string name, out UnitInfo? unit);

        // 유닛 컬렉션 접근
        IReadOnlyList<UnitInfo> AllUnits { get; }
        IEnumerable<UnitInfo> GetByRace(string race);
        IEnumerable<UnitInfo> Buildings { get; }
        IEnumerable<UnitInfo> Units { get; }
        IEnumerable<UnitInfo> CombatUnits { get; }
        IEnumerable<UnitInfo> Workers { get; }
        IEnumerable<UnitInfo> Heroes { get; }

        // 통계 정보
        int TotalCount { get; }
        int BuildingCount { get; }
        int UnitCount { get; }
    }
}
