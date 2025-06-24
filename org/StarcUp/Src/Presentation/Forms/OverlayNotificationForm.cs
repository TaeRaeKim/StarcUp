using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using StarcUp.Business.GameDetection;
using Timer = System.Windows.Forms.Timer;

namespace StarcUp.Presentation.Forms
{
    /// <summary>
    /// 스타크래프트 감지 시 나타나는 오버레이 활성화 알림 폼
    /// </summary>
    public partial class OverlayNotificationForm : Form
    {
        #region Win32 API for Overlay

        [DllImport("user32.dll", SetLastError = true)]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetLayeredWindowAttributes(IntPtr hwnd, uint crKey, byte bAlpha, uint dwFlags);

        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_LAYERED = 0x80000;
        private const int WS_EX_TOPMOST = 0x8;
        private const uint LWA_ALPHA = 0x2;

        #endregion

        #region Private Fields

        private GameInfo _gameInfo;
        private Timer _animationTimer;
        private Timer _autoHideTimer;
        private int _animationStep = 0;
        private bool _isAnimating = true;
        private bool _isDisposed = false;

        // UI 컨트롤들
        private Panel _mainPanel;
        private Label _titleLabel;
        private Label _gameInfoLabel;
        private PictureBox _sampleImagePictureBox;
        private Button _activateButton;
        private Button _closeButton;
        private ProgressBar _autoHideProgressBar;

        #endregion

        #region Events

        public event EventHandler OverlayActivationRequested;
        public event EventHandler FormClosed;

        #endregion

        #region Constructor

        public OverlayNotificationForm(GameInfo gameInfo)
        {
            _gameInfo = gameInfo ?? throw new ArgumentNullException(nameof(gameInfo));

            InitializeComponent();
            SetupOverlayProperties();
            CreateSampleImage();
            SetupTimers();
            PositionForm();

            Console.WriteLine($"[OverlayNotificationForm] 오버레이 알림 폼 생성됨: {gameInfo.ProcessName}");
        }

        #endregion

        #region Initialization

        private void InitializeComponent()
        {
            this.Text = "StarcUp - 오버레이 활성화";
            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition = FormStartPosition.Manual;
            this.Size = new Size(400, 300);
            this.BackColor = Color.FromArgb(240, 240, 240);
            this.ShowInTaskbar = false;
            this.TopMost = true;

            CreateControls();
            LayoutControls();
        }

        private void CreateControls()
        {
            // 메인 패널
            _mainPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(10),
                BackColor = Color.FromArgb(45, 45, 48)
            };

            // 제목 라벨
            _titleLabel = new Label
            {
                Text = "🎮 오버레이 활성화",
                Font = new Font("맑은 고딕", 16, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 120, 215),
                TextAlign = ContentAlignment.MiddleCenter,
                Size = new Size(380, 40),
                Location = new Point(10, 10)
            };

            // 게임 정보 라벨
            _gameInfoLabel = new Label
            {
                Text = $"스타크래프트가 감지되었습니다!\nPID: {_gameInfo.ProcessId} | 프로세스: {_gameInfo.ProcessName}",
                Font = new Font("맑은 고딕", 10, FontStyle.Regular),
                ForeColor = Color.White,
                TextAlign = ContentAlignment.MiddleCenter,
                Size = new Size(380, 40),
                Location = new Point(10, 55)
            };

            // 샘플 이미지 표시
            _sampleImagePictureBox = new PictureBox
            {
                Size = new Size(200, 100),
                Location = new Point(100, 105),
                SizeMode = PictureBoxSizeMode.Zoom,
                BorderStyle = BorderStyle.FixedSingle
            };

            // 활성화 버튼
            _activateButton = new Button
            {
                Text = "오버레이 활성화",
                Font = new Font("맑은 고딕", 10, FontStyle.Bold),
                Size = new Size(120, 35),
                Location = new Point(90, 220),
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            _activateButton.FlatAppearance.BorderSize = 0;

            // 닫기 버튼
            _closeButton = new Button
            {
                Text = "나중에",
                Font = new Font("맑은 고딕", 9, FontStyle.Regular),
                Size = new Size(80, 35),
                Location = new Point(220, 220),
                BackColor = Color.FromArgb(99, 99, 99),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            _closeButton.FlatAppearance.BorderSize = 0;

            // 자동 숨김 프로그레스바
            _autoHideProgressBar = new ProgressBar
            {
                Size = new Size(380, 8),
                Location = new Point(10, 270),
                Style = ProgressBarStyle.Continuous,
                Maximum = 100,
                Value = 100,
                ForeColor = Color.FromArgb(0, 120, 215)
            };

            _mainPanel.Controls.AddRange(new Control[] {
                _titleLabel, _gameInfoLabel, _sampleImagePictureBox,
                _activateButton, _closeButton, _autoHideProgressBar
            });
        }

        private void LayoutControls()
        {
            this.Controls.Add(_mainPanel);
        }

        private void SetupOverlayProperties()
        {
            try
            {
                // 레이어드 윈도우 설정
                var exStyle = GetWindowLong(this.Handle, GWL_EXSTYLE);
                exStyle |= WS_EX_LAYERED | WS_EX_TOPMOST;
                SetWindowLong(this.Handle, GWL_EXSTYLE, exStyle);

                // 95% 불투명도
                SetLayeredWindowAttributes(this.Handle, 0, 242, LWA_ALPHA);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[OverlayNotificationForm] 오버레이 속성 설정 실패: {ex.Message}");
            }
        }

        private void CreateSampleImage()
        {
            try
            {
                // 샘플 오버레이 이미지 생성
                Bitmap sampleBitmap = new Bitmap(200, 100);
                using (Graphics g = Graphics.FromImage(sampleBitmap))
                {
                    // 그라데이션 배경
                    using (LinearGradientBrush brush = new LinearGradientBrush(
                        new Rectangle(0, 0, 200, 100),
                        Color.FromArgb(150, 0, 0, 0),
                        Color.FromArgb(200, 0, 0, 0),
                        LinearGradientMode.Vertical))
                    {
                        g.FillRectangle(brush, 0, 0, 200, 100);
                    }

                    // 테두리
                    using (Pen borderPen = new Pen(Color.Yellow, 2))
                    {
                        g.DrawRectangle(borderPen, 1, 1, 198, 98);
                    }

                    // 텍스트
                    using (Font font = new Font("맑은 고딕", 12, FontStyle.Bold))
                    {
                        g.DrawString("StarcUp", font, Brushes.Yellow, new PointF(10, 10));
                        g.DrawString("포인터: 1337", font, Brushes.LightGreen, new PointF(10, 35));
                        g.DrawString("상태: 활성화됨", font, Brushes.LightBlue, new PointF(10, 60));
                    }

                    // 아이콘
                    using (SolidBrush iconBrush = new SolidBrush(Color.FromArgb(0, 120, 215)))
                    {
                        g.FillEllipse(iconBrush, 150, 15, 30, 30);
                        using (Font iconFont = new Font("Arial", 10, FontStyle.Bold))
                        {
                            g.DrawString("SC", iconFont, Brushes.White, new PointF(158, 25));
                        }
                    }
                }

                _sampleImagePictureBox.Image = sampleBitmap;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[OverlayNotificationForm] 샘플 이미지 생성 실패: {ex.Message}");
            }
        }

        private void SetupTimers()
        {
            // 애니메이션 타이머 (제목 깜빡임)
            _animationTimer = new Timer
            {
                Interval = 500
            };
            _animationTimer.Tick += AnimationTimer_Tick;
            _animationTimer.Start();

            // 자동 숨김 타이머 (10초 후 자동 닫기)
            _autoHideTimer = new Timer
            {
                Interval = 100
            };
            _autoHideTimer.Tick += AutoHideTimer_Tick;
            _autoHideTimer.Start();
        }

        private void PositionForm()
        {
            // 게임 윈도우 위치를 기준으로 오버레이 위치 결정
            if (_gameInfo.WindowBounds != Rectangle.Empty)
            {
                int x = _gameInfo.WindowBounds.Left + (_gameInfo.WindowBounds.Width - this.Width) / 2;
                int y = _gameInfo.WindowBounds.Top + 50;
                this.Location = new Point(x, y);
            }
            else
            {
                // 화면 중앙에 배치
                this.Location = new Point(
                    (Screen.PrimaryScreen.WorkingArea.Width - this.Width) / 2,
                    (Screen.PrimaryScreen.WorkingArea.Height - this.Height) / 2
                );
            }
        }

        #endregion

        #region Event Handlers

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            // 이벤트 핸들러 설정
            _activateButton.Click += ActivateButton_Click;
            _closeButton.Click += CloseButton_Click;

            // 폼 클릭으로도 닫기 가능
            this.Click += (s, ev) => CloseForm();
            _mainPanel.Click += (s, ev) => CloseForm();
        }

        private void ActivateButton_Click(object sender, EventArgs e)
        {
            try
            {
                Console.WriteLine("[OverlayNotificationForm] 오버레이 활성화 요청됨");
                OverlayActivationRequested?.Invoke(this, EventArgs.Empty);
                CloseForm();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[OverlayNotificationForm] 오버레이 활성화 요청 실패: {ex.Message}");
            }
        }

        private void CloseButton_Click(object sender, EventArgs e)
        {
            CloseForm();
        }

        private void AnimationTimer_Tick(object sender, EventArgs e)
        {
            if (_isAnimating)
            {
                _animationStep++;

                // 제목 색상 애니메이션 (깜빡임)
                if (_animationStep % 2 == 0)
                {
                    _titleLabel.ForeColor = Color.FromArgb(0, 120, 215);
                }
                else
                {
                    _titleLabel.ForeColor = Color.FromArgb(0, 200, 255);
                }
            }
        }

        private void AutoHideTimer_Tick(object sender, EventArgs e)
        {
            if (_autoHideProgressBar.Value > 0)
            {
                _autoHideProgressBar.Value -= 1;
            }
            else
            {
                // 10초 후 자동 닫기
                CloseForm();
            }
        }

        #endregion

        #region Public Methods

        public void CloseForm()
        {
            if (_isDisposed)
                return;

            try
            {
                Console.WriteLine("[OverlayNotificationForm] 오버레이 알림 폼 닫기");
                FormClosed?.Invoke(this, EventArgs.Empty);
                this.Hide();
                this.Dispose();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[OverlayNotificationForm] 폼 닫기 실패: {ex.Message}");
            }
        }

        #endregion

        #region Protected Methods

        protected override CreateParams CreateParams
        {
            get
            {
                var cp = base.CreateParams;
                cp.ExStyle |= WS_EX_LAYERED | WS_EX_TOPMOST;
                return cp;
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            // 닫기 버튼 클릭 시 숨기기만 함
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                CloseForm();
                return;
            }

            base.OnFormClosing(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            try
            {
                base.OnPaint(e);

                // 테두리 그리기
                using (Pen borderPen = new Pen(Color.FromArgb(0, 120, 215), 2))
                {
                    e.Graphics.DrawRectangle(borderPen, 0, 0, this.Width - 1, this.Height - 1);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[OverlayNotificationForm] OnPaint 오류: {ex.Message}");
            }
        }

        #endregion

        #region Dispose

        protected override void Dispose(bool disposing)
        {
            if (!_isDisposed && disposing)
            {
                Console.WriteLine("[OverlayNotificationForm] 오버레이 알림 폼 해제 시작");

                try
                {
                    _isAnimating = false;

                    _animationTimer?.Stop();
                    _animationTimer?.Dispose();

                    _autoHideTimer?.Stop();
                    _autoHideTimer?.Dispose();

                    _sampleImagePictureBox?.Image?.Dispose();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[OverlayNotificationForm] 리소스 해제 중 오류: {ex.Message}");
                }

                _isDisposed = true;
                Console.WriteLine("[OverlayNotificationForm] 오버레이 알림 폼 해제 완료");
            }

            base.Dispose(disposing);
        }

        #endregion
    }
}