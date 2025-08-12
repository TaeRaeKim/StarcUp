// 건물 아이콘 import - Electron 빌드 호환성을 위한 정적 import
// Protoss Buildings
import ProtossForgeIcon from '/resources/Icon/Protoss/Buildings/ProtossForge.png';
import ProtossCyberneticsCoreIcon from '/resources/Icon/Protoss/Buildings/ProtossCyberneticsCore.png';
import ProtossCitadelOfAdunIcon from '/resources/Icon/Protoss/Buildings/ProtossCitadelOfAdun.png';
import ProtossTemplarArchivesIcon from '/resources/Icon/Protoss/Buildings/ProtossTemplarArchives.png';
import ProtossRoboticsSupportBayIcon from '/resources/Icon/Protoss/Buildings/ProtossRoboticsSupportBay.png';
import ProtossFleetBeaconIcon from '/resources/Icon/Protoss/Buildings/ProtossFleetBeacon.png';
import ProtossObservatoryIcon from '/resources/Icon/Protoss/Buildings/ProtossObservatory.png';
import ProtossArbiterTribunalIcon from '/resources/Icon/Protoss/Buildings/ProtossArbiterTribunal.png';

// Terran Buildings
import TerranEngineeringBayIcon from '/resources/Icon/Terran/Buildings/TerranEngineeringBay.png';
import TerranAcademyIcon from '/resources/Icon/Terran/Buildings/TerranAcademy.png';
import TerranMachineShopIcon from '/resources/Icon/Terran/Buildings/TerranMachineShop.png';
import TerranControlTowerIcon from '/resources/Icon/Terran/Buildings/TerranControlTower.png';
import TerranScienceFacilityIcon from '/resources/Icon/Terran/Buildings/TerranScienceFacility.png';
import TerranCovertOpsIcon from '/resources/Icon/Terran/Buildings/TerranCovertOps.png';
import TerranPhysicsLabIcon from '/resources/Icon/Terran/Buildings/TerranPhysicsLab.png';
import TerranArmoryIcon from '/resources/Icon/Terran/Buildings/TerranArmory.png';

// Zerg Buildings
import ZergHatcheryIcon from '/resources/Icon/Zerg/Buildings/ZergHatchery.png';
import ZergSpawningPoolIcon from '/resources/Icon/Zerg/Buildings/ZergSpawningPool.png';
import ZergEvolutionChamberIcon from '/resources/Icon/Zerg/Buildings/ZergEvolutionChamber.png';
import ZergHydraliskDenIcon from '/resources/Icon/Zerg/Buildings/ZergHydraliskDen.png';
import ZergLairIcon from '/resources/Icon/Zerg/Buildings/ZergLair.png';
import ZergSpireIcon from '/resources/Icon/Zerg/Buildings/ZergSpire.png';
import ZergQueensNestIcon from '/resources/Icon/Zerg/Buildings/ZergQueensNest.png';
import ZergUltraliskCavernIcon from '/resources/Icon/Zerg/Buildings/ZergUltraliskCavern.png';
import ZergDefilerMoundIcon from '/resources/Icon/Zerg/Buildings/ZergDefilerMound.png';

// 건물 아이콘 매핑
export const BUILDING_ICONS = {
  // Protoss
  forge: ProtossForgeIcon,
  cyberneticsCore: ProtossCyberneticsCoreIcon,
  citadelOfAdun: ProtossCitadelOfAdunIcon,
  templarArchives: ProtossTemplarArchivesIcon,
  roboticsSupportBay: ProtossRoboticsSupportBayIcon,
  fleetBeacon: ProtossFleetBeaconIcon,
  observatory: ProtossObservatoryIcon,
  arbiterTribunal: ProtossArbiterTribunalIcon,

  // Terran
  engineeringBay: TerranEngineeringBayIcon,
  academy: TerranAcademyIcon,
  machineShop: TerranMachineShopIcon,
  controlTower: TerranControlTowerIcon,
  scienceFacility: TerranScienceFacilityIcon,
  covertOps: TerranCovertOpsIcon,
  physicsLab: TerranPhysicsLabIcon,
  armory: TerranArmoryIcon,

  // Zerg
  hatchery: ZergHatcheryIcon,
  spawningPool: ZergSpawningPoolIcon,
  evolutionChamber: ZergEvolutionChamberIcon,
  hydraliskDen: ZergHydraliskDenIcon,
  lair: ZergLairIcon,
  spire: ZergSpireIcon,
  queensNest: ZergQueensNestIcon,
  ultraliskCavern: ZergUltraliskCavernIcon,
  defilerMound: ZergDefilerMoundIcon,
} as const;

export const getBuildingIcon = (buildingId: string): string => {
  return BUILDING_ICONS[buildingId as keyof typeof BUILDING_ICONS] || '';
};