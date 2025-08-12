using System;

namespace StarcUp.Business.Units.Types
{
    /// <summary>
    /// TechType enum에 대한 확장 메서드
    /// </summary>
    public static class TechTypeExtensions
    {
        /// <summary>
        /// 기술이 유효한지 확인
        /// </summary>
        public static bool IsValid(this TechType techType)
        {
            return techType != TechType.None && 
                   techType != TechType.Unknown && 
                   techType != TechType.Unused_26 &&
                   techType != TechType.Unused_33 &&
                   techType < TechType.MAX;
        }

        /// <summary>
        /// 기술이 능력(Ability)인지 확인
        /// </summary>
        public static bool IsAbility(this TechType techType)
        {
            return techType == TechType.Stim_Packs ||
                   techType == TechType.Lockdown ||
                   techType == TechType.EMP_Shockwave ||
                   techType == TechType.Scanner_Sweep ||
                   techType == TechType.Defensive_Matrix ||
                   techType == TechType.Irradiate ||
                   techType == TechType.Yamato_Gun ||
                   techType == TechType.Cloaking_Field ||
                   techType == TechType.Personnel_Cloaking ||
                   techType == TechType.Restoration ||
                   techType == TechType.Optical_Flare ||
                   techType == TechType.Nuclear_Strike ||
                   techType == TechType.Spawn_Broodlings ||
                   techType == TechType.Dark_Swarm ||
                   techType == TechType.Plague ||
                   techType == TechType.Consume ||
                   techType == TechType.Ensnare ||
                   techType == TechType.Parasite ||
                   techType == TechType.Psionic_Storm ||
                   techType == TechType.Hallucination ||
                   techType == TechType.Recall ||
                   techType == TechType.Stasis_Field ||
                   techType == TechType.Disruption_Web ||
                   techType == TechType.Mind_Control ||
                   techType == TechType.Feedback ||
                   techType == TechType.Maelstrom ||
                   techType == TechType.Healing;
        }

        /// <summary>
        /// 기술이 변태/변형 관련인지 확인
        /// </summary>
        public static bool IsMorph(this TechType techType)
        {
            return techType == TechType.Tank_Siege_Mode ||
                   techType == TechType.Lurker_Aspect ||
                   techType == TechType.Archon_Warp ||
                   techType == TechType.Dark_Archon_Meld;
        }

        /// <summary>
        /// 기술이 패시브 능력인지 확인
        /// </summary>
        public static bool IsPassive(this TechType techType)
        {
            return techType == TechType.Spider_Mines ||
                   techType == TechType.Burrowing ||
                   techType == TechType.Infestation;
        }

        /// <summary>
        /// 기술이 공격 능력인지 확인
        /// </summary>
        public static bool IsOffensive(this TechType techType)
        {
            return techType == TechType.Lockdown ||
                   techType == TechType.EMP_Shockwave ||
                   techType == TechType.Irradiate ||
                   techType == TechType.Yamato_Gun ||
                   techType == TechType.Optical_Flare ||
                   techType == TechType.Nuclear_Strike ||
                   techType == TechType.Spawn_Broodlings ||
                   techType == TechType.Plague ||
                   techType == TechType.Ensnare ||
                   techType == TechType.Parasite ||
                   techType == TechType.Psionic_Storm ||
                   techType == TechType.Stasis_Field ||
                   techType == TechType.Mind_Control ||
                   techType == TechType.Feedback ||
                   techType == TechType.Maelstrom ||
                   techType == TechType.Disruption_Web;
        }

        /// <summary>
        /// 기술이 방어/지원 능력인지 확인
        /// </summary>
        public static bool IsDefensive(this TechType techType)
        {
            return techType == TechType.Defensive_Matrix ||
                   techType == TechType.Restoration ||
                   techType == TechType.Dark_Swarm ||
                   techType == TechType.Hallucination ||
                   techType == TechType.Recall ||
                   techType == TechType.Healing;
        }

        /// <summary>
        /// 기술이 속한 종족 가져오기
        /// </summary>
        public static RaceType GetRace(this TechType techType)
        {
            // Terran technologies
            if (techType == TechType.Stim_Packs ||
                techType == TechType.Lockdown ||
                techType == TechType.EMP_Shockwave ||
                techType == TechType.Spider_Mines ||
                techType == TechType.Scanner_Sweep ||
                techType == TechType.Tank_Siege_Mode ||
                techType == TechType.Defensive_Matrix ||
                techType == TechType.Irradiate ||
                techType == TechType.Yamato_Gun ||
                techType == TechType.Cloaking_Field ||
                techType == TechType.Personnel_Cloaking ||
                techType == TechType.Restoration ||
                techType == TechType.Optical_Flare ||
                techType == TechType.Nuclear_Strike)
            {
                return RaceType.Terran;
            }

            // Zerg technologies
            if (techType == TechType.Burrowing ||
                techType == TechType.Infestation ||
                techType == TechType.Spawn_Broodlings ||
                techType == TechType.Dark_Swarm ||
                techType == TechType.Plague ||
                techType == TechType.Consume ||
                techType == TechType.Ensnare ||
                techType == TechType.Parasite ||
                techType == TechType.Lurker_Aspect)
            {
                return RaceType.Zerg;
            }

            // Protoss technologies
            if (techType == TechType.Psionic_Storm ||
                techType == TechType.Hallucination ||
                techType == TechType.Recall ||
                techType == TechType.Stasis_Field ||
                techType == TechType.Archon_Warp ||
                techType == TechType.Disruption_Web ||
                techType == TechType.Mind_Control ||
                techType == TechType.Dark_Archon_Meld ||
                techType == TechType.Feedback ||
                techType == TechType.Maelstrom)
            {
                return RaceType.Protoss;
            }

            throw new ArgumentException($"Unknown race for tech: {techType}");
        }

        /// <summary>
        /// 기술의 한국어 이름 가져오기
        /// </summary>
        public static string GetKoreanName(this TechType techType)
        {
            return techType switch
            {
                // Terran
                TechType.Stim_Packs => "스팀 팩",
                TechType.Lockdown => "락다운",
                TechType.EMP_Shockwave => "EMP 쇼크웨이브",
                TechType.Spider_Mines => "스파이더 마인",
                TechType.Scanner_Sweep => "스캐너 스윕",
                TechType.Tank_Siege_Mode => "시즈 모드",
                TechType.Defensive_Matrix => "디펜시브 매트릭스",
                TechType.Irradiate => "이래디에이트",
                TechType.Yamato_Gun => "야마토 건",
                TechType.Cloaking_Field => "클로킹 필드",
                TechType.Personnel_Cloaking => "퍼스널 클로킹",
                TechType.Restoration => "리스토레이션",
                TechType.Optical_Flare => "옵티컬 플레어",
                TechType.Nuclear_Strike => "핵 공격",
                
                // Zerg
                TechType.Burrowing => "버로우",
                TechType.Infestation => "인페스테이션",
                TechType.Spawn_Broodlings => "브루들링 소환",
                TechType.Dark_Swarm => "다크 스웜",
                TechType.Plague => "플레이그",
                TechType.Consume => "컨슘",
                TechType.Ensnare => "인스네어",
                TechType.Parasite => "패러사이트",
                TechType.Lurker_Aspect => "러커 변태",
                
                // Protoss
                TechType.Psionic_Storm => "사이오닉 스톰",
                TechType.Hallucination => "할루시네이션",
                TechType.Recall => "리콜",
                TechType.Stasis_Field => "스테이시스 필드",
                TechType.Archon_Warp => "아콘 융합",
                TechType.Disruption_Web => "디스럽션 웹",
                TechType.Mind_Control => "마인드 컨트롤",
                TechType.Dark_Archon_Meld => "다크 아콘 융합",
                TechType.Feedback => "피드백",
                TechType.Maelstrom => "메일스트롬",
                
                // Special
                TechType.Healing => "치료",
                TechType.None => "없음",
                TechType.Unknown => "알 수 없음",
                _ => techType.ToString()
            };
        }

        /// <summary>
        /// 기술 연구에 필요한 건물 타입 가져오기
        /// </summary>
        public static UnitType? GetRequiredBuilding(this TechType techType)
        {
            return techType switch
            {
                // Terran
                TechType.Stim_Packs => UnitType.TerranAcademy,
                TechType.Lockdown => UnitType.TerranCovertOps,
                TechType.EMP_Shockwave => UnitType.TerranScienceFacility,
                TechType.Spider_Mines => UnitType.TerranMachineShop,
                TechType.Tank_Siege_Mode => UnitType.TerranMachineShop,
                TechType.Irradiate => UnitType.TerranScienceFacility,
                TechType.Yamato_Gun => UnitType.TerranPhysicsLab,
                TechType.Cloaking_Field => UnitType.TerranControlTower,
                TechType.Personnel_Cloaking => UnitType.TerranCovertOps,
                TechType.Restoration => UnitType.TerranAcademy,
                TechType.Optical_Flare => UnitType.TerranAcademy,
                
                // Zerg
                TechType.Burrowing => UnitType.ZergHatchery,
                TechType.Spawn_Broodlings => UnitType.ZergQueensNest,
                TechType.Plague => UnitType.ZergDefilerMound,
                TechType.Consume => UnitType.ZergDefilerMound,
                TechType.Ensnare => UnitType.ZergQueensNest,
                TechType.Lurker_Aspect => UnitType.ZergHydraliskDen,
                
                // Protoss
                TechType.Psionic_Storm => UnitType.ProtossTemplarArchives,
                TechType.Hallucination => UnitType.ProtossTemplarArchives,
                TechType.Recall => UnitType.ProtossArbiterTribunal,
                TechType.Stasis_Field => UnitType.ProtossArbiterTribunal,
                TechType.Disruption_Web => UnitType.ProtossFleetBeacon,
                TechType.Mind_Control => UnitType.ProtossTemplarArchives,
                TechType.Maelstrom => UnitType.ProtossTemplarArchives,
                
                _ => null
            };
        }
    }
}