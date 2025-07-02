using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Windows.Forms;
using Timer = System.Windows.Forms.Timer;

namespace StarcUp.Presentation.Forms
{
    /// <summary>
    /// 스타크래프트 위에 오버레이되는 게임 정보 표시 폼
    /// </summary>
    public partial class GameOverlayForm : Form
    {
        #region Win32 API for Overlay

        [DllImport("user32.dll", SetLastError = true)]
        private static extern int GetWindowLong(nint hWnd, int nIndex);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern int SetWindowLong(nint hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetLayeredWindowAttributes(nint hwnd, uint crKey, byte bAlpha, uint dwFlags);

        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_LAYERED = 0x80000;
        private const int WS_EX_TOPMOST = 0x8;
        private const int WS_EX_TRANSPARENT = 0x20;
        private const uint LWA_ALPHA = 0x2;

        #endregion

        #region Private Fields

        private bool _isDisposed = false;
        private Timer _dataUpdateTimer;
        private Timer _simulationTimer;

        // 오버레이 컴포넌트들
        private Panel _workerStatusPanel;
        private Panel _buildOrderPanel;
        private Panel _unitCountPanel;
        private Panel _upgradeProgressPanel;
        private Panel _populationWarningPanel;

        // 시뮬레이션 데이터
        private int _totalWorkers = 24;
        private int _idleWorkers = 2;
        private bool _isDeathFlash = false;
        private Random _random = new Random();

        // 드래그 이동 관련
        private bool _isDragMode = false;
        private readonly List<Panel> _draggablePanels = new List<Panel>();
        private Point _dragOffset;
        private bool _isDragging = false;
        private Panel _draggingPanel;
        private Panel _hoveredPanel; // hover 상태 추적용
        
        // 위치 저장 파일 경로
        private readonly string _positionsFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "StarcUp",
            "overlay_positions.json"
        );

        // 디자인 상수
        private readonly Color OVERLAY_BACKGROUND = Color.FromArgb(216, 0, 0, 0); // rgba(0,0,0,0.85)
        private readonly Color OVERLAY_BORDER = Color.FromArgb(51, 255, 255, 255); // rgba(255,255,255,0.2)
        private readonly Color TEXT_PRIMARY = Color.White;
        private readonly Color TEXT_SECONDARY = Color.FromArgb(224, 224, 224);
        private readonly Color BRAND_PRIMARY = Color.FromArgb(0, 153, 255);
        private readonly Color BRAND_SECONDARY = Color.FromArgb(255, 107, 53);
        private readonly Color SUCCESS_COLOR = Color.FromArgb(0, 208, 132);
        private readonly Color WARNING_COLOR = Color.FromArgb(255, 184, 0);
        private readonly Color ERROR_COLOR = Color.FromArgb(255, 59, 48);

        #endregion

        #region Constructor

        public GameOverlayForm()
        {
            // 더블 버퍼링 활성화 (깜빡임 방지)
            this.SetStyle(ControlStyles.AllPaintingInWmPaint | 
                         ControlStyles.UserPaint | 
                         ControlStyles.DoubleBuffer | 
                         ControlStyles.ResizeRedraw, true);
            
            InitializeComponent();
            SetupOverlayProperties();
            CreateOverlayComponents();
            LoadPositions(); // 저장된 위치 복원
            SetupTimers();
            PositionForm();

            Console.WriteLine("[GameOverlayForm] 게임 오버레이 폼 생성됨");
        }

        #endregion

        #region Initialization

        private void InitializeComponent()
        {
            this.Text = "StarcUp - Game Overlay";
            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition = FormStartPosition.Manual;
            this.Size = new Size(1920, 1080); // 기준 해상도
            this.BackColor = Color.Magenta; // 투명 처리할 색상
            this.TransparencyKey = Color.Magenta;
            this.ShowInTaskbar = false;
            this.TopMost = true;
            this.WindowState = FormWindowState.Maximized;
            
            // 마우스 클릭이 폼을 통과하도록 설정
            this.SetStyle(ControlStyles.SupportsTransparentBackColor, true);
            this.SetStyle(ControlStyles.AllPaintingInWmPaint, true);
            this.SetStyle(ControlStyles.UserPaint, true);
            this.SetStyle(ControlStyles.DoubleBuffer, true);
            
            // 마우스 이벤트를 위한 설정
            this.KeyPreview = true;
        }

        private void SetupOverlayProperties()
        {
            try
            {
                // 창을 항상 최상위로 유지하고 마우스 클릭을 통과시킴
                var exStyle = GetWindowLong(this.Handle, GWL_EXSTYLE);
                exStyle |= WS_EX_LAYERED | WS_EX_TOPMOST;
                SetWindowLong(this.Handle, GWL_EXSTYLE, exStyle);

                // 85% 불투명도 설정 (255 * 0.85 = 217)
                SetLayeredWindowAttributes(this.Handle, 0, 217, LWA_ALPHA);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GameOverlayForm] 오버레이 속성 설정 실패: {ex.Message}");
            }
        }

        private void CreateOverlayComponents()
        {
            // Zone A: 좌상단 - 일꾼 상태 (20, 20)
            _workerStatusPanel = CreateWorkerStatusPanel();
            _workerStatusPanel.Location = new Point(20, 20);
            this.Controls.Add(_workerStatusPanel);
            _draggablePanels.Add(_workerStatusPanel);

            // Zone A: 좌상단 - 빌드오더 가이드 (20, 72)
            _buildOrderPanel = CreateBuildOrderPanel();
            _buildOrderPanel.Location = new Point(20, 72);
            this.Controls.Add(_buildOrderPanel);
            _draggablePanels.Add(_buildOrderPanel);

            // Zone B: 우상단 - 업그레이드 진행 (1720, 20) - 우측 정렬
            _upgradeProgressPanel = CreateUpgradeProgressPanel();
            _upgradeProgressPanel.Location = new Point(1720, 20);
            this.Controls.Add(_upgradeProgressPanel);
            _draggablePanels.Add(_upgradeProgressPanel);

            // Zone B: 우상단 - 유닛 카운트 (1740, 82) - 우측 정렬
            _unitCountPanel = CreateUnitCountPanel();
            _unitCountPanel.Location = new Point(1740, 82);
            this.Controls.Add(_unitCountPanel);
            _draggablePanels.Add(_unitCountPanel);

            // 인구 경고 팝업 (중앙 하단, 필요시에만 표시)
            _populationWarningPanel = CreatePopulationWarningPanel();
            _populationWarningPanel.Location = new Point(860, 900);
            _populationWarningPanel.Visible = false;
            this.Controls.Add(_populationWarningPanel);
            _draggablePanels.Add(_populationWarningPanel);

            // 각 패널에 마우스 이벤트 추가
            SetupPanelDragEvents();
        }

        private Panel CreateWorkerStatusPanel()
        {
            var panel = new Panel
            {
                Size = new Size(120, 40),
                BackColor = OVERLAY_BACKGROUND,
                Padding = new Padding(8, 8, 12, 8)
            };

            var iconLabel = new Label
            {
                Text = "👷",
                Font = new Font("Segoe UI Emoji", 16),
                ForeColor = WARNING_COLOR,
                Size = new Size(27, 27),
                Location = new Point(8, 6),
                TextAlign = ContentAlignment.MiddleCenter
            };

            var countLabel = new Label
            {
                Text = "24",
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                ForeColor = TEXT_PRIMARY,
                Size = new Size(40, 24),
                Location = new Point(40, 8),
                TextAlign = ContentAlignment.MiddleLeft
            };

            var idleLabel = new Label
            {
                Text = "(2)",
                Font = new Font("Segoe UI", 14),
                ForeColor = BRAND_SECONDARY,
                Size = new Size(32, 24),
                Location = new Point(80, 8),
                TextAlign = ContentAlignment.MiddleLeft
            };

            panel.Controls.AddRange(new Control[] { iconLabel, countLabel, idleLabel });
            return panel;
        }

        private Panel CreateBuildOrderPanel()
        {
            var panel = new Panel
            {
                Size = new Size(200, 60),
                BackColor = OVERLAY_BACKGROUND,
                Padding = new Padding(12, 12, 16, 12)
            };

            var currentLabel = new Label
            {
                Text = "현재: 🏢(2) 🔬",
                Font = new Font(FontFamily.GenericSansSerif, 12, FontStyle.Bold),
                ForeColor = TEXT_PRIMARY,
                Size = new Size(176, 20),
                Location = new Point(12, 8),
                TextAlign = ContentAlignment.MiddleLeft
            };

            var nextLabel = new Label
            {
                Text = "다음: 🛡️ 🏢(2)",
                Font = new Font("Segoe UI", 10),
                ForeColor = Color.FromArgb(199, 199, 199),
                Size = new Size(176, 16),
                Location = new Point(12, 32),
                TextAlign = ContentAlignment.MiddleLeft
            };

            panel.Controls.AddRange(new Control[] { currentLabel, nextLabel });
            return panel;
        }

        private Panel CreateUpgradeProgressPanel()
        {
            var panel = new Panel
            {
                Size = new Size(180, 50),
                BackColor = OVERLAY_BACKGROUND,
                Padding = new Padding(8, 8, 12, 8)
            };

            var iconLabel = new Label
            {
                Text = "⚔️",
                Font = new Font("Segoe UI Emoji", 16),
                ForeColor = TEXT_PRIMARY,
                Size = new Size(20, 20),
                Location = new Point(8, 8),
                TextAlign = ContentAlignment.MiddleCenter
            };

            var timeLabel = new Label
            {
                Text = "2:30",
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                ForeColor = TEXT_PRIMARY,
                Size = new Size(50, 20),
                Location = new Point(32, 8),
                TextAlign = ContentAlignment.MiddleLeft
            };

            var progressBar = new ProgressBar
            {
                Size = new Size(60, 4),
                Location = new Point(32, 28),
                Style = ProgressBarStyle.Continuous,
                Maximum = 100,
                Value = 75,
                ForeColor = BRAND_PRIMARY
            };

            var percentLabel = new Label
            {
                Text = "75%",
                Font = new Font("Segoe UI", 10),
                ForeColor = TEXT_SECONDARY,
                Size = new Size(30, 16),
                Location = new Point(100, 28),
                TextAlign = ContentAlignment.MiddleLeft
            };

            panel.Controls.AddRange(new Control[] { iconLabel, timeLabel, progressBar, percentLabel });
            return panel;
        }

        private Panel CreateUnitCountPanel()
        {
            var panel = new Panel
            {
                Size = new Size(160, 100),
                BackColor = OVERLAY_BACKGROUND,
                Padding = new Padding(8, 8, 12, 8)
            };

            var categoryLabel1 = new Label
            {
                Text = "지상",
                Font = new Font("Segoe UI", 10),
                ForeColor = Color.FromArgb(199, 199, 199),
                Size = new Size(144, 16),
                Location = new Point(8, 4),
                TextAlign = ContentAlignment.MiddleLeft
            };

            var unitsLabel1 = new Label
            {
                Text = "⚔️5 🛡️3",
                Font = new Font(FontFamily.GenericSansSerif, 12, FontStyle.Bold),
                ForeColor = TEXT_PRIMARY,
                Size = new Size(144, 20),
                Location = new Point(8, 20),
                TextAlign = ContentAlignment.MiddleLeft
            };

            var categoryLabel2 = new Label
            {
                Text = "공중",
                Font = new Font("Segoe UI", 10),
                ForeColor = Color.FromArgb(199, 199, 199),
                Size = new Size(144, 16),
                Location = new Point(8, 50),
                TextAlign = ContentAlignment.MiddleLeft
            };

            var unitsLabel2 = new Label
            {
                Text = "✈️2",
                Font = new Font(FontFamily.GenericSansSerif, 12, FontStyle.Bold),
                ForeColor = TEXT_PRIMARY,
                Size = new Size(144, 20),
                Location = new Point(8, 66),
                TextAlign = ContentAlignment.MiddleLeft
            };

            panel.Controls.AddRange(new Control[] { categoryLabel1, unitsLabel1, categoryLabel2, unitsLabel2 });
            return panel;
        }

        private Panel CreatePopulationWarningPanel()
        {
            var panel = new Panel
            {
                Size = new Size(200, 60),
                BackColor = ERROR_COLOR,
                Padding = new Padding(12, 12, 16, 12)
            };

            var iconLabel = new Label
            {
                Text = "⚠️",
                Font = new Font("Segoe UI Emoji", 16),
                ForeColor = TEXT_PRIMARY,
                Size = new Size(20, 20),
                Location = new Point(12, 12),
                TextAlign = ContentAlignment.MiddleCenter
            };

            var messageLabel = new Label
            {
                Text = "인구수 한계 도달!",
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                ForeColor = TEXT_PRIMARY,
                Size = new Size(150, 20),
                Location = new Point(36, 16),
                TextAlign = ContentAlignment.MiddleLeft
            };

            panel.Controls.AddRange(new Control[] { iconLabel, messageLabel });
            return panel;
        }

        private void SetupTimers()
        {
            // 데이터 업데이트 타이머 (실시간 데이터 반영용)
            _dataUpdateTimer = new Timer
            {
                Interval = 100 // 100ms마다 업데이트
            };
            _dataUpdateTimer.Tick += DataUpdateTimer_Tick;

            // 시뮬레이션 타이머 (데모용 데이터 변경)
            _simulationTimer = new Timer
            {
                Interval = 3000 // 3초마다 시뮬레이션 데이터 변경
            };
            _simulationTimer.Tick += SimulationTimer_Tick;
            _simulationTimer.Start();
        }

        private void PositionForm()
        {
            // 전체 화면을 덮도록 설정
            this.WindowState = FormWindowState.Maximized;
            this.Bounds = Screen.PrimaryScreen.Bounds;
        }

        #endregion

        #region Event Handlers

        private void SetupPanelDragEvents()
        {
            foreach (var panel in _draggablePanels)
            {
                panel.MouseDown += Panel_MouseDown;
                panel.MouseMove += Panel_MouseMove;
                panel.MouseUp += Panel_MouseUp;
                panel.MouseEnter += Panel_MouseEnter;
                panel.MouseLeave += Panel_MouseLeave;
            }
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            this.Focus(); // 키보드 이벤트를 받기 위해 포커스 설정
            Console.WriteLine("[GameOverlayForm] 게임 오버레이 폼 로드됨");
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Tab && e.Shift)
            {
                e.Handled = true;
                ToggleDragMode();
            }
            else if (_isDragMode && e.KeyCode == Keys.R)
            {
                ResetPositions();
                e.Handled = true;
            }
            else if (_isDragMode && e.KeyCode == Keys.Escape)
            {
                ToggleDragMode();
                e.Handled = true;
            }
            
            base.OnKeyDown(e);
        }

        private void ToggleDragMode()
        {
            _isDragMode = !_isDragMode;
            
            if (_isDragMode)
            {
                Console.WriteLine("[GameOverlayForm] 드래그 모드 활성화");
            }
            else
            {
                Console.WriteLine("[GameOverlayForm] 드래그 모드 비활성화");
                
                // 드래그 모드 종료 시 모든 패널을 원래 상태로 복원
                ResetPanelStates();
            }
            
            this.Invalidate(); // 화면 다시 그리기
        }

        private void ResetPanelStates()
        {
            foreach (var panel in _draggablePanels)
            {
                if (panel != null)
                {
                    panel.BackColor = OVERLAY_BACKGROUND;
                    panel.Cursor = Cursors.Default;
                }
            }
            
            _isDragging = false;
            _draggingPanel = null;
        }

        private string GetPanelName(Panel panel)
        {
            if (panel == _workerStatusPanel) return "일꾼 상태";
            if (panel == _buildOrderPanel) return "빌드오더";
            if (panel == _upgradeProgressPanel) return "업그레이드 진행";
            if (panel == _unitCountPanel) return "유닛 카운트";
            if (panel == _populationWarningPanel) return "인구 경고";
            return "알 수 없음";
        }


        private void Panel_MouseDown(object sender, MouseEventArgs e)
        {
            if (!_isDragMode) return;
            
            var panel = sender as Panel;
            if (panel == null) return;
            
            _draggingPanel = panel;
            _isDragging = true;
            _dragOffset = e.Location;
            _hoveredPanel = null; // 드래그 시작 시 hover 상태 해제
            
            // 패널을 다른 패널들보다 위로 올리기
            panel.BringToFront();
            panel.Capture = true;
            
            // 드래그 시작 시 패널에 시각적 효과 추가
            panel.BackColor = Color.FromArgb(230, panel.BackColor.R, panel.BackColor.G, panel.BackColor.B);
            
            InvalidatePanel(panel);
            Console.WriteLine($"[GameOverlayForm] {GetPanelName(panel)} 드래그 시작");
        }

        private void Panel_MouseMove(object sender, MouseEventArgs e)
        {
            if (!_isDragMode || !_isDragging) return;
            
            var panel = sender as Panel;
            if (panel == null || panel != _draggingPanel) return;
            
            Point newLocation = new Point(
                panel.Location.X + e.X - _dragOffset.X,
                panel.Location.Y + e.Y - _dragOffset.Y
            );
            
            // 화면 경계 체크
            newLocation.X = Math.Max(0, Math.Min(this.Width - panel.Width, newLocation.X));
            newLocation.Y = Math.Max(0, Math.Min(this.Height - panel.Height, newLocation.Y));
            
            panel.Location = newLocation;
            // 전체 폼 대신 드래그 중인 패널 영역만 업데이트
            InvalidatePanel(panel);
        }

        private void Panel_MouseUp(object sender, MouseEventArgs e)
        {
            if (!_isDragMode || !_isDragging) return;
            
            var panel = sender as Panel;
            if (panel == null || panel != _draggingPanel) return;
            
            _isDragging = false;
            panel.Capture = false;
            _hoveredPanel = null; // 드래그 완료 시 hover 상태 해제
            
            Console.WriteLine($"[GameOverlayForm] {GetPanelName(panel)} 위치 변경 완료: {panel.Location}");
            
            // 위치 변경 후 저장
            SavePositions();
            
            _draggingPanel = null;
            // 드래그 완료 후 UI 업데이트
            InvalidatePanel(panel);
        }

        private void Panel_MouseEnter(object sender, EventArgs e)
        {
            if (!_isDragMode) return;
            
            var panel = sender as Panel;
            if (panel == null) return;
            
            // 드래그 모드에서 마우스가 패널 위에 있을 때 커서 변경만 (리렌더링 없음)
            panel.Cursor = Cursors.SizeAll;
            
            // hover 시각적 효과 제거 - 커서 변경만으로 충분한 피드백 제공
            // InvalidatePanel 호출 제거로 깜빡임 완전 방지
        }

        private void Panel_MouseLeave(object sender, EventArgs e)
        {
            if (!_isDragMode) return;
            
            var panel = sender as Panel;
            if (panel == null) return;
            
            // 마우스가 패널을 벗어날 때 원래 상태로 복원
            panel.Cursor = Cursors.Default;
            
            if (!_isDragging && _hoveredPanel == panel)
            {
                _hoveredPanel = null;
                // 테두리 제거를 위해 패널 주변 영역만 업데이트
                InvalidatePanel(panel);
            }
        }

        private void DataUpdateTimer_Tick(object sender, EventArgs e)
        {
            // 실제 게임 데이터 업데이트 로직
            UpdateWorkerStatus();
            UpdateUpgradeProgress();
            // TODO: 실제 게임 데이터와 연동
        }

        private void SimulationTimer_Tick(object sender, EventArgs e)
        {
            // 시뮬레이션용 데이터 변경
            _totalWorkers = 24 + _random.Next(0, 10);
            _idleWorkers = _random.Next(0, 5);
            
            // 데스 플래시 시뮬레이션
            _isDeathFlash = _random.Next(0, 10) < 2;

            // 인구 경고 시뮬레이션
            if (_random.Next(0, 10) < 3)
            {
                ShowPopulationWarning();
            }

            UpdateWorkerStatus();
        }

        private void UpdateWorkerStatus()
        {
            if (_workerStatusPanel?.Controls.Count >= 3)
            {
                var countLabel = _workerStatusPanel.Controls[1] as Label;
                var idleLabel = _workerStatusPanel.Controls[2] as Label;
                var iconLabel = _workerStatusPanel.Controls[0] as Label;

                if (countLabel != null)
                    countLabel.Text = _totalWorkers.ToString();

                if (idleLabel != null)
                {
                    idleLabel.Text = _idleWorkers > 0 ? $"({_idleWorkers})" : "";
                    idleLabel.ForeColor = _idleWorkers > 0 ? BRAND_SECONDARY : Color.Transparent;
                }

                if (iconLabel != null)
                {
                    iconLabel.ForeColor = _isDeathFlash ? ERROR_COLOR : WARNING_COLOR;
                }
            }
        }

        private void UpdateUpgradeProgress()
        {
            if (_upgradeProgressPanel?.Controls.Count >= 4)
            {
                var progressBar = _upgradeProgressPanel.Controls[2] as ProgressBar;
                var percentLabel = _upgradeProgressPanel.Controls[3] as Label;

                if (progressBar != null && percentLabel != null)
                {
                    var progress = _random.Next(0, 101);
                    progressBar.Value = progress;
                    percentLabel.Text = $"{progress}%";
                }
            }
        }

        private void ShowPopulationWarning()
        {
            if (_populationWarningPanel != null)
            {
                _populationWarningPanel.Visible = true;

                // 3초 후 자동으로 숨김
                var hideTimer = new Timer { Interval = 3000 };
                hideTimer.Tick += (s, e) =>
                {
                    _populationWarningPanel.Visible = false;
                    hideTimer.Stop();
                    hideTimer.Dispose();
                };
                hideTimer.Start();
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

        protected override void OnPaint(PaintEventArgs e)
        {
            try
            {
                base.OnPaint(e);
                
                // 각 오버레이 컴포넌트에 둥근 모서리와 테두리 그리기
                DrawOverlayPanelBorders(e.Graphics);
                
                // 드래그 모드일 때 추가 UI 그리기
                if (_isDragMode)
                {
                    DrawDragModeUI(e.Graphics);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GameOverlayForm] OnPaint 오류: {ex.Message}");
            }
        }

        private void DrawDragModeUI(Graphics g)
        {
            // 고품질 렌더링 설정 (잔상 방지)
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
            g.CompositingQuality = CompositingQuality.HighQuality;
            
            // 드래그 모드 알림 표시 (상단 중앙) - 크기 최적화
            var dragModeText = "🎯 드래그 모드 | 마우스로 이동 | R: 리셋 | ESC: 종료";
            var font = new Font("맑은 고딕", 12, FontStyle.Bold);
            var textSize = g.MeasureString(dragModeText, font);
            var textRect = new RectangleF(
                (this.Width - textSize.Width) / 2,
                15,
                textSize.Width + 16,
                textSize.Height + 8
            );
            
            // 배경 그리기 (단순화)
            using (var brush = new SolidBrush(Color.FromArgb(220, 0, 153, 255)))
            {
                g.FillRectangle(brush, Rectangle.Round(textRect));
            }
            
            // 텍스트 그리기 (안티앨리어싱 적용)
            using (var textBrush = new SolidBrush(Color.White))
            {
                g.DrawString(dragModeText, font, textBrush, 
                    textRect.X + 8, textRect.Y + 4);
            }
            
            // 드래그 중인 패널 하이라이트만 (단순화)
            if (_draggingPanel != null && _draggingPanel.Visible && _isDragging)
            {
                var panelRect = new Rectangle(
                    _draggingPanel.Location.X - 2,
                    _draggingPanel.Location.Y - 2,
                    _draggingPanel.Width + 4,
                    _draggingPanel.Height + 4
                );
                
                // 단순한 사각형 하이라이트 (성능 최적화)
                using (var highlightPen = new Pen(SUCCESS_COLOR, 2))
                {
                    g.DrawRectangle(highlightPen, panelRect);
                }
            }
        }

        private void DrawOverlayPanelBorders(Graphics g)
        {
            var panels = new[] { _workerStatusPanel, _buildOrderPanel, _upgradeProgressPanel, _unitCountPanel };
            
            foreach (var panel in panels)
            {
                if (panel != null && panel.Visible)
                {
                    // 기본 테두리
                    DrawRoundedPanel(g, panel, 6, OVERLAY_BORDER, 1);
                    
                    // 편집 모드일 때 모든 패널에 두꺼운 파란색 테두리
                    if (_isDragMode)
                    {
                        DrawEditModeHighlight(g, panel);
                    }
                }
            }

            // 인구 경고 팝업은 다른 스타일
            if (_populationWarningPanel != null && _populationWarningPanel.Visible)
            {
                DrawRoundedPanel(g, _populationWarningPanel, 8, Color.FromArgb(100, 255, 255, 255), 1);
                
                // 편집 모드일 때 인구 경고에도 하이라이트
                if (_isDragMode)
                {
                    DrawEditModeHighlight(g, _populationWarningPanel);
                }
            }
        }

        private void DrawRoundedPanel(Graphics g, Panel panel, int radius, Color borderColor, float thickness = 1.0f)
        {
            var rect = new Rectangle(panel.Location.X - 1, panel.Location.Y - 1, panel.Width + 1, panel.Height + 1);
            
            using (var path = GetRoundedRectPath(rect, radius))
            using (var pen = new Pen(borderColor, thickness))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.DrawPath(pen, path);
            }
        }
        
        /// <summary>
        /// 편집 모드에서 패널에 두꺼운 파란색 하이라이트 테두리 그리기
        /// </summary>
        private void DrawEditModeHighlight(Graphics g, Panel panel)
        {
            // 외곽에 두꺼운 파란색 테두리 (4px)
            var highlightRect = new Rectangle(
                panel.Location.X - 3, 
                panel.Location.Y - 3, 
                panel.Width + 5, 
                panel.Height + 5
            );
            
            using (var path = GetRoundedRectPath(highlightRect, 8))
            using (var pen = new Pen(BRAND_PRIMARY, 4.0f)) // 4px 두께의 파란색 테두리
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.DrawPath(pen, path);
            }
            
            // 드래그 중인 패널은 추가로 녹색 하이라이트
            if (_isDragging && _draggingPanel == panel)
            {
                using (var path = GetRoundedRectPath(highlightRect, 8))
                using (var pen = new Pen(SUCCESS_COLOR, 3.0f))
                {
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                    g.DrawPath(pen, path);
                }
            }
        }

        private GraphicsPath GetRoundedRectPath(Rectangle rect, int radius)
        {
            var path = new GraphicsPath();
            
            path.AddArc(rect.X, rect.Y, radius * 2, radius * 2, 180, 90);
            path.AddArc(rect.Right - radius * 2, rect.Y, radius * 2, radius * 2, 270, 90);
            path.AddArc(rect.Right - radius * 2, rect.Bottom - radius * 2, radius * 2, radius * 2, 0, 90);
            path.AddArc(rect.X, rect.Bottom - radius * 2, radius * 2, radius * 2, 90, 90);
            path.CloseFigure();
            
            return path;
        }

        /// <summary>
        /// 특정 패널과 그 주변 영역만 다시 그리기 (성능 최적화)
        /// </summary>
        private void InvalidatePanel(Panel panel)
        {
            if (panel == null) return;
            
            // 패널 영역 + 주변 여백 (드래그 하이라이트 고려)
            var invalidateRect = new Rectangle(
                panel.Location.X - 10,
                panel.Location.Y - 30, // 패널 이름 표시 영역 포함
                panel.Width + 20,
                panel.Height + 40
            );
            
            // 화면 경계 내로 제한
            invalidateRect.X = Math.Max(0, invalidateRect.X);
            invalidateRect.Y = Math.Max(0, invalidateRect.Y);
            invalidateRect.Width = Math.Min(this.Width - invalidateRect.X, invalidateRect.Width);
            invalidateRect.Height = Math.Min(this.Height - invalidateRect.Y, invalidateRect.Height);
            
            this.Invalidate(invalidateRect);
        }

        #endregion

        #region Position Save/Load

        private void SavePositions()
        {
            try
            {
                var positions = new Dictionary<string, Point>
                {
                    ["workerStatus"] = _workerStatusPanel.Location,
                    ["buildOrder"] = _buildOrderPanel.Location,
                    ["upgradeProgress"] = _upgradeProgressPanel.Location,
                    ["unitCount"] = _unitCountPanel.Location,
                    ["populationWarning"] = _populationWarningPanel.Location
                };

                // 디렉터리가 없으면 생성
                var directory = Path.GetDirectoryName(_positionsFilePath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var json = JsonSerializer.Serialize(positions, new JsonSerializerOptions 
                { 
                    WriteIndented = true 
                });
                
                File.WriteAllText(_positionsFilePath, json);
                Console.WriteLine("[GameOverlayForm] 오버레이 위치 저장됨");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GameOverlayForm] 위치 저장 실패: {ex.Message}");
            }
        }

        private void LoadPositions()
        {
            try
            {
                if (!File.Exists(_positionsFilePath))
                {
                    Console.WriteLine("[GameOverlayForm] 저장된 위치 파일이 없습니다. 기본 위치 사용");
                    return;
                }

                var json = File.ReadAllText(_positionsFilePath);
                var positions = JsonSerializer.Deserialize<Dictionary<string, Point>>(json);

                if (positions != null)
                {
                    if (positions.ContainsKey("workerStatus"))
                        _workerStatusPanel.Location = positions["workerStatus"];
                    
                    if (positions.ContainsKey("buildOrder"))
                        _buildOrderPanel.Location = positions["buildOrder"];
                    
                    if (positions.ContainsKey("upgradeProgress"))
                        _upgradeProgressPanel.Location = positions["upgradeProgress"];
                    
                    if (positions.ContainsKey("unitCount"))
                        _unitCountPanel.Location = positions["unitCount"];
                    
                    if (positions.ContainsKey("populationWarning"))
                        _populationWarningPanel.Location = positions["populationWarning"];

                    Console.WriteLine("[GameOverlayForm] 저장된 오버레이 위치 복원됨");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GameOverlayForm] 위치 복원 실패: {ex.Message}");
            }
        }

        private void ResetPositions()
        {
            // 기본 위치로 리셋
            _workerStatusPanel.Location = new Point(20, 20);
            _buildOrderPanel.Location = new Point(20, 72);
            _upgradeProgressPanel.Location = new Point(1720, 20);
            _unitCountPanel.Location = new Point(1740, 82);
            _populationWarningPanel.Location = new Point(860, 900);
            
            SavePositions();
            this.Invalidate();
            
            Console.WriteLine("[GameOverlayForm] 오버레이 위치가 기본값으로 리셋됨");
        }

        #endregion

        #region Public Methods

        public void StartOverlay()
        {
            _dataUpdateTimer?.Start();
            this.Show();
            Console.WriteLine("[GameOverlayForm] 오버레이 시작됨");
        }

        public void StopOverlay()
        {
            _dataUpdateTimer?.Stop();
            this.Hide();
            Console.WriteLine("[GameOverlayForm] 오버레이 중지됨");
        }

        #endregion

        #region Dispose

        protected override void Dispose(bool disposing)
        {
            if (!_isDisposed && disposing)
            {
                Console.WriteLine("[GameOverlayForm] 게임 오버레이 폼 해제 시작");

                try
                {
                    _dataUpdateTimer?.Stop();
                    _dataUpdateTimer?.Dispose();

                    _simulationTimer?.Stop();
                    _simulationTimer?.Dispose();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[GameOverlayForm] 리소스 해제 중 오류: {ex.Message}");
                }

                _isDisposed = true;
                Console.WriteLine("[GameOverlayForm] 게임 오버레이 폼 해제 완료");
            }

            base.Dispose(disposing);
        }

        #endregion
    }
}