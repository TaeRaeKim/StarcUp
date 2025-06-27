
using System;
using System.Diagnostics;
using Xunit;
using Shouldly;
using StarcUp.Business.Units.StaticData.Repositories;
using StarcUp.Business.Units.Types;

namespace StarcUp.Test.Src.Business.Units.StaticData.Repositories
{
    public class UnitInfoRepositoryTest
    {
        private readonly UnitInfoRepository _unitInfoRepository;
        public UnitInfoRepositoryTest()
        {
            _unitInfoRepository = new UnitInfoRepository();
        }

        [Fact]
        public void AllUnits_ShouldReturn205Units_WhenLoaded()
        {
            // Act & Assert
            _unitInfoRepository.AllUnits.Count.ShouldBe(205);
        }

        [Theory]
        [InlineData(UnitType.TerranMarine, UnitType.ZergZergling, UnitType.ProtossZealot)]
        public void GetByName_ShouldReturnCorrectUnits_WhenValidUnitNames(UnitType marineType, UnitType zerglingType, UnitType zealotType)
        {
            // Act
            var marine = _unitInfoRepository.GetByName(marineType.GetUnitName());
            var zergling = _unitInfoRepository.GetByName(zerglingType.GetUnitName());
            var zealot = _unitInfoRepository.GetByName(zealotType.GetUnitName());

            // Assert
            marine.ShouldNotBeNull();
            zergling.ShouldNotBeNull();
            zealot.ShouldNotBeNull();
            zealot.HP.ShouldBeGreaterThan(marine.HP);
            marine.HP.ShouldBeGreaterThan(zergling.HP);
        }

        [Fact]
        public void GetById_ShouldReturnCorrectUnit_WhenValidId()
        {
            // Act
            var unit = _unitInfoRepository.GetById(194);
            
            // Assert
            unit.ShouldNotBeNull();
            Console.WriteLine($"Unit ID 194: {unit.DisplayName}");
        }

        [Fact]
        public void Get_ShouldReturnCorrectUnit_WhenValidUnitType()
        {
            // Act
            var marine = _unitInfoRepository.Get(UnitType.TerranMarine);
            
            // Assert
            marine.ShouldNotBeNull();
            marine.Name.ShouldBe("Terran_Marine");
        }

        [Fact]
        public void GetByRace_ShouldReturnOnlyTerranUnits_WhenTerranRace()
        {
            // Act
            var terranUnits = _unitInfoRepository.GetByRace("Terran");
            
            // Assert
            terranUnits.ShouldNotBeEmpty();
            terranUnits.ShouldAllBe(unit => unit.Race == "Terran");
        }

        [Fact]
        public void Workers_ShouldReturnOnlyWorkerUnits_WhenCalled()
        {
            // Act
            var workers = _unitInfoRepository.Workers;
            
            // Assert
            workers.ShouldNotBeEmpty();
            workers.ShouldAllBe(unit => unit.IsWorker);
        }

        [Fact]
        public void Buildings_ShouldReturnOnlyBuildingUnits_WhenCalled()
        {
            // Act
            var buildings = _unitInfoRepository.Buildings;
            
            // Assert
            buildings.ShouldNotBeEmpty();
            buildings.ShouldAllBe(unit => unit.IsBuilding);
        }

        [Fact]
        public void CombatUnits_ShouldReturnOnlyCombatUnits_WhenCalled()
        {
            // Act
            var combatUnits = _unitInfoRepository.CombatUnits;
            
            // Assert
            combatUnits.ShouldNotBeEmpty();
            combatUnits.ShouldAllBe(unit => unit.IsCombatUnit);
        }

        [Fact]
        public void GetCheapestUnits_ShouldReturnUnitsUnderCost_WhenMaxCostSpecified()
        {
            // Arrange
            int maxCost = 100;
            
            // Act
            var cheapUnits = _unitInfoRepository.GetCheapestUnits(maxCost);
            
            // Assert
            cheapUnits.ShouldNotBeEmpty();
            cheapUnits.ShouldAllBe(unit => unit.TotalCost <= maxCost);
        }
    }
}
