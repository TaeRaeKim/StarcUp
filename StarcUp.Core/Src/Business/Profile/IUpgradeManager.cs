using System;
using StarcUp.Business.Upgrades.Models;

namespace StarcUp.Business.Profile
{
    /// <summary>
    /// 업그레이드 관리자 인터페이스
    /// </summary>
    public interface IUpgradeManager : IDisposable
    {
        // 이벤트
        event EventHandler<UpgradeStateChangedEventArgs>? StateChanged;
        event EventHandler<UpgradeCompletedEventArgs>? UpgradeCompleted;
        
        // 프로퍼티
        UpgradeTechStatistics? CurrentStatistics { get; }
        UpgradeSettings? CurrentSettings { get; }
        int LocalPlayerId { get; }
        
        // 초기화
        void Initialize(int localPlayerId);
        
        // 설정 업데이트
        void UpdateSettings(UpgradeSettings settings);
        
        // 업데이트 (GameManager에서 24fps로 호출)
        void Update();
    }
}