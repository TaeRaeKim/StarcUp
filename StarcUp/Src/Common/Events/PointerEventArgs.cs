using System;
using StarcUp.Business.Models;

namespace StarcUp.Common.Events
{
    /// <summary>
    /// 포인터 관련 이벤트 아규먼트
    /// </summary>
    public class PointerEventArgs : EventArgs
    {
        public PointerValue PointerValue { get; }
        public string EventType { get; }

        public PointerEventArgs(PointerValue pointerValue, string eventType = "ValueChanged")
        {
            PointerValue = pointerValue ?? throw new ArgumentNullException(nameof(pointerValue));
            EventType = eventType;
        }

        public override string ToString()
        {
            return $"PointerEvent: {EventType} - {PointerValue}";
        }
    }
}