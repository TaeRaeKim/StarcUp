import { ICoreCommunicationService, NamedPipeService, CoreCommunicationService } from './core'
import { IShortcutManager, IWindowManager, WindowManager, ShortcutManager, WINDOW_CONFIG } from './window'
import { IIPCService, IPCService, ChannelHandlers } from './ipc'
import { IForegroundWindowService, ForegroundWindowService } from './foreground'
import { IOverlayAutoManager, OverlayAutoManager } from './overlay'
import { ICoreProcessService, CoreProcessService } from './process'
import { DataStorageService } from './storage'
import { AuthService } from './auth'
import { IPresetStateManager, PresetStateManager, IPresetChangeEvent } from './preset'
import { FilePresetRepository } from './storage/repositories/FilePresetRepository'
import { PresetInitMessage, PresetUpdateMessage, calculateWorkerSettingsMask, WorkerSettings } from '../../../src/utils/presetUtils'
import { RaceType, UnitType, RACE_BUILDINGS } from '../../../src/types/enums'

export interface IServiceContainer {
  register<T>(name: string, factory: () => T): void
  registerSingleton<T>(name: string, factory: () => T): void
  resolve<T>(name: string): T
  initialize(): Promise<void>
  dispose(): Promise<void>
}

export class ServiceContainer implements IServiceContainer {
  private services = new Map<string, any>()
  private singletons = new Map<string, any>()
  private initialized = false
  
  // 서비스 등록
  register<T>(name: string, factory: () => T): void {
    this.services.set(name, factory)
  }
  
  registerSingleton<T>(name: string, factory: () => T): void {
    this.register(name, factory)
    this.singletons.set(name, null)
  }
  
  // 서비스 해결
  resolve<T>(name: string): T {
    if (this.singletons.has(name)) {
      let instance = this.singletons.get(name)
      if (!instance) {
        instance = this.services.get(name)()
        this.singletons.set(name, instance)
      }
      return instance
    }
    
    const factory = this.services.get(name)
    if (!factory) {
      throw new Error(`Service ${name} not found`)
    }
    
    return factory()
  }
  
  // 서비스 초기화
  async initialize(): Promise<void> {
    if (this.initialized) {
      throw new Error('ServiceContainer already initialized')
    }
    
    this.registerServices()
    await this.setupServices()
    this.initialized = true
  }
  
  private registerServices(): void {
    // 기본 서비스 등록 (의존성 없음)
    this.registerSingleton('dataStorageService', () => {
      return new DataStorageService()
    })
    
    this.registerSingleton('windowManager', () => {
      return new WindowManager(WINDOW_CONFIG)
    })
    
    this.registerSingleton('shortcutManager', () => {
      return new ShortcutManager(this.resolve('windowManager'))
    })    
    
    this.registerSingleton('namedPipeService', () => {
      const pipeName = process.env.NODE_ENV === 'development' ? 'StarcUp.Dev' : 'StarcUp'
      return new NamedPipeService(pipeName)
    })
    
    this.registerSingleton('coreCommunicationService', () => {
      return new CoreCommunicationService(
        this.resolve('namedPipeService')
      )
    })
    
    this.registerSingleton('authService', () => {
      return new AuthService()
    })
    
    this.registerSingleton('foregroundWindowService', () => {
      return new ForegroundWindowService()
    })

    this.registerSingleton('overlayAutoManager', () => {
      return new OverlayAutoManager(this.resolve('windowManager'))
    })
    
    this.registerSingleton('coreProcessService', () => {
      return new CoreProcessService()
    })
    
    // 프리셋 관련 서비스 등록
    this.registerSingleton('filePresetRepository', () => {
      return new FilePresetRepository()
    })
    
    this.registerSingleton('presetStateManager', () => {
      return new PresetStateManager(this.resolve('filePresetRepository'))
    })
    
    this.registerSingleton('ipcService', () => {
      return new IPCService()
    })
    
    this.registerSingleton('channelHandlers', () => {
      return new ChannelHandlers(
        this.resolve('ipcService'),
        this.resolve('coreCommunicationService'),
        this.resolve('authService'),
        this.resolve('dataStorageService'),
        this.resolve('windowManager'),
        this.resolve('shortcutManager'),
        this.resolve('overlayAutoManager'),
        this.resolve('presetStateManager')
      )
    })
    
    console.log('🔧 모든 서비스 등록 완료')
  }
  
  private async setupServices(): Promise<void> {
    // 서비스 초기화 및 설정
    const channelHandlers = this.resolve<{ setupAllHandlers(): void }>('channelHandlers')
    channelHandlers.setupAllHandlers()
    
    // PresetStateManager 초기화
    await this.initializePresetStateManager()
    
    // CoreCommunicationService와 ForegroundWindowService 연결
    this.setupGameEventHandlers()
    
    // Core 연결 성공 시 프리셋 자동 전송 설정
    this.setupCoreConnectionHandler()
    
    // ShortcutManager에 OverlayAutoManager 연결
    const shortcutManager = this.resolve<IShortcutManager>('shortcutManager')
    const overlayAutoManager = this.resolve<IOverlayAutoManager>('overlayAutoManager')
    shortcutManager.setOverlayAutoManager(overlayAutoManager)
    
    console.log('✅ 모든 서비스 초기화 완료')
  }
  
  private setupGameEventHandlers(): void {
    const coreService = this.resolve<ICoreCommunicationService>('coreCommunicationService')
    const foregroundService = this.resolve<IForegroundWindowService>('foregroundWindowService')
    const overlayAutoManager = this.resolve<IOverlayAutoManager>('overlayAutoManager')
    
    // 게임 감지 시 foreground 모니터링 시작
    coreService.onGameDetected((gameInfo: any) => {
      console.log('🎮 게임 감지됨 - ForegroundWindowService 시작')
      foregroundService.startMonitoring(gameInfo)
    })
    
    // 게임 종료 시 foreground 모니터링 중지
    coreService.onGameEnded(() => {
      console.log('🔚 게임 종료됨 - ForegroundWindowService 중지')
      foregroundService.stopMonitoring()
    })

    // InGame 상태 변경을 OverlayAutoManager로 전달
    coreService.onGameStatusChanged((status: string) => {
      const isInGame = status === 'playing'
      overlayAutoManager.updateInGameStatus(isInGame)
      const windowManager = this.resolve<IWindowManager>('windowManager')
      windowManager.sendToMainWindow('game-status-changed', { status })
      // 오버레이 윈도우에도 게임 상태 변경 이벤트 전송
      windowManager.sendToOverlayWindow('game-status-changed', { status })
    })

    // Foreground 상태 변경을 OverlayAutoManager로 전달
    foregroundService.on('foreground-changed', (event) => {
      overlayAutoManager.updateForegroundStatus(event.isStarcraftInForeground)
    })

    // 윈도우 위치 동기화 이벤트 연결
    coreService.onWindowPositionChanged((position: any) => {
      overlayAutoManager.updateStarCraftWindowPosition(position)
    })

    // WorkerManager 이벤트를 Overlay에 전달 (개발자 도구 콘솔 출력용)
    const windowManager = this.resolve<IWindowManager>('windowManager')
    
    coreService.onWorkerStatusChanged((data: any) => {
      // Overlay 윈도우에 이벤트 전송
      windowManager.sendToOverlayWindow('worker-status-changed', data)
    })

    coreService.onGasBuildingAlert(() => {
      // Overlay 윈도우에 이벤트 전송
      windowManager.sendToOverlayWindow('gas-building-alert', {})
    })

    coreService.onWorkerPresetChanged((data: any) => {
      // Overlay 윈도우에 이벤트 전송
      windowManager.sendToOverlayWindow('worker-preset-changed', data)
    })

    coreService.onSupplyAlert(() => {
      // Overlay 윈도우에 이벤트 전송
      windowManager.sendToOverlayWindow('supply-alert', {})
    })
    
    console.log('🔗 게임 이벤트 핸들러 설정 완료')
  }
  
  private async initializePresetStateManager(): Promise<void> {
    try {
      console.log('🎯 PresetStateManager 초기화 시작')
      
      const presetStateManager = this.resolve<IPresetStateManager>('presetStateManager')
      await presetStateManager.initialize()
      
      // 프리셋 상태 변경 이벤트를 다른 서비스들과 연결
      this.setupPresetEventHandlers(presetStateManager)
      
      console.log('✅ PresetStateManager 초기화 완료')
    } catch (error) {
      console.error('❌ PresetStateManager 초기화 실패:', error)
      throw error
    }
  }
  
  private setupPresetEventHandlers(presetStateManager: IPresetStateManager): void {
    const windowManager = this.resolve<IWindowManager>('windowManager')
    const coreService = this.resolve<ICoreCommunicationService>('coreCommunicationService')
    
    // 프리셋 상태 변경을 UI에 알림
    presetStateManager.onStateChanged(async (event) => {
      console.log('📢 프리셋 상태 변경 감지:', event.type)
      
      // 메인 윈도우에 이벤트 전송
      windowManager.sendToMainWindow('preset-state-changed', {
        type: event.type,
        preset: event.preset,
        state: presetStateManager.getPresetState(),
        timestamp: event.timestamp
      })
      
      // 오버레이 윈도우에도 전송 (필요한 경우)
      if (event.type === 'preset-switched' || event.type === 'feature-toggled') {
        windowManager.sendToOverlayWindow('preset-state-changed', {
          type: event.type,
          preset: event.preset,
          featureStates: event.preset?.featureStates,
          timestamp: event.timestamp
        })
      }
      
      // Core 자동 동기화 (새로운 기능)
      await this.syncPresetToCore(event, presetStateManager, coreService)
    })
    
    console.log('🔗 프리셋 이벤트 핸들러 설정 완료')
  }
  
  private setupCoreConnectionHandler(): void {
    const coreService = this.resolve<ICoreCommunicationService>('coreCommunicationService')
    const presetStateManager = this.resolve<IPresetStateManager>('presetStateManager')
    
    // Core 연결 성공 시 현재 프리셋을 자동으로 전송
    coreService.onConnectionEstablished(async () => {
      console.log('🔗 Core 연결 성공 - 현재 프리셋 자동 전송 시작')
      
      try {
        // 짧은 지연 후 프리셋 전송 (연결 안정화를 위해)
        await new Promise(resolve => setTimeout(resolve, 100))
        
        const currentPreset = presetStateManager.getCurrentPreset()
        if (currentPreset) {
          console.log('📤 현재 프리셋 Core로 전송:', currentPreset.name)
          
          // 프리셋 데이터를 Core 프로토콜로 변환
          const coreMessage = this.convertPresetForCore(currentPreset)
          
          // Core로 전송
          const response = await coreService.sendPresetInit(coreMessage)
          
          if (response.success) {
            console.log('✅ 연결 시 프리셋 전송 완료')
          } else {
            console.warn('⚠️ 연결 시 프리셋 전송 실패:', response.error)
          }
        } else {
          console.warn('⚠️ 전송할 현재 프리셋이 없습니다')
        }
      } catch (error) {
        console.error('❌ 연결 시 프리셋 전송 중 오류:', error)
      }
    })
    
    console.log('🔗 Core 연결 핸들러 설정 완료')
  }
  
  /**
   * PresetStateManager와 Core 간 자동 동기화
   * @param event 프리셋 변경 이벤트
   * @param presetStateManager 프리셋 상태 관리자
   * @param coreService Core 통신 서비스
   */
  private async syncPresetToCore(
    event: IPresetChangeEvent,
    presetStateManager: IPresetStateManager,
    coreService: ICoreCommunicationService
  ): Promise<void> {
    try {
      console.log('🔄 Core 프리셋 동기화 시작:', event.type)
      
      // Core 연결 상태 확인
      if (!coreService.isConnected) {
        console.warn('⚠️ Core 연결이 안된 상태 - 동기화 건너뛰기')
        return
      }
      
      // 프리셋 데이터를 Core 프로토콜로 변환
      const coreMessage = this.convertPresetForCore(presetStateManager.getCurrentPreset())
      
      // Core로 전송
      const response = await coreService.sendPresetInit(coreMessage)
      
      if (response.success) {
        console.log('✅ Core 프리셋 동기화 완료')
      } else {
        console.warn('⚠️ Core 프리셋 동기화 실패:', response.error)
      }
    } catch (error) {
      console.error('❌ Core 프리셋 동기화 중 오류:', error)
      // 에러가 발생해도 UI 동작은 계속 진행
    }
  }
  
  /**
   * 프리셋 데이터를 Core 프로토콜 형식으로 변환
   * @param preset UI 프리셋 데이터
   * @returns Core 프로토콜 메시지
   */
  private convertPresetForCore(preset: any): PresetInitMessage {
    if (!preset) {
      console.log('🔄 빈 프리셋을 Core로 전송')
      return {
        type: 'preset-init',
        timestamp: Date.now(),
        presets: {
          worker: { enabled: false, settingsMask: 1 }, // 최소한 Default는 활성화
          population: { enabled: false, settingsMask: 0 },
          unit: { enabled: false, settingsMask: 0 },
          upgrade: { enabled: false, settingsMask: 0 },
          buildOrder: { enabled: false, settingsMask: 0 }
        }
      }
    }
    
    console.log('🔄 프리셋 데이터 변환:', {
      id: preset.id,
      name: preset.name,
      featureStates: preset.featureStates,
      populationSettings: preset.populationSettings
    })
    
    return {
      type: 'preset-init', // 전체 상태 전송
      timestamp: Date.now(),
      presets: {
        worker: {
          enabled: preset.featureStates?.[0] || false,
          settingsMask: this.calculateWorkerSettingsMask(preset.workerSettings)
        },
        population: {
          enabled: preset.featureStates?.[1] || false,
          settingsMask: 0, // 인구수는 비트마스크 대신 settings 객체 사용
          settings: (() => {
            const converted = this.convertPopulationSettingsForCore(preset.populationSettings)
            console.log('🔍 전송할 인구수 설정:', {
              original: preset.populationSettings,
              converted: converted
            })
            return converted
          })()
        },
        unit: {
          enabled: preset.featureStates?.[2] || false,
          settingsMask: 0 // 추후 구현
        },
        upgrade: {
          enabled: preset.featureStates?.[3] || false,
          settingsMask: 0 // 추후 구현
        },
        buildOrder: {
          enabled: preset.featureStates?.[4] || false,
          settingsMask: 0 // 추후 구현
        }
      }
    }
  }
  
  /**
   * 인구수 설정을 Core 형식으로 변환 (enum 문자열을 int로 변환하고 name 필드 제거)
   * @param populationSettings UI 인구수 설정
   * @returns Core 형식 인구수 설정
   */
  private convertPopulationSettingsForCore(populationSettings: any): any {
    if (!populationSettings) {
      return null
    }

    const converted = {
      mode: populationSettings.mode, // "fixed" 또는 "building" - 문자열 그대로
      fixedSettings: populationSettings.fixedSettings,
      buildingSettings: populationSettings.buildingSettings ? {
        race: populationSettings.buildingSettings.race,
        trackedBuildings: populationSettings.buildingSettings.trackedBuildings?.map((building: any) => ({
          buildingType: building.buildingType,
          multiplier: building.multiplier,
          enabled: building.enabled
          // name 필드는 제거됨
        })) || []
      } : undefined
    }

    console.log('🔄 인구수 설정 변환:', {
      원본_race: populationSettings.buildingSettings?.race,
      변환된_race: converted.buildingSettings?.race,
      원본_buildings: populationSettings.buildingSettings?.trackedBuildings?.length || 0,
      변환된_buildings: converted.buildingSettings?.trackedBuildings?.length || 0
    })

    return converted
  }



  /**
   * 일꾼 설정을 비트마스크로 변환
   * @param workerSettings 일꾼 설정 객체
   * @returns 비트마스크 값
   */
  private calculateWorkerSettingsMask(workerSettings: WorkerSettings | undefined): number {
    if (!workerSettings) {
      return 1 // 기본값: Default만 활성화
    }
    
    try {
      const mask = calculateWorkerSettingsMask(workerSettings)
      console.log('🔢 일꾼 설정 비트마스크 계산:', {
        settings: workerSettings,
        mask: mask,
        binary: '0b' + mask.toString(2).padStart(8, '0')
      })
      return mask || 1 // 최소한 Default는 활성화
    } catch (error) {
      console.warn('⚠️ 일꾼 설정 비트마스크 계산 실패:', error)
      return 1 // 안전한 기본값
    }
  }
  
  // 서비스 정리
  async dispose(): Promise<void> {
    if (!this.initialized) return
    
    try {
      // Core 연결 종료
      const coreService = this.resolve<ICoreCommunicationService>('coreCommunicationService')
      if (coreService && typeof coreService.stopConnection === 'function') {
        await coreService.stopConnection()
      }
      
      // Core 프로세스 종료
      const coreProcessService = this.resolve<ICoreProcessService>('coreProcessService')
      if (coreProcessService && typeof coreProcessService.stopCoreProcess === 'function') {
        await coreProcessService.stopCoreProcess()
      }
      
      
      // 단축키 해제
      const shortcutManager = this.resolve<IShortcutManager>('shortcutManager')
      if (shortcutManager && typeof shortcutManager.unregisterAllShortcuts === 'function') {
        shortcutManager.unregisterAllShortcuts()
      }
      
      // IPC 핸들러 정리
      const ipcService = this.resolve<IIPCService>('ipcService')
      if (ipcService && typeof ipcService.removeAllHandlers === 'function') {
        ipcService.removeAllHandlers()
      }
      
      // 윈도우 정리
      const windowManager = this.resolve<IWindowManager>('windowManager')
      if (windowManager && typeof windowManager.cleanup === 'function') {
        windowManager.cleanup()
      }
      
      // ForegroundWindowService 정리
      const foregroundService = this.resolve<IForegroundWindowService>('foregroundWindowService')
      if (foregroundService && typeof foregroundService.dispose === 'function') {
        foregroundService.dispose()
      }

      // OverlayAutoManager 정리
      const overlayAutoManager = this.resolve<IOverlayAutoManager>('overlayAutoManager')
      if (overlayAutoManager && typeof overlayAutoManager.dispose === 'function') {
        overlayAutoManager.dispose()
      }
      
      // PresetStateManager 정리
      const presetStateManager = this.resolve<IPresetStateManager>('presetStateManager')
      if (presetStateManager && typeof presetStateManager.dispose === 'function') {
        await presetStateManager.dispose()
      }
      
      // 싱글톤 서비스들 정리
      for (const [, instance] of this.singletons) {
        if (instance && typeof instance.dispose === 'function') {
          await instance.dispose()
        }
      }
      
      this.services.clear()
      this.singletons.clear()
      this.initialized = false
      
      console.log('✅ ServiceContainer 정리 완료')
    } catch (error) {
      console.error('❌ ServiceContainer 정리 중 오류:', error)
    }
  }
}

// 전역 서비스 컨테이너
export const serviceContainer = new ServiceContainer()