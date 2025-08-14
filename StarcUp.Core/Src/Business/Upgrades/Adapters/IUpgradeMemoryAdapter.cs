using System;
using System.Collections.Generic;
using StarcUp.Business.Units.Runtime.Models;
using StarcUp.Business.Units.Types;

namespace StarcUp.Business.Upgrades.Adapters
{
    /// <summary>
    /// 업그레이드 및 테크 메모리 읽기 어댑터 인터페이스
    /// </summary>
    public interface IUpgradeMemoryAdapter : IDisposable
    {
        /// <summary>
        /// 업그레이드 레벨 읽기 (0-3)
        /// </summary>
        byte GetUpgradeLevel(UpgradeType type, byte playerIndex);
        
        /// <summary>
        /// 테크 완료 여부 읽기
        /// </summary>
        bool IsTechCompleted(TechType type, byte playerIndex);
        
        /// <summary>
        /// 진행 중인 업그레이드/테크 정보
        /// </summary>
        (bool isProgressing, int remainingFrames, int totalFrames, int currentUpgradeLevel) GetProgressInfo(int upgradeOrTechId, IEnumerable<Unit> buildings, bool isUpgrade);
        
        /// <summary>
        /// 전체 업그레이드 상태 읽기 (성능 최적화용 배치 읽기)
        /// </summary>
        byte[] ReadAllUpgrades(byte playerIndex);
        
        /// <summary>
        /// 전체 테크 상태 읽기 (성능 최적화용 배치 읽기)
        /// </summary>
        byte[] ReadAllTechs(byte playerIndex);
        
        /// <summary>
        /// 어댑터 초기화
        /// </summary>
        bool Initialize();
    }
}