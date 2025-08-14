using System.Text.Json;
using StarcUp.Business.Units.Runtime.Models;
using StarcUp.Business.Units.Types;

namespace StarcUp.Infrastructure.Memory
{
    /// <summary>
    /// 게임 메모리 오프셋 저장소 구현
    /// </summary>
    public class GameOffsetRepository : IGameOffsetRepository
    {
        private readonly string _dataPath;
        private GameOffsetConfigDto? _cachedConfig;
        private Dictionary<UnitType, int>? _cachedUnitOffsets;

        public GameOffsetRepository(string dataPath = "Data")
        {
            _dataPath = dataPath;
        }

        public GameOffsetConfigDto LoadConfig()
        {
            if (_cachedConfig != null)
                return _cachedConfig;

            var filePath = Path.Combine(_dataPath, "offsets.json");
            
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"오프셋 파일을 찾을 수 없습니다: {filePath}");

            var jsonContent = File.ReadAllText(filePath);
            _cachedConfig = JsonSerializer.Deserialize<GameOffsetConfigDto>(jsonContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return _cachedConfig ?? throw new InvalidOperationException("JSON 파일 파싱에 실패했습니다.");
        }

        // 기본 오프셋
        public int GetBaseOffset()
        {
            var config = LoadConfig();
            return config.BaseOffset;
        }

        public int GetMapNameOffset()
        {
            var config = LoadConfig();
            return config.MapNameOffset;
        }

        public int GetProductionOffset()
        {
            var config = LoadConfig();
            return config.ProductionOffset;
        }

        // 유닛 관련 오프셋
        public int GetUnitCompletedOffset(UnitType unitType)
        {
            var offsets = GetAllUnitCompletedOffsets();
            
            if (!offsets.TryGetValue(unitType, out var offset))
                throw new ArgumentException($"해당 유닛 타입의 오프셋을 찾을 수 없습니다: {unitType}");

            return offset;
        }

        public int GetUnitProductionOffset(UnitType unitType)
        {
            var config = LoadConfig();
            var completedOffset = GetUnitCompletedOffset(unitType);
            return completedOffset - config.ProductionOffset;
        }

        public int GetUnitPlayerOffset(UnitType unitType, byte playerIndex, bool includeProduction = false)
        {
            var baseOffset = includeProduction ? GetUnitProductionOffset(unitType) : GetUnitCompletedOffset(unitType);
            return baseOffset + (playerIndex * 4);
        }

        public Dictionary<UnitType, int> GetAllUnitCompletedOffsets()
        {
            if (_cachedUnitOffsets != null)
                return _cachedUnitOffsets;

            var config = LoadConfig();
            _cachedUnitOffsets = new Dictionary<UnitType, int>();

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
                    _cachedUnitOffsets[unitType] = unit.CompletedOffset;
                }
            }

            return _cachedUnitOffsets;
        }

        // 인구수 관련 오프셋
        public int GetTerranSupplyUsedOffset()
        {
            var config = LoadConfig();
            return config.PopulationOffsets.Terran.SupplyUsed;
        }

        public int GetTerranSupplyMaxOffset()
        {
            var config = LoadConfig();
            return config.PopulationOffsets.Terran.SupplyMax;
        }

        public int GetZergSupplyUsedOffset()
        {
            var config = LoadConfig();
            return config.PopulationOffsets.Zerg.SupplyUsed;
        }

        public int GetZergSupplyMaxOffset()
        {
            var config = LoadConfig();
            return config.PopulationOffsets.Zerg.SupplyMax;
        }

        public int GetProtossSupplyUsedOffset()
        {
            var config = LoadConfig();
            return config.PopulationOffsets.Protoss.SupplyUsed;
        }

        public int GetProtossSupplyMaxOffset()
        {
            var config = LoadConfig();
            return config.PopulationOffsets.Protoss.SupplyMax;
        }

        // 업그레이드 관련 오프셋
        public int GetUpgradeOffset1()
        {
            var config = LoadConfig();
            return config.UpgradeOffsets.Upgrades.Section1.Offset;
        }

        public int GetUpgradeOffset2()
        {
            var config = LoadConfig();
            return config.UpgradeOffsets.Upgrades.Section2.Offset;
        }

        public int GetTechOffset1()
        {
            var config = LoadConfig();
            return config.UpgradeOffsets.Techs.Section1.Offset;
        }

        public int GetTechOffset2()
        {
            var config = LoadConfig();
            return config.UpgradeOffsets.Techs.Section2.Offset;
        }

        // 버퍼 관련
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

        // 캐시 관리
        public void ClearCache()
        {
            _cachedConfig = null;
            _cachedUnitOffsets = null;
        }
    }
}