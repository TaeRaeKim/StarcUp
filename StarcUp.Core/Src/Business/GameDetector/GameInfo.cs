using System;
using System.Drawing;

namespace StarcUp.Business.GameDetection
{
    /// <summary>
    /// 게임 정보 모델
    /// </summary>
    public class GameInfo
    {
        public int ProcessId { get; set; }
        public string ProcessName { get; set; }
        public DateTime DetectedAt { get; set; }

        public GameInfo()
        {
            DetectedAt = DateTime.Now;
        }

        public GameInfo(int processId, string processName) : this()
        {
            ProcessId = processId;
            ProcessName = processName;
        }

        public override string ToString()
        {
            return $"Game: {ProcessName} (PID: {ProcessId}, Detected At: {DetectedAt})";
        }
    }
}