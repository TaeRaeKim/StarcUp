using System;
using System.Collections.Generic;
using StarcUp.Business.Units.Types;

namespace StarcUp.Business.Profile.Models
{
    /// <summary>
    /// 인구수 통계 정보 (Core 전용)  
    /// 스타크래프트 메모리에서 읽어온 원시 데이터로만 구성
    /// 모든 값은 실제 UI 값 × 2로 저장됨
    /// </summary>
    public class PopulationStatistics
    {
        // 메모리에서 읽어온 원시 데이터 (실제값 × 2)
        public int RawCurrentSupply { get; set; }    // 메모리의 현재 인구수
        public int RawMaxSupply { get; set; }        // 메모리의 최대 인구수
        public int RawSupplyUsed { get; set; }       // 메모리의 사용된 인구수
        public DateTime LastUpdated { get; set; }
        
        // 건물 개수 (모드 B용) - UnitType enum 사용
        public Dictionary<UnitType, int> BuildingCounts { get; set; } = new();

        /// <summary>
        /// 메모리 기준 여유 인구수
        /// </summary>
        public int RawAvailableSupply => RawMaxSupply - RawCurrentSupply;

        /// <summary>
        /// 현재 인구수가 변경되었는지 확인
        /// </summary>
        public bool HasCurrentSupplyChanged(PopulationStatistics other)
        {
            if (other == null) return true;
            return RawCurrentSupply != other.RawCurrentSupply;
        }

        /// <summary>
        /// 최대 인구수가 변경되었는지 확인
        /// </summary>
        public bool HasMaxSupplyChanged(PopulationStatistics other)
        {
            if (other == null) return true;
            return RawMaxSupply != other.RawMaxSupply;
        }

        /// <summary>
        /// 인구가 막혔는지 확인 (여유분이 0 이하)
        /// </summary>
        public bool IsSupplyBlocked()
        {
            return RawAvailableSupply <= 0;
        }

        /// <summary>
        /// 인구가 설정된 기준값에 근접했는지 확인
        /// </summary>
        /// <param name="userThreshold">사용자 설정 기준값 (UI 기준)</param>
        public bool IsSupplyNearThreshold(int userThreshold)
        {
            // 사용자 설정값을 메모리 기준으로 변환 (× 2)
            int memoryThreshold = userThreshold * 2;
            return RawAvailableSupply <= memoryThreshold && RawAvailableSupply > 0;
        }

        /// <summary>
        /// 인구가 증가했는지 확인
        /// </summary>
        public bool HasSupplyIncreased(PopulationStatistics other)
        {
            if (other == null) return false;
            return RawMaxSupply > other.RawMaxSupply;
        }

        /// <summary>
        /// 특정 건물의 개수가 변경되었는지 확인
        /// </summary>
        public bool HasBuildingCountChanged(PopulationStatistics other, UnitType buildingType)
        {
            if (other == null) return true;
            
            var currentCount = BuildingCounts.GetValueOrDefault(buildingType, 0);
            var previousCount = other.BuildingCounts.GetValueOrDefault(buildingType, 0);
            
            return currentCount != previousCount;
        }

        /// <summary>
        /// 건물 기반 모드에서 계산된 경고 기준값과 비교
        /// </summary>
        /// <param name="calculatedThreshold">건물 배수로 계산된 기준값 (UI 기준)</param>
        public bool IsSupplyNearBuildingThreshold(int calculatedThreshold)
        {
            // 계산된 기준값을 메모리 기준으로 변환 (× 2)
            int memoryThreshold = calculatedThreshold * 2;
            return RawAvailableSupply <= memoryThreshold && RawAvailableSupply > 0;
        }

        /// <summary>
        /// 통계 복사본 생성
        /// </summary>
        public PopulationStatistics Clone()
        {
            return new PopulationStatistics
            {
                RawCurrentSupply = RawCurrentSupply,
                RawMaxSupply = RawMaxSupply,
                RawSupplyUsed = RawSupplyUsed,
                LastUpdated = LastUpdated,
                BuildingCounts = new Dictionary<UnitType, int>(BuildingCounts)
            };
        }
    }
}