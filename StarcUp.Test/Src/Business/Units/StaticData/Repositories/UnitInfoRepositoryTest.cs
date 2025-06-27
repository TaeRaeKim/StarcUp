
using System;
using System.Linq;
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

        #region Basic Get Methods Tests

        [Fact]
        public void Get_ShouldReturnCorrectUnit_WhenValidUnitType()
        {
            var marine = _unitInfoRepository.Get(UnitType.TerranMarine);
            
            marine.ShouldNotBeNull();
            marine.Name.ShouldBe("Terran_Marine");
            marine.Race.ShouldBe("Terran");
        }

        [Fact]
        public void Get_ShouldReturnNull_WhenInvalidUnitType()
        {
            var result = _unitInfoRepository.Get((UnitType)999);
            
            result.ShouldBeNull();
        }

        [Fact]
        public void GetById_ShouldReturnCorrectUnit_WhenValidId()
        {
            var unit = _unitInfoRepository.GetById(0);
            
            unit.ShouldNotBeNull();
            unit.Name.ShouldBe("Terran_Marine");
        }

        [Fact]
        public void GetById_ShouldReturnNull_WhenInvalidId()
        {
            var unit = _unitInfoRepository.GetById(9999);
            
            unit.ShouldBeNull();
        }

        [Theory]
        [InlineData("Terran_Marine")]
        [InlineData("terran_marine")]
        [InlineData("TERRAN_MARINE")]
        public void GetByName_ShouldReturnCorrectUnit_WhenValidNameWithDifferentCasing(string unitName)
        {
            var unit = _unitInfoRepository.GetByName(unitName);
            
            unit.ShouldNotBeNull();
            unit.Name.ShouldBe("Terran_Marine");
        }

        [Fact]
        public void GetByName_ShouldReturnNull_WhenInvalidName()
        {
            var unit = _unitInfoRepository.GetByName("NonExistentUnit");
            
            unit.ShouldBeNull();
        }

        [Fact]
        public void TryGetById_ShouldReturnTrue_WhenValidId()
        {
            var success = _unitInfoRepository.TryGetById(0, out var unit);
            
            success.ShouldBeTrue();
            unit.ShouldNotBeNull();
            unit.Name.ShouldBe("Terran_Marine");
        }

        [Fact]
        public void TryGetById_ShouldReturnFalse_WhenInvalidId()
        {
            var success = _unitInfoRepository.TryGetById(9999, out var unit);
            
            success.ShouldBeFalse();
            unit.ShouldBeNull();
        }

        [Fact]
        public void TryGetByName_ShouldReturnTrue_WhenValidName()
        {
            var success = _unitInfoRepository.TryGetByName("Terran_Marine", out var unit);
            
            success.ShouldBeTrue();
            unit.ShouldNotBeNull();
            unit.Name.ShouldBe("Terran_Marine");
        }

        [Fact]
        public void TryGetByName_ShouldReturnFalse_WhenInvalidName()
        {
            var success = _unitInfoRepository.TryGetByName("NonExistentUnit", out var unit);
            
            success.ShouldBeFalse();
            unit.ShouldBeNull();
        }

        #endregion

        #region Collection Properties Tests

        [Fact]
        public void AllUnits_ShouldReturnAllLoadedUnits_WhenCalled()
        {
            var allUnits = _unitInfoRepository.AllUnits;
            
            allUnits.ShouldNotBeEmpty();
            allUnits.Count.ShouldBeGreaterThan(200);
        }

        [Theory]
        [InlineData("Terran")]
        [InlineData("Protoss")]
        [InlineData("Zerg")]
        public void GetByRace_ShouldReturnOnlyUnitsOfSpecifiedRace_WhenValidRace(string race)
        {
            var units = _unitInfoRepository.GetByRace(race);
            
            units.ShouldNotBeEmpty();
            units.ShouldAllBe(unit => unit.Race.Equals(race, StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public void GetByRace_ShouldReturnEmpty_WhenInvalidRace()
        {
            var units = _unitInfoRepository.GetByRace("InvalidRace");
            
            units.ShouldBeEmpty();
        }

        [Fact]
        public void Buildings_ShouldReturnOnlyBuildingUnits_WhenCalled()
        {
            var buildings = _unitInfoRepository.Buildings;
            
            buildings.ShouldNotBeEmpty();
            buildings.ShouldAllBe(unit => unit.IsBuilding);
        }

        [Fact]
        public void Units_ShouldReturnOnlyNonBuildingUnits_WhenCalled()
        {
            var units = _unitInfoRepository.Units;
            
            units.ShouldNotBeEmpty();
            units.ShouldAllBe(unit => !unit.IsBuilding);
        }

        [Fact]
        public void CombatUnits_ShouldReturnOnlyCombatUnits_WhenCalled()
        {
            var combatUnits = _unitInfoRepository.CombatUnits;
            
            combatUnits.ShouldNotBeEmpty();
            combatUnits.ShouldAllBe(unit => unit.IsCombatUnit);
        }

        [Fact]
        public void Workers_ShouldReturnOnlyWorkerUnits_WhenCalled()
        {
            var workers = _unitInfoRepository.Workers;
            
            workers.ShouldNotBeEmpty();
            workers.ShouldAllBe(unit => unit.IsWorker);
            workers.Count().ShouldBe(3);
        }

        [Fact]
        public void Heroes_ShouldReturnOnlyHeroUnits_WhenCalled()
        {
            var heroes = _unitInfoRepository.Heroes;
            
            heroes.ShouldNotBeEmpty();
            heroes.ShouldAllBe(unit => unit.IsHero);
        }

        #endregion

        #region Count Properties Tests

        [Fact]
        public void TotalCount_ShouldReturnCorrectCount_WhenCalled()
        {
            var totalCount = _unitInfoRepository.TotalCount;
            
            totalCount.ShouldBeGreaterThan(200);
            totalCount.ShouldBe(_unitInfoRepository.AllUnits.Count);
        }

        [Fact]
        public void BuildingCount_ShouldReturnCorrectCount_WhenCalled()
        {
            var buildingCount = _unitInfoRepository.BuildingCount;
            
            buildingCount.ShouldBeGreaterThan(0);
            buildingCount.ShouldBe(_unitInfoRepository.Buildings.Count());
        }

        [Fact]
        public void UnitCount_ShouldReturnCorrectCount_WhenCalled()
        {
            var unitCount = _unitInfoRepository.UnitCount;
            
            unitCount.ShouldBeGreaterThan(0);
            unitCount.ShouldBe(_unitInfoRepository.Units.Count());
        }

        [Fact]
        public void BuildingCount_Plus_UnitCount_ShouldEqual_TotalCount()
        {
            var totalCount = _unitInfoRepository.TotalCount;
            var buildingCount = _unitInfoRepository.BuildingCount;
            var unitCount = _unitInfoRepository.UnitCount;
            
            (buildingCount + unitCount).ShouldBe(totalCount);
        }

        #endregion

        #region Advanced Query Methods Tests

        [Theory]
        [InlineData(50)]
        [InlineData(100)]
        [InlineData(200)]
        public void GetCheapestUnits_ShouldReturnUnitsUnderCost_WhenMaxCostSpecified(int maxCost)
        {
            var cheapUnits = _unitInfoRepository.GetCheapestUnits(maxCost);
            
            cheapUnits.ShouldAllBe(unit => unit.TotalCost <= maxCost && !unit.IsBuilding);
            if (cheapUnits.Any())
            {
                cheapUnits.OrderBy(unit => unit.TotalCost).ShouldBe(cheapUnits);
            }
        }

        [Fact]
        public void GetCheapestUnits_ShouldReturnEmpty_WhenMaxCostIsZero()
        {
            var cheapUnits = _unitInfoRepository.GetCheapestUnits(0);
            
            cheapUnits.ShouldBeEmpty();
        }

        [Theory]
        [InlineData(100)]
        [InlineData(200)]
        [InlineData(500)]
        public void GetStrongestUnits_ShouldReturnUnitsAboveHP_WhenMinHPSpecified(int minHP)
        {
            var strongUnits = _unitInfoRepository.GetStrongestUnits(minHP);
            
            strongUnits.ShouldAllBe(unit => unit.TotalHitPoints >= minHP);
            if (strongUnits.Any())
            {
                strongUnits.OrderByDescending(unit => unit.TotalHitPoints).ShouldBe(strongUnits);
            }
        }

        [Fact]
        public void GetStrongestUnits_ShouldReturnAllUnits_WhenMinHPIsZero()
        {
            var strongUnits = _unitInfoRepository.GetStrongestUnits(0);
            
            strongUnits.Count().ShouldBe(_unitInfoRepository.TotalCount);
        }

        [Fact]
        public void GetBestValueUnit_ShouldReturnHighestHPPerMineralUnit_WhenCalled()
        {
            var bestValueUnit = _unitInfoRepository.GetBestValueUnit();
            
            if (bestValueUnit != null)
            {
                bestValueUnit.IsCombatUnit.ShouldBeTrue();
                bestValueUnit.MineralCost.ShouldBeGreaterThan(0);
                bestValueUnit.HPPerMineral.ShouldBeGreaterThan(0);
                
                var allCombatUnitsWithCost = _unitInfoRepository.CombatUnits
                    .Where(u => u.MineralCost > 0)
                    .ToList();
                
                if (allCombatUnitsWithCost.Any())
                {
                    var maxHPPerMineral = allCombatUnitsWithCost.Max(u => u.HPPerMineral);
                    bestValueUnit.HPPerMineral.ShouldBe(maxHPPerMineral);
                }
            }
        }

        [Fact]
        public void GetBestValueUnit_ShouldReturnNull_WhenNoCombatUnitsWithCost()
        {
            var bestValueUnit = _unitInfoRepository.GetBestValueUnit();
            
            if (bestValueUnit == null)
            {
                var combatUnitsWithCost = _unitInfoRepository.CombatUnits
                    .Where(u => u.MineralCost > 0);
                combatUnitsWithCost.ShouldBeEmpty();
            }
        }

        #endregion

        #region Data Integrity Tests

        [Fact]
        public void AllUnits_ShouldHaveUniqueIds_WhenLoaded()
        {
            var allUnits = _unitInfoRepository.AllUnits;
            var uniqueIds = allUnits.Select(u => u.Id).Distinct().Count();
            
            uniqueIds.ShouldBe(allUnits.Count);
        }

        [Fact]
        public void AllUnits_ShouldHaveUniqueNames_WhenLoaded()
        {
            var allUnits = _unitInfoRepository.AllUnits;
            var uniqueNames = allUnits.Select(u => u.Name).Distinct().Count();
            
            uniqueNames.ShouldBe(allUnits.Count);
        }

        [Fact]
        public void AllUnits_ShouldHaveValidRaces_WhenLoaded()
        {
            var validRaces = new[] { "Terran", "Protoss", "Zerg", "Neutral" };
            var allUnits = _unitInfoRepository.AllUnits;
            
            allUnits.ShouldAllBe(unit => validRaces.Contains(unit.Race));
        }

        [Fact]
        public void Workers_ShouldContainOnePerRace_WhenLoaded()
        {
            var workers = _unitInfoRepository.Workers.ToList();
            var raceCount = workers.GroupBy(w => w.Race).Count();
            
            raceCount.ShouldBe(3);
            workers.ShouldContain(w => w.Race == "Terran");
            workers.ShouldContain(w => w.Race == "Protoss"); 
            workers.ShouldContain(w => w.Race == "Zerg");
        }

        #endregion
    }
}
