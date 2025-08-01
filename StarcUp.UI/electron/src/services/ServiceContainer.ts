import { ICoreCommunicationService, NamedPipeService, CoreCommunicationService } from './core'
import { IShortcutManager, IWindowManager, WindowManager, ShortcutManager, WINDOW_CONFIG } from './window'
import { IIPCService, IPCService, ChannelHandlers } from './ipc'
import { IForegroundWindowService, ForegroundWindowService } from './foreground'
import { IOverlayAutoManager, OverlayAutoManager } from './overlay'
import { DataStorageService } from './storage'
import { AuthService } from './auth'

export interface IServiceContainer {
  register<T>(name: string, factory: () => T): void
  registerSingleton<T>(name: string, factory: () => T): void
  resolve<T>(name: string): T
  initialize(): void
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
  initialize(): void {
    if (this.initialized) {
      throw new Error('ServiceContainer already initialized')
    }
    
    this.registerServices()
    this.setupServices()
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
        this.resolve('overlayAutoManager')
      )
    })
    
    console.log('🔧 모든 서비스 등록 완료')
  }
  
  private setupServices(): void {
    // 서비스 초기화 및 설정
    const channelHandlers = this.resolve<{ setupAllHandlers(): void }>('channelHandlers')
    channelHandlers.setupAllHandlers()
    
    // CoreCommunicationService와 ForegroundWindowService 연결
    this.setupGameEventHandlers()
    
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
  
  // 서비스 정리
  async dispose(): Promise<void> {
    if (!this.initialized) return
    
    try {
      // Core 연결 종료
      const coreService = this.resolve<ICoreCommunicationService>('coreCommunicationService')
      if (coreService && typeof coreService.stopConnection === 'function') {
        await coreService.stopConnection()
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