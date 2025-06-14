using System;
using System.Drawing;
using StarcUp.Business.Interfaces;
using StarcUp.Business.Models;
using StarcUp.Common.Events;
using StarcUp.Presentation.Forms;

namespace StarcUp.Business.Services
{
    /// <summary>
    /// 오버레이 서비스 (메인 컨트롤러) - 수동 시작 모드
    /// </summary>
    public class OverlayService : IOverlayService
    {
        private readonly IGameDetectionService _gameDetection;
        private readonly IPointerMonitorService _pointerMonitor;
        private OverlayForm _overlayForm;
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

            // 이벤트 구독은 미리 설정하지만 서비스는 시작하지 않음
            SubscribeToEvents();
        }

        public void Start()
        {
            if (_isRunning)
            {
                Console.WriteLine("오버레이 서비스가 이미 실행 중입니다.");
                return;
            }

            Console.WriteLine("오버레이 서비스 시작... (포인터 추적 모드)");

            try
            {
                // 오버레이 폼 생성 (처음에는 숨김)
                _overlayForm = new OverlayForm();

                // 게임 감지 서비스가 이미 실행 중인지 확인하고 필요하면 시작
                if (!_gameDetection.IsGameRunning)
                {
                    Console.WriteLine("게임 감지 서비스 시작 (오버레이에서)");
                    _gameDetection.StartDetection();
                }

                _isRunning = true;
                Console.WriteLine("오버레이 서비스 활성화됨 - 포인터 추적 대기 중");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"오버레이 서비스 시작 실패: {ex.Message}");
                Stop();
                throw; // 상위로 예외 전파
            }
        }

        public void Stop()
        {
            if (!_isRunning)
            {
                Console.WriteLine("오버레이 서비스가 이미 중지되어 있습니다.");
                return;
            }

            Console.WriteLine("오버레이 서비스 중지... (포인터 추적만 중지, 게임 감지는 유지)");

            try
            {
                // 포인터 모니터링 중지
                _pointerMonitor.StopMonitoring();

                // 오버레이 폼 정리
                if (_overlayForm != null)
                {
                    _overlayForm.Hide();
                    _overlayForm.Dispose();
                    _overlayForm = null;
                }

                _isRunning = false;
                Console.WriteLine("오버레이 서비스 중지됨 (게임 감지는 계속 실행 중)");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"오버레이 서비스 중지 중 오류: {ex.Message}");
            }
        }

        private void SubscribeToEvents()
        {
            _gameDetection.HandleFound += OnHandleFound;
            _gameDetection.HandleLost += OnHandleLost;
            _gameDetection.WindowMove += OnWindowMove;
            _gameDetection.WindowFocusIn += OnWindowFocusIn;
            _gameDetection.WindowFocusOut += OnWindowFocusOut;

            _pointerMonitor.ValueChanged += OnPointerValueChanged;
        }

        private void OnHandleFound(object sender, GameEventArgs e)
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

        private void OnHandleLost(object sender, GameEventArgs e)
        {
            Console.WriteLine($"게임 종료됨: {e.GameInfo}");

            // 포인터 모니터링 중지
            _pointerMonitor.StopMonitoring();

            // 오버레이 숨김
            HideOverlay();
        }

        private void OnWindowMove(object sender, GameEventArgs e)
        {
            Console.WriteLine($"윈도우 변경됨: {e.GameInfo}");

            // 오버레이 위치 업데이트
            if (e.GameInfo.IsActive && !e.GameInfo.IsMinimized)
            {
                UpdateOverlayPosition(e.GameInfo);
            }
        }

        private void OnWindowFocusIn(object sender, GameEventArgs e)
        {
            Console.WriteLine("게임 윈도우 활성화됨");

            if (!e.GameInfo.IsMinimized)
            {
                ShowOverlay(e.GameInfo);
            }
        }

        private void OnWindowFocusOut(object sender, GameEventArgs e)
        {
            Console.WriteLine("게임 윈도우 비활성화됨");
            HideOverlay();
        }

        private void OnPointerValueChanged(object sender, PointerEventArgs e)
        {
            // 오버레이 UI 업데이트
            _overlayForm?.UpdatePointerValue(e.PointerValue);

            // 외부로 이벤트 전파 (컨트롤 폼에서 수신)
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