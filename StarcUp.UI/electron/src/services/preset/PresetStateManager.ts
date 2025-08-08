import { EventEmitter } from 'events'
import { 
  IPresetStateManager, 
  IPreset, 
  IPresetState, 
  IPresetChangeEvent,
  IPresetSettingsUpdate,
  DebounceOptions,
  IPerformanceMetrics,
  IPresetStateManagerState
} from './interfaces'
import { IPresetRepository, StoredPreset } from '../storage/repositories/IPresetRepository'

/**
 * 프리셋 상태 관리자 구현체
 * 
 * 주요 기능:
 * - 중앙화된 프리셋 상태 관리
 * - 이벤트 기반 상태 변경 알림
 * - FilePresetRepository와 연동
 * - 성능 최적화 (캐싱, 디바운싱)
 * - 에러 복구 로직
 */
export class PresetStateManager extends EventEmitter implements IPresetStateManager {
  private state: IPresetStateManagerState
  private repository: IPresetRepository
  private debounceTimers: Map<string, NodeJS.Timeout> = new Map()
  private performanceMetrics: IPerformanceMetrics[] = []
  
  // 디바운스 기본 설정
  private readonly DEFAULT_DEBOUNCE_DELAY = 300
  private readonly MAX_DEBOUNCE_WAIT = 1000
  
  constructor(repository: IPresetRepository) {
    super()
    this.repository = repository
    this.state = {
      currentPresetId: null,
      allPresets: new Map(),
      selectedIndex: 0,
      isInitialized: false,
      isLoading: false,
      lastSyncTime: 0
    }
    
    this.setupEventHandlers()
  }
  
  // ==================== 공개 메서드 ====================
  
  async initialize(): Promise<void> {
    const startTime = Date.now()
    
    try {
      console.log('🚀 PresetStateManager 초기화 시작')
      
      if (this.state.isInitialized) {
        console.warn('⚠️ PresetStateManager가 이미 초기화되었습니다')
        return
      }
      
      this.state.isLoading = true
      
      // Repository 초기화
      await this.repository.initialize()
      
      // 초기 데이터 로드
      await this.loadAllPresets()
      
      // 현재 선택된 프리셋 설정
      await this.loadCurrentPreset()
      
      this.state.isInitialized = true
      this.state.isLoading = false
      
      const metrics: IPerformanceMetrics = {
        operationName: 'initialize',
        executionTime: Date.now() - startTime,
        timestamp: new Date(),
        success: true
      }
      this.recordMetrics(metrics)
      
      console.log('✅ PresetStateManager 초기화 완료', {
        presetCount: this.state.allPresets.size,
        currentPreset: this.state.currentPresetId,
        initTime: metrics.executionTime + 'ms'
      })
      
      // 초기화 완료 이벤트 발행
      this.emitStateChange('presets-loaded', null, {
        allPresets: this.getAllPresets()
      })
      
    } catch (error) {
      this.state.isLoading = false
      const errorMsg = error instanceof Error ? error.message : String(error)
      
      const metrics: IPerformanceMetrics = {
        operationName: 'initialize',
        executionTime: Date.now() - startTime,
        timestamp: new Date(),
        success: false,
        error: errorMsg
      }
      this.recordMetrics(metrics)
      
      console.error('❌ PresetStateManager 초기화 실패:', error)
      throw new Error(`PresetStateManager 초기화 실패: ${errorMsg}`)
    }
  }
  
  getCurrentPreset(): IPreset | null {
    if (!this.state.currentPresetId) {
      return null
    }
    
    return this.state.allPresets.get(this.state.currentPresetId) || null
  }
  
  getPresetState(): IPresetState {
    return {
      currentPreset: this.getCurrentPreset(),
      allPresets: this.getAllPresets(),
      selectedPresetIndex: this.state.selectedIndex,
      isLoading: this.state.isLoading,
      lastUpdated: new Date(this.state.lastSyncTime)
    }
  }
  
  getAllPresets(): IPreset[] {
    return Array.from(this.state.allPresets.values())
      .sort((a, b) => a.createdAt.getTime() - b.createdAt.getTime())
  }
  
  async switchPreset(presetId: string): Promise<void> {
    const startTime = Date.now()
    
    try {
      console.log('🔄 프리셋 전환 시작:', presetId)
      
      if (!this.state.isInitialized) {
        throw new Error('PresetStateManager가 초기화되지 않았습니다')
      }
      
      const targetPreset = this.state.allPresets.get(presetId)
      if (!targetPreset) {
        throw new Error(`프리셋을 찾을 수 없습니다: ${presetId}`)
      }
      
      const previousPresetId = this.state.currentPresetId
      
      // Repository에 선택된 인덱스 업데이트
      const allPresets = this.getAllPresets()
      const newIndex = allPresets.findIndex(p => p.id === presetId)
      
      if (newIndex === -1) {
        throw new Error(`프리셋 인덱스를 찾을 수 없습니다: ${presetId}`)
      }
      
      await this.repository.updateSelectedIndex(newIndex)
      
      // 내부 상태 업데이트
      this.state.currentPresetId = presetId
      this.state.selectedIndex = newIndex
      this.state.lastSyncTime = Date.now()
      
      const metrics: IPerformanceMetrics = {
        operationName: 'switchPreset',
        executionTime: Date.now() - startTime,
        timestamp: new Date(),
        success: true
      }
      this.recordMetrics(metrics)
      
      console.log('✅ 프리셋 전환 완료:', {
        from: previousPresetId,
        to: presetId,
        name: targetPreset.name,
        switchTime: metrics.executionTime + 'ms'
      })
      
      // 이벤트 발행
      this.emitStateChange('preset-switched', presetId, {
        previousPresetId
      })
      
    } catch (error) {
      const errorMsg = error instanceof Error ? error.message : String(error)
      
      const metrics: IPerformanceMetrics = {
        operationName: 'switchPreset',
        executionTime: Date.now() - startTime,
        timestamp: new Date(),
        success: false,
        error: errorMsg
      }
      this.recordMetrics(metrics)
      
      console.error('❌ 프리셋 전환 실패:', error)
      throw error
    }
  }
  
  async updatePresetSettings(presetType: string, settings: any): Promise<void> {
    const startTime = Date.now()
    
    try {
      console.log('⚙️ 프리셋 설정 업데이트:', { presetType, settings })
      
      if (!this.state.currentPresetId) {
        throw new Error('현재 선택된 프리셋이 없습니다')
      }
      
      const currentPreset = this.getCurrentPreset()
      if (!currentPreset) {
        throw new Error('현재 프리셋 데이터를 찾을 수 없습니다')
      }
      
      // 설정 타입에 따른 업데이트 처리
      let updateData: any = {}
      
      switch (presetType) {
        case 'worker':
          updateData = { workerSettings: { ...currentPreset.workerSettings, ...settings } }
          break
        case 'population':
          updateData = { populationSettings: { ...currentPreset.populationSettings, ...settings } }
          console.log('🏘️ 인구수 설정 업데이트:', updateData)
          break
        case 'race':
          updateData = { selectedRace: settings.selectedRace }
          break
        case 'basic':
          // 프리셋 기본 정보 (이름, 설명) 업데이트
          updateData = {}
          if (settings.name !== undefined) updateData.name = settings.name
          if (settings.description !== undefined) updateData.description = settings.description
          console.log('📝 프리셋 기본 정보 업데이트:', updateData)
          break
        default:
          console.warn('⚠️ 지원되지 않는 프리셋 타입:', presetType)
          updateData = settings
          break
      }
      
      // Repository 업데이트 (디바운싱 적용)
      await this.debouncedRepositoryUpdate(currentPreset.id, updateData)
      
      const metrics: IPerformanceMetrics = {
        operationName: 'updatePresetSettings',
        executionTime: Date.now() - startTime,
        timestamp: new Date(),
        success: true
      }
      this.recordMetrics(metrics)
      
      console.log('✅ 프리셋 설정 업데이트 완료:', {
        presetId: currentPreset.id,
        type: presetType,
        updateTime: metrics.executionTime + 'ms'
      })
      
    } catch (error) {
      const errorMsg = error instanceof Error ? error.message : String(error)
      
      const metrics: IPerformanceMetrics = {
        operationName: 'updatePresetSettings',
        executionTime: Date.now() - startTime,
        timestamp: new Date(),
        success: false,
        error: errorMsg
      }
      this.recordMetrics(metrics)
      
      console.error('❌ 프리셋 설정 업데이트 실패:', error)
      throw error
    }
  }
  
  async toggleFeature(featureIndex: number, enabled: boolean): Promise<void> {
    const startTime = Date.now()
    
    try {
      console.log('🎛️ 기능 토글:', { featureIndex, enabled })
      
      if (!this.state.currentPresetId) {
        throw new Error('현재 선택된 프리셋이 없습니다')
      }
      
      const currentPreset = this.getCurrentPreset()
      if (!currentPreset) {
        throw new Error('현재 프리셋 데이터를 찾을 수 없습니다')
      }
      
      if (featureIndex < 0 || featureIndex >= currentPreset.featureStates.length) {
        throw new Error(`잘못된 기능 인덱스: ${featureIndex}`)
      }
      
      // 새로운 기능 상태 배열 생성
      const newFeatureStates = [...currentPreset.featureStates]
      newFeatureStates[featureIndex] = enabled
      
      // Repository 업데이트 (디바운싱 적용)
      await this.debouncedRepositoryUpdate(currentPreset.id, {
        featureStates: newFeatureStates
      })
      
      const metrics: IPerformanceMetrics = {
        operationName: 'toggleFeature',
        executionTime: Date.now() - startTime,
        timestamp: new Date(),
        success: true
      }
      this.recordMetrics(metrics)
      
      console.log('✅ 기능 토글 완료:', {
        presetId: currentPreset.id,
        feature: featureIndex,
        enabled,
        toggleTime: metrics.executionTime + 'ms'
      })
      
    } catch (error) {
      const errorMsg = error instanceof Error ? error.message : String(error)
      
      const metrics: IPerformanceMetrics = {
        operationName: 'toggleFeature',
        executionTime: Date.now() - startTime,
        timestamp: new Date(),
        success: false,
        error: errorMsg
      }
      this.recordMetrics(metrics)
      
      console.error('❌ 기능 토글 실패:', error)
      throw error
    }
  }
  
  onStateChanged(callback: (event: IPresetChangeEvent) => void): () => void {
    console.log('👂 상태 변경 리스너 등록')
    
    this.on('state-changed', callback)
    
    // 언구독 함수 반환
    return () => {
      console.log('👋 상태 변경 리스너 해제')
      this.off('state-changed', callback)
    }
  }
  
  async dispose(): Promise<void> {
    const startTime = Date.now()
    
    try {
      console.log('🔄 PresetStateManager 정리 시작')
      
      // 디바운스 타이머 정리
      this.clearAllDebounceTimers()
      
      // 이벤트 리스너 정리
      this.removeAllListeners()
      
      // 상태 초기화
      this.state = {
        currentPresetId: null,
        allPresets: new Map(),
        selectedIndex: 0,
        isInitialized: false,
        isLoading: false,
        lastSyncTime: 0
      }
      
      const metrics: IPerformanceMetrics = {
        operationName: 'dispose',
        executionTime: Date.now() - startTime,
        timestamp: new Date(),
        success: true
      }
      this.recordMetrics(metrics)
      
      console.log('✅ PresetStateManager 정리 완료:', {
        disposeTime: metrics.executionTime + 'ms'
      })
      
    } catch (error) {
      const errorMsg = error instanceof Error ? error.message : String(error)
      console.error('❌ PresetStateManager 정리 실패:', error)
      
      const metrics: IPerformanceMetrics = {
        operationName: 'dispose',
        executionTime: Date.now() - startTime,
        timestamp: new Date(),
        success: false,
        error: errorMsg
      }
      this.recordMetrics(metrics)
      
      throw error
    }
  }
  
  // ==================== 디버그 메서드 ====================
  
  getPerformanceMetrics(): IPerformanceMetrics[] {
    return this.performanceMetrics.slice(-50) // 최근 50개만 반환
  }
  
  getDebugInfo(): any {
    return {
      state: {
        isInitialized: this.state.isInitialized,
        isLoading: this.state.isLoading,
        currentPresetId: this.state.currentPresetId,
        presetCount: this.state.allPresets.size,
        selectedIndex: this.state.selectedIndex,
        lastSyncTime: new Date(this.state.lastSyncTime).toISOString()
      },
      performance: {
        recentMetrics: this.performanceMetrics.slice(-10),
        debounceTimers: this.debounceTimers.size
      }
    }
  }
  
  // ==================== 내부 메서드 ====================
  
  private async loadAllPresets(): Promise<void> {
    console.log('📥 모든 프리셋 로드 시작')
    
    const collection = await this.repository.loadAll()
    
    // 내부 캐시 업데이트
    this.state.allPresets.clear()
    collection.presets.forEach(storedPreset => {
      const preset = this.convertStoredPresetToIPreset(storedPreset)
      this.state.allPresets.set(preset.id, preset)
    })
    
    this.state.selectedIndex = collection.selectedPresetIndex
    this.state.lastSyncTime = Date.now()
    
    console.log('📥 모든 프리셋 로드 완료:', {
      count: this.state.allPresets.size,
      selectedIndex: this.state.selectedIndex
    })
  }
  
  private async loadCurrentPreset(): Promise<void> {
    console.log('📍 현재 프리셋 로드 시작')
    
    const selectedPreset = await this.repository.getSelected()
    if (selectedPreset) {
      this.state.currentPresetId = selectedPreset.id
      console.log('📍 현재 프리셋 설정:', selectedPreset.name)
    } else {
      console.warn('⚠️ 선택된 프리셋이 없습니다')
    }
  }
  
  private convertStoredPresetToIPreset(storedPreset: StoredPreset): IPreset {
    return {
      id: storedPreset.id,
      name: storedPreset.name,
      description: storedPreset.description,
      featureStates: storedPreset.featureStates,
      selectedRace: storedPreset.selectedRace,
      workerSettings: storedPreset.workerSettings,
      populationSettings: storedPreset.populationSettings,
      createdAt: storedPreset.createdAt,
      updatedAt: storedPreset.updatedAt
    }
  }
  
  private async debouncedRepositoryUpdate(presetId: string, updateData: any): Promise<void> {
    const debounceKey = `update-${presetId}`
    
    // 기존 타이머 제거
    if (this.debounceTimers.has(debounceKey)) {
      clearTimeout(this.debounceTimers.get(debounceKey)!)
      this.debounceTimers.delete(debounceKey)
    }
    
    // 새로운 타이머 설정
    const timer = setTimeout(async () => {
      try {
        await this.repository.update({
          id: presetId,
          ...updateData
        })
        
        // 로컬 캐시 업데이트
        await this.syncWithRepository()
        
        // 이벤트 발행
        this.emitStateChange('settings-updated', presetId, {
          settings: updateData
        })
        
      } catch (error) {
        console.error('❌ 디바운스된 Repository 업데이트 실패:', error)
      } finally {
        this.debounceTimers.delete(debounceKey)
      }
    }, this.DEFAULT_DEBOUNCE_DELAY)
    
    this.debounceTimers.set(debounceKey, timer)
  }
  
  private async syncWithRepository(): Promise<void> {
    try {
      await this.loadAllPresets()
      await this.loadCurrentPreset()
    } catch (error) {
      console.error('❌ Repository 동기화 실패:', error)
    }
  }
  
  private emitStateChange(type: IPresetChangeEvent['type'], presetId: string | null, changes: any = {}): void {
    const preset = presetId ? this.state.allPresets.get(presetId) : null
    
    const event: IPresetChangeEvent = {
      type,
      presetId: presetId || '',
      preset: preset || null,
      changes,
      timestamp: new Date()
    }
    
    this.emit('state-changed', event)
    
    console.log(`📢 상태 변경 이벤트 발행: ${type}`, {
      presetId,
      presetName: preset?.name,
      changes
    })
  }
  
  private clearAllDebounceTimers(): void {
    for (const [key, timer] of this.debounceTimers) {
      clearTimeout(timer)
    }
    this.debounceTimers.clear()
    console.log('🔄 모든 디바운스 타이머 정리 완료')
  }
  
  private recordMetrics(metrics: IPerformanceMetrics): void {
    this.performanceMetrics.push(metrics)
    
    // 최대 100개 메트릭만 보관 (메모리 관리)
    if (this.performanceMetrics.length > 100) {
      this.performanceMetrics = this.performanceMetrics.slice(-100)
    }
    
    // 성능이 좋지 않은 경우 경고 로그
    if (metrics.executionTime > 1000) {
      console.warn(`⚠️ 성능 경고: ${metrics.operationName} 실행시간 ${metrics.executionTime}ms`)
    }
  }
  
  private setupEventHandlers(): void {
    // 예상치 못한 에러 처리
    this.on('error', (error: Error) => {
      console.error('❌ PresetStateManager 에러:', error)
    })
    
    // 메모리 누수 방지를 위한 리스너 수 제한
    this.setMaxListeners(20)
  }
}