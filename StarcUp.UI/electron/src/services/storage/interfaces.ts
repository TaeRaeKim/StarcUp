import { IUserData, IStoredPresetConfig, IGameHistory } from '../types'
import { RaceType } from '../../../../src/types/game'

export interface IDataStorageService {
  // 사용자 데이터 관리
  saveUserData(userId: string, data: Partial<IUserData>): Promise<{ success: boolean }>
  loadUserData(userId: string): Promise<IUserData | null>
  
  // 프리셋 관리
  savePreset(userId: string, preset: Omit<IStoredPresetConfig, 'id' | 'createdAt' | 'updatedAt'>): Promise<{ success: boolean; id?: string }>
  loadPresets(userId: string): Promise<IStoredPresetConfig[]>
  loadPreset(userId: string, presetId: string): Promise<IStoredPresetConfig | null>
  deletePreset(userId: string, presetId: string): Promise<{ success: boolean }>
  
  // 프리셋 UI 편의 메서드들
  updatePreset(userId: string, presetId: string, updates: {
    name?: string
    description?: string
    featureStates?: boolean[]
    selectedRace?: RaceType
    workerSettings?: {
      workerCountDisplay?: boolean
      includeProducingWorkers?: boolean
      idleWorkerDisplay?: boolean
      workerProductionDetection?: boolean
      workerDeathDetection?: boolean
      gasWorkerCheck?: boolean
    }
  }): Promise<{ success: boolean }>
  getSelectedPreset(userId: string): Promise<IStoredPresetConfig | null>
  setSelectedPreset(userId: string, index: number): Promise<{ success: boolean }>
  getPresetsWithSelection(userId: string): Promise<{
    presets: IStoredPresetConfig[]
    selectedIndex: number
    maxPresets: number
  }>
  
  // 게임 히스토리 관리
  saveGameHistory(userId: string, gameData: Omit<IGameHistory, 'id' | 'playedAt'>): Promise<{ success: boolean; id?: string }>
  loadGameHistory(userId: string, limit?: number): Promise<IGameHistory[]>
  
  // 데이터 백업 및 복원
  exportUserData(userId: string): Promise<{ success: boolean; data?: any }>
  importUserData(userId: string, importData: any): Promise<{ success: boolean }>
}

export interface IUserDataRepository {
  save(userId: string, data: Partial<IUserData>): Promise<void>
  load(userId: string): Promise<IUserData | null>
}

export interface IPresetRepository {
  save(userId: string, preset: Omit<IStoredPresetConfig, 'id' | 'createdAt' | 'updatedAt'>): Promise<string>
  load(userId: string, presetId: string): Promise<IStoredPresetConfig | null>
  loadAll(userId: string): Promise<IStoredPresetConfig[]>
  delete(userId: string, presetId: string): Promise<void>
}

export interface IGameHistoryRepository {
  save(userId: string, gameData: Omit<IGameHistory, 'id' | 'playedAt'>): Promise<string>
  loadAll(userId: string, limit?: number): Promise<IGameHistory[]>
}