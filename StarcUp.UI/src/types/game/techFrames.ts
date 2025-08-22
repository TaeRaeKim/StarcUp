import { TechType } from './TechType';

// 테크별 소요 프레임 수
export const techFrames: Record<TechType, number> = {
  // Terran Technologies
  [TechType.Stim_Packs]: 1200,
  [TechType.Lockdown]: 1500,
  [TechType.EMP_Shockwave]: 1800,
  [TechType.Spider_Mines]: 1200,
  [TechType.Scanner_Sweep]: 0,              // 즉시 사용 기술
  [TechType.Tank_Siege_Mode]: 1200,
  [TechType.Defensive_Matrix]: 0,           // 즉시 사용 기술
  [TechType.Irradiate]: 1200,
  [TechType.Yamato_Gun]: 1800,
  [TechType.Cloaking_Field]: 1500,
  [TechType.Personnel_Cloaking]: 1200,
  [TechType.Restoration]: 1200,
  [TechType.Optical_Flare]: 1800,
  [TechType.Nuclear_Strike]: 0,             // 즉시 사용 기술

  // Zerg Technologies
  [TechType.Burrowing]: 1200,
  [TechType.Infestation]: 0,                // 특수 기술
  [TechType.Spawn_Broodlings]: 1200,
  [TechType.Dark_Swarm]: 0,                 // 즉시 사용 기술
  [TechType.Plague]: 1500,
  [TechType.Consume]: 1500,
  [TechType.Ensnare]: 1200,
  [TechType.Parasite]: 1200,
  [TechType.Lurker_Aspect]: 1800,

  // Protoss Technologies
  [TechType.Psionic_Storm]: 1800,
  [TechType.Hallucination]: 1200,
  [TechType.Recall]: 1800,
  [TechType.Stasis_Field]: 1500,
  [TechType.Archon_Warp]: 0,                // 즉시 사용 기술
  [TechType.Disruption_Web]: 1200,
  [TechType.Mind_Control]: 1800,
  [TechType.Dark_Archon_Meld]: 0,           // 즉시 사용 기술
  [TechType.Feedback]: 0,                   // 즉시 사용 기술
  [TechType.Maelstrom]: 1500,

  // Special/Unused
  [TechType.Unused_26]: 0,
  [TechType.Unused_33]: 0,
  [TechType.Healing]: 0,                    // 즉시 사용 기술
  [TechType.None]: 0,
  [TechType.Unknown]: 0,
  [TechType.MAX]: 0,
};

// 테크 정보 인터페이스
export interface TechInfo {
  frames: number;
  maxLevel: number;  // 테크는 항상 1레벨
}

// 테크 정보 가져오기 헬퍼 함수
export function getTechFrameInfo(techType: TechType): TechInfo {
  return {
    frames: techFrames[techType] || 0,
    maxLevel: 1  // 모든 테크는 1레벨
  };
}

// 테크 프레임 수 가져오기
export function getTechFrames(techType: TechType): number {
  return techFrames[techType] || 0;
}