using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace StarcUp.UI.Views
{
    /// <summary>
    /// StarcUp 메인 홈페이지 - Discord 스타일의 어두운 테마
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();
        }

        private void ControlTestButton_Click(object sender, RoutedEventArgs e)
        {
            // ControlTestPage로 네비게이션
            if (this.Frame != null)
            {
                this.Frame.Navigate(typeof(ControlTestPage));
            }
        }
    }
}