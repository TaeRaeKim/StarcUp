using StarcUp.Common.Events;
using StarcUp.Infrastructure.Memory;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace StarcUp.Business.Memory
{
    /// <summary>
    /// 메모리 서비스
    /// </summary>
    public class MemoryService : IMemoryService
    {
        public event EventHandler<ProcessEventArgs> ProcessConnect;
        public event EventHandler<ProcessEventArgs> ProcessDisconnect;

        private readonly IMemoryReader _memoryReader;
        private bool _isDisposed;

        public bool IsConnected => _memoryReader.IsConnected;
        public int ConnectedProcessId => _memoryReader.ConnectedProcessId;

        private nint _pebAddress;
        private nint _user32Address;
        private nint _A, _B;

        public MemoryService(IMemoryReader memoryReader)
        {
            _memoryReader = memoryReader ?? throw new ArgumentNullException(nameof(memoryReader));
        }

        public bool ConnectToProcess(int processId)
        {
            try
            {
                Console.WriteLine($"프로세스 연결 시도: PID {processId}");
                bool success = _memoryReader.ConnectToProcess(processId);

                if (success)
                {
                    InitPointer();
                    Console.WriteLine($"프로세스 연결 성공: PID {processId}");
                    ProcessConnect.Invoke(this, new ProcessEventArgs(processId));
                }
                else
                {
                    Console.WriteLine($"프로세스 연결 실패: PID {processId}");
                }

                return success;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"프로세스 연결 중 오류: {ex.Message}");
                return false;
            }
        }

        public void Disconnect()
        {
            try
            {
                ProcessDisconnect.Invoke(this, new ProcessEventArgs(0, "ProcessDisConnect"));
                _memoryReader.Disconnect();
                Console.WriteLine("프로세스 연결 해제됨");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"프로세스 연결 해제 중 오류: {ex.Message}");
            }
        }
        private void InitPointer()
        {
            _pebAddress = _memoryReader.GetPebAddress();
            _memoryReader.GetUser32ModuleInfo(out MemoryAPI.MODULEENTRY32 moduleInfo);
            _user32Address = moduleInfo.modBaseAddr;
            _A = _pebAddress + 0x828;
            Console.WriteLine(_memoryReader.ReadPointer(_A));
        }

        public nint GetPebAddress()
        {
            if (!IsConnected)
            {
                Console.WriteLine("프로세스가 연결되지 않음");
                return 0;
            }
            try
            {
                return _memoryReader.GetPebAddress();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"PEB 주소 가져오기 실패: {ex.Message}");
                return 0;
            }
        }
        public nint GetUser32Address()
        {
            if (!IsConnected)
            {
                Console.WriteLine("프로세스가 연결되지 않음");
                return 0;
            }

            try
            {
                if (_memoryReader.GetUser32ModuleInfo(out MemoryAPI.MODULEENTRY32 moduleInfo))
                {
                    Console.WriteLine($"User32.dll 베이스 주소: 0x{moduleInfo.modBaseAddr.ToInt64():X16}");
                    return moduleInfo.modBaseAddr;
                }
                else
                {
                    Console.WriteLine("User32.dll 모듈을 찾을 수 없음");
                    return 0;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"User32.dll 주소 가져오기 실패: {ex.Message}");
                return 0;
            }
        }

        public nint GetStackStart(int threadIndex = 0)
        {
            if (!IsConnected)
            {
                Console.WriteLine("프로세스가 연결되지 않음");
                return 0;
            }

            try
            {
                return _memoryReader.GetStackStart(threadIndex);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"StackStart 가져오기 실패: {ex.Message}");
                return 0;
            }
        }

        public List<TebInfo> GetTebAddresses()
        {
            if (!IsConnected)
            {
                Console.WriteLine("프로세스가 연결되지 않음");
                return new List<TebInfo>();
            }

            try
            {
                return _memoryReader.GetTebAddresses();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"TEB 주소 가져오기 실패: {ex.Message}");
                return new List<TebInfo>();
            }
        }

        public void Dispose()
        {
            if (_isDisposed)
                return;

            Disconnect();
            _memoryReader?.Dispose();
            _isDisposed = true;
        }
    }
}