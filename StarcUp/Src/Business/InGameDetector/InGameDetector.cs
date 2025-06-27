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

        private ModuleInfo _starcraftModule;
        private ModuleInfo _user32Module;

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
                CacheModuleInformation();

                if (_starcraftModule == null || _user32Module == null)
                {
                    Console.WriteLine("필수 모듈이 로드되지 않아 모니터링을 시작할 수 없습니다.");
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

            _starcraftModule = null;
            _user32Module = null;
            IsInGame = false;

            _isMonitoring = false;
            Console.WriteLine("InGame 상태 모니터링 중지됨");
        }

        private void CacheModuleInformation()
        {
            Console.WriteLine("[InGameStateMonitor] 모듈 정보 캐싱 시작...");

            Console.WriteLine("[InGameStateMonitor] === 전체 모듈 목록 (치트엔진 스타일) ===");
            _memoryService.DebugAllModulesCheatEngineStyle();

            Console.WriteLine("[InGameStateMonitor] === StarCraft 관련 모듈 검색 ===");
            _memoryService.FindModulesByPattern("star");

            Console.WriteLine("[InGameStateMonitor] === StarCraft 모듈 검색 (치트엔진 스타일) ===");

            // 간소화된 모듈명 목록 (대소문자 구분 없이 처리됨)
            string[] possibleNames = {
                "StarCraft.exe",
                "StarCraft"
            };

            foreach (string moduleName in possibleNames)
            {
                _starcraftModule = _memoryService.FindModuleCheatEngineStyle(moduleName);
                if (_starcraftModule != null)
                {
                    Console.WriteLine($"[InGameStateMonitor] ★ StarCraft 모듈 발견: {_starcraftModule}");
                    break;
                }
            }

            if (_starcraftModule == null)
            {
                Console.WriteLine("[InGameStateMonitor] ❌ StarCraft 모듈을 찾을 수 없습니다.");
            }

            Console.WriteLine("[InGameStateMonitor] === USER32 모듈 검색 ===");
            _user32Module = _memoryService.FindModuleCheatEngineStyle("user32.dll");

            if (_user32Module != null)
            {
                Console.WriteLine($"[InGameStateMonitor] ★ USER32 모듈 발견: {_user32Module}");
            }
            else
            {
                Console.WriteLine("[InGameStateMonitor] ❌ USER32 모듈을 찾을 수 없습니다.");
            }

            Console.WriteLine("[InGameStateMonitor] === 모듈 캐싱 결과 ===");
            Console.WriteLine($"StarCraft 모듈: {(_starcraftModule != null ? "✅ 성공" : "❌ 실패")}");
            Console.WriteLine($"USER32 모듈: {(_user32Module != null ? "✅ 성공" : "❌ 실패")}");

            if (_starcraftModule != null && _user32Module != null)
            {
                Console.WriteLine("[InGameStateMonitor] ✅ 모든 필수 모듈 로드 완료!");
            }
            else
            {
                Console.WriteLine("[InGameStateMonitor] ⚠️ 일부 모듈 로드 실패 - InGame 상태 모니터링이 제한될 수 있습니다.");
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
                // 1. TEB[0] 주소 가져오기 (첫 번째 스레드 = 메인 스레드)
                var tebList = _memoryService.GetTebAddresses();
                if (tebList.Count == 0)
                {
                    Console.WriteLine("TEB 주소를 가져올 수 없습니다.");
                    return false;
                }

                // 2. TEB[0] 주소 직접 사용 (TEB + 0x30 값이 TEB 자체 주소이므로)
                nint tebBaseAddress = tebList[0].TebAddress;

                // 3. B 값 계산
                nint bValue = CalculateBValue();
                if (bValue == 0)
                {
                    Console.WriteLine("B 값을 계산할 수 없습니다.");
                    return false;
                }

                // 4. 최종 주소 계산: [[TEB + 0x828] + [B] + 0xD8] + 0xB4
                nint step1 = _memoryService.ReadPointer(tebBaseAddress + 0x828);
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

                // 5. InGame 상태 값 읽기
                bool isInGame = _memoryService.ReadBool(finalAddress);

                //Console.WriteLine($"InGame 체크: TEB[0]=0x{tebBaseAddress:X}, B=0x{bValue:X}, Final=0x{finalAddress:X}, InGame={isInGame}");

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