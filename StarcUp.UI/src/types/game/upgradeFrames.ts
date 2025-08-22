import { UpgradeType } from './UpgradeType';

// 업그레이드별 소요 프레임 수 및 최대 레벨
export interface UpgradeInfo {
  frames: number[];  // 레벨별 프레임 수 배열 (1lv, 2lv, 3lv)
  maxLevel: number;
}

// Armor & Weapons 업그레이드 (3레벨)
const ARMOR_WEAPON_FRAMES = [4000, 4480, 4960];

export const upgradeFrames: Record<UpgradeType, UpgradeInfo> = {
  // Terran Armor & Weapons (3레벨)
  [UpgradeType.Terran_Infantry_Armor]: { frames: ARMOR_WEAPON_FRAMES, maxLevel: 3 },
  [UpgradeType.Terran_Vehicle_Plating]: { frames: ARMOR_WEAPON_FRAMES, maxLevel: 3 },
  [UpgradeType.Terran_Ship_Plating]: { frames: ARMOR_WEAPON_FRAMES, maxLevel: 3 },
  [UpgradeType.Terran_Infantry_Weapons]: { frames: ARMOR_WEAPON_FRAMES, maxLevel: 3 },
  [UpgradeType.Terran_Vehicle_Weapons]: { frames: ARMOR_WEAPON_FRAMES, maxLevel: 3 },
  [UpgradeType.Terran_Ship_Weapons]: { frames: ARMOR_WEAPON_FRAMES, maxLevel: 3 },

  // Zerg Armor & Weapons (3레벨)
  [UpgradeType.Zerg_Carapace]: { frames: ARMOR_WEAPON_FRAMES, maxLevel: 3 },
  [UpgradeType.Zerg_Flyer_Carapace]: { frames: ARMOR_WEAPON_FRAMES, maxLevel: 3 },
  [UpgradeType.Zerg_Melee_Attacks]: { frames: ARMOR_WEAPON_FRAMES, maxLevel: 3 },
  [UpgradeType.Zerg_Missile_Attacks]: { frames: ARMOR_WEAPON_FRAMES, maxLevel: 3 },
  [UpgradeType.Zerg_Flyer_Attacks]: { frames: ARMOR_WEAPON_FRAMES, maxLevel: 3 },

  // Protoss Armor & Weapons (3레벨)
  [UpgradeType.Protoss_Ground_Armor]: { frames: ARMOR_WEAPON_FRAMES, maxLevel: 3 },
  [UpgradeType.Protoss_Air_Armor]: { frames: ARMOR_WEAPON_FRAMES, maxLevel: 3 },
  [UpgradeType.Protoss_Ground_Weapons]: { frames: ARMOR_WEAPON_FRAMES, maxLevel: 3 },
  [UpgradeType.Protoss_Air_Weapons]: { frames: ARMOR_WEAPON_FRAMES, maxLevel: 3 },
  [UpgradeType.Protoss_Plasma_Shields]: { frames: ARMOR_WEAPON_FRAMES, maxLevel: 3 },

  // Terran Unit Upgrades (1레벨)
  [UpgradeType.U_238_Shells]: { frames: [1500], maxLevel: 1 },
  [UpgradeType.Ion_Thrusters]: { frames: [1500], maxLevel: 1 },
  [UpgradeType.Titan_Reactor]: { frames: [2500], maxLevel: 1 },
  [UpgradeType.Ocular_Implants]: { frames: [2500], maxLevel: 1 },
  [UpgradeType.Moebius_Reactor]: { frames: [2500], maxLevel: 1 },
  [UpgradeType.Apollo_Reactor]: { frames: [2500], maxLevel: 1 },
  [UpgradeType.Colossus_Reactor]: { frames: [2500], maxLevel: 1 },
  [UpgradeType.Caduceus_Reactor]: { frames: [2500], maxLevel: 1 },
  [UpgradeType.Charon_Boosters]: { frames: [2000], maxLevel: 1 },

  // Zerg Unit Upgrades (1레벨)
  [UpgradeType.Ventral_Sacs]: { frames: [2400], maxLevel: 1 },
  [UpgradeType.Antennae]: { frames: [2000], maxLevel: 1 },
  [UpgradeType.Pneumatized_Carapace]: { frames: [2000], maxLevel: 1 },
  [UpgradeType.Metabolic_Boost]: { frames: [1500], maxLevel: 1 },
  [UpgradeType.Adrenal_Glands]: { frames: [1500], maxLevel: 1 },
  [UpgradeType.Muscular_Augments]: { frames: [1500], maxLevel: 1 },
  [UpgradeType.Grooved_Spines]: { frames: [1500], maxLevel: 1 },
  [UpgradeType.Gamete_Meiosis]: { frames: [2500], maxLevel: 1 },
  [UpgradeType.Metasynaptic_Node]: { frames: [2500], maxLevel: 1 },
  [UpgradeType.Chitinous_Plating]: { frames: [2000], maxLevel: 1 },
  [UpgradeType.Anabolic_Synthesis]: { frames: [2000], maxLevel: 1 },

  // Protoss Unit Upgrades (1레벨)
  [UpgradeType.Singularity_Charge]: { frames: [2500], maxLevel: 1 },
  [UpgradeType.Leg_Enhancements]: { frames: [2000], maxLevel: 1 },
  [UpgradeType.Scarab_Damage]: { frames: [2500], maxLevel: 1 },
  [UpgradeType.Reaver_Capacity]: { frames: [2500], maxLevel: 1 },
  [UpgradeType.Gravitic_Drive]: { frames: [2500], maxLevel: 1 },
  [UpgradeType.Sensor_Array]: { frames: [2000], maxLevel: 1 },
  [UpgradeType.Gravitic_Boosters]: { frames: [2000], maxLevel: 1 },
  [UpgradeType.Khaydarin_Amulet]: { frames: [2500], maxLevel: 1 },
  [UpgradeType.Apial_Sensors]: { frames: [2000], maxLevel: 1 },
  [UpgradeType.Gravitic_Thrusters]: { frames: [2500], maxLevel: 1 },
  [UpgradeType.Carrier_Capacity]: { frames: [1500], maxLevel: 1 },
  [UpgradeType.Khaydarin_Core]: { frames: [2500], maxLevel: 1 },
  [UpgradeType.Argus_Jewel]: { frames: [2500], maxLevel: 1 },
  [UpgradeType.Argus_Talisman]: { frames: [2500], maxLevel: 1 },

  // Special values
  [UpgradeType.Upgrade_60]: { frames: [0], maxLevel: 0 },
  [UpgradeType.None]: { frames: [0], maxLevel: 0 },
  [UpgradeType.Unknown]: { frames: [0], maxLevel: 0 },
};

// 업그레이드 정보 가져오기 헬퍼 함수
export function getUpgradeFrameInfo(upgradeType: UpgradeType): UpgradeInfo {
  return upgradeFrames[upgradeType] || { frames: [0], maxLevel: 0 };
}

// 특정 레벨의 프레임 수 가져오기
export function getUpgradeFramesForLevel(upgradeType: UpgradeType, level: number): number {
  const info = getUpgradeFrameInfo(upgradeType);
  if (level < 1 || level > info.maxLevel) return 0;
  return info.frames[level - 1] || 0;
}