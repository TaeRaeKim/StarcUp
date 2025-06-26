using System;
using System.Timers;
using StarcUp.Business.Memory;
using StarcUp.Common.Constants;
using StarcUp.Common.Events;
using Timer = System.Timers.Timer;

namespace StarcUp.Business.InGameStateMonitor
{
    /// <summary>
    /// 포인터 모니터링 서비스
    /// </summary>
    public class InGameStateMonitor : IInGameStateMonitor
    {

        private readonly IMemoryService _memoryService;
        private Timer _monitorTimer;
        private PointerValue _currentValue;
        private bool _isInGame;
        private bool _isMonitoring;
        private bool _isDisposed;


        public event EventHandler<PointerEventArgs> ValueChanged;

        public bool IsMonitoring => _isMonitoring;
        public PointerValue CurrentValue => _currentValue;
        public bool IsInGame => _isInGame;

        public InGameStateMonitor(IMemoryService memoryService)
        {
            _memoryService = memoryService ?? throw new ArgumentNullException(nameof(memoryService));
            _memoryService.ProcessConnect += OnProcessConnect;
            _memoryService.ProcessDisconnect += OnProcessDisConnect;
        }

        public void StartMonitoring(int processId)
        {
            if (_isMonitoring)
                return;

            Console.WriteLine($"포인터 모니터링 시작: PID {processId}");

            try
            {
                // Peb 주소 가져오기
                _pebAddress = _memoryService.GetPebAddress();
                _user32Address = _memoryService.GetUser32Address();
                
                _B = _pebAddress + 0x828;


                if (_pebAddress == 0)
                {
                    Console.WriteLine("PebAddress 주소를 가져올 수 없습니다.");
                    return;
                }

                Console.WriteLine($"PebAddress 주소: 0x{_pebAddress:X16}");

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
            _pebAddress = 0;
            _currentValue = null;

            _isMonitoring = false;
            Console.WriteLine("포인터 모니터링 중지됨");
        }

        private void MonitorPointerValue(object sender, ElapsedEventArgs e)
        {
            try
            {
                if (!_memoryService.IsConnected || _pebAddress == 0)
                {
                    StopMonitoring();
                    return;
                }

                // 임시로 더미 값을 생성
                var newValue = GenerateDummyPointerValue();

                if (_currentValue == null)
                {
                    _currentValue = newValue;
                }
                else if (_currentValue.NewValue != newValue.NewValue)
                {
                    var pointerValue = new PointerValue(_currentValue.NewValue, newValue.NewValue, _pebAddress);
                    _currentValue = pointerValue;

                    var eventArgs = new PointerEventArgs(pointerValue, GameConstants.EventTypes.POINTER_VALUE_CHANGED);
                    Console.WriteLine(eventArgs);
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
            return new PointerValue(0, value, _pebAddress);
        }

        private void OnProcessConnect(object sender, ProcessEventArgs e)
        {
            if (e.ProcessId <= 0)
            {
                Console.WriteLine("유효하지 않은 프로세스 ID입니다.");
                return;
            }
            Console.WriteLine($"프로세스 연결됨: PID {e.ProcessId}");
            StartMonitoring(e.ProcessId);
        }

        private void OnProcessDisConnect(object sender, ProcessEventArgs e)
        {
            Console.WriteLine($"프로세스 연결 해제됨: PID {e.ProcessId}");
            StopMonitoring();
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