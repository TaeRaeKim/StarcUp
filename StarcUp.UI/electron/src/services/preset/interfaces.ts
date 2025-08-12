import { StoredPreset, WorkerSettings } from '../storage/repositories/IPresetRepository'
import { RaceType } from '../../../../src/types/enums'
import { EventEmitter } from 'events'

/**
 * 프리셋 상태 관리자 인터페이스
 * 중앙화된 프리셋 상태 관리 및 이벤트 시스템 제공
 */
export interface IPresetStateManager {
  // 상태 조회
  getCurrentPreset(): IPreset | null
  getPresetState(): IPresetState
  getAllPresets(): IPreset[]
  
  // 프리셋 관리  
  switchPreset(presetId: string): Promise<void>
  updatePresetSettings(presetType: string, settings: any): Promise<void>
  updatePresetBatch(updates: IBatchPresetUpdate): Promise<void>
  toggleFeature(featureIndex: number, enabled: boolean): Promise<void>
  
  // 임시 저장 관리
  updateTempSettings(presetType: string, settings: any): void
  getTempSettings(presetType: string): any | null
  clearTempSettings(presetType?: string): void
  hasTempChanges(presetType?: string): boolean
  applyTempSettings(): Promise<void>
  
  // 이벤트 관리
  onStateChanged(callback: (event: IPresetChangeEvent) => void): () => void
  
  // 생명주기
  initialize(): Promise<void>
  dispose(): Promise<void>
  
  // Pro 기능 해제 (구독 기간 종료 시)
  sanitizeAllPresetsForNonPro(): Promise<void>
  sanitizePresetForNonPro(presetId: string): Promise<StoredPreset>
  refreshState(): Promise<void>
  getState(): IPresetState
}

/**
 * UI 계층에서 사용하는 프리셋 데이터 구조
 * StoredPreset을 기반으로 하되 UI에 최적화된 형태
 */
export interface IPreset {
  id: string
  name: string
  description: string
  featureStates: boolean[]
  selectedRace: RaceType
  workerSettings?: WorkerSettings
  populationSettings?: PopulationSettings
  createdAt: Date
  updatedAt: Date
}

// 인구수 설정 관련 타입들 (presetUtils.ts와 동기화)
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

/**
 * 전체 프리셋 상태를 나타내는 인터페이스
 */
export interface IPresetState {
  currentPreset: IPreset | null
  allPresets: IPreset[]
  selectedPresetIndex: number
  isLoading: boolean
  lastUpdated: Date
}

/**
 * 프리셋 변경 이벤트 타입 정의
 */
export interface IPresetChangeEvent {
  type: 'preset-switched' | 'settings-updated' | 'feature-toggled' | 'presets-loaded' | 
        'temp-settings-updated' | 'temp-settings-cleared' | 'temp-settings-applied' |
        'presets-sanitized' | 'preset-sanitized' | 'state-refreshed'
  presetId: string
  preset: IPreset | null
  changes: {
    featureStates?: boolean[]
    settings?: Record<string, any>
    toggledFeature?: { index: number; enabled: boolean }
    previousPresetId?: string
    allPresets?: IPreset[]
    presetType?: string
    tempSettings?: any
    appliedSettings?: IBatchPresetUpdate
  }
  timestamp: Date
}

/**
 * 프리셋 설정 업데이트 요청 인터페이스
 */
export interface IPresetSettingsUpdate {
  presetType: 'basic' | 'race' | 'worker' | 'unit' | 'upgrade' | 'population' | 'build-order'
  settings: Record<string, any>
}

/**
 * 배치 프리셋 업데이트 인터페이스
 */
export interface IBatchPresetUpdate {
  name?: string
  description?: string
  featureStates?: boolean[]
  selectedRace?: RaceType
  workerSettings?: WorkerSettings
  populationSettings?: PopulationSettings
}


/**
 * 성능 메트릭 정보
 */
export interface IPerformanceMetrics {
  operationName: string
  executionTime: number
  timestamp: Date
  success: boolean
  error?: string
}

/**
 * PresetStateManager 내부 상태 관리를 위한 인터페이스
 */
export interface IPresetStateManagerState {
  currentPresetId: string | null
  allPresets: Map<string, IPreset>
  selectedIndex: number
  isInitialized: boolean
  isLoading: boolean
  lastSyncTime: number
  tempSettings: Map<string, any>
}