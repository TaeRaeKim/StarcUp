using System.Text.Json.Serialization;
using StarcUp.Business.Units.Types;

namespace StarcUp.Business.Upgrades.Models
{
    /// <summary>
    /// 테크 데이터
    /// </summary>
    public class TechData
    {
        [JsonPropertyName("type")]
        public TechType Type { get; set; }
        
        [JsonPropertyName("isCompleted")]
        public bool IsCompleted { get; set; }
        
        [JsonPropertyName("remainingFrames")]
        public int RemainingFrames { get; set; }
        
        [JsonPropertyName("totalFrames")]
        public int TotalFrames { get; set; }
        
        [JsonPropertyName("isProgressing")]
        public bool IsProgressing { get; set; }
        
        /// <summary>
        /// 진행률 (0.0 ~ 1.0)
        /// </summary>
        public float Progress => TotalFrames > 0 && IsProgressing 
            ? 1.0f - (RemainingFrames / (float)TotalFrames) 
            : IsCompleted ? 1.0f : 0.0f;
        
        /// <summary>
        /// 복사본 생성
        /// </summary>
        public TechData Clone()
        {
            return new TechData
            {
                Type = Type,
                IsCompleted = IsCompleted,
                RemainingFrames = RemainingFrames,
                TotalFrames = TotalFrames,
                IsProgressing = IsProgressing
            };
        }
    }
}