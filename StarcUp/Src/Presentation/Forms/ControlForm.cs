using System;
using System.Drawing;
using System.Windows.Forms;
using StarcUp.Business.Interfaces;
using StarcUp.Business.Models;
using StarcUp.Common.Events;

namespace StarcUp.Presentation.Forms
{
    /// <summary>
    /// 메인 컨트롤 폼 - 사용자가 추적을 시작/정지할 수 있는 컨트롤 패널
    /// </summary>
    public partial class ControlForm : Form
    {
        private readonly IOverlayService _overlayService;
        private readonly IGameDetectionService _gameDetectionService;
        private readonly IPointerMonitorService _pointerMonitorService;

        // UI 컨트롤들
        private Button _startTrackingButton;
        private Button _stopTrackingButton;
        private Label _statusLabel;
        private Label _gameStatusLabel;
        private Label _pointerValueLabel;
        private GroupBox _gameInfoGroup;
        private GroupBox _trackingGroup;
        private GroupBox _settingsGroup;
        private CheckBox _autoStartCheckbox;
        private CheckBox _showOnlyActiveCheckbox;
        private NumericUpDown _offsetXNumeric;
        private NumericUpDown _offsetYNumeric;
        private NotifyIcon _notifyIcon;

        private bool _isTracking = false;
        private bool _isDisposed = false;

        public ControlForm(IOverlayService overlayService,
                          IGameDetectionService gameDetectionService,
                          IPointerMonitorService pointerMonitorService)
        {
            _overlayService = overlayService ?? throw new ArgumentNullException(nameof(overlayService));
            _gameDetectionService = gameDetectionService ?? throw new ArgumentNullException(nameof(gameDetectionService));
            _pointerMonitorService = pointerMonitorService ?? throw new ArgumentNullException(nameof(pointerMonitorService));

            InitializeComponent();
            SetupEventHandlers();
            SetupNotifyIcon();
            UpdateButtonStates();
        }

        private void InitializeComponent()
        {
            this.Text = "StarcUp - 스타크래프트 오버레이 컨트롤";
            this.Size = new Size(400, 500);
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Icon = CreateApplicationIcon();

            CreateControls();
            LayoutControls();
        }

        private void CreateControls()
        {
            // 게임 정보 그룹
            _gameInfoGroup = new GroupBox
            {
                Text = "게임 상태",
                Size = new Size(360, 80),
                Location = new Point(10, 10)
            };

            _gameStatusLabel = new Label
            {
                Text = "게임: 감지되지 않음",
                Location = new Point(10, 25),
                Size = new Size(340, 20),
                ForeColor = Color.Red
            };

            _gameInfoGroup.Controls.Add(_gameStatusLabel);

            // 추적 컨트롤 그룹
            _trackingGroup = new GroupBox
            {
                Text = "포인터 추적",
                Size = new Size(360, 120),
                Location = new Point(10, 100)
            };

            _startTrackingButton = new Button
            {
                Text = "포인터 추적 시작",
                Size = new Size(120, 30),
                Location = new Point(10, 25),
                BackColor = Color.LightGreen
            };

            _stopTrackingButton = new Button
            {
                Text = "포인터 추적 중지",
                Size = new Size(120, 30),
                Location = new Point(140, 25),
                BackColor = Color.LightCoral,
                Enabled = false
            };

            _statusLabel = new Label
            {
                Text = "상태: 게임 감지 대기 중",
                Location = new Point(10, 65),
                Size = new Size(340, 20)
            };

            _pointerValueLabel = new Label
            {
                Text = "포인터 값: --",
                Location = new Point(10, 85),
                Size = new Size(340, 20),
                Font = new Font("Consolas", 9, FontStyle.Regular)
            };

            _trackingGroup.Controls.AddRange(new Control[] {
                _startTrackingButton, _stopTrackingButton, _statusLabel, _pointerValueLabel
            });

            // 설정 그룹
            _settingsGroup = new GroupBox
            {
                Text = "설정",
                Size = new Size(360, 150),
                Location = new Point(10, 230)
            };

            _autoStartCheckbox = new CheckBox
            {
                Text = "게임 감지 시 자동으로 포인터 추적 시작",
                Location = new Point(10, 25),
                Size = new Size(280, 20),
                Checked = false
            };

            _showOnlyActiveCheckbox = new CheckBox
            {
                Text = "게임이 활성화된 경우에만 오버레이 표시",
                Location = new Point(10, 50),
                Size = new Size(300, 20),
                Checked = true
            };

            // 오프셋 설정
            var offsetXLabel = new Label
            {
                Text = "X 오프셋:",
                Location = new Point(10, 80),
                Size = new Size(60, 20)
            };

            _offsetXNumeric = new NumericUpDown
            {
                Location = new Point(75, 78),
                Size = new Size(60, 20),
                Minimum = 0,
                Maximum = 500,
                Value = 15
            };

            var offsetYLabel = new Label
            {
                Text = "Y 오프셋:",
                Location = new Point(150, 80),
                Size = new Size(60, 20)
            };

            _offsetYNumeric = new NumericUpDown
            {
                Location = new Point(215, 78),
                Size = new Size(60, 20),
                Minimum = 0,
                Maximum = 500,
                Value = 10
            };

            var resetButton = new Button
            {
                Text = "기본값 복원",
                Location = new Point(10, 110),
                Size = new Size(100, 25)
            };
            resetButton.Click += ResetButton_Click;

            _settingsGroup.Controls.AddRange(new Control[] {
                _autoStartCheckbox, _showOnlyActiveCheckbox,
                offsetXLabel, _offsetXNumeric, offsetYLabel, _offsetYNumeric,
                resetButton
            });
        }

        private void LayoutControls()
        {
            this.Controls.AddRange(new Control[] {
                _gameInfoGroup, _trackingGroup, _settingsGroup
            });
        }

        private void SetupEventHandlers()
        {
            _startTrackingButton.Click += StartTrackingButton_Click;
            _stopTrackingButton.Click += StopTrackingButton_Click;

            // 게임 감지 서비스 이벤트 (항상 감지)
            _gameDetectionService.HandleFound += OnHandleFound;
            _gameDetectionService.HandleLost += OnHandleLost;

            // 포인터 모니터링 서비스 이벤트
            _pointerMonitorService.ValueChanged += OnPointerValueChanged;

            // 폼 이벤트
            this.FormClosing += ControlForm_FormClosing;
            this.Resize += ControlForm_Resize;
        }

        private void SetupNotifyIcon()
        {
            _notifyIcon = new NotifyIcon
            {
                Icon = this.Icon,
                Text = "StarcUp - 스타크래프트 오버레이",
                Visible = true
            };

            var contextMenu = new ContextMenuStrip();
            contextMenu.Items.Add("열기", null, (s, e) => ShowForm());
            contextMenu.Items.Add("포인터 추적 시작", null, (s, e) => StartTracking());
            contextMenu.Items.Add("포인터 추적 중지", null, (s, e) => StopTracking());
            contextMenu.Items.Add("-");
            contextMenu.Items.Add("종료", null, (s, e) => ExitApplication());

            _notifyIcon.ContextMenuStrip = contextMenu;
            _notifyIcon.DoubleClick += (s, e) => ShowForm();
        }

        private void StartTrackingButton_Click(object sender, EventArgs e)
        {
            StartTracking();
        }

        private void StopTrackingButton_Click(object sender, EventArgs e)
        {
            StopTracking();
        }

        private void StartTracking()
        {
            if (_isTracking)
                return;

            try
            {
                Console.WriteLine("사용자가 포인터 추적 시작을 요청했습니다.");

                // 오버레이 서비스 시작 (게임 감지는 이미 실행 중)
                _overlayService.Start();

                _isTracking = true;
                UpdateButtonStates();
                UpdateStatus("포인터 추적 시작됨");

                // 토스트 알림
                _notifyIcon.ShowBalloonTip(2000, "StarcUp", "포인터 추적이 시작되었습니다.", ToolTipIcon.Info);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"포인터 추적 시작 중 오류가 발생했습니다:\n{ex.Message}", "오류",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void StopTracking()
        {
            if (!_isTracking)
                return;

            try
            {
                Console.WriteLine("사용자가 포인터 추적 중지를 요청했습니다.");

                // 오버레이 서비스만 중지 (게임 감지는 계속 실행)
                _overlayService.Stop();

                _isTracking = false;
                UpdateButtonStates();
                UpdateStatus("포인터 추적 중지됨 (게임 감지는 계속됨)");
                UpdatePointerValue(null);

                // 토스트 알림
                _notifyIcon.ShowBalloonTip(2000, "StarcUp", "포인터 추적이 중지되었습니다.", ToolTipIcon.Info);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"포인터 추적 중지 중 오류가 발생했습니다:\n{ex.Message}", "오류",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void OnHandleFound(object sender, GameEventArgs e)
        {
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new Action<object, GameEventArgs>(OnHandleFound), sender, e);
                return;
            }

            UpdateGameStatus($"게임: {e.GameInfo.ProcessName} (PID: {e.GameInfo.ProcessId}) - 연결됨", Color.Green);

            // 자동 시작 옵션이 켜져있고 아직 추적하지 않는 경우에만 자동 시작
            if (_autoStartCheckbox.Checked && !_isTracking)
            {
                StartTracking();
            }
        }

        private void OnHandleLost(object sender, GameEventArgs e)
        {
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new Action<object, GameEventArgs>(OnHandleLost), sender, e);
                return;
            }

            UpdateGameStatus("게임: 감지되지 않음", Color.Red);
            // 게임이 종료되어도 포인터 값만 초기화 (추적 상태는 유지)
            UpdatePointerValue(null);
        }

        private void OnPointerValueChanged(object sender, PointerEventArgs e)
        {
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new Action<object, PointerEventArgs>(OnPointerValueChanged), sender, e);
                return;
            }

            UpdatePointerValue(e.PointerValue);
        }

        private void UpdateButtonStates()
        {
            _startTrackingButton.Enabled = !_isTracking;
            _stopTrackingButton.Enabled = _isTracking;
        }

        private void UpdateStatus(string status)
        {
            if (_statusLabel != null)
            {
                _statusLabel.Text = $"상태: {status}";
            }
        }

        private void UpdateGameStatus(string status, Color color)
        {
            if (_gameStatusLabel != null)
            {
                _gameStatusLabel.Text = status;
                _gameStatusLabel.ForeColor = color;
            }
        }

        private void UpdatePointerValue(PointerValue pointerValue)
        {
            if (_pointerValueLabel != null)
            {
                if (pointerValue != null)
                {
                    string text = $"포인터 값: {pointerValue.NewValue}";
                    if (pointerValue.HasChanged)
                    {
                        text += $" (변화: {pointerValue.Difference:+#;-#;0})";
                    }
                    _pointerValueLabel.Text = text;
                    _pointerValueLabel.ForeColor = pointerValue.HasChanged ? Color.Blue : Color.Black;
                }
                else
                {
                    _pointerValueLabel.Text = "포인터 값: --";
                    _pointerValueLabel.ForeColor = Color.Gray;
                }
            }
        }

        private void ResetButton_Click(object sender, EventArgs e)
        {
            _offsetXNumeric.Value = 15;
            _offsetYNumeric.Value = 10;
            _autoStartCheckbox.Checked = false;
            _showOnlyActiveCheckbox.Checked = true;
        }

        private void ControlForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                // X 버튼을 눌렀을 때는 트레이로 최소화
                e.Cancel = true;
                this.Hide();
                _notifyIcon.ShowBalloonTip(1000, "StarcUp", "트레이에서 계속 실행됩니다.", ToolTipIcon.Info);
            }
        }

        private void ControlForm_Resize(object sender, EventArgs e)
        {
            if (this.WindowState == FormWindowState.Minimized)
            {
                this.Hide();
            }
        }

        private void ShowForm()
        {
            this.Show();
            this.WindowState = FormWindowState.Normal;
            this.BringToFront();
            this.Activate();
        }

        private void ExitApplication()
        {
            if (_isTracking)
            {
                StopTracking();
            }

            // 게임 감지 서비스도 완전히 중지
            _gameDetectionService.StopDetection();

            _notifyIcon.Visible = false;
            Application.Exit();
        }

        private Icon CreateApplicationIcon()
        {
            try
            {
                Bitmap bitmap = new Bitmap(32, 32);
                using (Graphics g = Graphics.FromImage(bitmap))
                {
                    g.FillEllipse(Brushes.DarkBlue, 2, 2, 28, 28);
                    g.DrawEllipse(new Pen(Color.Yellow, 2), 2, 2, 28, 28);
                    using (Font font = new Font("Arial", 10, FontStyle.Bold))
                    {
                        g.DrawString("SC", font, Brushes.White, new PointF(8, 8));
                    }
                }
                return Icon.FromHandle(bitmap.GetHicon());
            }
            catch
            {
                return SystemIcons.Application;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (!_isDisposed && disposing)
            {
                if (_isTracking)
                {
                    StopTracking();
                }

                _notifyIcon?.Dispose();
                _isDisposed = true;
            }
            base.Dispose(disposing);
        }
    }
}