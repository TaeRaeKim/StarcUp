using System;

namespace StarcUp.Infrastructure.Windows
{
    /// <summary>
    /// 새롭게 작성된 윈도우 매니저 인터페이스
    /// </summary>
    public interface IWindowManager : IDisposable
    {
        event Action<nint> WindowPositionChanged;
        event Action<nint> WindowActivated;
        event Action<nint> WindowDeactivated;

        bool SetupWindowEventHook(nint windowHandle, uint processId);
        bool SetupForegroundEventHook();

        void RemoveAllHooks();

        WindowInfo GetWindowInfo(nint windowHandle);

        bool IsWindowMinimized(nint windowHandle);

        bool IsWindowMaximized(nint windowHandle);

        nint GetForegroundWindow();

    }
}