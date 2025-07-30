using System;
using StarcUp.Business.Units.Types;

namespace StarcUp.Business.Profile.Models
{
    public class WorkerStatistics
    {
        public int TotalWorkers { get; set; }
        public int IdleWorkers { get; set; }
        public int ProductionWorkers { get; set; }
        public int CalculatedTotalWorkers { get; set; }  // 프리셋 적용된 총 개수
        public DateTime LastUpdated { get; set; }

        public bool HasChanged(WorkerStatistics other)
        {
            if (other == null) return true;
            
            return TotalWorkers != other.TotalWorkers ||
                   IdleWorkers != other.IdleWorkers ||
                   ProductionWorkers != other.ProductionWorkers ||
                   CalculatedTotalWorkers != other.CalculatedTotalWorkers;
        }

        public bool HasTotalCountChanged(WorkerStatistics other)
        {
            if (other == null) return true;
            return CalculatedTotalWorkers != other.CalculatedTotalWorkers;
        }

        public bool HasIdleCountChanged(WorkerStatistics other)
        {
            if (other == null) return true;
            return IdleWorkers != other.IdleWorkers;
        }

        public bool IsProductionCompleted(WorkerStatistics other)
        {
            if (other == null) return false;
            return (TotalWorkers == other.TotalWorkers &&
                   ProductionWorkers < other.ProductionWorkers) ||
                     (TotalWorkers > other.TotalWorkers &&
                     ProductionWorkers == other.ProductionWorkers && ProductionWorkers > 0);
        }

        public bool IsWorkerDied(WorkerStatistics other)
        {
            if (other == null) return false;
            return TotalWorkers < other.TotalWorkers && 
                   !(ProductionWorkers < other.ProductionWorkers);
        }

        // 새로운 이벤트 감지 메서드들
        public bool IsSimpleIncrease(WorkerStatistics other)
        {
            if (other == null) return false;
            return TotalWorkers > other.TotalWorkers && 
                   ProductionWorkers == other.ProductionWorkers;
        }

        public bool IsProductionStarted(WorkerStatistics other)
        {
            if (other == null) return false;
            return TotalWorkers > other.TotalWorkers && 
                   ProductionWorkers > other.ProductionWorkers;
        }

        public bool IsProductionCanceled(WorkerStatistics other)
        {
            if (other == null) return false;
            return TotalWorkers < other.TotalWorkers && 
                   ProductionWorkers < other.ProductionWorkers;
        }

        public WorkerStatistics Clone()
        {
            return new WorkerStatistics
            {
                TotalWorkers = TotalWorkers,
                IdleWorkers = IdleWorkers,
                ProductionWorkers = ProductionWorkers,
                CalculatedTotalWorkers = CalculatedTotalWorkers,
                LastUpdated = LastUpdated
            };
        }
    }

    public class GasBuildingState
    {
        public int UnitId { get; set; }
        public ActionIndex ActionIndex { get; set; }
        public byte GatheringState { get; set; }
        public DateTime StateStartTime { get; set; }
        public DateTime LastChecked { get; set; }

        public TimeSpan StateDuration => DateTime.Now - StateStartTime;
        
        public bool ShouldTriggerAlert()
        {
            // 건물이 완성되어 Idle 상태이면서 가스 채취가 중단된 상태가 500ms 이상 지속
            return ActionIndex == ActionIndex.BuildingIdle && 
                   GatheringState == 0 && 
                   StateDuration >= TimeSpan.FromMilliseconds(500);
        }

        public void UpdateState(ActionIndex newActionIndex, byte newGatheringState)
        {
            if (ActionIndex != newActionIndex || GatheringState != newGatheringState)
            {
                ActionIndex = newActionIndex;
                GatheringState = newGatheringState;
                StateStartTime = DateTime.Now;
            }
            LastChecked = DateTime.Now;
        }
    }
}