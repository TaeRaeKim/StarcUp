namespace StarcUp.Business.Units.Types.Extensions
{
    /// <summary>
    /// TechType 확장 메서드
    /// </summary>
    public static class TechTypeExtensions
    {
        /// <summary>
        /// 테크의 소요 프레임 수를 가져옵니다.
        /// </summary>
        public static int GetFrames(this TechType techType)
        {
            if (!System.Enum.IsDefined(typeof(TechFrames), techType.ToString()))
                return 0;

            return (int)(TechFrames)System.Enum.Parse(typeof(TechFrames), techType.ToString());
        }

        /// <summary>
        /// 즉시 사용 가능한 테크인지 확인합니다.
        /// </summary>
        public static bool IsInstantTech(this TechType techType)
        {
            return GetFrames(techType) == 0;
        }
    }
}