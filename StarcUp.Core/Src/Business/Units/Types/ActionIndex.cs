namespace StarcUp.Business.Units.Types
{
    /// <summary>
    /// 스타크래프트 유닛의 액션 인덱스 열거형
    /// </summary>
    public enum ActionIndex : byte
    {
        // 기본 상태
        Idle = 3,                    // 유휴 상태
        Move = 6,                    // 이동
        Attack = 10,                 // 공격
        BuildingIdle = 23,                 // 빌딩 Idle 상태

        // 건물 건설 관련
        ConstructBuilding = 30,      // 건물 생성 명령 중 (테란-건설)
        WarpBuilding = 31,           // 건물 생성 명령 중 (토스-워프)
        Repair = 34,                 // 리페어

        // 건물 건설 중 상태
        ConstructingTerran = 44,     // 건물 건설 중 (테란)
        MorphingZerg = 45,           // 건물 변태 중 (저그)
        WarpingProtoss = 48,         // 건물 워프 중 (토스)

        // 저그 특수
        MorphBuilding = 70,          // 건물 생성 명령 중 (저그-변태)

        // 자원 채취 - 가스
        MovingToGas = 81,            // 가스 캐러 가는 중
        WaitingForGas = 82,          // 가스 채취 대기 중
        GatheringGas = 83,           // 가스 채취 중
        ReturningGas = 84,           // 가스 반환하러 가는 중

        // 자원 채취 - 미네랄
        MovingToMineral = 85,        // 미네랄 캐러 가는 중
        WaitingForMineral = 86,      // 미네랄 채취 대기 중
        GatheringMineral = 87,       // 미네랄 채취 중
        ReturningMineral = 90,       // 미네랄 반환하러 가는 중

        // 저그 특수 능력
        Burrowing = 116,             // 버로우 하는 중 (지상->지하)
        BurrowedIdle = 117,          // 버로우 상태에서 유휴
        Unburrowing = 118,           // 버로우 해제 중 (지하->지상)

        // 기타
        Patrol = 152                 // 패트롤
    }

    /// <summary>
    /// ActionIndex 확장 메서드
    /// </summary>
    public static class ActionIndexExtensions
    {
        /// <summary>
        /// 자원 채취 관련 액션인지 확인
        /// </summary>
        public static bool IsGathering(this ActionIndex actionIndex)
        {
            return actionIndex == ActionIndex.MovingToGas ||
                   actionIndex == ActionIndex.WaitingForGas ||
                   actionIndex == ActionIndex.GatheringGas ||
                   actionIndex == ActionIndex.ReturningGas ||
                   actionIndex == ActionIndex.MovingToMineral ||
                   actionIndex == ActionIndex.WaitingForMineral ||
                   actionIndex == ActionIndex.GatheringMineral ||
                   actionIndex == ActionIndex.ReturningMineral;
        }

        /// <summary>
        /// 가스 채취 관련 액션인지 확인
        /// </summary>
        public static bool IsGasGathering(this ActionIndex actionIndex)
        {
            return actionIndex == ActionIndex.MovingToGas ||
                   actionIndex == ActionIndex.WaitingForGas ||
                   actionIndex == ActionIndex.GatheringGas ||
                   actionIndex == ActionIndex.ReturningGas;
        }

        /// <summary>
        /// 미네랄 채취 관련 액션인지 확인
        /// </summary>
        public static bool IsMineralGathering(this ActionIndex actionIndex)
        {
            return actionIndex == ActionIndex.MovingToMineral ||
                   actionIndex == ActionIndex.WaitingForMineral ||
                   actionIndex == ActionIndex.GatheringMineral ||
                   actionIndex == ActionIndex.ReturningMineral;
        }

        /// <summary>
        /// 건물 건설 관련 액션인지 확인
        /// </summary>
        public static bool IsConstructing(this ActionIndex actionIndex)
        {
            return actionIndex == ActionIndex.ConstructBuilding ||
                   actionIndex == ActionIndex.WarpBuilding ||
                   actionIndex == ActionIndex.MorphBuilding ||
                   actionIndex == ActionIndex.ConstructingTerran ||
                   actionIndex == ActionIndex.MorphingZerg ||
                   actionIndex == ActionIndex.WarpingProtoss;
        }

        /// <summary>
        /// 유휴 상태인지 확인
        /// </summary>
        public static bool IsIdle(this ActionIndex actionIndex)
        {
            return actionIndex == ActionIndex.Idle || actionIndex == ActionIndex.BurrowedIdle;
        }

        /// <summary>
        /// 액션 이름 반환
        /// </summary>
        public static string GetActionName(this ActionIndex actionIndex)
        {
            return actionIndex switch
            {
                ActionIndex.Idle => "유휴",
                ActionIndex.Move => "이동",
                ActionIndex.Attack => "공격",
                ActionIndex.ConstructBuilding => "건설 명령",
                ActionIndex.WarpBuilding => "워프 명령",
                ActionIndex.Repair => "수리",
                ActionIndex.ConstructingTerran => "건설 중",
                ActionIndex.MorphingZerg => "변태 중",
                ActionIndex.WarpingProtoss => "워프 중",
                ActionIndex.MorphBuilding => "변태 명령",
                ActionIndex.MovingToGas => "가스로 이동",
                ActionIndex.WaitingForGas => "가스 대기",
                ActionIndex.GatheringGas => "가스 채취",
                ActionIndex.ReturningGas => "가스 반환",
                ActionIndex.MovingToMineral => "미네랄로 이동",
                ActionIndex.WaitingForMineral => "미네랄 대기",
                ActionIndex.GatheringMineral => "미네랄 채취",
                ActionIndex.ReturningMineral => "미네랄 반환",
                ActionIndex.Burrowing => "버로우 중",
                ActionIndex.BurrowedIdle => "버로우 상태",
                ActionIndex.Unburrowing => "언버로우 중",
                ActionIndex.Patrol => "패트롤",
                _ => $"알 수 없음({(byte)actionIndex})"
            };
        }
    }
}