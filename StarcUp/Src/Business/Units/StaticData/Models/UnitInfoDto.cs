namespace StarcUp.Business.Units.StaticData.Models
{
    public class UnitInfoDto
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

        public UnitInfo ToUnitInfo() => new(
            id, name, displayName, race,
            maxHitPoints, maxShields, armor,
            mineralPrice, gasPrice, buildTime,
            supplyRequired, supplyProvided,
            isBuilding, isWorker, canAttack, canMove, isFlyer, isHero
        );
    }
}