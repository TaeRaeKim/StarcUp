namespace StarcUp.Business.Units.Runtime.Models
{
    public class UnitCountDto
    {
        public string UnitType { get; set; } = string.Empty;
        public int UnitId { get; set; }
        public string Name { get; set; } = string.Empty;
        public int CompletedOffset { get; set; }
        public string Description { get; set; } = string.Empty;
    }

    public class UnitCountConfigDto
    {
        public int BaseOffset { get; set; }
        public int ProductionOffset { get; set; }
        public string Comment { get; set; } = string.Empty;
        public List<UnitCountDto> Units { get; set; } = new();
    }
}