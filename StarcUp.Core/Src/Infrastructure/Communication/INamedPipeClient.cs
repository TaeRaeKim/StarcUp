using System;
using System.Threading.Tasks;
using StarcUp.Common.Events;

namespace StarcUp.Infrastructure.Communication
{
    /// <summary>
    /// Named Pipes 클라이언트 인터페이스
    /// StarcUp.UI의 Named Pipes Server와 통신하기 위한 클라이언트
    /// </summary>
    public interface INamedPipeClient : IDisposable
    {
        /// <summary>
        /// 연결 상태
        /// </summary>
        bool IsConnected { get; }

        /// <summary>
        /// 재연결 시도 중인지 여부
        /// </summary>
        bool IsReconnecting { get; }

        /// <summary>
        /// 서버에 연결
        /// </summary>
        /// <param name="pipeName">파이프 이름 (기본값: "StarcUp")</param>
        /// <param name="timeout">연결 타임아웃 (기본값: 5초)</param>
        /// <returns>연결 성공 여부</returns>
        Task<bool> ConnectAsync(string pipeName = "StarcUp", int timeout = 5000);

        /// <summary>
        /// 서버와의 연결 종료
        /// </summary>
        void Disconnect();

        /// <summary>
        /// 명령 전송
        /// </summary>
        /// <param name="command">명령 이름</param>
        /// <param name="args">명령 인수</param>
        /// <returns>서버 응답</returns>
        Task<NamedPipeResponse> SendCommandAsync(string command, string[] args = null);

        /// <summary>
        /// 이벤트 전송
        /// </summary>
        /// <param name="eventType">이벤트 타입</param>
        /// <param name="data">이벤트 데이터</param>
        /// <returns>전송 성공 여부</returns>
        bool SendEvent(string eventType, object data = null);

        /// <summary>
        /// 자동 재연결 시작
        /// </summary>
        /// <param name="pipeName">파이프 이름</param>
        /// <param name="reconnectInterval">재연결 간격 (밀리초)</param>
        /// <param name="maxRetries">최대 재연결 횟수 (0 = 무제한)</param>
        void StartAutoReconnect(string pipeName = "StarcUp", int reconnectInterval = 3000, int maxRetries = 0);

        /// <summary>
        /// 자동 재연결 중지
        /// </summary>
        void StopAutoReconnect();

        /// <summary>
        /// 재연결 루프를 수동으로 시작
        /// </summary>
        void TriggerReconnect();

        /// <summary>
        /// 연결 상태 변경 이벤트
        /// </summary>
        event EventHandler<bool> ConnectionStateChanged;

        /// <summary>
        /// 명령 요청 수신 이벤트 (UI로부터 온 명령을 Business 계층으로 전달)
        /// </summary>
        event EventHandler<CommandRequestEventArgs> CommandRequestReceived;
    }
}