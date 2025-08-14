namespace StarcUp.Business.Units.Types.Extensions
{
    /// <summary>
    /// UpgradeType 확장 메서드
    /// </summary>
    public static class UpgradeTypeExtensions
    {
        /// <summary>
        /// 업그레이드의 기본 소요 프레임 수를 가져옵니다.
        /// 레벨별 업그레이드의 경우 1레벨 기준입니다.
        /// </summary>
        public static int GetBaseFrames(this UpgradeType upgradeType)
        {
            if (!System.Enum.IsDefined(typeof(UpgradeFrames), upgradeType.ToString()))
                return 0;

            return (int)(UpgradeFrames)System.Enum.Parse(typeof(UpgradeFrames), upgradeType.ToString());
        }

        /// <summary>
        /// 업그레이드의 특정 레벨 소요 프레임 수를 가져옵니다.
        /// </summary>
        /// <param name="upgradeType">업그레이드 타입</param>
        /// <param name="level">업그레이드 레벨 (1-3)</param>
        public static int GetFramesForLevel(this UpgradeType upgradeType, int level)
        {
            if (level < 1 || level > 3)
                return 0;

            int baseFrames = GetBaseFrames(upgradeType);
            if (baseFrames == 0)
                return 0;

            // 레벨별 업그레이드 (무기/방어력)인지 확인
            if (IsLeveledUpgrade(upgradeType))
            {
                return level switch
                {
                    1 => 4000,
                    2 => 4480,
                    3 => 4960,
                    _ => baseFrames
                };
            }

            // 일반 업그레이드는 레벨에 관계없이 동일
            return baseFrames;
        }

        /// <summary>
        /// 레벨별 업그레이드 (무기/방어력)인지 확인합니다.
        /// </summary>
        public static bool IsLeveledUpgrade(this UpgradeType upgradeType)
        {
            return upgradeType switch
            {
                // Terran Armor & Weapons
                UpgradeType.Terran_Infantry_Armor or
                UpgradeType.Terran_Vehicle_Plating or
                UpgradeType.Terran_Ship_Plating or
                UpgradeType.Terran_Infantry_Weapons or
                UpgradeType.Terran_Vehicle_Weapons or
                UpgradeType.Terran_Ship_Weapons or
                // Zerg Armor & Weapons
                UpgradeType.Zerg_Carapace or
                UpgradeType.Zerg_Flyer_Carapace or
                UpgradeType.Zerg_Melee_Attacks or
                UpgradeType.Zerg_Missile_Attacks or
                UpgradeType.Zerg_Flyer_Attacks or
                // Protoss Armor & Weapons
                UpgradeType.Protoss_Ground_Armor or
                UpgradeType.Protoss_Air_Armor or
                UpgradeType.Protoss_Ground_Weapons or
                UpgradeType.Protoss_Air_Weapons or
                UpgradeType.Protoss_Plasma_Shields => true,
                _ => false
            };
        }
    }
}