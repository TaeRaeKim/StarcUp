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
        public byte GatheringState { get; set; }
        public nint MemoryAddress { get; set; } = 0; // 유닛의 메모리 주소 (고유 식별자)

        /// <summary>
        /// 기본 생성자 (미리 할당된 배열에서 사용)
        /// </summary>
        public Unit()
        {
            Init();
        }

        public bool IsValid => UnitType != Types.UnitType.None && PlayerIndex < 12;
        public bool IsAlive => Health > 0;
        public bool IsBuilding => UnitType.IsBuilding();
        public bool IsWorker => UnitType.IsWorker();
        public bool IsHero => UnitType.IsHero();
        public bool IsGasBuilding => UnitType.IsGasBuilding();

        public int TotalHitPoints => (int)(Health + Shield);
        public double HealthPercentage => Health > 0 ? (double)Health / GetMaxHealth() : 0;
        public string DisplayName => UnitType.GetUnitName();
        public string Race => UnitType.GetRace();

        private uint GetMaxHealth()
        {
            return 100;
        }

        public static Unit FromRaw(UnitRaw raw, nint memoryAddress = 0)
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
                ProductionQueue = productionQueue,
                GatheringState = raw.gatheringState,
                MemoryAddress = memoryAddress
            };
        }

        /// <summary>
        /// 기존 Unit 인스턴스에 UnitRaw 데이터를 직접 파싱 (메모리 재활용)
        /// </summary>
        /// <param name="raw">파싱할 UnitRaw 데이터</param>
        /// <param name="memoryAddress">유닛의 메모리 주소</param>
        public void ParseRaw(UnitRaw raw, nint memoryAddress = 0)
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
            GatheringState = raw.gatheringState;
            MemoryAddress = memoryAddress;

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

        public void CopyFrom(Unit other)
        {
            if (other == null) return;

            Health = other.Health;
            Shield = other.Shield;
            CurrentX = other.CurrentX;
            CurrentY = other.CurrentY;
            DestX = other.DestX;
            DestY = other.DestY;
            PlayerIndex = other.PlayerIndex;
            UnitType = other.UnitType;
            ActionIndex = other.ActionIndex;
            ActionState = other.ActionState;
            AttackCooldown = other.AttackCooldown;
            Timer = other.Timer;
            CurrentUpgrade = other.CurrentUpgrade;
            CurrentUpgradeLevel = other.CurrentUpgradeLevel;

            // ProductionQueue 배열 재활용
            if (ProductionQueue == null || ProductionQueue.Length != 5)
            {
                ProductionQueue = new ushort[5];
            }

            for (int i = 0; i < 5; i++)
            {
                ProductionQueue[i] = other.ProductionQueue[i];
            }
        }

        public override string ToString()
        {
            return $"{DisplayName} [Player={PlayerIndex}, HP={Health}/{Shield}, Pos=({CurrentX},{CurrentY})]";
        }

        public void Init()
        {
            Health = 0;
            Shield = 0;
            CurrentX = 0;
            CurrentY = 0;
            DestX = 0;
            DestY = 0;
            UnitType = UnitType.None;
            ActionIndex = 0;
            ActionState = 0;
            AttackCooldown = 0;
            Timer = 0;
            CurrentUpgrade = 0;
            CurrentUpgradeLevel = 0;
            ProductionQueueIndex = 0;
            GatheringState = 0;

            if (ProductionQueue == null || ProductionQueue.Length != 5)
            {
                ProductionQueue = new ushort[5];
            }
            else
            {
                for (int i = 0; i < 5; i++)
                {
                    ProductionQueue[i] = 0;
                }
            }
        }
    }
}