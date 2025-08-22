using System;
using StarcUp.Core.Common.Events;

namespace StarcUp.Common.Events
{
    public class InGameEventArgs : EventArgs
    {
        public InGameState State { get; }
        public DateTime Timestamp { get; }

        public InGameEventArgs(InGameState state)
        {
            State = state;
            Timestamp = DateTime.Now;
        }

        public override string ToString()
        {
            return $"InGameStateChanged: {State} at {Timestamp:HH:mm:ss.fff}";
        }
    }
}