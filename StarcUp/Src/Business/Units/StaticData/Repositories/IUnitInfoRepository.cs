using StarcUp.Business.Units.StaticData.Models;
using StarcUp.Business.Units.Types;
using System.Collections.Generic;

namespace StarcUp.Business.Units.StaticData.Repositories
{
    public interface IUnitInfoRepository
    {
        UnitInfo? Get(UnitType unitType);
        UnitInfo? GetById(int id);
        UnitInfo? GetByName(string name);
        bool TryGetById(int id, out UnitInfo? unit);
        bool TryGetByName(string name, out UnitInfo? unit);

        IReadOnlyList<UnitInfo> AllUnits { get; }
        IEnumerable<UnitInfo> GetByRace(string race);
        IEnumerable<UnitInfo> Buildings { get; }
        IEnumerable<UnitInfo> Units { get; }
        IEnumerable<UnitInfo> CombatUnits { get; }
        IEnumerable<UnitInfo> Workers { get; }
        IEnumerable<UnitInfo> Heroes { get; }

        int TotalCount { get; }
        int BuildingCount { get; }
        int UnitCount { get; }

        IEnumerable<UnitInfo> GetCheapestUnits(int maxCost);
        IEnumerable<UnitInfo> GetStrongestUnits(int minHP);
        UnitInfo? GetBestValueUnit();
    }
}