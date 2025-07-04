using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace StarcUp.UI.Views
{
    public sealed partial class MainPage : Page
    {
        // 게임 상태 열거형
        public enum GameStatus
        {
            Playing,
            Waiting,
            Error
        }

        // 프리셋 데이터 클래스
        public class Preset
        {
            public string Id { get; set; }
            public string Name { get; set; }
            public string Description { get; set; }
            public bool[] FeatureStates { get; set; }
        }

        // 프리셋 목록
        private readonly List<Preset> _presets = new()
        {
            new Preset
            {
                Id = "preset1",
                Name = "공발질-8겟뽕",
                Description = "공중 발업 질럿 러쉬 + 8마리 겟뽕",
                FeatureStates = new bool[] { true, true, false, true, true, false, true, false, true, false }
            },
            new Preset
            {
                Id = "preset2",
                Name = "커공발-운영",
                Description = "커세어 + 공중 발업 운영 빌드",
                FeatureStates = new bool[] { true, false, true, true, false, true, false, true, false, true }
            },
            new Preset
            {
                Id = "preset3",
                Name = "패닼아비터",
                Description = "패스트 다크템플러 + 아비터 전략",
                FeatureStates = new bool[] { false, true, true, false, true, true, false, true, false, true }
            }
        };

        // 스타크래프트 팁 목록
        private readonly List<string> _starcraftTips = new()
        {
            "일꾼은 게임의 핵심! 항상 일꾼 생산을 우선하세요.",
            "미네랄과 가스의 비율을 2:1로 유지하는 것이 효율적입니다.",
            "정찰은 승리의 열쇠! 상대의 전략을 파악하세요.",
            "컨트롤 그룹(1~9)을 활용해 유닛을 빠르게 선택하세요.",
            "건물 배치는 방어와 효율성을 모두 고려해야 합니다.",
            "업그레이드는 유닛 수량보다 우선할 때가 많습니다.",
            "멀티 확장 타이밍이 경기의 흐름을 좌우합니다.",
            "상성을 고려한 유닛 조합이 승부의 관건입니다.",
            "자원 관리: 미네랄과 가스를 남기지 마세요!",
            "게임 센스: 상대의 패턴을 읽고 대응하세요."
        };

        // 상태 변수들
        private bool _isOverlayActive = false;
        private GameStatus _currentGameStatus = GameStatus.Playing;
        private int _currentPresetIndex = 0;
        private int _currentTipIndex = 0;

        // 타이머들
        private DispatcherTimer _gameStatusTimer;
        private DispatcherTimer _tipTimer;
        private DispatcherTimer _scrollTimer;
        private Storyboard _currentGlowAnimation;

        // 기능 점들 참조
        private Ellipse[] _featureDots;
        private Ellipse[] _presetIndicators;

        public MainPage()
        {
            this.InitializeComponent();
            this.Loaded += MainPage_Loaded;
            InitializeFeatureDots();
            InitializePresetIndicators();
        }

        private void InitializeFeatureDots()
        {
            _featureDots = new Ellipse[]
            {
                FeatureDot1, FeatureDot2, FeatureDot3, FeatureDot4, FeatureDot5,
                FeatureDot6, FeatureDot7, FeatureDot8, FeatureDot9, FeatureDot10
            };
        }

        private void InitializePresetIndicators()
        {
            _presetIndicators = new Ellipse[]
            {
                PresetIndicator1, PresetIndicator2, PresetIndicator3
            };
        }

        private void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            StartGameStatusCycle();
            StartTipCycle();
            UpdatePresetDisplay();
            UpdateFeatureStates();
        }

        private void StartGameStatusCycle()
        {
            // 3초마다 게임 상태 순환 (데모용)
            _gameStatusTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(3)
            };
            _gameStatusTimer.Tick += (s, e) =>
            {
                var statuses = new[] { GameStatus.Playing, GameStatus.Waiting, GameStatus.Error };
                var currentIndex = Array.IndexOf(statuses, _currentGameStatus);
                _currentGameStatus = statuses[(currentIndex + 1) % statuses.Length];
                UpdateGameStatusDisplay();
            };
            _gameStatusTimer.Start();
            
            // 초기 상태 설정
            UpdateGameStatusDisplay();
        }

        private void StartTipCycle()
        {
            // 12초마다 팁 변경
            _tipTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(12)
            };
            _tipTimer.Tick += (s, e) =>
            {
                _currentTipIndex = (_currentTipIndex + 1) % _starcraftTips.Count;
                StartScrollingAnimation();
            };
            _tipTimer.Start();
            
            // 첫 번째 팁 시작
            StartScrollingAnimation();
        }

        private void StartScrollingAnimation()
        {
            ScrollingTipText.Text = _starcraftTips[_currentTipIndex];
            
            // 스크롤 애니메이션 구현
            _scrollTimer?.Stop();
            _scrollTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(50)
            };
            
            double scrollPosition = TipScrollViewer.ActualWidth;
            _scrollTimer.Tick += (s, e) =>
            {
                scrollPosition -= 2;
                if (scrollPosition < -ScrollingTipText.ActualWidth)
                {
                    scrollPosition = TipScrollViewer.ActualWidth;
                }
                
                var transform = new TranslateTransform { X = scrollPosition };
                ScrollingTipText.RenderTransform = transform;
            };
            
            _scrollTimer.Start();
        }

        private void UpdateGameStatusDisplay()
        {
            if (!_isOverlayActive) return;

            // 현재 애니메이션 중지
            _currentGlowAnimation?.Stop();

            // 상태별 색상 및 아이콘 업데이트
            SolidColorBrush statusColor;
            string iconText;
            string statusText;

            switch (_currentGameStatus)
            {
                case GameStatus.Playing:
                    statusColor = (SolidColorBrush)Resources["StarcraftGreen"];
                    iconText = "⚡";
                    statusText = "Active";
                    break;
                case GameStatus.Waiting:
                    statusColor = (SolidColorBrush)Resources["StarcraftYellow"];
                    iconText = "⏰";
                    statusText = "In-game waiting";
                    break;
                case GameStatus.Error:
                    statusColor = (SolidColorBrush)Resources["StarcraftRed"];
                    iconText = "📶";
                    statusText = "Game not detected";
                    break;
                default:
                    return;
            }

            // UI 업데이트
            PowerIconText.Foreground = statusColor;
            PowerStatusText.Foreground = (SolidColorBrush)Resources["StarcraftInactiveText"];
            PowerIconText.Text = iconText;
            PowerStatusText.Text = statusText;
            OuterGlowEllipse.Stroke = statusColor;
            MiddleGlowEllipse.Stroke = statusColor;
            InnerGlowEllipse.Stroke = statusColor;
            RippleEllipse1.Stroke = statusColor;
            RippleEllipse2.Stroke = statusColor;
            RippleEllipse3.Stroke = statusColor;
            OverlayStatusText.Foreground = statusColor;

            // 메인 버튼 스타일 업데이트
            MainPowerButton.BorderBrush = statusColor;

            // CommunityToolkit 애니메이션은 Implicit으로 자동 실행되므로
            // 별도의 시작 코드가 필요하지 않습니다.
            // 상태별로 Opacity를 설정하여 애니메이션을 활성화합니다.
            SetAnimationState(_currentGameStatus);
        }

        private void SetAnimationState(GameStatus status)
        {
            // 상태별로 글로우와 리플 효과의 투명도를 설정하여 애니메이션을 활성화
            switch (status)
            {
                case GameStatus.Playing:
                    // 강한 활성화 상태
                    OuterGlowEllipse.Opacity = 0.4;
                    MiddleGlowEllipse.Opacity = 0.7;
                    InnerGlowEllipse.Opacity = 1.0;
                    RippleEllipse1.Opacity = 0.9;
                    RippleEllipse2.Opacity = 0.7;
                    RippleEllipse3.Opacity = 0.5;
                    break;
                case GameStatus.Waiting:
                    // 중간 활성화 상태
                    OuterGlowEllipse.Opacity = 0.2;
                    MiddleGlowEllipse.Opacity = 0.4;
                    InnerGlowEllipse.Opacity = 0.6;
                    RippleEllipse1.Opacity = 0;
                    RippleEllipse2.Opacity = 0;
                    RippleEllipse3.Opacity = 0;
                    break;
                case GameStatus.Error:
                    // 오류 상태 (빠른 깜빡임)
                    OuterGlowEllipse.Opacity = 0.6;
                    MiddleGlowEllipse.Opacity = 0.8;
                    InnerGlowEllipse.Opacity = 1.0;
                    RippleEllipse1.Opacity = 0;
                    RippleEllipse2.Opacity = 0;
                    RippleEllipse3.Opacity = 0;
                    break;
            }
        }

        private void UpdatePresetDisplay()
        {
            var currentPreset = _presets[_currentPresetIndex];
            PresetNameText.Text = currentPreset.Name;
            
            // 프리셋 인디케이터 업데이트
            for (int i = 0; i < _presetIndicators.Length; i++)
            {
                _presetIndicators[i].Fill = i == _currentPresetIndex 
                    ? (SolidColorBrush)Resources["StarcraftGreen"]
                    : (SolidColorBrush)Resources["StarcraftInactiveBorder"];
            }
        }

        private void UpdateFeatureStates()
        {
            var currentPreset = _presets[_currentPresetIndex];
            
            for (int i = 0; i < _featureDots.Length && i < currentPreset.FeatureStates.Length; i++)
            {
                _featureDots[i].Fill = currentPreset.FeatureStates[i]
                    ? new SolidColorBrush(Microsoft.UI.Colors.White)
                    : (SolidColorBrush)Resources["StarcraftInactiveSecondary"];
            }
        }

        private void MainPowerButton_Click(object sender, RoutedEventArgs e)
        {
            _isOverlayActive = !_isOverlayActive;

            if (_isOverlayActive)
            {
                // 활성화 상태
                PowerIconText.Text = GetCurrentStatusIcon();
                PowerStatusText.Text = GetCurrentStatusText();
                OverlayStatusText.Opacity = 0.8;
                
                UpdateGameStatusDisplay();
            }
            else
            {
                // 비활성화 상태 - 모든 애니메이션 효과 중지
                OuterGlowEllipse.Opacity = 0;
                MiddleGlowEllipse.Opacity = 0;
                InnerGlowEllipse.Opacity = 0;
                RippleEllipse1.Opacity = 0;
                RippleEllipse2.Opacity = 0;
                RippleEllipse3.Opacity = 0;
                OverlayStatusText.Opacity = 0;
                
                PowerIconText.Text = "⚡";
                PowerIconText.Foreground = (SolidColorBrush)Resources["StarcraftInactiveText"];
                PowerStatusText.Text = "모든 기능 비활성화됨";
                PowerStatusText.Foreground = (SolidColorBrush)Resources["StarcraftInactiveText"];
                MainPowerButton.BorderBrush = (SolidColorBrush)Resources["StarcraftInactiveBorder"];
            }
        }

        private string GetCurrentStatusIcon()
        {
            return _currentGameStatus switch
            {
                GameStatus.Playing => "⚡",
                GameStatus.Waiting => "⏰",
                GameStatus.Error => "📶",
                _ => "⚡"
            };
        }

        private string GetCurrentStatusText()
        {
            return _currentGameStatus switch
            {
                GameStatus.Playing => "Active",
                GameStatus.Waiting => "In-game waiting",
                GameStatus.Error => "Game not detected",
                _ => "Active"
            };
        }

        private void PrevPresetButton_Click(object sender, RoutedEventArgs e)
        {
            _currentPresetIndex = _currentPresetIndex == 0 
                ? _presets.Count - 1 
                : _currentPresetIndex - 1;
            
            UpdatePresetDisplay();
            UpdateFeatureStates();
        }

        private void NextPresetButton_Click(object sender, RoutedEventArgs e)
        {
            _currentPresetIndex = (_currentPresetIndex + 1) % _presets.Count;
            UpdatePresetDisplay();
            UpdateFeatureStates();
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            // 상세설정 페이지로 네비게이션
            if (this.Frame != null)
            {
                this.Frame.Navigate(typeof(DetailSettingsPage));
            }
        }
    }
}