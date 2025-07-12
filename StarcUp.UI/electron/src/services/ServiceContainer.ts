import { ICoreCommunicationService } from './core/interfaces'
import { IShortcutManager, IWindowManager } from './window/interfaces'
import { IIPCService } from './ipc/interfaces'
import { DataStorageService } from './storage/DataStorageService'
import { WindowManager } from './window/WindowManager'
import { WINDOW_CONFIG } from './window/WindowConfiguration'
import { ShortcutManager } from './window/ShortcutManager'
import { NamedPipeService } from './core/NamedPipeService'
import { CoreCommunicationService } from './core/CoreCommunicationService'
import { AuthService } from './auth/AuthService'
import { IPCService } from './ipc/IPCService'
import { ChannelHandlers } from './ipc/ChannelHandlers'

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
        this.resolve('shortcutManager')
      )
    })
    
    console.log('🔧 모든 서비스 등록 완료')
  }
  
  private setupServices(): void {
    // 서비스 초기화 및 설정
    const channelHandlers = this.resolve<{ setupAllHandlers(): void }>('channelHandlers')
    channelHandlers.setupAllHandlers()
    
    console.log('✅ 모든 서비스 초기화 완료')
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
      
      // 싱글톤 서비스들 정리
      for (const [name, instance] of this.singletons) {
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