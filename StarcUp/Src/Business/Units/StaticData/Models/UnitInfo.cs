namespace StarcUp.Business.Units.StaticData.Models
{
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
}