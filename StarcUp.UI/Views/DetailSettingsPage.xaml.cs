using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace StarcUp.UI.Views
{
    /// <summary>
    /// 상세 설정 페이지 - 개별 기능 토글 설정
    /// </summary>
    public sealed partial class DetailSettingsPage : Page
    {
        public DetailSettingsPage()
        {
            this.InitializeComponent();
        }

        private void BackToMainButton_Click(object sender, RoutedEventArgs e)
        {
            // MainPage로 돌아가기
            if (this.Frame != null)
            {
                this.Frame.Navigate(typeof(MainPage));
            }
        }

        private void ControlTestButton_Click(object sender, RoutedEventArgs e)
        {
            // ControlTestPage로 네비게이션
            if (this.Frame != null)
            {
                this.Frame.Navigate(typeof(ControlTestPage));
            }
        }

        // 기능 토글 이벤트 핸들러들
        private void WorkerStatusToggle_Toggled(object sender, RoutedEventArgs e)
        {
            var toggle = sender as ToggleSwitch;
            // TODO: 일꾼 상태 오버레이 활성화/비활성화 로직
        }

        private void UpgradeProgressToggle_Toggled(object sender, RoutedEventArgs e)
        {
            var toggle = sender as ToggleSwitch;
            // TODO: 업그레이드 진행 오버레이 활성화/비활성화 로직
        }

        private void UnitCountToggle_Toggled(object sender, RoutedEventArgs e)
        {
            var toggle = sender as ToggleSwitch;
            // TODO: 유닛 카운트 오버레이 활성화/비활성화 로직
        }

        private void BuildingAlertToggle_Toggled(object sender, RoutedEventArgs e)
        {
            var toggle = sender as ToggleSwitch;
            // TODO: 건물 알림 오버레이 활성화/비활성화 로직
        }

        private void BuildOrderToggle_Toggled(object sender, RoutedEventArgs e)
        {
            var toggle = sender as ToggleSwitch;
            // TODO: 빌드오더 가이드 오버레이 활성화/비활성화 로직
        }

        private void PopulationWarningToggle_Toggled(object sender, RoutedEventArgs e)
        {
            var toggle = sender as ToggleSwitch;
            // TODO: 인구수 경고 오버레이 활성화/비활성화 로직
        }
    }
}