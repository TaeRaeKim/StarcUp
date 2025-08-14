using System;
using System.Collections.Generic;
using System.Linq;
using StarcUp.Business.Profile.Models;
using StarcUp.Business.Units.Runtime.Models;
using StarcUp.Business.Units.Runtime.Services;
using StarcUp.Business.Units.Types;
using StarcUp.Business.MemoryService;
using StarcUp.Common.Events;
using StarcUp.Common.Logging;

namespace StarcUp.Business.Profile
{
    /// <summary>
    /// 인구수 관리 및 경고 시스템
    /// 설정된 조건에 따라 인구 부족 경고를 발생시킴
    /// </summary>
    public class PopulationManager : IPopulationManager
    {
        private PopulationStatistics _currentStats;
        private readonly object _lock = new object();
        private TimeSpan _currentGameTime = TimeSpan.Zero;

        // 의존성 주입된 서비스들
        private readonly IMemoryService _memoryService;
        private readonly IUnitCountService _unitCountService;

        // 이벤트 및 쿨타임 관리
        public event EventHandler<PopulationEventArgs> SupplyAlert;
        private DateTime _lastSupplyAlertTime = DateTime.MinValue;
        private readonly TimeSpan _supplyAlertCooldown = TimeSpan.FromSeconds(10); // 2초 쿨타임

        // 속성
        public PopulationSettings Settings { get; set; } = new PopulationSettings();
        public PopulationStatistics CurrentStatistics => _currentStats;
        public int LocalPlayerId { get; private set; }
        public RaceType LocalPlayerRace { get; private set; }

        /// <summary>
        /// PopulationManager 생성자
        /// 의존성 주입을 통해 필요한 서비스들을 받아옴
        /// </summary>
        /// <param name="memoryService">메모리 서비스</param>
        /// <param name="unitCountService">유닛 카운트 서비스</param>
        public PopulationManager(IMemoryService memoryService, IUnitCountService unitCountService)
        {
            _memoryService = memoryService ?? throw new ArgumentNullException(nameof(memoryService));
            _unitCountService = unitCountService ?? throw new ArgumentNullException(nameof(unitCountService));
            _currentStats = new PopulationStatistics();
        }

        /// <summary>
        /// 인구수 관리자 초기화
        /// 플레이어 ID와 종족을 설정하고 통계를 초기화함
        /// </summary>
        /// <param name="localPlayerId">로컬 플레이어 ID</param>
        /// <param name="localPlayerRace">로컬 플레이어 종족</param>
        public void Initialize(int localPlayerId, RaceType localPlayerRace)
        {
            LocalPlayerId = localPlayerId;
            LocalPlayerRace = localPlayerRace;
            LoggerHelper.Info($"인구수 관리자 초기화 - 플레이어 ID: {localPlayerId}, 종족: {localPlayerRace}");
            
            // 통계 초기화
            lock (_lock)
            {
                _currentStats = new PopulationStatistics();
            }
        }

        /// <summary>
        /// 메모리에서 인구수 데이터를 직접 읽어와서 업데이트하고 경고 조건을 확인함
        /// 모든 값은 스타크래프트 메모리 기준 (실제값 × 2)
        /// </summary>
        public void UpdatePopulationData()
        {
            if (!_memoryService.IsConnected || LocalPlayerId < 0) return;

            try
            {
                lock (_lock)
                {
                    // 메모리에서 종족별 인구수 직접 읽기
                    var rawSupplyUsed = _memoryService.ReadSupplyUsed(LocalPlayerId, LocalPlayerRace);
                    var rawSupplyMax = _memoryService.ReadSupplyMax(LocalPlayerId, LocalPlayerRace);

                    _currentStats.RawCurrentSupply = rawSupplyUsed;
                    _currentStats.RawMaxSupply = rawSupplyMax;
                    _currentStats.RawSupplyUsed = rawSupplyUsed;
                    _currentStats.LastUpdated = DateTime.Now;

                    // 모드 B인 경우 건물 개수도 업데이트
                    if (Settings.Mode == PopulationMode.Building && Settings.BuildingSettings != null)
                    {
                        UpdateBuildingCounts();
                    }

                    // 인구수 경고 조건 확인
                    CheckSupplyAlert();
                }
            }
            catch (Exception ex)
            {
                LoggerHelper.Error($"인구수 데이터 업데이트 실패: {ex.Message}");
            }
        }

        /// <summary>
        /// 게임 시간을 업데이트함 (모드 A의 시간 제한 기능용)
        /// </summary>
        /// <param name="gameTime">현재 게임 시간</param>
        public void UpdateGameTime(TimeSpan gameTime)
        {
            _currentGameTime = gameTime;
        }

        /// <summary>
        /// 인구수 설정을 초기화함
        /// 게임 시작 시 또는 프리셋 로드 시 호출됨
        /// </summary>
        /// <param name="settings">인구수 설정 객체</param>
        public void InitializePopulationSettings(PopulationSettings settings)
        {
            if (settings == null)
            {
                LoggerHelper.Warning("인구수 설정이 null입니다. 기본 설정을 사용합니다.");
                Settings = new PopulationSettings();
                return;
            }

            LoggerHelper.Info($"인구수 설정 초기화 시작 - 모드: {settings.Mode}");
            Settings = settings;
            LoggerHelper.Info("인구수 설정 초기화 완료");
        }

        /// <summary>
        /// 인구수 설정을 업데이트함
        /// UI에서 설정 변경 시 호출됨
        /// </summary>
        /// <param name="newSettings">새로운 인구수 설정</param>
        /// <returns>이전 설정 (복원용)</returns>
        public PopulationSettings UpdatePopulationSettings(PopulationSettings newSettings)
        {
            var previousSettings = Settings;
            LoggerHelper.Info($"인구수 설정 업데이트: {previousSettings.Mode} → {newSettings.Mode}");
            Settings = newSettings ?? new PopulationSettings();
            return previousSettings;
        }

        /// <summary>
        /// 설정된 조건에 따라 인구 부족 경고가 필요한지 확인하고 이벤트를 발생시킴
        /// 모드 A: 고정값 기반 + 시간 제한 확인
        /// 모드 B: 건물 기반 계산값 비교
        /// </summary>
        private void CheckSupplyAlert()
        {
            if (Settings == null) return;

            // 최대인구수가 200(메모리상 400)인 경우 경고하지 않음
            if (_currentStats.RawMaxSupply >= 400)
            {
                return;
            }

            bool shouldAlert = false;
            int thresholdValue = 0;
            string alertMessage = "";

            if (Settings.Mode == PopulationMode.Fixed && Settings.FixedSettings != null)
            {
                // 모드 A: 고정값 기반
                var fixedSettings = Settings.FixedSettings;
                thresholdValue = fixedSettings.ThresholdValue;

                // 시간 제한 확인
                if (fixedSettings.TimeLimit?.Enabled == true)
                {
                    var timeLimit = TimeSpan.FromSeconds(fixedSettings.TimeLimit.TotalSeconds);
                    if (_currentGameTime < timeLimit)
                    {
                        // 아직 시간 제한에 걸림, 경고하지 않음
                        return;
                    }
                }

                // 인구수 기준값과 비교 (막힘 또는 기준값 근접)
                shouldAlert = _currentStats.IsSupplyNearThreshold(thresholdValue) || _currentStats.IsSupplyBlocked();
                alertMessage = $"인구 부족 경고! 여유분: {_currentStats.RawAvailableSupply / 2}, 기준값: {thresholdValue}";
            }
            else if (Settings.Mode == PopulationMode.Building && Settings.BuildingSettings != null)
            {
                // 모드 B: 건물 기반
                var buildingSettings = Settings.BuildingSettings;
                int calculatedThreshold = CalculateBuildingBasedThreshold(buildingSettings);
                
                if (calculatedThreshold > 0)
                {
                    thresholdValue = calculatedThreshold;
                    shouldAlert = _currentStats.IsSupplyNearBuildingThreshold(calculatedThreshold) || _currentStats.IsSupplyBlocked();
                    alertMessage = $"인구 부족 경고! 여유분: {_currentStats.RawAvailableSupply / 2}, 계산된 기준값: {calculatedThreshold}";
                }
            }

            // 경고 조건 만족 시 이벤트 발생
            if (shouldAlert)
            {
                RaiseSupplyAlert(thresholdValue, alertMessage);
            }
        }

        /// <summary>
        /// 건물 기반 모드에서 경고 기준값을 계산함
        /// (각 건물 개수 × 배수)의 합계를 반환
        /// 저그의 경우 진화 건물들의 총합을 계산 (해처리+레어+하이브)
        /// </summary>
        /// <param name="buildingSettings">건물 기반 설정</param>
        /// <returns>계산된 경고 기준값 (UI 기준)</returns>
        private int CalculateBuildingBasedThreshold(BuildingModeSettings buildingSettings)
        {
            int totalThreshold = 0;

            foreach (var trackedBuilding in buildingSettings.TrackedBuildings.Where(b => b.Enabled))
            {
                int buildingCount = 0;
                
                // 저그 해처리 진화 로직: 해처리 추적 시 레어+하이브도 포함
                if (LocalPlayerRace == RaceType.Zerg && trackedBuilding.BuildingType == UnitType.ZergHatchery)
                {
                    buildingCount = _currentStats.BuildingCounts.GetValueOrDefault(UnitType.ZergHatchery, 0) +
                                   _currentStats.BuildingCounts.GetValueOrDefault(UnitType.ZergLair, 0) +
                                   _currentStats.BuildingCounts.GetValueOrDefault(UnitType.ZergHive, 0);
                }
                else
                {
                    buildingCount = _currentStats.BuildingCounts.GetValueOrDefault(trackedBuilding.BuildingType, 0);
                }
                
                totalThreshold += buildingCount * trackedBuilding.Multiplier;
            }

            return totalThreshold;
        }

        /// <summary>
        /// 건물 개수가 변경되었는지 확인함
        /// </summary>
        /// <param name="previousCounts">이전 건물 개수</param>
        /// <returns>변경된 경우 true</returns>
        private bool HasBuildingCountsChanged(Dictionary<UnitType, int> previousCounts)
        {
            if (previousCounts.Count != _currentStats.BuildingCounts.Count)
                return true;

            foreach (var kvp in _currentStats.BuildingCounts)
            {
                if (!previousCounts.TryGetValue(kvp.Key, out var previousCount) || previousCount != kvp.Value)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// 인구 부족 경고 이벤트를 발생시킴
        /// UI로 경고 정보를 전송함
        /// 2초 쿨타임 적용으로 스팸 방지
        /// </summary>
        /// <param name="thresholdValue">경고 기준값</param>
        /// <param name="alertMessage">경고 메시지</param>
        private void RaiseSupplyAlert(int thresholdValue, string alertMessage)
        {
            var currentTime = DateTime.Now;
            
            // 쿨타임 체크 - 마지막 경고 후 2초가 지나지 않았으면 무시
            if (currentTime - _lastSupplyAlertTime < _supplyAlertCooldown)
            {
                return;
            }
            
            // 쿨타임 갱신
            _lastSupplyAlertTime = currentTime;
            
            var eventArgs = new PopulationEventArgs(
                PopulationEventType.SupplyAlert,
                _currentStats,
                null, // previousStats 불필요
                (byte)LocalPlayerId)
            {
                ThresholdValue = thresholdValue,
                AlertMessage = alertMessage
            };

            LoggerHelper.Info($"{alertMessage}");
            SupplyAlert?.Invoke(this, eventArgs);
        }

        /// <summary>
        /// 건물 개수를 업데이트함 (모드 B 전용)
        /// 종족별 핵심 생산 건물만 조회
        /// </summary>
        private void UpdateBuildingCounts()
        {
            if (!_unitCountService.IsRunning) return;

            try
            {
                _currentStats.BuildingCounts.Clear();

                // 종족별 핵심 생산 건물 조회
                UnitType[] buildingTypes = LocalPlayerRace switch
                {
                    RaceType.Terran => new[] 
                    { 
                        UnitType.TerranBarracks, 
                        UnitType.TerranFactory, 
                        UnitType.TerranStarport 
                    },
                    RaceType.Protoss => new[] 
                    { 
                        UnitType.ProtossGateway, 
                        UnitType.ProtossRoboticsFacility, 
                        UnitType.ProtossStargate 
                    },
                    RaceType.Zerg => new[] 
                    { 
                        UnitType.ZergHatchery,
                        UnitType.ZergLair,
                        UnitType.ZergHive
                    },
                    _ => Array.Empty<UnitType>()
                };

                foreach (var buildingType in buildingTypes)
                {
                    var count = _unitCountService.GetUnitCount(buildingType, (byte)LocalPlayerId);
                    if (count > 0)
                    {
                        _currentStats.BuildingCounts[buildingType] = count;
                    }
                }
            }
            catch (Exception ex)
            {
                LoggerHelper.Error($"건물 개수 업데이트 실패: {ex.Message}");
            }
        }

        /// <summary>
        /// 리소스 정리
        /// 이벤트 핸들러를 해제함
        /// </summary>
        public void Dispose()
        {
            SupplyAlert = null;
        }
    }
}