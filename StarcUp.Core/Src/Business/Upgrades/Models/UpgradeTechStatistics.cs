using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace StarcUp.Business.Upgrades.Models
{
    /// <summary>
    /// 업그레이드/테크 통계 데이터
    /// </summary>
    public class UpgradeTechStatistics
    {
        [JsonPropertyName("categories")]
        public List<UpgradeCategoryData> Categories { get; set; } = new();
        
        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; } = DateTime.Now;
        
        /// <summary>
        /// 복사본 생성
        /// </summary>
        public UpgradeTechStatistics Clone()
        {
            var clone = new UpgradeTechStatistics
            {
                Timestamp = Timestamp,
                Categories = new List<UpgradeCategoryData>()
            };
            
            foreach (var category in Categories)
            {
                clone.Categories.Add(category.Clone());
            }
            
            return clone;
        }
    }
}