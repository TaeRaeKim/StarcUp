﻿using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace StarcUp.Infrastructure.Windows
{
    public class WindowManager : IWindowManager
    {
        private readonly List<IntPtr> _eventHooks;
        private readonly WindowsAPI.WinEventDelegate _winEventDelegate;
        private bool _isDisposed;

        public event Action<IntPtr> WindowPositionChanged;
        public event Action<IntPtr> WindowActivated;
        public event Action<IntPtr> WindowDeactivated;

        public WindowManager()
        {
            _eventHooks = new List<IntPtr>();
            _winEventDelegate = new WindowsAPI.WinEventDelegate(WinEventCallback);
        }

        public bool SetupWindowEventHook(IntPtr windowHandle, uint processId)
        {
            if (_isDisposed)
                return false;

            try
            {
                Console.WriteLine($"[WindowManager] 윈도우 이벤트 후킹 설정: Handle=0x{windowHandle:X8}, PID={processId}");

                // 위치 변경 이벤트 후킹
                var locationHook = WindowsAPI.SetWinEventHook(
                    WindowsAPI.EVENT_OBJECT_LOCATIONCHANGE,
                    WindowsAPI.EVENT_OBJECT_LOCATIONCHANGE,
                    IntPtr.Zero,
                    _winEventDelegate,
                    processId,
                    0,
                    WindowsAPI.WINEVENT_OUTOFCONTEXT);

                if (locationHook != IntPtr.Zero)
                {
                    _eventHooks.Add(locationHook);
                    Console.WriteLine($"[WindowManager] 위치 변경 이벤트 후킹 성공: 0x{locationHook:X8}");
                }

                // 최소화/복원 이벤트 후킹
                var minimizeHook = WindowsAPI.SetWinEventHook(
                    WindowsAPI.EVENT_SYSTEM_MINIMIZESTART,
                    WindowsAPI.EVENT_SYSTEM_MINIMIZEEND,
                    IntPtr.Zero,
                    _winEventDelegate,
                    processId,
                    0,
                    WindowsAPI.WINEVENT_OUTOFCONTEXT);

                if (minimizeHook != IntPtr.Zero)
                {
                    _eventHooks.Add(minimizeHook);
                    Console.WriteLine($"[WindowManager] 최소화 이벤트 후킹 성공: 0x{minimizeHook:X8}");
                }

                return _eventHooks.Count > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[WindowManager] 윈도우 이벤트 후킹 실패: {ex.Message}");
                return false;
            }
        }

        public bool SetupForegroundEventHook()
        {
            if (_isDisposed)
                return false;

            try
            {
                Console.WriteLine("[WindowManager] 포어그라운드 이벤트 후킹 설정");

                var foregroundHook = WindowsAPI.SetWinEventHook(
                    WindowsAPI.EVENT_SYSTEM_FOREGROUND,
                    WindowsAPI.EVENT_SYSTEM_FOREGROUND,
                    IntPtr.Zero,
                    _winEventDelegate,
                    0, // 모든 프로세스
                    0, // 모든 스레드
                    WindowsAPI.WINEVENT_OUTOFCONTEXT | WindowsAPI.WINEVENT_SKIPOWNPROCESS);

                if (foregroundHook != IntPtr.Zero)
                {
                    _eventHooks.Add(foregroundHook);
                    Console.WriteLine($"[WindowManager] 포어그라운드 이벤트 후킹 성공: 0x{foregroundHook:X8}");
                    return true;
                }

                Console.WriteLine("[WindowManager] 포어그라운드 이벤트 후킹 실패");
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[WindowManager] 포어그라운드 이벤트 후킹 실패: {ex.Message}");
                return false;
            }
        }
        public void RemoveAllHooks()
        {
            try
            {
                Console.WriteLine($"[WindowManager] 모든 이벤트 후킹 해제 시작 ({_eventHooks.Count}개)");

                foreach (var hook in _eventHooks)
                {
                    if (hook != IntPtr.Zero)
                    {
                        bool success = WindowsAPI.UnhookWinEvent(hook);
                        Console.WriteLine($"[WindowManager] 후킹 해제: 0x{hook:X8} - {(success ? "성공" : "실패")}");
                    }
                }

                _eventHooks.Clear();
                Console.WriteLine("[WindowManager] 모든 이벤트 후킹 해제 완료");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[WindowManager] 이벤트 후킹 해제 중 오류: {ex.Message}");
            }
        }

        public WindowInfo GetWindowInfo(IntPtr windowHandle)
        {
            try
            {
                var windowInfo = new WindowInfo(windowHandle);
                return windowInfo;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[WindowManager] 윈도우 정보 가져오기 실패: {ex.Message}");
                return new WindowInfo();
            }
        }

        public bool IsWindowMinimized(IntPtr windowHandle)
        {
            return WindowsAPI.IsValidWindow(windowHandle) && WindowsAPI.IsIconic(windowHandle);
        }

        public bool IsWindowMaximized(IntPtr windowHandle)
        {
            return WindowsAPI.IsValidWindow(windowHandle) && WindowsAPI.IsZoomed(windowHandle);
        }

        public IntPtr GetForegroundWindow()
        {
            return WindowsAPI.GetForegroundWindow();
        }

        private void WinEventCallback(IntPtr hWinEventHook, uint eventType, IntPtr hwnd,
            int idObject, int idChild, uint dwEventThread, uint dwmsEventTime)
        {
            if (hwnd == IntPtr.Zero || _isDisposed)
                return;

            try
            {
                switch (eventType)
                {
                    case WindowsAPI.EVENT_OBJECT_LOCATIONCHANGE:
                        Console.WriteLine($"[WindowManager] 윈도우 위치 변경: 0x{hwnd:X8}");
                        WindowPositionChanged?.Invoke(hwnd);
                        break;

                    case WindowsAPI.EVENT_SYSTEM_MINIMIZESTART:
                        Console.WriteLine($"[WindowManager] 윈도우 최소화 시작: 0x{hwnd:X8}");
                        WindowPositionChanged?.Invoke(hwnd);
                        break;

                    case WindowsAPI.EVENT_SYSTEM_MINIMIZEEND:
                        Console.WriteLine($"[WindowManager] 윈도우 최소화 종료: 0x{hwnd:X8}");
                        WindowPositionChanged?.Invoke(hwnd);
                        break;

                    case WindowsAPI.EVENT_SYSTEM_FOREGROUND:
                        Console.WriteLine($"[WindowManager] 포어그라운드 변경: 0x{hwnd:X8}");

                        // 현재 프로세스는 무시
                        WindowsAPI.GetWindowThreadProcessId(hwnd, out uint processId);
                        if (processId == Process.GetCurrentProcess().Id)
                            return;

                        WindowActivated?.Invoke(hwnd);
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[WindowManager] 윈도우 이벤트 콜백 오류: {ex.Message}");
            }
        }

        public void Dispose()
        {
            if (_isDisposed)
                return;

            Console.WriteLine("[WindowManager] 윈도우 매니저 해제 시작");

            RemoveAllHooks();
            _isDisposed = true;

            Console.WriteLine("[WindowManager] 윈도우 매니저 해제 완료");
        }
    }
}