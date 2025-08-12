using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text.Json.Serialization;

namespace StarcUp.Business.Units.Types
{

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

        public static bool IsGasBuilding(this UnitType unitType)
        {
            return unitType == UnitType.TerranRefinery ||
                   unitType == UnitType.ZergExtractor ||
                   unitType == UnitType.ProtossAssimilator;
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