using System.Collections.Generic;
using System.Text.Json.Serialization;
using StarcUp.Business.Units.Types;

namespace StarcUp.Business.Upgrades.Models
{
    /// <summary>
    /// 통합 업그레이드/테크 아이템 데이터 (순서 보장)
    /// UpgradeItem 베이스에 진행 상태 정보 추가
    /// </summary>
    public class UpgradeItemData
    {
        [JsonPropertyName("item")]
        public UpgradeItem Item { get; set; } = new();
        
        [JsonPropertyName("level")]
        public int Level { get; set; }
        
        [JsonPropertyName("isCompleted")]
        public bool IsCompleted { get; set; }
        
        [JsonPropertyName("remainingFrames")]
        public int RemainingFrames { get; set; }
        
        [JsonPropertyName("totalFrames")]
        public int TotalFrames { get; set; }
        
        [JsonPropertyName("isProgressing")]
        public bool IsProgressing { get; set; }
        
        [JsonPropertyName("progress")]
        public double Progress { get; set; }
        
        /// <summary>
        /// 복사본 생성
        /// </summary>
        public UpgradeItemData Clone()
        {
            return new UpgradeItemData
            {
                Item = new UpgradeItem { Type = Item.Type, Value = Item.Value },
                Level = Level,
                IsCompleted = IsCompleted,
                RemainingFrames = RemainingFrames,
                TotalFrames = TotalFrames,
                IsProgressing = IsProgressing,
                Progress = Progress
            };
        }
    }
    
    /// <summary>
    /// 카테고리별 업그레이드/테크 데이터
    /// </summary>
    public class UpgradeCategoryData
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;
        
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;
        
        [JsonPropertyName("items")]
        public List<UpgradeItemData> Items { get; set; } = new();
        
        /// <summary>
        /// 복사본 생성
        /// </summary>
        public UpgradeCategoryData Clone()
        {
            var clone = new UpgradeCategoryData
            {
                Id = Id,
                Name = Name,
                Items = new List<UpgradeItemData>()
            };
            
            foreach (var item in Items)
            {
                clone.Items.Add(item.Clone());
            }
            
            return clone;
        }
    }
}