using System;
using System.Collections.Generic;
using StarcUp.Business.Profile.Models;
using StarcUp.Business.Units.Runtime.Models;
using StarcUp.Common.Events;

namespace StarcUp.Business.Profile
{
    public interface IWorkerManager : IDisposable
    {
        // 5가지 이벤트
        event EventHandler<WorkerEventArgs> TotalCountChanged;      // 총 일꾼 개수 변경
        event EventHandler<WorkerEventArgs> ProductionCompleted;    // 생산 완료
        event EventHandler<WorkerEventArgs> WorkerDied;            // 일꾼 사망
        event EventHandler<WorkerEventArgs> IdleCountChanged;      // 유휴 일꾼 개수 변경
        event EventHandler<GasBuildingEventArgs> GasBuildingAlert; // 가스 건물 알림

        // 속성
        WorkerPresetEnum WorkerPreset { get; set; }
        WorkerStatistics CurrentStatistics { get; }
        int LocalPlayerId { get; }
        
        // 메서드
        void Initialize(int localPlayerId);
        void UpdateWorkerData(IEnumerable<Unit> units);
        void UpdateGasBuildings(IEnumerable<Unit> gasBuildings);
        
        // 프리셋 관리
        void SetWorkerState(WorkerPresetEnum state);
        void ClearWorkerState(WorkerPresetEnum state);
        bool IsWorkerStateSet(WorkerPresetEnum state);
        
        // 통계 메서드
        double GetWorkerEfficiency();
        TimeSpan GetAverageIdleTime();
        int GetCalculatedTotalWorkers(); // 프리셋 적용된 총 개수
    }
}