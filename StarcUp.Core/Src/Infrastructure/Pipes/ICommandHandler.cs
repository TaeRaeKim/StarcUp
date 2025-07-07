using System;
using System.Threading.Tasks;

namespace StarcUp.Infrastructure.Pipes
{
    /// <summary>
    /// 명령 처리 핸들러 인터페이스
    /// </summary>
    public interface ICommandHandler
    {
        /// <summary>
        /// 명령 처리
        /// </summary>
        /// <param name="command">명령</param>
        /// <param name="arguments">인자</param>
        /// <param name="commandId">명령 ID</param>
        /// <returns>처리 결과</returns>
        Task<string> HandleCommandAsync(string command, string[] arguments, string commandId);
    }
}