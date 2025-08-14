using System;
using System.Collections.Generic;
using System.Linq;
using StarcUp.Business.Upgrades.Adapters;
using StarcUp.Business.Upgrades.Models;
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
                    LoggerHelper.Error("[UpgradeManager] 메모리 어댑터 초기화 실패");
                    return;
                }
                
                // 초기 통계 생성
                _currentStats = new UpgradeTechStatistics
                {
                    PlayerIndex = (byte)localPlayerId,
                    Categories = new List<UpgradeCategoryData>()
                };
                _previousStats = _currentStats.Clone();
                
                LoggerHelper.Info($"[UpgradeManager] 초기화 완료 - 플레이어: {localPlayerId}");
            }
            catch (Exception ex)
            {
                LoggerHelper.Error($"[UpgradeManager] 초기화 실패: {ex.Message}");
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
                        LoggerHelper.Info($"[UpgradeManager] 설정 업데이트 완료 - 카테고리: {_currentSettings.Categories.Count}개");
                        
                        // 설정 변경 시 즉시 업데이트
                        UpdateStatistics();
                    }
                }
            }
            catch (Exception ex)
            {
                LoggerHelper.Error($"[UpgradeManager] 설정 업데이트 실패: {ex.Message}");
            }
        }
        
        public void Update()
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
                    UpdateStatistics();
                    
                    // 변경사항 감지 및 이벤트 발생
                    if (_previousStats != null && _currentStats != null)
                    {
                        DetectAndRaiseEvents();
                    }
                }
            }
            catch (Exception ex)
            {
                LoggerHelper.Error($"[UpgradeManager] 업데이트 실패: {ex.Message}");
            }
        }
        
        private void UpdateStatistics()
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
                        var (isProgressing, remainingFrames, totalFrames) = _upgradeMemoryAdapter.GetProgressInfo((int)upgradeType);
                        
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
                        LoggerHelper.Warning($"[UpgradeManager] 업그레이드 데이터 수집 실패 - {upgradeType}: {ex.Message}");
                    }
                }
                
                // 테크 데이터 수집
                foreach (var techType in categorySettings.Techs)
                {
                    try
                    {
                        var isCompleted = _upgradeMemoryAdapter.IsTechCompleted(techType, (byte)LocalPlayerId);
                        var (isProgressing, remainingFrames, totalFrames) = _upgradeMemoryAdapter.GetProgressInfo((int)techType);
                        
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
                        LoggerHelper.Warning($"[UpgradeManager] 테크 데이터 수집 실패 - {techType}: {ex.Message}");
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
                            // 상태 변경 이벤트
                            StateChanged?.Invoke(this, new UpgradeStateChangedEventArgs
                            {
                                UpgradeType = currentUpgrade.Type,
                                OldLevel = previousUpgrade.Level,
                                NewLevel = currentUpgrade.Level,
                                WasCompleted = previousUpgrade.IsCompleted,
                                IsCompleted = currentUpgrade.IsCompleted,
                                PlayerIndex = (byte)LocalPlayerId
                            });
                            
                            // 완료 알림 (레벨이 증가한 경우)
                            if (_currentSettings.UpgradeCompletionAlert && currentUpgrade.Level > previousUpgrade.Level)
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
                            // 상태 변경 이벤트
                            StateChanged?.Invoke(this, new UpgradeStateChangedEventArgs
                            {
                                TechType = currentTech.Type,
                                OldLevel = (byte)(previousTech.IsCompleted ? 1 : 0),
                                NewLevel = (byte)(currentTech.IsCompleted ? 1 : 0),
                                WasCompleted = previousTech.IsCompleted,
                                IsCompleted = currentTech.IsCompleted,
                                PlayerIndex = (byte)LocalPlayerId
                            });
                            
                            // 완료 알림 (새로 완료된 경우)
                            if (_currentSettings.UpgradeCompletionAlert && currentTech.IsCompleted && !previousTech.IsCompleted)
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
                LoggerHelper.Error($"[UpgradeManager] 이벤트 감지 실패: {ex.Message}");
            }
        }
        
        public void Dispose()
        {
            if (_disposed)
                return;
            
            _disposed = true;
            _upgradeMemoryAdapter?.Dispose();
            
            LoggerHelper.Debug("[UpgradeManager] 리소스 정리 완료");
        }
    }
}