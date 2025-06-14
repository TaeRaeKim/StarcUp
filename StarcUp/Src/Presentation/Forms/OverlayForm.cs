using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using StarcUp.Business.Models;
using StarcUp.Infrastructure.Windows;
using StarcUp.Presentation.Controls;

namespace StarcUp.Presentation.Forms
{
    /// <summary>
    /// 새롭게 작성된 메인 오버레이 폼 - 단순하고 안정적인 구조
    /// </summary>
    public partial class OverlayForm : Form
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
        private const int WS_EX_TRANSPARENT = 0x20;
        private const int WS_EX_TOPMOST = 0x8;
        private const uint LWA_COLORKEY = 0x1;
        private const uint LWA_ALPHA = 0x2;

        #endregion

        #region Private Fields

        private OverlayControl _overlayControl;
        private bool _isInitialized;
        private bool _isDisposed;
        private Timer _refreshTimer;

        #endregion

        #region Constructor

        public OverlayForm()
        {
            Console.WriteLine("[MainForm] 새 메인 폼 생성 시작");

            try
            {
                InitializeComponent();
                SetupOverlay();
                CreateOverlayControl();
                SetupRefreshTimer();

                _isInitialized = true;
                Console.WriteLine("[MainForm] 메인 폼 초기화 완료");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MainForm] 메인 폼 생성 실패: {ex.Message}");
                throw;
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// 포인터 값 업데이트
        /// </summary>
        public void UpdatePointerValue(PointerValue pointerValue)
        {
            if (!_isInitialized || _isDisposed)
                return;

            try
            {
                if (this.InvokeRequired)
                {
                    this.BeginInvoke(new Action<PointerValue>(UpdatePointerValue), pointerValue);
                    return;
                }

                _overlayControl?.UpdatePointerValue(pointerValue);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MainForm] 포인터 값 업데이트 실패: {ex.Message}");
            }
        }

        /// <summary>
        /// 상태 업데이트
        /// </summary>
        public void UpdateStatus(string status)
        {
            if (!_isInitialized || _isDisposed)
                return;

            try
            {
                if (this.InvokeRequired)
                {
                    this.BeginInvoke(new Action<string>(UpdateStatus), status);
                    return;
                }

                _overlayControl?.UpdateStatus(status);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MainForm] 상태 업데이트 실패: {ex.Message}");
            }
        }

        /// <summary>
        /// 안전한 Show 메서드
        /// </summary>
        public new void Show()
        {
            if (!_isInitialized || _isDisposed)
                return;

            try
            {
                Console.WriteLine("[MainForm] 오버레이 표시 시작");

                // 기본 Show 호출
                base.Show();

                // 윈도우 속성 재설정 (가끔 리셋되는 경우가 있음)
                EnsureOverlayProperties();

                // 최상위로 가져오기
                this.BringToFront();
                this.TopMost = true;

                Console.WriteLine("[MainForm] 오버레이 표시 완료");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MainForm] 오버레이 표시 실패: {ex.Message}");
            }
        }

        /// <summary>
        /// 안전한 Hide 메서드
        /// </summary>
        public new void Hide()
        {
            if (_isDisposed)
                return;

            try
            {
                Console.WriteLine("[MainForm] 오버레이 숨김");
                base.Hide();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MainForm] 오버레이 숨김 실패: {ex.Message}");
            }
        }

        #endregion

        #region Private Methods

        private void InitializeComponent()
        {
            // 기본 폼 속성 설정
            this.Text = "StarcUp Overlay";
            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition = FormStartPosition.Manual;
            this.Size = new Size(220, 80);
            this.BackColor = Color.Magenta; // 투명 키 색상
            this.TransparencyKey = Color.Magenta;
            this.ShowInTaskbar = false;
            this.TopMost = true;

            // 컨트롤 스타일 설정
            this.SetStyle(ControlStyles.AllPaintingInWmPaint |
                         ControlStyles.UserPaint |
                         ControlStyles.DoubleBuffer, true);
        }

        private void SetupOverlay()
        {
            try
            {
                Console.WriteLine("[MainForm] 오버레이 속성 설정");

                // 레이어드 윈도우 설정
                var exStyle = GetWindowLong(this.Handle, GWL_EXSTYLE);
                exStyle |= WS_EX_LAYERED | WS_EX_TOPMOST;
                SetWindowLong(this.Handle, GWL_EXSTYLE, exStyle);

                // 투명도 설정 (95% 불투명)
                SetLayeredWindowAttributes(this.Handle, 0, 242, LWA_ALPHA);

                Console.WriteLine("[MainForm] 오버레이 속성 설정 완료");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MainForm] 오버레이 속성 설정 실패: {ex.Message}");
            }
        }

        private void CreateOverlayControl()
        {
            try
            {
                Console.WriteLine("[MainForm] 오버레이 컨트롤 생성");

                _overlayControl = new OverlayControl
                {
                    Dock = DockStyle.Fill
                };

                this.Controls.Add(_overlayControl);
                _overlayControl.BringToFront();

                // 초기 값 설정
                _overlayControl.UpdateStatus("StarcUp 대기중");
                _overlayControl.UpdatePointerValue(null);

                Console.WriteLine("[MainForm] 오버레이 컨트롤 생성 완료");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MainForm] 오버레이 컨트롤 생성 실패: {ex.Message}");
            }
        }

        private void SetupRefreshTimer()
        {
            try
            {
                // 주기적으로 오버레이 속성 확인 및 갱신
                _refreshTimer = new Timer
                {
                    Interval = 5000, // 5초마다
                    Enabled = false
                };
                _refreshTimer.Tick += RefreshTimer_Tick;

                Console.WriteLine("[MainForm] 새로고침 타이머 설정 완료");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MainForm] 새로고침 타이머 설정 실패: {ex.Message}");
            }
        }

        private void RefreshTimer_Tick(object sender, EventArgs e)
        {
            try
            {
                if (this.Visible && !_isDisposed)
                {
                    EnsureOverlayProperties();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MainForm] 새로고침 타이머 오류: {ex.Message}");
            }
        }

        private void EnsureOverlayProperties()
        {
            try
            {
                if (_isDisposed || !this.IsHandleCreated)
                    return;

                // TopMost 속성 유지
                if (!this.TopMost)
                {
                    this.TopMost = true;
                    Console.WriteLine("[MainForm] TopMost 속성 복원");
                }

                // 레이어드 윈도우 속성 확인
                var currentStyle = GetWindowLong(this.Handle, GWL_EXSTYLE);
                if ((currentStyle & WS_EX_LAYERED) == 0)
                {
                    SetupOverlay();
                    Console.WriteLine("[MainForm] 레이어드 윈도우 속성 복원");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MainForm] 오버레이 속성 확인 실패: {ex.Message}");
            }
        }

        #endregion

        #region Protected Methods

        protected override CreateParams CreateParams
        {
            get
            {
                var cp = base.CreateParams;

                // 클릭 투과 및 최상위 설정
                cp.ExStyle |= WS_EX_LAYERED | WS_EX_TRANSPARENT | WS_EX_TOPMOST;

                return cp;
            }
        }

        protected override void SetVisibleCore(bool value)
        {
            // 초기화 완료 전에는 표시하지 않음
            if (!_isInitialized && value)
            {
                value = false;
            }

            base.SetVisibleCore(value);

            // 표시될 때 새로고침 타이머 시작
            if (_refreshTimer != null)
            {
                _refreshTimer.Enabled = value;
            }
        }

        protected override void OnLoad(EventArgs e)
        {
            try
            {
                base.OnLoad(e);

                // 로드 완료 후 오버레이 속성 재설정
                if (this.Visible)
                {
                    EnsureOverlayProperties();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MainForm] OnLoad 오류: {ex.Message}");
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            // X 버튼으로 닫으려 할 때는 숨기기만 함
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                this.Hide();
                return;
            }

            base.OnFormClosing(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            try
            {
                // 배경을 투명 키 색상으로 채움
                e.Graphics.Clear(this.TransparencyKey);
                base.OnPaint(e);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MainForm] OnPaint 오류: {ex.Message}");
            }
        }

        #endregion

        #region Dispose

        protected override void Dispose(bool disposing)
        {
            if (!_isDisposed && disposing)
            {
                Console.WriteLine("[MainForm] 메인 폼 해제 시작");

                try
                {
                    _refreshTimer?.Stop();
                    _refreshTimer?.Dispose();
                    _overlayControl?.Dispose();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[MainForm] 리소스 해제 중 오류: {ex.Message}");
                }

                _isDisposed = true;
                Console.WriteLine("[MainForm] 메인 폼 해제 완료");
            }

            base.Dispose(disposing);
        }

        #endregion
    }
}