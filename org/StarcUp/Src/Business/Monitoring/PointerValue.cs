using System;

namespace StarcUp.Business.Monitoring
{
    /// <summary>
    /// 포인터 값 변화 정보 모델
    /// </summary>
    public class PointerValue
    {
        public int OldValue { get; set; }
        public int NewValue { get; set; }
        public int Difference => NewValue - OldValue;
        public DateTime Timestamp { get; set; }
        public IntPtr Address { get; set; }

        public PointerValue()
        {
            Timestamp = DateTime.Now;
        }

        public PointerValue(int oldValue, int newValue, IntPtr address = default) : this()
        {
            OldValue = oldValue;
            NewValue = newValue;
            Address = address;
        }

        public bool HasChanged => OldValue != NewValue;

        public override string ToString()
        {
            return $"Pointer: {OldValue} → {NewValue} (차이: {Difference:+#;-#;0}) at 0x{Address.ToInt64():X}";
        }
    }
}