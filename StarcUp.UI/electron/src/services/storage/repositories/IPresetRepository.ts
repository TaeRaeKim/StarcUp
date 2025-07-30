// 프리셋 Repository 인터페이스 - 데이터 접근 계층 추상화

// 일꾼 설정 인터페이스 (presetUtils.ts와 동일)
export interface WorkerSettings {
  workerCountDisplay: boolean
  includeProducingWorkers: boolean
  idleWorkerDisplay: boolean
  workerProductionDetection: boolean
  workerDeathDetection: boolean
  gasWorkerCheck: boolean
}

// UI 계층과 호환되는 프리셋 데이터 구조
export interface StoredPreset {
  id: string
  name: string
  description: string
  featureStates: boolean[]
  selectedRace: 'protoss' | 'terran' | 'zerg'
  workerSettings?: WorkerSettings  // 일꾼 상세 설정 (선택적)
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
  selectedRace: 'protoss' | 'terran' | 'zerg'
  workerSettings?: WorkerSettings
}

export interface UpdatePresetRequest {
  id: string
  name?: string
  description?: string
  featureStates?: boolean[]
  selectedRace?: 'protoss' | 'terran' | 'zerg'
  workerSettings?: WorkerSettings
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
}