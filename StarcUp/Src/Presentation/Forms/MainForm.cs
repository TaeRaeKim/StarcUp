using System;
using System.Drawing;
using System.Windows.Forms;
using StarcUp.Business.Models;
using StarcUp.Presentation.Controls;

namespace StarcUp.Presentation.Forms
{
    /// <summary>
    /// 메인 오버레이 폼
    /// </summary>
    public partial class MainForm : Form
    {
        private OverlayControl _overlayControl;
        private bool _isDisposed;
        private bool _isFullyInitialized = false;

        public MainForm()
        {
            Console.WriteLine("[DEBUG] MainForm 생성자 시작");

            // 1단계: 기본 폼 설정
            InitializeComponent();

            // 2단계: 폼이 완전히 준비될 때까지 대기
            this.Load += MainForm_Load;
            this.Shown += MainForm_Shown;

            // 처음에는 숨김
            this.Hide();

            Console.WriteLine("[DEBUG] MainForm 생성자 완료");
        }

        private void InitializeComponent()
        {
            Console.WriteLine("[DEBUG] InitializeComponent 시작");

            this.FormBorderStyle = FormBorderStyle.None;
            this.TopMost = true;
            this.ShowInTaskbar = false;
            this.StartPosition = FormStartPosition.Manual;
            this.AutoSize = true;
            this.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            this.Padding = new Padding(10);

            // 투명도 설정 (나중에 설정)
            this.BackColor = Color.Black;
            this.TransparencyKey = Color.Black;

            this.SetStyle(ControlStyles.SupportsTransparentBackColor, true);

            Console.WriteLine("[DEBUG] InitializeComponent 완료");
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            Console.WriteLine("[DEBUG] MainForm_Load 시작");

            try
            {
                // 폼이 로드된 후 오버레이 컨트롤 설정
                if (_overlayControl == null)
                {
                    SetupOverlayControl();
                }

                Console.WriteLine("[DEBUG] MainForm_Load 완료");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] MainForm_Load 실패: {ex.Message}");
            }
        }

        private void MainForm_Shown(object sender, EventArgs e)
        {
            Console.WriteLine("[DEBUG] MainForm_Shown 시작");

            try
            {
                // 폼이 표시된 후 모든 것이 준비되었는지 확인
                EnsureControlsAreReady();

                // 강제 리프레시
                this.Refresh();
                _overlayControl?.Refresh();

                _isFullyInitialized = true;
                Console.WriteLine("[DEBUG] MainForm 완전 초기화 완료");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] MainForm_Shown 실패: {ex.Message}");
            }
        }

        private void SetupOverlayControl()
        {
            Console.WriteLine("[DEBUG] SetupOverlayControl 시작");

            try
            {
                _overlayControl = new OverlayControl();

                // 컨트롤이 완전히 생성될 때까지 대기
                Application.DoEvents();

                // 초기 상태 설정
                _overlayControl.UpdateStatus("StarcUp 대기중...");
                _overlayControl.UpdatePointerValue(null);

                // 폼에 추가
                this.Controls.Add(_overlayControl);
                _overlayControl.BringToFront();

                // 다시 한번 대기
                Application.DoEvents();

                Console.WriteLine("[DEBUG] SetupOverlayControl 완료");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] SetupOverlayControl 실패: {ex.Message}");
            }
        }

        private void EnsureControlsAreReady()
        {
            Console.WriteLine("[DEBUG] EnsureControlsAreReady 시작");

            if (_overlayControl != null)
            {
                // 컨트롤이 제대로 표시되는지 확인
                _overlayControl.Visible = true;
                _overlayControl.BringToFront();

                // 강제로 다시 그리기
                _overlayControl.Invalidate(true);
                _overlayControl.Update();

                Console.WriteLine("[DEBUG] 오버레이 컨트롤 준비 확인 완료");
            }
        }

        public void UpdatePointerValue(PointerValue pointerValue)
        {
            // 완전히 초기화되지 않았으면 무시
            if (!_isFullyInitialized)
            {
                Console.WriteLine("[WARNING] UpdatePointerValue 호출되었지만 아직 초기화 중");
                return;
            }

            if (_overlayControl != null && !_isDisposed)
            {
                try
                {
                    if (this.InvokeRequired)
                    {
                        this.BeginInvoke(new Action<PointerValue>(UpdatePointerValue), pointerValue);
                        return;
                    }

                    _overlayControl.UpdatePointerValue(pointerValue);

                    // 업데이트 후 강제 리프레시
                    _overlayControl.Refresh();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ERROR] 포인터 값 업데이트 실패: {ex.Message}");
                }
            }
        }

        public void UpdateStatus(string status)
        {
            // 완전히 초기화되지 않았으면 무시
            if (!_isFullyInitialized)
            {
                Console.WriteLine("[WARNING] UpdateStatus 호출되었지만 아직 초기화 중");
                return;
            }

            if (_overlayControl != null && !_isDisposed)
            {
                try
                {
                    if (this.InvokeRequired)
                    {
                        this.BeginInvoke(new Action<string>(UpdateStatus), status);
                        return;
                    }

                    _overlayControl.UpdateStatus(status);

                    // 업데이트 후 강제 리프레시
                    _overlayControl.Refresh();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ERROR] 상태 업데이트 실패: {ex.Message}");
                }
            }
        }

        // 새로운 메서드: 안전한 Show
        public new void Show()
        {
            Console.WriteLine("[DEBUG] MainForm.Show() 호출");

            try
            {
                // 기본 Show 호출
                base.Show();

                // 표시 후 잠깐 대기하여 렌더링 완료 보장
                Application.DoEvents();
                System.Threading.Thread.Sleep(50); // 50ms 대기

                // 컨트롤들이 제대로 표시되는지 다시 확인
                if (_overlayControl != null)
                {
                    _overlayControl.Visible = true;
                    _overlayControl.BringToFront();
                    _overlayControl.Refresh();
                }

                this.Refresh();
                Console.WriteLine("[DEBUG] MainForm.Show() 완료");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] MainForm.Show() 실패: {ex.Message}");
            }
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= 0x80000 | 0x20; // WS_EX_LAYERED | WS_EX_TRANSPARENT
                return cp;
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                this.Hide();
                return;
            }
            base.OnFormClosing(e);
        }

        protected override void Dispose(bool disposing)
        {
            if (!_isDisposed && disposing)
            {
                Console.WriteLine("[DEBUG] MainForm Dispose 시작");
                _overlayControl?.Dispose();
                _isDisposed = true;
                Console.WriteLine("[DEBUG] MainForm Dispose 완료");
            }
            base.Dispose(disposing);
        }
    }
}