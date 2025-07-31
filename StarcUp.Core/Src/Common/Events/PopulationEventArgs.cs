using System;
using StarcUp.Business.Profile.Models;

namespace StarcUp.Common.Events
{
    /// <summary>
    /// 인구수 관련 이벤트 인자
    /// </summary>
    public class PopulationEventArgs : EventArgs
    {
        public PopulationEventType EventType { get; set; }
        public PopulationStatistics Current { get; set; }
        public PopulationStatistics Previous { get; set; }
        public DateTime Timestamp { get; set; }
        public byte PlayerIndex { get; set; }
        
        // 경고 관련 추가 정보
        public int ThresholdValue { get; set; }      // 경고 기준값 (UI 기준)
        public int ActualAvailable { get; set; }     // 실제 사용 가능한 인구수 (UI 기준)
        public string AlertMessage { get; set; } = "";

        public PopulationEventArgs(
            PopulationEventType eventType, 
            PopulationStatistics current, 
            PopulationStatistics previous, 
            byte playerIndex)
        {
            EventType = eventType;
            Current = current;
            Previous = previous;
            PlayerIndex = playerIndex;
            Timestamp = DateTime.Now;
            
            // UI 표시용 값 계산 (Raw 값 ÷ 2)
            ActualAvailable = current?.RawAvailableSupply / 2 ?? 0;
        }
    }

    /// <summary>
    /// 인구수 이벤트 타입
    /// </summary>
    public enum PopulationEventType
    {
        /// <summary>
        /// 인구 부족 경고 (설정된 기준값에 근접 또는 막힘)
        /// </summary>
        SupplyAlert
    }
}