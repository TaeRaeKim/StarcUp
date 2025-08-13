import { RaceType, UpgradeType, TechType } from '../../types/game';
import { BuildingInfo } from '../../types/preset';
import { BUILDING_ICONS } from '../../utils/buildingIcons';

// 프로토스 건물별 업그레이드/테크 매핑
export const PROTOSS_BUILDING_UPGRADES: Record<string, BuildingInfo> = {
  forge: {
    id: 'forge',
    name: '포지',
    iconPath: BUILDING_ICONS.forge,
    upgrades: [
      UpgradeType.Protoss_Ground_Weapons,
      UpgradeType.Protoss_Ground_Armor,
      UpgradeType.Protoss_Plasma_Shields
    ],
    techs: []
  },
  cyberneticsCore: {
    id: 'cyberneticsCore',
    name: '사이버네틱스 코어',
    iconPath: BUILDING_ICONS.cyberneticsCore,
    upgrades: [
      UpgradeType.Protoss_Air_Weapons,
      UpgradeType.Protoss_Air_Armor,
      UpgradeType.Singularity_Charge
    ],
    techs: []
  },
  citadelOfAdun: {
    id: 'citadelOfAdun',
    name: '시타델 오브 아둔',
    iconPath: BUILDING_ICONS.citadelOfAdun,
    upgrades: [
      UpgradeType.Leg_Enhancements
    ],
    techs: []
  },
  templarArchives: {
    id: 'templarArchives',
    name: '템플러 아카이브스',
    iconPath: BUILDING_ICONS.templarArchives,
    upgrades: [
      UpgradeType.Khaydarin_Amulet,
      UpgradeType.Argus_Talisman
    ],
    techs: [
      TechType.Psionic_Storm,
      TechType.Hallucination,
      TechType.Mind_Control,
      TechType.Maelstrom
    ]
  },
  roboticsSupportBay: {
    id: 'roboticsSupportBay',
    name: '로보틱스 서포트 베이',
    iconPath: BUILDING_ICONS.roboticsSupportBay,
    upgrades: [
      UpgradeType.Scarab_Damage,
      UpgradeType.Reaver_Capacity,
      UpgradeType.Gravitic_Drive
    ],
    techs: []
  },
  fleetBeacon: {
    id: 'fleetBeacon',
    name: '플릿 비콘',
    iconPath: BUILDING_ICONS.fleetBeacon,
    upgrades: [
      UpgradeType.Apial_Sensors,
      UpgradeType.Gravitic_Thrusters,
      UpgradeType.Carrier_Capacity,
      UpgradeType.Argus_Jewel
    ],
    techs: [
      TechType.Disruption_Web
    ]
  },
  observatory: {
    id: 'observatory',
    name: '옵저버토리',
    iconPath: BUILDING_ICONS.observatory,
    upgrades: [
      UpgradeType.Gravitic_Boosters,
      UpgradeType.Sensor_Array
    ],
    techs: []
  },
  arbiterTribunal: {
    id: 'arbiterTribunal',
    name: '아비터 트리뷰날',
    iconPath: BUILDING_ICONS.arbiterTribunal,
    upgrades: [
      UpgradeType.Khaydarin_Core
    ],
    techs: [
      TechType.Recall,
      TechType.Stasis_Field
    ]
  }
};

// 테란 건물별 업그레이드/테크 매핑
export const TERRAN_BUILDING_UPGRADES: Record<string, BuildingInfo> = {
  engineeringBay: {
    id: 'engineeringBay',
    name: '엔지니어링 베이',
    iconPath: BUILDING_ICONS.engineeringBay,
    upgrades: [
      UpgradeType.Terran_Infantry_Weapons,
      UpgradeType.Terran_Infantry_Armor
    ],
    techs: []
  },
  academy: {
    id: 'academy',
    name: '아카데미',
    iconPath: BUILDING_ICONS.academy,
    upgrades: [
      UpgradeType.U_238_Shells,
      UpgradeType.Caduceus_Reactor
    ],
    techs: [
      TechType.Stim_Packs,
      TechType.Restoration,
      TechType.Optical_Flare
    ]
  },
  machineShop: {
    id: 'machineShop',
    name: '머신 샵',
    iconPath: BUILDING_ICONS.machineShop,
    upgrades: [
      UpgradeType.Ion_Thrusters,
      UpgradeType.Charon_Boosters
    ],
    techs: [
      TechType.Spider_Mines,
      TechType.Tank_Siege_Mode
    ]
  },
  controlTower: {
    id: 'controlTower',
    name: '컨트롤 타워',
    iconPath: BUILDING_ICONS.controlTower,
    upgrades: [
      UpgradeType.Apollo_Reactor
    ],
    techs: [
      TechType.Cloaking_Field
    ]
  },
  scienceFacility: {
    id: 'scienceFacility',
    name: '사이언스 퍼실리티',
    iconPath: BUILDING_ICONS.scienceFacility,
    upgrades: [
      UpgradeType.Titan_Reactor
    ],
    techs: [
      TechType.EMP_Shockwave,
      TechType.Irradiate
    ]
  },
  covertOps: {
    id: 'covertOps',
    name: '코버트 옵스',
    iconPath: BUILDING_ICONS.covertOps,
    upgrades: [
      UpgradeType.Ocular_Implants,
      UpgradeType.Moebius_Reactor
    ],
    techs: [
      TechType.Lockdown,
      TechType.Personnel_Cloaking
    ]
  },
  physicsLab: {
    id: 'physicsLab',
    name: '피직스 랩',
    iconPath: BUILDING_ICONS.physicsLab,
    upgrades: [
      UpgradeType.Colossus_Reactor
    ],
    techs: [
      TechType.Yamato_Gun
    ]
  },
  armory: {
    id: 'armory',
    name: '아머리',
    iconPath: BUILDING_ICONS.armory,
    upgrades: [
      UpgradeType.Terran_Vehicle_Weapons,
      UpgradeType.Terran_Vehicle_Plating,
      UpgradeType.Terran_Ship_Weapons,
      UpgradeType.Terran_Ship_Plating
    ],
    techs: []
  }
};

// 저그 건물별 업그레이드/테크 매핑
export const ZERG_BUILDING_UPGRADES: Record<string, BuildingInfo> = {
  hatchery: {
    id: 'hatchery',
    name: '해처리',
    iconPath: BUILDING_ICONS.hatchery,
    upgrades: [],
    techs: [
      TechType.Burrowing
    ]
  },
  spawningPool: {
    id: 'spawningPool',
    name: '스포닝 풀',
    iconPath: BUILDING_ICONS.spawningPool,
    upgrades: [
      UpgradeType.Metabolic_Boost,
      UpgradeType.Adrenal_Glands
    ],
    techs: []
  },
  evolutionChamber: {
    id: 'evolutionChamber',
    name: '에볼루션 챔버',
    iconPath: BUILDING_ICONS.evolutionChamber,
    upgrades: [
      UpgradeType.Zerg_Melee_Attacks,
      UpgradeType.Zerg_Missile_Attacks,
      UpgradeType.Zerg_Carapace
    ],
    techs: []
  },
  hydraliskDen: {
    id: 'hydraliskDen',
    name: '히드라리스크 덴',
    iconPath: BUILDING_ICONS.hydraliskDen,
    upgrades: [
      UpgradeType.Muscular_Augments,
      UpgradeType.Grooved_Spines
    ],
    techs: [
      TechType.Lurker_Aspect
    ]
  },
  lair: {
    id: 'lair',
    name: '레어',
    iconPath: BUILDING_ICONS.lair,
    upgrades: [
      UpgradeType.Ventral_Sacs,
      UpgradeType.Antennae,
      UpgradeType.Pneumatized_Carapace
    ],
    techs: []
  },
  spire: {
    id: 'spire',
    name: '스파이어',
    iconPath: BUILDING_ICONS.spire,
    upgrades: [
      UpgradeType.Zerg_Flyer_Attacks,
      UpgradeType.Zerg_Flyer_Carapace
    ],
    techs: []
  },
  queensNest: {
    id: 'queensNest',
    name: '퀸즈 네스트',
    iconPath: BUILDING_ICONS.queensNest,
    upgrades: [
      UpgradeType.Gamete_Meiosis
    ],
    techs: [
      TechType.Spawn_Broodlings,
      TechType.Ensnare
    ]
  },
  ultraliskCavern: {
    id: 'ultraliskCavern',
    name: '울트라리스크 캐번',
    iconPath: BUILDING_ICONS.ultraliskCavern,
    upgrades: [
      UpgradeType.Anabolic_Synthesis,
      UpgradeType.Chitinous_Plating
    ],
    techs: []
  },
  defilerMound: {
    id: 'defilerMound',
    name: '디파일러 마운드',
    iconPath: BUILDING_ICONS.defilerMound,
    upgrades: [
      UpgradeType.Metasynaptic_Node
    ],
    techs: [
      TechType.Plague,
      TechType.Consume
    ]
  }
};

// 종족별 건물 매핑
export const RACE_BUILDING_UPGRADES = {
  [RaceType.Protoss]: PROTOSS_BUILDING_UPGRADES,
  [RaceType.Terran]: TERRAN_BUILDING_UPGRADES,
  [RaceType.Zerg]: ZERG_BUILDING_UPGRADES
};

// 종족별 건물 목록 가져오기
export const getBuildingsByRace = (race: RaceType): BuildingInfo[] => {
  return Object.values(RACE_BUILDING_UPGRADES[race]);
};

// 특정 건물의 모든 업그레이드/테크 가져오기
export const getUpgradesByBuilding = (race: RaceType, buildingId: string) => {
  const building = RACE_BUILDING_UPGRADES[race][buildingId];
  if (!building) return { upgrades: [], techs: [] };
  
  return {
    upgrades: building.upgrades,
    techs: building.techs
  };
};