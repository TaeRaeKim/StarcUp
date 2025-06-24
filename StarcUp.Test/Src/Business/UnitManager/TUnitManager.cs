using StarcUp.Infrastructure.Memory;
using StarcUp.Src.Business.UnitManager;
using Xunit;

namespace StarcUp.Test.Src.Business.TUnitManager
{
    public class TUnitManager
    {
        private readonly IUnitManager _unitManager;
        private readonly IMemoryReader memoryReader;
        public TUnitManager()
        {
            memoryReader = new MemoryReader();
            memoryReader.ConnectToProcess(17604);
            _unitManager = new UnitManager(memoryReader);

            nint address = (nint)Convert.ToInt64("00007FF4FC4C0010", 16); 
            _unitManager.SetUnitArrayBaseAddress(address);
        }

        [Fact]
        public void LoadAllUnitTest()
        {
            bool result = _unitManager.LoadAllUnits();
            Assert.True(result, "Failed to load all units");
            var myUnits = _unitManager.GetPlayerUnits(1);

            Console.WriteLine(_unitManager.CountActiveUnits());
            Assert.Equal(5, myUnits.Count());
        }
    }
}
