using System;

namespace StarcUp.Infrastructure.Windows
{
    public interface IWindowManager : IDisposable
    {
        event EventHandler<WindowChangedEventArgs> WindowPositionChanged;
        event EventHandler<WindowChangedEventArgs> WindowSizeChanged;
        event EventHandler<WindowChangedEventArgs> WindowLost;

        bool StartMonitoring(int processId);
        bool StartMonitoring(string processName);
        void StopMonitoring();
        
        WindowInfo GetCurrentWindowInfo();
        bool IsMonitoring { get; }
        bool IsWindowValid { get; }
        bool IsMessageLoopRunning { get; }
        uint MessageLoopThreadId { get; }
    }

    public class WindowChangedEventArgs : EventArgs
    {
        public WindowInfo PreviousWindowInfo { get; }
        public WindowInfo CurrentWindowInfo { get; }
        public WindowChangeType ChangeType { get; }

        public WindowChangedEventArgs(WindowInfo previousWindowInfo, WindowInfo currentWindowInfo, WindowChangeType changeType)
        {
            PreviousWindowInfo = previousWindowInfo;
            CurrentWindowInfo = currentWindowInfo;
            ChangeType = changeType;
        }
    }

    public enum WindowChangeType
    {
        PositionChanged,
        SizeChanged,
        BothChanged,
        WindowLost
    }
}