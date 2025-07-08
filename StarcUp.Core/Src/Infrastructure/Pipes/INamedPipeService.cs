using System;
using System.Threading;
using System.Threading.Tasks;

namespace StarcUp.Infrastructure.Pipes
{
    /// <summary>
    /// Named Pipe 통신 서비스 인터페이스
    /// </summary>
    public interface INamedPipeService : IDisposable
    {
        /// <summary>
        /// 명령 수신 시 발생하는 이벤트
        /// </summary>
        event EventHandler<CommandReceivedEventArgs> CommandReceived;

        /// <summary>
        /// 클라이언트 연결 시 발생하는 이벤트
        /// </summary>
        event EventHandler<ClientConnectedEventArgs> ClientConnected;

        /// <summary>
        /// 클라이언트 연결 해제 시 발생하는 이벤트
        /// </summary>
        event EventHandler<ClientDisconnectedEventArgs> ClientDisconnected;

        /// <summary>
        /// Named Pipe 서버 시작
        /// </summary>
        /// <param name="pipeName">파이프 이름</param>
        /// <param name="cancellationToken">취소 토큰</param>
        Task StartAsync(string pipeName, CancellationToken cancellationToken = default);

        /// <summary>
        /// 응답 전송
        /// </summary>
        /// <param name="response">응답 메시지</param>
        /// <param name="clientId">클라이언트 ID (null인 경우 모든 클라이언트에게 전송)</param>
        /// <param name="cancellationToken">취소 토큰</param>
        Task SendResponseAsync(string response, string clientId = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Named Pipe 서비스 중지
        /// </summary>
        Task StopAsync();

        /// <summary>
        /// 연결된 클라이언트 수
        /// </summary>
        int ConnectedClientCount { get; }
    }
}