using StarcUp.Infrastructure.Memory;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace StarcUp.Src.Infrastructure.Memory
{
    /// <summary>
    /// 최적화된 메모리 리더 구현 - 기존 MemoryReader 확장
    /// </summary>
    public partial class OptimizedMemoryReader : MemoryReader, IOptimizedMemoryReader
    {
        private readonly ArrayPool<byte> _bytePool;

        public OptimizedMemoryReader() : base()
        {
            _bytePool = ArrayPool<byte>.Shared;
        }

        /// <summary>
        /// 기존 버퍼에 메모리 읽기
        /// </summary>
        public bool ReadMemoryIntoBuffer(nint address, byte[] buffer, int size)
        {
            if (buffer == null || size <= 0 || size > buffer.Length)
                return false;

            return ReadProcessMemory(address, buffer, size);
        }

        /// <summary>
        /// 구조체를 직접 읽기 (마샬링 오버헤드 없음)
        /// </summary>
        public unsafe bool ReadStructureDirect<T>(nint address, out T result) where T : unmanaged
        {
            result = default;

            if (!IsConnected)
                return false;

            int size = sizeof(T);
            fixed (T* ptr = &result)
            {
                return MemoryAPI.ReadProcessMemory(_processHandle, address, (nint)ptr, size, out _);
            }
        }

        /// <summary>
        /// 구조체 배열을 기존 버퍼로 읽기
        /// </summary>
        public unsafe bool ReadStructureArrayIntoBuffer<T>(nint address, T[] buffer, int count) where T : unmanaged
        {
            if (buffer == null || count <= 0 || count > buffer.Length)
                return false;

            int structSize = sizeof(T);
            int totalSize = structSize * count;

            fixed (T* ptr = buffer)
            {
                return MemoryAPI.ReadProcessMemory(_processHandle, address, (nint)ptr, totalSize, out _);
            }
        }
    }
}
