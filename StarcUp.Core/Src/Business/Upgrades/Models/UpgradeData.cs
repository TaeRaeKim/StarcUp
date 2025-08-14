using System.Text.Json.Serialization;
using StarcUp.Business.Units.Types;

namespace StarcUp.Business.Upgrades.Models
{
    /// <summary>
    /// 업그레이드 데이터
    /// </summary>
    public class UpgradeData
    {
        [JsonPropertyName("type")]
        public UpgradeType Type { get; set; }
        
        [JsonPropertyName("level")]
        public byte Level { get; set; }           // 0-3
        
        [JsonPropertyName("remainingFrames")]
        public int RemainingFrames { get; set; }  // 진행 중일 때
        
        [JsonPropertyName("totalFrames")]
        public int TotalFrames { get; set; }      // 총 소요 프레임
        
        [JsonPropertyName("isProgressing")]
        public bool IsProgressing { get; set; }
        
        /// <summary>
        /// 업그레이드가 완료되었는지 여부
        /// </summary>
        public bool IsCompleted => Level > 0;
        
        /// <summary>
        /// 진행률 (0.0 ~ 1.0)
        /// </summary>
        public float Progress => TotalFrames > 0 && IsProgressing 
            ? 1.0f - (RemainingFrames / (float)TotalFrames) 
            : IsCompleted ? 1.0f : 0.0f;
        
        /// <summary>
        /// 복사본 생성
        /// </summary>
        public UpgradeData Clone()
        {
            return new UpgradeData
            {
                Type = Type,
                Level = Level,
                RemainingFrames = RemainingFrames,
                TotalFrames = TotalFrames,
                IsProgressing = IsProgressing
            };
        }
    }
}