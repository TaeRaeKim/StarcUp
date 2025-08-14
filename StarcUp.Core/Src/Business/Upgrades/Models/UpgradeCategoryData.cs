using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace StarcUp.Business.Upgrades.Models
{
    /// <summary>
    /// 카테고리별 업그레이드/테크 데이터
    /// </summary>
    public class UpgradeCategoryData
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;
        
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;
        
        [JsonPropertyName("upgrades")]
        public List<UpgradeData> Upgrades { get; set; } = new();
        
        [JsonPropertyName("techs")]
        public List<TechData> Techs { get; set; } = new();
        
        /// <summary>
        /// 복사본 생성
        /// </summary>
        public UpgradeCategoryData Clone()
        {
            var clone = new UpgradeCategoryData
            {
                Id = Id,
                Name = Name,
                Upgrades = new List<UpgradeData>(),
                Techs = new List<TechData>()
            };
            
            foreach (var upgrade in Upgrades)
            {
                clone.Upgrades.Add(upgrade.Clone());
            }
            
            foreach (var tech in Techs)
            {
                clone.Techs.Add(tech.Clone());
            }
            
            return clone;
        }
    }
}