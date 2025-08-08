// Core의 RaceType과 동기화된 enum (byte 값)
export enum RaceType {
  Zerg = 0,    // 저그
  Terran = 1,  // 테란
  Protoss = 2  // 프로토스
}

// 자주 사용되는 UnitType 목록 (Core의 UnitType enum과 동기화)
export enum UnitType {
  // Terran Units
  TerranMarine = 0,
  TerranGhost = 1,
  TerranVulture = 2,
  TerranGoliath = 3,
  TerranGoliathTurret = 4,
  TerranSiegeTankTankMode = 5,
  TerranSiegeTankTankModeTurret = 6,
  TerranSCV = 7,
  TerranWraith = 8,
  TerranScienceVessel = 9,
  
  // Terran Buildings (106-)
  TerranCommandCenter = 106,
  TerranComsat = 107,
  TerranNuclearSilo = 108,
  TerranSupplyDepot = 109,
  TerranRefinery = 110,
  TerranBarracks = 111,
  TerranAcademy = 112,
  TerranFactory = 113,
  TerranStarport = 114,
  TerranControlTower = 115,
  TerranScienceFacility = 116,
  TerranCovertOps = 117,
  TerranPhysicsLab = 118,
  TerranMachineShop = 119,
  TerranEngineeringBay = 120,
  TerranArmory = 121,
  TerranMissileTurret = 122,
  TerranBunker = 123,
  
  // Protoss Buildings (154-)
  ProtossNexus = 154,
  ProtossRoboticsFacility = 155,
  ProtossPylon = 156,
  ProtossAssimilator = 157,
  ProtossObservatory = 159,
  ProtossGateway = 160,
  ProtossPhotonCannon = 162,
  ProtossCitadelOfAdun = 163,
  ProtossCyberneticsCore = 164,
  ProtossTemplarArchives = 165,
  ProtossForge = 166,
  ProtossStargate = 167,
  ProtossFleetBeacon = 169,
  ProtossArbiterTribunal = 170,
  ProtossRoboticsSupportBay = 171,
  ProtossShieldBattery = 172,
  
  // Zerg Buildings (131-)
  ZergHatchery = 131,
  ZergLair = 132,
  ZergHive = 133,
  ZergNydusCanal = 134,
  ZergHydralisknDen = 135,
  ZergDefilerMound = 136,
  ZergGreaterSpire = 137,
  ZergQueensNest = 138,
  ZergEvolutionChamber = 139,
  ZergUltraliskCavern = 140,
  ZergSpire = 141,
  ZergSpawningPool = 142,
  ZergCreepColony = 143,
  ZergSporeColony = 144,
  ZergSunkenColony = 146,
  ZergExtractor = 149,
}

// Race별 건물 매핑
export const RACE_BUILDINGS = {
  [RaceType.Protoss]: {
    gateway: UnitType.ProtossGateway,
    robotics: UnitType.ProtossRoboticsFacility,
    stargate: UnitType.ProtossStargate,
    forge: UnitType.ProtossForge,
    citadel: UnitType.ProtossCitadelOfAdun,
    cybernetics: UnitType.ProtossCyberneticsCore,
    templar: UnitType.ProtossTemplarArchives,
    observatory: UnitType.ProtossObservatory,
    fleet: UnitType.ProtossFleetBeacon,
    arbiter: UnitType.ProtossArbiterTribunal,
    support: UnitType.ProtossRoboticsSupportBay
  },
  [RaceType.Terran]: {
    barracks: UnitType.TerranBarracks,
    factory: UnitType.TerranFactory,
    starport: UnitType.TerranStarport,
    academy: UnitType.TerranAcademy,
    armory: UnitType.TerranArmory,
    engineering: UnitType.TerranEngineeringBay,
    science: UnitType.TerranScienceFacility,
    covert: UnitType.TerranCovertOps,
    physics: UnitType.TerranPhysicsLab,
    machine: UnitType.TerranMachineShop,
    control: UnitType.TerranControlTower
  },
  [RaceType.Zerg]: {
    hatchery: UnitType.ZergHatchery,
    lair: UnitType.ZergLair,
    hive: UnitType.ZergHive,
    spawning: UnitType.ZergSpawningPool,
    hydralisk: UnitType.ZergHydralisknDen,
    spire: UnitType.ZergSpire,
    greater: UnitType.ZergGreaterSpire,
    queens: UnitType.ZergQueensNest,
    evolution: UnitType.ZergEvolutionChamber,
    ultralisk: UnitType.ZergUltraliskCavern,
    defiler: UnitType.ZergDefilerMound
  }
} as const;

// 건물 이름 매핑 (한국어)
export const UNIT_NAMES = {
  // Protoss Buildings
  [UnitType.ProtossGateway]: '게이트웨이',
  [UnitType.ProtossRoboticsFacility]: '로보틱스',
  [UnitType.ProtossStargate]: '스타게이트',
  [UnitType.ProtossForge]: '포지',
  [UnitType.ProtossCitadelOfAdun]: '시타델',
  [UnitType.ProtossCyberneticsCore]: '사이버네틱스 코어',
  [UnitType.ProtossTemplarArchives]: '템플러 아카이브',
  [UnitType.ProtossObservatory]: '옵저버토리',
  [UnitType.ProtossFleetBeacon]: '플릿 비컨',
  [UnitType.ProtossArbiterTribunal]: '아비터 트리뷰날',
  [UnitType.ProtossRoboticsSupportBay]: '로보틱스 서포트 베이',
  
  // Terran Buildings  
  [UnitType.TerranBarracks]: '배럭',
  [UnitType.TerranFactory]: '팩토리',
  [UnitType.TerranStarport]: '스타포트',
  [UnitType.TerranAcademy]: '아카데미',
  [UnitType.TerranArmory]: '아머리',
  [UnitType.TerranEngineeringBay]: '엔지니어링 베이',
  [UnitType.TerranScienceFacility]: '사이언스 퍼실리티',
  [UnitType.TerranCovertOps]: '커버트 옵스',
  [UnitType.TerranPhysicsLab]: '피직스 랩',
  [UnitType.TerranMachineShop]: '머신 샵',
  [UnitType.TerranControlTower]: '컨트롤 타워',
  
  // Zerg Buildings
  [UnitType.ZergHatchery]: '해처리',
  [UnitType.ZergLair]: '레어',
  [UnitType.ZergHive]: '하이브',
  [UnitType.ZergSpawningPool]: '스포닝 풀',
  [UnitType.ZergHydralisknDen]: '히드라리스크 덴',
  [UnitType.ZergSpire]: '스파이어',
  [UnitType.ZergGreaterSpire]: '그레이터 스파이어',
  [UnitType.ZergQueensNest]: '퀸즈 네스트',
  [UnitType.ZergEvolutionChamber]: '에볼루션 챔버',
  [UnitType.ZergUltraliskCavern]: '울트라리스크 캐번',
  [UnitType.ZergDefilerMound]: '디파일러 마운드'
} as const;

// Race 이름 매핑
export const RACE_NAMES = {
  [RaceType.Protoss]: '프로토스',
  [RaceType.Terran]: '테란',
  [RaceType.Zerg]: '저그'
} as const;