using StarcUp.Business.Units.StaticData.Models;
using StarcUp.Business.Units.Types;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace StarcUp.Business.Units.StaticData.Repositories
{
    public class UnitInfoRepository : IUnitInfoRepository
    {
        private static readonly string JsonFileName = "starcraft_units.json";

        private readonly Dictionary<int, UnitInfo> _unitsById;
        private readonly Dictionary<string, UnitInfo> _unitsByName;
        private readonly IReadOnlyList<UnitInfo> _allUnits;

        public UnitInfoRepository()
        {
            _allUnits = LoadUnitsFromJson(JsonFileName).AsReadOnly();
            _unitsById = _allUnits.ToDictionary(u => u.Id);
            _unitsByName = _allUnits.ToDictionary(u => u.Name, StringComparer.OrdinalIgnoreCase);
        }

        private static List<UnitInfo> LoadUnitsFromJson(string fileName)
        {
            try
            {
                var possiblePaths = new[]
                {
                    fileName,
                    Path.Combine("data", fileName),
                    Path.Combine("..", "data", fileName),
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, fileName)
                };

                string jsonPath = possiblePaths.FirstOrDefault(File.Exists) ?? "";

                if (string.IsNullOrEmpty(jsonPath))
                {
                    throw new FileNotFoundException("starcraft_units.json not found");
                }

                var jsonString = File.ReadAllText(jsonPath);
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    ReadCommentHandling = JsonCommentHandling.Skip
                };

                var container = JsonSerializer.Deserialize<UnitsContainer>(jsonString, options);
                return container?.units?.Select(dto => dto.ToUnitInfo()).ToList() ?? new List<UnitInfo>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading units: {ex.Message}");
                return new List<UnitInfo>();
            }
        }

        public UnitInfo? Get(UnitType unitType) => _unitsById.TryGetValue(unitType.GetId(), out var unit) ? unit : null;
        public UnitInfo? GetById(int id) => _unitsById.TryGetValue(id, out var unit) ? unit : null;
        public UnitInfo? GetByName(string name) => _unitsByName.TryGetValue(name, out var unit) ? unit : null;
        public bool TryGetById(int id, out UnitInfo? unit) => _unitsById.TryGetValue(id, out unit);
        public bool TryGetByName(string name, out UnitInfo? unit) => _unitsByName.TryGetValue(name, out unit);

        public IReadOnlyList<UnitInfo> AllUnits => _allUnits;
        public IEnumerable<UnitInfo> GetByRace(string race) => _allUnits.Where(u => u.Race.Equals(race, StringComparison.OrdinalIgnoreCase));
        public IEnumerable<UnitInfo> Buildings => _allUnits.Where(u => u.IsBuilding);
        public IEnumerable<UnitInfo> Units => _allUnits.Where(u => !u.IsBuilding);
        public IEnumerable<UnitInfo> CombatUnits => _allUnits.Where(u => u.IsCombatUnit);
        public IEnumerable<UnitInfo> Workers => _allUnits.Where(u => u.IsWorker);
        public IEnumerable<UnitInfo> Heroes => _allUnits.Where(u => u.IsHero);

        public int TotalCount => _allUnits.Count;
        public int BuildingCount => Buildings.Count();
        public int UnitCount => Units.Count();

        public IEnumerable<UnitInfo> GetCheapestUnits(int maxCost) =>
            _allUnits.Where(u => u.TotalCost <= maxCost && !u.IsBuilding).OrderBy(u => u.TotalCost);

        public IEnumerable<UnitInfo> GetStrongestUnits(int minHP) =>
            _allUnits.Where(u => u.TotalHitPoints >= minHP).OrderByDescending(u => u.TotalHitPoints);

        public UnitInfo? GetBestValueUnit() =>
            CombatUnits.Where(u => u.MineralCost > 0).OrderByDescending(u => u.HPPerMineral).FirstOrDefault();
    }
}