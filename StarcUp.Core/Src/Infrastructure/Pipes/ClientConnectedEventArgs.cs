using System;

namespace StarcUp.Infrastructure.Pipes
{
    /// <summary>
    /// 클라이언트 연결 이벤트 인수
    /// </summary>
    public class ClientConnectedEventArgs : EventArgs
    {
        /// <summary>
        /// 클라이언트 ID
        /// </summary>
        public string ClientId { get; }

        /// <summary>
        /// 연결 시간
        /// </summary>
        public DateTime ConnectedTime { get; }

        /// <summary>
        /// ClientConnectedEventArgs 생성자
        /// </summary>
        /// <param name="clientId">클라이언트 ID</param>
        public ClientConnectedEventArgs(string clientId)
        {
            ClientId = clientId;
            ConnectedTime = DateTime.UtcNow;
        }
    }
}