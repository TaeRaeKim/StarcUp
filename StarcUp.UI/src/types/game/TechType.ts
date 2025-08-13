// Core의 TechType과 동기화된 enum
export enum TechType {
  // Terran
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
  
  // Zerg
  Burrowing = 11,
  Infestation = 12,
  Spawn_Broodlings = 13,
  Dark_Swarm = 14,
  Plague = 15,
  Consume = 16,
  Ensnare = 17,
  Parasite = 18,
  Lurker_Aspect = 32,
  
  // Protoss
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
  
  // Special
  Unused_26 = 26,
  Unused_33 = 33,
  Healing = 34,
  None = 44,
  Unknown = 46,
  MAX = 47
}

// 기술 이름 매핑 (한국어)
export const TECH_NAMES = {
  // Terran Techs
  [TechType.Stim_Packs]: '스팀 팩',
  [TechType.Lockdown]: '락다운',
  [TechType.EMP_Shockwave]: 'EMP 쇼크웨이브',
  [TechType.Spider_Mines]: '스파이더 마인',
  [TechType.Scanner_Sweep]: '스캐너 스윕',
  [TechType.Tank_Siege_Mode]: '공성 모드',
  [TechType.Defensive_Matrix]: '디펜시브 매트릭스',
  [TechType.Irradiate]: '이래디에이트',
  [TechType.Yamato_Gun]: '야마토 건',
  [TechType.Cloaking_Field]: '클로킹 필드',
  [TechType.Personnel_Cloaking]: '퍼스널 클로킹',
  [TechType.Restoration]: '리스토레이션',
  [TechType.Optical_Flare]: '옵티컬 플레어',
  [TechType.Nuclear_Strike]: '핵 공격',
  
  // Zerg Techs
  [TechType.Burrowing]: '버로우',
  [TechType.Infestation]: '인페스테이션',
  [TechType.Spawn_Broodlings]: '브루들링 소환',
  [TechType.Dark_Swarm]: '다크 스웜',
  [TechType.Plague]: '플레이그',
  [TechType.Consume]: '컨슘',
  [TechType.Ensnare]: '인스네어',
  [TechType.Parasite]: '패러사이트',
  [TechType.Lurker_Aspect]: '러커',
  
  // Protoss Techs
  [TechType.Psionic_Storm]: '사이오닉 스톰',
  [TechType.Hallucination]: '할루시네이션',
  [TechType.Recall]: '리콜',
  [TechType.Stasis_Field]: '스테이시스 필드',
  [TechType.Archon_Warp]: '아콘 융합',
  [TechType.Disruption_Web]: '디스럽션 웹',
  [TechType.Mind_Control]: '마인드 컨트롤',
  [TechType.Dark_Archon_Meld]: '다크 아콘 융합',
  [TechType.Feedback]: '피드백',
  [TechType.Maelstrom]: '메일스트롬',
  
  // Special
  [TechType.Unused_26]: '미사용 26',
  [TechType.Unused_33]: '미사용 33',
  [TechType.Healing]: '치료',
  [TechType.None]: '없음',
  [TechType.Unknown]: '알 수 없음',
  [TechType.MAX]: '최대값'
} as const;