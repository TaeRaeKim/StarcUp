using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using StarcUp.Business.Models;

namespace StarcUp.Presentation.Controls
{
    /// <summary>
    /// 새롭게 작성된 오버레이 컨트롤 - 단순하고 안정적인 구조
    /// </summary>
    public class OverlayControl : UserControl
    {
        #region Private Fields

        private string _statusText = "StarcUp";
        private string _pointerText = "포인터: --";
        private Color _statusColor = Color.Yellow;
        private Color _pointerColor = Color.LightGreen;
        private Color _backgroundColor = Color.FromArgb(120, 0, 0, 0); // 반투명 검은색
        private Font _statusFont;
        private Font _pointerFont;
        private bool _isDisposed;

        // 그리기 관련
        private readonly SolidBrush _backgroundBrush;
        private readonly SolidBrush _statusBrush;
        private readonly SolidBrush _pointerBrush;
        private readonly Pen _borderPen;

        #endregion

        #region Constructor

        public OverlayControl()
        {
            Console.WriteLine("[OverlayControl] 새 오버레이 컨트롤 생성");

            // 기본 컨트롤 설정
            SetStyle(ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.UserPaint |
                     ControlStyles.DoubleBuffer |
                     ControlStyles.SupportsTransparentBackColor, true);

            this.BackColor = Color.Transparent;
            this.Size = new Size(200, 60);

            // 폰트 초기화
            InitializeFonts();

            // 브러시 및 펜 초기화
            _backgroundBrush = new SolidBrush(_backgroundColor);
            _statusBrush = new SolidBrush(_statusColor);
            _pointerBrush = new SolidBrush(_pointerColor);
            _borderPen = new Pen(Color.Yellow, 1);

            Console.WriteLine("[OverlayControl] 오버레이 컨트롤 초기화 완료");
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// 상태 텍스트 업데이트
        /// </summary>
        public void UpdateStatus(string status)
        {
            if (_isDisposed || string.IsNullOrEmpty(status))
                return;

            try
            {
                if (this.InvokeRequired)
                {
                    this.BeginInvoke(new Action<string>(UpdateStatus), status);
                    return;
                }

                _statusText = status;
                _statusBrush.Color = _statusColor;

                Console.WriteLine($"[OverlayControl] 상태 업데이트: {status}");
                this.Invalidate();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[OverlayControl] 상태 업데이트 실패: {ex.Message}");
            }
        }

        /// <summary>
        /// 포인터 값 업데이트
        /// </summary>
        public void UpdatePointerValue(PointerValue pointerValue)
        {
            if (_isDisposed)
                return;

            try
            {
                if (this.InvokeRequired)
                {
                    this.BeginInvoke(new Action<PointerValue>(UpdatePointerValue), pointerValue);
                    return;
                }

                if (pointerValue != null)
                {
                    _pointerText = $"포인터: {pointerValue.NewValue}";

                    // 변화에 따른 색상 변경
                    if (pointerValue.HasChanged)
                    {
                        if (pointerValue.Difference > 0)
                        {
                            _pointerColor = Color.LightGreen; // 증가
                            _pointerText += $" (+{pointerValue.Difference})";
                        }
                        else if (pointerValue.Difference < 0)
                        {
                            _pointerColor = Color.LightCoral; // 감소
                            _pointerText += $" ({pointerValue.Difference})";
                        }
                        else
                        {
                            _pointerColor = Color.LightBlue; // 동일
                        }
                    }
                    else
                    {
                        _pointerColor = Color.LightGray;
                    }

                    Console.WriteLine($"[OverlayControl] 포인터 업데이트: {_pointerText}");
                }
                else
                {
                    _pointerText = "포인터: --";
                    _pointerColor = Color.Gray;
                }

                _pointerBrush.Color = _pointerColor;
                this.Invalidate();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[OverlayControl] 포인터 값 업데이트 실패: {ex.Message}");
            }
        }

        /// <summary>
        /// 테마 색상 변경
        /// </summary>
        public void SetTheme(Color statusColor, Color pointerColor, Color backgroundColor)
        {
            try
            {
                _statusColor = statusColor;
                _pointerColor = pointerColor;
                _backgroundColor = backgroundColor;

                _statusBrush.Color = _statusColor;
                _pointerBrush.Color = _pointerColor;
                _backgroundBrush.Color = _backgroundColor;

                this.Invalidate();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[OverlayControl] 테마 설정 실패: {ex.Message}");
            }
        }

        #endregion

        #region Protected Methods

        protected override void OnPaint(PaintEventArgs e)
        {
            if (_isDisposed)
                return;

            try
            {
                var g = e.Graphics;

                // 고품질 렌더링 설정
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

                // 배경 그리기
                DrawBackground(g);

                // 텍스트 그리기
                DrawTexts(g);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[OverlayControl] 그리기 실패: {ex.Message}");
            }
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            this.Invalidate();
        }

        #endregion

        #region Private Methods

        private void InitializeFonts()
        {
            try
            {
                _statusFont = new Font("맑은 고딕", 10f, FontStyle.Bold);
                _pointerFont = new Font("Consolas", 9f, FontStyle.Regular);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[OverlayControl] 폰트 초기화 실패: {ex.Message}");

                // 기본 폰트로 대체
                _statusFont = new Font("Arial", 10f, FontStyle.Bold);
                _pointerFont = new Font("Arial", 9f, FontStyle.Regular);
            }
        }

        private void DrawBackground(Graphics g)
        {
            try
            {
                var rect = new Rectangle(0, 0, this.Width, this.Height);

                // 둥근 모서리 배경
                using (var path = CreateRoundedRectangle(rect, 8))
                {
                    g.FillPath(_backgroundBrush, path);
                    g.DrawPath(_borderPen, path);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[OverlayControl] 배경 그리기 실패: {ex.Message}");
            }
        }

        private void DrawTexts(Graphics g)
        {
            try
            {
                var padding = 8;
                var lineHeight = 20;

                // 상태 텍스트
                var statusRect = new Rectangle(padding, padding, this.Width - padding * 2, lineHeight);
                g.DrawString(_statusText, _statusFont, _statusBrush, statusRect);

                // 포인터 텍스트
                var pointerRect = new Rectangle(padding, padding + lineHeight, this.Width - padding * 2, lineHeight);
                g.DrawString(_pointerText, _pointerFont, _pointerBrush, pointerRect);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[OverlayControl] 텍스트 그리기 실패: {ex.Message}");
            }
        }

        private GraphicsPath CreateRoundedRectangle(Rectangle rect, int radius)
        {
            var path = new GraphicsPath();

            try
            {
                int diameter = radius * 2;

                // 둥근 모서리 경로 생성
                path.AddArc(rect.X, rect.Y, diameter, diameter, 180, 90);
                path.AddArc(rect.Right - diameter, rect.Y, diameter, diameter, 270, 90);
                path.AddArc(rect.Right - diameter, rect.Bottom - diameter, diameter, diameter, 0, 90);
                path.AddArc(rect.X, rect.Bottom - diameter, diameter, diameter, 90, 90);
                path.CloseFigure();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[OverlayControl] 둥근 사각형 생성 실패: {ex.Message}");

                // 실패 시 일반 사각형
                path.AddRectangle(rect);
            }

            return path;
        }

        #endregion

        #region Dispose

        protected override void Dispose(bool disposing)
        {
            if (!_isDisposed && disposing)
            {
                Console.WriteLine("[OverlayControl] 오버레이 컨트롤 해제 시작");

                try
                {
                    _statusFont?.Dispose();
                    _pointerFont?.Dispose();
                    _backgroundBrush?.Dispose();
                    _statusBrush?.Dispose();
                    _pointerBrush?.Dispose();
                    _borderPen?.Dispose();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[OverlayControl] 리소스 해제 중 오류: {ex.Message}");
                }

                _isDisposed = true;
                Console.WriteLine("[OverlayControl] 오버레이 컨트롤 해제 완료");
            }

            base.Dispose(disposing);
        }

        #endregion
    }
}