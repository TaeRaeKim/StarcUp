using StarcUp.Infrastructure.Memory;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StarcUp.Src.Infrastructure.Memory
{
    public interface IOptimizedMemoryReader : IMemoryReader
    {
        /// <summary>
        /// 기존 버퍼에 메모리 읽기 (재할당 없음)
        /// </summary>
        bool ReadMemoryIntoBuffer(nint address, byte[] buffer, int size);

        /// <summary>
        /// 구조체를 기존 메모리에서 직접 읽기 (마샬링 없음)
        /// </summary>
        unsafe bool ReadStructureDirect<T>(nint address, out T result) where T : unmanaged;

        /// <summary>
        /// 구조체 배열을 기존 버퍼로 읽기
        /// </summary>
        unsafe bool ReadStructureArrayIntoBuffer<T>(nint address, T[] buffer, int count) where T : unmanaged;
    }
}
