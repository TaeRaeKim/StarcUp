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
        public nint WindowHandle { get; set; }
        public string ProcessName { get; set; }
        public Rectangle WindowBounds { get; set; }
        public bool IsFullscreen { get; set; }
        public bool IsMinimized { get; set; }
        public bool IsActive { get; set; }
        public DateTime DetectedAt { get; set; }

        public GameInfo()
        {
            DetectedAt = DateTime.Now;
        }

        public GameInfo(int processId, nint windowHandle, string processName) : this()
        {
            ProcessId = processId;
            WindowHandle = windowHandle;
            ProcessName = processName;
        }

        public override string ToString()
        {
            return $"Game: {ProcessName} (PID: {ProcessId}, Handle: 0x{WindowHandle.ToInt64():X})";
        }
    }
}