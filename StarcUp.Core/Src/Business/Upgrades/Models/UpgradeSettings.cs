using System.Collections.Generic;
using System.Text.Json.Serialization;
using StarcUp.Business.Units.Types;

namespace StarcUp.Business.Upgrades.Models
{
    /// <summary>
    /// 업그레이드 설정 (UI에서 전달받는 프리셋)
    /// </summary>
    public class UpgradeSettings
    {
        [JsonPropertyName("categories")]
        public List<UpgradeCategory> Categories { get; set; } = new();
        
        [JsonPropertyName("showRemainingTime")]
        public bool ShowRemainingTime { get; set; }        // 잔여시간표기
        
        [JsonPropertyName("showProgressPercentage")]
        public bool ShowProgressPercentage { get; set; }   // 진행률표기
        
        [JsonPropertyName("showProgressBar")]
        public bool ShowProgressBar { get; set; }          // 프로그레스바표기
        
        [JsonPropertyName("upgradeCompletionAlert")]
        public bool UpgradeCompletionAlert { get; set; }   // 업그레이드완료알림
        
        [JsonPropertyName("upgradeStateTracking")]
        public bool UpgradeStateTracking { get; set; }     // 업그레이드상태추적
    }
    
    /// <summary>
    /// 업그레이드 아이템 타입
    /// </summary>
    public enum UpgradeItemType
    {
        Upgrade = 0,
        Tech = 1
    }
    
    /// <summary>
    /// 업그레이드 아이템
    /// </summary>
    public class UpgradeItem
    {
        [JsonPropertyName("type")]
        public UpgradeItemType Type { get; set; }
        
        [JsonPropertyName("value")]
        public int Value { get; set; }
    }
    
    /// <summary>
    /// 업그레이드 카테고리
    /// </summary>
    public class UpgradeCategory
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;
        
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;
        
        [JsonPropertyName("items")]
        public List<UpgradeItem> Items { get; set; } = new();
    }
}