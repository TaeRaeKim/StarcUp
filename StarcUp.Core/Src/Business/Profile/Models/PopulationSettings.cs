using System;
using System.Collections.Generic;
using StarcUp.Business.Units.Types;

namespace StarcUp.Business.Profile.Models
{
    /// <summary>
    /// 인구수 설정 메인 클래스
    /// </summary>
    public class PopulationSettings
    {
        public PopulationMode Mode { get; set; } = PopulationMode.Fixed;
        public FixedModeSettings? FixedSettings { get; set; }
        public BuildingModeSettings? BuildingSettings { get; set; }
    }

    /// <summary>
    /// 인구수 경고 모드
    /// </summary>
    public enum PopulationMode
    {
        Fixed,   // 모드 A: 고정값 기반
        Building // 모드 B: 생산 건물 기반
    }

    /// <summary>
    /// 모드 A: 고정값 기반 설정
    /// </summary>
    public class FixedModeSettings
    {
        public int ThresholdValue { get; set; } = 4;  // 1-50 범위
        public TimeLimitSettings? TimeLimit { get; set; }
    }

    /// <summary>
    /// 시간 제한 설정
    /// </summary>
    public class TimeLimitSettings
    {
        public bool Enabled { get; set; } = true;
        public int Minutes { get; set; } = 3;    // 0-59
        public int Seconds { get; set; } = 0;    // 0-59
        public int TotalSeconds => Minutes * 60 + Seconds;
    }

    /// <summary>
    /// 모드 B: 생산 건물 기반 설정
    /// </summary>
    public class BuildingModeSettings
    {
        public RaceType Race { get; set; } = RaceType.Protoss;
        public List<TrackedBuilding> TrackedBuildings { get; set; } = new();
    }

    /// <summary>
    /// 추적할 건물 정보
    /// </summary>
    public class TrackedBuilding
    {
        public UnitType BuildingType { get; set; } = UnitType.ProtossGateway;
        public string Name { get; set; } = "";
        public int Multiplier { get; set; } = 1;  // 1-10 범위
        public bool Enabled { get; set; } = false;
    }

}