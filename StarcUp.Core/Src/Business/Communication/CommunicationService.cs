using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Text.Json;
using StarcUp.Infrastructure.Communication;
using StarcUp.Infrastructure.Windows;
using StarcUp.Common.Events;
using StarcUp.Common.Logging;
using StarcUp.Business.GameDetection;
using StarcUp.Business.InGameDetector;
using StarcUp.Business.Profile;
using StarcUp.Business.Profile.Models;
using StarcUp.Business.Upgrades.Models;

namespace StarcUp.Business.Communication
{
    /// <summary>
    /// StarcUp.UI와의 통신을 관리하는 비즈니스 서비스
    /// </summary>
    public class CommunicationService : ICommunicationService
    {
        private readonly INamedPipeClient _pipeClient;
        private readonly IGameDetector _gameDetector;
        private readonly IInGameDetector _inGameDetector;
        private readonly IWindowManager _windowManager;
        private readonly IWorkerManager _workerManager;
        private readonly IPopulationManager _populationManager;
        private readonly IUpgradeManager _upgradeManager;
        private bool _disposed = false;
        
        // 윈도우 위치 변경 관련 필드
        private WindowPositionData _lastWindowPosition;
        private DateTime _lastPositionSentTime = DateTime.MinValue;
        private const int ThrottleMs = 50; // 50ms 제한
        
        // Debounced Throttling을 위한 필드
        private WindowPositionData _pendingWindowPosition;
        private Timer _debounceTimer;
        private readonly object _debounceLock = new object();
        private const int DebounceDelayMs = 80; // 마지막 이벤트 처리 지연 시간 (UI보다 길게 설정)

        public bool IsConnected => _pipeClient.IsConnected;

        public event EventHandler<bool> ConnectionStateChanged;

        public CommunicationService(INamedPipeClient pipeClient, IGameDetector gameDetector, IInGameDetector inGameDetector, IWindowManager windowManager, IWorkerManager workerManager, IPopulationManager populationManager, IUpgradeManager upgradeManager)
        {
            _pipeClient = pipeClient ?? throw new ArgumentNullException(nameof(pipeClient));
            _gameDetector = gameDetector ?? throw new ArgumentNullException(nameof(gameDetector));
            _inGameDetector = inGameDetector ?? throw new ArgumentNullException(nameof(inGameDetector));
            _windowManager = windowManager ?? throw new ArgumentNullException(nameof(windowManager));
            _workerManager = workerManager ?? throw new ArgumentNullException(nameof(workerManager));
            _populationManager = populationManager ?? throw new ArgumentNullException(nameof(populationManager));
            _upgradeManager = upgradeManager ?? throw new ArgumentNullException(nameof(upgradeManager));
        }

        public async Task<bool> StartAsync(string pipeName = "StarcUp.Dev")
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(CommunicationService));

            try
            {
                // 파이프 이름이 null이거나 기본값이라면 환경에 맞는 이름 사용
                if (string.IsNullOrEmpty(pipeName) || pipeName == "StarcUp.Dev")
                {
                    pipeName = NamedPipeConfig.GetPipeNameForCurrentEnvironment();
                }

                LoggerHelper.Info($"통신 서비스 시작: {pipeName}");

                // 연결 상태 변경 이벤트 구독
                _pipeClient.ConnectionStateChanged += OnConnectionStateChanged;

                // 명령 요청 이벤트 구독
                _pipeClient.CommandRequestReceived += OnCommandRequestReceived;

                // 게임 감지 이벤트 구독
                _gameDetector.HandleFound += OnGameDetected;
                _gameDetector.HandleLost += OnGameEnded;

                // 인 게임 감지 이벤트 구독
                _inGameDetector.InGameStateChanged += OnInGameStatus;

                // 윈도우 위치 변경 이벤트 구독
                _windowManager.WindowPositionChanged += OnWindowPositionChanged;
                _windowManager.WindowSizeChanged += OnWindowSizeChanged;

                // WorkerManager 이벤트 구독
                _workerManager.WorkerStatusChanged += OnWWorkerStatusChanged;
                _workerManager.ProductionStarted += OnWWorkerStatusChanged;
                _workerManager.ProductionCompleted += OnWWorkerStatusChanged;
                _workerManager.ProductionCanceled += OnWWorkerStatusChanged;
                _workerManager.WorkerDied += OnWWorkerStatusChanged;
                _workerManager.IdleCountChanged += OnWorkerIdleCountChanged;
                _workerManager.GasBuildingAlert += OnGasBuildingAlert;

                // PopulationManager 이벤트 구독
                _populationManager.SupplyAlert += OnSupplyAlert;

                // UpgradeManager 이벤트 구독
                // _upgradeManager.StateChanged += OnUpgradeStateChanged;  // 주석처리 - UpgradeCompleted만 사용
                _upgradeManager.UpgradeCompleted += OnUpgradeCompleted;
                _upgradeManager.UpgradeCancelled += OnUpgradeCancelled;
                _upgradeManager.ProgressChanged += OnUpgradeProgressChanged;
                _upgradeManager.InitialStateDetected += OnUpgradeInitialStateDetected;

                // 자동 재연결 시작 (3초 간격, 최대 10회 재시도)
                _pipeClient.StartAutoReconnect(pipeName, 3000, 10);

                // 연결 시도 (재연결 간격에 맞춘 짧은 타임아웃)
                var connected = await _pipeClient.ConnectAsync(pipeName, 2000);
                
                if (connected)
                {
                    LoggerHelper.Info("StarcUp.UI 서버에 연결 성공");
                }
                else
                {
                    LoggerHelper.Warning("StarcUp.UI 서버 연결 실패 - 자동 재연결 시작");
                    
                    // 첫 연결 실패 시 즉시 재연결 루프 시작
                    if (_pipeClient.IsReconnecting == false)
                    {
                        _pipeClient.TriggerReconnect();
                    }
                }

                return connected;
            }
            catch (Exception ex)
            {
                LoggerHelper.Error($"통신 서비스 시작 실패: {ex.Message}");
                return false;
            }
        }

        public void Stop()
        {
            if (_disposed)
                return;

            try
            {
                LoggerHelper.Info("통신 서비스 중지");

                // 자동 재연결 중지
                _pipeClient.StopAutoReconnect();

                // 종료 알림 전송
                if (_pipeClient.IsConnected)
                {
                    _pipeClient.SendEvent("core-shutdown");
                }

                // 연결 해제
                _pipeClient.Disconnect();

                // 이벤트 구독 해제
                _pipeClient.ConnectionStateChanged -= OnConnectionStateChanged;
                _pipeClient.CommandRequestReceived -= OnCommandRequestReceived;
                _gameDetector.HandleFound -= OnGameDetected;
                _gameDetector.HandleLost -= OnGameEnded;
                _inGameDetector.InGameStateChanged -= OnInGameStatus;
                _windowManager.WindowPositionChanged -= OnWindowPositionChanged;
                _windowManager.WindowSizeChanged -= OnWindowSizeChanged;
                
                // WorkerManager 이벤트 구독 해제
                _workerManager.WorkerStatusChanged -= OnWWorkerStatusChanged;
                _workerManager.ProductionStarted -= OnWWorkerStatusChanged;
                _workerManager.ProductionCompleted -= OnWWorkerStatusChanged;
                _workerManager.ProductionCanceled -= OnWWorkerStatusChanged;
                _workerManager.WorkerDied -= OnWWorkerStatusChanged;
                _workerManager.IdleCountChanged -= OnWorkerIdleCountChanged;
                _workerManager.GasBuildingAlert -= OnGasBuildingAlert;

                // PopulationManager 이벤트 구독 해제
                _populationManager.SupplyAlert -= OnSupplyAlert;

                // UpgradeManager 이벤트 구독 해제
                // _upgradeManager.StateChanged -= OnUpgradeStateChanged;  // 주석처리 - UpgradeCompleted만 사용
                _upgradeManager.UpgradeCompleted -= OnUpgradeCompleted;
                _upgradeManager.UpgradeCancelled -= OnUpgradeCancelled;
                _upgradeManager.ProgressChanged -= OnUpgradeProgressChanged;
                _upgradeManager.InitialStateDetected -= OnUpgradeInitialStateDetected;

                // Debounce 타이머 정리
                ClearDebounceTimer();
                lock (_debounceLock)
                {
                    _pendingWindowPosition = null;
                }

                LoggerHelper.Info("통신 서비스 중지 완료");
            }
            catch (Exception ex)
            {
                LoggerHelper.Error($"통신 서비스 중지 오류: {ex.Message}");
            }
        }

        private void OnCommandRequestReceived(object sender, CommandRequestEventArgs e)
        {            
            try
            {
                switch (e.Command)
                {
                    case NamedPipeProtocol.Commands.StartGameDetect:
                        _gameDetector.StartDetection();
                        break;
                        
                    case NamedPipeProtocol.Commands.StopGameDetect:
                        _gameDetector.StopDetection();
                        break;
                        
                    // 프리셋 관련 명령들
                    case NamedPipeProtocol.Commands.PresetInit:
                        HandlePresetInit(e);
                        break;
                        
                    case NamedPipeProtocol.Commands.PresetUpdate:
                        HandlePresetUpdate(e);
                        break;
                        
                    default:
                        LoggerHelper.Warning($" 알 수 없는 명령: {e.Command}");
                        break;
                }
            }
            catch (Exception ex)
            {
                LoggerHelper.Error($" 명령 처리 중 오류 발생: {e.Command} - {ex.Message}");
            }
        }
        private void OnInGameStatus(object sender, InGameEventArgs e){
            try
            {
                var eventData = new
                {
                    eventType = "in-game-status",
                    inGameInfo = new
                    {
                        isInGame = e.IsInGame,
                        timestamp = e.Timestamp.ToString("HH:mm:ss.fff")
                    }
                };
                _pipeClient.SendEvent(eventData.eventType, eventData);
            }
            catch (Exception ex)
            {
                LoggerHelper.Error($" 게임 중 이벤트 전송 실패: {ex.Message}");
            }
        }

        private void OnWindowPositionChanged(object sender, WindowChangedEventArgs e)
        {
            OnWindowChanged(e, "window-position-changed");
        }

        private void OnWindowSizeChanged(object sender, WindowChangedEventArgs e)
        {
            OnWindowChanged(e, "window-size-changed");
        }

        private void OnWindowChanged(WindowChangedEventArgs e, string eventType)
        {
            try
            {
                // 현재 윈도우 정보를 WindowPositionData로 변환
                var positionData = e.CurrentWindowInfo?.ToPositionData();
                if (positionData == null)
                {
                    return;
                }

                // 중복 이벤트 필터링 (5픽셀 이하 변경은 무시) - window-overlay-init은 필터링 제외
                if (eventType != "window-overlay-init" && _lastWindowPosition != null && !positionData.HasPositionChanged(_lastWindowPosition, 5))
                {
                    return;
                }

                // window-overlay-init은 즉시 전송 (throttling 및 debouncing 건너뛰기)
                if (eventType == "window-overlay-init")
                {
                    SendWindowPositionEvent(positionData, eventType);
                    return;
                }

                lock (_debounceLock)
                {
                    _pendingWindowPosition = positionData.Clone();
                    _pendingWindowPosition.EventType = eventType; // 이벤트 타입 저장

                    // Throttling 체크
                    var now = DateTime.UtcNow;
                    if ((now - _lastPositionSentTime).TotalMilliseconds >= ThrottleMs)
                    {
                        // 즉시 전송 가능
                        SendWindowPositionEvent(_pendingWindowPosition, eventType);
                        _pendingWindowPosition = null;
                        ClearDebounceTimer();
                        //LoggerHelper.Info("✅ Core: 즉시 위치 이벤트 전송");
                    }
                    else
                    {
                        // Throttling으로 인해 지연, debounce 타이머 설정
                        SetupDebounceTimer();
                        //LoggerHelper.Info("⏳ Core: Throttling으로 인해 debounce 타이머 설정");
                    }
                }
            }
            catch (Exception ex)
            {
                LoggerHelper.Error($" 윈도우 위치 변경 이벤트 처리 실패: {ex.Message}");
            }
        }

        /// <summary>
        /// Debounce 타이머 설정 (마지막 이벤트 처리 보장)
        /// </summary>
        private void SetupDebounceTimer()
        {
            ClearDebounceTimer();
            
            _debounceTimer = new Timer(OnDebounceTimerElapsed, null, DebounceDelayMs, Timeout.Infinite);
        }

        /// <summary>
        /// Debounce 타이머 정리
        /// </summary>
        private void ClearDebounceTimer()
        {
            _debounceTimer?.Dispose();
            _debounceTimer = null;
        }

        /// <summary>
        /// Debounce 타이머 콜백 (마지막 이벤트 전송)
        /// </summary>
        private void OnDebounceTimerElapsed(object state)
        {
            lock (_debounceLock)
            {
                if (_pendingWindowPosition != null)
                {
                    LoggerHelper.Info("⏰ Core: Debounce 타이머로 마지막 위치 이벤트 전송");
                    SendWindowPositionEvent(_pendingWindowPosition, _pendingWindowPosition.EventType ?? "window-position-changed");
                    _pendingWindowPosition = null;
                }
            }
        }

        /// <summary>
        /// 윈도우 위치 이벤트 실제 전송
        /// </summary>
        private void SendWindowPositionEvent(WindowPositionData positionData, string eventType)
        {
            try
            {
                var eventData = new
                {
                    eventType = eventType,
                    windowPosition = new
                    {
                        x = positionData.X,
                        y = positionData.Y,
                        width = positionData.Width,
                        height = positionData.Height,
                        clientX = positionData.ClientX,
                        clientY = positionData.ClientY,
                        clientWidth = positionData.ClientWidth,
                        clientHeight = positionData.ClientHeight,
                        isMinimized = positionData.IsMinimized,
                        isMaximized = positionData.IsMaximized,
                        isVisible = positionData.IsVisible,
                        timestamp = positionData.Timestamp.ToString("HH:mm:ss.fff")
                    }
                };

                _pipeClient.SendEvent(eventData.eventType, eventData);
                
                _lastWindowPosition = positionData.Clone();
                _lastPositionSentTime = DateTime.UtcNow;
            }
            catch (Exception ex)
            {
                LoggerHelper.Error($" 윈도우 위치 이벤트 전송 실패: {ex.Message}");
            }
        }
        private void OnGameDetected(object sender, GameEventArgs e)
        {
            try
            {
                var eventData = new
                {
                    eventType = "game-detected",
                    gameInfo = new
                    {
                        processId = e.GameInfo.ProcessId,
                        processName = e.GameInfo.ProcessName,
                        detectedAt = e.GameInfo.DetectedAt
                    }
                };

                _pipeClient.SendEvent(eventData.eventType, eventData);

                // 스타크래프트 윈도우 모니터링 시작
                if (_windowManager.StartMonitoring(e.GameInfo.ProcessId))
                {
                    LoggerHelper.Info($"🪟 스타크래프트 윈도우 모니터링 시작 (PID: {e.GameInfo.ProcessId})");
                    
                    // 윈도우 정보 가져와서 window-overlay-init 이벤트 전송
                    var currentWindowInfo = _windowManager.GetCurrentWindowInfo();
                    var windowChangedArgs = new WindowChangedEventArgs(
                        previousWindowInfo: null,
                        currentWindowInfo: currentWindowInfo,
                        changeType: WindowChangeType.PositionChanged
                    );
                    OnWindowChanged(windowChangedArgs, "window-overlay-init");
                }
                else
                {
                    LoggerHelper.Error($" 스타크래프트 윈도우 모니터링 시작 실패 (PID: {e.GameInfo.ProcessId})");
                }
            }
            catch (Exception ex)
            {
                LoggerHelper.Error($" 게임 발견 이벤트 전송 실패: {ex.Message}");
            }
        }
        private void OnGameEnded(object sender, GameEventArgs e)
        {
            try
            {
                var eventData = new
                {
                    eventType = "game-ended",
                    gameInfo = new
                    {
                        processId = e.GameInfo.ProcessId,
                        processName = e.GameInfo.ProcessName,
                        detectedAt = e.GameInfo.DetectedAt,
                        lostAt = DateTime.Now
                    }
                };

                _pipeClient.SendEvent(eventData.eventType, eventData);

                // 스타크래프트 윈도우 모니터링 중지
                _windowManager.StopMonitoring();
                LoggerHelper.Info($"🪟 스타크래프트 윈도우 모니터링 중지 (PID: {e.GameInfo.ProcessId})");
            }
            catch (Exception ex)
            {
                LoggerHelper.Error($" 게임 종료 이벤트 전송 실패: {ex.Message}");
            }
        }
        private async void OnConnectionStateChanged(object sender, bool isConnected)
        {
            if (isConnected)
            {
                LoggerHelper.Info("✅ StarcUp.UI 서버에 연결되었습니다");
                try
                {
                    var pingResponse = await _pipeClient.SendCommandAsync("ping", new[] { "core-ready" });
                }
                catch (Exception ex)
                {
                    LoggerHelper.Error($" 핑 전송 실패: {ex.Message}");
                }
            }
            else
            {
                LoggerHelper.Error(" StarcUp.UI 서버와의 연결이 끊어졌습니다");
            }

            ConnectionStateChanged?.Invoke(this, isConnected);
        }

        /// <summary>
        /// 프리셋 초기화 요청 처리
        /// </summary>
        private void HandlePresetInit(CommandRequestEventArgs e)
        {
            try
            {
                LoggerHelper.Info("🚀 프리셋 초기화 요청 수신");
                
                if (e.Payload == null)
                {
                    LoggerHelper.Error(" 프리셋 초기화 데이터가 없습니다");
                    return;
                }

                // 새로운 프로토콜: payload에 직접 프리셋 데이터가 포함됨
                PresetInitData initData;
                
                if (e.Payload is JsonElement element)
                {
                    initData = JsonSerializer.Deserialize<PresetInitData>(element.GetRawText());
                }
                else if (e.Payload is string jsonString)
                {
                    initData = JsonSerializer.Deserialize<PresetInitData>(jsonString);
                }
                else
                {
                    LoggerHelper.Error($" 지원되지 않는 페이로드 타입: {e.Payload.GetType()}");
                    return;
                }
                
                // 일꾼 프리셋 처리
                if (initData.Presets?.Worker != null)
                {
                    var workerPreset = (WorkerPresetEnum)initData.Presets.Worker.SettingsMask;
                    _workerManager.InitializeWorkerPreset(workerPreset);
                    
                    // 프리셋 변경 이벤트 전송
                    SendWorkerPresetChangedEvent(workerPreset, true);
                }
                
                // 인구수 프리셋 처리
                if (initData.Presets?.Population != null)
                {
                    HandlePopulationPreset(initData.Presets.Population);
                }
                
                // 업그레이드 프리셋 처리
                if (initData.Presets?.Upgrade != null)
                {
                    HandleUpgradePreset(initData.Presets.Upgrade);
                }
                
                // 향후 다른 프리셋들도 여기서 처리...
                // if (initData.Presets?.Unit != null) { ... }
                // if (initData.Presets?.BuildOrder != null) { ... }
                
            }
            catch (Exception ex)
            {
                LoggerHelper.Error($" 프리셋 초기화 실패: {ex.Message}");
            }
        }

        /// <summary>
        /// 프리셋 업데이트 요청 처리
        /// </summary>
        private void HandlePresetUpdate(CommandRequestEventArgs e)
        {
            try
            {
                LoggerHelper.Info("🔄 프리셋 업데이트 요청 수신");
                
                if (e.Payload == null)
                {
                    LoggerHelper.Error(" 프리셋 업데이트 데이터가 없습니다");
                    return;
                }

                // 새로운 프로토콜: payload에 직접 프리셋 데이터가 포함됨
                PresetUpdateData updateData;
                
                if (e.Payload is JsonElement element)
                {
                    updateData = JsonSerializer.Deserialize<PresetUpdateData>(element.GetRawText());
                }
                else if (e.Payload is string jsonString)
                {
                    updateData = JsonSerializer.Deserialize<PresetUpdateData>(jsonString);
                }
                else
                {
                    LoggerHelper.Error($" 지원되지 않는 페이로드 타입: {e.Payload.GetType()}");
                    return;
                }
                
                switch (updateData.PresetType?.ToLower())
                {
                    case "worker":
                        var workerPreset = (WorkerPresetEnum)updateData.Data.SettingsMask;
                        _workerManager.UpdateWorkerPreset(workerPreset);
                        
                        // 프리셋 변경 이벤트 전송
                        SendWorkerPresetChangedEvent(workerPreset, true);
                        break;
                        
                    case "population":
                        HandlePopulationPreset(new PresetItem 
                        { 
                            Enabled = true,  // 업데이트 시에는 활성화되어 있다고 가정
                            Settings = updateData.Data.Settings
                        });
                        break;
                        
                    case "unit":
                        // 향후 구현
                        LoggerHelper.Warning(" 유닛 프리셋은 아직 구현되지 않았습니다");
                        break;
                        
                    case "upgrade":
                        HandleUpgradePreset(new PresetItem 
                        { 
                            Enabled = true,  // 업데이트 시에는 활성화되어 있다고 가정
                            Settings = updateData.Data.Settings
                        });
                        break;
                        
                    case "buildorder":
                        // 향후 구현
                        LoggerHelper.Warning(" 빌드오더 프리셋은 아직 구현되지 않았습니다");
                        break;
                        
                    default:
                        LoggerHelper.Warning($" 알 수 없는 프리셋 타입: {updateData.PresetType}");
                        return;
                }
                
            }
            catch (Exception ex)
            {
                LoggerHelper.Error($" 프리셋 업데이트 실패: {ex.Message}");
            }
        }

        private void OnWWorkerStatusChanged(object sender, WorkerEventArgs e)
        {
            try
            {
                SendWorkerStatusEvent(e);
            }
            catch (Exception ex)
            {
                LoggerHelper.Error($" 일꾼 이벤트 전송 실패: {ex.Message}");
            }
        }

        /// <summary>
        /// 유휴 일꾼 개수 변경 이벤트 처리
        /// </summary>
        private void OnWorkerIdleCountChanged(object sender, WorkerEventArgs e)
        {
            try
            {
                // Idle 프리셋이 설정된 경우에만 전송
                if (!_workerManager.IsWorkerStateSet(WorkerPresetEnum.Idle))
                    return;

                SendWorkerStatusEvent(e);
            }
            catch (Exception ex)
            {
                LoggerHelper.Error($" 유휴 일꾼 변경 이벤트 전송 실패: {ex.Message}");
            }
        }

        /// <summary>
        /// 가스 건물 알림 이벤트 처리
        /// </summary>
        private void OnGasBuildingAlert(object sender, GasBuildingEventArgs e)
        {
            try
            {
                // CheckGas 프리셋이 설정된 경우에만 전송
                if (!_workerManager.IsWorkerStateSet(WorkerPresetEnum.CheckGas))
                    return;

                // 빈 데이터로 알림만 전송
                var eventData = new { };
                _pipeClient.SendEvent(NamedPipeProtocol.Events.GasBuildingAlert, eventData);
            }
            catch (Exception ex)
            {
                LoggerHelper.Error($" 가스 건물 알림 이벤트 전송 실패: {ex.Message}");
            }
        }

        /// <summary>
        /// 인구수 부족 알림 이벤트 처리
        /// </summary>
        private void OnSupplyAlert(object sender, PopulationEventArgs e)
        {
            try
            {
                // 빈 데이터로 알림만 전송
                var eventData = new { };
                _pipeClient.SendEvent(NamedPipeProtocol.Events.SupplyAlert, eventData);
            }
            catch (Exception ex)
            {
                LoggerHelper.Error($" 인구수 부족 알림 이벤트 전송 실패: {ex.Message}");
            }
        }

        /// <summary>
        /// 일꾼 상태 변경 이벤트 전송 (공통)
        /// </summary>
        private void SendWorkerStatusEvent(WorkerEventArgs e)
        {
            var eventData = new
            {
                eventType = e.EventType.ToString(),
                totalWorkers = e.TotalWorkers,
                calculatedTotal = e.CalculatedTotalWorkers,
                idleWorkers = e.IdleWorkers,
                productionWorkers = e.ProductionWorkers
            };

            _pipeClient.SendEvent(NamedPipeProtocol.Events.WorkerStatusChanged, eventData);
        }

        /// <summary>
        /// 일꾼 프리셋 변경 이벤트 전송
        /// </summary>
        private void SendWorkerPresetChangedEvent(WorkerPresetEnum currentPreset, bool success = true)
        {
            try
            {
                var eventData = new
                {
                    success = success,
                    currentPreset = new
                    {
                        mask = (int)currentPreset,
                        flags = GetWorkerPresetFlags(currentPreset)
                    }
                };

                _pipeClient.SendEvent(NamedPipeProtocol.Events.WorkerPresetChanged, eventData);
            }
            catch (Exception ex)
            {
                LoggerHelper.Error($" 일꾼 프리셋 변경 이벤트 전송 실패: {ex.Message}");
            }
        }

        /// <summary>
        /// 인구수 프리셋 처리
        /// </summary>
        private void HandlePopulationPreset(PresetItem populationPreset)
        {
            try
            {
                LoggerHelper.Info($"🏘️ 인구수 프리셋 처리: enabled={populationPreset.Enabled}");
                
                if (!populationPreset.Enabled)
                {
                    LoggerHelper.Warning(" 인구수 기능이 비활성화되어 있습니다");
                    return;
                }

                // settings 필드에서 PopulationSettings 객체 파싱
                if (populationPreset.Settings != null)
                {
                    PopulationSettings populationSettings;
                    
                    if (populationPreset.Settings is JsonElement element)
                    {
                        var jsonText = element.GetRawText();
                        populationSettings = JsonSerializer.Deserialize<PopulationSettings>(jsonText);
                    }
                    else
                    {
                        LoggerHelper.Error($" 지원되지 않는 인구수 설정 타입: {populationPreset.Settings.GetType()}");
                        return;
                    }
                    
                    // PopulationManager에 설정 적용
                    _populationManager.InitializePopulationSettings(populationSettings);
                    LoggerHelper.Info($"✅ 인구수 설정 적용 완료: {populationSettings.Mode}");
                }
                else
                {
                    LoggerHelper.Warning(" 인구수 설정 데이터가 없습니다. 기본 설정 사용");
                    _populationManager.InitializePopulationSettings(new PopulationSettings());
                }
            }
            catch (Exception ex)
            {
                LoggerHelper.Error($" 인구수 프리셋 처리 실패: {ex.Message}");
            }
        }

        /// <summary>
        /// 업그레이드 프리셋 처리
        /// </summary>
        private void HandleUpgradePreset(PresetItem upgradePreset)
        {
            try
            {
                LoggerHelper.Info($"🔧 업그레이드 프리셋 처리: enabled={upgradePreset.Enabled}");
                
                if (!upgradePreset.Enabled)
                {
                    LoggerHelper.Warning("⚡ 업그레이드 기능이 비활성화되어 있습니다");
                    return;
                }

                // settings 필드에서 UpgradeSettings 객체 파싱
                LoggerHelper.Info($"🛠️ 업그레이드 설정 데이터 확인: Settings={upgradePreset.Settings?.ToString() ?? "null"}");
                
                if (upgradePreset.Settings != null)
                {
                    UpgradeSettings upgradeSettings;
                    
                    if (upgradePreset.Settings is JsonElement element)
                    {
                        var jsonText = element.GetRawText();
                        LoggerHelper.Info($"🛠️ 파싱할 JSON: {jsonText}");
                        upgradeSettings = JsonSerializer.Deserialize<UpgradeSettings>(jsonText);
                        LoggerHelper.Info($"🛠️ 파싱된 설정: Categories={upgradeSettings?.Categories?.Count ?? 0}개");
                    }
                    else
                    {
                        LoggerHelper.Error($"🛠️ 지원되지 않는 업그레이드 설정 타입: {upgradePreset.Settings.GetType()}");
                        return;
                    }
                    
                    // UpgradeManager에 설정 적용
                    _upgradeManager.UpdateSettings(upgradeSettings);
                    LoggerHelper.Info($"✅ 업그레이드 설정 적용 완료: {upgradeSettings.Categories.Count}개 카테고리");
                }
                else
                {
                    LoggerHelper.Warning("🛠️ 업그레이드 설정 데이터가 없습니다");
                    
                    // 기본 설정으로 초기화
                    var defaultSettings = new UpgradeSettings
                    {
                        UpgradeStateTracking = true,
                        UpgradeCompletionAlert = false,
                        Categories = new List<UpgradeCategory>()
                    };
                    
                    _upgradeManager.UpdateSettings(defaultSettings);
                    LoggerHelper.Info("🛠️ 기본 업그레이드 설정으로 초기화됨");
                }
            }
            catch (Exception ex)
            {
                LoggerHelper.Error($"🛠️ 업그레이드 프리셋 처리 실패: {ex.Message}");
            }
        }

        /// <summary>
        /// 업그레이드 상태 변경 이벤트 처리 (주석처리 - UpgradeCompleted만 사용)
        /// </summary>
        // private void OnUpgradeStateChanged(object sender, UpgradeStateChangedEventArgs e)
        // {
        //     try
        //     {
        //         var eventData = new
        //         {
        //             upgradeType = e.UpgradeType?.ToString(),
        //             techType = e.TechType?.ToString(),
        //             oldLevel = e.OldLevel,
        //             newLevel = e.NewLevel,
        //             wasCompleted = e.WasCompleted,
        //             isCompleted = e.IsCompleted,
        //             playerIndex = e.PlayerIndex,
        //             timestamp = e.Timestamp
        //         };
        // 
        //         _pipeClient.SendEvent(NamedPipeProtocol.Events.UpgradeStateChanged, eventData);
        //     }
        //     catch (Exception ex)
        //     {
        //         LoggerHelper.Error($"🛠️ 업그레이드 상태 변경 이벤트 전송 실패: {ex.Message}");
        //     }
        // }

        /// <summary>
        /// 업그레이드 완료 이벤트 처리 (UpgradeItem 기반)
        /// </summary>
        private void OnUpgradeCompleted(object sender, UpgradeCompletedEventArgs e)
        {
            try
            {
                var eventData = new
                {
                    item = new
                    {
                        type = (int)e.Item.Type,
                        value = e.Item.Value
                    },
                    level = e.Level,
                    categoryId = e.CategoryId,
                    categoryName = e.CategoryName,
                    timestamp = e.Timestamp
                };

                _pipeClient.SendEvent(NamedPipeProtocol.Events.UpgradeCompleted, eventData);
            }
            catch (Exception ex)
            {
                LoggerHelper.Error($"🛠️ 업그레이드 완료 이벤트 전송 실패: {ex.Message}");
            }
        }

        /// <summary>
        /// 업그레이드 취소 이벤트 핸들러
        /// </summary>
        private void OnUpgradeCancelled(object sender, UpgradeCancelledEventArgs e)
        {
            try
            {
                var eventData = new
                {
                    item = new
                    {
                        type = (int)e.Item.Type,
                        value = e.Item.Value
                    },
                    lastUpgradeItemData = new
                    {
                        item = new
                        {
                            type = (int)e.LastUpgradeItemData.Item.Type,
                            value = e.LastUpgradeItemData.Item.Value
                        },
                        level = e.LastUpgradeItemData.Level,
                        remainingFrames = e.LastUpgradeItemData.RemainingFrames,
                        currentUpgradeLevel = e.LastUpgradeItemData.CurrentUpgradeLevel
                    },
                    categoryId = e.CategoryId,
                    categoryName = e.CategoryName,
                    timestamp = e.Timestamp
                };

                _pipeClient.SendEvent(NamedPipeProtocol.Events.UpgradeCancelled, eventData);
            }
            catch (Exception ex)
            {
                LoggerHelper.Error($"🛠️ 업그레이드 취소 이벤트 전송 실패: {ex.Message}");
            }
        }

        /// <summary>
        /// 업그레이드 진행률 이벤트 핸들러
        /// </summary>
        private void OnUpgradeProgressChanged(object sender, UpgradeProgressEventArgs e)
        {
            try
            {
                // 전체 통계 데이터를 upgrade-data-updated 이벤트로 전송
                _pipeClient.SendEvent(NamedPipeProtocol.Events.UpgradeDataUpdated, e.Statistics);
            }
            catch (Exception ex)
            {
                LoggerHelper.Error($"🛠️ 업그레이드 진행률 이벤트 전송 실패: {ex.Message}");
            }
        }

        /// <summary>
        /// 업그레이드 초기 상태 감지 이벤트 핸들러
        /// </summary>
        private void OnUpgradeInitialStateDetected(object sender, UpgradeProgressEventArgs e)
        {
            try
            {
                // 초기 완료된 상태를 upgrade-init 이벤트로 전송
                _pipeClient.SendEvent(NamedPipeProtocol.Events.UpgradeInit, e.Statistics);
                LoggerHelper.Info($"🛠️ 업그레이드 초기 상태 전송");
            }
            catch (Exception ex)
            {
                LoggerHelper.Error($"🛠️ 업그레이드 초기 상태 이벤트 전송 실패: {ex.Message}");
            }
        }
        
        /// <summary>
        /// WorkerPresetEnum을 문자열 배열로 변환
        /// </summary>
        private string[] GetWorkerPresetFlags(WorkerPresetEnum preset)
        {
            var flags = new List<string>();

            if ((preset & WorkerPresetEnum.Default) != 0) flags.Add("Default");
            if ((preset & WorkerPresetEnum.IncludeProduction) != 0) flags.Add("IncludeProduction");
            if ((preset & WorkerPresetEnum.Idle) != 0) flags.Add("Idle");
            if ((preset & WorkerPresetEnum.DetectProduction) != 0) flags.Add("DetectProduction");
            if ((preset & WorkerPresetEnum.DetectDeath) != 0) flags.Add("DetectDeath");
            if ((preset & WorkerPresetEnum.CheckGas) != 0) flags.Add("CheckGas");

            return flags.ToArray();
        }
        
        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;

            try
            {
                Stop();

                // 추가적인 리소스 정리
                ClearDebounceTimer();
            }
            catch (Exception ex)
            {
                LoggerHelper.Error($" 통신 서비스 종료 오류: {ex.Message}");
            }

            ConnectionStateChanged = null;
        }
    }
}