namespace StarcUp.Business.Units.Types
{
    /// <summary>
    /// 스타크래프트 기술 타입
    /// </summary>
    public enum TechType : byte
    {
        // Terran Technologies
        Stim_Packs = 0,
        Lockdown = 1,
        EMP_Shockwave = 2,
        Spider_Mines = 3,
        Scanner_Sweep = 4,
        Tank_Siege_Mode = 5,
        Defensive_Matrix = 6,
        Irradiate = 7,
        Yamato_Gun = 8,
        Cloaking_Field = 9,
        Personnel_Cloaking = 10,
        Restoration = 24,
        Optical_Flare = 30,
        Nuclear_Strike = 45,

        // Zerg Technologies
        Burrowing = 11,
        Infestation = 12,
        Spawn_Broodlings = 13,
        Dark_Swarm = 14,
        Plague = 15,
        Consume = 16,
        Ensnare = 17,
        Parasite = 18,
        Lurker_Aspect = 32,

        // Protoss Technologies
        Psionic_Storm = 19,
        Hallucination = 20,
        Recall = 21,
        Stasis_Field = 22,
        Archon_Warp = 23,
        Disruption_Web = 25,
        Mind_Control = 27,
        Dark_Archon_Meld = 28,
        Feedback = 29,
        Maelstrom = 31,

        // Special/Unused
        Unused_26 = 26,
        Unused_33 = 33,
        Healing = 34,
        None = 44,
        Unknown = 46,
        MAX = 47
    }
}