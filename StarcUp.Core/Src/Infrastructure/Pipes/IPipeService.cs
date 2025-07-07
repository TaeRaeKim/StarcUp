using System;
using System.Threading;
using System.Threading.Tasks;

namespace StarcUp.Infrastructure.Pipes
{
    /// <summary>
    /// Anonymous Pipe 통신 서비스 인터페이스
    /// </summary>
    public interface IPipeService : IDisposable
    {
        /// <summary>
        /// 명령 수신 시 발생하는 이벤트
        /// </summary>
        event EventHandler<CommandReceivedEventArgs> CommandReceived;

        /// <summary>
        /// Pipe 서버 시작 (자식 프로세스에서 사용)
        /// </summary>
        /// <param name="pipeInHandle">부모로부터 받은 입력 핸들</param>
        /// <param name="pipeOutHandle">부모로부터 받은 출력 핸들</param>
        /// <param name="cancellationToken">취소 토큰</param>
        Task StartAsync(string pipeInHandle, string pipeOutHandle, CancellationToken cancellationToken = default);

        /// <summary>
        /// 응답 전송
        /// </summary>
        /// <param name="response">응답 메시지</param>
        /// <param name="cancellationToken">취소 토큰</param>
        Task SendResponseAsync(string response, CancellationToken cancellationToken = default);

        /// <summary>
        /// Pipe 서비스 중지
        /// </summary>
        Task StopAsync();
    }
}