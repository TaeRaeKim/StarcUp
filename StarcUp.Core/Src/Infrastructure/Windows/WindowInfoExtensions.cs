using System;

namespace StarcUp.Infrastructure.Windows
{
    /// <summary>
    /// WindowInfo 확장 메서드
    /// </summary>
    public static class WindowInfoExtensions
    {
        /// <summary>
        /// WindowInfo를 WindowPositionData로 변환
        /// </summary>
        /// <param name="windowInfo">변환할 윈도우 정보</param>
        /// <returns>윈도우 위치 데이터</returns>
        public static WindowPositionData ToPositionData(this WindowInfo windowInfo)
        {
            if (windowInfo == null)
                return null;

            var handle = windowInfo.Handle;
            
            // 클라이언트 영역 정보 얻기
            WindowsAPI.GetClientRect(handle, out var clientRect);
            
            // 클라이언트 영역의 화면 좌표 계산
            var clientPoint = new WindowsAPI.POINT { X = 0, Y = 0 };
            WindowsAPI.ClientToScreen(handle, ref clientPoint);
            
            // 윈도우 상태 확인
            bool isMinimized = WindowsAPI.IsIconic(handle);
            bool isMaximized = WindowsAPI.IsZoomed(handle);
            bool isVisible = WindowsAPI.IsWindowVisible(handle);

            return new WindowPositionData(
                x: windowInfo.X,
                y: windowInfo.Y,
                width: windowInfo.Width,
                height: windowInfo.Height,
                clientX: clientPoint.X,
                clientY: clientPoint.Y,
                clientWidth: clientRect.Right - clientRect.Left,
                clientHeight: clientRect.Bottom - clientRect.Top,
                isMinimized: isMinimized,
                isMaximized: isMaximized,
                isVisible: isVisible
            );
        }

        /// <summary>
        /// 두 WindowInfo가 위치나 크기 변경이 있었는지 확인
        /// </summary>
        /// <param name="current">현재 윈도우 정보</param>
        /// <param name="previous">이전 윈도우 정보</param>
        /// <param name="threshold">변경 임계값 (픽셀)</param>
        /// <returns>변경되었으면 true</returns>
        public static bool HasSignificantChange(this WindowInfo current, WindowInfo previous, int threshold = 5)
        {
            if (previous == null) return true;
            
            return Math.Abs(current.X - previous.X) > threshold ||
                   Math.Abs(current.Y - previous.Y) > threshold ||
                   Math.Abs(current.Width - previous.Width) > threshold ||
                   Math.Abs(current.Height - previous.Height) > threshold;
        }
    }
}