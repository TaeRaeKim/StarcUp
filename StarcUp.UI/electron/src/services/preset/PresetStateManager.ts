import { EventEmitter } from 'events'
import { 
  IPresetStateManager, 
  IPreset, 
  IPresetState, 
  IPresetChangeEvent,
  IPresetSettingsUpdate,
  IBatchPresetUpdate,
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
 * - 배치 업데이트 지원
 * - 에러 복구 로직
 */
export class PresetStateManager extends EventEmitter implements IPresetStateManager {
  private state: IPresetStateManagerState
  private repository: IPresetRepository
  private performanceMetrics: IPerformanceMetrics[] = []
  
  constructor(repository: IPresetRepository) {
    super()
    this.repository = repository
    this.state = {
      currentPresetId: null,
      allPresets: new Map(),
      selectedIndex: 0,
      isInitialized: false,
      isLoading: false,
      lastSyncTime: 0,
      tempSettings: new Map()
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
  
  getState(): IPresetState {
    return this.getPresetState()
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
      
      // Repository 즉시 업데이트
      await this.repository.update({
        id: currentPreset.id,
        ...updateData
      })
      
      // 로컬 캐시 동기화
      await this.syncWithRepository()
      
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
      
      // 이벤트 발행
      this.emitStateChange('settings-updated', currentPreset.id, {
        settings: updateData
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
  
  async updatePresetBatch(updates: IBatchPresetUpdate): Promise<void> {
    const startTime = Date.now()
    
    try {
      console.log('🔄 프리셋 배치 업데이트:', updates)
      
      if (!this.state.currentPresetId) {
        throw new Error('현재 선택된 프리셋이 없습니다')
      }
      
      const currentPreset = this.getCurrentPreset()
      if (!currentPreset) {
        throw new Error('현재 프리셋 데이터를 찾을 수 없습니다')
      }
      
      // 모든 변경사항을 한 번에 Repository에 적용
      await this.repository.update({
        id: currentPreset.id,
        ...updates
      })
      
      // 로컬 캐시 동기화
      await this.syncWithRepository()
      
      const metrics: IPerformanceMetrics = {
        operationName: 'updatePresetBatch',
        executionTime: Date.now() - startTime,
        timestamp: new Date(),
        success: true
      }
      this.recordMetrics(metrics)
      
      console.log('✅ 프리셋 배치 업데이트 완료:', {
        presetId: currentPreset.id,
        updates,
        updateTime: metrics.executionTime + 'ms'
      })
      
      // 이벤트 발행
      this.emitStateChange('settings-updated', currentPreset.id, {
        settings: updates
      })
      
    } catch (error) {
      const errorMsg = error instanceof Error ? error.message : String(error)
      
      const metrics: IPerformanceMetrics = {
        operationName: 'updatePresetBatch',
        executionTime: Date.now() - startTime,
        timestamp: new Date(),
        success: false,
        error: errorMsg
      }
      this.recordMetrics(metrics)
      
      console.error('❌ 프리셋 배치 업데이트 실패:', error)
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
      
      // Repository 즉시 업데이트
      await this.repository.update({
        id: currentPreset.id,
        featureStates: newFeatureStates
      })
      
      // 로컬 캐시 동기화
      await this.syncWithRepository()
      
      // 이벤트 발행
      this.emitStateChange('feature-toggled', currentPreset.id, {
        toggledFeature: { index: featureIndex, enabled }
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
  
  // ==================== 임시 저장 관리 메서드 ====================
  
  updateTempSettings(presetType: string, settings: any): void {
    console.log('💾 임시 설정 업데이트:', { presetType, settings })
    
    // 현재 저장된 임시 설정 가져오기
    const currentTempSettings = this.state.tempSettings.get(presetType) || {}
    
    // 병합하여 저장
    this.state.tempSettings.set(presetType, {
      ...currentTempSettings,
      ...settings
    })
    
    // 임시 설정 변경 이벤트 발행
    this.emitStateChange('temp-settings-updated', this.state.currentPresetId, {
      presetType,
      tempSettings: this.state.tempSettings.get(presetType)
    })
  }
  
  getTempSettings(presetType: string): any | null {
    return this.state.tempSettings.get(presetType) || null
  }
  
  clearTempSettings(presetType?: string): void {
    console.log('🗑️ 임시 설정 초기화:', presetType || '전체')
    
    if (presetType) {
      this.state.tempSettings.delete(presetType)
    } else {
      this.state.tempSettings.clear()
    }
    
    // 임시 설정 초기화 이벤트 발행
    this.emitStateChange('temp-settings-cleared', this.state.currentPresetId, {
      presetType: presetType || 'all'
    })
  }
  
  hasTempChanges(presetType?: string): boolean {
    if (presetType) {
      return this.state.tempSettings.has(presetType)
    }
    return this.state.tempSettings.size > 0
  }
  
  async applyTempSettings(): Promise<void> {
    const startTime = Date.now()
    
    try {
      console.log('📝 임시 설정 적용 시작')
      
      if (this.state.tempSettings.size === 0) {
        console.log('ℹ️ 적용할 임시 설정이 없습니다')
        return
      }
      
      if (!this.state.currentPresetId) {
        throw new Error('현재 선택된 프리셋이 없습니다')
      }
      
      const currentPreset = this.getCurrentPreset()
      if (!currentPreset) {
        throw new Error('현재 프리셋 데이터를 찾을 수 없습니다')
      }
      
      // 모든 임시 설정을 배치 업데이트로 변환
      const batchUpdate: IBatchPresetUpdate = {}
      
      for (const [presetType, settings] of this.state.tempSettings) {
        switch (presetType) {
          case 'worker':
            batchUpdate.workerSettings = { ...currentPreset.workerSettings, ...settings }
            break
          case 'population':
            batchUpdate.populationSettings = { ...currentPreset.populationSettings, ...settings }
            break
          case 'race':
            batchUpdate.selectedRace = settings.selectedRace
            break
          case 'basic':
            if (settings.name !== undefined) batchUpdate.name = settings.name
            if (settings.description !== undefined) batchUpdate.description = settings.description
            break
        }
      }
      
      // 배치 업데이트 실행
      await this.updatePresetBatch(batchUpdate)
      
      // 임시 설정 초기화
      this.state.tempSettings.clear()
      
      const metrics: IPerformanceMetrics = {
        operationName: 'applyTempSettings',
        executionTime: Date.now() - startTime,
        timestamp: new Date(),
        success: true
      }
      this.recordMetrics(metrics)
      
      console.log('✅ 임시 설정 적용 완료:', {
        presetId: currentPreset.id,
        applyTime: metrics.executionTime + 'ms'
      })
      
      // 임시 설정 적용 완료 이벤트 발행
      this.emitStateChange('temp-settings-applied', this.state.currentPresetId, {
        appliedSettings: batchUpdate
      })
      
    } catch (error) {
      const errorMsg = error instanceof Error ? error.message : String(error)
      
      const metrics: IPerformanceMetrics = {
        operationName: 'applyTempSettings',
        executionTime: Date.now() - startTime,
        timestamp: new Date(),
        success: false,
        error: errorMsg
      }
      this.recordMetrics(metrics)
      
      console.error('❌ 임시 설정 적용 실패:', error)
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
      
      // 이벤트 리스너 정리
      this.removeAllListeners()
      
      // 상태 초기화
      this.state = {
        currentPresetId: null,
        allPresets: new Map(),
        selectedIndex: 0,
        isInitialized: false,
        isLoading: false,
        lastSyncTime: 0,
        tempSettings: new Map()
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
        recentMetrics: this.performanceMetrics.slice(-10)
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

  // ==================== Pro 기능 해제 메서드 ====================

  /**
   * 구독 기간 종료 시 모든 프리셋에서 Pro 기능을 해제하고 데이터베이스에 저장합니다.
   */
  async sanitizeAllPresetsForNonPro(): Promise<void> {
    const startTime = Date.now()
    
    try {
      console.log('🔒 모든 프리셋 Pro 기능 해제 시작...')
      
      // Repository를 통해 데이터베이스에서 직접 Pro 기능 해제 및 저장
      await this.repository.sanitizeAllPresetsForNonPro()
      
      // 메모리 상태 새로고침
      await this.refreshState()
      
      const metrics: IPerformanceMetrics = {
        operationName: 'sanitizeAllPresetsForNonPro',
        executionTime: Date.now() - startTime,
        timestamp: new Date(),
        success: true
      }
      this.recordMetrics(metrics)
      
      console.log('✅ 모든 프리셋 Pro 기능 해제 완료')
      
      // Pro 기능 해제 완료 이벤트 발행
      this.emitStateChange('presets-sanitized', null, {
        type: 'all-presets',
        message: '모든 프리셋에서 Pro 기능이 해제되었습니다.'
      })
      
    } catch (error) {
      const errorMsg = error instanceof Error ? error.message : String(error)
      
      const metrics: IPerformanceMetrics = {
        operationName: 'sanitizeAllPresetsForNonPro',
        executionTime: Date.now() - startTime,
        timestamp: new Date(),
        success: false,
        error: errorMsg
      }
      this.recordMetrics(metrics)
      
      console.error('❌ 모든 프리셋 Pro 기능 해제 실패:', error)
      throw error
    }
  }

  /**
   * 특정 프리셋의 Pro 기능을 해제하고 데이터베이스에 저장합니다.
   */
  async sanitizePresetForNonPro(presetId: string): Promise<StoredPreset> {
    const startTime = Date.now()
    
    try {
      console.log(`🔒 프리셋 "${presetId}" Pro 기능 해제 시작...`)
      
      // Repository를 통해 데이터베이스에서 직접 Pro 기능 해제 및 저장
      const sanitizedPreset = await this.repository.sanitizePresetForNonPro(presetId)
      
      // 현재 프리셋이 변경된 경우 메모리 상태 업데이트
      if (this.state.currentPresetId === presetId) {
        this.state.allPresets.set(presetId, {
          id: sanitizedPreset.id,
          name: sanitizedPreset.name,
          description: sanitizedPreset.description,
          selectedRace: sanitizedPreset.selectedRace,
          featureStates: sanitizedPreset.featureStates,
          workerSettings: sanitizedPreset.workerSettings,
          populationSettings: sanitizedPreset.populationSettings,
          createdAt: sanitizedPreset.createdAt,
          updatedAt: sanitizedPreset.updatedAt
        })
        this.state.lastSyncTime = Date.now()
      }
      
      // 전체 프리셋 목록 새로고침
      await this.refreshState()
      
      const metrics: IPerformanceMetrics = {
        operationName: 'sanitizePresetForNonPro',
        executionTime: Date.now() - startTime,
        timestamp: new Date(),
        success: true
      }
      this.recordMetrics(metrics)
      
      console.log(`✅ 프리셋 "${presetId}" Pro 기능 해제 완료`)
      
      // Pro 기능 해제 완료 이벤트 발행
      this.emitStateChange('preset-sanitized', presetId, {
        type: 'single-preset',
        presetId,
        sanitizedPreset,
        message: `프리셋 "${sanitizedPreset.name}"에서 Pro 기능이 해제되었습니다.`
      })
      
      return sanitizedPreset
      
    } catch (error) {
      const errorMsg = error instanceof Error ? error.message : String(error)
      
      const metrics: IPerformanceMetrics = {
        operationName: 'sanitizePresetForNonPro',
        executionTime: Date.now() - startTime,
        timestamp: new Date(),
        success: false,
        error: errorMsg
      }
      this.recordMetrics(metrics)
      
      console.error(`❌ 프리셋 "${presetId}" Pro 기능 해제 실패:`, error)
      throw error
    }
  }

  /**
   * 메모리 상태를 데이터베이스와 다시 동기화합니다.
   */
  async refreshState(): Promise<void> {
    const startTime = Date.now()
    
    try {
      console.log('🔄 프리셋 상태 새로고침 시작...')
      
      // 데이터베이스에서 최신 데이터 로드
      const collection = await this.repository.loadAll()
      
      // 상태 업데이트 - Map 기반으로 변환
      this.state.allPresets.clear()
      collection.presets.forEach(preset => {
        this.state.allPresets.set(preset.id, {
          id: preset.id,
          name: preset.name,
          description: preset.description,
          selectedRace: preset.selectedRace,
          featureStates: preset.featureStates,
          workerSettings: preset.workerSettings,
          populationSettings: preset.populationSettings,
          createdAt: preset.createdAt,
          updatedAt: preset.updatedAt
        })
      })
      
      this.state.selectedIndex = collection.selectedPresetIndex
      this.state.currentPresetId = collection.presets[collection.selectedPresetIndex]?.id || null
      this.state.lastSyncTime = Date.now()
      this.state.isLoading = false
      
      const metrics: IPerformanceMetrics = {
        operationName: 'refreshState',
        executionTime: Date.now() - startTime,
        timestamp: new Date(),
        success: true
      }
      this.recordMetrics(metrics)
      
      console.log('✅ 프리셋 상태 새로고침 완료')
      
      // 상태 새로고침 완료 이벤트 발행
      this.emitStateChange('state-refreshed', this.state.currentPresetId, {
        refreshTime: metrics.executionTime,
        presetCount: this.state.allPresets.size
      })
      
    } catch (error) {
      const errorMsg = error instanceof Error ? error.message : String(error)
      
      const metrics: IPerformanceMetrics = {
        operationName: 'refreshState',
        executionTime: Date.now() - startTime,
        timestamp: new Date(),
        success: false,
        error: errorMsg
      }
      this.recordMetrics(metrics)
      
      console.error('❌ 프리셋 상태 새로고침 실패:', error)
      throw error
    }
  }
}