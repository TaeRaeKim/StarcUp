using System;

namespace StarcUp.Infrastructure.Windows
{
    /// <summary>
    /// 윈도우 위치 및 상태 정보를 담는 데이터 클래스
    /// Named Pipe를 통한 UI 전송용
    /// </summary>
    public class WindowPositionData
    {
        /// <summary>
        /// 윈도우 X 좌표
        /// </summary>
        public int X { get; set; }

        /// <summary>
        /// 윈도우 Y 좌표
        /// </summary>
        public int Y { get; set; }

        /// <summary>
        /// 윈도우 너비
        /// </summary>
        public int Width { get; set; }

        /// <summary>
        /// 윈도우 높이
        /// </summary>
        public int Height { get; set; }

        /// <summary>
        /// 클라이언트 영역 X 좌표
        /// </summary>
        public int ClientX { get; set; }

        /// <summary>
        /// 클라이언트 영역 Y 좌표
        /// </summary>
        public int ClientY { get; set; }

        /// <summary>
        /// 클라이언트 영역 너비
        /// </summary>
        public int ClientWidth { get; set; }

        /// <summary>
        /// 클라이언트 영역 높이
        /// </summary>
        public int ClientHeight { get; set; }

        /// <summary>
        /// 윈도우가 최소화되었는지 여부
        /// </summary>
        public bool IsMinimized { get; set; }

        /// <summary>
        /// 윈도우가 최대화되었는지 여부
        /// </summary>
        public bool IsMaximized { get; set; }

        /// <summary>
        /// 윈도우가 보이는지 여부
        /// </summary>
        public bool IsVisible { get; set; }

        /// <summary>
        /// 타임스탬프
        /// </summary>
        public DateTime Timestamp { get; set; }

        public WindowPositionData()
        {
            Timestamp = DateTime.UtcNow;
        }

        public WindowPositionData(int x, int y, int width, int height, 
                                 int clientX, int clientY, int clientWidth, int clientHeight,
                                 bool isMinimized, bool isMaximized, bool isVisible)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
            ClientX = clientX;
            ClientY = clientY;
            ClientWidth = clientWidth;
            ClientHeight = clientHeight;
            IsMinimized = isMinimized;
            IsMaximized = isMaximized;
            IsVisible = isVisible;
            Timestamp = DateTime.UtcNow;
        }

        /// <summary>
        /// 위치나 크기가 변경되었는지 확인
        /// </summary>
        /// <param name="other">비교할 다른 위치 데이터</param>
        /// <param name="threshold">변경 임계값 (픽셀)</param>
        /// <returns>변경되었으면 true</returns>
        public bool HasPositionChanged(WindowPositionData other, int threshold = 5)
        {
            if (other == null) return true;

            return Math.Abs(X - other.X) > threshold ||
                   Math.Abs(Y - other.Y) > threshold ||
                   Math.Abs(Width - other.Width) > threshold ||
                   Math.Abs(Height - other.Height) > threshold ||
                   Math.Abs(ClientX - other.ClientX) > threshold ||
                   Math.Abs(ClientY - other.ClientY) > threshold ||
                   Math.Abs(ClientWidth - other.ClientWidth) > threshold ||
                   Math.Abs(ClientHeight - other.ClientHeight) > threshold ||
                   IsMinimized != other.IsMinimized ||
                   IsMaximized != other.IsMaximized ||
                   IsVisible != other.IsVisible;
        }

        public override string ToString()
        {
            return $"Window[{X},{Y} {Width}x{Height}] Client[{ClientX},{ClientY} {ClientWidth}x{ClientHeight}] " +
                   $"Min:{IsMinimized} Max:{IsMaximized} Visible:{IsVisible}";
        }

        public WindowPositionData Clone()
        {
            return new WindowPositionData(X, Y, Width, Height, 
                                        ClientX, ClientY, ClientWidth, ClientHeight,
                                        IsMinimized, IsMaximized, IsVisible);
        }
    }
}