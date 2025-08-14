namespace StarcUp.Business.Units.Types
{
    /// <summary>
    /// 테크별 소요 프레임 수
    /// </summary>
    public enum TechFrames : ushort
    {
        // Terran Technologies
        Stim_Packs = 1200,
        Lockdown = 1500,
        EMP_Shockwave = 1800,
        Spider_Mines = 1200,
        Scanner_Sweep = 0,              // 즉시 사용 기술
        Tank_Siege_Mode = 1200,
        Defensive_Matrix = 0,           // 즉시 사용 기술
        Irradiate = 1200,
        Yamato_Gun = 1800,
        Cloaking_Field = 1500,
        Personnel_Cloaking = 1200,
        Restoration = 1200,
        Optical_Flare = 1800,
        Nuclear_Strike = 0,             // 즉시 사용 기술

        // Zerg Technologies
        Burrowing = 1200,
        Infestation = 0,                // 특수 기술
        Spawn_Broodlings = 1200,
        Dark_Swarm = 0,                 // 즉시 사용 기술
        Plague = 1500,
        Consume = 1500,
        Ensnare = 1200,
        Parasite = 0,                   // 즉시 사용 기술
        Lurker_Aspect = 1800,

        // Protoss Technologies
        Psionic_Storm = 1800,
        Hallucination = 1200,
        Recall = 1800,
        Stasis_Field = 1500,
        Archon_Warp = 0,                // 즉시 사용 기술
        Disruption_Web = 1200,
        Mind_Control = 1800,
        Dark_Archon_Meld = 0,           // 즉시 사용 기술
        Feedback = 0,                   // 즉시 사용 기술
        Maelstrom = 1500,

        // Special/Unused
        Unused_26 = 0,
        Unused_33 = 0,
        Healing = 0,                    // 즉시 사용 기술
        None = 0,
        Unknown = 0,
        MAX = 0
    }
}