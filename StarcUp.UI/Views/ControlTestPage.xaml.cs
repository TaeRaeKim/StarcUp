namespace StarcUp.UI.Views
{
    /// <summary>
    /// ControlForm의 WinUI 3 버전 - 스타크래프트 프로세스 감지 및 메모리 테스트 도구
    /// </summary>
    public partial class ControlTestPage : Page
    {
        public ControlTestPage()
        {
            this.InitializeComponent();
        }

        private void ShowStatusButton_Click(object sender, RoutedEventArgs e)
        {
            // TODO: 감지 상태 상세 정보 표시
        }

        private void ConnectToProcessButton_Click(object sender, RoutedEventArgs e)
        {
            // TODO: 프로세스 연결 로직
        }

        private void ShowOverlayNotificationButton_Click(object sender, RoutedEventArgs e)
        {
            // TODO: 오버레이 알림 표시
        }

        private void ToggleGameOverlayButton_Click(object sender, RoutedEventArgs e)
        {
            // TODO: 게임 오버레이 토글
        }

        private void SearchUnitsButton_Click(object sender, RoutedEventArgs e)
        {
            // TODO: 유닛 검색 로직
        }

        private void UpdateUnitsButton_Click(object sender, RoutedEventArgs e)
        {
            // TODO: 유닛 데이터 갱신
        }

        private void RefreshMemoryButton_Click(object sender, RoutedEventArgs e)
        {
            // TODO: 메모리 정보 새로고침
        }

        private void BackToMainButton_Click(object sender, RoutedEventArgs e)
        {
            // MainPage로 돌아가기
            if (this.Frame != null)
            {
                this.Frame.Navigate(typeof(MainPage));
            }
        }
    }
}
