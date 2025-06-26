using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace StarcUp.Infrastructure.Memory
{
    /// <summary>
    /// Windows API를 직접 사용하여 메모리를 읽는 저수준 클래스
    /// - Windows API와의 직접적인 접촉만 담당
    /// - 에러 처리나 유효성 검사는 최소한으로만 수행
    /// </summary>
    public class MemoryReader : IMemoryReader
    {
        private Process _process;
        protected nint _processHandle;
        private bool _isDisposed;

        public bool IsConnected => _processHandle != 0 && _process != null && !_process.HasExited;
        public int ConnectedProcessId => _process?.Id ?? 0;

        public bool ConnectToProcess(int processId)
        {
            try
            {
                Disconnect();
                _process = Process.GetProcessById(processId);
                _processHandle = MemoryAPI.OpenProcess(
                    MemoryAPI.PROCESS_QUERY_INFORMATION | MemoryAPI.PROCESS_VM_READ,
                    false, processId);
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
            _process = null;
        }

        public byte[] ReadMemoryRaw(nint address, int size)
        {
            if (!IsConnected || size <= 0)
                return null;

            byte[] buffer = new byte[size];
            return ReadProcessMemory(address, buffer, size) ? buffer : null;
        }

        protected bool ReadProcessMemory(nint address, byte[] buffer, int size)
        {
            return MemoryAPI.ReadProcessMemory(_processHandle, address, buffer, size, out _);
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
            if (buffer == null) return 0;

            return nint.Size == 8
                ? new nint(BitConverter.ToInt64(buffer, 0))
                : new nint(BitConverter.ToInt32(buffer, 0));
        }

        public string ReadString(nint address, int maxLength = 256, Encoding encoding = null)
        {
            encoding ??= Encoding.UTF8;
            byte[] buffer = ReadMemoryRaw(address, maxLength);
            if (buffer == null) return string.Empty;

            int nullIndex = Array.IndexOf(buffer, (byte)0);
            if (nullIndex >= 0)
                Array.Resize(ref buffer, nullIndex);

            return encoding.GetString(buffer);
        }

        public T ReadStructure<T>(nint address) where T : struct
        {
            int size = Marshal.SizeOf<T>();
            byte[] buffer = ReadMemoryRaw(address, size);
            if (buffer == null) return default(T);

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
                byte[] structBytes = new byte[structSize];
                Array.Copy(buffer, i * structSize, structBytes, 0, structSize);

                GCHandle handle = GCHandle.Alloc(structBytes, GCHandleType.Pinned);
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

        public nint CreateThreadSnapshot()
        {
            return MemoryAPI.CreateToolhelp32Snapshot(MemoryAPI.TH32CS_SNAPTHREAD, 0);
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
            nint threadHandle = MemoryAPI.OpenThread(MemoryAPI.THREAD_QUERY_INFORMATION, false, threadId);
            if (threadHandle == 0) return 0;

            try
            {
                if (MemoryAPI.NtQueryInformationThread(
                    threadHandle,
                    MemoryAPI.ThreadBasicInformation,
                    out var basicInfo,
                    Marshal.SizeOf(typeof(MemoryAPI.THREAD_BASIC_INFORMATION)),
                    0) == 0)
                {
                    return basicInfo.TebBaseAddress;
                }
            }
            catch { }
            finally
            {
                MemoryAPI.CloseHandle(threadHandle);
            }

            return 0;
        }

        public nint CreateModuleSnapshot()
        {
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

        public void CloseHandle(nint handle)
        {
            if (handle != 0)
                MemoryAPI.CloseHandle(handle);
        }

        public void Dispose()
        {
            if (_isDisposed) return;
            Disconnect();
            _isDisposed = true;
        }
    }
}