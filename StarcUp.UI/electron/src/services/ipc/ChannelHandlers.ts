import { IIPCService } from './interfaces'
import { ICoreCommunicationService } from '../core'
import { IAuthService } from '../auth'
import { IDataStorageService } from '../storage'
import { IWindowManager, IShortcutManager } from '../window'
import { IOverlayAutoManager } from '../overlay'
import { IPresetStateManager } from '../preset'
import { RaceType } from '../../../../src/types/enums'

export class ChannelHandlers {
  private ipcService: IIPCService
  private coreService: ICoreCommunicationService
  private authService: IAuthService
  private dataService: IDataStorageService
  private windowManager: IWindowManager
  private shortcutManager: IShortcutManager
  private overlayAutoManager: IOverlayAutoManager
  private presetStateManager: IPresetStateManager

  constructor(
    ipcService: IIPCService,
    coreService: ICoreCommunicationService,
    authService: IAuthService,
    dataService: IDataStorageService,
    windowManager: IWindowManager,
    shortcutManager: IShortcutManager,
    overlayAutoManager: IOverlayAutoManager,
    presetStateManager: IPresetStateManager
  ) {
    this.ipcService = ipcService
    this.coreService = coreService
    this.authService = authService
    this.dataService = dataService
    this.windowManager = windowManager
    this.shortcutManager = shortcutManager
    this.overlayAutoManager = overlayAutoManager
    this.presetStateManager = presetStateManager
  }

  setupAllHandlers(): void {    
    this.setupCoreHandlers()
    this.setupAuthHandlers()
    this.setupDataHandlers()
    this.setupWindowHandlers()
    this.setupShortcutHandlers()
    this.setupOverlayHandlers()
    this.setupPresetHandlers()
    console.log('✅ 모든 IPC 핸들러 설정 완료')
  }

  private setupCoreHandlers(): void {
    // Core 관련 핸들러
    this.ipcService.registerHandler('core:status', async () => ({
      connected: this.coreService.isConnected
    }))

    this.ipcService.registerHandler('core:start-detection', async () => {
      const result = await this.coreService.startGameDetection()
      // 게임 감지 시작 시 자동 overlay 관리 활성화
      if (result.success) {
        this.overlayAutoManager.enableAutoMode()
      }
      return result
    })

    this.ipcService.registerHandler('core:stop-detection', async () => {
      const result = await this.coreService.stopGameDetection()
      // 게임 감지 중지 시 자동 overlay 관리 비활성화
      this.overlayAutoManager.disableAutoMode()
      return result
    })

    this.ipcService.registerHandler('core:get-game-status', async () => 
      await this.coreService.getGameStatus()
    )

    this.ipcService.registerHandler('core:get-unit-counts', async (data) => 
      await this.coreService.getUnitCounts(data.playerId)
    )

    // 프리셋 관련 핸들러
    this.ipcService.registerHandler('core:send-preset-init', async (data) => 
      await this.coreService.sendPresetInit(data)
    )

    this.ipcService.registerHandler('core:send-preset-update', async (data) => 
      await this.coreService.sendPresetUpdate(data)
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
    // 기본 프리셋 CRUD 핸들러
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

    // 새로운 프리셋 관리 핸들러들
    this.ipcService.registerHandler('data:update-preset', async (data) => 
      await this.dataService.updatePreset(data.userId, data.presetId, data.updates)
    )

    this.ipcService.registerHandler('data:get-selected-preset', async (data) => ({
      success: true,
      data: await this.dataService.getSelectedPreset(data.userId)
    }))

    this.ipcService.registerHandler('data:set-selected-preset', async (data) => 
      await this.dataService.setSelectedPreset(data.userId, data.index)
    )

    this.ipcService.registerHandler('data:get-presets-with-selection', async (data) => ({
      success: true,
      data: await this.dataService.getPresetsWithSelection(data.userId)
    }))

    console.log('📡 Data IPC 핸들러 등록 완료 (8개 핸들러)')
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
      // 오버레이가 표시되는 경우 저장된 위치 적용
      if (this.windowManager.isOverlayWindowVisible()) {
        this.overlayAutoManager.applyStoredPositionOnShow()
      }
    })

    this.ipcService.registerHandler('window:show-overlay', async () => {
      this.windowManager.showOverlay()
      // 오버레이 표시 시 저장된 위치 적용
      this.overlayAutoManager.applyStoredPositionOnShow()
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

    // 위치 저장/복원 핸들러
    this.ipcService.registerHandler('window:save-position', async () => {
      this.windowManager.saveMainWindowPosition()
    })

    this.ipcService.registerHandler('window:restore-position', async () => {
      this.windowManager.restoreMainWindowPosition()
    })

    this.ipcService.registerHandler('window:set-position', async (data) => {
      this.windowManager.setMainWindowPosition(data.x, data.y)
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

  private setupOverlayHandlers(): void {
    // 오버레이 설정 업데이트 핸들러
    this.ipcService.registerHandler('overlay:update-settings', async (settings) => {
      // 메인 윈도우에서 오버레이 윈도우로 설정 전달
      this.windowManager.sendToOverlayWindow('overlay-settings-changed', settings)
      return { success: true }
    })

    console.log('📡 Overlay IPC 핸들러 등록 완료')
  }

  private setupPresetHandlers(): void {
    // 상태 조회 핸들러
    this.ipcService.registerHandler('preset:get-current', async () => {
      try {
        return {
          success: true,
          data: this.presetStateManager.getCurrentPreset()
        }
      } catch (error) {
        console.error('❌ preset:get-current 실패:', error)
        return {
          success: false,
          error: error instanceof Error ? error.message : String(error)
        }
      }
    })

    this.ipcService.registerHandler('preset:get-state', async () => {
      try {
        return {
          success: true,
          data: this.presetStateManager.getPresetState()
        }
      } catch (error) {
        console.error('❌ preset:get-state 실패:', error)
        return {
          success: false,
          error: error instanceof Error ? error.message : String(error)
        }
      }
    })

    this.ipcService.registerHandler('preset:get-all', async () => {
      try {
        return {
          success: true,
          data: this.presetStateManager.getAllPresets()
        }
      } catch (error) {
        console.error('❌ preset:get-all 실패:', error)
        return {
          success: false,
          error: error instanceof Error ? error.message : String(error)
        }
      }
    })

    // 프리셋 관리 핸들러
    this.ipcService.registerHandler('preset:switch', async (data) => {
      try {
        if (!data?.presetId) {
          throw new Error('presetId가 필요합니다')
        }
        
        await this.presetStateManager.switchPreset(data.presetId)
        
        return {
          success: true,
          data: this.presetStateManager.getCurrentPreset()
        }
      } catch (error) {
        console.error('❌ preset:switch 실패:', error)
        return {
          success: false,
          error: error instanceof Error ? error.message : String(error)
        }
      }
    })

    this.ipcService.registerHandler('preset:update-settings', async (data) => {
      try {
        if (!data?.presetType || !data?.settings) {
          throw new Error('presetType과 settings가 필요합니다')
        }
        
        await this.presetStateManager.updatePresetSettings(data.presetType, data.settings)
        
        return {
          success: true,
          data: this.presetStateManager.getCurrentPreset()
        }
      } catch (error) {
        console.error('❌ preset:update-settings 실패:', error)
        return {
          success: false,
          error: error instanceof Error ? error.message : String(error)
        }
      }
    })

    this.ipcService.registerHandler('preset:update-batch', async (data) => {
      try {
        if (!data?.updates) {
          throw new Error('updates가 필요합니다')
        }
        
        await this.presetStateManager.updatePresetBatch(data.updates)
        
        return {
          success: true,
          data: this.presetStateManager.getCurrentPreset()
        }
      } catch (error) {
        console.error('❌ preset:update-batch 실패:', error)
        return {
          success: false,
          error: error instanceof Error ? error.message : String(error)
        }
      }
    })

    this.ipcService.registerHandler('preset:toggle-feature', async (data) => {
      try {
        if (typeof data?.featureIndex !== 'number' || typeof data?.enabled !== 'boolean') {
          throw new Error('featureIndex (number)와 enabled (boolean)가 필요합니다')
        }
        
        await this.presetStateManager.toggleFeature(data.featureIndex, data.enabled)
        
        return {
          success: true,
          data: this.presetStateManager.getCurrentPreset()
        }
      } catch (error) {
        console.error('❌ preset:toggle-feature 실패:', error)
        return {
          success: false,
          error: error instanceof Error ? error.message : String(error)
        }
      }
    })

    // Overlay 전용 성능 최적화 핸들러 (기능 상태 + 종족 정보)
    this.ipcService.registerHandler('preset:get-features-only', async () => {
      try {
        const currentPreset = this.presetStateManager.getCurrentPreset()
        
        // Overlay가 필요로 하는 기본 기능 On/Off 상태와 종족 정보 반환 (성능 최적화)
        return {
          success: true,
          data: {
            featureStates: currentPreset?.featureStates || [false, false, false, false, false],
            selectedRace: currentPreset?.selectedRace ?? RaceType.Zerg // undefined인 경우에만 기본값 사용, 0도 유효한 값
          }
        }
      } catch (error) {
        console.error('❌ preset:get-features-only 실패:', error)
        return {
          success: false,
          error: error instanceof Error ? error.message : String(error)
        }
      }
    })

    // 프리셋 상태 변경 이벤트를 IPC로 브로드캐스트
    this.setupPresetEventBroadcasting()
    
    console.log('📡 Preset IPC 핸들러 등록 완료 (6개 핸들러)')
  }

  private setupPresetEventBroadcasting(): void {
    // PresetStateManager의 상태 변경 이벤트를 모든 renderer 프로세스에 브로드캐스트
    this.presetStateManager.onStateChanged((event) => {
      console.log('📢 프리셋 상태 변경 IPC 브로드캐스트:', event.type)
      
      // 메인 페이지에 전체 상태 정보 전송 (기존 방식 유지)
      this.windowManager.sendToMainWindow('preset:state-changed', {
        type: event.type,
        presetId: event.presetId,
        preset: event.preset,
        state: this.presetStateManager.getPresetState(),
        changes: event.changes,
        timestamp: event.timestamp
      })
      
      // Overlay 전용: 성능 최적화를 위해 기능 상태와 종족 정보 전송
      if (event.type === 'preset-switched' || event.type === 'feature-toggled') {
        this.windowManager.sendToOverlayWindow('preset:features-changed', {
          featureStates: event.preset?.featureStates || [false, false, false, false, false],
          selectedRace: event.preset?.selectedRace ?? RaceType.Zerg, // undefined인 경우에만 기본값 사용, 0도 유효한 값
          timestamp: event.timestamp
        })
        
        console.log('📡 Overlay에 기능 상태 변경 알림:', {
          type: event.type,
          featureStates: event.preset?.featureStates || [false, false, false, false, false],
          selectedRace: event.preset?.selectedRace ?? RaceType.Zerg
        })
      }
      
      // 호환성을 위해 기존 오버레이 이벤트도 유지 (향후 제거 예정)
      if (event.type === 'preset-switched' || event.type === 'feature-toggled') {
        this.windowManager.sendToOverlayWindow('preset:state-changed', {
          type: event.type,
          presetId: event.presetId,
          featureStates: event.preset?.featureStates || [],
          selectedRace: event.preset?.selectedRace ?? RaceType.Zerg, // undefined인 경우에만 기본값 사용, 0도 유효한 값
          timestamp: event.timestamp
        })
      }
    })
    
    console.log('📡 프리셋 이벤트 브로드캐스팅 설정 완료 (Main: 전체, Overlay: 기능 상태만)')
  }

  private getShortcutCallback(action: string): (() => void) | null {
    switch (action) {
      case 'toggle-overlay':
        return () => {
          this.windowManager.toggleOverlay()
          // 오버레이가 표시되는 경우 저장된 위치 적용
          if (this.windowManager.isOverlayWindowVisible()) {
            this.overlayAutoManager.applyStoredPositionOnShow()
          }
        }
      case 'show-overlay':
        return () => {
          this.windowManager.showOverlay()
          // 오버레이 표시 시 저장된 위치 적용
          this.overlayAutoManager.applyStoredPositionOnShow()
        }
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