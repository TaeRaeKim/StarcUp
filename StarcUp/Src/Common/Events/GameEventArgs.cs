using System;
using StarcUp.Business.Models;

namespace StarcUp.Common.Events
{
    /// <summary>
    /// 게임 관련 이벤트 아규먼트
    /// </summary>
    public class GameEventArgs : EventArgs
    {
        public GameInfo GameInfo { get; }
        public string EventType { get; }

        public GameEventArgs(GameInfo gameInfo, string eventType = "")
        {
            GameInfo = gameInfo ?? throw new ArgumentNullException(nameof(gameInfo));
            EventType = eventType;
        }

        public override string ToString()
        {
            return $"GameEvent: {EventType} - {GameInfo}";
        }
    }
}