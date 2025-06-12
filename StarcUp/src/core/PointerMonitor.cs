using System;

namespace StarcUp
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("스타크래프트 TEB 주소 조회");
            Console.WriteLine("========================");

            MemoryReader reader = new MemoryReader();

            // 스타크래프트 연결
            if (!reader.ConnectToStarcraft())
            {
                Console.WriteLine("연결 실패");
                Console.ReadKey();
                return;
            }

            // TEB 주소들 가져오기
            var tebAddresses = reader.GetTebAddresses();

            if (tebAddresses.Count > 0)
            {
                Console.WriteLine("\n=== TEB 주소 목록 (처음 5개만) ===");
                for (int i = 0; i < Math.Min(5, tebAddresses.Count); i++)
                {
                    var teb = tebAddresses[i];
                    Console.WriteLine($"TEB{teb.Index}: 0x{teb.TebAddress.ToInt64():X16} (스레드 ID: {teb.ThreadId})");
                }

                // 첫 번째 스레드의 StackTop 가져오기 (Pascal 코드 방식)
                Console.WriteLine("\n=== 간단한 StackTop 가져오기 ===");
                IntPtr stackTop = reader.GetStackTop(0); // 첫 번째 스레드

                if (stackTop != IntPtr.Zero)
                {
                    Console.WriteLine($"첫 번째 스레드의 StackTop: 0x{stackTop.ToInt64():X16}");
                }

                // Pascal 코드의 전체 로직 실행 (GetStackStart)
                Console.WriteLine("\n=== Pascal 코드 전체 로직 (GetStackStart) ===");
                IntPtr stackStart = reader.GetStackStart(0); // 첫 번째 스레드

                if (stackStart != IntPtr.Zero)
                {
                    Console.WriteLine($"계산된 StackStart: 0x{stackStart.ToInt64():X16}");
                    Console.WriteLine($"예상 주소와 비교: 0x000000A3989DFBE8");

                    long expected = 0x000000A3989DFBE8;
                    long actual = stackStart.ToInt64();
                    long difference = Math.Abs(actual - expected);

                    Console.WriteLine($"차이: 0x{difference:X} ({difference} 바이트)");

                    if (difference < 1000)
                    {
                        Console.WriteLine("✅ 매우 근접한 결과!");
                    }
                    else if (difference < 10000)
                    {
                        Console.WriteLine("⚠️ 어느 정도 근접한 결과");
                    }
                    else
                    {
                        Console.WriteLine("❌ 차이가 많이 남");
                    }
                }
                else
                {
                    Console.WriteLine("StackStart를 계산할 수 없습니다.");
                }
            }
            else
            {
                Console.WriteLine("TEB 주소를 찾을 수 없습니다.");
            }

            Console.WriteLine("\n아무 키나 누르면 종료...");
            Console.Read();

            reader.Disconnect();
        }
    }
}