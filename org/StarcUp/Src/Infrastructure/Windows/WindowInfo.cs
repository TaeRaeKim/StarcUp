using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StarcUp.Infrastructure.Windows
{
    /// <summary>
    /// 윈도우 정보를 담는 구조체
    /// </summary>
    public struct WindowInfo
    {
        public IntPtr Handle;
        public WindowsAPI.RECT WindowRect;
        public WindowsAPI.RECT ClientRect;
        public bool IsVisible;
        public bool IsMinimized;
        public bool IsMaximized;
        public bool IsFullscreen;
        public string Title;
        public uint ProcessId;

        public WindowInfo(IntPtr handle)
        {
            Handle = handle;
            WindowRect = new WindowsAPI.RECT();
            ClientRect = new WindowsAPI.RECT();
            IsVisible = false;
            IsMinimized = false;
            IsMaximized = false;
            IsFullscreen = false;
            Title = string.Empty;
            ProcessId = 0;

            if (WindowsAPI.IsValidWindow(handle))
            {
                RefreshInfo();
            }
        }

        public void RefreshInfo()
        {
            if (!WindowsAPI.IsValidWindow(Handle))
                return;

            // 윈도우 사각형 정보
            WindowsAPI.GetWindowRect(Handle, out WindowRect);
            WindowsAPI.GetClientRect(Handle, out ClientRect);

            // 상태 정보
            IsVisible = WindowsAPI.IsWindowVisible(Handle);
            IsMinimized = WindowsAPI.IsIconic(Handle);
            IsMaximized = WindowsAPI.IsZoomed(Handle);
            IsFullscreen = WindowsAPI.IsFullscreen(Handle);

            // 제목 및 프로세스 ID
            Title = WindowsAPI.GetWindowTitle(Handle);
            WindowsAPI.GetWindowThreadProcessId(Handle, out ProcessId);
        }

        public override string ToString()
        {
            return $"Window: {Title} ({Handle:X8}) - {WindowRect} - Visible:{IsVisible}, Min:{IsMinimized}, Max:{IsMaximized}, Full:{IsFullscreen}";
        }
    }

}
