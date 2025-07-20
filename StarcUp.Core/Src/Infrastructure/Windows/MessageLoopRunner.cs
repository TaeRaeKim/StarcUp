using System;
using System.Threading;
using System.Threading.Tasks;

namespace StarcUp.Infrastructure.Windows
{
    /// <summary>
    /// Windows 메시지 루프를 관리하는 클래스
    /// </summary>
    public class MessageLoopRunner : IMessageLoopRunner
    {
        private CancellationTokenSource _cancellationTokenSource;
        private Task _messageLoopTask;
        private uint _threadId;
        private bool _isDisposed;

        public bool IsRunning => _messageLoopTask != null && !_messageLoopTask.IsCompleted;
        public uint ThreadId => _threadId;

        public async Task StartAsync(Action onLoopStarted = null)
        {
            if (IsRunning)
                return;

            _cancellationTokenSource = new CancellationTokenSource();
            
            _messageLoopTask = Task.Run(() =>
            {
                _threadId = WindowsAPI.GetCurrentThreadId();
                Console.WriteLine($"[MessageLoopRunner] 메시지 루프 시작 (스레드 ID: {_threadId})");
                
                try
                {
                    // 메시지 루프 스레드에서 초기화 작업 실행
                    onLoopStarted?.Invoke();
                    
                    // 메시지 루프 실행
                    RunMessageLoop(_cancellationTokenSource.Token);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[MessageLoopRunner] 메시지 루프 오류: {ex.Message}");
                }
                finally
                {
                    Console.WriteLine("[MessageLoopRunner] 메시지 루프 종료");
                }
            });

            // 메시지 루프가 시작될 때까지 잠시 대기
            await Task.Delay(100);
        }

        public void Stop()
        {
            if (_cancellationTokenSource != null)
            {
                _cancellationTokenSource.Cancel();
                PostQuitMessage();
            }

            if (_messageLoopTask != null)
            {
                try
                {
                    _messageLoopTask.Wait(TimeSpan.FromSeconds(2));
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[MessageLoopRunner] 메시지 루프 종료 대기 중 오류: {ex.Message}");
                }
                finally
                {
                    _messageLoopTask = null;
                    _threadId = 0;
                }
            }

            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
        }

        public void PostQuitMessage()
        {
            if (_threadId != 0)
            {
                WindowsAPI.PostThreadMessage(_threadId, WindowsAPI.WM_QUIT, IntPtr.Zero, IntPtr.Zero);
            }
        }

        private void RunMessageLoop(CancellationToken cancellationToken)
        {
            WindowsAPI.MSG msg;
            int messageCount = 0;
            
            Console.WriteLine("[MessageLoopRunner] 메시지 루프 실행 중...");
            
            while (!cancellationToken.IsCancellationRequested)
            {
                bool hasMessage = WindowsAPI.GetMessage(out msg, IntPtr.Zero, 0, 0);
                
                if (!hasMessage || msg.message == WindowsAPI.WM_QUIT)
                {
                    Console.WriteLine($"[MessageLoopRunner] 메시지 루프 종료 신호 수신 (총 {messageCount}개 메시지 처리)");
                    break;
                }
                
                messageCount++;
                
                // 주요 메시지만 로그 출력 (처음 5개와 100개마다)
                if (messageCount <= 5 || messageCount % 100 == 0)
                {
                    Console.WriteLine($"[MessageLoopRunner] 메시지 처리 #{messageCount}: 0x{msg.message:X} (hwnd: 0x{msg.hwnd:X})");
                }
                
                WindowsAPI.TranslateMessage(ref msg);
                WindowsAPI.DispatchMessage(ref msg);
            }
            
            Console.WriteLine($"[MessageLoopRunner] 메시지 루프 완료 (총 {messageCount}개 메시지 처리됨)");
        }

        public void Dispose()
        {
            if (_isDisposed) return;

            Stop();
            _isDisposed = true;
        }
    }
}