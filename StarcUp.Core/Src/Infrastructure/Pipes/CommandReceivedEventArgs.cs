using System;

namespace StarcUp.Infrastructure.Pipes
{
    /// <summary>
    /// 명령 수신 이벤트 인자
    /// </summary>
    public class CommandReceivedEventArgs : EventArgs
    {
        /// <summary>
        /// 수신된 명령
        /// </summary>
        public string Command { get; }

        /// <summary>
        /// 명령 인자 (있는 경우)
        /// </summary>
        public string[] Arguments { get; }

        /// <summary>
        /// 명령 ID (응답 매칭용)
        /// </summary>
        public string CommandId { get; }

        public CommandReceivedEventArgs(string command, string[] arguments = null, string commandId = null)
        {
            Command = command ?? throw new ArgumentNullException(nameof(command));
            Arguments = arguments ?? Array.Empty<string>();
            CommandId = commandId ?? Guid.NewGuid().ToString();
        }
    }
}