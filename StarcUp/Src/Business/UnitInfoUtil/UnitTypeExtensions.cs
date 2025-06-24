using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;

namespace StarcUp.Src.Business.UnitInfoUtil
{
    // 완전한 UnitType Enum (BWAPI 순서와 ID 매칭)
    public enum UnitType
    {
        // Terran Units (0-34)
        [Description("Terran_Marine")]
        TerranMarine = 0,

        [Description("Terran_Ghost")]
        TerranGhost = 1,

        [Description("Terran_Vulture")]
        TerranVulture = 2,

        [Description("Terran_Goliath")]
        TerranGoliath = 3,

        [Description("Terran_Goliath_Turret")]
        TerranGoliathTurret = 4,

        [Description("Terran_Siege_Tank_Tank_Mode")]
        TerranSiegeTankTankMode = 5,

        [Description("Terran_Siege_Tank_Tank_Mode_Turret")]
        TerranSiegeTankTankModeTurret = 6,

        [Description("Terran_SCV")]
        TerranSCV = 7,

        [Description("Terran_Wraith")]
        TerranWraith = 8,

        [Description("Terran_Science_Vessel")]
        TerranScienceVessel = 9,

        [Description("Hero_Gui_Montag")]
        HeroGuiMontag = 10,

        [Description("Terran_Dropship")]
        TerranDropship = 11,

        [Description("Terran_Battlecruiser")]
        TerranBattlecruiser = 12,

        [Description("Terran_Vulture_Spider_Mine")]
        TerranVultureSpiderMine = 13,

        [Description("Terran_Nuclear_Missile")]
        TerranNuclearMissile = 14,

        [Description("Terran_Civilian")]
        TerranCivilian = 15,

        [Description("Hero_Sarah_Kerrigan")]
        HeroSarahKerrigan = 16,

        [Description("Hero_Alan_Schezar")]
        HeroAlanSchezar = 17,

        [Description("Hero_Alan_Schezar_Turret")]
        HeroAlanSchezarTurret = 18,

        [Description("Hero_Jim_Raynor_Vulture")]
        HeroJimRaynorVulture = 19,

        [Description("Hero_Jim_Raynor_Marine")]
        HeroJimRaynorMarine = 20,

        [Description("Hero_Tom_Kazansky")]
        HeroTomKazansky = 21,

        [Description("Hero_Magellan")]
        HeroMagellan = 22,

        [Description("Hero_Edmund_Duke_Tank_Mode")]
        HeroEdmundDukeTankMode = 23,

        [Description("Hero_Edmund_Duke_Tank_Mode_Turret")]
        HeroEdmundDukeTankModeTurret = 24,

        [Description("Hero_Edmund_Duke_Siege_Mode")]
        HeroEdmundDukeSiegeMode = 25,

        [Description("Hero_Edmund_Duke_Siege_Mode_Turret")]
        HeroEdmundDukeSiegeModeTurret = 26,

        [Description("Hero_Arcturus_Mengsk")]
        HeroArcturusMengsk = 27,

        [Description("Hero_Hyperion")]
        HeroHyperion = 28,

        [Description("Hero_Norad_II")]
        HeroNoradII = 29,

        [Description("Terran_Siege_Tank_Siege_Mode")]
        TerranSiegeTankSiegeMode = 30,

        [Description("Terran_Siege_Tank_Siege_Mode_Turret")]
        TerranSiegeTankSiegeModeTurret = 31,

        [Description("Terran_Firebat")]
        TerranFirebat = 32,

        [Description("Spell_Scanner_Sweep")]
        SpellScannerSweep = 33,

        [Description("Terran_Medic")]
        TerranMedic = 34,

        // Zerg Units (35-57)
        [Description("Zerg_Larva")]
        ZergLarva = 35,

        [Description("Zerg_Egg")]
        ZergEgg = 36,

        [Description("Zerg_Zergling")]
        ZergZergling = 37,

        [Description("Zerg_Hydralisk")]
        ZergHydralisk = 38,

        [Description("Zerg_Ultralisk")]
        ZergUltralisk = 39,

        [Description("Zerg_Broodling")]
        ZergBroodling = 40,

        [Description("Zerg_Drone")]
        ZergDrone = 41,

        [Description("Zerg_Overlord")]
        ZergOverlord = 42,

        [Description("Zerg_Mutalisk")]
        ZergMutalisk = 43,

        [Description("Zerg_Guardian")]
        ZergGuardian = 44,

        [Description("Zerg_Queen")]
        ZergQueen = 45,

        [Description("Zerg_Defiler")]
        ZergDefiler = 46,

        [Description("Zerg_Scourge")]
        ZergScourge = 47,

        [Description("Hero_Torrasque")]
        HeroTorrasque = 48,

        [Description("Hero_Matriarch")]
        HeroMatriarch = 49,

        [Description("Zerg_Infested_Terran")]
        ZergInfestedTerran = 50,

        [Description("Hero_Infested_Kerrigan")]
        HeroInfestedKerrigan = 51,

        [Description("Hero_Unclean_One")]
        HeroUncleanOne = 52,

        [Description("Hero_Hunter_Killer")]
        HeroHunterKiller = 53,

        [Description("Hero_Devouring_One")]
        HeroDevouringOne = 54,

        [Description("Hero_Kukulza_Mutalisk")]
        HeroKukulzaMutalisk = 55,

        [Description("Hero_Kukulza_Guardian")]
        HeroKukulzaGuardian = 56,

        [Description("Hero_Yggdrasill")]
        HeroYggdrasill = 57,

        // Expansion Units (58-63)
        [Description("Terran_Valkyrie")]
        TerranValkyrie = 58,

        [Description("Zerg_Cocoon")]
        ZergCocoon = 59,

        [Description("Protoss_Corsair")]
        ProtossCorsair = 60,

        [Description("Protoss_Dark_Templar")]
        ProtossDarkTemplar = 61,

        [Description("Zerg_Devourer")]
        ZergDevourer = 62,

        [Description("Protoss_Dark_Archon")]
        ProtossDarkArchon = 63,

        // Protoss Units (64-88)
        [Description("Protoss_Probe")]
        ProtossProbe = 64,

        [Description("Protoss_Zealot")]
        ProtossZealot = 65,

        [Description("Protoss_Dragoon")]
        ProtossDragoon = 66,

        [Description("Protoss_High_Templar")]
        ProtossHighTemplar = 67,

        [Description("Protoss_Archon")]
        ProtossArchon = 68,

        [Description("Protoss_Shuttle")]
        ProtossShuttle = 69,

        [Description("Protoss_Scout")]
        ProtossScout = 70,

        [Description("Protoss_Arbiter")]
        ProtossArbiter = 71,

        [Description("Protoss_Carrier")]
        ProtossCarrier = 72,

        [Description("Protoss_Interceptor")]
        ProtossInterceptor = 73,

        [Description("Hero_Dark_Templar")]
        HeroDarkTemplar = 74,

        [Description("Hero_Zeratul")]
        HeroZeratul = 75,

        [Description("Hero_Tassadar_Zeratul_Archon")]
        HeroTassadarZeratulArchon = 76,

        [Description("Hero_Fenix_Zealot")]
        HeroFenixZealot = 77,

        [Description("Hero_Fenix_Dragoon")]
        HeroFenixDragoon = 78,

        [Description("Hero_Tassadar")]
        HeroTassadar = 79,

        [Description("Hero_Mojo")]
        HeroMojo = 80,

        [Description("Hero_Warbringer")]
        HeroWarbringer = 81,

        [Description("Hero_Gantrithor")]
        HeroGantrithor = 82,

        [Description("Protoss_Reaver")]
        ProtossReaver = 83,

        [Description("Protoss_Observer")]
        ProtossObserver = 84,

        [Description("Protoss_Scarab")]
        ProtossScarab = 85,

        [Description("Hero_Danimoth")]
        HeroDanimoth = 86,

        [Description("Hero_Aldaris")]
        HeroAldaris = 87,

        [Description("Hero_Artanis")]
        HeroArtanis = 88,

        // Critters and Special Units (89-105)
        [Description("Critter_Rhynadon")]
        CritterRhynadon = 89,

        [Description("Critter_Bengalaas")]
        CritterBengalaas = 90,

        [Description("Special_Cargo_Ship")]
        SpecialCargoShip = 91,

        [Description("Special_Mercenary_Gunship")]
        SpecialMercenaryGunship = 92,

        [Description("Critter_Scantid")]
        CritterScantid = 93,

        [Description("Critter_Kakaru")]
        CritterKakaru = 94,

        [Description("Critter_Ragnasaur")]
        CritterRagnasaur = 95,

        [Description("Critter_Ursadon")]
        CritterUrsadon = 96,

        [Description("Zerg_Lurker_Egg")]
        ZergLurkerEgg = 97,

        [Description("Hero_Raszagal")]
        HeroRaszagal = 98,

        [Description("Hero_Samir_Duran")]
        HeroSamirDuran = 99,

        [Description("Hero_Alexei_Stukov")]
        HeroAlexeiStukov = 100,

        [Description("Special_Map_Revealer")]
        SpecialMapRevealer = 101,

        [Description("Hero_Gerard_DuGalle")]
        HeroGerardDuGalle = 102,

        [Description("Zerg_Lurker")]
        ZergLurker = 103,

        [Description("Hero_Infested_Duran")]
        HeroInfestedDuran = 104,

        [Description("Spell_Disruption_Web")]
        SpellDisruptionWeb = 105,

        // Terran Buildings (106-129)
        [Description("Terran_Command_Center")]
        TerranCommandCenter = 106,

        [Description("Terran_Comsat_Station")]
        TerranComsatStation = 107,

        [Description("Terran_Nuclear_Silo")]
        TerranNuclearSilo = 108,

        [Description("Terran_Supply_Depot")]
        TerranSupplyDepot = 109,

        [Description("Terran_Refinery")]
        TerranRefinery = 110,

        [Description("Terran_Barracks")]
        TerranBarracks = 111,

        [Description("Terran_Academy")]
        TerranAcademy = 112,

        [Description("Terran_Factory")]
        TerranFactory = 113,

        [Description("Terran_Starport")]
        TerranStarport = 114,

        [Description("Terran_Control_Tower")]
        TerranControlTower = 115,

        [Description("Terran_Science_Facility")]
        TerranScienceFacility = 116,

        [Description("Terran_Covert_Ops")]
        TerranCovertOps = 117,

        [Description("Terran_Physics_Lab")]
        TerranPhysicsLab = 118,

        [Description("Unused_Terran1")]
        UnusedTerran1 = 119,

        [Description("Terran_Machine_Shop")]
        TerranMachineShop = 120,

        [Description("Unused_Terran2")]
        UnusedTerran2 = 121,

        [Description("Terran_Engineering_Bay")]
        TerranEngineeringBay = 122,

        [Description("Terran_Armory")]
        TerranArmory = 123,

        [Description("Terran_Missile_Turret")]
        TerranMissileTurret = 124,

        [Description("Terran_Bunker")]
        TerranBunker = 125,

        [Description("Special_Crashed_Norad_II")]
        SpecialCrashedNoradII = 126,

        [Description("Special_Ion_Cannon")]
        SpecialIonCannon = 127,

        [Description("Powerup_Uraj_Crystal")]
        PowerupUrajCrystal = 128,

        [Description("Powerup_Khalis_Crystal")]
        PowerupKhalisCrystal = 129,

        // Zerg Buildings (130-153)
        [Description("Zerg_Infested_Command_Center")]
        ZergInfestedCommandCenter = 130,

        [Description("Zerg_Hatchery")]
        ZergHatchery = 131,

        [Description("Zerg_Lair")]
        ZergLair = 132,

        [Description("Zerg_Hive")]
        ZergHive = 133,

        [Description("Zerg_Nydus_Canal")]
        ZergNydusCanal = 134,

        [Description("Zerg_Hydralisk_Den")]
        ZergHydraliskDen = 135,

        [Description("Zerg_Defiler_Mound")]
        ZergDefilerMound = 136,

        [Description("Zerg_Greater_Spire")]
        ZergGreaterSpire = 137,

        [Description("Zerg_Queens_Nest")]
        ZergQueensNest = 138,

        [Description("Zerg_Evolution_Chamber")]
        ZergEvolutionChamber = 139,

        [Description("Zerg_Ultralisk_Cavern")]
        ZergUltraliskCavern = 140,

        [Description("Zerg_Spire")]
        ZergSpire = 141,

        [Description("Zerg_Spawning_Pool")]
        ZergSpawningPool = 142,

        [Description("Zerg_Creep_Colony")]
        ZergCreepColony = 143,

        [Description("Zerg_Spore_Colony")]
        ZergSporeColony = 144,

        [Description("Unused_Zerg1")]
        UnusedZerg1 = 145,

        [Description("Zerg_Sunken_Colony")]
        ZergSunkenColony = 146,

        [Description("Special_Overmind_With_Shell")]
        SpecialOvermindWithShell = 147,

        [Description("Special_Overmind")]
        SpecialOvermind = 148,

        [Description("Zerg_Extractor")]
        ZergExtractor = 149,

        [Description("Special_Mature_Chrysalis")]
        SpecialMatureChrysalis = 150,

        [Description("Special_Cerebrate")]
        SpecialCerebrate = 151,

        [Description("Special_Cerebrate_Daggoth")]
        SpecialCerebrateDaggoth = 152,

        [Description("Unused_Zerg2")]
        UnusedZerg2 = 153,

        // Protoss Buildings (154-175)
        [Description("Protoss_Nexus")]
        ProtossNexus = 154,

        [Description("Protoss_Robotics_Facility")]
        ProtossRoboticsFacility = 155,

        [Description("Protoss_Pylon")]
        ProtossPylon = 156,

        [Description("Protoss_Assimilator")]
        ProtossAssimilator = 157,

        [Description("Unused_Protoss1")]
        UnusedProtoss1 = 158,

        [Description("Protoss_Observatory")]
        ProtossObservatory = 159,

        [Description("Protoss_Gateway")]
        ProtossGateway = 160,

        [Description("Unused_Protoss2")]
        UnusedProtoss2 = 161,

        [Description("Protoss_Photon_Cannon")]
        ProtossPhotonCannon = 162,

        [Description("Protoss_Citadel_of_Adun")]
        ProtossCitadelOfAdun = 163,

        [Description("Protoss_Cybernetics_Core")]
        ProtossCyberneticsCore = 164,

        [Description("Protoss_Templar_Archives")]
        ProtossTemplarArchives = 165,

        [Description("Protoss_Forge")]
        ProtossForge = 166,

        [Description("Protoss_Stargate")]
        ProtossStargate = 167,

        [Description("Special_Stasis_Cell_Prison")]
        SpecialStasisCellPrison = 168,

        [Description("Protoss_Fleet_Beacon")]
        ProtossFleetBeacon = 169,

        [Description("Protoss_Arbiter_Tribunal")]
        ProtossArbiterTribunal = 170,

        [Description("Protoss_Robotics_Support_Bay")]
        ProtossRoboticsSupportBay = 171,

        [Description("Protoss_Shield_Battery")]
        ProtossShieldBattery = 172,

        [Description("Special_Khaydarin_Crystal_Form")]
        SpecialKhaydarinCrystalForm = 173,

        [Description("Special_Protoss_Temple")]
        SpecialProtossTemple = 174,

        [Description("Special_XelNaga_Temple")]
        SpecialXelNagaTemple = 175,

        // Resources and Special Objects (176-227)
        [Description("Resource_Mineral_Field")]
        ResourceMineralField = 176,

        [Description("Resource_Mineral_Field_Type_2")]
        ResourceMineralFieldType2 = 177,

        [Description("Resource_Mineral_Field_Type_3")]
        ResourceMineralFieldType3 = 178,

        [Description("Unused_Cave")]
        UnusedCave = 179,

        [Description("Unused_Cave_In")]
        UnusedCaveIn = 180,

        [Description("Unused_Cantina")]
        UnusedCantina = 181,

        [Description("Unused_Mining_Platform")]
        UnusedMiningPlatform = 182,

        [Description("Unused_Independant_Command_Center")]
        UnusedIndependantCommandCenter = 183,

        [Description("Special_Independant_Starport")]
        SpecialIndependantStarport = 184,

        [Description("Unused_Independant_Jump_Gate")]
        UnusedIndependantJumpGate = 185,

        [Description("Unused_Ruins")]
        UnusedRuins = 186,

        [Description("Unused_Khaydarin_Crystal_Formation")]
        UnusedKhaydarinCrystalFormation = 187,

        [Description("Resource_Vespene_Geyser")]
        ResourceVespeneGeyser = 188,

        [Description("Special_Warp_Gate")]
        SpecialWarpGate = 189,

        [Description("Special_Psi_Disrupter")]
        SpecialPsiDisrupter = 190,

        [Description("Unused_Zerg_Marker")]
        UnusedZergMarker = 191,

        [Description("Unused_Terran_Marker")]
        UnusedTerranMarker = 192,

        [Description("Unused_Protoss_Marker")]
        UnusedProtossMarker = 193,

        [Description("Special_Zerg_Beacon")]
        SpecialZergBeacon = 194,

        [Description("Special_Terran_Beacon")]
        SpecialTerranBeacon = 195,

        [Description("Special_Protoss_Beacon")]
        SpecialProtossBeacon = 196,

        [Description("Special_Zerg_Flag_Beacon")]
        SpecialZergFlagBeacon = 197,

        [Description("Special_Terran_Flag_Beacon")]
        SpecialTerranFlagBeacon = 198,

        [Description("Special_Protoss_Flag_Beacon")]
        SpecialProtossFlagBeacon = 199,

        [Description("Special_Power_Generator")]
        SpecialPowerGenerator = 200,

        [Description("Special_Overmind_Cocoon")]
        SpecialOvermindCocoon = 201,

        [Description("Spell_Dark_Swarm")]
        SpellDarkSwarm = 202,

        [Description("Special_Floor_Missile_Trap")]
        SpecialFloorMissileTrap = 203,

        [Description("Special_Floor_Hatch")]
        SpecialFloorHatch = 204,

        [Description("Special_Upper_Level_Door")]
        SpecialUpperLevelDoor = 205,

        [Description("Special_Right_Upper_Level_Door")]
        SpecialRightUpperLevelDoor = 206,

        [Description("Special_Pit_Door")]
        SpecialPitDoor = 207,

        [Description("Special_Right_Pit_Door")]
        SpecialRightPitDoor = 208,

        [Description("Special_Floor_Gun_Trap")]
        SpecialFloorGunTrap = 209,

        [Description("Special_Wall_Missile_Trap")]
        SpecialWallMissileTrap = 210,

        [Description("Special_Wall_Flame_Trap")]
        SpecialWallFlameTrap = 211,

        [Description("Special_Right_Wall_Missile_Trap")]
        SpecialRightWallMissileTrap = 212,

        [Description("Special_Right_Wall_Flame_Trap")]
        SpecialRightWallFlameTrap = 213,

        [Description("Special_Start_Location")]
        SpecialStartLocation = 214,

        [Description("Powerup_Flag")]
        PowerupFlag = 215,

        [Description("Powerup_Young_Chrysalis")]
        PowerupYoungChrysalis = 216,

        [Description("Powerup_Psi_Emitter")]
        PowerupPsiEmitter = 217,

        [Description("Powerup_Data_Disk")]
        PowerupDataDisk = 218,

        [Description("Powerup_Khaydarin_Crystal")]
        PowerupKhaydarinCrystal = 219,

        [Description("Powerup_Mineral_Cluster_Type_1")]
        PowerupMineralClusterType1 = 220,

        [Description("Powerup_Mineral_Cluster_Type_2")]
        PowerupMineralClusterType2 = 221,

        [Description("Powerup_Protoss_Gas_Orb_Type_1")]
        PowerupProtossGasOrbType1 = 222,

        [Description("Powerup_Protoss_Gas_Orb_Type_2")]
        PowerupProtossGasOrbType2 = 223,

        [Description("Powerup_Zerg_Gas_Sac_Type_1")]
        PowerupZergGasSacType1 = 224,

        [Description("Powerup_Zerg_Gas_Sac_Type_2")]
        PowerupZergGasSacType2 = 225,

        [Description("Powerup_Terran_Gas_Tank_Type_1")]
        PowerupTerranGasTankType1 = 226,

        [Description("Powerup_Terran_Gas_Tank_Type_2")]
        PowerupTerranGasTankType2 = 227,

        // Meta Types (228-234)
        [Description("None")]
        None = 228,

        [Description("AllUnits")]
        AllUnits = 229,

        [Description("Men")]
        Men = 230,

        [Description("Buildings")]
        Buildings = 231,

        [Description("Factories")]
        Factories = 232,

        [Description("Unknown")]
        Unknown = 233,

        [Description("MAX")]
        MAX = 234
    }

    public enum UnitCategory
    {
        Unit,
        Building,
        Hero,
        Critter,
        Spell,
        Powerup,
        Special,
        Unused,
        Resource
    }

    public static class UnitTypeExtensions
    {
        private static readonly Dictionary<UnitType, string> _descriptions;
        private static readonly Dictionary<string, UnitType> _nameToEnum;

        static UnitTypeExtensions()
        {
            _descriptions = new Dictionary<UnitType, string>();
            _nameToEnum = new Dictionary<string, UnitType>(StringComparer.OrdinalIgnoreCase);

            foreach (UnitType unitType in Enum.GetValues<UnitType>())
            {
                var field = typeof(UnitType).GetField(unitType.ToString());
                var description = field?.GetCustomAttribute<DescriptionAttribute>()?.Description ?? unitType.ToString();

                _descriptions[unitType] = description;
                _nameToEnum[description] = unitType;
            }
        }

        public static string GetUnitName(this UnitType unitType)
        {
            return _descriptions.TryGetValue(unitType, out var name) ? name : unitType.ToString();
        }

        public static int GetId(this UnitType unitType)
        {
            return (int)unitType;
        }

        public static UnitType? ParseFromName(string name)
        {
            return _nameToEnum.TryGetValue(name, out var unitType) ? unitType : null;
        }

        public static string GetRace(this UnitType unitType)
        {
            var name = unitType.GetUnitName();

            if (name.StartsWith("Terran_") || name.StartsWith("Hero_") && IsTerranHero(name))
                return "Terran";
            else if (name.StartsWith("Protoss_") || name.StartsWith("Hero_") && IsProtossHero(name))
                return "Protoss";
            else if (name.StartsWith("Zerg_") || name.StartsWith("Hero_") && IsZergHero(name))
                return "Zerg";
            else
                return "Neutral";
        }

        private static bool IsTerranHero(string name)
        {
            var terranHeroes = new[] { "Sarah_Kerrigan", "Jim_Raynor", "Edmund_Duke", "Arcturus_Mengsk",
                                     "Tom_Kazansky", "Magellan", "Gui_Montag", "Alan_Schezar", "Hyperion",
                                     "Norad_II", "Gerard_DuGalle", "Alexei_Stukov" };
            return terranHeroes.Any(hero => name.Contains(hero));
        }

        private static bool IsProtossHero(string name)
        {
            var protossHeroes = new[] { "Dark_Templar", "Zeratul", "Tassadar", "Fenix", "Mojo",
                                       "Warbringer", "Gantrithor", "Danimoth", "Aldaris",
                                       "Artanis", "Raszagal" };
            return protossHeroes.Any(hero => name.Contains(hero));
        }

        private static bool IsZergHero(string name)
        {
            var zergHeroes = new[] { "Torrasque", "Matriarch", "Infested_Kerrigan", "Unclean_One",
                                    "Hunter_Killer", "Devouring_One", "Kukulza", "Yggdrasill",
                                    "Infested_Duran", "Samir_Duran" };
            return zergHeroes.Any(hero => name.Contains(hero));
        }

        public static bool IsBuilding(this UnitType unitType)
        {
            var id = unitType.GetId();
            return id >= 106 && id <= 175;
        }

        public static bool IsHero(this UnitType unitType)
        {
            return unitType.GetUnitName().StartsWith("Hero_");
        }

        public static bool IsWorker(this UnitType unitType)
        {
            return unitType == UnitType.TerranSCV ||
                   unitType == UnitType.ProtossProbe ||
                   unitType == UnitType.ZergDrone;
        }

        public static bool IsCritter(this UnitType unitType)
        {
            return unitType.GetUnitName().StartsWith("Critter_");
        }

        public static bool IsSpell(this UnitType unitType)
        {
            return unitType.GetUnitName().StartsWith("Spell_");
        }

        public static bool IsPowerup(this UnitType unitType)
        {
            return unitType.GetUnitName().StartsWith("Powerup_");
        }

        public static bool IsSpecial(this UnitType unitType)
        {
            return unitType.GetUnitName().StartsWith("Special_");
        }

        public static bool IsUnused(this UnitType unitType)
        {
            return unitType.GetUnitName().StartsWith("Unused_");
        }

        public static bool IsResource(this UnitType unitType)
        {
            return unitType.GetUnitName().StartsWith("Resource_");
        }

        public static bool IsTurret(this UnitType unitType)
        {
            return unitType.GetUnitName().Contains("Turret");
        }

        public static UnitCategory GetCategory(this UnitType unitType)
        {
            if (unitType.IsBuilding()) return UnitCategory.Building;
            if (unitType.IsHero()) return UnitCategory.Hero;
            if (unitType.IsCritter()) return UnitCategory.Critter;
            if (unitType.IsSpell()) return UnitCategory.Spell;
            if (unitType.IsPowerup()) return UnitCategory.Powerup;
            if (unitType.IsSpecial()) return UnitCategory.Special;
            if (unitType.IsUnused()) return UnitCategory.Unused;
            if (unitType.IsResource()) return UnitCategory.Resource;
            return UnitCategory.Unit;
        }

        public static bool IsSameRace(this UnitType unit1, UnitType unit2)
        {
            return unit1.GetRace() == unit2.GetRace();
        }

        public static IEnumerable<UnitType> GetAllByRace(string race)
        {
            return Enum.GetValues<UnitType>().Where(ut => ut.GetRace().Equals(race, StringComparison.OrdinalIgnoreCase));
        }

        public static IEnumerable<UnitType> GetAllByCategory(UnitCategory category)
        {
            return Enum.GetValues<UnitType>().Where(ut => ut.GetCategory() == category);
        }

        public static IEnumerable<UnitType> GetAllBuildings()
        {
            return Enum.GetValues<UnitType>().Where(ut => ut.IsBuilding());
        }

        public static IEnumerable<UnitType> GetAllUnits()
        {
            return Enum.GetValues<UnitType>().Where(ut => !ut.IsBuilding() && !ut.IsSpecial() && !ut.IsUnused() && !ut.IsResource());
        }

        public static IEnumerable<UnitType> GetAllHeroes()
        {
            return Enum.GetValues<UnitType>().Where(ut => ut.IsHero());
        }

        public static IEnumerable<UnitType> GetAllWorkers()
        {
            return Enum.GetValues<UnitType>().Where(ut => ut.IsWorker());
        }
    }
}