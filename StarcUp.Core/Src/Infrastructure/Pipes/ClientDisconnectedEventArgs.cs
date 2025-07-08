using System;

namespace StarcUp.Infrastructure.Pipes
{
    /// <summary>
    /// 클라이언트 연결 해제 이벤트 인수
    /// </summary>
    public class ClientDisconnectedEventArgs : EventArgs
    {
        /// <summary>
        /// 클라이언트 ID
        /// </summary>
        public string ClientId { get; }

        /// <summary>
        /// 연결 해제 시간
        /// </summary>
        public DateTime DisconnectedTime { get; }

        /// <summary>
        /// 연결 해제 이유
        /// </summary>
        public string Reason { get; }

        /// <summary>
        /// ClientDisconnectedEventArgs 생성자
        /// </summary>
        /// <param name="clientId">클라이언트 ID</param>
        /// <param name="reason">연결 해제 이유</param>
        public ClientDisconnectedEventArgs(string clientId, string reason = "Unknown")
        {
            ClientId = clientId;
            DisconnectedTime = DateTime.UtcNow;
            Reason = reason;
        }
    }
}