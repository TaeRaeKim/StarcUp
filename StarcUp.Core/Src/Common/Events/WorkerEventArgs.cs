using System;
using StarcUp.Business.Units.Types;

namespace StarcUp.Common.Events
{
    public class WorkerEventArgs : EventArgs
    {
        public int PlayerId { get; set; }
        public int TotalWorkers { get; set; }
        public int PreviousTotalWorkers { get; set; }
        public int CalculatedTotalWorkers { get; set; }      // 프리셋 적용된 총 일꾼 수
        public int PreviousCalculatedWorkers { get; set; }   // 이전 계산된 총 일꾼 수
        public int IdleWorkers { get; set; }
        public int PreviousIdleWorkers { get; set; }
        public int ProductionWorkers { get; set; }
        public int PreviousProductionWorkers { get; set; }
        public int ActiveWorkers { get; set; }
        public DateTime Timestamp { get; set; }
        public WorkerEventType EventType { get; set; }
    }

    public class GasBuildingEventArgs : EventArgs
    {
        public int PlayerId { get; set; }
        public int GasBuildingUnitId { get; set; }
        public ActionIndex ActionIndex { get; set; }
        public byte GatheringState { get; set; }
        public TimeSpan Duration { get; set; }              // 상태 지속 시간
        public DateTime Timestamp { get; set; }
    }

    public enum WorkerEventType
    {
        WorkerStatusChanged,  // 일반적인 일꾼 상태 변경 (단순 증가, 프리셋 Off 시 사용)
        ProductionStarted,    // 일꾼 생산 시작
        ProductionCompleted,  // 일꾼 생산 완료
        ProductionCanceled,   // 일꾼 생산 취소
        WorkerDied,          // 일꾼 사망
        IdleCountChanged,    // 유휴 일꾼 개수 변경
        GasBuildingAlert     // 가스 건물 채취 중단 알림
    }
}