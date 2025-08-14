using System;
using System.Collections.Generic;
using System.Linq;
using StarcUp.Business.Upgrades.Adapters;
using StarcUp.Business.Upgrades.Models;
using StarcUp.Business.Units.Runtime.Models;
using StarcUp.Business.Units.Types;
using StarcUp.Common.Logging;

namespace StarcUp.Business.Profile
{
    /// <summary>
    /// 업그레이드 관리자 구현 (WorkerManager 패턴 참조)
    /// </summary>
    public class UpgradeManager : IUpgradeManager
    {
        private readonly IUpgradeMemoryAdapter _upgradeMemoryAdapter;
        private readonly object _lock = new object();

        private UpgradeTechStatistics? _currentStats;
        private UpgradeTechStatistics? _previousStats;
        private UpgradeSettings? _currentSettings;
        private bool _disposed;

        public event EventHandler<UpgradeStateChangedEventArgs>? StateChanged;
        public event EventHandler<UpgradeCompletedEventArgs>? UpgradeCompleted;
        public event EventHandler<UpgradeProgressEventArgs>? ProgressChanged;
        public event EventHandler<UpgradeProgressEventArgs>? InitialStateDetected;

        public UpgradeTechStatistics? CurrentStatistics => _currentStats;
        public UpgradeSettings? CurrentSettings => _currentSettings;
        public int LocalPlayerId { get; private set; }

        public UpgradeManager(IUpgradeMemoryAdapter upgradeMemoryAdapter)
        {
            _upgradeMemoryAdapter = upgradeMemoryAdapter ?? throw new ArgumentNullException(nameof(upgradeMemoryAdapter));
        }

        public void Initialize(int localPlayerId)
        {
            try
            {
                LocalPlayerId = localPlayerId;

                // 메모리 어댑터 초기화
                if (!_upgradeMemoryAdapter.Initialize())
                {
                    LoggerHelper.Error(" 메모리 어댑터 초기화 실패");
                    return;
                }

                UpdateStatistics(Enumerable.Empty<Unit>());
                SendInitialState();

                LoggerHelper.Info($" 초기화 완료 - 플레이어: {localPlayerId}");
            }
            catch (Exception ex)
            {
                LoggerHelper.Error($" 초기화 실패: {ex.Message}");
            }
        }

        public void UpdateSettings(UpgradeSettings settings)
        {
            try
            {
                lock (_lock)
                {
                    _currentSettings = settings;

                    if (_currentSettings != null)
                    {
                        LoggerHelper.Info($" 설정 업데이트 완료 - 카테고리: {_currentSettings.Categories.Count}개");
                        SendInitialState();
                    }
                }
            }
            catch (Exception ex)
            {
                LoggerHelper.Error($" 설정 업데이트 실패: {ex.Message}");
            }
        }

        public void Update(IEnumerable<Unit> buildings)
        {
            if (_disposed || _currentSettings == null)
                return;

            try
            {
                lock (_lock)
                {
                    // 이전 상태 저장
                    _previousStats = _currentStats?.Clone();

                    // 새로운 통계 생성
                    UpdateStatistics(buildings);

                    // 변경사항 감지 및 이벤트 발생
                    if (_previousStats != null && _currentStats != null)
                    {
                        DetectAndRaiseEvents();
                        DetectProgressChanges();
                    }
                }
            }
            catch (Exception ex)
            {
                LoggerHelper.Error($" 업데이트 실패: {ex.Message}");
            }
        }

        private void UpdateStatistics(IEnumerable<Unit> buildings)
        {
            if (_currentSettings == null)
                return;

            var newStats = new UpgradeTechStatistics
            {
                PlayerIndex = (byte)LocalPlayerId,
                Timestamp = DateTime.Now,
                Categories = new List<UpgradeCategoryData>()
            };

            // 설정된 카테고리별로 데이터 수집
            foreach (var categorySettings in _currentSettings.Categories)
            {
                var categoryData = new UpgradeCategoryData
                {
                    Id = categorySettings.Id,
                    Name = categorySettings.Name,
                    Upgrades = new List<UpgradeData>(),
                    Techs = new List<TechData>()
                };

                // 업그레이드 데이터 수집
                foreach (var upgradeType in categorySettings.Upgrades)
                {
                    try
                    {
                        var level = _upgradeMemoryAdapter.GetUpgradeLevel(upgradeType, (byte)LocalPlayerId);
                        var (isProgressing, remainingFrames, totalFrames) = _upgradeMemoryAdapter.GetProgressInfo((int)upgradeType, buildings);

                        categoryData.Upgrades.Add(new UpgradeData
                        {
                            Type = upgradeType,
                            Level = level,
                            RemainingFrames = remainingFrames,
                            TotalFrames = totalFrames,
                            IsProgressing = isProgressing
                        });
                    }
                    catch (Exception ex)
                    {
                        LoggerHelper.Warning($" 업그레이드 데이터 수집 실패 - {upgradeType}: {ex.Message}");
                    }
                }

                // 테크 데이터 수집
                foreach (var techType in categorySettings.Techs)
                {
                    try
                    {
                        var isCompleted = _upgradeMemoryAdapter.IsTechCompleted(techType, (byte)LocalPlayerId);
                        var (isProgressing, remainingFrames, totalFrames) = _upgradeMemoryAdapter.GetProgressInfo((int)techType, buildings);

                        categoryData.Techs.Add(new TechData
                        {
                            Type = techType,
                            IsCompleted = isCompleted,
                            RemainingFrames = remainingFrames,
                            TotalFrames = totalFrames,
                            IsProgressing = isProgressing
                        });
                    }
                    catch (Exception ex)
                    {
                        LoggerHelper.Warning($" 테크 데이터 수집 실패 - {techType}: {ex.Message}");
                    }
                }

                newStats.Categories.Add(categoryData);
            }

            _currentStats = newStats;
        }

        private void DetectAndRaiseEvents()
        {
            if (_previousStats == null || _currentStats == null || _currentSettings == null)
                return;

            // 상태 추적이 비활성화되어 있으면 이벤트 발생하지 않음
            if (!_currentSettings.UpgradeStateTracking)
                return;

            try
            {
                // 카테고리별로 변경사항 확인
                foreach (var currentCategory in _currentStats.Categories)
                {
                    var previousCategory = _previousStats.Categories.FirstOrDefault(c => c.Id == currentCategory.Id);
                    if (previousCategory == null) continue;

                    // 업그레이드 변경사항 확인
                    foreach (var currentUpgrade in currentCategory.Upgrades)
                    {
                        var previousUpgrade = previousCategory.Upgrades.FirstOrDefault(u => u.Type == currentUpgrade.Type);
                        if (previousUpgrade == null) continue;

                        if (currentUpgrade.Level != previousUpgrade.Level)
                        {
                            // 상태 변경 이벤트 (주석처리 - UpgradeCompleted만 사용)
                            // StateChanged?.Invoke(this, new UpgradeStateChangedEventArgs
                            // {
                            //     UpgradeType = currentUpgrade.Type,
                            //     OldLevel = previousUpgrade.Level,
                            //     NewLevel = currentUpgrade.Level,
                            //     WasCompleted = previousUpgrade.IsCompleted,
                            //     IsCompleted = currentUpgrade.IsCompleted,
                            //     PlayerIndex = (byte)LocalPlayerId
                            // });

                            // 완료 알림 (레벨이 증가한 경우) - 항상 전송
                            if (currentUpgrade.Level > previousUpgrade.Level)
                            {
                                UpgradeCompleted?.Invoke(this, new UpgradeCompletedEventArgs
                                {
                                    UpgradeType = currentUpgrade.Type,
                                    Name = currentUpgrade.Type.ToString(),
                                    Level = currentUpgrade.Level,
                                    PlayerIndex = (byte)LocalPlayerId
                                });
                            }
                        }
                    }

                    // 테크 변경사항 확인
                    foreach (var currentTech in currentCategory.Techs)
                    {
                        var previousTech = previousCategory.Techs.FirstOrDefault(t => t.Type == currentTech.Type);
                        if (previousTech == null) continue;

                        if (currentTech.IsCompleted != previousTech.IsCompleted)
                        {
                            // 상태 변경 이벤트 (주석처리 - UpgradeCompleted만 사용)
                            // StateChanged?.Invoke(this, new UpgradeStateChangedEventArgs
                            // {
                            //     TechType = currentTech.Type,
                            //     OldLevel = (byte)(previousTech.IsCompleted ? 1 : 0),
                            //     NewLevel = (byte)(currentTech.IsCompleted ? 1 : 0),
                            //     WasCompleted = previousTech.IsCompleted,
                            //     IsCompleted = currentTech.IsCompleted,
                            //     PlayerIndex = (byte)LocalPlayerId
                            // });

                            // 완료 알림 (새로 완료된 경우) - 항상 전송
                            if (currentTech.IsCompleted && !previousTech.IsCompleted)
                            {
                                UpgradeCompleted?.Invoke(this, new UpgradeCompletedEventArgs
                                {
                                    TechType = currentTech.Type,
                                    Name = currentTech.Type.ToString(),
                                    Level = 1,
                                    PlayerIndex = (byte)LocalPlayerId
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LoggerHelper.Error($" 이벤트 감지 실패: {ex.Message}");
            }
        }

        /// <summary>
        /// 진행률 변경을 감지하고 이벤트를 발생시킵니다
        /// </summary>
        private void DetectProgressChanges()
        {
            if (_currentStats == null || _currentSettings == null)
                return;

            // 상태 추적이 비활성화되어 있으면 진행률 이벤트도 발생시키지 않음
            if (!_currentSettings.UpgradeStateTracking)
                return;

            try
            {
                // 현재 진행 중인 업그레이드/테크가 있는지 확인
                bool hasProgressingItems = false;

                foreach (var category in _currentStats.Categories)
                {
                    // 진행 중인 업그레이드 확인
                    foreach (var upgrade in category.Upgrades)
                    {
                        if (upgrade.IsProgressing)
                        {
                            hasProgressingItems = true;
                            break;
                        }
                    }

                    // 진행 중인 테크 확인
                    if (!hasProgressingItems)
                    {
                        foreach (var tech in category.Techs)
                        {
                            if (tech.IsProgressing)
                            {
                                hasProgressingItems = true;
                                break;
                            }
                        }
                    }

                    if (hasProgressingItems) break;
                }

                // 진행 중인 항목이 있을 때만 이벤트 발생
                if (hasProgressingItems)
                {
                    ProgressChanged?.Invoke(this, new UpgradeProgressEventArgs
                    {
                        Statistics = _currentStats,
                        PlayerIndex = (byte)LocalPlayerId
                    });
                }
            }
            catch (Exception ex)
            {
                LoggerHelper.Error($" 진행률 감지 실패: {ex.Message}");
            }
        }

        /// <summary>
        /// 초기 상태 감지 및 전송 (게임 중간 실행 시)
        /// </summary>
        private void SendInitialState()
        {
            if (_currentStats == null || _currentSettings == null)
                return;

            try
            {
                // 상태 추적이 비활성화되어 있으면 전송하지 않음
                if (!_currentSettings.UpgradeStateTracking)
                    return;

                // 완료된 업그레이드/테크가 있는지 확인
                bool hasCompletedItems = false;

                foreach (var category in _currentStats.Categories)
                {
                    // 완료된 업그레이드 확인
                    foreach (var upgrade in category.Upgrades)
                    {
                        if (upgrade.Level > 0)
                        {
                            hasCompletedItems = true;
                            break;
                        }
                    }

                    // 완료된 테크 확인
                    if (!hasCompletedItems)
                    {
                        foreach (var tech in category.Techs)
                        {
                            if (tech.IsCompleted)
                            {
                                hasCompletedItems = true;
                                break;
                            }
                        }
                    }

                    if (hasCompletedItems) break;
                }

                // 완료된 항목이 있을 때만 초기 상태 이벤트 발생
                if (hasCompletedItems)
                {
                    InitialStateDetected?.Invoke(this, new UpgradeProgressEventArgs
                    {
                        Statistics = _currentStats,
                        PlayerIndex = (byte)LocalPlayerId
                    });

                    LoggerHelper.Info($" 초기 완료 상태 감지 - 플레이어: {LocalPlayerId}");
                }
            }
            catch (Exception ex)
            {
                LoggerHelper.Error($" 초기 상태 전송 실패: {ex.Message}");
            }
        }

        /// <summary>
        /// 게임 중간 실행 시 완료된 업그레이드/테크 감지를 위한 초기 빈 통계 생성
        /// </summary>
        private UpgradeTechStatistics CreateInitialEmptyStats(byte playerIndex)
        {
            var emptyStats = new UpgradeTechStatistics
            {
                PlayerIndex = playerIndex,
                Timestamp = DateTime.Now,
                Categories = new List<UpgradeCategoryData>()
            };

            // 설정이 있는 경우에만 빈 데이터 구조 생성
            if (_currentSettings != null)
            {
                foreach (var categorySettings in _currentSettings.Categories)
                {
                    var emptyCategory = new UpgradeCategoryData
                    {
                        Id = categorySettings.Id,
                        Name = categorySettings.Name,
                        Upgrades = new List<UpgradeData>(),
                        Techs = new List<TechData>()
                    };

                    // 모든 업그레이드를 레벨 0으로 초기화
                    foreach (var upgradeType in categorySettings.Upgrades)
                    {
                        emptyCategory.Upgrades.Add(new UpgradeData
                        {
                            Type = upgradeType,
                            Level = 0,
                            RemainingFrames = 0,
                            TotalFrames = 0,
                            IsProgressing = false
                        });
                    }

                    // 모든 테크를 미완료로 초기화
                    foreach (var techType in categorySettings.Techs)
                    {
                        emptyCategory.Techs.Add(new TechData
                        {
                            Type = techType,
                            IsCompleted = false,
                            RemainingFrames = 0,
                            TotalFrames = 0,
                            IsProgressing = false
                        });
                    }

                    emptyStats.Categories.Add(emptyCategory);
                }
            }

            return emptyStats;
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            _upgradeMemoryAdapter?.Dispose();

            LoggerHelper.Debug(" 리소스 정리 완료");
        }
    }
}