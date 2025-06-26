using StarcUp.Business.MemoryService;
using StarcUp.Common.Constants;
using StarcUp.Common.Events;
using System;

namespace StarcUp.Business.InGameStateMonitor
{
    /// <summary>
    /// 게임 내 상태를 모니터링하는 서비스
    /// </summary>
    public class InGameStateMonitor : IInGameStateMonitor
    {
        private readonly IMemoryService _memoryService;
        private bool _isMonitoring;
        private System.Timers.Timer _monitorTimer;

        // 캐시된 모듈 정보들
        private ModuleInfo _starcraftModule;
        private ModuleInfo _user32Module;

        public event EventHandler<InGameStateEventArgs> InGameStateChanged;

        public bool IsInGame { get; private set; }

        public InGameStateMonitor(IMemoryService memoryService)
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
                // 필요한 모듈 정보 캐시
                CacheModuleInformation();

                // 모니터링 타이머 설정 (500ms 간격)
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

            _starcraftModule = null;
            _user32Module = null;
            IsInGame = false;

            _isMonitoring = false;
            Console.WriteLine("InGame 상태 모니터링 중지됨");
        }

        private void CacheModuleInformation()
        {
            // StarCraft.exe 모듈 정보 가져오기 - MemoryService의 FindModule 사용
            if (_memoryService.FindModule("StarCraft.exe", out var starcraftModuleInfo) ||
                _memoryService.FindModule("StarCraft", out starcraftModuleInfo))
            {
                _starcraftModule = starcraftModuleInfo;
                Console.WriteLine($"StarCraft 모듈 찾음: Base=0x{_starcraftModule.BaseAddress:X}");
            }
            else
            {
                Console.WriteLine("StarCraft 모듈을 찾을 수 없습니다.");
            }

            // USER32.dll 모듈 정보 가져오기 - MemoryService의 GetUser32Module 사용
            _user32Module = _memoryService.GetUser32Module();
            if (_user32Module != null)
            {
                Console.WriteLine($"USER32 모듈 찾음: Base=0x{_user32Module.BaseAddress:X}");
            }
            else
            {
                Console.WriteLine("USER32 모듈을 찾을 수 없습니다.");
            }
        }

        private void CheckInGameState(object sender, System.Timers.ElapsedEventArgs e)
        {
            try
            {
                if (!_memoryService.IsConnected || _starcraftModule == null || _user32Module == null)
                {
                    StopMonitoring();
                    return;
                }

                bool newInGameState = ReadInGameState();

                if (newInGameState != IsInGame)
                {
                    IsInGame = newInGameState;
                    Console.WriteLine($"InGame 상태 변경: {IsInGame}");

                    var eventArgs = new InGameStateEventArgs(IsInGame);
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
                // 1. PEB 주소 가져오기
                nint pebAddress = _memoryService.GetPebAddress();
                if (pebAddress == 0)
                {
                    Console.WriteLine("PEB 주소를 가져올 수 없습니다.");
                    return false;
                }

                // 2. B 값 계산
                nint bValue = CalculateBValue();
                if (bValue == 0)
                {
                    Console.WriteLine("B 값을 계산할 수 없습니다.");
                    return false;
                }

                // 3. 최종 주소 계산: [[PEB Address + 0x828] + [B] + 0xD8] + 0xB4
                nint step1 = _memoryService.ReadPointer(pebAddress + 0x828);
                if (step1 == 0)
                {
                    Console.WriteLine("Step1 포인터 읽기 실패");
                    return false;
                }

                nint step2 = _memoryService.ReadPointer(step1 + bValue + 0xD8);
                if (step2 == 0)
                {
                    Console.WriteLine("Step2 포인터 읽기 실패");
                    return false;
                }

                nint finalAddress = step2 + 0xB4;

                // 4. InGame 상태 값 읽기
                int inGameValue = _memoryService.ReadInt(finalAddress);

                // InGame 상태는 보통 1이면 게임 중, 0이면 게임 밖
                bool isInGame = inGameValue == 1;

                Console.WriteLine($"InGame 체크: PEB=0x{pebAddress:X}, B=0x{bValue:X}, Final=0x{finalAddress:X}, Value={inGameValue}, InGame={isInGame}");

                return isInGame;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"InGame 상태 읽기 실패: {ex.Message}");
                return false;
            }
        }

        private nint CalculateBValue()
        {
            try
            {
                if (_starcraftModule == null || _user32Module == null)
                {
                    Console.WriteLine("모듈 정보가 없습니다.");
                    return 0;
                }

                // 1. rcx = StarCraft.exe+10B1838
                nint rcx = _starcraftModule.BaseAddress + 0x10B1838;

                // 2. movzx edx, cx (cx의 하위 16비트를 edx에 복사)
                ushort cx = (ushort)_memoryService.ReadShort(rcx);
                nint edx = cx;

                // 3. edx = edx * [USER32.dll+D52B0]
                nint user32Factor = _user32Module.BaseAddress + 0xD52B0;
                int factorValue = _memoryService.ReadInt(user32Factor);
                edx = edx * factorValue;

                // 4. B = edx + [USER32.dll+D52A8]
                nint user32Offset = _user32Module.BaseAddress + 0xD52A8;
                nint offsetValue = _memoryService.ReadPointer(user32Offset);
                nint bValue = edx + offsetValue;

                Console.WriteLine($"B 계산: rcx=0x{rcx:X}, cx=0x{cx:X}, factor={factorValue}, offset={offsetValue}, B=0x{bValue:X}");

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
            // 프로세스가 연결되면 자동으로 모니터링 시작하지 않음
            // 외부에서 명시적으로 StartMonitoring 호출해야 함
            StartMonitoring(e.ProcessId);
        }

        private void OnProcessDisconnect(object sender, ProcessEventArgs e)
        {
            Console.WriteLine("InGameMonitoring: 프로세스 연결 해제됨");
            StopMonitoring();
        }

        public void Dispose()
        {
            StopMonitoring();

            if (_memoryService != null)
            {
                _memoryService.ProcessConnect -= OnProcessConnect;
                _memoryService.ProcessDisconnect -= OnProcessDisconnect;
            }
        }
    }
}
