namespace StarcUp.Common.Constants
{
    /// <summary>
    /// 게임 관련 상수들
    /// </summary>
    public static class GameConstants
    {
        // 프로세스 이름들
        public static readonly string[] STARCRAFT_PROCESS_NAMES = {
            "StarCraft",
            "StarCraft_BW",
            "StarCraft Remastered"
        };

        // 윈도우 감지 관련
        public const int GAME_CHECK_INTERVAL_MS = 1000;
        public const int POINTER_MONITOR_INTERVAL_MS = 100;

        // 오버레이 위치 관련
        public const int DEFAULT_FULLSCREEN_OFFSET_X = 15;
        public const int DEFAULT_FULLSCREEN_OFFSET_Y = 10;
        public const int DEFAULT_WINDOW_OFFSET_X = 20;
        public const int DEFAULT_WINDOW_OFFSET_Y = 40;

        // 메모리 관련
        public const int STACK_SEARCH_SIZE = 4096;
        public const int POINTER_SIZE_64BIT = 8;

        // 이벤트 타입들
        public static class EventTypes
        {
            public const string GAME_FOUND = "GameFound";
            public const string GAME_LOST = "GameLost";
            public const string WINDOW_CHANGED = "WindowChanged";
            public const string WINDOW_ACTIVATED = "WindowActivated";
            public const string WINDOW_DEACTIVATED = "WindowDeactivated";
            public const string POINTER_VALUE_CHANGED = "PointerValueChanged";
        }
    }
}