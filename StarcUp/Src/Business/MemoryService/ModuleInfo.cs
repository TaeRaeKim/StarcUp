using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StarcUp.Business.MemoryService
{
    /// <summary>
    /// 모듈 정보 클래스
    /// </summary>
    public class ModuleInfo
    {
        public string Name { get; set; }
        public nint BaseAddress { get; set; }
        public uint Size { get; set; }
        public string FullPath { get; set; }

        public ModuleInfo(string name, nint baseAddress, uint size, string fullPath = "")
        {
            Name = name;
            BaseAddress = baseAddress;
            Size = size;
            FullPath = fullPath;
        }

        public bool IsInRange(nint address)
        {
            long addr = address;
            long baseAddr = BaseAddress;
            return addr >= baseAddr && addr < (baseAddr + Size);
        }

        public override string ToString()
        {
            return $"{Name} - Base: 0x{BaseAddress:X}, Size: 0x{Size:X}";
        }
    }
}
