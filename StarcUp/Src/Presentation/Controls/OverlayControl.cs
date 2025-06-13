using System;
using System.Drawing;
using System.Windows.Forms;
using StarcUp.Business.Models;
using StarcUp.Presentation.Controls;

namespace StarcUp.Presentation.Controls
{
    /// <summary>
    /// 오버레이 컨트롤
    /// </summary>
    public class OverlayControl : UserControl
    {
        private PictureBox _iconBox;
        private Label _statusLabel;
        private Label _pointerLabel;
        private Panel _container;
        private bool _isDisposed;

        public OverlayControl()
        {
            InitializeComponent();
            CreateControls();
            UpdateStatus("대기 중...");
        }

        private void InitializeComponent()
        {
            this.AutoSize = true;
            this.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            this.BackColor = Color.Transparent;
            this.Padding = new Padding(5);
        }

        private void CreateControls()
        {
            // 컨테이너 패널
            _container = new Panel
            {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                BackColor = Color.Transparent,
                Location = new Point(0, 0)
            };

            // 아이콘 박스
            _iconBox = new PictureBox
            {
                Size = new Size(48, 48),
                Location = new Point(0, 0),
                BackColor = Color.Transparent,
                SizeMode = PictureBoxSizeMode.StretchImage,
                Image = CreateDefaultIcon()
            };

            // 상태 라벨
            _statusLabel = new Label
            {
                Text = "StarcUp 활성화",
                ForeColor = Color.Yellow,
                BackColor = Color.Transparent,
                Font = new Font("맑은 고딕", 11, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(58, 5),
                TextAlign = ContentAlignment.MiddleLeft
            };

            // 포인터 값 라벨
            _pointerLabel = new Label
            {
                Text = "포인터: --",
                ForeColor = Color.LightGreen,
                BackColor = Color.Transparent,
                Font = new Font("맑은 고딕", 10, FontStyle.Regular),
                AutoSize = true,
                Location = new Point(58, 25),
                TextAlign = ContentAlignment.MiddleLeft
            };

            // 컨테이너에 컨트롤들 추가
            _container.Controls.Add(_iconBox);
            _container.Controls.Add(_statusLabel);
            _container.Controls.Add(_pointerLabel);

            // 메인 컨트롤에 컨테이너 추가
            this.Controls.Add(_container);
        }

        /// <summary>
        /// 포인터 값 업데이트
        /// </summary>
        public void UpdatePointerValue(PointerValue pointerValue)
        {
            if (_pointerLabel != null && !_isDisposed)
            {
                try
                {
                    string text;
                    Color color;

                    if (pointerValue != null)
                    {
                        text = $"포인터: {pointerValue.NewValue}";

                        // 변화량에 따라 색상 변경
                        if (pointerValue.Difference > 0)
                        {
                            color = Color.LightGreen; // 증가
                            text += $" (+{pointerValue.Difference})";
                        }
                        else if (pointerValue.Difference < 0)
                        {
                            color = Color.LightCoral; // 감소
                            text += $" ({pointerValue.Difference})";
                        }
                        else
                        {
                            color = Color.LightBlue; // 변화 없음
                        }
                    }
                    else
                    {
                        text = "포인터: --";
                        color = Color.Gray;
                    }

                    _pointerLabel.Text = text;
                    _pointerLabel.ForeColor = color;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"포인터 라벨 업데이트 실패: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// 상태 업데이트
        /// </summary>
        public void UpdateStatus(string status)
        {
            if (_statusLabel != null && !_isDisposed)
            {
                try
                {
                    _statusLabel.Text = status ?? "StarcUp";
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"상태 라벨 업데이트 실패: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// 아이콘 업데이트
        /// </summary>
        public void UpdateIcon(Image icon)
        {
            if (_iconBox != null && !_isDisposed)
            {
                try
                {
                    if (icon != null)
                    {
                        _iconBox.Image?.Dispose();
                        _iconBox.Image = icon;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"아이콘 업데이트 실패: {ex.Message}");
                }
            }
        }

        private Bitmap CreateDefaultIcon()
        {
            try
            {
                Bitmap bitmap = new Bitmap(48, 48);
                using (Graphics g = Graphics.FromImage(bitmap))
                {
                    // 배경 원
                    g.FillEllipse(Brushes.DarkBlue, 2, 2, 44, 44);
                    g.DrawEllipse(new Pen(Color.Yellow, 2), 2, 2, 44, 44);

                    // SC 텍스트
                    using (Font font = new Font("Arial", 14, FontStyle.Bold))
                    {
                        g.DrawString("SC", font, Brushes.White, new PointF(12, 14));
                    }
                }
                return bitmap;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"기본 아이콘 생성 실패: {ex.Message}");

                // 실패 시 단순한 아이콘 생성
                Bitmap fallback = new Bitmap(48, 48);
                using (Graphics g = Graphics.FromImage(fallback))
                {
                    g.FillRectangle(Brushes.Blue, 0, 0, 48, 48);
                    g.DrawString("SC", new Font("Arial", 12), Brushes.White, 10, 15);
                }
                return fallback;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (!_isDisposed && disposing)
            {
                _iconBox?.Image?.Dispose();
                _iconBox?.Dispose();
                _statusLabel?.Dispose();
                _pointerLabel?.Dispose();
                _container?.Dispose();
                _isDisposed = true;
            }

            base.Dispose(disposing);
        }
    }
}