using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StarcUp.Src.Business.UnitInfoUtil
{    // 1. 컴팩트한 유닛 데이터 구조
    public record UnitInfo(
        int Id,
        string Name,
        string DisplayName,
        string Race,
        int HP,
        int Shields,
        int Armor,
        int MineralCost,
        int GasCost,
        int BuildTime,
        int SupplyRequired,
        int SupplyProvided,
        bool IsBuilding,
        bool IsWorker,
        bool CanAttack,
        bool CanMove,
        bool IsFlyer,
        bool IsHero
    )
    {
        public int TotalHitPoints => HP + Shields;
        public int TotalCost => MineralCost + GasCost;
        public bool IsCombatUnit => CanAttack && !IsBuilding;
        public double HPPerMineral => MineralCost > 0 ? (double)TotalHitPoints / MineralCost : 0;
    }

    // 2. JSON 매핑용 DTO 클래스
    public class UnitDto
    {
        public int id { get; set; }
        public string name { get; set; } = "";
        public string displayName { get; set; } = "";
        public string race { get; set; } = "";
        public int maxHitPoints { get; set; }
        public int maxShields { get; set; }
        public int armor { get; set; }
        public int mineralPrice { get; set; }
        public int gasPrice { get; set; }
        public int buildTime { get; set; }
        public int supplyRequired { get; set; }
        public int supplyProvided { get; set; }
        public bool isBuilding { get; set; }
        public bool isWorker { get; set; }
        public bool canAttack { get; set; }
        public bool canMove { get; set; }
        public bool isFlyer { get; set; }
        public bool isHero { get; set; }

        // DTO를 Record로 변환
        public UnitInfo ToUnitInfo() => new(
            id, name, displayName, race,
            maxHitPoints, maxShields, armor,
            mineralPrice, gasPrice, buildTime,
            supplyRequired, supplyProvided,
            isBuilding, isWorker, canAttack, canMove, isFlyer, isHero
        );
    }
    public class UnitsContainer
    {
        public List<UnitDto> units { get; set; } = new();
    }
}
