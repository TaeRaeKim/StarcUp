using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using StarcUp.Infrastructure.Windows;

namespace StarcUp.Test
{
    class Program
    {
        private static IWindowManager? _windowManager;
        private static bool _isRunning = true;

        static async Task Main(string[] args)
        {
            Console.WriteLine("=== WindowManager 이벤트 기반 테스트 ===");
            Console.WriteLine("스타크래프트 윈도우 위치/크기 변화 감지 테스트 (이벤트 기반)");
            Console.WriteLine();

            // WindowManager 초기화
            InitializeWindowManager();

            // 이벤트 핸들러 등록
            _windowManager.WindowPositionChanged += OnWindowPositionChanged;
            _windowManager.WindowSizeChanged += OnWindowSizeChanged;
            _windowManager.WindowLost += OnWindowLost;

            Console.WriteLine("WindowManager 초기화 완료");
            Console.WriteLine();

            // 메뉴 표시
            ShowMenu();
            
            string? choice;
            do
            {
                Console.Write("\n선택하세요 (1-4): ");
                choice = Console.ReadLine();

                switch (choice)
                {
                    case "1":
                        await TestStarcraftWindowMonitoring();
                        break;
                    case "2":
                        await TestNotepadWindowMonitoring();
                        break;
                    case "3":
                        ShowStatus();
                        break;
                    case "4":
                        Console.WriteLine("프로그램을 종료합니다...");
                        break;
                    default:
                        Console.WriteLine("잘못된 선택입니다. 1-4 중에서 선택해주세요.");
                        break;
                }
            } while (choice != "4" && _isRunning);

            // 정리
            Console.WriteLine("WindowManager 정리 중...");
            _windowManager?.Dispose();
            Console.WriteLine("프로그램이 종료되었습니다.");
        }


        private static void InitializeWindowManager()
        {
            _windowManager = new WindowManager();
            Console.WriteLine("WindowManager 초기화 완료 (이벤트 기반 모니터링)");
            Console.WriteLine();
        }

        private static void ShowMenu()
        {
            Console.WriteLine("=== 테스트 메뉴 ===");
            Console.WriteLine("1. 스타크래프트 윈도우 모니터링");
            Console.WriteLine("2. 메모장 윈도우 모니터링");
            Console.WriteLine("3. 현재 상태 확인");
            Console.WriteLine("4. 종료");
            Console.WriteLine();
        }

        private static async Task TestStarcraftWindowMonitoring()
        {
            Console.WriteLine("\n=== 스타크래프트 윈도우 모니터링 ===");
            Console.WriteLine("스타크래프트를 실행해주세요...");
            
            // 기존 모니터링 중지
            _windowManager?.StopMonitoring();

            bool found = false;
            for (int attempt = 0; attempt < 60 && !found; attempt++)
            {
                // 스타크래프트 프로세스 검색
                var starcraftProcesses = Process.GetProcessesByName("StarCraft");
                if (starcraftProcesses.Length == 0)
                {
                    starcraftProcesses = Process.GetProcessesByName("StarCraft_Original");
                }
                if (starcraftProcesses.Length == 0)
                {
                    starcraftProcesses = Process.GetProcessesByName("starcraft");
                }
                
                foreach (var process in starcraftProcesses)
                {
                    try
                    {
                        Console.WriteLine($"스타크래프트 프로세스 발견: PID {process.Id}");
                        
                        if (_windowManager!.StartMonitoring(process.Id))
                        {
                            Console.WriteLine($"✅ 스타크래프트 윈도우 모니터링 시작");
                            var currentInfo = _windowManager.GetCurrentWindowInfo();
                            if (currentInfo != null)
                            {
                                Console.WriteLine($"📊 현재 윈도우 정보: {currentInfo}");
                            }
                            found = true;
                            break;
                        }
                        else
                        {
                            Console.WriteLine("❌ 모니터링 시작 실패");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"❌ 오류: {ex.Message}");
                    }
                    finally
                    {
                        process.Dispose();
                    }
                }

                if (!found)
                {
                    Console.Write(".");
                    await Task.Delay(1000);
                }
            }

            if (!found)
            {
                Console.WriteLine("\n❌ 스타크래프트 프로세스를 찾을 수 없습니다.");
                return;
            }

            Console.WriteLine("\n🎮 이벤트 기반 모니터링 활성화됨! 스타크래프트 윈도우를 이동하거나 크기를 조정해보세요.");
            
            // 메시지 루프가 시작될 시간을 주고 상태 확인
            await Task.Delay(500);
            Console.WriteLine($"💡 메시지 루프가 별도 스레드에서 실행 중입니다. (스레드 ID: {_windowManager?.MessageLoopThreadId})");
            Console.WriteLine($"📊 메시지 루프 실행 상태: {_windowManager?.IsMessageLoopRunning}");
            Console.WriteLine("ESC 키를 누르면 모니터링을 중지합니다.\n");

            // ESC 키 대기
            ConsoleKeyInfo keyInfo;
            do
            {
                keyInfo = Console.ReadKey(true);
            } while (keyInfo.Key != ConsoleKey.Escape);

            _windowManager?.StopMonitoring();
            Console.WriteLine("✅ 모니터링 중지됨");
        }

        private static async Task TestNotepadWindowMonitoring()
        {
            Console.WriteLine("\n=== 메모장 윈도우 모니터링 ===");
            Console.WriteLine("메모장을 실행해주세요...");
            
            // 기존 모니터링 중지
            _windowManager?.StopMonitoring();

            bool found = false;
            for (int attempt = 0; attempt < 30 && !found; attempt++)
            {
                var notepadProcesses = Process.GetProcessesByName("notepad");
                
                foreach (var process in notepadProcesses)
                {
                    try
                    {
                        Console.WriteLine($"메모장 프로세스 발견: PID {process.Id}");
                        
                        if (_windowManager!.StartMonitoring(process.Id))
                        {
                            Console.WriteLine($"✅ 메모장 윈도우 모니터링 시작");
                            var currentInfo = _windowManager.GetCurrentWindowInfo();
                            if (currentInfo != null)
                            {
                                Console.WriteLine($"📊 현재 윈도우 정보: {currentInfo}");
                            }
                            found = true;
                            break;
                        }
                        else
                        {
                            Console.WriteLine("❌ 모니터링 시작 실패");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"❌ 오류: {ex.Message}");
                    }
                    finally
                    {
                        process.Dispose();
                    }
                }

                if (!found)
                {
                    Console.Write(".");
                    await Task.Delay(1000);
                }
            }

            if (!found)
            {
                Console.WriteLine("\n❌ 메모장 프로세스를 찾을 수 없습니다.");
                return;
            }

            Console.WriteLine("\n📝 이벤트 기반 모니터링 활성화됨! 메모장 윈도우를 이동하거나 크기를 조정해보세요.");
            
            // 메시지 루프가 시작될 시간을 주고 상태 확인
            await Task.Delay(500);
            Console.WriteLine($"💡 메시지 루프가 별도 스레드에서 실행 중입니다. (스레드 ID: {_windowManager?.MessageLoopThreadId})");
            Console.WriteLine($"📊 메시지 루프 실행 상태: {_windowManager?.IsMessageLoopRunning}");
            Console.WriteLine("ESC 키를 누르면 모니터링을 중지합니다.\n");

            // ESC 키 대기
            ConsoleKeyInfo keyInfo;
            do
            {
                keyInfo = Console.ReadKey(true);
            } while (keyInfo.Key != ConsoleKey.Escape);

            _windowManager?.StopMonitoring();
            Console.WriteLine("✅ 모니터링 중지됨");
        }


        private static void ShowStatus()
        {
            Console.WriteLine("\n=== 현재 상태 ===");
            Console.WriteLine("모니터링 방식: 이벤트 기반 (메시지 루프)");
            
            if (_windowManager != null)
            {
                Console.WriteLine($"WindowManager 모니터링 상태: {_windowManager.IsMonitoring}");
                Console.WriteLine($"WindowManager 윈도우 유효성: {_windowManager.IsWindowValid}");
                Console.WriteLine($"메시지 루프 실행 상태: {_windowManager.IsMessageLoopRunning}");
                Console.WriteLine($"메시지 루프 스레드 ID: {_windowManager.MessageLoopThreadId}");
                Console.WriteLine($"현재 스레드 ID: {System.Threading.Thread.CurrentThread.ManagedThreadId}");
                
                var currentInfo = _windowManager.GetCurrentWindowInfo();
                if (currentInfo != null)
                {
                    Console.WriteLine($"현재 윈도우 정보: {currentInfo}");
                }
                else
                {
                    Console.WriteLine("현재 모니터링 중인 윈도우 없음");
                }
            }
            else
            {
                Console.WriteLine("WindowManager가 초기화되지 않음");
            }
            
            Console.WriteLine("\nEnter를 눌러 계속...");
            Console.ReadLine();
        }

        private static void OnWindowPositionChanged(object? sender, WindowChangedEventArgs eventArgs)
        {
            try
            {
                var prev = eventArgs.PreviousWindowInfo;
                var current = eventArgs.CurrentWindowInfo;
                
                Console.WriteLine($"📍 [위치 변경 - 이벤트] '{current.Title}' (PID: {current.ProcessId})");
                Console.WriteLine($"   이전: ({prev.X}, {prev.Y}) → 현재: ({current.X}, {current.Y})");
                
                if (eventArgs.ChangeType == WindowChangeType.BothChanged)
                {
                    Console.WriteLine($"   크기도 함께 변경: {prev.Width}x{prev.Height} → {current.Width}x{current.Height}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 위치 변경 이벤트 처리 중 오류: {ex.Message}");
            }
        }

        private static void OnWindowSizeChanged(object? sender, WindowChangedEventArgs eventArgs)
        {
            try
            {
                var prev = eventArgs.PreviousWindowInfo;
                var current = eventArgs.CurrentWindowInfo;
                
                Console.WriteLine($"📏 [크기 변경 - 이벤트] '{current.Title}' (PID: {current.ProcessId})");
                Console.WriteLine($"   이전: {prev.Width}x{prev.Height} → 현재: {current.Width}x{current.Height}");
                
                if (eventArgs.ChangeType == WindowChangeType.BothChanged)
                {
                    Console.WriteLine($"   위치도 함께 변경: ({prev.X}, {prev.Y}) → ({current.X}, {current.Y})");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 크기 변경 이벤트 처리 중 오류: {ex.Message}");
            }
        }

        private static void OnWindowLost(object? sender, WindowChangedEventArgs eventArgs)
        {
            try
            {
                var windowInfo = eventArgs.PreviousWindowInfo;
                if (windowInfo != null)
                {
                    Console.WriteLine($"❌ [윈도우 손실] '{windowInfo.Title}' (PID: {windowInfo.ProcessId})");
                    Console.WriteLine("   윈도우가 닫히거나 프로세스가 종료됨");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 윈도우 손실 이벤트 처리 중 오류: {ex.Message}");
            }
        }
    }
}