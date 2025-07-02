using System.Runtime.InteropServices;

namespace StarcUp.Business.Units.Runtime.Models
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct UnitCountRaw
    {
        public int CompletedCount;
        public int ProductionCount;
        
        public UnitCountRaw(int completedCount, int productionCount)
        {
            CompletedCount = completedCount;
            ProductionCount = productionCount;
        }
    }
}