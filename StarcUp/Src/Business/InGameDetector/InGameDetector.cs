using StarcUp.Business.GameDetection;
using StarcUp.Business.MemoryService;
using StarcUp.Common.Constants;
using StarcUp.Common.Events;
using System;

namespace StarcUp.Business.InGameDetector
{
    public class InGameDetector : IInGameDetector
    {
        private readonly IMemoryService _memoryService;
        private bool _isMonitoring;
        private System.Timers.Timer _monitorTimer;

        private nint _cachedInGameAddress; // 캐싱된 InGame 상태 주소
        private bool _isAddressCached; // 주소 캐싱 여부

        public event EventHandler<InGameEventArgs> InGameStateChanged;

        public bool IsInGame { get; private set; }

        public InGameDetector(IMemoryService memoryService)
        {
            _memoryService = memoryService ?? throw new ArgumentNullException(nameof(memoryService));
            _memoryService.ProcessConnect += OnProcessConnect;
            _memoryService.ProcessDisconnect += OnProcessDisconnect;
        }


        public void StartMonitoring(int processId)
        {
            if (_isMonitoring)
                return;

            Console.WriteLine($"InGame 상태 모니터링 시작: PID {processId}");

            try
            {
                if (GetIsAddress() == 0)
                {
                    Console.WriteLine($"[InGameStateMonitor] ❌ InGame 상태 주소를 캐싱할 수 없습니다. 모니터링을 중지합니다.");
                    return;
                }

                _monitorTimer = new System.Timers.Timer(500);
                _monitorTimer.Elapsed += CheckInGameState;
                _monitorTimer.Start();

                _isMonitoring = true;
                Console.WriteLine("InGame 상태 모니터링 활성화됨");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"InGame 모니터링 시작 실패: {ex.Message}");
                StopMonitoring();
            }
        }

        public void StopMonitoring()
        {
            if (!_isMonitoring)
                return;

            Console.WriteLine("InGame 상태 모니터링 중지...");

            _monitorTimer?.Stop();
            _monitorTimer?.Dispose();
            _monitorTimer = null;

            IsInGame = false;

            _isMonitoring = false;
            Console.WriteLine("InGame 상태 모니터링 중지됨");
        }

        private void CheckInGameState(object sender, System.Timers.ElapsedEventArgs e)
        {
            try
            {
                if (!_memoryService.IsConnected)
                {
                    StopMonitoring();
                    return;
                }

                bool newInGameState = ReadInGameState();

                if (newInGameState != IsInGame)
                {
                    IsInGame = newInGameState;
                    Console.WriteLine($"InGame 상태 변경: {IsInGame}");

                    var eventArgs = new InGameEventArgs(IsInGame);
                    InGameStateChanged?.Invoke(this, eventArgs);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"InGame 상태 확인 중 오류: {ex.Message}");
            }
        }

        private bool ReadInGameState()
        {
            try
            {
                var finalAddress = GetIsAddress(); ;

                bool isInGame = _memoryService.ReadBool(finalAddress);

                return isInGame;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"InGame 상태 읽기 실패: {ex.Message}");
                _isAddressCached = false; // 오류 발생 시 캐시 무효화
                return false;
            }
        }

        private nint GetIsAddress()
        {
            if (_isAddressCached && _cachedInGameAddress != 0)
            {
                return _cachedInGameAddress;
            }

            Console.WriteLine("[InGameDetector] InGame 주소 계산 중...");

            // 1. TEB[0] 주소 가져오기 (첫 번째 스레드 = 메인 스레드)
            var tebList = _memoryService.GetTebAddresses();
            if (tebList.Count == 0)
            {
                Console.WriteLine("TEB 주소를 가져올 수 없습니다.");
                return 0;
            }

            // 2. TEB[0] 주소 직접 사용 (TEB + 0x30 값이 TEB 자체 주소이므로)
            nint tebBaseAddress = tebList[0].TebAddress;
            Console.WriteLine($"  - TEB[0]: 0x{tebBaseAddress:X}");

            // 3. B 값 계산
            nint bValue = CalculateBValue();
            if (bValue == 0)
            {
                Console.WriteLine("B 값을 계산할 수 없습니다.");
                return 0;
            }
            Console.WriteLine($"  - B값: 0x{bValue:X}");

            // 4. 최종 주소 계산: [[TEB + 0x828] + [B] + 0xD8] + 0xB4
            nint step1 = _memoryService.ReadPointer(tebBaseAddress + 0x828);
            if (step1 == 0)
            {
                Console.WriteLine("Step1 포인터 읽기 실패");
                return 0;
            }

            nint step2 = _memoryService.ReadPointer(step1 + bValue + 0xD8);
            if (step2 == 0)
            {
                Console.WriteLine("Step2 포인터 읽기 실패");
                return 0;
            }

            nint finalAddress = step2 + 0xB4;
            Console.WriteLine($"  - 최종주소: 0x{finalAddress:X}");

            // 5. 주소 캐싱
            _cachedInGameAddress = finalAddress;
            _isAddressCached = true;

            return finalAddress;
        }

        private nint CalculateBValue()
        {
            try
            {
                var _starcraftModule = _memoryService.GetStarCraftModule();
                var _user32Module = _memoryService.GetUser32Module();

                if (_starcraftModule == null || _user32Module == null)
                {
                    Console.WriteLine("모듈 정보가 없습니다.");
                    return 0;
                }

                nint rcx = _starcraftModule.BaseAddress + 0x10B1838;

                ushort cx = (ushort)_memoryService.ReadShort(rcx);
                nint edx = cx;

                nint user32Factor = _user32Module.BaseAddress + 0xD52B0;
                int factorValue = _memoryService.ReadInt(user32Factor);
                edx = edx * factorValue;

                nint user32Offset = _user32Module.BaseAddress + 0xD52A8;
                nint offsetValue = _memoryService.ReadPointer(user32Offset);
                nint bAddress = edx + offsetValue;
                nint bValue = _memoryService.ReadPointer(bAddress);

                Console.WriteLine($"B 계산: rcx=0x{rcx:X}, cx=0x{cx:X}, factor={factorValue}, offset={offsetValue}, B=0x{bAddress:X}");

                return bValue;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"B 값 계산 실패: {ex.Message}");
                return 0;
            }
        }

        private void OnProcessConnect(object sender, ProcessEventArgs e)
        {
            Console.WriteLine($"InGameMonitoring: 프로세스 연결됨 (PID: {e.ProcessId})");
            StartMonitoring(e.ProcessId);
        }

        private void OnProcessDisconnect(object sender, ProcessEventArgs e)
        {
            Console.WriteLine("InGameMonitoring: 프로세스 연결 해제됨");
            _isAddressCached = false; // 캐시 무효화
            _cachedInGameAddress = 0;
            StopMonitoring();
        }

        public void Dispose()
        {
            StopMonitoring();
            _cachedInGameAddress = 0;
            _isAddressCached = false;

            if (_memoryService != null)
            {
                _memoryService.ProcessConnect -= OnProcessConnect;
                _memoryService.ProcessDisconnect -= OnProcessDisconnect;
            }
        }
    }
}