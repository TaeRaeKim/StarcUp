using StarcUp.Infrastructure.Memory;
using StarcUp.Src.Business.UnitManager;
using StarcUp.Src.Infrastructure.Memory;
using Xunit;

namespace StarcUp.Test.Src.Business.TUnitManager
{
    public class TUnitManager
    {
        private readonly IMemoryReader _memoryReader;
        private readonly IUnitManager _unitManager;
        private readonly IOptimizedMemoryReader _optiMemoryReader;
        private readonly UnitManager _optimizedUnitManager;
        public TUnitManager()
        {
            _memoryReader = new MemoryReader();
            _optiMemoryReader = new OptimizedMemoryReader();

            _memoryReader.ConnectToProcess(17604);
            _optiMemoryReader.ConnectToProcess(17604);

            _unitManager = new UnitManager(_memoryReader);
            _optimizedUnitManager = new UnitManager(_optiMemoryReader);

            nint address = (nint)Convert.ToInt64("00007FF4FC4C0010", 16); 
            _unitManager.SetUnitArrayBaseAddress(address);
            _optimizedUnitManager.SetUnitArrayBaseAddress(address);
        }

        [Fact]
        public void LoadAllUnitTest()
        {
            bool result = _unitManager.LoadAllUnits();
            Assert.True(result, "Failed to load all units");
            var myUnits = _unitManager.GetPlayerUnits(1);
            Assert.Equal(5, myUnits.Count());
        }

        [Fact]
        public void LoadAllUnitTest2()
        {
            bool result = _optimizedUnitManager.LoadAllUnits();
            Assert.True(result, "Failed to load all units");
            var myUnits = _optimizedUnitManager.GetPlayerUnits(1);
            Assert.Equal(5, myUnits.Count());
        }
    }
}
