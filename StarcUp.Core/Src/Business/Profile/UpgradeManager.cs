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
        public event EventHandler<UpgradeCancelledEventArgs>? UpgradeCancelled;
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
                    Items = new List<UpgradeItemData>()
                };

                // 업그레이드/테크 데이터 수집 (새로운 Items 구조)
                foreach (var item in categorySettings.Items)
                {
                    if (item.Type == UpgradeItemType.Upgrade)
                    {
                        var upgradeType = (UpgradeType)item.Value;
                        try
                        {
                            var level = _upgradeMemoryAdapter.GetUpgradeLevel(upgradeType, (byte)LocalPlayerId);
                            var (isProgressing, remainingFrames, _, currentUpgradeLevel) = _upgradeMemoryAdapter.GetProgressInfo((int)upgradeType, buildings, true);

                            categoryData.Items.Add(new UpgradeItemData
                            {
                                Item = item,
                                Level = level,
                                RemainingFrames = remainingFrames,
                                CurrentUpgradeLevel = currentUpgradeLevel
                            });
                        }
                        catch (Exception ex)
                        {
                            LoggerHelper.Warning($" 업그레이드 데이터 수집 실패 - {upgradeType}: {ex.Message}");
                        }
                    }
                    else if (item.Type == UpgradeItemType.Tech)
                    {
                        var techType = (TechType)item.Value;
                        try
                        {
                            var isCompleted = _upgradeMemoryAdapter.IsTechCompleted(techType, (byte)LocalPlayerId);
                            var (isProgressing, remainingFrames, _, currentUpgradeLevel) = _upgradeMemoryAdapter.GetProgressInfo((int)techType, buildings, false);

                            categoryData.Items.Add(new UpgradeItemData
                            {
                                Item = item,
                                Level = isCompleted ? 1 : 0, // 테크는 완료 시 1, 미완료 시 0
                                RemainingFrames = remainingFrames,
                                CurrentUpgradeLevel = currentUpgradeLevel // GetProgressInfo에서 반환된 값 사용
                            });
                        }
                        catch (Exception ex)
                        {
                            LoggerHelper.Warning($" 테크 데이터 수집 실패 - {techType}: {ex.Message}");
                        }
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

                    // 아이템 변경사항 확인 (새로운 Items 구조)
                    foreach (var currentItem in currentCategory.Items)
                    {
                        var previousItem = previousCategory.Items.FirstOrDefault(i =>
                            i.Item.Type == currentItem.Item.Type && i.Item.Value == currentItem.Item.Value);
                        if (previousItem == null) continue;

                        if (currentItem.Item.Type == UpgradeItemType.Upgrade)
                        {
                            // 업그레이드 변경사항 확인
                            if (currentItem.Level != previousItem.Level)
                            {
                                // 완료 알림 (레벨이 증가한 경우)
                                if (currentItem.Level > previousItem.Level)
                                {
                                    UpgradeCompleted?.Invoke(this, new UpgradeCompletedEventArgs
                                    {
                                        Item = currentItem.Item,
                                        Level = (byte)currentItem.Level,
                                        CategoryId = currentCategory.Id,
                                        CategoryName = currentCategory.Name
                                    });
                                }
                            }

                            // 업그레이드 취소 감지: 이전에 진행 중이었으나 현재 진행 중이지 않고, 완료된 레벨에 변화가 없는 경우
                            if (previousItem.RemainingFrames > 0 && currentItem.RemainingFrames == 0 &&
                                currentItem.Level == previousItem.Level)
                            {
                                UpgradeCancelled?.Invoke(this, new UpgradeCancelledEventArgs
                                {
                                    Item = currentItem.Item,
                                    LastUpgradeItemData = previousItem.Clone(),
                                    CategoryId = currentCategory.Id,
                                    CategoryName = currentCategory.Name
                                });
                            }
                        }
                        else if (currentItem.Item.Type == UpgradeItemType.Tech)
                        {
                            // 테크 변경사항 확인
                            if (currentItem.Level != previousItem.Level)
                            {
                                // 완료 알림 (새로 완료된 경우)
                                if (currentItem.Level > previousItem.Level)
                                {
                                    UpgradeCompleted?.Invoke(this, new UpgradeCompletedEventArgs
                                    {
                                        Item = currentItem.Item,
                                        Level = 1,
                                        CategoryId = currentCategory.Id,
                                        CategoryName = currentCategory.Name
                                    });
                                }
                            }

                            // 테크 취소 감지: 이전에 진행 중이었으나 현재 진행 중이지 않고, 완료되지 않은 경우
                            if (previousItem.RemainingFrames > 0 && currentItem.RemainingFrames == 0 &&
                                currentItem.Level == 0 && previousItem.Level == 0)
                            {
                                UpgradeCancelled?.Invoke(this, new UpgradeCancelledEventArgs
                                {
                                    Item = currentItem.Item,
                                    LastUpgradeItemData = previousItem.Clone(),
                                    CategoryId = currentCategory.Id,
                                    CategoryName = currentCategory.Name
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
                // 진행 중인 항목만 포함하는 통계 생성
                var filteredStats = new UpgradeTechStatistics
                {
                    Categories = new List<UpgradeCategoryData>()
                };

                foreach (var category in _currentStats.Categories)
                {
                    // 진행 중인 아이템만 필터링 (remainingFrames > 0)
                    var progressingItems = category.Items.Where(item => item.RemainingFrames > 0).ToList();

                    if (progressingItems.Any())
                    {
                        // 진행 중인 아이템이 있는 카테고리만 추가
                        var filteredCategory = new UpgradeCategoryData
                        {
                            Id = category.Id,
                            Name = category.Name,
                            Items = progressingItems.Select(item => item.Clone()).ToList()
                        };
                        filteredStats.Categories.Add(filteredCategory);
                    }
                }

                // 진행 중인 항목이 있을 때만 이벤트 발생
                if (filteredStats.Categories.Any())
                {
                    ProgressChanged?.Invoke(this, new UpgradeProgressEventArgs
                    {
                        Statistics = filteredStats
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

                InitialStateDetected?.Invoke(this, new UpgradeProgressEventArgs
                {
                    Statistics = _currentStats
                });
            }
            catch (Exception ex)
            {
                LoggerHelper.Error($" 초기 상태 전송 실패: {ex.Message}");
            }
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