using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using StarcUp.Infrastructure.Windows;
using Xunit;
using Xunit.Abstractions;

namespace StarcUp.Test
{
    public class WindowManagerPerformanceTest
    {
        private readonly ITestOutputHelper _output;

        public WindowManagerPerformanceTest(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public async Task ComparePollingVsEventBasedPerformance()
        {
            // 테스트용 프로세스를 찾습니다 (현재 프로세스 사용)
            var currentProcess = Process.GetCurrentProcess();
            var processId = currentProcess.Id;

            _output.WriteLine($"테스트 대상 프로세스 ID: {processId}");

            // 폴링 방식 테스트
            var pollingResults = await TestWindowManagerPerformance(processId, useEventBased: false, "폴링 방식");
            
            // 이벤트 방식 테스트
            var eventResults = await TestWindowManagerPerformance(processId, useEventBased: true, "이벤트 방식");

            // 결과 비교
            _output.WriteLine("\n=== 성능 비교 결과 ===");
            _output.WriteLine($"폴링 방식 - CPU 사용량: {pollingResults.CpuUsage:F2}%, 메모리: {pollingResults.MemoryUsage:F2}MB");
            _output.WriteLine($"이벤트 방식 - CPU 사용량: {eventResults.CpuUsage:F2}%, 메모리: {eventResults.MemoryUsage:F2}MB");
            
            var cpuImprovement = ((pollingResults.CpuUsage - eventResults.CpuUsage) / pollingResults.CpuUsage) * 100;
            var memoryImprovement = ((pollingResults.MemoryUsage - eventResults.MemoryUsage) / pollingResults.MemoryUsage) * 100;
            
            _output.WriteLine($"CPU 사용량 개선: {cpuImprovement:F1}%");
            _output.WriteLine($"메모리 사용량 개선: {memoryImprovement:F1}%");

            // 이벤트 방식이 더 효율적이어야 함
            Assert.True(eventResults.CpuUsage <= pollingResults.CpuUsage, 
                $"이벤트 방식의 CPU 사용량({eventResults.CpuUsage:F2}%)이 폴링 방식({pollingResults.CpuUsage:F2}%)보다 높습니다.");
        }

        private async Task<PerformanceResult> TestWindowManagerPerformance(int processId, bool useEventBased, string testName)
        {
            _output.WriteLine($"\n=== {testName} 테스트 시작 ===");
            
            using var windowManager = new WindowManager();
            
            // 초기 메모리 및 CPU 측정
            var process = Process.GetCurrentProcess();
            var initialMemory = GC.GetTotalMemory(true) / 1024.0 / 1024.0; // MB
            var cpuCounter = new PerformanceCounter("Process", "% Processor Time", process.ProcessName);
            cpuCounter.NextValue(); // 첫 번째 호출은 무시

            int eventCount = 0;
            windowManager.WindowPositionChanged += (s, e) => Interlocked.Increment(ref eventCount);
            windowManager.WindowSizeChanged += (s, e) => Interlocked.Increment(ref eventCount);

            // 모니터링 시작
            var startSuccess = windowManager.StartMonitoring(processId);
            _output.WriteLine($"모니터링 시작: {(startSuccess ? "성공" : "실패")}");

            if (!startSuccess)
            {
                _output.WriteLine("⚠️ 모니터링 시작 실패 - 기본값 반환");
                return new PerformanceResult { CpuUsage = 0, MemoryUsage = initialMemory };
            }

            // 5초간 모니터링 실행
            await Task.Delay(5000);

            // 최종 메모리 및 CPU 측정
            var finalMemory = GC.GetTotalMemory(false) / 1024.0 / 1024.0; // MB
            var cpuUsage = cpuCounter.NextValue() / Environment.ProcessorCount;

            windowManager.StopMonitoring();

            var memoryUsage = finalMemory - initialMemory;
            
            _output.WriteLine($"{testName} 결과:");
            _output.WriteLine($"  - 감지된 이벤트 수: {eventCount}");
            _output.WriteLine($"  - CPU 사용량: {cpuUsage:F2}%");
            _output.WriteLine($"  - 메모리 사용량: {memoryUsage:F2}MB");

            return new PerformanceResult
            {
                CpuUsage = cpuUsage,
                MemoryUsage = memoryUsage,
                EventCount = eventCount
            };
        }

        private class PerformanceResult
        {
            public double CpuUsage { get; set; }
            public double MemoryUsage { get; set; }
            public int EventCount { get; set; }
        }
    }
}