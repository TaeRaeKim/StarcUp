using System;
using System.Collections.Generic;
using System.Linq;
using StarcUp.Business.MemoryService;
using StarcUp.Business.Units.Runtime.Models;
using StarcUp.Business.Units.Types;
using StarcUp.Business.Units.Types.Extensions;
using StarcUp.Infrastructure.Memory;
using StarcUp.Common.Logging;

namespace StarcUp.Business.Upgrades.Adapters
{
    /// <summary>
    /// 업그레이드 및 테크 메모리 읽기 어댑터 구현
    /// </summary>
    public class UpgradeMemoryAdapter : IUpgradeMemoryAdapter
    {
        private readonly IMemoryService _memoryService;
        private readonly IGameOffsetRepository _offsetRepository;
        private nint _threadStackBase;
        private bool _disposed;
        private bool _isInitialized;
        
        // 업그레이드 총 개수
        private const int UPGRADE_COUNT_SECTION1 = 44;  // 0-43
        private const int UPGRADE_COUNT_SECTION2 = 8;   // 47-54 (실제로는 0-7 인덱스로 저장)
        private const int TOTAL_UPGRADE_COUNT = 55;     // 0-54 (45,46은 사용 안함)
        
        // 테크 총 개수
        private const int TECH_COUNT_SECTION1 = 24;     // 0-23
        private const int TECH_COUNT_SECTION2 = 20;     // 24-43
        private const int TOTAL_TECH_COUNT = 44;        // 0-43
        
        public UpgradeMemoryAdapter(IMemoryService memoryService, IGameOffsetRepository offsetRepository)
        {
            _memoryService = memoryService ?? throw new ArgumentNullException(nameof(memoryService));
            _offsetRepository = offsetRepository ?? throw new ArgumentNullException(nameof(offsetRepository));
        }
        
        public bool Initialize()
        {
            try
            {
                LoggerHelper.Debug("[UpgradeMemoryAdapter] 초기화 시작...");
                
                // 스레드 스택 주소 가져오기 (0번째 스레드)
                _threadStackBase = _memoryService.GetThreadStackAddress(0);
                if (_threadStackBase == 0)
                {
                    LoggerHelper.Error("[UpgradeMemoryAdapter] 스레드 스택 주소를 찾을 수 없습니다.");
                    return false;
                }
                
                // 베이스 주소 조정 (["THREADSTACK0"-00000520])
                _threadStackBase -= 0x520;
                
                LoggerHelper.Debug($"[UpgradeMemoryAdapter] THREADSTACK0 베이스 주소: 0x{_threadStackBase:X}");
                
                _isInitialized = true;
                return true;
            }
            catch (Exception ex)
            {
                LoggerHelper.Error($"[UpgradeMemoryAdapter] 초기화 실패: {ex.Message}");
                return false;
            }
        }
        
        public byte GetUpgradeLevel(UpgradeType type, byte playerIndex)
        {
            if (!_isInitialized)
            {
                LoggerHelper.Warning("[UpgradeMemoryAdapter] 초기화되지 않음");
                return 0;
            }
            
            try
            {
                int upgradeIndex = (int)type;
                nint address = CalculateUpgradeAddress(upgradeIndex, playerIndex);
                
                if (address == 0)
                {
                    LoggerHelper.Warning($"[UpgradeMemoryAdapter] 잘못된 업그레이드 인덱스: {upgradeIndex}");
                    return 0;
                }
                
                return _memoryService.ReadByte(address);
            }
            catch (Exception ex)
            {
                LoggerHelper.Error($"[UpgradeMemoryAdapter] 업그레이드 레벨 읽기 실패: {ex.Message}");
                return 0;
            }
        }
        
        public bool IsTechCompleted(TechType type, byte playerIndex)
        {
            if (!_isInitialized)
            {
                LoggerHelper.Warning("[UpgradeMemoryAdapter] 초기화되지 않음");
                return false;
            }
            
            try
            {
                int techIndex = (int)type;
                nint address = CalculateTechAddress(techIndex, playerIndex);
                
                if (address == 0)
                {
                    LoggerHelper.Warning($"[UpgradeMemoryAdapter] 잘못된 테크 인덱스: {techIndex}");
                    return false;
                }
                
                byte value = _memoryService.ReadByte(address);
                return value > 0;
            }
            catch (Exception ex)
            {
                LoggerHelper.Error($"[UpgradeMemoryAdapter] 테크 상태 읽기 실패: {ex.Message}");
                return false;
            }
        }
        
        public (bool isProgressing, int remainingFrames, int totalFrames, int currentUpgradeLevel) GetProgressInfo(int upgradeOrTechId, IEnumerable<Unit> buildings, bool isUpgrade)
        {
            if (!_isInitialized)
                return (false, 0, 0, 0);
            
            try
            {
                // ActionIndex 76 (업그레이드 중)인 건물들 확인
                if (isUpgrade)
                {
                    var upgradingBuildings = buildings.Where(b => b.ActionIndex == 76);
                    foreach (var building in upgradingBuildings)
                    {
                        // CurrentUpgrade 값이 요청한 upgradeOrTechId와 일치하는지 확인
                        if (building.CurrentUpgrade == upgradeOrTechId)
                        {
                            // Timer 값을 remainingFrames로 사용
                            int totalFrames = GetTotalFrames(upgradeOrTechId, true, building.CurrentUpgradeLevel);
                            int remainingFrames = building.Timer;
                            int currentUpgradeLevel = building.CurrentUpgradeLevel;
                            
                            return (true, remainingFrames, totalFrames, currentUpgradeLevel);
                        }
                    }
                }
                
                // ActionIndex 75 (테크 연구 중)인 건물들 확인
                else
                {
                    var researchingBuildings = buildings.Where(b => b.ActionIndex == 75);
                    foreach (var building in researchingBuildings)
                    {
                        // CurrentTech 값이 요청한 upgradeOrTechId와 일치하는지 확인 (44 = TechType.None)
                        if (building.CurrentTech != 44 && building.CurrentTech == upgradeOrTechId)
                        {
                            // Timer 값을 remainingFrames로 사용
                            int totalFrames = GetTotalFrames(upgradeOrTechId, false, 0);
                            int remainingFrames = building.Timer;
                            
                            return (true, remainingFrames, totalFrames, 1); // 테크는 진행 중일 때 1
                        }
                    }
                }
                
                return (false, 0, 0, 0);
            }
            catch (Exception ex)
            {
                LoggerHelper.Error($"[UpgradeMemoryAdapter] GetProgressInfo 실패: {ex.Message}");
                return (false, 0, 0, 0);
            }
        }
        
        /// <summary>
        /// 업그레이드/테크의 정확한 총 프레임 수 반환
        /// </summary>
        private int GetTotalFrames(int upgradeOrTechId, bool isUpgrade, byte currentLevel)
        {
            try
            {
                if (isUpgrade)
                {
                    var upgradeType = (UpgradeType)upgradeOrTechId;
                    // 레벨별 업그레이드인 경우 currentLevel 사용, 아니면 기본 프레임 사용
                    return upgradeType.IsLeveledUpgrade() 
                        ? upgradeType.GetFramesForLevel(currentLevel > 0 ? currentLevel : 1)
                        : upgradeType.GetBaseFrames();
                }
                else
                {
                    var techType = (TechType)upgradeOrTechId;
                    return techType.GetFrames();
                }
            }
            catch (Exception ex)
            {
                LoggerHelper.Warning($"[UpgradeMemoryAdapter] GetTotalFrames 실패 - ID: {upgradeOrTechId}, {ex.Message}");
                // 기본값 반환
                return isUpgrade ? 2000 : 1500;
            }
        }
        
        public byte[] ReadAllUpgrades(byte playerIndex)
        {
            if (!_isInitialized)
            {
                LoggerHelper.Warning("[UpgradeMemoryAdapter] 초기화되지 않음");
                return new byte[TOTAL_UPGRADE_COUNT];
            }
            
            try
            {
                // 먼저 _threadStackBase에서 포인터 값을 읽음
                nint basePointer = _memoryService.ReadPointer(_threadStackBase);
                if (basePointer == 0)
                {
                    LoggerHelper.Warning($"[UpgradeMemoryAdapter] THREADSTACK0 포인터 읽기 실패");
                    return new byte[TOTAL_UPGRADE_COUNT];
                }
                
                byte[] upgrades = new byte[TOTAL_UPGRADE_COUNT];
                
                // Section 1: 0-43
                nint section1Address = basePointer + _offsetRepository.GetUpgradeOffset1() + (playerIndex * UPGRADE_COUNT_SECTION1);
                byte[] section1Buffer = new byte[UPGRADE_COUNT_SECTION1];
                if (!_memoryService.ReadMemoryIntoBuffer(section1Address, section1Buffer, UPGRADE_COUNT_SECTION1))
                {
                    LoggerHelper.Warning($"[UpgradeMemoryAdapter] Section 1 메모리 읽기 실패 - 플레이어: {playerIndex}");
                    return upgrades; // 빈 배열 반환
                }
                Array.Copy(section1Buffer, 0, upgrades, 0, UPGRADE_COUNT_SECTION1);
                
                // 45, 46은 사용하지 않으므로 0으로 유지
                
                // Section 2: 47-54
                nint section2Address = basePointer + _offsetRepository.GetUpgradeOffset2() + (playerIndex * UPGRADE_COUNT_SECTION2);
                byte[] section2Buffer = new byte[UPGRADE_COUNT_SECTION2];
                if (!_memoryService.ReadMemoryIntoBuffer(section2Address, section2Buffer, UPGRADE_COUNT_SECTION2))
                {
                    LoggerHelper.Warning($"[UpgradeMemoryAdapter] Section 2 메모리 읽기 실패 - 플레이어: {playerIndex}");
                    return upgrades; // Section 1만 있는 상태로 반환
                }
                Array.Copy(section2Buffer, 0, upgrades, 47, UPGRADE_COUNT_SECTION2);
                
                return upgrades;
            }
            catch (Exception ex)
            {
                LoggerHelper.Error($"[UpgradeMemoryAdapter] 전체 업그레이드 읽기 실패: {ex.Message}");
                return new byte[TOTAL_UPGRADE_COUNT];
            }
        }
        
        public byte[] ReadAllTechs(byte playerIndex)
        {
            if (!_isInitialized)
            {
                LoggerHelper.Warning("[UpgradeMemoryAdapter] 초기화되지 않음");
                return new byte[TOTAL_TECH_COUNT];
            }
            
            try
            {
                // 먼저 _threadStackBase에서 포인터 값을 읽음
                nint basePointer = _memoryService.ReadPointer(_threadStackBase);
                if (basePointer == 0)
                {
                    LoggerHelper.Warning($"[UpgradeMemoryAdapter] THREADSTACK0 포인터 읽기 실패");
                    return new byte[TOTAL_TECH_COUNT];
                }
                
                byte[] techs = new byte[TOTAL_TECH_COUNT];
                
                // Section 1: 0-23
                nint section1Address = basePointer + _offsetRepository.GetTechOffset1() + (playerIndex * TECH_COUNT_SECTION1);
                byte[] section1Buffer = new byte[TECH_COUNT_SECTION1];
                if (!_memoryService.ReadMemoryIntoBuffer(section1Address, section1Buffer, TECH_COUNT_SECTION1))
                {
                    LoggerHelper.Warning($"[UpgradeMemoryAdapter] Tech Section 1 메모리 읽기 실패 - 플레이어: {playerIndex}");
                    return techs; // 빈 배열 반환
                }
                Array.Copy(section1Buffer, 0, techs, 0, TECH_COUNT_SECTION1);
                
                // Section 2: 24-43
                nint section2Address = basePointer + _offsetRepository.GetTechOffset2() + (playerIndex * TECH_COUNT_SECTION2);
                byte[] section2Buffer = new byte[TECH_COUNT_SECTION2];
                if (!_memoryService.ReadMemoryIntoBuffer(section2Address, section2Buffer, TECH_COUNT_SECTION2))
                {
                    LoggerHelper.Warning($"[UpgradeMemoryAdapter] Tech Section 2 메모리 읽기 실패 - 플레이어: {playerIndex}");
                    return techs; // Section 1만 있는 상태로 반환
                }
                Array.Copy(section2Buffer, 0, techs, 24, TECH_COUNT_SECTION2);
                
                return techs;
            }
            catch (Exception ex)
            {
                LoggerHelper.Error($"[UpgradeMemoryAdapter] 전체 테크 읽기 실패: {ex.Message}");
                return new byte[TOTAL_TECH_COUNT];
            }
        }
        
        private nint CalculateUpgradeAddress(int upgradeIndex, byte playerIndex)
        {
            if (upgradeIndex < 0 || upgradeIndex >= TOTAL_UPGRADE_COUNT)
                return 0;
            
            try
            {
                // 먼저 _threadStackBase에서 포인터 값을 읽음
                nint basePointer = _memoryService.ReadPointer(_threadStackBase);
                if (basePointer == 0)
                {
                    LoggerHelper.Warning($"[UpgradeMemoryAdapter] THREADSTACK0 포인터 읽기 실패");
                    return 0;
                }
                
                // 0-43 범위
                if (upgradeIndex <= 43)
                {
                    nint finalAddress = basePointer + _offsetRepository.GetUpgradeOffset1() + (playerIndex * UPGRADE_COUNT_SECTION1) + upgradeIndex;
                    return finalAddress;
                }
                // 47-54 범위
                else if (upgradeIndex >= 47 && upgradeIndex <= 54)
                {
                    int adjustedIndex = upgradeIndex - 47; // 0-7로 조정
                    nint finalAddress = basePointer + _offsetRepository.GetUpgradeOffset2() + (playerIndex * UPGRADE_COUNT_SECTION2) + adjustedIndex;
                    return finalAddress;
                }
                
                return 0;
            }
            catch (Exception ex)
            {
                LoggerHelper.Error($"[UpgradeMemoryAdapter] 업그레이드 주소 계산 실패: {ex.Message}");
                return 0;
            }
        }
        
        private nint CalculateTechAddress(int techIndex, byte playerIndex)
        {
            if (techIndex < 0 || techIndex >= TOTAL_TECH_COUNT)
                return 0;
            
            try
            {
                // 먼저 _threadStackBase에서 포인터 값을 읽음
                nint basePointer = _memoryService.ReadPointer(_threadStackBase);
                if (basePointer == 0)
                {
                    LoggerHelper.Warning($"[UpgradeMemoryAdapter] THREADSTACK0 포인터 읽기 실패");
                    return 0;
                }
                
                // 0-23 범위
                if (techIndex <= 23)
                {
                    nint finalAddress = basePointer + _offsetRepository.GetTechOffset1() + (playerIndex * TECH_COUNT_SECTION1) + techIndex;
                    return finalAddress;
                }
                // 24-43 범위
                else if (techIndex <= 43)
                {
                    int adjustedIndex = techIndex - 24; // 0-19로 조정
                    nint finalAddress = basePointer + _offsetRepository.GetTechOffset2() + (playerIndex * TECH_COUNT_SECTION2) + adjustedIndex;
                    return finalAddress;
                }
                
                return 0;
            }
            catch (Exception ex)
            {
                LoggerHelper.Error($"[UpgradeMemoryAdapter] 테크 주소 계산 실패: {ex.Message}");
                return 0;
            }
        }
        
        public void Dispose()
        {
            if (_disposed)
                return;
            
            _disposed = true;
            LoggerHelper.Debug("[UpgradeMemoryAdapter] 리소스 정리 완료");
        }
    }
}