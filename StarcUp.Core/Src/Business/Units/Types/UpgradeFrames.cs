namespace StarcUp.Business.Units.Types
{
    /// <summary>
    /// 업그레이드별 소요 프레임 수
    /// </summary>
    public enum UpgradeFrames : ushort
    {
        // Terran Armor & Weapons (레벨별 다름: 1lv=4000, 2lv=4480, 3lv=4960)
        Terran_Infantry_Armor = 4000,
        Terran_Vehicle_Plating = 4000,
        Terran_Ship_Plating = 4000,
        Terran_Infantry_Weapons = 4000,
        Terran_Vehicle_Weapons = 4000,
        Terran_Ship_Weapons = 4000,

        // Zerg Armor & Weapons (레벨별 다름: 1lv=4000, 2lv=4480, 3lv=4960)
        Zerg_Carapace = 4000,
        Zerg_Flyer_Carapace = 4000,
        Zerg_Melee_Attacks = 4000,
        Zerg_Missile_Attacks = 4000,
        Zerg_Flyer_Attacks = 4000,

        // Protoss Armor & Weapons (레벨별 다름: 1lv=4000, 2lv=4480, 3lv=4960)
        Protoss_Ground_Armor = 4000,
        Protoss_Air_Armor = 4000,
        Protoss_Ground_Weapons = 4000,
        Protoss_Air_Weapons = 4000,
        Protoss_Plasma_Shields = 4000,

        // Terran Unit Upgrades
        U_238_Shells = 1500,
        Ion_Thrusters = 1500,
        Titan_Reactor = 2500,
        Ocular_Implants = 2500,
        Moebius_Reactor = 2500,
        Apollo_Reactor = 2500,
        Colossus_Reactor = 2500,
        Caduceus_Reactor = 2500,
        Charon_Boosters = 2000,

        // Zerg Unit Upgrades
        Ventral_Sacs = 2400,
        Antennae = 2000,
        Pneumatized_Carapace = 2000,
        Metabolic_Boost = 1500,
        Adrenal_Glands = 1500,
        Muscular_Augments = 1500,
        Grooved_Spines = 1500,
        Gamete_Meiosis = 2500,
        Metasynaptic_Node = 2500,
        Chitinous_Plating = 2000,
        Anabolic_Synthesis = 2000,

        // Protoss Unit Upgrades
        Singularity_Charge = 2500,
        Leg_Enhancements = 2000,
        Scarab_Damage = 2500,
        Reaver_Capacity = 2500,
        Gravitic_Drive = 2500,
        Sensor_Array = 2000,
        Gravitic_Boosters = 2000,
        Khaydarin_Amulet = 2500,
        Apial_Sensors = 2000,
        Gravitic_Thrusters = 2500,
        Carrier_Capacity = 1500,
        Khaydarin_Core = 2500,
        Argus_Jewel = 2500,
        Argus_Talisman = 2500,

        // Special values
        Upgrade_60 = 0,
        None = 0,
        Unknown = 0,
        MAX = 0
    }
}