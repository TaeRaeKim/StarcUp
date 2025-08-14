import { RaceType, UpgradeType, TechType } from '../../types/game';
import { UIBuildingInfo, UpgradeItem, UpgradeItemType } from '../../types/preset';
import { BUILDING_ICONS } from '../../utils/buildingIcons';

// 프로토스 건물별 업그레이드/테크 매핑 (순서대로 정렬)
export const PROTOSS_BUILDING_UPGRADES: Record<string, UIBuildingInfo> = {
  forge: {
    id: 'forge',
    name: '포지',
    iconPath: BUILDING_ICONS.forge,
    items: [
      { type: UpgradeItemType.Upgrade, value: UpgradeType.Protoss_Ground_Weapons },
      { type: UpgradeItemType.Upgrade, value: UpgradeType.Protoss_Ground_Armor },
      { type: UpgradeItemType.Upgrade, value: UpgradeType.Protoss_Plasma_Shields }
    ]
  },
  cyberneticsCore: {
    id: 'cyberneticsCore',
    name: '사이버네틱스 코어',
    iconPath: BUILDING_ICONS.cyberneticsCore,
    items: [
      { type: UpgradeItemType.Upgrade, value: UpgradeType.Protoss_Air_Weapons },
      { type: UpgradeItemType.Upgrade, value: UpgradeType.Protoss_Air_Armor },
      { type: UpgradeItemType.Upgrade, value: UpgradeType.Singularity_Charge }
    ]
  },
  citadelOfAdun: {
    id: 'citadelOfAdun',
    name: '시타델 오브 아둔',
    iconPath: BUILDING_ICONS.citadelOfAdun,
    items: [
      { type: UpgradeItemType.Upgrade, value: UpgradeType.Leg_Enhancements }
    ]
  },
  templarArchives: {
    id: 'templarArchives',
    name: '템플러 아카이브스',
    iconPath: BUILDING_ICONS.templarArchives,
    items: [
      { type: UpgradeItemType.Tech, value: TechType.Psionic_Storm },
      { type: UpgradeItemType.Tech, value: TechType.Hallucination },
      { type: UpgradeItemType.Upgrade, value: UpgradeType.Khaydarin_Amulet },
      { type: UpgradeItemType.Tech, value: TechType.Mind_Control },
      { type: UpgradeItemType.Tech, value: TechType.Maelstrom },
      { type: UpgradeItemType.Upgrade, value: UpgradeType.Argus_Talisman }
    ]
  },
  roboticsSupportBay: {
    id: 'roboticsSupportBay',
    name: '로보틱스 서포트 베이',
    iconPath: BUILDING_ICONS.roboticsSupportBay,
    items: [
      { type: UpgradeItemType.Upgrade, value: UpgradeType.Scarab_Damage },
      { type: UpgradeItemType.Upgrade, value: UpgradeType.Reaver_Capacity },
      { type: UpgradeItemType.Upgrade, value: UpgradeType.Gravitic_Drive }
    ]
  },
  fleetBeacon: {
    id: 'fleetBeacon',
    name: '플릿 비콘',
    iconPath: BUILDING_ICONS.fleetBeacon,
    items: [
      { type: UpgradeItemType.Upgrade, value: UpgradeType.Apial_Sensors },
      { type: UpgradeItemType.Upgrade, value: UpgradeType.Gravitic_Thrusters },
      { type: UpgradeItemType.Upgrade, value: UpgradeType.Carrier_Capacity },
      { type: UpgradeItemType.Tech, value: TechType.Disruption_Web },
      { type: UpgradeItemType.Upgrade, value: UpgradeType.Argus_Jewel }
    ]
  },
  observatory: {
    id: 'observatory',
    name: '옵저버토리',
    iconPath: BUILDING_ICONS.observatory,
    items: [
      { type: UpgradeItemType.Upgrade, value: UpgradeType.Gravitic_Boosters },
      { type: UpgradeItemType.Upgrade, value: UpgradeType.Sensor_Array }
    ]
  },
  arbiterTribunal: {
    id: 'arbiterTribunal',
    name: '아비터 트리뷰날',
    iconPath: BUILDING_ICONS.arbiterTribunal,
    items: [
      { type: UpgradeItemType.Tech, value: TechType.Recall },
      { type: UpgradeItemType.Tech, value: TechType.Stasis_Field },
      { type: UpgradeItemType.Upgrade, value: UpgradeType.Khaydarin_Core }
    ]
  }
};

// 테란 건물별 업그레이드/테크 매핑 (순서대로 정렬)
export const TERRAN_BUILDING_UPGRADES: Record<string, UIBuildingInfo> = {
  engineeringBay: {
    id: 'engineeringBay',
    name: '엔지니어링 베이',
    iconPath: BUILDING_ICONS.engineeringBay,
    items: [
      { type: UpgradeItemType.Upgrade, value: UpgradeType.Terran_Infantry_Weapons },
      { type: UpgradeItemType.Upgrade, value: UpgradeType.Terran_Infantry_Armor }
    ]
  },
  academy: {
    id: 'academy',
    name: '아카데미',
    iconPath: BUILDING_ICONS.academy,
    items: [
      { type: UpgradeItemType.Upgrade, value: UpgradeType.U_238_Shells },
      { type: UpgradeItemType.Tech, value: TechType.Stim_Packs },
      { type: UpgradeItemType.Tech, value: TechType.Restoration },
      { type: UpgradeItemType.Tech, value: TechType.Optical_Flare },
      { type: UpgradeItemType.Upgrade, value: UpgradeType.Caduceus_Reactor }
    ]
  },
  machineShop: {
    id: 'machineShop',
    name: '머신 샵',
    iconPath: BUILDING_ICONS.machineShop,
    items: [
      { type: UpgradeItemType.Upgrade, value: UpgradeType.Ion_Thrusters },
      { type: UpgradeItemType.Tech, value: TechType.Spider_Mines },
      { type: UpgradeItemType.Tech, value: TechType.Tank_Siege_Mode },
      { type: UpgradeItemType.Upgrade, value: UpgradeType.Charon_Boosters }
    ]
  },
  controlTower: {
    id: 'controlTower',
    name: '컨트롤 타워',
    iconPath: BUILDING_ICONS.controlTower,
    items: [
      { type: UpgradeItemType.Tech, value: TechType.Cloaking_Field },
      { type: UpgradeItemType.Upgrade, value: UpgradeType.Apollo_Reactor }
    ]
  },
  scienceFacility: {
    id: 'scienceFacility',
    name: '사이언스 퍼실리티',
    iconPath: BUILDING_ICONS.scienceFacility,
    items: [
      { type: UpgradeItemType.Tech, value: TechType.EMP_Shockwave },
      { type: UpgradeItemType.Tech, value: TechType.Irradiate },
      { type: UpgradeItemType.Upgrade, value: UpgradeType.Titan_Reactor }
    ]
  },
  covertOps: {
    id: 'covertOps',
    name: '코버트 옵스',
    iconPath: BUILDING_ICONS.covertOps,
    items: [
      { type: UpgradeItemType.Tech, value: TechType.Lockdown },
      { type: UpgradeItemType.Tech, value: TechType.Personnel_Cloaking },
      { type: UpgradeItemType.Upgrade, value: UpgradeType.Ocular_Implants },
      { type: UpgradeItemType.Upgrade, value: UpgradeType.Moebius_Reactor }
    ]
  },
  physicsLab: {
    id: 'physicsLab',
    name: '피직스 랩',
    iconPath: BUILDING_ICONS.physicsLab,
    items: [
      { type: UpgradeItemType.Tech, value: TechType.Yamato_Gun },
      { type: UpgradeItemType.Upgrade, value: UpgradeType.Colossus_Reactor }
    ]
  },
  armory: {
    id: 'armory',
    name: '아머리',
    iconPath: BUILDING_ICONS.armory,
    items: [
      { type: UpgradeItemType.Upgrade, value: UpgradeType.Terran_Vehicle_Weapons },
      { type: UpgradeItemType.Upgrade, value: UpgradeType.Terran_Vehicle_Plating },
      { type: UpgradeItemType.Upgrade, value: UpgradeType.Terran_Ship_Weapons },
      { type: UpgradeItemType.Upgrade, value: UpgradeType.Terran_Ship_Plating }
    ]
  }
};

// 저그 건물별 업그레이드/테크 매핑 (순서대로 정렬)
export const ZERG_BUILDING_UPGRADES: Record<string, UIBuildingInfo> = {
  hatchery: {
    id: 'hatchery',
    name: '해처리',
    iconPath: BUILDING_ICONS.hatchery,
    items: [
      { type: UpgradeItemType.Tech, value: TechType.Burrowing }
    ]
  },
  spawningPool: {
    id: 'spawningPool',
    name: '스포닝 풀',
    iconPath: BUILDING_ICONS.spawningPool,
    items: [
      { type: UpgradeItemType.Upgrade, value: UpgradeType.Metabolic_Boost },
      { type: UpgradeItemType.Upgrade, value: UpgradeType.Adrenal_Glands }
    ]
  },
  evolutionChamber: {
    id: 'evolutionChamber',
    name: '에볼루션 챔버',
    iconPath: BUILDING_ICONS.evolutionChamber,
    items: [
      { type: UpgradeItemType.Upgrade, value: UpgradeType.Zerg_Melee_Attacks },
      { type: UpgradeItemType.Upgrade, value: UpgradeType.Zerg_Missile_Attacks },
      { type: UpgradeItemType.Upgrade, value: UpgradeType.Zerg_Carapace }
    ]
  },
  hydraliskDen: {
    id: 'hydraliskDen',
    name: '히드라리스크 덴',
    iconPath: BUILDING_ICONS.hydraliskDen,
    items: [
      { type: UpgradeItemType.Upgrade, value: UpgradeType.Muscular_Augments },
      { type: UpgradeItemType.Upgrade, value: UpgradeType.Grooved_Spines },
      { type: UpgradeItemType.Tech, value: TechType.Lurker_Aspect }
    ]
  },
  lair: {
    id: 'lair',
    name: '레어',
    iconPath: BUILDING_ICONS.lair,
    items: [
      { type: UpgradeItemType.Upgrade, value: UpgradeType.Ventral_Sacs },
      { type: UpgradeItemType.Upgrade, value: UpgradeType.Antennae },
      { type: UpgradeItemType.Upgrade, value: UpgradeType.Pneumatized_Carapace }
    ]
  },
  spire: {
    id: 'spire',
    name: '스파이어',
    iconPath: BUILDING_ICONS.spire,
    items: [
      { type: UpgradeItemType.Upgrade, value: UpgradeType.Zerg_Flyer_Attacks },
      { type: UpgradeItemType.Upgrade, value: UpgradeType.Zerg_Flyer_Carapace }
    ]
  },
  queensNest: {
    id: 'queensNest',
    name: '퀸즈 네스트',
    iconPath: BUILDING_ICONS.queensNest,
    items: [
      { type: UpgradeItemType.Tech, value: TechType.Spawn_Broodlings },
      { type: UpgradeItemType.Tech, value: TechType.Ensnare },
      { type: UpgradeItemType.Upgrade, value: UpgradeType.Gamete_Meiosis }
    ]
  },
  ultraliskCavern: {
    id: 'ultraliskCavern',
    name: '울트라리스크 캐번',
    iconPath: BUILDING_ICONS.ultraliskCavern,
    items: [
      { type: UpgradeItemType.Upgrade, value: UpgradeType.Anabolic_Synthesis },
      { type: UpgradeItemType.Upgrade, value: UpgradeType.Chitinous_Plating }
    ]
  },
  defilerMound: {
    id: 'defilerMound',
    name: '디파일러 마운드',
    iconPath: BUILDING_ICONS.defilerMound,
    items: [
      { type: UpgradeItemType.Tech, value: TechType.Plague },
      { type: UpgradeItemType.Tech, value: TechType.Consume },
      { type: UpgradeItemType.Upgrade, value: UpgradeType.Metasynaptic_Node }
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
export const getBuildingsByRace = (race: RaceType): UIBuildingInfo[] => {
  return Object.values(RACE_BUILDING_UPGRADES[race]);
};

// 특정 건물의 모든 업그레이드/테크 아이템 가져오기 (새로운 구조)
export const getUpgradeItemsByBuilding = (race: RaceType, buildingId: string): UpgradeItem[] => {
  const building = RACE_BUILDING_UPGRADES[race][buildingId];
  if (!building) return [];
  
  return building.items;
};

