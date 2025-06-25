using System;
using System.Collections.Generic;
using StarcUp.Infrastructure.Memory;

namespace StarcUp.Business.Memory
{
    /// <summary>
    /// 메모리 서비스
    /// </summary>
    public class ProcessConnector : IProcessConnector
    {
        private readonly IMemoryReader _memoryReader;
        private bool _isDisposed;

        public bool IsConnected => _memoryReader.IsConnected;
        public int ConnectedProcessId => _memoryReader.ConnectedProcessId;

        public ProcessConnector(IMemoryReader memoryReader)
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
                    Console.WriteLine($"프로세스 연결 성공: PID {processId}");
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
                _memoryReader.Disconnect();
                Console.WriteLine("프로세스 연결 해제됨");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"프로세스 연결 해제 중 오류: {ex.Message}");
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