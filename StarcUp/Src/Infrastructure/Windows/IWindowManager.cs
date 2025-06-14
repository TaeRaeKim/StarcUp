using System;

namespace StarcUp.Infrastructure.Windows
{
    /// <summary>
    /// 새롭게 작성된 윈도우 매니저 인터페이스
    /// </summary>
    public interface IWindowManager : IDisposable
    {
        event Action<IntPtr> WindowPositionChanged;
        event Action<IntPtr> WindowActivated;
        event Action<IntPtr> WindowDeactivated;

        bool SetupWindowEventHook(IntPtr windowHandle, uint processId);
        bool SetupForegroundEventHook();

        void RemoveAllHooks();

        WindowInfo GetWindowInfo(IntPtr windowHandle);

        bool IsWindowMinimized(IntPtr windowHandle);

        bool IsWindowMaximized(IntPtr windowHandle);

        IntPtr GetForegroundWindow();

    }
}