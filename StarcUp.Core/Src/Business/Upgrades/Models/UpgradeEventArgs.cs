using System;
using StarcUp.Business.Units.Types;

namespace StarcUp.Business.Upgrades.Models
{
    /// <summary>
    /// 업그레이드 상태 변경 이벤트 인수
    /// </summary>
    public class UpgradeStateChangedEventArgs : EventArgs
    {
        public UpgradeType? UpgradeType { get; set; }
        public TechType? TechType { get; set; }
        public byte OldLevel { get; set; }
        public byte NewLevel { get; set; }
        public bool WasCompleted { get; set; }
        public bool IsCompleted { get; set; }
        public byte PlayerIndex { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.Now;
    }
    
    /// <summary>
    /// 업그레이드 완료 이벤트 인수
    /// </summary>
    public class UpgradeCompletedEventArgs : EventArgs
    {
        public UpgradeType? UpgradeType { get; set; }
        public TechType? TechType { get; set; }
        public string Name { get; set; } = string.Empty;
        public byte Level { get; set; }
        public byte PlayerIndex { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.Now;
    }
}