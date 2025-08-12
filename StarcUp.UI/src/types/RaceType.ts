// Core의 RaceType과 동기화된 enum (byte 값)
export enum RaceType {
  Zerg = 0,    // 저그
  Terran = 1,  // 테란
  Protoss = 2  // 프로토스
}

// Race 이름 매핑
export const RACE_NAMES = {
  [RaceType.Protoss]: '프로토스',
  [RaceType.Terran]: '테란',
  [RaceType.Zerg]: '저그'
} as const;