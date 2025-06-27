using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using Shouldly;
using StarcUp.Business.Units.Runtime.Services;
using StarcUp.Business.Units.Runtime.Adapters;
using StarcUp.Business.Units.Runtime.Models;
using StarcUp.Business.Units.Types;

namespace StarcUp.Test.Src.Business.Units.Runtime.Services
{
    public class UnitServiceTest
    {
        private readonly MockUnitMemoryAdapter _mockAdapter;
        private readonly UnitService _unitService;

        public UnitServiceTest()
        {
            _mockAdapter = new MockUnitMemoryAdapter();
            _unitService = new UnitService(_mockAdapter);
        }

        #region Test Helper Methods

        private static UnitRaw CreateTestUnitRaw(
            UnitType unitType = UnitType.TerranMarine,
            byte playerId = 0,
            uint health = 40,
            uint shield = 0,
            ushort x = 100,
            ushort y = 100,
            bool isValid = true)
        {
            var unitRaw = new UnitRaw();
            unsafe
            {
                unitRaw.health = health;
                unitRaw.shield = shield;
                unitRaw.currentX = x;
                unitRaw.currentY = y;
                unitRaw.destX = x;
                unitRaw.destY = y;
                unitRaw.playerIndex = playerId;
                unitRaw.unitType = (ushort)unitType;
                unitRaw.actionIndex = 0;
                unitRaw.actionState = 0;
                unitRaw.attackCooldown = 0;
                unitRaw.timer = 0;
                unitRaw.currentUpgrade = 0;
                unitRaw.currentUpgradeLevel = 0;
                unitRaw.productionQueueIndex = 0;
                
                for (int i = 0; i < 5; i++)
                {
                    unitRaw.productionQueue[i] = 0;
                }

                if (!isValid)
                {
                    unitRaw.unitType = (ushort)UnitType.None;
                }
            }
            return unitRaw;
        }

        #endregion

        #region Basic Service Methods Tests

        [Fact]
        public void Constructor_ShouldThrowArgumentNullException_WhenAdapterIsNull()
        {
            Action act = () => new UnitService(null);
            
            act.ShouldThrow<ArgumentNullException>();
        }

        [Fact]
        public void SetUnitArrayBaseAddress_ShouldCallAdapter_WhenValidAddress()
        {
            nint testAddress = (nint)0x12345678;
            
            _unitService.SetUnitArrayBaseAddress(testAddress);
            
            _mockAdapter.SetBaseAddressCalled.ShouldBeTrue();
            _mockAdapter.LastBaseAddress.ShouldBe(testAddress);
        }

        [Fact]
        public void SetUnitArrayBaseAddress_ShouldThrowObjectDisposedException_WhenDisposed()
        {
            _unitService.Dispose();
            
            Action act = () => _unitService.SetUnitArrayBaseAddress((nint)0x12345678);
            
            act.ShouldThrow<ObjectDisposedException>();
        }

        [Fact]
        public void LoadAllUnits_ShouldReturnTrue_WhenSuccessful()
        {
            _mockAdapter.LoadAllUnitsResult = true;
            
            var result = _unitService.LoadAllUnits();
            
            result.ShouldBeTrue();
            _mockAdapter.LoadAllUnitsCalled.ShouldBeTrue();
        }

        [Fact]
        public void LoadAllUnits_ShouldReturnFalse_WhenFailed()
        {
            _mockAdapter.LoadAllUnitsResult = false;
            
            var result = _unitService.LoadAllUnits();
            
            result.ShouldBeFalse();
        }

        [Fact]
        public void LoadAllUnits_ShouldThrowObjectDisposedException_WhenDisposed()
        {
            _unitService.Dispose();
            
            Action act = () => _unitService.LoadAllUnits();
            
            act.ShouldThrow<ObjectDisposedException>();
        }

        [Fact]
        public void RefreshUnits_ShouldReturnTrue_WhenSuccessful()
        {
            _mockAdapter.RefreshUnitsResult = true;
            
            var result = _unitService.RefreshUnits();
            
            result.ShouldBeTrue();
            _mockAdapter.RefreshUnitsCalled.ShouldBeTrue();
        }

        #endregion

        #region Unit Retrieval Tests

        [Fact]
        public void GetAllUnits_ShouldReturnAllValidUnits_WhenCalled()
        {
            var testUnits = new[]
            {
                CreateTestUnitRaw(UnitType.TerranMarine, 0, 40),
                CreateTestUnitRaw(UnitType.TerranSCV, 0, 60),
                CreateTestUnitRaw(UnitType.None, 0, 0, isValid: false)
            };
            _mockAdapter.AllRawUnits = testUnits;

            var result = _unitService.GetAllUnits().ToList();

            result.Count.ShouldBe(2);
            result.ShouldAllBe(u => u.IsValid);
        }

        [Fact]
        public void GetAllUnits_ShouldThrowObjectDisposedException_WhenDisposed()
        {
            _unitService.Dispose();
            
            Action act = () => _unitService.GetAllUnits();
            
            act.ShouldThrow<ObjectDisposedException>();
        }

        [Theory]
        [InlineData((byte)0)]
        [InlineData((byte)1)]
        [InlineData((byte)7)]
        public void GetPlayerUnits_ShouldReturnOnlyPlayerUnits_WhenValidPlayerId(byte playerId)
        {
            var testUnits = new[]
            {
                CreateTestUnitRaw(UnitType.TerranMarine, 0),
                CreateTestUnitRaw(UnitType.TerranSCV, 1),
                CreateTestUnitRaw(UnitType.ProtossZealot, 7)
            };
            _mockAdapter.SetPlayerUnits(playerId, testUnits.Where(u => u.playerIndex == playerId));

            var result = _unitService.GetPlayerUnits(playerId).ToList();

            result.ShouldAllBe(u => u.PlayerIndex == playerId);
            _mockAdapter.GetPlayerRawUnitsCalled.ShouldBeTrue();
            _mockAdapter.LastPlayerId.ShouldBe(playerId);
        }

        [Fact]
        public void GetUnitsByType_ShouldReturnOnlySpecifiedType_WhenValidType()
        {
            var unitType = UnitType.TerranMarine;
            var testUnits = new[]
            {
                CreateTestUnitRaw(UnitType.TerranMarine),
                CreateTestUnitRaw(UnitType.TerranMarine),
                CreateTestUnitRaw(UnitType.TerranSCV)
            };
            _mockAdapter.SetUnitsByType((ushort)unitType, testUnits.Where(u => u.unitType == (ushort)unitType));

            var result = _unitService.GetUnitsByType(unitType).ToList();

            result.ShouldAllBe(u => u.UnitType == unitType);
            _mockAdapter.GetRawUnitsByTypeCalled.ShouldBeTrue();
            _mockAdapter.LastUnitType.ShouldBe((ushort)unitType);
        }

        [Fact]
        public void GetUnitsNearPosition_ShouldReturnUnitsInRadius_WhenValidParameters()
        {
            ushort x = 100, y = 100;
            int radius = 50;
            var testUnits = new[]
            {
                CreateTestUnitRaw(x: 110, y: 110),
                CreateTestUnitRaw(x: 90, y: 90),
                CreateTestUnitRaw(x: 200, y: 200)
            };
            _mockAdapter.SetUnitsNearPosition(x, y, radius, testUnits.Take(2));

            var result = _unitService.GetUnitsNearPosition(x, y, radius).ToList();

            result.Count.ShouldBe(2);
            _mockAdapter.GetRawUnitsNearPositionCalled.ShouldBeTrue();
            _mockAdapter.LastX.ShouldBe(x);
            _mockAdapter.LastY.ShouldBe(y);
            _mockAdapter.LastRadius.ShouldBe(radius);
        }

        #endregion

        #region Filtered Unit Tests

        [Fact]
        public void GetAliveUnits_ShouldReturnOnlyAliveUnits_WhenCalled()
        {
            var testUnits = new[]
            {
                CreateTestUnitRaw(health: 40),
                CreateTestUnitRaw(health: 0),
                CreateTestUnitRaw(health: 100)
            };
            _mockAdapter.AllRawUnits = testUnits;

            var result = _unitService.GetAliveUnits().ToList();

            result.Count.ShouldBe(2);
            result.ShouldAllBe(u => u.IsAlive);
        }

        [Fact]
        public void GetBuildingUnits_ShouldReturnOnlyBuildings_WhenCalled()
        {
            var testUnits = new[]
            {
                CreateTestUnitRaw(UnitType.TerranCommandCenter),
                CreateTestUnitRaw(UnitType.TerranMarine),
                CreateTestUnitRaw(UnitType.TerranBarracks)
            };
            _mockAdapter.AllRawUnits = testUnits;

            var result = _unitService.GetBuildingUnits().ToList();

            result.ShouldAllBe(u => u.IsBuilding);
        }

        [Fact]
        public void GetWorkerUnits_ShouldReturnOnlyWorkers_WhenCalled()
        {
            var testUnits = new[]
            {
                CreateTestUnitRaw(UnitType.TerranSCV),
                CreateTestUnitRaw(UnitType.TerranMarine),
                CreateTestUnitRaw(UnitType.ProtossProbe)
            };
            _mockAdapter.AllRawUnits = testUnits;

            var result = _unitService.GetWorkerUnits().ToList();

            result.ShouldAllBe(u => u.IsWorker);
        }

        [Fact]
        public void GetHeroUnits_ShouldReturnOnlyHeroes_WhenCalled()
        {
            var testUnits = new[]
            {
                CreateTestUnitRaw(UnitType.HeroJimRaynorMarine),
                CreateTestUnitRaw(UnitType.TerranMarine),
                CreateTestUnitRaw(UnitType.HeroSarahKerrigan)
            };
            _mockAdapter.AllRawUnits = testUnits;

            var result = _unitService.GetHeroUnits().ToList();

            result.ShouldAllBe(u => u.IsHero);
        }

        #endregion

        #region Utility Method Tests

        [Fact]
        public void GetActiveUnitCount_ShouldReturnAdapterCount_WhenCalled()
        {
            _mockAdapter.ActiveUnitCount = 42;

            var result = _unitService.GetActiveUnitCount();

            result.ShouldBe(42);
            _mockAdapter.GetActiveUnitCountCalled.ShouldBeTrue();
        }

        [Fact]
        public void GetActiveUnitCount_ShouldThrowObjectDisposedException_WhenDisposed()
        {
            _unitService.Dispose();
            
            Action act = () => _unitService.GetActiveUnitCount();
            
            act.ShouldThrow<ObjectDisposedException>();
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void IsUnitValid_ShouldReturnUnitValidProperty_WhenCalled(bool isValid)
        {
            var unit = Unit.FromRaw(CreateTestUnitRaw(isValid: isValid));

            var result = _unitService.IsUnitValid(unit);

            result.ShouldBe(isValid);
        }

        #endregion

        #region IDisposable Tests

        [Fact]
        public void Dispose_ShouldDisposeAdapter_WhenCalled()
        {
            _unitService.Dispose();

            _mockAdapter.DisposeCalled.ShouldBeTrue();
        }

        [Fact]
        public void Dispose_ShouldBeIdempotent_WhenCalledMultipleTimes()
        {
            _unitService.Dispose();
            _unitService.Dispose();

            _mockAdapter.DisposeCallCount.ShouldBe(1);
        }

        #endregion
    }

    #region Mock Implementation

    public class MockUnitMemoryAdapter : IUnitMemoryAdapter
    {
        public bool SetBaseAddressCalled { get; private set; }
        public nint LastBaseAddress { get; private set; }
        public bool LoadAllUnitsCalled { get; private set; }
        public bool LoadAllUnitsResult { get; set; } = true;
        public bool RefreshUnitsCalled { get; private set; }
        public bool RefreshUnitsResult { get; set; } = true;
        public bool GetPlayerRawUnitsCalled { get; private set; }
        public byte LastPlayerId { get; private set; }
        public bool GetRawUnitsByTypeCalled { get; private set; }
        public ushort LastUnitType { get; private set; }
        public bool GetRawUnitsNearPositionCalled { get; private set; }
        public ushort LastX { get; private set; }
        public ushort LastY { get; private set; }
        public int LastRadius { get; private set; }
        public bool GetActiveUnitCountCalled { get; private set; }
        public int ActiveUnitCount { get; set; } = 0;
        public bool DisposeCalled { get; private set; }
        public int DisposeCallCount { get; private set; }

        public UnitRaw[] AllRawUnits { get; set; } = Array.Empty<UnitRaw>();
        private readonly Dictionary<byte, IEnumerable<UnitRaw>> _playerUnits = new();
        private readonly Dictionary<ushort, IEnumerable<UnitRaw>> _unitsByType = new();
        private readonly Dictionary<string, IEnumerable<UnitRaw>> _unitsNearPosition = new();

        public void SetUnitArrayBaseAddress(nint baseAddress)
        {
            SetBaseAddressCalled = true;
            LastBaseAddress = baseAddress;
        }

        public bool LoadAllUnits()
        {
            LoadAllUnitsCalled = true;
            return LoadAllUnitsResult;
        }

        public bool RefreshUnits()
        {
            RefreshUnitsCalled = true;
            return RefreshUnitsResult;
        }

        public IEnumerable<UnitRaw> GetAllRawUnits()
        {
            return AllRawUnits;
        }

        public IEnumerable<UnitRaw> GetPlayerRawUnits(byte playerId)
        {
            GetPlayerRawUnitsCalled = true;
            LastPlayerId = playerId;
            return _playerUnits.TryGetValue(playerId, out var units) ? units : Enumerable.Empty<UnitRaw>();
        }

        public IEnumerable<UnitRaw> GetRawUnitsByType(ushort unitType)
        {
            GetRawUnitsByTypeCalled = true;
            LastUnitType = unitType;
            return _unitsByType.TryGetValue(unitType, out var units) ? units : Enumerable.Empty<UnitRaw>();
        }

        public IEnumerable<UnitRaw> GetRawUnitsNearPosition(ushort x, ushort y, int radius)
        {
            GetRawUnitsNearPositionCalled = true;
            LastX = x;
            LastY = y;
            LastRadius = radius;
            var key = $"{x}_{y}_{radius}";
            return _unitsNearPosition.TryGetValue(key, out var units) ? units : Enumerable.Empty<UnitRaw>();
        }

        public IEnumerable<(int Index, UnitRaw Current, UnitRaw Previous)> GetChangedRawUnits()
        {
            return Enumerable.Empty<(int, UnitRaw, UnitRaw)>();
        }

        public int GetActiveUnitCount()
        {
            GetActiveUnitCountCalled = true;
            return ActiveUnitCount;
        }

        public bool IsRawUnitValid(UnitRaw unit)
        {
            return unit.unitType != (ushort)UnitType.None && unit.playerIndex < 12;
        }

        public void SetPlayerUnits(byte playerId, IEnumerable<UnitRaw> units)
        {
            _playerUnits[playerId] = units;
        }

        public void SetUnitsByType(ushort unitType, IEnumerable<UnitRaw> units)
        {
            _unitsByType[unitType] = units;
        }

        public void SetUnitsNearPosition(ushort x, ushort y, int radius, IEnumerable<UnitRaw> units)
        {
            var key = $"{x}_{y}_{radius}";
            _unitsNearPosition[key] = units;
        }

        public void Dispose()
        {
            DisposeCalled = true;
            DisposeCallCount++;
        }
    }

    #endregion
}
