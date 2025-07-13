import { IIPCService } from './interfaces'
import { ICoreCommunicationService } from '../core/interfaces'
import { IAuthService } from '../auth/interfaces'
import { IDataStorageService } from '../storage/interfaces'
import { IWindowManager, IShortcutManager } from '../window/interfaces'

export class ChannelHandlers {
  private ipcService: IIPCService
  private coreService: ICoreCommunicationService
  private authService: IAuthService
  private dataService: IDataStorageService
  private windowManager: IWindowManager
  private shortcutManager: IShortcutManager

  constructor(
    ipcService: IIPCService,
    coreService: ICoreCommunicationService,
    authService: IAuthService,
    dataService: IDataStorageService,
    windowManager: IWindowManager,
    shortcutManager: IShortcutManager
  ) {
    this.ipcService = ipcService
    this.coreService = coreService
    this.authService = authService
    this.dataService = dataService
    this.windowManager = windowManager
    this.shortcutManager = shortcutManager
  }

  setupAllHandlers(): void {    
    this.setupCoreHandlers()
    this.setupAuthHandlers()
    this.setupDataHandlers()
    this.setupWindowHandlers()
    this.setupShortcutHandlers()
    console.log('✅ 모든 IPC 핸들러 설정 완료')
  }

  private setupCoreHandlers(): void {
    // Core 관련 핸들러
    this.ipcService.registerHandler('core:status', async () => ({
      connected: this.coreService.isConnected
    }))

    this.ipcService.registerHandler('core:start-detection', async () => 
      await this.coreService.startGameDetection()
    )

    this.ipcService.registerHandler('core:stop-detection', async () => 
      await this.coreService.stopGameDetection()
    )

    this.ipcService.registerHandler('core:get-game-status', async () => 
      await this.coreService.getGameStatus()
    )

    this.ipcService.registerHandler('core:get-unit-counts', async (data) => 
      await this.coreService.getUnitCounts(data.playerId)
    )

    console.log('📡 Core IPC 핸들러 등록 완료')
  }

  private setupAuthHandlers(): void {
    // 인증 관련 핸들러
    this.ipcService.registerHandler('auth:login', async (data) => 
      await this.authService.login(data.username, data.password)
    )

    this.ipcService.registerHandler('auth:logout', async () => 
      await this.authService.logout()
    )

    this.ipcService.registerHandler('auth:get-session', async () => {
      const user = await this.authService.getCurrentUser()
      return { user: user || undefined }
    })

    console.log('📡 Auth IPC 핸들러 등록 완료')
  }

  private setupDataHandlers(): void {
    // 데이터 관련 핸들러
    this.ipcService.registerHandler('data:save-preset', async (data) => 
      await this.dataService.savePreset(data.userId, data.preset)
    )

    this.ipcService.registerHandler('data:load-preset', async (data) => ({
      success: true,
      data: await this.dataService.loadPreset(data.userId, data.presetId)
    }))

    this.ipcService.registerHandler('data:get-presets', async (data) => ({
      presets: await this.dataService.loadPresets(data.userId)
    }))

    this.ipcService.registerHandler('data:delete-preset', async (data) => 
      await this.dataService.deletePreset(data.userId, data.presetId)
    )

    console.log('📡 Data IPC 핸들러 등록 완료')
  }

  private setupWindowHandlers(): void {
    // 윈도우 관리 핸들러
    this.ipcService.registerHandler('window:minimize', async () => {
      this.windowManager.minimizeMain()
    })

    this.ipcService.registerHandler('window:maximize', async () => {
      this.windowManager.maximizeMain()
    })

    this.ipcService.registerHandler('window:close', async () => {
      this.windowManager.closeMain()
    })

    this.ipcService.registerHandler('window:drag', async () => {
      // 드래그는 CSS의 -webkit-app-region: drag; 로 처리됨
      // 이 핸들러는 필요시 추가 드래그 로직을 위해 보관
    })

    this.ipcService.registerHandler('window:toggle-overlay', async () => {
      this.windowManager.toggleOverlay()
    })

    this.ipcService.registerHandler('window:show-overlay', async () => {
      this.windowManager.showOverlay()
    })

    this.ipcService.registerHandler('window:hide-overlay', async () => {
      this.windowManager.hideOverlay()
    })

    this.ipcService.registerHandler('window:resize', async (data) => {
      if (data && typeof data.width === 'number' && typeof data.height === 'number') {
        this.windowManager.resizeMain(data.width, data.height)
      } else {
        console.error('❌ window:resize - 잘못된 인자:', data)
      }
    })

    this.ipcService.registerHandler('window:get-window-bounds', async () => ({
      main: this.windowManager.getMainWindowBounds(),
      overlay: this.windowManager.getOverlayWindowBounds()
    }))

    this.ipcService.registerHandler('window:set-overlay-position', async (data) => {
      this.windowManager.setOverlayPosition(data.x, data.y)
    })

    this.ipcService.registerHandler('window:set-overlay-size', async (data) => {
      this.windowManager.setOverlaySize(data.width, data.height)
    })

    this.ipcService.registerHandler('window:set-overlay-opacity', async (data) => {
      this.windowManager.setOverlayOpacity(data.opacity)
    })

    this.ipcService.registerHandler('window:toggle-dev-tools', async () => {
      this.windowManager.toggleDevTools()
    })

    console.log('📡 Window IPC 핸들러 등록 완료')
  }

  private setupShortcutHandlers(): void {
    // 단축키 관리 핸들러
    this.ipcService.registerHandler('shortcut:register', async (data) => {
      const callback = this.getShortcutCallback(data.action)
      if (callback) {
        return { success: this.shortcutManager.registerCustomShortcut(data.accelerator, callback) }
      }
      return { success: false }
    })

    this.ipcService.registerHandler('shortcut:unregister', async (data) => {
      this.shortcutManager.unregisterShortcut(data.accelerator)
      return { success: true }
    })

    this.ipcService.registerHandler('shortcut:list', async () => ({
      shortcuts: this.shortcutManager.getRegisteredShortcuts()
    }))

    console.log('📡 Shortcut IPC 핸들러 등록 완료')
  }

  private getShortcutCallback(action: string): (() => void) | null {
    switch (action) {
      case 'toggle-overlay':
        return () => this.windowManager.toggleOverlay()
      case 'show-overlay':
        return () => this.windowManager.showOverlay()
      case 'hide-overlay':
        return () => this.windowManager.hideOverlay()
      case 'toggle-dev-tools':
        return () => this.windowManager.toggleDevTools()
      case 'minimize-main':
        return () => this.windowManager.minimizeMain()
      case 'maximize-main':
        return () => this.windowManager.maximizeMain()
      case 'close-main':
        return () => this.windowManager.closeMain()
      default:
        console.warn(`⚠️ 알 수 없는 단축키 액션: ${action}`)
        return null
    }
  }
}