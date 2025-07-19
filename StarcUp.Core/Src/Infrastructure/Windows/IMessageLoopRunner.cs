using System;
using System.Threading.Tasks;

namespace StarcUp.Infrastructure.Windows
{
    /// <summary>
    /// Windows 메시지 루프를 관리하는 인터페이스
    /// </summary>
    public interface IMessageLoopRunner : IDisposable
    {
        /// <summary>
        /// 메시지 루프가 실행 중인지 여부
        /// </summary>
        bool IsRunning { get; }

        /// <summary>
        /// 메시지 루프 스레드 ID
        /// </summary>
        uint ThreadId { get; }

        /// <summary>
        /// 메시지 루프를 시작합니다.
        /// </summary>
        /// <param name="onLoopStarted">메시지 루프 스레드에서 실행될 초기화 작업</param>
        /// <returns>메시지 루프 Task</returns>
        Task StartAsync(Action onLoopStarted = null);

        /// <summary>
        /// 메시지 루프를 중지합니다.
        /// </summary>
        void Stop();

        /// <summary>
        /// 메시지 루프 스레드에 종료 메시지를 전송합니다.
        /// </summary>
        void PostQuitMessage();
    }
}