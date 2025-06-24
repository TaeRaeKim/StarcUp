
using System;
using System.Diagnostics;
using Xunit;
using Shouldly;
using StarcUp.Src.Business.UnitInfoUtil;

namespace StarcUp.Test.Src.Business.Game
{
    public class TUnitInfoUtils
    {
        private readonly UnitInfoUtils _unitManager;
        public TUnitInfoUtils()
        {
            _unitManager = new UnitInfoUtils();
        }

        [Fact]
        public void UnitLoadTest()
        {
            _unitManager.AllUnits.Count.ShouldBe(205);
        }

        [Theory]
        [InlineData(UnitType.TerranMarine, UnitType.ZergZergling, UnitType.ProtossZealot)]
        public void UnitSearchTest(UnitType marinType, UnitType zerglingType, UnitType zealotType)
        {
            var marin = _unitManager.GetByName(marinType.GetUnitName());
            var zergling = _unitManager.GetByName(zerglingType.GetUnitName());
            var zealot = _unitManager.GetByName(zealotType.GetUnitName());

            marin.ShouldNotBeNull();
            zergling.ShouldNotBeNull();
            zealot.ShouldNotBeNull();
            zealot.HP.ShouldBeGreaterThan(marin.HP);
            marin.HP.ShouldBeGreaterThan(zergling.HP);

        }

        [Fact]
        public void WhatIsUnitType()
        {
            var unit = _unitManager.GetById(194);
            Console.WriteLine(unit.DisplayName);
        }
    }
}
