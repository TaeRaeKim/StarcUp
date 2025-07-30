using System;
using StarcUp.Business.Profile.Models;
using StarcUp.Common.Events;
using StarcUp.Business.Units.Types;

namespace StarcUp.Business.Profile
{
    public interface IPopulationManager : IDisposable
    {
        // 인구수 경고 이벤트
        event EventHandler<PopulationEventArgs> SupplyAlert;

        // 속성
        PopulationSettings Settings { get; set; }
        PopulationStatistics CurrentStatistics { get; }
        int LocalPlayerId { get; }
        RaceType LocalPlayerRace { get; }

        // 메서드
        void Initialize(int localPlayerId, RaceType localPlayerRace);
        void UpdatePopulationData(); // 메모리에서 직접 읽어옴

        // 설정 관리
        void InitializePopulationSettings(PopulationSettings settings);
        PopulationSettings UpdatePopulationSettings(PopulationSettings newSettings);
        
        // 게임 시간 업데이트 (모드 A의 시간 제한 기능용)
        void UpdateGameTime(TimeSpan gameTime);
    }
}