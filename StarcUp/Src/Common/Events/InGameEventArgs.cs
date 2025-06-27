using System;

namespace StarcUp.Common.Events
{
    public class InGameEventArgs : EventArgs
    {
        public bool IsInGame { get; }
        public DateTime Timestamp { get; }

        public InGameEventArgs(bool isInGame)
        {
            IsInGame = isInGame;
            Timestamp = DateTime.Now;
        }

        public override string ToString()
        {
            return $"InGameStateChanged: {IsInGame} at {Timestamp:HH:mm:ss.fff}";
        }
    }
}