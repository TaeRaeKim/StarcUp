using System;
using StarcUp.Business.InGameStateMonitor;

namespace StarcUp.Common.Events
{
    /// <summary>
    /// 포인터 관련 이벤트 아규먼트
    /// </summary>
    public class ProcessEventArgs : EventArgs
    {
        public int ProcessId { get; }
        public string EventType { get; }

        public ProcessEventArgs(int value, string eventType = "ProcessFound")
        {
            ProcessId = value;
            EventType = eventType;
        }

        public override string ToString()
        {
            return $"ProcessEvent: {EventType} - {ProcessId}";
        }
    }
}