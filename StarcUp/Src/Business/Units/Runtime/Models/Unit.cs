using StarcUp.Business.Units.Types;

namespace StarcUp.Business.Units.Runtime.Models
{
    public class Unit
    {
        public uint Health { get; set; }
        public uint Shield { get; set; }
        public ushort CurrentX { get; set; }
        public ushort CurrentY { get; set; }
        public ushort DestX { get; set; }
        public ushort DestY { get; set; }
        public byte PlayerIndex { get; set; }
        public UnitType UnitType { get; set; }
        public byte ActionIndex { get; set; }
        public byte ActionState { get; set; }
        public byte AttackCooldown { get; set; }
        public ushort Timer { get; set; }
        public byte CurrentUpgrade { get; set; }
        public byte CurrentUpgradeLevel { get; set; }
        public byte ProductionQueueIndex { get; set; }
        public ushort[] ProductionQueue { get; set; } = new ushort[5];

        /// <summary>
        /// 기본 생성자 (미리 할당된 배열에서 사용)
        /// </summary>
        public Unit()
        {
            // 기본값으로 초기화
            ProductionQueue = new ushort[5];
        }

        public bool IsValid => UnitType != Types.UnitType.None && PlayerIndex < 12;
        public bool IsAlive => Health > 0;
        public bool IsBuilding => UnitType.IsBuilding();
        public bool IsWorker => UnitType.IsWorker();
        public bool IsHero => UnitType.IsHero();
        
        public int TotalHitPoints => (int)(Health + Shield);
        public double HealthPercentage => Health > 0 ? (double)Health / GetMaxHealth() : 0;
        public string DisplayName => UnitType.GetUnitName();
        public string Race => UnitType.GetRace();

        private uint GetMaxHealth()
        {
            return 100;
        }

        public static Unit FromRaw(UnitRaw raw)
        {
            var productionQueue = new ushort[5];
            unsafe
            {
                for (int i = 0; i < 5; i++)
                {
                    productionQueue[i] = raw.productionQueue[i];
                }
            }

            return new Unit
            {
                Health = raw.health,
                Shield = raw.shield,
                CurrentX = raw.currentX,
                CurrentY = raw.currentY,
                DestX = raw.destX,
                DestY = raw.destY,
                PlayerIndex = raw.playerIndex,
                UnitType = (UnitType)raw.unitType,
                ActionIndex = raw.actionIndex,
                ActionState = raw.actionState,
                AttackCooldown = raw.attackCooldown,
                Timer = raw.timer,
                CurrentUpgrade = raw.currentUpgrade,
                CurrentUpgradeLevel = raw.currentUpgradeLevel,
                ProductionQueueIndex = raw.productionQueueIndex,
                ProductionQueue = productionQueue
            };
        }

        /// <summary>
        /// 기존 Unit 인스턴스에 UnitRaw 데이터를 직접 파싱 (메모리 재활용)
        /// </summary>
        /// <param name="raw">파싱할 UnitRaw 데이터</param>
        public void ParseRaw(UnitRaw raw)
        {
            Health = raw.health;
            Shield = raw.shield;
            CurrentX = raw.currentX;
            CurrentY = raw.currentY;
            DestX = raw.destX;
            DestY = raw.destY;
            PlayerIndex = raw.playerIndex;
            UnitType = (UnitType)raw.unitType;
            ActionIndex = raw.actionIndex;
            ActionState = raw.actionState;
            AttackCooldown = raw.attackCooldown;
            Timer = raw.timer;
            CurrentUpgrade = raw.currentUpgrade;
            CurrentUpgradeLevel = raw.currentUpgradeLevel;
            ProductionQueueIndex = raw.productionQueueIndex;
            
            // ProductionQueue 배열 재활용 (이미 할당된 배열이 있으면 재사용)
            if (ProductionQueue == null || ProductionQueue.Length != 5)
            {
                ProductionQueue = new ushort[5];
            }
            
            unsafe
            {
                for (int i = 0; i < 5; i++)
                {
                    ProductionQueue[i] = raw.productionQueue[i];
                }
            }
        }

        public override string ToString()
        {
            return $"{DisplayName} [Player={PlayerIndex}, HP={Health}/{Shield}, Pos=({CurrentX},{CurrentY})]";
        }
    }
}