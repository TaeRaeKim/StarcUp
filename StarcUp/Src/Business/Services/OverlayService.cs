using System;
using System.Drawing;
using StarcUp.Business.Interfaces;
using StarcUp.Business.Models;
using StarcUp.Common.Events;
using StarcUp.Presentation.Forms;

namespace StarcUp.Business.Services
{
    /// <summary>
    /// 오버레이 서비스 (메인 컨트롤러)
    /// </summary>
    public class OverlayService : IOverlayService
    {
        private readonly IGameDetectionService _gameDetection;
        private readonly IPointerMonitorService _pointerMonitor;
        private MainForm _overlayForm;
        private bool _isRunning;
        private bool _isDisposed;

        public event EventHandler<PointerEventArgs> PointerValueChanged;

        public bool IsRunning => _isRunning;

        public OverlayService(
            IGameDetectionService gameDetection,
            IPointerMonitorService pointerMonitor)
        {
            _gameDetection = gameDetection ?? throw new ArgumentNullException(nameof(gameDetection));
            _pointerMonitor = pointerMonitor ?? throw new ArgumentNullException(nameof(pointerMonitor));

            SubscribeToEvents();
        }

        public void Start()
        {
            if (_isRunning)
                return;

            Console.WriteLine("오버레이 서비스 시작...");

            try
            {
                // 오버레이 폼 생성 (처음에는 숨김)
                _overlayForm = new MainForm();

                // 게임 감지 시작
                _gameDetection.StartDetection();

                _isRunning = true;
                Console.WriteLine("오버레이 서비스 활성화됨");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"오버레이 서비스 시작 실패: {ex.Message}");
                Stop();
            }
        }

        public void Stop()
        {
            if (!_isRunning)
                return;

            Console.WriteLine("오버레이 서비스 중지...");

            try
            {
                _gameDetection.StopDetection();
                _pointerMonitor.StopMonitoring();

                _overlayForm?.Hide();
                _overlayForm?.Dispose();
                _overlayForm = null;

                _isRunning = false;
                Console.WriteLine("오버레이 서비스 중지됨");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"오버레이 서비스 중지 중 오류: {ex.Message}");
            }
        }

        private void SubscribeToEvents()
        {
            _gameDetection.GameFound += OnGameFound;
            _gameDetection.GameLost += OnGameLost;
            _gameDetection.WindowChanged += OnWindowChanged;
            _gameDetection.WindowActivated += OnWindowActivated;
            _gameDetection.WindowDeactivated += OnWindowDeactivated;

            _pointerMonitor.ValueChanged += OnPointerValueChanged;
        }

        private void OnGameFound(object sender, GameEventArgs e)
        {
            Console.WriteLine($"게임 발견됨: {e.GameInfo}");

            // 포인터 모니터링 시작
            _pointerMonitor.StartMonitoring(e.GameInfo.ProcessId);

            // 게임이 활성 상태면 오버레이 표시
            if (e.GameInfo.IsActive && !e.GameInfo.IsMinimized)
            {
                ShowOverlay(e.GameInfo);
            }
        }

        private void OnGameLost(object sender, GameEventArgs e)
        {
            Console.WriteLine($"게임 종료됨: {e.GameInfo}");

            // 포인터 모니터링 중지
            _pointerMonitor.StopMonitoring();

            // 오버레이 숨김
            HideOverlay();
        }

        private void OnWindowChanged(object sender, GameEventArgs e)
        {
            Console.WriteLine($"윈도우 변경됨: {e.GameInfo}");

            // 오버레이 위치 업데이트
            if (e.GameInfo.IsActive && !e.GameInfo.IsMinimized)
            {
                UpdateOverlayPosition(e.GameInfo);
            }
        }

        private void OnWindowActivated(object sender, GameEventArgs e)
        {
            Console.WriteLine("게임 윈도우 활성화됨");

            if (!e.GameInfo.IsMinimized)
            {
                ShowOverlay(e.GameInfo);
            }
        }

        private void OnWindowDeactivated(object sender, GameEventArgs e)
        {
            Console.WriteLine("게임 윈도우 비활성화됨");
            HideOverlay();
        }

        private void OnPointerValueChanged(object sender, PointerEventArgs e)
        {
            Console.WriteLine($"포인터 값 변경: {e.PointerValue}");

            // 오버레이 UI 업데이트
            _overlayForm?.UpdatePointerValue(e.PointerValue);

            // 외부로 이벤트 전파
            PointerValueChanged?.Invoke(this, e);
        }

        private void ShowOverlay(GameInfo gameInfo)
        {
            if (_overlayForm != null)
            {
                UpdateOverlayPosition(gameInfo);

                if (!_overlayForm.Visible)
                {
                    _overlayForm.Show();
                    Console.WriteLine("오버레이 표시됨");
                }
            }
        }

        private void HideOverlay()
        {
            if (_overlayForm != null && _overlayForm.Visible)
            {
                _overlayForm.Hide();
                Console.WriteLine("오버레이 숨김");
            }
        }

        private void UpdateOverlayPosition(GameInfo gameInfo)
        {
            if (_overlayForm == null)
                return;

            try
            {
                var settings = new OverlaySettings(); // 기본 설정 사용
                var offset = settings.GetOffset(gameInfo.IsFullscreen);

                var newLocation = new Point(
                    gameInfo.WindowBounds.Left + offset.X,
                    gameInfo.WindowBounds.Top + offset.Y);

                _overlayForm.Location = newLocation;
                Console.WriteLine($"오버레이 위치 업데이트: {newLocation}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"오버레이 위치 업데이트 실패: {ex.Message}");
            }
        }

        public void Dispose()
        {
            if (_isDisposed)
                return;

            Stop();
            _gameDetection?.Dispose();
            _pointerMonitor?.Dispose();
            _isDisposed = true;
        }
    }
}