import { IDataStorageService } from './interfaces'
import { IUserData, IPreset, IGameHistory } from '../types'
import { 
  IPresetRepository, 
  FilePresetRepository, 
  StoredPreset, 
  CreatePresetRequest, 
  UpdatePresetRequest,
  WorkerSettings
} from './repositories'

export class DataStorageService implements IDataStorageService {
  private presetRepository: IPresetRepository
  
  constructor() {
    // Repository 주입 (나중에 DI 컨테이너로 교체 가능)
    this.presetRepository = new FilePresetRepository()
    this.initialize()
  }
  
  private async initialize(): Promise<void> {
    try {
      await this.presetRepository.initialize()
      console.log('✅ DataStorageService 초기화 완료')
    } catch (error) {
      console.error('❌ DataStorageService 초기화 실패:', error)
    }
  }
  
  // 사용자 데이터 관리
  async saveUserData(userId: string, data: Partial<IUserData>): Promise<{ success: boolean }> {
    try {
      console.log(`💾 사용자 데이터 저장: ${userId}`)
      // TODO: 실제 파일 저장 로직 구현
      return { success: true }
    } catch (error) {
      console.error('Save user data failed:', error)
      return { success: false }
    }
  }
  
  async loadUserData(userId: string): Promise<IUserData | null> {
    try {
      console.log(`📂 사용자 데이터 로드: ${userId}`)
      // TODO: 실제 파일 로드 로직 구현
      return null
    } catch (error) {
      console.error('Load user data failed:', error)
      return null
    }
  }
  
  // 프리셋 관리 (Repository를 통한 구현)
  async savePreset(userId: string, preset: Omit<IPreset, 'id' | 'createdAt' | 'updatedAt'>): Promise<{ success: boolean; id?: string }> {
    try {
      console.log(`💾 프리셋 저장 요청: ${userId}`)
      
      // IPreset을 CreatePresetRequest로 변환
      const createRequest: CreatePresetRequest = {
        name: preset.name || 'New Preset',
        description: preset.data?.description || '',
        featureStates: preset.data?.featureStates || [true, false, false, false, false],
        selectedRace: preset.data?.selectedRace || 'protoss'
      }
      
      const savedPreset = await this.presetRepository.create(createRequest)
      return { success: true, id: savedPreset.id }
    } catch (error) {
      console.error('Save preset failed:', error)
      return { success: false }
    }
  }
  
  async loadPresets(userId: string): Promise<IPreset[]> {
    try {
      console.log(`📂 프리셋 목록 로드: ${userId}`)
      
      const collection = await this.presetRepository.loadAll()
      
      // StoredPreset을 IPreset으로 변환
      const presets: IPreset[] = collection.presets.map(stored => ({
        id: stored.id,
        name: stored.name,
        type: 'game' as const,
        data: {
          description: stored.description,
          featureStates: stored.featureStates,
          selectedRace: stored.selectedRace,
          workerSettings: stored.workerSettings
        },
        createdAt: stored.createdAt,
        updatedAt: stored.updatedAt
      }))
      
      return presets
    } catch (error) {
      console.error('Load presets failed:', error)
      return []
    }
  }
  
  async loadPreset(userId: string, presetId: string): Promise<IPreset | null> {
    try {
      console.log(`📂 프리셋 로드: ${userId}/${presetId}`)
      
      const stored = await this.presetRepository.findById(presetId)
      if (!stored) return null
      
      // StoredPreset을 IPreset으로 변환
      const preset: IPreset = {
        id: stored.id,
        name: stored.name,
        type: 'game' as const,
        data: {
          description: stored.description,
          featureStates: stored.featureStates,
          selectedRace: stored.selectedRace,
          workerSettings: stored.workerSettings
        },
        createdAt: stored.createdAt,
        updatedAt: stored.updatedAt
      }
      
      return preset
    } catch (error) {
      console.error('Load preset failed:', error)
      return null
    }
  }
  
  async deletePreset(userId: string, presetId: string): Promise<{ success: boolean }> {
    try {
      console.log(`🗑️ 프리셋 삭제: ${userId}/${presetId}`)
      
      await this.presetRepository.delete(presetId)
      return { success: true }
    } catch (error) {
      console.error('Delete preset failed:', error)
      return { success: false }
    }
  }
  
  // UI 편의 메서드들
  async updatePreset(userId: string, presetId: string, updates: {
    name?: string
    description?: string
    featureStates?: boolean[]
    selectedRace?: 'protoss' | 'terran' | 'zerg'
    workerSettings?: {
      workerCountDisplay?: boolean
      includeProducingWorkers?: boolean
      idleWorkerDisplay?: boolean
      workerProductionDetection?: boolean
      workerDeathDetection?: boolean
      gasWorkerCheck?: boolean
    }
  }): Promise<{ success: boolean }> {
    try {
      console.log(`🔄 프리셋 업데이트: ${userId}/${presetId}`)
      
      // 워커 설정 처리 - 부분 업데이트 시 기존 설정과 병합
      let finalWorkerSettings: WorkerSettings | undefined = undefined
      if (updates.workerSettings) {
        const existing = await this.presetRepository.findById(presetId)
        finalWorkerSettings = {
          workerCountDisplay: updates.workerSettings.workerCountDisplay ?? existing?.workerSettings?.workerCountDisplay ?? true,
          includeProducingWorkers: updates.workerSettings.includeProducingWorkers ?? existing?.workerSettings?.includeProducingWorkers ?? true,
          idleWorkerDisplay: updates.workerSettings.idleWorkerDisplay ?? existing?.workerSettings?.idleWorkerDisplay ?? true,
          workerProductionDetection: updates.workerSettings.workerProductionDetection ?? existing?.workerSettings?.workerProductionDetection ?? true,
          workerDeathDetection: updates.workerSettings.workerDeathDetection ?? existing?.workerSettings?.workerDeathDetection ?? true,
          gasWorkerCheck: updates.workerSettings.gasWorkerCheck ?? existing?.workerSettings?.gasWorkerCheck ?? true
        }
      }
      
      const updateRequest: UpdatePresetRequest = {
        id: presetId,
        name: updates.name,
        description: updates.description,
        featureStates: updates.featureStates,
        selectedRace: updates.selectedRace,
        workerSettings: finalWorkerSettings
      }
      
      await this.presetRepository.update(updateRequest)
      return { success: true }
    } catch (error) {
      console.error('Update preset failed:', error)
      return { success: false }
    }
  }
  
  async getSelectedPreset(userId: string): Promise<IPreset | null> {
    try {
      console.log(`🎯 선택된 프리셋 조회: ${userId}`)
      
      const stored = await this.presetRepository.getSelected()
      if (!stored) return null
      
      const preset: IPreset = {
        id: stored.id,
        name: stored.name,
        type: 'game' as const,
        data: {
          description: stored.description,
          featureStates: stored.featureStates,
          selectedRace: stored.selectedRace,
          workerSettings: stored.workerSettings
        },
        createdAt: stored.createdAt,
        updatedAt: stored.updatedAt
      }
      
      return preset
    } catch (error) {
      console.error('Get selected preset failed:', error)
      return null
    }
  }
  
  async setSelectedPreset(userId: string, index: number): Promise<{ success: boolean }> {
    try {
      console.log(`🎯 프리셋 선택 변경: ${userId} → index ${index}`)
      
      await this.presetRepository.updateSelectedIndex(index)
      return { success: true }
    } catch (error) {
      console.error('Set selected preset failed:', error)
      return { success: false }
    }
  }
  
  async getPresetsWithSelection(userId: string): Promise<{
    presets: IPreset[]
    selectedIndex: number
    maxPresets: number
  }> {
    try {
      console.log(`📋 프리셋 목록과 선택정보 조회: ${userId}`)
      
      const collection = await this.presetRepository.loadAll()
      
      const presets: IPreset[] = collection.presets.map(stored => ({
        id: stored.id,
        name: stored.name,
        type: 'game' as const,
        data: {
          description: stored.description,
          featureStates: stored.featureStates,
          selectedRace: stored.selectedRace,
          workerSettings: stored.workerSettings
        },
        createdAt: stored.createdAt,
        updatedAt: stored.updatedAt
      }))
      
      return {
        presets,
        selectedIndex: collection.selectedPresetIndex,
        maxPresets: collection.maxPresets
      }
    } catch (error) {
      console.error('Get presets with selection failed:', error)
      return {
        presets: [],
        selectedIndex: 0,
        maxPresets: 3
      }
    }
  }
  
  // 게임 히스토리 관리
  async saveGameHistory(userId: string, gameData: Omit<IGameHistory, 'id' | 'playedAt'>): Promise<{ success: boolean; id?: string }> {
    try {
      const id = this.generateId('game')
      console.log(`💾 게임 히스토리 저장: ${userId}/${id}`)
      // TODO: 실제 게임 히스토리 저장 로직 구현
      return { success: true, id }
    } catch (error) {
      console.error('Save game history failed:', error)
      return { success: false }
    }
  }
  
  async loadGameHistory(userId: string, limit?: number): Promise<IGameHistory[]> {
    try {
      console.log(`📂 게임 히스토리 로드: ${userId} (limit: ${limit})`)
      // TODO: 실제 게임 히스토리 로드 로직 구현
      return []
    } catch (error) {
      console.error('Load game history failed:', error)
      return []
    }
  }
  
  // 데이터 백업 및 복원
  async exportUserData(userId: string): Promise<{ success: boolean; data?: any }> {
    try {
      console.log(`📤 사용자 데이터 내보내기: ${userId}`)
      // TODO: 실제 데이터 내보내기 로직 구현
      return { success: true, data: {} }
    } catch (error) {
      console.error('Export user data failed:', error)
      return { success: false }
    }
  }
  
  async importUserData(userId: string, importData: any): Promise<{ success: boolean }> {
    try {
      console.log(`📥 사용자 데이터 가져오기: ${userId}`)
      // TODO: 실제 데이터 가져오기 로직 구현
      return { success: true }
    } catch (error) {
      console.error('Import user data failed:', error)
      return { success: false }
    }
  }
  
  // ID 생성 헬퍼 메서드
  private generateId(prefix: string): string {
    const timestamp = Date.now()
    const random = Math.random().toString(36).substr(2, 9)
    return `${prefix}-${timestamp}-${random}`
  }
  
}