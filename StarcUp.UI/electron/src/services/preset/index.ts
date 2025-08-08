// 프리셋 상태 관리 모듈
// PresetStateManager와 관련 인터페이스들을 외부에 노출

export { PresetStateManager } from './PresetStateManager'
export type { 
  IPresetStateManager,
  IPreset,
  IPresetState,
  IPresetChangeEvent,
  IPresetSettingsUpdate,
  IBatchPresetUpdate,
  IPerformanceMetrics,
  IPresetStateManagerState
} from './interfaces'