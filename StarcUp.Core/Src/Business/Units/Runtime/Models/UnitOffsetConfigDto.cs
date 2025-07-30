namespace StarcUp.Business.Units.Runtime.Models
{
    public class UnitOffsetDto
    {
        public string UnitType { get; set; } = string.Empty;
        public int UnitId { get; set; }
        public string Name { get; set; } = string.Empty;
        public int CompletedOffset { get; set; }
    }

    public class RaceUnitsDto
    {
        public List<UnitOffsetDto> Units { get; set; } = new();
        public List<UnitOffsetDto> Buildings { get; set; } = new();
    }

    public class BufferInfoDto
    {
        public int MinOffset { get; set; }
        public int MaxOffset { get; set; }
        public int BufferSize { get; set; }
        public string Comment { get; set; } = string.Empty;
    }

    public class RacesDto
    {
        public RaceUnitsDto Terran { get; set; } = new();
        public RaceUnitsDto Zerg { get; set; } = new();
        public RaceUnitsDto Protoss { get; set; } = new();
    }

    public class RacePopulationDto
    {
        public int SupplyUsed { get; set; }
        public int SupplyMax { get; set; }
    }

    public class PopulationOffsetsDto
    {
        public string Comment { get; set; } = string.Empty;
        public RacePopulationDto Terran { get; set; } = new();
        public RacePopulationDto Zerg { get; set; } = new();
        public RacePopulationDto Protoss { get; set; } = new();
    }

    public class UnitOffsetConfigDto
    {
        public int BaseOffset { get; set; }
        public int ProductionOffset { get; set; }
        public int MapNameOffset { get; set; }
        public string Comment { get; set; } = string.Empty;
        public PopulationOffsetsDto PopulationOffsets { get; set; } = new();
        public BufferInfoDto BufferInfo { get; set; } = new();
        public RacesDto Races { get; set; } = new();
    }
}