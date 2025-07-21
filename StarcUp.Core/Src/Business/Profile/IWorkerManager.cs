using System;
using System.Collections.Generic;
using StarcUp.Business.Profile.Models;
using StarcUp.Business.Units.Runtime.Models;
using StarcUp.Common.Events;

namespace StarcUp.Business.Profile
{
    public interface IWorkerManager : IDisposable
    {
        // 워커 관련 이벤트들
        event EventHandler<WorkerEventArgs> WorkerStatusChanged;   // 일반적인 일꾼 상태 변경
        event EventHandler<WorkerEventArgs> ProductionStarted;     // 일꾼 생산 시작
        event EventHandler<WorkerEventArgs> ProductionCompleted;   // 일꾼 생산 완료
        event EventHandler<WorkerEventArgs> ProductionCanceled;    // 일꾼 생산 취소
        event EventHandler<WorkerEventArgs> WorkerDied;           // 일꾼 사망
        event EventHandler<WorkerEventArgs> IdleCountChanged;     // 유휴 일꾼 개수 변경
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
        bool IsWorkerStateSet(WorkerPresetEnum state);

        // 프리셋 초기화 및 업데이트
        void InitializeWorkerPreset(WorkerPresetEnum preset);
        WorkerPresetEnum UpdateWorkerPreset(WorkerPresetEnum newPreset);
    }
}