using System;
using System.Drawing;

namespace StarcUp.Business.Models
{
    /// <summary>
    /// TEB (Thread Environment Block) 정보 모델
    /// </summary>
    public class TebInfo
    {
        public uint ThreadId { get; set; }
        public IntPtr TebAddress { get; set; }
        public int Index { get; set; }
        public DateTime DiscoveredAt { get; set; }

        public TebInfo()
        {
            DiscoveredAt = DateTime.Now;
        }

        public TebInfo(uint threadId, IntPtr tebAddress, int index) : this()
        {
            ThreadId = threadId;
            TebAddress = tebAddress;
            Index = index;
        }

        public override string ToString()
        {
            return $"TEB{Index}: Thread={ThreadId}, Address=0x{TebAddress.ToInt64():X16}";
        }
    }

}