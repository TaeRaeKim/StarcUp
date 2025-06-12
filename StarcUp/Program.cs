using System;
using System.Windows.Forms;

namespace StarcUp
{
    public static class Program
    {
        private static PointerMonitor pointerMonitor;

        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            Console.WriteLine("스타크래프트 오버레이 프로그램 시작");
            Console.WriteLine("스타크래프트를 실행하면 포인터 값 모니터링이 시작됩니다.");
            Console.WriteLine("포인터 값이 변경될 때마다 자동으로 출력됩니다.");
            Console.WriteLine();

            // 포인터 모니터 초기화
            pointerMonitor = new PointerMonitor();

            // 포인터 값 변경 이벤트 구독
            pointerMonitor.ValueChanged += OnPointerValueChanged;

            // 오버레이 폼을 생성하되 메인 폼으로 실행하지 않음
            OverlayForm overlayForm = new OverlayForm();

            // 스타크래프트 감지 시 포인터 모니터링 시작
            overlayForm.GameFound += OnGameFound;
            overlayForm.GameLost += OnGameLost;

            // 메시지 루프만 실행 (폼을 표시하지 않음)
            Application.Run();

            // 프로그램 종료 시 정리
            pointerMonitor?.Dispose();
        }

        private static void OnGameFound(IntPtr gameWindow)
        {
            Console.WriteLine("=== 스타크래프트 감지됨 ===");
            Console.WriteLine("포인터 모니터링을 시작합니다...");
            pointerMonitor?.StartMonitoring();
        }

        private static void OnGameLost()
        {
            Console.WriteLine("=== 스타크래프트 종료됨 ===");
            Console.WriteLine("포인터 모니터링을 중지합니다.");
            pointerMonitor?.StopMonitoring();
        }

        private static void OnPointerValueChanged(int oldValue, int newValue)
        {
            // 포인터 값 변경 시 추가 정보 출력 (선택사항)
            Console.WriteLine($"[변경 감지] {oldValue} → {newValue} (차이: {newValue - oldValue:+#;-#;0})");
        }
    }
}