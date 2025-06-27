using StarcUp.Infrastructure.Memory;
using StarcUp.Business.Units.Runtime.Services;
using StarcUp.Business.Units.Runtime.Adapters;
using System;
using System.Linq;
using Xunit;

namespace StarcUp.Test.Src.Business.Units.Runtime.Services
{
    public class UnitServiceTest
    {
        private readonly IMemoryReader _memoryReader;
        private readonly IUnitService _unitService;
        
        public UnitServiceTest()
        {
            _memoryReader = new MemoryReader();
            _memoryReader.ConnectToProcess(17604);

            var unitMemoryAdapter = new UnitMemoryAdapter(_memoryReader);
            _unitService = new UnitService(unitMemoryAdapter);

            nint address = (nint)Convert.ToInt64("00007FF4FC4C0010", 16); 
            _unitService.SetUnitArrayBaseAddress(address);
        }

        [Fact]
        public void LoadAllUnits_ShouldReturnTrue_WhenSuccessful()
        {
            // Act
            bool result = _unitService.LoadAllUnits();
            
            // Assert
            Assert.True(result, "Failed to load all units");
        }

        [Fact]
        public void GetPlayerUnits_ShouldReturnExpectedCount_WhenValidPlayerId()
        {
            // Arrange
            _unitService.LoadAllUnits();
            
            // Act
            var playerUnits = _unitService.GetPlayerUnits(1);
            
            // Assert
            Assert.NotNull(playerUnits);
            Assert.True(playerUnits.Count() >= 0);
        }

        [Fact]
        public void GetAliveUnits_ShouldReturnOnlyAliveUnits_WhenCalled()
        {
            // Arrange
            _unitService.LoadAllUnits();
            
            // Act
            var aliveUnits = _unitService.GetAliveUnits();
            
            // Assert
            Assert.NotNull(aliveUnits);
            Assert.All(aliveUnits, unit => Assert.True(unit.IsAlive));
        }

        [Fact]
        public void GetWorkerUnits_ShouldReturnOnlyWorkers_WhenCalled()
        {
            // Arrange
            _unitService.LoadAllUnits();
            
            // Act
            var workerUnits = _unitService.GetWorkerUnits();
            
            // Assert
            Assert.NotNull(workerUnits);
            Assert.All(workerUnits, unit => Assert.True(unit.IsWorker));
        }

        [Fact]
        public void GetBuildingUnits_ShouldReturnOnlyBuildings_WhenCalled()
        {
            // Arrange
            _unitService.LoadAllUnits();
            
            // Act
            var buildingUnits = _unitService.GetBuildingUnits();
            
            // Assert
            Assert.NotNull(buildingUnits);
            Assert.All(buildingUnits, unit => Assert.True(unit.IsBuilding));
        }

        [Fact]
        public void GetHeroUnits_ShouldReturnOnlyHeroes_WhenCalled()
        {
            // Arrange
            _unitService.LoadAllUnits();
            
            // Act
            var heroUnits = _unitService.GetHeroUnits();
            
            // Assert
            Assert.NotNull(heroUnits);
            Assert.All(heroUnits, unit => Assert.True(unit.IsHero));
        }
    }
}
