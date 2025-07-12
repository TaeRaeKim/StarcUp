import { IUserData, IPreset, IGameHistory } from '../types'

export interface IDataStorageService {
  // 사용자 데이터 관리
  saveUserData(userId: string, data: Partial<IUserData>): Promise<{ success: boolean }>
  loadUserData(userId: string): Promise<IUserData | null>
  
  // 프리셋 관리
  savePreset(userId: string, preset: Omit<IPreset, 'id' | 'createdAt' | 'updatedAt'>): Promise<{ success: boolean; id?: string }>
  loadPresets(userId: string): Promise<IPreset[]>
  loadPreset(userId: string, presetId: string): Promise<IPreset | null>
  deletePreset(userId: string, presetId: string): Promise<{ success: boolean }>
  
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
  save(userId: string, preset: Omit<IPreset, 'id' | 'createdAt' | 'updatedAt'>): Promise<string>
  load(userId: string, presetId: string): Promise<IPreset | null>
  loadAll(userId: string): Promise<IPreset[]>
  delete(userId: string, presetId: string): Promise<void>
}

export interface IGameHistoryRepository {
  save(userId: string, gameData: Omit<IGameHistory, 'id' | 'playedAt'>): Promise<string>
  loadAll(userId: string, limit?: number): Promise<IGameHistory[]>
}