using Microsoft.UI.Xaml.Navigation;
using StarcUp.UI.Views;
using Microsoft.UI;
using Microsoft.UI.Windowing;

namespace StarcUp.UI
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public partial class App : Application
    {
        private Window window = Window.Current;

        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            this.InitializeComponent();
        }

        /// <summary>
        /// Invoked when the application is launched normally by the end user.  Other entry points
        /// will be used such as when the application is launched to open a specific file.
        /// </summary>
        /// <param name="e">Details about the launch request and process.</param>
        protected override void OnLaunched(LaunchActivatedEventArgs e)
        {
            window ??= new Window();

            // 타이틀 바 어두운 테마 설정
            SetupDarkTitleBar();

            if (window.Content is not Frame rootFrame)
            {
                rootFrame = new Frame();
                rootFrame.NavigationFailed += OnNavigationFailed;
                window.Content = rootFrame;
            }

            _ = rootFrame.Navigate(typeof(MainPage), e.Arguments);
            window.Activate();
        }

        private void SetupDarkTitleBar()
        {
            if (window?.AppWindow?.TitleBar != null)
            {
                var titleBar = window.AppWindow.TitleBar;
                
                // 타이틀 바 색상 설정 (Discord Dark Theme)
                titleBar.BackgroundColor = Windows.UI.Color.FromArgb(0xFF, 0x1E, 0x1F, 0x22); // BackgroundSecondary
                titleBar.ForegroundColor = Windows.UI.Color.FromArgb(0xFF, 0xFF, 0xFF, 0xFF); // TextPrimary
                titleBar.InactiveBackgroundColor = Windows.UI.Color.FromArgb(0xFF, 0x2B, 0x2D, 0x31); // BackgroundPrimary
                titleBar.InactiveForegroundColor = Windows.UI.Color.FromArgb(0xFF, 0xB9, 0xBB, 0xBE); // TextSecondary
                
                // 버튼 색상 설정 - 타이틀 바와 동일한 배경색
                titleBar.ButtonBackgroundColor = Windows.UI.Color.FromArgb(0xFF, 0x1E, 0x1F, 0x22); // BackgroundSecondary
                titleBar.ButtonForegroundColor = Windows.UI.Color.FromArgb(0xFF, 0xFF, 0xFF, 0xFF); // 흰색
                titleBar.ButtonHoverBackgroundColor = Windows.UI.Color.FromArgb(0xFF, 0x40, 0x44, 0x4B); // BackgroundAccent
                titleBar.ButtonHoverForegroundColor = Windows.UI.Color.FromArgb(0xFF, 0xFF, 0xFF, 0xFF); // 흰색
                titleBar.ButtonPressedBackgroundColor = Windows.UI.Color.FromArgb(0xFF, 0x58, 0x65, 0xF2); // BrandPrimary
                titleBar.ButtonPressedForegroundColor = Windows.UI.Color.FromArgb(0xFF, 0xFF, 0xFF, 0xFF); // 흰색
                
                // 비활성 상태 버튼 색상
                titleBar.ButtonInactiveBackgroundColor = Windows.UI.Color.FromArgb(0xFF, 0x2B, 0x2D, 0x31); // BackgroundPrimary
                titleBar.ButtonInactiveForegroundColor = Windows.UI.Color.FromArgb(0xFF, 0x72, 0x76, 0x7D); // TextMuted
                
                // 아이콘 숨기기
                titleBar.IconShowOptions = IconShowOptions.HideIconAndSystemMenu;
                
                // 제목 설정 (빈 문자열로 설정)
                window.Title = "";
            }
        }

        /// <summary>
        /// Invoked when Navigation to a certain page fails
        /// </summary>
        /// <param name="sender">The Frame which failed navigation</param>
        /// <param name="e">Details about the navigation failure</param>
        void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            throw new Exception("Failed to load Page " + e.SourcePageType.FullName);
        }
    }
}
