using System;
using System.Runtime.InteropServices;
using System.Text;

namespace StarcUp.Infrastructure.Windows
{
    /// <summary>
    /// 새롭게 작성된 Windows API 함수 및 상수 정의
    /// </summary>
    public static class WindowsAPI
    {
        #region User32.dll Functions

        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetClientRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool IsWindow(IntPtr hWnd);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool IsZoomed(IntPtr hWnd);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool IsIconic(IntPtr hWnd);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll", SetLastError = true)]
        public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern int GetWindowTextLength(IntPtr hWnd);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr SetWinEventHook(uint eventMin, uint eventMax, IntPtr hmodWinEventProc,
            WinEventDelegate lpfnWinEventProc, uint idProcess, uint idThread, uint dwFlags);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool UnhookWinEvent(IntPtr hWinEventHook);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern int GetSystemMetrics(int nIndex);

        #endregion

        #region Delegates

        public delegate void WinEventDelegate(IntPtr hWinEventHook, uint eventType, IntPtr hwnd,
            int idObject, int idChild, uint dwEventThread, uint dwmsEventTime);

        #endregion

        #region Constants

        // Window Event Constants
        public const uint EVENT_SYSTEM_FOREGROUND = 0x0003;
        public const uint EVENT_SYSTEM_MINIMIZESTART = 0x0016;
        public const uint EVENT_SYSTEM_MINIMIZEEND = 0x0017;
        public const uint EVENT_OBJECT_LOCATIONCHANGE = 0x800B;
        public const uint EVENT_OBJECT_NAMECHANGE = 0x800C;
        public const uint EVENT_SYSTEM_MOVESIZESTART = 0x000A;
        public const uint EVENT_SYSTEM_MOVESIZEEND = 0x000B;

        // WinEvent Flags
        public const uint WINEVENT_OUTOFCONTEXT = 0x0000;
        public const uint WINEVENT_SKIPOWNTHREAD = 0x0001;
        public const uint WINEVENT_SKIPOWNPROCESS = 0x0002;

        // SetWindowPos Constants
        public const uint SWP_NOSIZE = 0x0001;
        public const uint SWP_NOMOVE = 0x0002;
        public const uint SWP_NOZORDER = 0x0004;
        public const uint SWP_NOREDRAW = 0x0008;
        public const uint SWP_NOACTIVATE = 0x0010;
        public const uint SWP_FRAMECHANGED = 0x0020;
        public const uint SWP_SHOWWINDOW = 0x0040;
        public const uint SWP_HIDEWINDOW = 0x0080;

        // Z-Order Constants
        public static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        public static readonly IntPtr HWND_NOTOPMOST = new IntPtr(-2);
        public static readonly IntPtr HWND_TOP = new IntPtr(0);
        public static readonly IntPtr HWND_BOTTOM = new IntPtr(1);

        // System Metrics
        public const int SM_CXSCREEN = 0;
        public const int SM_CYSCREEN = 1;

        #endregion

        #region Structures

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;

            public int Width => Right - Left;
            public int Height => Bottom - Top;

            public RECT(int left, int top, int right, int bottom)
            {
                Left = left;
                Top = top;
                Right = right;
                Bottom = bottom;
            }

            public override string ToString()
            {
                return $"RECT(L:{Left}, T:{Top}, R:{Right}, B:{Bottom}, W:{Width}, H:{Height})";
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int X;
            public int Y;

            public POINT(int x, int y)
            {
                X = x;
                Y = y;
            }

            public override string ToString()
            {
                return $"POINT({X}, {Y})";
            }
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// 윈도우 핸들이 유효한지 확인
        /// </summary>
        public static bool IsValidWindow(IntPtr hWnd)
        {
            return hWnd != 0 && IsWindow(hWnd);
        }

        /// <summary>
        /// 윈도우 제목 가져오기
        /// </summary>
        public static string GetWindowTitle(IntPtr hWnd)
        {
            if (!IsValidWindow(hWnd))
                return string.Empty;

            int length = GetWindowTextLength(hWnd);
            if (length == 0)
                return string.Empty;

            StringBuilder sb = new StringBuilder(length + 1);
            GetWindowText(hWnd, sb, sb.Capacity);
            return sb.ToString();
        }

        /// <summary>
        /// 화면 크기 가져오기
        /// </summary>
        public static POINT GetScreenSize()
        {
            return new POINT(
                GetSystemMetrics(SM_CXSCREEN),
                GetSystemMetrics(SM_CYSCREEN)
            );
        }

        /// <summary>
        /// 윈도우가 전체화면인지 확인
        /// </summary>
        public static bool IsFullscreen(IntPtr hWnd)
        {
            if (!IsValidWindow(hWnd))
                return false;

            if (!GetWindowRect(hWnd, out RECT windowRect))
                return false;

            var screenSize = GetScreenSize();

            return windowRect.Left <= 0 &&
                   windowRect.Top <= 0 &&
                   windowRect.Width >= screenSize.X &&
                   windowRect.Height >= screenSize.Y;
        }

        #endregion
    }
}