import { UpgradeType, TechType, UPGRADE_NAMES, TECH_NAMES } from '../types/game';

// Terran Upgrade Icons
import TerranInfantryArmorIcon from '/resources/UpgradeIcon/Terran/TerranInfantryArmor.png';
import TerranVehiclePlatingIcon from '/resources/UpgradeIcon/Terran/TerranVehiclePlating.png';
import TerranShipPlatingIcon from '/resources/UpgradeIcon/Terran/TerranShipPlating.png';
import TerranInfantryWeaponsIcon from '/resources/UpgradeIcon/Terran/TerranInfantryWeapons.png';
import TerranVehicleWeaponsIcon from '/resources/UpgradeIcon/Terran/TerranVehicleWeapons.png';
import TerranShipWeaponsIcon from '/resources/UpgradeIcon/Terran/TerranShipWeapons.png';
import U238ShellsIcon from '/resources/UpgradeIcon/Terran/U238Shells.png';
import IonThrustersIcon from '/resources/UpgradeIcon/Terran/IonThrusters.png';
import TitanReactorIcon from '/resources/UpgradeIcon/Terran/TitanReactor.png';
import OcularImplantsIcon from '/resources/UpgradeIcon/Terran/OcularImplants.png';
import MoebiusReactorIcon from '/resources/UpgradeIcon/Terran/MoebiusReactor.png';
import ApolloReactorIcon from '/resources/UpgradeIcon/Terran/ApolloReactor.png';
import ColossusReactorIcon from '/resources/UpgradeIcon/Terran/ColossusReactor.png';
import CaduceusReactorIcon from '/resources/UpgradeIcon/Terran/CaduceusReactor.png';
import CharonBoostersIcon from '/resources/UpgradeIcon/Terran/CharonBoosters.png';

// Zerg Upgrade Icons
import ZergCarapaceIcon from '/resources/UpgradeIcon/Zerg/ZergCarapace.png';
import ZergFlyerCarapaceIcon from '/resources/UpgradeIcon/Zerg/ZergFlyerCarapace.png';
import ZergMeleeAttacksIcon from '/resources/UpgradeIcon/Zerg/ZergMeleeAttacks.png';
import ZergMissileAttacksIcon from '/resources/UpgradeIcon/Zerg/ZergMissileAttacks.png';
import ZergFlyerAttacksIcon from '/resources/UpgradeIcon/Zerg/ZergFlyerAttacks.png';
import VentralSacsIcon from '/resources/UpgradeIcon/Zerg/VentralSacs.png';
import AntennaeIcon from '/resources/UpgradeIcon/Zerg/Antennae.png';
import PneumatizedCarapaceIcon from '/resources/UpgradeIcon/Zerg/PneumatizedCarapace.png';
import MetabolicBoostIcon from '/resources/UpgradeIcon/Zerg/MetabolicBoost.png';
import AdrenalGlandsIcon from '/resources/UpgradeIcon/Zerg/AdrenalGlands.png';
import MuscularAugmentsIcon from '/resources/UpgradeIcon/Zerg/MuscularAugments.png';
import GroovedSpinesIcon from '/resources/UpgradeIcon/Zerg/GroovedSpines.png';
import GameteMeiosisIcon from '/resources/UpgradeIcon/Zerg/GameteMeiosis.png';
import MetasynapticNodeIcon from '/resources/UpgradeIcon/Zerg/MetasynapticNode.png';
import ChitinousPlatingIcon from '/resources/UpgradeIcon/Zerg/ChitinousPlating.png';
import AnabolicSynthesisIcon from '/resources/UpgradeIcon/Zerg/AnabolicSynthesis.png';

// Protoss Upgrade Icons
import ProtossGroundArmorIcon from '/resources/UpgradeIcon/Protoss/ProtossGroundArmor.png';
import ProtossAirArmorIcon from '/resources/UpgradeIcon/Protoss/ProtossAirArmor.png';
import ProtossGroundWeaponsIcon from '/resources/UpgradeIcon/Protoss/ProtossGroundWeapons.png';
import ProtossAirWeaponsIcon from '/resources/UpgradeIcon/Protoss/ProtossAirWeapons.png';
import ProtossPlasmaShieldsIcon from '/resources/UpgradeIcon/Protoss/ProtossPlasmaShields.png';
import SingularityChargeIcon from '/resources/UpgradeIcon/Protoss/SingularityCharge.png';
import LegEnhancementsIcon from '/resources/UpgradeIcon/Protoss/LegEnhancements.png';
import ScarabDamageIcon from '/resources/UpgradeIcon/Protoss/ScarabDamage.png';
import ReaverCapacityIcon from '/resources/UpgradeIcon/Protoss/ReaverCapacity.png';
import GraviticDriveIcon from '/resources/UpgradeIcon/Protoss/GraviticDrive.png';
import SensorArrayIcon from '/resources/UpgradeIcon/Protoss/SensorArray.png';
import GraviticBoostersIcon from '/resources/UpgradeIcon/Protoss/GraviticBoosters.png';
import KhaydarinAmuletIcon from '/resources/UpgradeIcon/Protoss/KhaydarinAmulet.png';
import ApialSensorsIcon from '/resources/UpgradeIcon/Protoss/ApialSensors.png';
import GraviticThrustersIcon from '/resources/UpgradeIcon/Protoss/GraviticThrusters.png';
import CarrierCapacityIcon from '/resources/UpgradeIcon/Protoss/CarrierCapacity.png';
import KhaydarinCoreIcon from '/resources/UpgradeIcon/Protoss/KhaydarinCore.png';
import ArgusJewelIcon from '/resources/UpgradeIcon/Protoss/ArgusJewel.png';
import ArgusTalismanIcon from '/resources/UpgradeIcon/Protoss/ArgusTalisman.png';

// Tech Icons
// Terran Tech Icons
import StimPacksIcon from '/resources/UpgradeIcon/Terran/StimPacks.png';
import LockdownIcon from '/resources/UpgradeIcon/Terran/Lockdown.png';
import EMPShockwaveIcon from '/resources/UpgradeIcon/Terran/EMPShockwave.png';
import SpiderMinesIcon from '/resources/UpgradeIcon/Terran/SpiderMines.png';
//import ScannerSweepIcon from '/resources/UpgradeIcon/Terran/ScannerSweep.png';
import TankSiegeModeIcon from '/resources/UpgradeIcon/Terran/TankSiegeMode.png';
//import DefensiveMatrixIcon from '/resources/UpgradeIcon/Terran/DefensiveMatrix.png';
import IrradiateIcon from '/resources/UpgradeIcon/Terran/Irradiate.png';
import YamatoGunIcon from '/resources/UpgradeIcon/Terran/YamatoGun.png';
import CloakingFieldIcon from '/resources/UpgradeIcon/Terran/CloakingField.png';
import PersonnelCloakingIcon from '/resources/UpgradeIcon/Terran/PersonnelCloaking.png';
import RestorationIcon from '/resources/UpgradeIcon/Terran/Restoration.png';
import OpticalFlareIcon from '/resources/UpgradeIcon/Terran/OpticalFlare.png';
//import NuclearStrikeIcon from '/resources/UpgradeIcon/Terran/NuclearStrike.png';

// Zerg Tech Icons
import BurrowingIcon from '/resources/UpgradeIcon/Zerg/Burrowing.png';
//import InfestationIcon from '/resources/UpgradeIcon/Zerg/Infestation.png';
import SpawnBroodlingsIcon from '/resources/UpgradeIcon/Zerg/SpawnBroodlings.png';
//import DarkSwarmIcon from '/resources/UpgradeIcon/Zerg/DarkSwarm.png';
import PlagueIcon from '/resources/UpgradeIcon/Zerg/Plague.png';
import ConsumeIcon from '/resources/UpgradeIcon/Zerg/Consume.png';
import EnsnareIcon from '/resources/UpgradeIcon/Zerg/Ensnare.png';
//import ParasiteIcon from '/resources/UpgradeIcon/Zerg/Parasite.png';
import LurkerAspectIcon from '/resources/UpgradeIcon/Zerg/LurkerAspect.png';

// Protoss Tech Icons
import PsionicStormIcon from '/resources/UpgradeIcon/Protoss/PsionicStorm.png';
import HallucinationIcon from '/resources/UpgradeIcon/Protoss/Hallucination.png';
import RecallIcon from '/resources/UpgradeIcon/Protoss/Recall.png';
import StasisFieldIcon from '/resources/UpgradeIcon/Protoss/StasisField.png';
//import ArchonWarpIcon from '/resources/UpgradeIcon/Protoss/ArchonWarp.png';
import DisruptionWebIcon from '/resources/UpgradeIcon/Protoss/DisruptionWeb.png';
import MindControlIcon from '/resources/UpgradeIcon/Protoss/MindControl.png';
//import DarkArchonMeldIcon from '/resources/UpgradeIcon/Protoss/DarkArchonMeld.png';
//import FeedbackIcon from '/resources/UpgradeIcon/Protoss/Feedback.png';
import MaelstromIcon from '/resources/UpgradeIcon/Protoss/Maelstrom.png';

// Special Icons
import HealingIcon from '/resources/UpgradeIcon/Healing.png';
import UnknownIcon from '/resources/UpgradeIcon/Unknown.png';
import NoneIcon from '/resources/UpgradeIcon/None.png';

// 업그레이드 아이콘 경로 생성
export const getUpgradeIconPath = (upgradeType: UpgradeType): string => {
  // 업그레이드 타입별 아이콘 경로 매핑
  const upgradeIconMap: Record<UpgradeType, string> = {
    // Terran Upgrades
    [UpgradeType.Terran_Infantry_Armor]: TerranInfantryArmorIcon,
    [UpgradeType.Terran_Vehicle_Plating]: TerranVehiclePlatingIcon,
    [UpgradeType.Terran_Ship_Plating]: TerranShipPlatingIcon,
    [UpgradeType.Terran_Infantry_Weapons]: TerranInfantryWeaponsIcon,
    [UpgradeType.Terran_Vehicle_Weapons]: TerranVehicleWeaponsIcon,
    [UpgradeType.Terran_Ship_Weapons]: TerranShipWeaponsIcon,
    [UpgradeType.U_238_Shells]: U238ShellsIcon,
    [UpgradeType.Ion_Thrusters]: IonThrustersIcon,
    [UpgradeType.Titan_Reactor]: TitanReactorIcon,
    [UpgradeType.Ocular_Implants]: OcularImplantsIcon,
    [UpgradeType.Moebius_Reactor]: MoebiusReactorIcon,
    [UpgradeType.Apollo_Reactor]: ApolloReactorIcon,
    [UpgradeType.Colossus_Reactor]: ColossusReactorIcon,
    [UpgradeType.Caduceus_Reactor]: CaduceusReactorIcon,
    [UpgradeType.Charon_Boosters]: CharonBoostersIcon,

    // Zerg Upgrades
    [UpgradeType.Zerg_Carapace]: ZergCarapaceIcon,
    [UpgradeType.Zerg_Flyer_Carapace]: ZergFlyerCarapaceIcon,
    [UpgradeType.Zerg_Melee_Attacks]: ZergMeleeAttacksIcon,
    [UpgradeType.Zerg_Missile_Attacks]: ZergMissileAttacksIcon,
    [UpgradeType.Zerg_Flyer_Attacks]: ZergFlyerAttacksIcon,
    [UpgradeType.Ventral_Sacs]: VentralSacsIcon,
    [UpgradeType.Antennae]: AntennaeIcon,
    [UpgradeType.Pneumatized_Carapace]: PneumatizedCarapaceIcon,
    [UpgradeType.Metabolic_Boost]: MetabolicBoostIcon,
    [UpgradeType.Adrenal_Glands]: AdrenalGlandsIcon,
    [UpgradeType.Muscular_Augments]: MuscularAugmentsIcon,
    [UpgradeType.Grooved_Spines]: GroovedSpinesIcon,
    [UpgradeType.Gamete_Meiosis]: GameteMeiosisIcon,
    [UpgradeType.Metasynaptic_Node]: MetasynapticNodeIcon,
    [UpgradeType.Chitinous_Plating]: ChitinousPlatingIcon,
    [UpgradeType.Anabolic_Synthesis]: AnabolicSynthesisIcon,

    // Protoss Upgrades
    [UpgradeType.Protoss_Ground_Armor]: ProtossGroundArmorIcon,
    [UpgradeType.Protoss_Air_Armor]: ProtossAirArmorIcon,
    [UpgradeType.Protoss_Ground_Weapons]: ProtossGroundWeaponsIcon,
    [UpgradeType.Protoss_Air_Weapons]: ProtossAirWeaponsIcon,
    [UpgradeType.Protoss_Plasma_Shields]: ProtossPlasmaShieldsIcon,
    [UpgradeType.Singularity_Charge]: SingularityChargeIcon,
    [UpgradeType.Leg_Enhancements]: LegEnhancementsIcon,
    [UpgradeType.Scarab_Damage]: ScarabDamageIcon,
    [UpgradeType.Reaver_Capacity]: ReaverCapacityIcon,
    [UpgradeType.Gravitic_Drive]: GraviticDriveIcon,
    [UpgradeType.Sensor_Array]: SensorArrayIcon,
    [UpgradeType.Gravitic_Boosters]: GraviticBoostersIcon,
    [UpgradeType.Khaydarin_Amulet]: KhaydarinAmuletIcon,
    [UpgradeType.Apial_Sensors]: ApialSensorsIcon,
    [UpgradeType.Gravitic_Thrusters]: GraviticThrustersIcon,
    [UpgradeType.Carrier_Capacity]: CarrierCapacityIcon,
    [UpgradeType.Khaydarin_Core]: KhaydarinCoreIcon,
    [UpgradeType.Argus_Jewel]: ArgusJewelIcon,
    [UpgradeType.Argus_Talisman]: ArgusTalismanIcon,

    // Special
    [UpgradeType.Upgrade_60]: UnknownIcon,
    [UpgradeType.None]: NoneIcon,
    [UpgradeType.Unknown]: UnknownIcon,
    [UpgradeType.MAX]: UnknownIcon
  };

  return upgradeIconMap[upgradeType] || UnknownIcon;
};

// 테크 아이콘 경로 생성
export const getTechIconPath = (techType: TechType): string => {
  // 테크 타입별 아이콘 경로 매핑
  const techIconMap: Record<TechType, string> = {
    // Terran Techs
    [TechType.Stim_Packs]: StimPacksIcon,
    [TechType.Lockdown]: LockdownIcon,
    [TechType.EMP_Shockwave]: EMPShockwaveIcon,
    [TechType.Spider_Mines]: SpiderMinesIcon,
    [TechType.Scanner_Sweep]: UnknownIcon,
    [TechType.Tank_Siege_Mode]: TankSiegeModeIcon,
    [TechType.Defensive_Matrix]: UnknownIcon,
    [TechType.Irradiate]: IrradiateIcon,
    [TechType.Yamato_Gun]: YamatoGunIcon,
    [TechType.Cloaking_Field]: CloakingFieldIcon,
    [TechType.Personnel_Cloaking]: PersonnelCloakingIcon,
    [TechType.Restoration]: RestorationIcon,
    [TechType.Optical_Flare]: OpticalFlareIcon,
    [TechType.Nuclear_Strike]: UnknownIcon,

    // Zerg Techs
    [TechType.Burrowing]: BurrowingIcon,
    [TechType.Infestation]: UnknownIcon,
    [TechType.Spawn_Broodlings]: SpawnBroodlingsIcon,
    [TechType.Dark_Swarm]: UnknownIcon,
    [TechType.Plague]: PlagueIcon,
    [TechType.Consume]: ConsumeIcon,
    [TechType.Ensnare]: EnsnareIcon,
    [TechType.Parasite]: UnknownIcon,
    [TechType.Lurker_Aspect]: LurkerAspectIcon,

    // Protoss Techs
    [TechType.Psionic_Storm]: PsionicStormIcon,
    [TechType.Hallucination]: HallucinationIcon,
    [TechType.Recall]: RecallIcon,
    [TechType.Stasis_Field]: StasisFieldIcon,
    [TechType.Archon_Warp]: UnknownIcon,
    [TechType.Disruption_Web]: DisruptionWebIcon,
    [TechType.Mind_Control]: MindControlIcon,
    [TechType.Dark_Archon_Meld]: UnknownIcon,
    [TechType.Feedback]: UnknownIcon,
    [TechType.Maelstrom]: MaelstromIcon,

    // Special/Unused
    [TechType.Unused_26]: UnknownIcon,
    [TechType.Unused_33]: UnknownIcon,
    [TechType.Healing]: HealingIcon,
    [TechType.None]: NoneIcon,
    [TechType.Unknown]: UnknownIcon,
    [TechType.MAX]: UnknownIcon
  };

  return techIconMap[techType] || UnknownIcon;
};

// 건물 아이콘 경로 생성
export const getBuildingIconPath = (iconPath: string): string => {
  return iconPath;
};

// 업그레이드/테크 이름 가져오기
export const getUpgradeName = (upgradeType: UpgradeType): string => {
  return UPGRADE_NAMES[upgradeType] || upgradeType.toString();
};

export const getTechName = (techType: TechType): string => {
  return TECH_NAMES[techType] || techType.toString();
};

// 업그레이드 아이템 정보 생성
export interface UpgradeItemInfo {
  type: 'upgrade' | 'tech';
  id: UpgradeType | TechType;
  name: string;
  iconPath: string;
}

export const createUpgradeItemInfo = (upgradeType: UpgradeType): UpgradeItemInfo => {
  return {
    type: 'upgrade',
    id: upgradeType,
    name: getUpgradeName(upgradeType),
    iconPath: getUpgradeIconPath(upgradeType)
  };
};

export const createTechItemInfo = (techType: TechType): UpgradeItemInfo => {
  return {
    type: 'tech',
    id: techType,
    name: getTechName(techType),
    iconPath: getTechIconPath(techType)
  };
};