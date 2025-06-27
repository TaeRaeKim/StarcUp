using System;
using System.Drawing;
using System.Windows.Forms;
using StarcUp.Business.GameDetection;
using StarcUp.Business.MemoryService;
using StarcUp.Common.Events;
using Timer = System.Windows.Forms.Timer;

namespace StarcUp.Presentation.Forms
{
    /// <summary>
    /// 하이브리드 감지 시스템을 활용한 향상된 컨트롤 폼
    /// </summary>
    public partial class ControlForm : Form
    {
        private readonly IGameDetector _gameDetectionService;
        private readonly IMemoryService _memoryService;
        private readonly GameDetector _hybridDetector; // 직접 참조로 상태 정보 접근

        // UI 컨트롤들
        private GroupBox _detectionStatusGroup = null!;
        private Label _detectionModeLabel = null!;
        private Label _gameStatusLabel = null!;
        private Label _processInfoLabel = null!;
        private Label _performanceLabel = null!;
        private Button _showStatusButton = null!;

        private GroupBox _gameMonitorGroup = null!;
        private Label _connectionStatusLabel = null!;
        private Button _connectToProcessButton = null!;

        private GroupBox _overlayStatusGroup = null!;
        private Label _overlayActiveLabel = null!;
        private Button _showOverlayNotificationButton = null!;

        private GroupBox _memoryInfoGroup = null!;
        private ListBox _threadStackListBox = null!;
        private Button _refreshMemoryButton = null!;

        private NotifyIcon _notifyIcon = null!;

        // 오버레이 관련
        private OverlayNotificationForm _overlayNotificationForm = null!;
        private bool _isConnectedToProcess = false;
        private bool _isOverlayActive = false;
        private bool _isDisposed = false;

        public ControlForm(IGameDetector gameDetectionService, IMemoryService memoryService)
        {
            _gameDetectionService = gameDetectionService ?? throw new ArgumentNullException(nameof(gameDetectionService));
            _memoryService = memoryService ?? throw new ArgumentNullException(nameof(memoryService));
            _hybridDetector = gameDetectionService as GameDetector; // 타입 캐스팅

            InitializeComponent();
            SetupEventHandlers();
            SetupNotifyIcon();
            UpdateUI();

            // 주기적으로 상태 업데이트
            var statusTimer = new Timer { Interval = 1000 };
            statusTimer.Tick += (s, e) => UpdateDetectionStatus();
            statusTimer.Start();
        }

        private void InitializeComponent()
        {
            this.Text = "StarcUp - 하이브리드 스타크래프트 감지";
            this.Size = new Size(600, 750);
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Icon = GetCreateApplicationIcon();

            CreateControls();
            LayoutControls();
        }

        private void CreateControls()
        {
            // 감지 상태 그룹 (새로 추가)
            _detectionStatusGroup = new GroupBox
            {
                Text = "🎯 하이브리드 감지 상태",
                Size = new Size(560, 100),
                Location = new Point(10, 10)
            };

            _detectionModeLabel = new Label
            {
                Text = "감지 모드: 폴링 모드 (2초 간격)",
                Location = new Point(10, 25),
                Size = new Size(300, 20),
                Font = new Font("맑은 고딕", 9, FontStyle.Bold),
                ForeColor = Color.Blue
            };

            _performanceLabel = new Label
            {
                Text = "성능 영향: 최소 (폴링 대기 중)",
                Location = new Point(10, 50),
                Size = new Size(300, 20),
                ForeColor = Color.Green
            };

            _showStatusButton = new Button
            {
                Text = "상세 상태",
                Size = new Size(80, 25),
                Location = new Point(450, 25),
                BackColor = Color.LightBlue
            };

            _detectionStatusGroup.Controls.AddRange(new Control[] {
                _detectionModeLabel, _performanceLabel, _showStatusButton
            });

            // 게임 모니터링 그룹
            _gameMonitorGroup = new GroupBox
            {
                Text = "게임 프로세스 모니터링",
                Size = new Size(560, 120),
                Location = new Point(10, 120)
            };

            _gameStatusLabel = new Label
            {
                Text = "게임 상태: 스타크래프트 프로세스 감지 중...",
                Location = new Point(10, 25),
                Size = new Size(540, 20),
                ForeColor = Color.Orange,
                Font = new Font("맑은 고딕", 9, FontStyle.Bold)
            };

            _processInfoLabel = new Label
            {
                Text = "프로세스 정보: --",
                Location = new Point(10, 50),
                Size = new Size(540, 20),
                Font = new Font("Consolas", 8, FontStyle.Regular)
            };

            _connectionStatusLabel = new Label
            {
                Text = "메모리 연결 상태: 연결되지 않음",
                Location = new Point(10, 75),
                Size = new Size(300, 20),
                ForeColor = Color.Red
            };

            _connectToProcessButton = new Button
            {
                Text = "프로세스에 연결",
                Size = new Size(120, 25),
                Location = new Point(420, 73),
                BackColor = Color.LightBlue,
                Enabled = false
            };

            _gameMonitorGroup.Controls.AddRange(new Control[] {
                _gameStatusLabel, _processInfoLabel, _connectionStatusLabel, _connectToProcessButton
            });

            // 오버레이 상태 그룹
            _overlayStatusGroup = new GroupBox
            {
                Text = "오버레이 상태",
                Size = new Size(560, 80),
                Location = new Point(10, 250)
            };

            _overlayActiveLabel = new Label
            {
                Text = "오버레이: 비활성화 (게임 감지 대기 중)",
                Location = new Point(10, 25),
                Size = new Size(400, 20),
                Font = new Font("맑은 고딕", 9, FontStyle.Bold),
                ForeColor = Color.Gray
            };

            _showOverlayNotificationButton = new Button
            {
                Text = "알림 다시 보기",
                Size = new Size(120, 25),
                Location = new Point(420, 23),
                BackColor = Color.LightYellow,
                Enabled = false
            };

            _overlayStatusGroup.Controls.AddRange(new Control[] {
                _overlayActiveLabel, _showOverlayNotificationButton
            });

            // 메모리 정보 그룹
            _memoryInfoGroup = new GroupBox
            {
                Text = "ThreadStack 메모리 정보",
                Size = new Size(560, 320),
                Location = new Point(10, 340)
            };

            _threadStackListBox = new ListBox
            {
                Location = new Point(10, 25),
                Size = new Size(540, 250),
                Font = new Font("Consolas", 9, FontStyle.Regular),
                ScrollAlwaysVisible = true,
                SelectionMode = SelectionMode.One
            };

            _refreshMemoryButton = new Button
            {
                Text = "메모리 정보 새로고침",
                Size = new Size(150, 30),
                Location = new Point(10, 285),
                BackColor = Color.LightGreen,
                Enabled = false
            };

            _memoryInfoGroup.Controls.AddRange(new Control[] {
                _threadStackListBox, _refreshMemoryButton
            });
        }

        private void LayoutControls()
        {
            this.Controls.AddRange(new Control[] {
                _detectionStatusGroup, _gameMonitorGroup, _overlayStatusGroup, _memoryInfoGroup
            });
        }

        private void SetupEventHandlers()
        {
            _connectToProcessButton.Click += ConnectToProcessButton_Click;
            _refreshMemoryButton.Click += RefreshMemoryButton_Click;
            _showOverlayNotificationButton.Click += ShowOverlayNotificationButton_Click;
            _showStatusButton.Click += ShowStatusButton_Click;

            // 게임 감지 서비스 이벤트
            _gameDetectionService.HandleFound += OnGameFound;
            _gameDetectionService.HandleLost += OnGameLost;
            _gameDetectionService.HandleChanged += OnGameChanged;

            // 폼 이벤트
            this.FormClosing += ControlForm_FormClosing;
            this.Resize += ControlForm_Resize;
        }

        private void SetupNotifyIcon()
        {
            _notifyIcon = new NotifyIcon
            {
                Icon = this.Icon,
                Text = "StarcUp - 하이브리드 스타크래프트 감지",
                Visible = true
            };

            var contextMenu = new ContextMenuStrip();
            contextMenu.Items.Add("열기", null, (s, e) => ShowForm());
            contextMenu.Items.Add("-");
            contextMenu.Items.Add("감지 상태", null, (s, e) => ShowDetectionStatus());
            contextMenu.Items.Add("-");
            contextMenu.Items.Add("종료", null, (s, e) => ExitApplication());

            _notifyIcon.ContextMenuStrip = contextMenu;
            _notifyIcon.DoubleClick += (s, e) => ShowForm();
        }

        private void UpdateDetectionStatus()
        {
            if (_hybridDetector == null || _isDisposed)
                return;

            try
            {
                // 현재 모드 확인
                bool isPollingMode = _hybridDetector.IsPollingMode;
                bool isGameRunning = _hybridDetector.IsGameRunning;

                if (isPollingMode)
                {
                    _detectionModeLabel.Text = "감지 모드: 🔍 폴링 모드 (2초 간격)";
                    _detectionModeLabel.ForeColor = Color.Blue;
                    _performanceLabel.Text = "성능 영향: 최소 (2초마다 프로세스 확인)";
                    _performanceLabel.ForeColor = Color.Green;
                }
                else if (isGameRunning)
                {
                    _detectionModeLabel.Text = "감지 모드: 🎯 이벤트 모드 (Process.Exited)";
                    _detectionModeLabel.ForeColor = Color.Purple;
                    _performanceLabel.Text = "성능 영향: 없음 (이벤트 대기 중)";
                    _performanceLabel.ForeColor = Color.DarkGreen;
                }
                else
                {
                    _detectionModeLabel.Text = "감지 모드: ⏸️ 대기 중";
                    _detectionModeLabel.ForeColor = Color.Gray;
                    _performanceLabel.Text = "성능 영향: 없음";
                    _performanceLabel.ForeColor = Color.Gray;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"감지 상태 업데이트 실패: {ex.Message}");
            }
        }

        private void ShowStatusButton_Click(object sender, EventArgs e)
        {
            ShowDetectionStatus();
        }

        private void ShowDetectionStatus()
        {
            if (_hybridDetector == null)
            {
                MessageBox.Show("감지 서비스 정보를 가져올 수 없습니다.", "상태 정보",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                string statusInfo = _hybridDetector.GetStatusInfo();
                MessageBox.Show(statusInfo, "하이브리드 감지 상태",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"상태 정보 조회 실패: {ex.Message}", "오류",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ConnectToProcessButton_Click(object sender, EventArgs e)
        {
            ConnectToProcess();
        }

        private void RefreshMemoryButton_Click(object sender, EventArgs e)
        {
            RefreshMemoryInfo();
        }

        private void ShowOverlayNotificationButton_Click(object sender, EventArgs e)
        {
            ShowOverlayNotification();
        }

        private void ConnectToProcess()
        {
            if (!_gameDetectionService.IsGameRunning)
            {
                MessageBox.Show("스타크래프트 프로세스가 감지되지 않았습니다.", "연결 실패",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                var gameInfo = _gameDetectionService.CurrentGame;
                bool success = _memoryService.ConnectToProcess(gameInfo.ProcessId);

                if (success)
                {
                    _isConnectedToProcess = true;
                    UpdateConnectionStatus("메모리 연결 상태: 연결됨", Color.Green);
                    _refreshMemoryButton.Enabled = true;

                    // 자동으로 메모리 정보 새로고침
                    RefreshMemoryInfo();

                    _notifyIcon.ShowBalloonTip(2000, "StarcUp",
                        $"프로세스 {gameInfo.ProcessId}에 성공적으로 연결되었습니다.", ToolTipIcon.Info);
                }
                else
                {
                    MessageBox.Show("프로세스에 연결할 수 없습니다.\n관리자 권한으로 실행해주세요.", "연결 실패",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"프로세스 연결 중 오류가 발생했습니다:\n{ex.Message}", "오류",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            UpdateUI();
        }

        private void RefreshMemoryInfo()
        {
            if (!_isConnectedToProcess || !_memoryService.IsConnected)
            {
                MessageBox.Show("프로세스에 연결되지 않았습니다.", "메모리 정보 오류",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                _threadStackListBox.Items.Clear();
                _threadStackListBox.Items.Add("=== ThreadStack 메모리 정보 조회 중... ===");
                Application.DoEvents();

                // TEB 주소들 가져오기
                var tebInfos = _memoryService.GetTebAddresses();

                _threadStackListBox.Items.Clear();
                _threadStackListBox.Items.Add($"=== 총 {tebInfos.Count}개의 스레드 발견 ===");
                _threadStackListBox.Items.Add("");

                if (tebInfos.Count == 0)
                {
                    _threadStackListBox.Items.Add("❌ 스레드 정보를 찾을 수 없습니다.");
                    return;
                }

                // 각 스레드의 StackStart 주소 조회
                for (int i = 0; i < tebInfos.Count; i++)
                {
                    var tebInfo = tebInfos[i];

                    _threadStackListBox.Items.Add($"🔸 스레드 #{i + 1}:");
                    _threadStackListBox.Items.Add($"   Thread ID: {tebInfo.ThreadId}");
                    _threadStackListBox.Items.Add($"   TEB Address: 0x{tebInfo.TebAddress.ToInt64():X16}");

                    // StackStart 주소 가져오기
                    nint stackStart = _memoryService.GetStackStart(i);
                    if (stackStart != 0)
                    {
                        _threadStackListBox.Items.Add($"   ✅ StackStart: 0x{stackStart.ToInt64():X16}");
                    }
                    else
                    {
                        _threadStackListBox.Items.Add($"   ❌ StackStart: 가져올 수 없음");
                    }

                    _threadStackListBox.Items.Add("");
                }

                _threadStackListBox.Items.Add("=== 메모리 정보 조회 완료 ===");
                Console.WriteLine($"메모리 정보 새로고침 완료 - {tebInfos.Count}개 스레드");
            }
            catch (Exception ex)
            {
                _threadStackListBox.Items.Clear();
                _threadStackListBox.Items.Add("❌ 메모리 정보 조회 실패:");
                _threadStackListBox.Items.Add($"   오류: {ex.Message}");

                Console.WriteLine($"메모리 정보 새로고침 실패: {ex.Message}");
            }
        }

        private void ShowOverlayNotification()
        {
            if (!_gameDetectionService.IsGameRunning)
            {
                MessageBox.Show("스타크래프트 프로세스가 감지되지 않았습니다.", "알림 표시 실패",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                // 기존 알림 폼이 있다면 닫기
                _overlayNotificationForm?.CloseForm();

                // 새 알림 폼 생성 및 표시
                _overlayNotificationForm = new OverlayNotificationForm(_gameDetectionService.CurrentGame);
                _overlayNotificationForm.OverlayActivationRequested += OnOverlayActivationRequested;
                _overlayNotificationForm.FormClosed += OnOverlayNotificationClosed;
                _overlayNotificationForm.Show();

                Console.WriteLine("오버레이 알림 폼 표시됨");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"오버레이 알림 표시 실패: {ex.Message}");
                MessageBox.Show($"오버레이 알림 표시 중 오류가 발생했습니다:\n{ex.Message}", "오류",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void OnGameFound(object sender, GameEventArgs e)
        {
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new Action<object, GameEventArgs>(OnGameFound), sender, e);
                return;
            }

            UpdateGameStatus($"게임 상태: 🎮 스타크래프트 발견! ({e.GameInfo.ProcessName})", Color.Green);
            UpdateProcessInfo($"프로세스 정보: PID={e.GameInfo.ProcessId}, Handle=0x{e.GameInfo.WindowHandle.ToInt64():X}");
            UpdateOverlayStatus("오버레이: 게임 감지됨 - 활성화 가능", Color.Blue);

            _connectToProcessButton.Enabled = true;
            _showOverlayNotificationButton.Enabled = true;

            // 🎉 자동으로 오버레이 알림 폼 표시
            ShowOverlayNotification();

            _notifyIcon.ShowBalloonTip(3000, "StarcUp",
                $"🎮 스타크래프트가 감지되었습니다!\nPID: {e.GameInfo.ProcessId}\n감지 모드가 이벤트 모드로 전환됩니다.", ToolTipIcon.Info);

            Console.WriteLine($"게임 발견: {e.GameInfo}");
        }

        private void OnGameLost(object sender, GameEventArgs e)
        {
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new Action<object, GameEventArgs>(OnGameLost), sender, e);
                return;
            }

            UpdateGameStatus("게임 상태: 스타크래프트 프로세스 감지 중...", Color.Orange);
            UpdateProcessInfo("프로세스 정보: --");
            UpdateOverlayStatus("오버레이: 비활성화 (게임 감지 대기 중)", Color.Gray);

            // 오버레이 알림 폼 닫기
            _overlayNotificationForm?.CloseForm();
            _isOverlayActive = false;

            // 메모리 연결 해제
            if (_isConnectedToProcess)
            {
                _memoryService.Disconnect();
                _isConnectedToProcess = false;
                UpdateConnectionStatus("메모리 연결 상태: 연결되지 않음 (게임 종료)", Color.Red);
                _threadStackListBox.Items.Clear();
                _refreshMemoryButton.Enabled = false;
            }

            _connectToProcessButton.Enabled = false;
            _showOverlayNotificationButton.Enabled = false;

            _notifyIcon.ShowBalloonTip(2000, "StarcUp",
                "🛑 스타크래프트가 종료되었습니다.\n감지 모드가 폴링 모드로 전환됩니다.", ToolTipIcon.Warning);

            Console.WriteLine($"게임 종료: {e.GameInfo}");
        }

        private void OnGameChanged(object sender, GameEventArgs e)
        {
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new Action<object, GameEventArgs>(OnGameChanged), sender, e);
                return;
            }

            UpdateProcessInfo($"프로세스 정보: PID={e.GameInfo.ProcessId}, Handle=0x{e.GameInfo.WindowHandle.ToInt64():X} (변경됨)");

            // 기존 연결이 있다면 재연결 필요
            if (_isConnectedToProcess)
            {
                _memoryService.Disconnect();
                _isConnectedToProcess = false;
                UpdateConnectionStatus("메모리 연결 상태: 재연결 필요 (프로세스 변경)", Color.Orange);
                _threadStackListBox.Items.Clear();
                _refreshMemoryButton.Enabled = false;
            }

            // 새로운 게임에 대한 알림 표시
            ShowOverlayNotification();

            Console.WriteLine($"게임 변경: {e.GameInfo}");
        }

        private void OnOverlayActivationRequested(object sender, EventArgs e)
        {
            try
            {
                Console.WriteLine("사용자가 오버레이 활성화를 요청했습니다.");

                _isOverlayActive = true;
                UpdateOverlayStatus("오버레이: 활성화됨 ✓", Color.Green);

                _notifyIcon.ShowBalloonTip(2000, "StarcUp",
                    "오버레이가 활성화되었습니다!", ToolTipIcon.Info);

                // 여기서 실제 오버레이 로직을 구현할 수 있습니다
                // 예: 실제 게임 오버레이 폼 생성 및 표시

            }
            catch (Exception ex)
            {
                Console.WriteLine($"오버레이 활성화 처리 실패: {ex.Message}");
            }
        }

        private void OnOverlayNotificationClosed(object sender, EventArgs e)
        {
            Console.WriteLine("오버레이 알림 폼이 닫혔습니다.");
            _overlayNotificationForm = null;
        }

        private void UpdateGameStatus(string status, Color color)
        {
            if (_gameStatusLabel != null)
            {
                _gameStatusLabel.Text = status;
                _gameStatusLabel.ForeColor = color;
            }
        }

        private void UpdateProcessInfo(string info)
        {
            if (_processInfoLabel != null)
            {
                _processInfoLabel.Text = info;
            }
        }

        private void UpdateConnectionStatus(string status, Color color)
        {
            if (_connectionStatusLabel != null)
            {
                _connectionStatusLabel.Text = status;
                _connectionStatusLabel.ForeColor = color;
            }
        }

        private void UpdateOverlayStatus(string status, Color color)
        {
            if (_overlayActiveLabel != null)
            {
                _overlayActiveLabel.Text = status;
                _overlayActiveLabel.ForeColor = color;
            }
        }

        private void UpdateUI()
        {
            if (_gameDetectionService.IsGameRunning)
            {
                _connectToProcessButton.Enabled = !_isConnectedToProcess;
                _showOverlayNotificationButton.Enabled = true;
            }
            else
            {
                _connectToProcessButton.Enabled = false;
                _showOverlayNotificationButton.Enabled = false;
            }

            _refreshMemoryButton.Enabled = _isConnectedToProcess && _memoryService.IsConnected;
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
            if (_isConnectedToProcess)
            {
                _memoryService.Disconnect();
            }

            // 오버레이 알림 폼 정리
            _overlayNotificationForm?.CloseForm();

            // 게임 감지 서비스 중지
            _gameDetectionService.StopDetection();

            _notifyIcon.Visible = false;
            Application.Exit();
        }

        private static Icon GetCreateApplicationIcon()
        {
            try
            {
                Bitmap bitmap = new(32, 32);
                using (Graphics g = Graphics.FromImage(bitmap))
                {
                    g.FillEllipse(Brushes.DarkBlue, 2, 2, 28, 28);
                    g.DrawEllipse(new Pen(Color.Yellow, 2), 2, 2, 28, 28);
                    using Font font = new("Arial", 10, FontStyle.Bold);
                    g.DrawString("SC", font, Brushes.White, new PointF(8, 8));
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
                if (_isConnectedToProcess)
                {
                    _memoryService.Disconnect();
                }

                _overlayNotificationForm?.CloseForm();
                _notifyIcon?.Dispose();
                _isDisposed = true;
            }
            base.Dispose(disposing);
        }
    }

}