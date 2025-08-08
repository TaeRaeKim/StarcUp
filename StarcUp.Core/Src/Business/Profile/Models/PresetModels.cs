using System.Text.Json.Serialization;

namespace StarcUp.Business.Profile.Models
{
    /// <summary>
    /// UI로부터 받은 프리셋 초기화 데이터
    /// </summary>
    public class PresetInitData
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = "preset-init";

        [JsonPropertyName("timestamp")]
        public long Timestamp { get; set; }

        [JsonPropertyName("presets")]
        public PresetCollection Presets { get; set; } = new();
    }

    /// <summary>
    /// UI로부터 받은 프리셋 업데이트 데이터
    /// </summary>
    public class PresetUpdateData
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = "preset-update";

        [JsonPropertyName("timestamp")]
        public long Timestamp { get; set; }

        [JsonPropertyName("presetType")]
        public string PresetType { get; set; }

        [JsonPropertyName("data")]
        public PresetItem Data { get; set; }
    }

    /// <summary>
    /// 모든 프리셋 타입을 포함하는 컬렉션
    /// </summary>
    public class PresetCollection
    {
        [JsonPropertyName("worker")]
        public PresetItem Worker { get; set; }

        [JsonPropertyName("population")]
        public PresetItem Population { get; set; }

        [JsonPropertyName("unit")]
        public PresetItem Unit { get; set; }

        [JsonPropertyName("upgrade")]
        public PresetItem Upgrade { get; set; }

        [JsonPropertyName("buildOrder")]
        public PresetItem BuildOrder { get; set; }
    }

    /// <summary>
    /// 개별 프리셋 항목 데이터
    /// </summary>
    public class PresetItem
    {
        [JsonPropertyName("enabled")]
        public bool Enabled { get; set; }

        [JsonPropertyName("settingsMask")]
        public int SettingsMask { get; set; }

        [JsonPropertyName("settings")]
        public object Settings { get; set; } // 인구수 등 복잡한 설정을 위한 범용 필드
    }
}