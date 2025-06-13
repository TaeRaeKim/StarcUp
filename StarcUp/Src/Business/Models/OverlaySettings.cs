using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StarcUp.Business.Models
{
    /// <summary>
    /// 오버레이 설정 모델
    /// </summary>
    public class OverlaySettings
    {
        public bool ShowOverlay { get; set; } = true;
        public int OffsetX { get; set; } = 15;
        public int OffsetY { get; set; } = 10;
        public int WindowModeOffsetX { get; set; } = 20;
        public int WindowModeOffsetY { get; set; } = 40;
        public string FontFamily { get; set; } = "맑은 고딕";
        public float FontSize { get; set; } = 12f;
        public Color TextColor { get; set; } = Color.Yellow;
        public bool ShowOnlyWhenActive { get; set; } = true;
        public int RefreshIntervalMs { get; set; } = 100;

        public OverlaySettings()
        {
        }

        public Point GetOffset(bool isFullscreen)
        {
            return isFullscreen
                ? new Point(OffsetX, OffsetY)
                : new Point(WindowModeOffsetX, WindowModeOffsetY);
        }
    }
}
