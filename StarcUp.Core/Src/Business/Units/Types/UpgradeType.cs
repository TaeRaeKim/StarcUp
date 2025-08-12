namespace StarcUp.Business.Units.Types
{
    /// <summary>
    /// 스타크래프트 업그레이드 타입
    /// </summary>
    public enum UpgradeType : byte
    {
        // Terran Armor & Weapons
        Terran_Infantry_Armor = 0,
        Terran_Vehicle_Plating = 1,
        Terran_Ship_Plating = 2,
        Terran_Infantry_Weapons = 7,
        Terran_Vehicle_Weapons = 8,
        Terran_Ship_Weapons = 9,

        // Zerg Armor & Weapons
        Zerg_Carapace = 3,
        Zerg_Flyer_Carapace = 4,
        Zerg_Melee_Attacks = 10,
        Zerg_Missile_Attacks = 11,
        Zerg_Flyer_Attacks = 12,

        // Protoss Armor & Weapons
        Protoss_Ground_Armor = 5,
        Protoss_Air_Armor = 6,
        Protoss_Ground_Weapons = 13,
        Protoss_Air_Weapons = 14,
        Protoss_Plasma_Shields = 15,

        // Terran Unit Upgrades
        U_238_Shells = 16,                // Marine range
        Ion_Thrusters = 17,                // Vulture speed
        Titan_Reactor = 19,                // Science Vessel energy
        Ocular_Implants = 20,              // Ghost sight
        Moebius_Reactor = 21,              // Ghost energy
        Apollo_Reactor = 22,               // Wraith energy
        Colossus_Reactor = 23,             // Battlecruiser energy
        Caduceus_Reactor = 51,             // Medic energy
        Charon_Boosters = 54,              // Goliath range

        // Zerg Unit Upgrades
        Ventral_Sacs = 24,                 // Overlord transport
        Antennae = 25,                     // Overlord sight
        Pneumatized_Carapace = 26,         // Overlord speed
        Metabolic_Boost = 27,              // Zergling speed
        Adrenal_Glands = 28,               // Zergling attack speed
        Muscular_Augments = 29,            // Hydralisk speed
        Grooved_Spines = 30,               // Hydralisk range
        Gamete_Meiosis = 31,               // Queen energy
        Metasynaptic_Node = 32,            // Defiler energy
        Chitinous_Plating = 52,            // Ultralisk armor
        Anabolic_Synthesis = 53,           // Ultralisk speed

        // Protoss Unit Upgrades
        Singularity_Charge = 33,           // Dragoon range
        Leg_Enhancements = 34,             // Zealot speed
        Scarab_Damage = 35,                // Reaver scarab damage
        Reaver_Capacity = 36,              // Reaver scarab capacity
        Gravitic_Drive = 37,               // Shuttle speed
        Sensor_Array = 38,                 // Observer sight
        Gravitic_Boosters = 39,            // Observer speed
        Khaydarin_Amulet = 40,             // High Templar energy
        Apial_Sensors = 41,                // Scout sight
        Gravitic_Thrusters = 42,           // Scout speed
        Carrier_Capacity = 43,             // Carrier interceptor capacity
        Khaydarin_Core = 44,               // Arbiter energy
        Argus_Jewel = 47,                  // Corsair energy
        Argus_Talisman = 49,               // Dark Archon energy

        // Special values
        Upgrade_60 = 60,
        None = 61,
        Unknown = 62,
        MAX = 63
    }
}