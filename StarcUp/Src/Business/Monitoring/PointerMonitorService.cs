using System;
using System.Timers;
using StarcUp.Business.Memory;
using StarcUp.Common.Constants;
using StarcUp.Common.Events;
using Timer = System.Timers.Timer;

namespace StarcUp.Business.Monitoring
{
    /// <summary>
    /// 포인터 모니터링 서비스
    /// </summary>
    public class PointerMonitorService : IPointerMonitorService
    {
        private readonly IProcessConnector _memoryService;
        private Timer _monitorTimer;
        private PointerValue _currentValue;
        private bool _isMonitoring;
        private bool _isDisposed;
        private nint _stackStartAddress;

        public event EventHandler<PointerEventArgs> ValueChanged;

        public bool IsMonitoring => _isMonitoring;
        public PointerValue CurrentValue => _currentValue;

        public PointerMonitorService(IProcessConnector memoryService)
        {
            _memoryService = memoryService ?? throw new ArgumentNullException(nameof(memoryService));
        }

        public void StartMonitoring(int processId)
        {
            if (_isMonitoring)
                return;

            Console.WriteLine($"포인터 모니터링 시작: PID {processId}");

            try
            {
                // 메모리 서비스에 연결
                if (!_memoryService.ConnectToProcess(processId))
                {
                    Console.WriteLine("메모리 서비스 연결 실패");
                    return;
                }

                // StackStart 주소 가져오기
                _stackStartAddress = _memoryService.GetStackStart(0);
                if (_stackStartAddress == 0)
                {
                    Console.WriteLine("StackStart 주소를 가져올 수 없습니다.");
                    return;
                }

                Console.WriteLine($"StackStart 주소: 0x{_stackStartAddress.ToInt64():X16}");

                // 모니터링 타이머 설정
                _monitorTimer = new Timer(GameConstants.POINTER_MONITOR_INTERVAL_MS);
                _monitorTimer.Elapsed += MonitorPointerValue;
                _monitorTimer.Start();

                _isMonitoring = true;
                Console.WriteLine("포인터 모니터링 활성화됨");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"포인터 모니터링 시작 실패: {ex.Message}");
                StopMonitoring();
            }
        }

        public void StopMonitoring()
        {
            if (!_isMonitoring)
                return;

            Console.WriteLine("포인터 모니터링 중지...");

            _monitorTimer?.Stop();
            _monitorTimer?.Dispose();
            _monitorTimer = null;

            _memoryService.Disconnect();
            _stackStartAddress = 0;
            _currentValue = null;

            _isMonitoring = false;
            Console.WriteLine("포인터 모니터링 중지됨");
        }

        private void MonitorPointerValue(object sender, ElapsedEventArgs e)
        {
            try
            {
                if (!_memoryService.IsConnected || _stackStartAddress == 0)
                {
                    StopMonitoring();
                    return;
                }

                // 여기서 실제 포인터 값을 읽어야 하는데, 
                // 현재는 StackStart 주소만 가져오는 상태이므로
                // 임시로 더미 값을 생성
                var newValue = GenerateDummyPointerValue();

                if (_currentValue == null)
                {
                    _currentValue = newValue;
                }
                else if (_currentValue.NewValue != newValue.NewValue)
                {
                    var pointerValue = new PointerValue(_currentValue.NewValue, newValue.NewValue, _stackStartAddress);
                    _currentValue = pointerValue;

                    var eventArgs = new PointerEventArgs(pointerValue, GameConstants.EventTypes.POINTER_VALUE_CHANGED);
                    ValueChanged?.Invoke(this, eventArgs);
                    }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"포인터 모니터링 중 오류: {ex.Message}");
            }
        }

        private PointerValue GenerateDummyPointerValue()
        {
            // 실제 구현에서는 메모리에서 값을 읽어야 함
            // 현재는 테스트용 더미 값 생성
            Random random = new Random();
            int value = random.Next(1000, 9999);
            return new PointerValue(0, value, _stackStartAddress);
        }

        public void Dispose()
        {
            if (_isDisposed)
                return;

            StopMonitoring();
            _memoryService?.Dispose();
            _isDisposed = true;
        }
    }
}