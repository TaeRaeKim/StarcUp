using System;

namespace StarcUp.Business.Units.Types
{
    /// <summary>
    /// UpgradeType enum에 대한 확장 메서드
    /// </summary>
    public static class UpgradeTypeExtensions
    {
        /// <summary>
        /// 업그레이드가 유효한지 확인
        /// </summary>
        public static bool IsValid(this UpgradeType upgradeType)
        {
            return upgradeType != UpgradeType.None && 
                   upgradeType != UpgradeType.Unknown && 
                   upgradeType < UpgradeType.MAX;
        }

        /// <summary>
        /// 업그레이드가 방어력 업그레이드인지 확인
        /// </summary>
        public static bool IsArmorUpgrade(this UpgradeType upgradeType)
        {
            return upgradeType == UpgradeType.Terran_Infantry_Armor ||
                   upgradeType == UpgradeType.Terran_Vehicle_Plating ||
                   upgradeType == UpgradeType.Terran_Ship_Plating ||
                   upgradeType == UpgradeType.Zerg_Carapace ||
                   upgradeType == UpgradeType.Zerg_Flyer_Carapace ||
                   upgradeType == UpgradeType.Protoss_Ground_Armor ||
                   upgradeType == UpgradeType.Protoss_Air_Armor ||
                   upgradeType == UpgradeType.Protoss_Plasma_Shields;
        }

        /// <summary>
        /// 업그레이드가 공격력 업그레이드인지 확인
        /// </summary>
        public static bool IsWeaponUpgrade(this UpgradeType upgradeType)
        {
            return upgradeType == UpgradeType.Terran_Infantry_Weapons ||
                   upgradeType == UpgradeType.Terran_Vehicle_Weapons ||
                   upgradeType == UpgradeType.Terran_Ship_Weapons ||
                   upgradeType == UpgradeType.Zerg_Melee_Attacks ||
                   upgradeType == UpgradeType.Zerg_Missile_Attacks ||
                   upgradeType == UpgradeType.Zerg_Flyer_Attacks ||
                   upgradeType == UpgradeType.Protoss_Ground_Weapons ||
                   upgradeType == UpgradeType.Protoss_Air_Weapons;
        }

        /// <summary>
        /// 업그레이드가 에너지 관련 업그레이드인지 확인
        /// </summary>
        public static bool IsEnergyUpgrade(this UpgradeType upgradeType)
        {
            return upgradeType == UpgradeType.Titan_Reactor ||
                   upgradeType == UpgradeType.Moebius_Reactor ||
                   upgradeType == UpgradeType.Apollo_Reactor ||
                   upgradeType == UpgradeType.Colossus_Reactor ||
                   upgradeType == UpgradeType.Caduceus_Reactor ||
                   upgradeType == UpgradeType.Gamete_Meiosis ||
                   upgradeType == UpgradeType.Metasynaptic_Node ||
                   upgradeType == UpgradeType.Khaydarin_Amulet ||
                   upgradeType == UpgradeType.Khaydarin_Core ||
                   upgradeType == UpgradeType.Argus_Jewel ||
                   upgradeType == UpgradeType.Argus_Talisman;
        }

        /// <summary>
        /// 업그레이드가 속도 관련 업그레이드인지 확인
        /// </summary>
        public static bool IsSpeedUpgrade(this UpgradeType upgradeType)
        {
            return upgradeType == UpgradeType.Ion_Thrusters ||
                   upgradeType == UpgradeType.Pneumatized_Carapace ||
                   upgradeType == UpgradeType.Metabolic_Boost ||
                   upgradeType == UpgradeType.Muscular_Augments ||
                   upgradeType == UpgradeType.Anabolic_Synthesis ||
                   upgradeType == UpgradeType.Leg_Enhancements ||
                   upgradeType == UpgradeType.Gravitic_Drive ||
                   upgradeType == UpgradeType.Gravitic_Boosters ||
                   upgradeType == UpgradeType.Gravitic_Thrusters;
        }

        /// <summary>
        /// 업그레이드가 사거리 관련 업그레이드인지 확인
        /// </summary>
        public static bool IsRangeUpgrade(this UpgradeType upgradeType)
        {
            return upgradeType == UpgradeType.U_238_Shells ||
                   upgradeType == UpgradeType.Charon_Boosters ||
                   upgradeType == UpgradeType.Grooved_Spines ||
                   upgradeType == UpgradeType.Singularity_Charge;
        }

        /// <summary>
        /// 업그레이드가 시야 관련 업그레이드인지 확인
        /// </summary>
        public static bool IsSightUpgrade(this UpgradeType upgradeType)
        {
            return upgradeType == UpgradeType.Ocular_Implants ||
                   upgradeType == UpgradeType.Antennae ||
                   upgradeType == UpgradeType.Sensor_Array ||
                   upgradeType == UpgradeType.Apial_Sensors;
        }

        /// <summary>
        /// 업그레이드가 용량 관련 업그레이드인지 확인
        /// </summary>
        public static bool IsCapacityUpgrade(this UpgradeType upgradeType)
        {
            return upgradeType == UpgradeType.Reaver_Capacity ||
                   upgradeType == UpgradeType.Carrier_Capacity ||
                   upgradeType == UpgradeType.Ventral_Sacs;
        }

        /// <summary>
        /// 업그레이드가 속한 종족 가져오기
        /// </summary>
        public static RaceType GetRace(this UpgradeType upgradeType)
        {
            // Terran upgrades
            if (upgradeType == UpgradeType.Terran_Infantry_Armor ||
                upgradeType == UpgradeType.Terran_Vehicle_Plating ||
                upgradeType == UpgradeType.Terran_Ship_Plating ||
                upgradeType == UpgradeType.Terran_Infantry_Weapons ||
                upgradeType == UpgradeType.Terran_Vehicle_Weapons ||
                upgradeType == UpgradeType.Terran_Ship_Weapons ||
                upgradeType == UpgradeType.U_238_Shells ||
                upgradeType == UpgradeType.Ion_Thrusters ||
                upgradeType == UpgradeType.Titan_Reactor ||
                upgradeType == UpgradeType.Ocular_Implants ||
                upgradeType == UpgradeType.Moebius_Reactor ||
                upgradeType == UpgradeType.Apollo_Reactor ||
                upgradeType == UpgradeType.Colossus_Reactor ||
                upgradeType == UpgradeType.Caduceus_Reactor ||
                upgradeType == UpgradeType.Charon_Boosters)
            {
                return RaceType.Terran;
            }

            // Zerg upgrades
            if (upgradeType == UpgradeType.Zerg_Carapace ||
                upgradeType == UpgradeType.Zerg_Flyer_Carapace ||
                upgradeType == UpgradeType.Zerg_Melee_Attacks ||
                upgradeType == UpgradeType.Zerg_Missile_Attacks ||
                upgradeType == UpgradeType.Zerg_Flyer_Attacks ||
                upgradeType == UpgradeType.Ventral_Sacs ||
                upgradeType == UpgradeType.Antennae ||
                upgradeType == UpgradeType.Pneumatized_Carapace ||
                upgradeType == UpgradeType.Metabolic_Boost ||
                upgradeType == UpgradeType.Adrenal_Glands ||
                upgradeType == UpgradeType.Muscular_Augments ||
                upgradeType == UpgradeType.Grooved_Spines ||
                upgradeType == UpgradeType.Gamete_Meiosis ||
                upgradeType == UpgradeType.Metasynaptic_Node ||
                upgradeType == UpgradeType.Chitinous_Plating ||
                upgradeType == UpgradeType.Anabolic_Synthesis)
            {
                return RaceType.Zerg;
            }

            // Protoss upgrades
            if (upgradeType == UpgradeType.Protoss_Ground_Armor ||
                upgradeType == UpgradeType.Protoss_Air_Armor ||
                upgradeType == UpgradeType.Protoss_Ground_Weapons ||
                upgradeType == UpgradeType.Protoss_Air_Weapons ||
                upgradeType == UpgradeType.Protoss_Plasma_Shields ||
                upgradeType == UpgradeType.Singularity_Charge ||
                upgradeType == UpgradeType.Leg_Enhancements ||
                upgradeType == UpgradeType.Scarab_Damage ||
                upgradeType == UpgradeType.Reaver_Capacity ||
                upgradeType == UpgradeType.Gravitic_Drive ||
                upgradeType == UpgradeType.Sensor_Array ||
                upgradeType == UpgradeType.Gravitic_Boosters ||
                upgradeType == UpgradeType.Khaydarin_Amulet ||
                upgradeType == UpgradeType.Apial_Sensors ||
                upgradeType == UpgradeType.Gravitic_Thrusters ||
                upgradeType == UpgradeType.Carrier_Capacity ||
                upgradeType == UpgradeType.Khaydarin_Core ||
                upgradeType == UpgradeType.Argus_Jewel ||
                upgradeType == UpgradeType.Argus_Talisman)
            {
                return RaceType.Protoss;
            }

            throw new ArgumentException($"Unknown race for upgrade: {upgradeType}");
        }

        /// <summary>
        /// 업그레이드의 한국어 이름 가져오기
        /// </summary>
        public static string GetKoreanName(this UpgradeType upgradeType)
        {
            return upgradeType switch
            {
                // Terran
                UpgradeType.Terran_Infantry_Armor => "보병 방어력",
                UpgradeType.Terran_Vehicle_Plating => "차량 방어력",
                UpgradeType.Terran_Ship_Plating => "우주선 방어력",
                UpgradeType.Terran_Infantry_Weapons => "보병 공격력",
                UpgradeType.Terran_Vehicle_Weapons => "차량 공격력",
                UpgradeType.Terran_Ship_Weapons => "우주선 공격력",
                UpgradeType.U_238_Shells => "U-238 쉘",
                UpgradeType.Ion_Thrusters => "이온 스러스터",
                UpgradeType.Titan_Reactor => "타이탄 리액터",
                UpgradeType.Ocular_Implants => "오큘러 임플란츠",
                UpgradeType.Moebius_Reactor => "뫼비우스 리액터",
                UpgradeType.Apollo_Reactor => "아폴로 리액터",
                UpgradeType.Colossus_Reactor => "콜로서스 리액터",
                UpgradeType.Caduceus_Reactor => "카두세우스 리액터",
                UpgradeType.Charon_Boosters => "카론 부스터",
                
                // Zerg
                UpgradeType.Zerg_Carapace => "갑피",
                UpgradeType.Zerg_Flyer_Carapace => "비행체 갑피",
                UpgradeType.Zerg_Melee_Attacks => "근접 공격",
                UpgradeType.Zerg_Missile_Attacks => "미사일 공격",
                UpgradeType.Zerg_Flyer_Attacks => "비행체 공격",
                UpgradeType.Ventral_Sacs => "벤트럴 섹",
                UpgradeType.Antennae => "안테나",
                UpgradeType.Pneumatized_Carapace => "뉴머타이즈드 캐러페이스",
                UpgradeType.Metabolic_Boost => "메타볼릭 부스트",
                UpgradeType.Adrenal_Glands => "아드레날 글랜즈",
                UpgradeType.Muscular_Augments => "머스큘러 아규먼츠",
                UpgradeType.Grooved_Spines => "그루브드 스파인스",
                UpgradeType.Gamete_Meiosis => "게미트 마이오시스",
                UpgradeType.Metasynaptic_Node => "메타시냅틱 노드",
                UpgradeType.Chitinous_Plating => "카이트너스 플레이팅",
                UpgradeType.Anabolic_Synthesis => "아나볼릭 신테시스",
                
                // Protoss
                UpgradeType.Protoss_Ground_Armor => "지상 방어력",
                UpgradeType.Protoss_Air_Armor => "공중 방어력",
                UpgradeType.Protoss_Ground_Weapons => "지상 공격력",
                UpgradeType.Protoss_Air_Weapons => "공중 공격력",
                UpgradeType.Protoss_Plasma_Shields => "플라스마 쉴드",
                UpgradeType.Singularity_Charge => "싱귤라리티 차지",
                UpgradeType.Leg_Enhancements => "레그 인핸스먼트",
                UpgradeType.Scarab_Damage => "스캐럽 공격력",
                UpgradeType.Reaver_Capacity => "리버 수용력",
                UpgradeType.Gravitic_Drive => "그래비틱 드라이브",
                UpgradeType.Sensor_Array => "센서 어레이",
                UpgradeType.Gravitic_Boosters => "그래비틱 부스터",
                UpgradeType.Khaydarin_Amulet => "케이다린 아뮬렛",
                UpgradeType.Apial_Sensors => "에이피얼 센서",
                UpgradeType.Gravitic_Thrusters => "그래비틱 스러스터",
                UpgradeType.Carrier_Capacity => "캐리어 수용력",
                UpgradeType.Khaydarin_Core => "케이다린 코어",
                UpgradeType.Argus_Jewel => "아르거스 쥬얼",
                UpgradeType.Argus_Talisman => "아르거스 탈리스만",
                
                // Special
                UpgradeType.None => "없음",
                UpgradeType.Unknown => "알 수 없음",
                _ => upgradeType.ToString()
            };
        }
    }
}