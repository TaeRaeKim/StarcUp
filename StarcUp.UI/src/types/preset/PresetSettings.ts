import { RaceType } from '../game/RaceType';
import { UnitType } from '../game/UnitType';
import { UpgradeType } from '../game/UpgradeType';
import { TechType } from '../game/TechType';

// 일꾼 설정 인터페이스
export interface WorkerSettings {
  workerCountDisplay: boolean
  includeProducingWorkers: boolean
  idleWorkerDisplay: boolean
  workerProductionDetection: boolean
  workerDeathDetection: boolean
  gasWorkerCheck: boolean
}

// 인구수 설정 인터페이스
export interface PopulationSettings {
  mode: 'fixed' | 'building'
  fixedSettings?: FixedModeSettings
  buildingSettings?: BuildingModeSettings
}

export interface FixedModeSettings {
  thresholdValue: number
  timeLimit?: TimeLimitSettings
}

export interface TimeLimitSettings {
  enabled: boolean
  minutes: number
  seconds: number
}

export interface BuildingModeSettings {
  race: RaceType
  trackedBuildings: TrackedBuilding[]
}

export interface TrackedBuilding {
  buildingType: UnitType
  multiplier: number
  enabled: boolean
}

// 업그레이드 설정 인터페이스
export interface UpgradeSettings {
  categories: UpgradeCategory[];
  showRemainingTime: boolean;        // 잔여시간표기
  showProgressPercentage: boolean;   // 진행률표기
  showProgressBar: boolean;          // 프로그레스바표기
  upgradeCompletionAlert: boolean;   // 업그레이드완료알림
  upgradeStateTracking: boolean;     // 업그레이드상태추적
}

export enum UpgradeItemType {
  Upgrade = 0,
  Tech = 1
}

export interface UpgradeItem {
  type: UpgradeItemType;
  value: UpgradeType | TechType;
}

export interface UpgradeCategory {
  id: string;
  name: string;
  items: UpgradeItem[];
}

// 업그레이드/테크 통합 아이템 (UI에서 사용)
export interface UIUpgradeItem {
  item: UpgradeItem;
  name: string;
  iconPath: string;
  buildingId: string;
}

// 건물 정보 (UI에서 사용)
export interface UIBuildingInfo {
  id: string;
  name: string;
  iconPath: string;
  items: UpgradeItem[];
}

// === 런타임 데이터 및 이벤트 타입들 ===

/**
 * WorkerManager에서 전송되는 일꾼 상태 데이터 (런타임)
 */
export interface WorkerStatusData {
  totalWorkers: number
  idleWorkers: number
  productionWorkers: number
  calculatedTotal: number
  eventType?: string // EffectType을 string으로 처리
}

/**
 * WorkerPreset 변경 이벤트 데이터
 */
export interface WorkerPresetData {
  success: boolean
  currentPreset?: {
    mask: number
  }
}

/**
 * 업그레이드 관련 이벤트 데이터
 * 런타임 업그레이드 카테고리를 참조하기 위해 별도 import 필요
 */
export interface UpgradeEventData {
  categories?: any[] // 런타임 UpgradeCategory 타입은 overlay에서 정의
  item?: UpgradeItem
  level?: number
  categoryId?: number
  categoryName?: string
}