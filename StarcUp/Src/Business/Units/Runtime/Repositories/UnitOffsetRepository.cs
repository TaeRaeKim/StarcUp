using System.Text.Json;
using StarcUp.Business.Units.Runtime.Models;
using StarcUp.Business.Units.Types;

namespace StarcUp.Business.Units.Runtime.Repositories
{
    public class UnitOffsetRepository
    {
        private readonly string _dataPath;
        private UnitOffsetConfigDto? _cachedConfig;
        private Dictionary<UnitType, int>? _cachedOffsets;

        public UnitOffsetRepository(string dataPath = "Data")
        {
            _dataPath = dataPath;
        }

        public UnitOffsetConfigDto LoadConfig()
        {
            if (_cachedConfig != null)
                return _cachedConfig;

            var filePath = Path.Combine(_dataPath, "all_race_unit_offsets.json");
            
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"유닛 오프셋 파일을 찾을 수 없습니다: {filePath}");

            var jsonContent = File.ReadAllText(filePath);
            _cachedConfig = JsonSerializer.Deserialize<UnitOffsetConfigDto>(jsonContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return _cachedConfig ?? throw new InvalidOperationException("JSON 파일 파싱에 실패했습니다.");
        }

        public Dictionary<UnitType, int> GetAllCompletedOffsets()
        {
            if (_cachedOffsets != null)
                return _cachedOffsets;

            var config = LoadConfig();
            _cachedOffsets = new Dictionary<UnitType, int>();

            // 모든 종족의 유닛과 건물을 합쳐서 Dictionary 생성
            var allUnits = config.Races.Terran.Units
                .Concat(config.Races.Terran.Buildings)
                .Concat(config.Races.Zerg.Units)
                .Concat(config.Races.Zerg.Buildings)
                .Concat(config.Races.Protoss.Units)
                .Concat(config.Races.Protoss.Buildings);

            foreach (var unit in allUnits)
            {
                if (Enum.TryParse<UnitType>(unit.UnitType, out var unitType))
                {
                    _cachedOffsets[unitType] = unit.CompletedOffset;
                }
            }

            return _cachedOffsets;
        }

        public int GetCompletedOffset(UnitType unitType)
        {
            var offsets = GetAllCompletedOffsets();
            
            if (!offsets.TryGetValue(unitType, out var offset))
                throw new ArgumentException($"해당 유닛 타입의 오프셋을 찾을 수 없습니다: {unitType}");

            return offset;
        }

        public int GetProductionOffset(UnitType unitType)
        {
            var config = LoadConfig();
            var completedOffset = GetCompletedOffset(unitType);
            return completedOffset + config.ProductionOffset;
        }

        public int GetPlayerOffset(UnitType unitType, byte playerIndex, bool includeProduction = false)
        {
            var baseOffset = includeProduction ? GetProductionOffset(unitType) : GetCompletedOffset(unitType);
            return baseOffset + (playerIndex * 4);
        }

        public int GetBaseOffset()
        {
            var config = LoadConfig();
            return config.BaseOffset;
        }

        public int GetBufferSize()
        {
            var config = LoadConfig();
            return config.BufferInfo.BufferSize;
        }

        public int GetMinBufferOffset()
        {
            var config = LoadConfig();
            return config.BufferInfo.MinOffset;
        }

        public int GetBufferOffset(int absoluteOffset)
        {
            var minOffset = GetMinBufferOffset();
            return absoluteOffset - minOffset;
        }

        public void ClearCache()
        {
            _cachedConfig = null;
            _cachedOffsets = null;
        }
    }
}