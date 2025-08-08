using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using StarcUp.Business.Units.Types;

namespace StarcUp.Business.Profile.Models
{
    /// <summary>
    /// 인구수 설정 메인 클래스
    /// </summary>
    public class PopulationSettings
    {
        [JsonPropertyName("mode")]
        public PopulationMode Mode { get; set; } = PopulationMode.Fixed;
        
        [JsonPropertyName("fixedSettings")]
        public FixedModeSettings? FixedSettings { get; set; }
        
        [JsonPropertyName("buildingSettings")]
        public BuildingModeSettings? BuildingSettings { get; set; }
    }

    /// <summary>
    /// 인구수 경고 모드
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
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
        [JsonPropertyName("thresholdValue")]
        public int ThresholdValue { get; set; } = 4;  // 1-50 범위
        
        [JsonPropertyName("timeLimit")]
        public TimeLimitSettings? TimeLimit { get; set; }
    }

    /// <summary>
    /// 시간 제한 설정
    /// </summary>
    public class TimeLimitSettings
    {
        [JsonPropertyName("enabled")]
        public bool Enabled { get; set; } = true;
        
        [JsonPropertyName("minutes")]
        public int Minutes { get; set; } = 3;    // 0-59
        
        [JsonPropertyName("seconds")]
        public int Seconds { get; set; } = 0;    // 0-59
        
        public int TotalSeconds => Minutes * 60 + Seconds;
    }

    /// <summary>
    /// 모드 B: 생산 건물 기반 설정
    /// </summary>
    public class BuildingModeSettings
    {
        [JsonPropertyName("race")]
        public RaceType Race { get; set; } = RaceType.Protoss;
        
        [JsonPropertyName("trackedBuildings")]
        public List<TrackedBuilding> TrackedBuildings { get; set; } = new();
    }

    /// <summary>
    /// 추적할 건물 정보
    /// </summary>
    public class TrackedBuilding
    {
        [JsonPropertyName("buildingType")]
        public UnitType BuildingType { get; set; } = UnitType.ProtossGateway;
        
        [JsonPropertyName("multiplier")]
        public int Multiplier { get; set; } = 1;  // 1-10 범위
        
        [JsonPropertyName("enabled")]
        public bool Enabled { get; set; } = false;
    }

}