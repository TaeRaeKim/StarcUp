using System;
using System.Drawing;
using System.Windows.Forms;

namespace StarcUp
{
    public partial class OverlayForm : Form
    {
        private GameDetector gameDetector;
        private OverlayUI overlayUI;
        private bool isStarcraftActive = false;

        public event Action<IntPtr> GameFound;
        public event Action GameLost;
        public OverlayForm()
        {
            InitializeComponent();
            SetupComponents();
            StartGameDetection();

            // 확실히 숨기기
            this.Hide();
            Console.WriteLine($"오버레이 초기 상태: Visible={this.Visible}");
        }

        private void InitializeComponent()
        {
            this.FormBorderStyle = FormBorderStyle.None;
            this.TopMost = true;
            this.ShowInTaskbar = false;
            this.StartPosition = FormStartPosition.Manual;
            this.AutoSize = true;
            this.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            this.Padding = new Padding(10);

            // 투명도 설정 (검은색을 투명하게)
            this.BackColor = Color.Black;
            this.TransparencyKey = Color.Black;

            // 마우스 클릭이 뒤로 전달되도록 설정
            this.SetStyle(ControlStyles.SupportsTransparentBackColor, true);
        }

        private void SetupComponents()
        {
            // UI 컴포넌트 생성
            overlayUI = new OverlayUI();
            overlayUI.CreateUI();
            this.Controls.Add(overlayUI.Container);

            // 게임 감지기 생성
            gameDetector = new GameDetector();
            gameDetector.GameFound += OnGameFound;
            gameDetector.GameLost += OnGameLost;
            gameDetector.WindowPositionChanged += OnWindowPositionChanged;
            gameDetector.WindowActivated += OnWindowActivated;
            gameDetector.WindowDeactivated += OnWindowDeactivated;
        }

        private void StartGameDetection()
        {
            gameDetector.StartDetection();
        }

        private void OnGameFound(IntPtr gameWindow)
        {
            Console.WriteLine("OnGameFound 호출됨 - 오버레이 표시");
            // 현재 활성 윈도우가 스타크래프트인지 확인
            IntPtr foregroundWindow = WindowsAPI.GetForegroundWindow();
            isStarcraftActive = (foregroundWindow == gameWindow);

            if (isStarcraftActive)
            {
                UpdateOverlayPosition();
                if (!this.Visible)
                {
                    this.Show();
                }
            }

            GameFound?.Invoke(gameWindow);
        }

        private void OnGameLost()
        {
            Console.WriteLine("OnGameLost 호출됨 - 오버레이 숨김");
            isStarcraftActive = false;
            if (this.Visible)
            {
                this.Hide();
            }

            GameLost?.Invoke();
        }

        private void OnWindowActivated(IntPtr gameWindow)
        {
            Console.WriteLine("스타크래프트가 활성화됨 - 오버레이 표시");
            isStarcraftActive = true;
            UpdateOverlayPosition();
            if (!this.Visible)
            {
                this.Show();
            }
        }

        private void OnWindowDeactivated(IntPtr otherWindow)
        {
            Console.WriteLine("다른 윈도우가 활성화됨 - 오버레이 숨김");
            isStarcraftActive = false;
            if (this.Visible)
            {
                this.Hide();
            }
        }

        private void OnWindowPositionChanged(IntPtr gameWindow)
        {
            // 스타크래프트가 활성화되어 있고 오버레이가 보이는 상태일 때만 위치 업데이트
            if (isStarcraftActive && this.Visible)
            {
                UpdateOverlayPosition();
            }
        }

        private int GetTitleBarHeight(IntPtr hWnd)
        {
            WindowsAPI.RECT windowRect, clientRect;
            if (WindowsAPI.GetWindowRect(hWnd, out windowRect) && WindowsAPI.GetClientRect(hWnd, out clientRect))
            {
                // 윈도우 전체 높이에서 클라이언트 영역 높이를 뺀 값이 타이틀바 + 테두리
                int totalBorderHeight = (windowRect.Bottom - windowRect.Top) - (clientRect.Bottom - clientRect.Top);

                // 대략적으로 타이틀바는 전체 테두리의 80% 정도
                // 또는 최소 20픽셀은 보장
                return Math.Max(20, (int)(totalBorderHeight * 0.8));
            }
            return 30; // 기본값
        }

        private void UpdateOverlayPosition()
        {
            if (gameDetector.StarcraftWindow != IntPtr.Zero)
            {
                // 최소화 상태인지 확인
                if (WindowsAPI.IsIconic(gameDetector.StarcraftWindow))
                {
                    this.Hide();
                    return;
                }

                WindowsAPI.RECT rect;
                if (WindowsAPI.GetWindowRect(gameDetector.StarcraftWindow, out rect))
                {
                    int leftOffset, topOffset;

                    // 방법 1: Windows API로 최대화 상태 확인
                    bool isZoomed = WindowsAPI.IsZoomed(gameDetector.StarcraftWindow);

                    // 방법 2: 화면 크기와 윈도우 크기 비교로 전체화면 감지
                    int screenWidth = Screen.PrimaryScreen.Bounds.Width;
                    int screenHeight = Screen.PrimaryScreen.Bounds.Height;
                    int windowWidth = rect.Right - rect.Left;
                    int windowHeight = rect.Bottom - rect.Top;

                    bool isFullscreenBySize = (windowWidth >= screenWidth && windowHeight >= screenHeight)
                                             || (rect.Left <= 0 && rect.Top <= 0 && windowWidth >= screenWidth - 20);

                    // 둘 중 하나라도 true면 전체화면으로 판단
                    bool isFullscreen = isZoomed || isFullscreenBySize;

                    Console.WriteLine($"IsZoomed: {isZoomed}, SizeCheck: {isFullscreenBySize}, 최종 전체화면: {isFullscreen}");
                    if (isFullscreen)
                    {
                        // 전체화면인 경우
                        leftOffset = 15;
                        topOffset = 10;
                    }
                    else
                    {
                        // 창모드인 경우 - 타이틀바 높이 + 10
                        int titleBarHeight = GetTitleBarHeight(gameDetector.StarcraftWindow);
                        leftOffset = 20;
                        topOffset = titleBarHeight + 10;

                        Console.WriteLine($"타이틀바 높이: {titleBarHeight}, 최종 topOffset: {topOffset}");
                    }

                    this.Location = new Point(rect.Left + leftOffset, rect.Top + topOffset);

                    // 오버레이가 숨겨져 있고 스타크래프트가 활성화되어 있다면 다시 표시
                    if (!this.Visible && isStarcraftActive)
                    {
                        this.Show();
                    }
                }
            }
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                // 클릭이 뒤로 전달되도록 설정
                cp.ExStyle |= 0x80000 /* WS_EX_LAYERED */ | 0x20 /* WS_EX_TRANSPARENT */;
                return cp;
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            gameDetector?.StopDetection();
            base.OnFormClosing(e);
        }
    }
}