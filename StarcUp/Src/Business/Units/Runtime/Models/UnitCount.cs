using StarcUp.Business.Units.Types;

namespace StarcUp.Business.Units.Runtime.Models
{
    /// <summary>
    /// 특정 플레이어의 유닛 카운트 정보를 나타내는 클래스
    /// </summary>
    public class UnitCount
    {
        public UnitType UnitType { get; set; }
        public byte PlayerIndex { get; set; }
        public int CompletedCount { get; set; }
        public int ProductionCount { get; set; }
        public int TotalCount => CompletedCount + ProductionCount;

        public UnitCount()
        {
        }

        public UnitCount(UnitType unitType, byte playerIndex, int completedCount, int productionCount)
        {
            UnitType = unitType;
            PlayerIndex = playerIndex;
            CompletedCount = completedCount;
            ProductionCount = productionCount;
        }

        /// <summary>
        /// 버퍼에서 읽어온 데이터로 객체 업데이트 (메모리 재사용)
        /// </summary>
        public void ReadFromBuffer(UnitType unitType, byte playerIndex, int completedCount, int productionCount)
        {
            UnitType = unitType;
            PlayerIndex = playerIndex;
            CompletedCount = completedCount;
            ProductionCount = productionCount;
        }

        public bool IsValid => CompletedCount >= 0 && ProductionCount >= 0 && PlayerIndex < 8;

        public override string ToString()
        {
            return $"{UnitType} (Player {PlayerIndex}): {CompletedCount}+{ProductionCount}={TotalCount}";
        }

        public override bool Equals(object? obj)
        {
            if (obj is UnitCount other)
            {
                return UnitType == other.UnitType && 
                       PlayerIndex == other.PlayerIndex &&
                       CompletedCount == other.CompletedCount &&
                       ProductionCount == other.ProductionCount;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(UnitType, PlayerIndex, CompletedCount, ProductionCount);
        }
    }
}