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

        public MainForm()
        {
            InitializeComponent();
            SetupOverlayControl();

            // 처음에는 숨김
            this.Hide();
        }

        private void InitializeComponent()
        {
            // 폼 기본 설정
            this.FormBorderStyle = FormBorderStyle.None;
            this.TopMost = true;
            this.ShowInTaskbar = false;
            this.StartPosition = FormStartPosition.Manual;
            this.AutoSize = true;
            this.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            this.Padding = new Padding(10);

            // 투명도 설정
            this.BackColor = Color.Black;
            this.TransparencyKey = Color.Black;

            // 마우스 클릭 투과 설정
            this.SetStyle(ControlStyles.SupportsTransparentBackColor, true);
        }

        private void SetupOverlayControl()
        {
            _overlayControl = new OverlayControl();
            this.Controls.Add(_overlayControl);
        }

        /// <summary>
        /// 포인터 값 업데이트
        /// </summary>
        public void UpdatePointerValue(PointerValue pointerValue)
        {
            if (_overlayControl != null && !_isDisposed)
            {
                try
                {
                    // UI 스레드에서 실행되도록 보장
                    if (this.InvokeRequired)
                    {
                        this.Invoke(new Action<PointerValue>(UpdatePointerValue), pointerValue);
                        return;
                    }

                    _overlayControl.UpdatePointerValue(pointerValue);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"포인터 값 업데이트 실패: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// 오버레이 표시 상태 업데이트
        /// </summary>
        public void UpdateStatus(string status)
        {
            if (_overlayControl != null && !_isDisposed)
            {
                try
                {
                    if (this.InvokeRequired)
                    {
                        this.Invoke(new Action<string>(UpdateStatus), status);
                        return;
                    }

                    _overlayControl.UpdateStatus(status);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"상태 업데이트 실패: {ex.Message}");
                }
            }
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                // 클릭 투과 설정 (WS_EX_LAYERED | WS_EX_TRANSPARENT)
                cp.ExStyle |= 0x80000 | 0x20;
                return cp;
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            // 폼이 닫히지 않고 숨겨지도록 함
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
                _overlayControl?.Dispose();
                _isDisposed = true;
            }

            base.Dispose(disposing);
        }
    }
}