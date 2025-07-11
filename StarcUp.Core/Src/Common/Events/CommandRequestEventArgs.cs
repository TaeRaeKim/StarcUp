using System;

namespace StarcUp.Common.Events
{
    /// <summary>
    /// Named Pipe로부터 받은 명령 요청 이벤트 데이터
    /// </summary>
    public class CommandRequestEventArgs : EventArgs
    {
        public string RequestId { get; }
        public string Command { get; }
        public object? Payload { get; }
        public DateTime ReceivedAt { get; }

        public CommandRequestEventArgs(string requestId, string command, object? payload = null)
        {
            RequestId = requestId ?? throw new ArgumentNullException(nameof(requestId));
            Command = command ?? throw new ArgumentNullException(nameof(command));
            Payload = payload;
            ReceivedAt = DateTime.UtcNow;
        }
    }
}