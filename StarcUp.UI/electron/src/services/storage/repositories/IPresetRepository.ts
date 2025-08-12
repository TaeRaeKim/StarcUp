// 프리셋 Repository 인터페이스 - 데이터 접근 계층 추상화

import { RaceType } from '../../../../../src/types/enums'

// 일꾼 설정 인터페이스 (presetUtils.ts와 동일)
export interface WorkerSettings {
  workerCountDisplay: boolean
  includeProducingWorkers: boolean
  idleWorkerDisplay: boolean
  workerProductionDetection: boolean
  workerDeathDetection: boolean
  gasWorkerCheck: boolean
}

// 인구수 설정 인터페이스 (interfaces.ts와 동일)
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
  buildingType: string
  multiplier: number
  enabled: boolean
}

// UI 계층과 호환되는 프리셋 데이터 구조
export interface StoredPreset {
  id: string
  name: string
  description: string
  featureStates: boolean[]
  selectedRace: RaceType
  workerSettings?: WorkerSettings      // 일꾼 상세 설정 (선택적)
  populationSettings?: PopulationSettings // 인구수 상세 설정 (선택적)
  createdAt: Date
  updatedAt: Date
}

export interface PresetCollection {
  version: string
  maxPresets: number
  presets: StoredPreset[]
  selectedPresetIndex: number
  lastUpdated: Date
}

export interface CreatePresetRequest {
  name: string
  description: string
  featureStates: boolean[]
  selectedRace: RaceType
  workerSettings?: WorkerSettings
  populationSettings?: PopulationSettings
}

export interface UpdatePresetRequest {
  id: string
  name?: string
  description?: string
  featureStates?: boolean[]
  selectedRace?: RaceType
  workerSettings?: WorkerSettings
  populationSettings?: PopulationSettings
}

/**
 * 프리셋 데이터 접근을 위한 Repository 인터페이스
 * 나중에 파일 기반에서 데이터베이스로 전환할 때 구현체만 교체하면 됨
 */
export interface IPresetRepository {
  /**
   * 모든 프리셋을 로드합니다
   */
  loadAll(): Promise<PresetCollection>
  
  /**
   * 특정 프리셋을 ID로 조회합니다
   */
  findById(id: string): Promise<StoredPreset | null>
  
  /**
   * 새 프리셋을 생성합니다 (최대 3개 제한)
   */
  create(preset: CreatePresetRequest): Promise<StoredPreset>
  
  /**
   * 기존 프리셋을 업데이트합니다
   */
  update(preset: UpdatePresetRequest): Promise<StoredPreset>
  
  /**
   * 프리셋을 삭제합니다
   */
  delete(id: string): Promise<void>
  
  /**
   * 선택된 프리셋 인덱스를 업데이트합니다
   */
  updateSelectedIndex(index: number): Promise<void>
  
  /**
   * 현재 선택된 프리셋을 반환합니다
   */
  getSelected(): Promise<StoredPreset | null>
  
  /**
   * 프리셋 개수를 반환합니다
   */
  count(): Promise<number>
  
  /**
   * 저장소를 초기화합니다 (기본 프리셋 생성)
   */
  initialize(): Promise<void>

  /**
   * 구독 기간 종료 시 모든 프리셋에서 Pro 기능을 해제하고 데이터베이스에 저장합니다.
   */
  sanitizeAllPresetsForNonPro(): Promise<void>

  /**
   * 특정 프리셋의 Pro 기능을 해제하고 데이터베이스에 저장합니다.
   */
  sanitizePresetForNonPro(presetId: string): Promise<StoredPreset>
}