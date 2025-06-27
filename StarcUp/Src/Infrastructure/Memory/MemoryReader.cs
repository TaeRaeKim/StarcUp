using System;
using System.Buffers;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace StarcUp.Infrastructure.Memory
{
    /// <summary>
    /// 통합된 메모리 리더 구현
    /// - 기본 메모리 읽기 기능과 최적화된 기능을 모두 포함
    /// - Windows API를 직접 사용하는 저수준 메모리 리더
    /// </summary>
    public class MemoryReader : IMemoryReader
    {
        private Process _process;
        private nint _processHandle;
        private bool _isDisposed;
        private readonly ArrayPool<byte> _bytePool;
        private readonly bool _useOptimizations;

        public MemoryReader(bool enableOptimizations = true)
        {
            _bytePool = ArrayPool<byte>.Shared;
            _useOptimizations = enableOptimizations;
        }

        public bool IsConnected => _process != null && !_process.HasExited && _processHandle != 0;
        public int ConnectedProcessId => _process?.Id ?? 0;

        protected nint ProcessHandle => _processHandle;

        public bool ConnectToProcess(int processId)
        {
            try
            {
                Disconnect();

                _process = Process.GetProcessById(processId);
                if (_process == null || _process.HasExited)
                    return false;

                _processHandle = MemoryAPI.OpenProcess(
                    MemoryAPI.PROCESS_QUERY_INFORMATION | MemoryAPI.PROCESS_VM_READ,
                    false,
                    processId);

                return _processHandle != 0;
            }
            catch
            {
                return false;
            }
        }

        public void Disconnect()
        {
            if (_processHandle != 0)
            {
                MemoryAPI.CloseHandle(_processHandle);
                _processHandle = 0;
            }

            _process?.Dispose();
            _process = null;
        }

        #region 기본 메모리 읽기 메서드들

        public byte[] ReadMemoryRaw(nint address, int size)
        {
            if (!IsConnected || size <= 0) return null;

            byte[] buffer = new byte[size];
            if (MemoryAPI.ReadProcessMemory(_processHandle, address, buffer, size, out nint bytesRead))
            {
                if (bytesRead == size) return buffer;

                Array.Resize(ref buffer, (int)bytesRead);
                return buffer;
            }

            return null;
        }

        public int ReadInt(nint address)
        {
            byte[] buffer = ReadMemoryRaw(address, sizeof(int));
            return buffer != null ? BitConverter.ToInt32(buffer, 0) : 0;
        }

        public float ReadFloat(nint address)
        {
            byte[] buffer = ReadMemoryRaw(address, sizeof(float));
            return buffer != null ? BitConverter.ToSingle(buffer, 0) : 0f;
        }

        public double ReadDouble(nint address)
        {
            byte[] buffer = ReadMemoryRaw(address, sizeof(double));
            return buffer != null ? BitConverter.ToDouble(buffer, 0) : 0.0;
        }

        public byte ReadByte(nint address)
        {
            byte[] buffer = ReadMemoryRaw(address, sizeof(byte));
            return buffer?[0] ?? 0;
        }

        public short ReadShort(nint address)
        {
            byte[] buffer = ReadMemoryRaw(address, sizeof(short));
            return buffer != null ? BitConverter.ToInt16(buffer, 0) : (short)0;
        }

        public long ReadLong(nint address)
        {
            byte[] buffer = ReadMemoryRaw(address, sizeof(long));
            return buffer != null ? BitConverter.ToInt64(buffer, 0) : 0L;
        }

        public bool ReadBool(nint address)
        {
            return ReadByte(address) != 0;
        }

        public nint ReadPointer(nint address)
        {
            byte[] buffer = ReadMemoryRaw(address, nint.Size);
            return buffer != null ? new nint(BitConverter.ToInt64(buffer, 0)) : 0;
        }

        public string ReadString(nint address, int maxLength = 256, Encoding encoding = null)
        {
            if (!IsConnected || maxLength <= 0) return string.Empty;

            encoding ??= Encoding.UTF8;
            byte[] buffer = ReadMemoryRaw(address, maxLength);
            if (buffer == null) return string.Empty;

            int nullTerminator = Array.IndexOf(buffer, (byte)0);
            if (nullTerminator >= 0)
            {
                Array.Resize(ref buffer, nullTerminator);
            }

            return encoding.GetString(buffer);
        }

        public T ReadStructure<T>(nint address) where T : struct
        {
            int size = Marshal.SizeOf<T>();
            byte[] buffer = ReadMemoryRaw(address, size);
            if (buffer == null) return default;

            GCHandle handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
            try
            {
                return Marshal.PtrToStructure<T>(handle.AddrOfPinnedObject());
            }
            finally
            {
                handle.Free();
            }
        }

        public T[] ReadStructureArray<T>(nint address, int count) where T : struct
        {
            if (count <= 0) return new T[0];

            int structSize = Marshal.SizeOf<T>();
            int totalSize = structSize * count;
            byte[] buffer = ReadMemoryRaw(address, totalSize);
            if (buffer == null) return new T[0];

            T[] result = new T[count];
            for (int i = 0; i < count; i++)
            {
                byte[] structBuffer = new byte[structSize];
                Array.Copy(buffer, i * structSize, structBuffer, 0, structSize);

                GCHandle handle = GCHandle.Alloc(structBuffer, GCHandleType.Pinned);
                try
                {
                    result[i] = Marshal.PtrToStructure<T>(handle.AddrOfPinnedObject());
                }
                finally
                {
                    handle.Free();
                }
            }

            return result;
        }

        #endregion

        #region 최적화된 메모리 읽기 메서드들

        public bool ReadMemoryIntoBuffer(nint address, byte[] buffer, int size)
        {
            if (buffer == null || size <= 0 || size > buffer.Length)
                return false;

            if (!IsConnected)
                return false;

            if (_useOptimizations)
            {
                return MemoryAPI.ReadProcessMemory(_processHandle, address, buffer, size, out _);
            }
            else
            {
                var data = ReadMemoryRaw(address, size);
                if (data != null && data.Length <= buffer.Length)
                {
                    Array.Copy(data, buffer, data.Length);
                    return true;
                }
                return false;
            }
        }

        public unsafe bool ReadStructureDirect<T>(nint address, out T result) where T : unmanaged
        {
            result = default;

            if (!IsConnected)
                return false;

            if (_useOptimizations)
            {
                int size = sizeof(T);
                fixed (T* ptr = &result)
                {
                    return MemoryAPI.ReadProcessMemory(_processHandle, address, (nint)ptr, size, out _);
                }
            }
            else
            {
                try
                {
                    int size = sizeof(T);
                    byte[] buffer = ReadMemoryRaw(address, size);
                    if (buffer != null && buffer.Length == size)
                    {
                        fixed (byte* bufferPtr = buffer)
                        {
                            result = *(T*)bufferPtr;
                            return true;
                        }
                    }
                }
                catch
                {
                }
                return false;
            }
        }

        public unsafe bool ReadStructureArrayIntoBuffer<T>(nint address, T[] buffer, int count) where T : unmanaged
        {
            if (buffer == null || count <= 0 || count > buffer.Length)
                return false;

            if (!IsConnected)
                return false;

            if (_useOptimizations)
            {
                int structSize = sizeof(T);
                int totalSize = structSize * count;

                fixed (T* ptr = buffer)
                {
                    return MemoryAPI.ReadProcessMemory(_processHandle, address, (nint)ptr, totalSize, out _);
                }
            }
            else
            {
                try
                {
                    int structSize = sizeof(T);
                    int totalSize = structSize * count;
                    byte[] rawBuffer = ReadMemoryRaw(address, totalSize);

                    if (rawBuffer != null && rawBuffer.Length >= totalSize)
                    {
                        fixed (byte* rawPtr = rawBuffer)
                        fixed (T* bufferPtr = buffer)
                        {
                            for (int i = 0; i < count; i++)
                            {
                                bufferPtr[i] = *((T*)(rawPtr + (i * structSize)));
                            }
                        }
                        return true;
                    }
                }
                catch
                {
                }
                return false;
            }
        }

        #endregion

        #region 스레드 관련 메서드들

        public nint CreateThreadSnapshot()
        {
            if (!IsConnected) return 0;

            return MemoryAPI.CreateToolhelp32Snapshot(
                MemoryAPI.TH32CS_SNAPTHREAD,
                (uint)_process.Id);
        }

        public bool GetFirstThread(nint snapshot, out MemoryAPI.THREADENTRY32 threadEntry)
        {
            threadEntry = new MemoryAPI.THREADENTRY32();
            threadEntry.dwSize = (uint)Marshal.SizeOf(typeof(MemoryAPI.THREADENTRY32));
            return MemoryAPI.Thread32First(snapshot, ref threadEntry);
        }

        public bool GetNextThread(nint snapshot, ref MemoryAPI.THREADENTRY32 threadEntry)
        {
            return MemoryAPI.Thread32Next(snapshot, ref threadEntry);
        }

        public nint GetThread(uint threadId)
        {
            return MemoryAPI.OpenThread(MemoryAPI.THREAD_QUERY_INFORMATION, false, threadId);
        }

        #endregion

        #region 모듈 관련 메서드들

        public nint CreateModuleSnapshot()
        {
            if (!IsConnected) return 0;

            return MemoryAPI.CreateToolhelp32Snapshot(
                MemoryAPI.TH32CS_SNAPMODULE | MemoryAPI.TH32CS_SNAPMODULE32,
                (uint)_process.Id);
        }

        public bool GetFirstModule(nint snapshot, out MemoryAPI.MODULEENTRY32 moduleEntry)
        {
            moduleEntry = new MemoryAPI.MODULEENTRY32();
            moduleEntry.dwSize = (uint)Marshal.SizeOf(typeof(MemoryAPI.MODULEENTRY32));
            return MemoryAPI.Module32First(snapshot, ref moduleEntry);
        }

        public bool GetNextModule(nint snapshot, ref MemoryAPI.MODULEENTRY32 moduleEntry)
        {
            return MemoryAPI.Module32Next(snapshot, ref moduleEntry);
        }

        public bool GetModuleInformation(nint moduleBase, out MemoryAPI.MODULEINFO moduleInfo)
        {
            moduleInfo = new MemoryAPI.MODULEINFO();
            return MemoryAPI.GetModuleInformation(_processHandle, moduleBase, out moduleInfo, (uint)Marshal.SizeOf<MemoryAPI.MODULEINFO>());
        }

        #endregion

        #region 프로세스 관련 메서드들

        public int QueryProcessInformation(out MemoryAPI.PROCESS_BASIC_INFORMATION processInfo)
        {
            processInfo = new MemoryAPI.PROCESS_BASIC_INFORMATION();
            int returnLength = 0;

            return MemoryAPI.NtQueryInformationProcess(
                _processHandle,
                MemoryAPI.ProcessBasicInformation,
                ref processInfo,
                Marshal.SizeOf(typeof(MemoryAPI.PROCESS_BASIC_INFORMATION)),
                ref returnLength);
        }

        public nint GetProcessHandle()
        {
            if (!IsConnected || _processHandle == IntPtr.Zero)
            {
                Console.WriteLine("[MemoryReader] GetProcessHandle: 프로세스에 연결되지 않음");
                return IntPtr.Zero;
            }

            return _processHandle;
        }

        #endregion

        #region 리소스 관리

        public void CloseHandle(nint handle)
        {
            if (handle != 0)
                MemoryAPI.CloseHandle(handle);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_isDisposed) return;

            if (disposing)
            {
                Disconnect();
            }

            _isDisposed = true;
        }

        #endregion
    }
}