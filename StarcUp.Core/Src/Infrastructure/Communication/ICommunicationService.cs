using System;
using System.Threading.Tasks;

namespace StarcUp.Core.Src.Infrastructure.Communication
{
    /// <summary>
    /// StarcUp.UI와의 통신을 관리하는 서비스 인터페이스
    /// </summary>
    public interface ICommunicationService : IDisposable
    {
        /// <summary>
        /// 통신 서비스 시작
        /// </summary>
        /// <param name="pipeName">Named Pipe 이름 (null이면 환경에 맞는 이름 자동 선택)</param>
        /// <returns>시작 성공 여부</returns>
        Task<bool> StartAsync(string pipeName = "StarcUp.Dev");

        /// <summary>
        /// 통신 서비스 중지
        /// </summary>
        Task StopAsync();

        /// <summary>
        /// 연결 상태
        /// </summary>
        bool IsConnected { get; }

        /// <summary>
        /// 게임 상태 변경 알림
        /// </summary>
        /// <param name="gameStatus">게임 상태</param>
        Task NotifyGameStatusAsync(object gameStatus);

        /// <summary>
        /// 유닛 데이터 변경 알림
        /// </summary>
        /// <param name="unitData">유닛 데이터</param>
        Task NotifyUnitDataAsync(object unitData);

        /// <summary>
        /// 연결 상태 변경 이벤트
        /// </summary>
        event EventHandler<bool> ConnectionStateChanged;
    }
}