using System;

namespace StarcUp.Business.InGameStateMonitor
{
    public class InGameStateEventArgs : EventArgs
    {
        public bool IsInGame { get; }
        public DateTime Timestamp { get; }

        public InGameStateEventArgs(bool isInGame)
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