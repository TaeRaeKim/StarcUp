import { BrowserWindow, app } from 'electron'
import path from 'node:path'
import { IWindowManager } from './interfaces'
import { IWindowConfiguration } from '../types'
import { WINDOW_CONFIG, DEV_TOOLS_CONFIG, OVERLAY_CONFIG } from './WindowConfiguration'

// 환경 변수 및 경로 설정
const VITE_DEV_SERVER_URL = process.env['VITE_DEV_SERVER_URL']
// 프로젝트 루트 기준으로 절대 경로 설정
const PROJECT_ROOT = process.cwd()
const MAIN_DIST = path.join(PROJECT_ROOT, 'dist-electron')
const RENDERER_DIST = path.join(PROJECT_ROOT, 'dist')

export class WindowManager implements IWindowManager {
  private mainWindow: BrowserWindow | null = null
  private overlayWindow: BrowserWindow | null = null
  private windowConfig: IWindowConfiguration
  private savedMainWindowPosition: { x: number; y: number } | null = null
  
  constructor(windowConfig: IWindowConfiguration = WINDOW_CONFIG) {
    this.windowConfig = windowConfig
  }
  
  createMainWindow(): void {
    if (this.mainWindow) {
      console.warn('Main window already exists')
      return
    }
    
    const preloadPath = path.join(MAIN_DIST, 'preload.mjs')
    console.log(`🔧 Preload 스크립트 경로: ${preloadPath}`)
    console.log(`🔧 PROJECT_ROOT: ${PROJECT_ROOT}`)
    console.log(`🔧 MAIN_DIST: ${MAIN_DIST}`)
    
    this.mainWindow = new BrowserWindow({
      ...this.windowConfig.main,
      icon: path.join(process.env.VITE_PUBLIC || '', 'electron-vite.svg'),
      webPreferences: {
        preload: preloadPath,
        nodeIntegration: false,
        contextIsolation: true,
      },
    })
    
    // 윈도우 이벤트 처리
    this.setupMainWindowEvents()
    
    // 페이지 로드
    this.loadMainPage()
    
    console.log('✅ Main window created')
  }
  
  createOverlayWindow(): void {
    if (this.overlayWindow) {
      console.warn('Overlay window already exists')
      return
    }
    
    const overlayPreloadPath = path.join(MAIN_DIST, 'preload.mjs')
    console.log(`🔧 Overlay Preload 스크립트 경로: ${overlayPreloadPath}`)
    
    this.overlayWindow = new BrowserWindow({
      ...this.windowConfig.overlay,
      webPreferences: {
        preload: overlayPreloadPath,
        nodeIntegration: false,
        contextIsolation: true,
      },
    })
    
    // 오버레이 특성 설정
    this.overlayWindow.setIgnoreMouseEvents(true)
    this.overlayWindow.center()
    
    // 오버레이 이벤트 처리
    this.setupOverlayWindowEvents()
    
    // 페이지 로드
    this.loadOverlayPage()
    
    // 기본적으로 숨김
    if (OVERLAY_CONFIG.defaultHidden) {
      this.overlayWindow.hide()
    }
    
    console.log('✅ Overlay window created')
  }
  
  getMainWindow(): BrowserWindow | null {
    return this.mainWindow
  }
  
  getOverlayWindow(): BrowserWindow | null {
    return this.overlayWindow
  }
  
  // 메인 윈도우 제어
  minimizeMain(): void {
    if (this.mainWindow) {
      this.mainWindow.minimize()
    }
  }
  
  maximizeMain(): void {
    if (this.mainWindow) {
      if (this.mainWindow.isMaximized()) {
        this.mainWindow.unmaximize()
      } else {
        this.mainWindow.maximize()
      }
    }
  }
  
  closeMain(): void {
    if (this.mainWindow) {
      this.mainWindow.close()
    }
  }
  
  focusMain(): void {
    if (this.mainWindow) {
      this.mainWindow.focus()
    }
  }
  
  centerMain(): void {
    if (this.mainWindow) {
      this.mainWindow.center()
    }
  }
  
  resizeMain(width: number, height: number): void {
    if (this.mainWindow) {
      this.mainWindow.setSize(width, height)
      // 위치 유지 - center() 호출 제거
      console.log(`메인 윈도우 크기 변경: ${width}x${height} (위치 유지)`)
    }
  }
  
  // 오버레이 제어
  toggleOverlay(): void {
    if (this.overlayWindow) {
      if (this.overlayWindow.isVisible()) {
        this.overlayWindow.hide()
      } else {
        this.overlayWindow.show()
      }
    }
  }
  
  showOverlay(): void {
    if (this.overlayWindow) {
      this.overlayWindow.show()
    }
  }
  
  hideOverlay(): void {
    if (this.overlayWindow) {
      this.overlayWindow.hide()
    }
  }
  
  setOverlayPosition(x: number, y: number): void {
    if (this.overlayWindow) {
      this.overlayWindow.setPosition(x, y)
    }
  }
  
  setOverlaySize(width: number, height: number): void {
    if (this.overlayWindow) {
      this.overlayWindow.setSize(width, height)
    }
  }
  
  setOverlayOpacity(opacity: number): void {
    if (this.overlayWindow) {
      this.overlayWindow.setOpacity(Math.max(0, Math.min(1, opacity)))
    }
  }
  
  setOverlayMouseEvents(ignore: boolean): void {
    if (this.overlayWindow) {
      this.overlayWindow.setIgnoreMouseEvents(ignore)
    }
  }
  
  // 개발 도구
  toggleDevTools(): void {
    if (this.mainWindow) {
      this.mainWindow.webContents.toggleDevTools()
    }
  }
  
  toggleOverlayDevTools(): void {
    if (this.overlayWindow) {
      this.overlayWindow.webContents.toggleDevTools()
    }
  }
  
  // 윈도우 상태 확인
  isMainWindowVisible(): boolean {
    return this.mainWindow ? this.mainWindow.isVisible() : false
  }
  
  isOverlayWindowVisible(): boolean {
    return this.overlayWindow ? this.overlayWindow.isVisible() : false
  }
  
  isMainWindowMaximized(): boolean {
    return this.mainWindow ? this.mainWindow.isMaximized() : false
  }
  
  isMainWindowMinimized(): boolean {
    return this.mainWindow ? this.mainWindow.isMinimized() : false
  }
  
  // 윈도우 정보 조회
  getMainWindowBounds(): Electron.Rectangle | null {
    return this.mainWindow ? this.mainWindow.getBounds() : null
  }
  
  getOverlayWindowBounds(): Electron.Rectangle | null {
    return this.overlayWindow ? this.overlayWindow.getBounds() : null
  }
  
  // 윈도우 통신
  sendToMainWindow(channel: string, data: any): void {
    if (this.mainWindow) {
      this.mainWindow.webContents.send(channel, data)
    }
  }
  
  sendToOverlayWindow(channel: string, data: any): void {
    if (this.overlayWindow) {
      this.overlayWindow.webContents.send(channel, data)
    }
  }
  
  broadcastToAllWindows(channel: string, data: any): void {
    this.sendToMainWindow(channel, data)
    this.sendToOverlayWindow(channel, data)
  }
  
  // 페이지 로드
  private loadMainPage(): void {
    if (!this.mainWindow) return
    
    if (VITE_DEV_SERVER_URL) {
      // 개발 환경
      this.mainWindow.loadURL(path.join(VITE_DEV_SERVER_URL, 'src', 'main-page', 'index.html'))
      
      if (DEV_TOOLS_CONFIG.autoOpenInDev) {
        this.mainWindow.webContents.openDevTools()
      }
    } else {
      // 프로덕션 환경
      this.mainWindow.loadFile(path.join(RENDERER_DIST, 'src', 'main-page', 'index.html'))
    }
  }
  
  private loadOverlayPage(): void {
    if (!this.overlayWindow) return
    
    if (VITE_DEV_SERVER_URL) {
      // 개발 환경
      this.overlayWindow.loadURL(path.join(VITE_DEV_SERVER_URL, 'src', 'overlay', 'index.html'))
    } else {
      // 프로덕션 환경
      this.overlayWindow.loadFile(path.join(RENDERER_DIST, 'src', 'overlay', 'index.html'))
    }
  }
  
  // 윈도우 이벤트 설정
  private setupMainWindowEvents(): void {
    if (!this.mainWindow) return
    
    this.mainWindow.on('closed', () => {
      this.mainWindow = null
      console.log('Main window closed')
      app.quit() // 앱 종료
    })
    
    this.mainWindow.on('ready-to-show', () => {
      this.mainWindow?.show()
      console.log('Main window ready to show')
    })
    
    this.mainWindow.webContents.on('did-finish-load', () => {
      this.mainWindow?.webContents.send('main-process-message', new Date().toLocaleString())
    })
    
    this.mainWindow.on('minimize', () => {
      console.log('Main window minimized')
    })
    
    this.mainWindow.on('maximize', () => {
      console.log('Main window maximized')
    })
    
    this.mainWindow.on('unmaximize', () => {
      console.log('Main window unmaximized')
    })
  }
  
  private setupOverlayWindowEvents(): void {
    if (!this.overlayWindow) return
    
    this.overlayWindow.on('closed', () => {
      this.overlayWindow = null
      console.log('Overlay window closed')
    })
    
    this.overlayWindow.on('show', () => {
      console.log('Overlay window shown')
    })
    
    this.overlayWindow.on('hide', () => {
      console.log('Overlay window hidden')
    })
    
    this.overlayWindow.on('blur', () => {
      // 오버레이가 포커스를 잃으면 다시 최상위로
      if (this.overlayWindow) {
        this.overlayWindow.setAlwaysOnTop(true)
      }
    })
  }
  
  // 윈도우 설정 업데이트
  updateWindowConfig(config: Partial<IWindowConfiguration>): void {
    this.windowConfig = { ...this.windowConfig, ...config }
  }
  
  applyMainWindowConfig(config: Partial<Electron.BrowserWindowConstructorOptions>): void {
    if (this.mainWindow) {
      // 런타임에 변경 가능한 설정들 적용
      if (config.width !== undefined && config.height !== undefined) {
        this.mainWindow.setSize(config.width, config.height)
      }
      
      if (config.minWidth !== undefined && config.minHeight !== undefined) {
        this.mainWindow.setMinimumSize(config.minWidth, config.minHeight)
      }
      
      if (config.maxWidth !== undefined && config.maxHeight !== undefined) {
        this.mainWindow.setMaximumSize(config.maxWidth, config.maxHeight)
      }
      
      if (config.resizable !== undefined) {
        this.mainWindow.setResizable(config.resizable)
      }
      
      if (config.alwaysOnTop !== undefined) {
        this.mainWindow.setAlwaysOnTop(config.alwaysOnTop)
      }
    }
  }
  
  applyOverlayWindowConfig(config: Partial<Electron.BrowserWindowConstructorOptions>): void {
    if (this.overlayWindow) {
      // 런타임에 변경 가능한 설정들 적용
      if (config.width !== undefined && config.height !== undefined) {
        this.overlayWindow.setSize(config.width, config.height)
      }
      
      if (config.alwaysOnTop !== undefined) {
        this.overlayWindow.setAlwaysOnTop(config.alwaysOnTop)
      }
      
      if (config.skipTaskbar !== undefined) {
        this.overlayWindow.setSkipTaskbar(config.skipTaskbar)
      }
    }
  }
  
  // 위치 저장/복원 기능
  saveMainWindowPosition(): void {
    if (this.mainWindow) {
      const bounds = this.mainWindow.getBounds()
      this.savedMainWindowPosition = { x: bounds.x, y: bounds.y }
      console.log(`📍 메인 윈도우 위치 저장: x=${bounds.x}, y=${bounds.y}`)
    }
  }
  
  restoreMainWindowPosition(): void {
    if (this.mainWindow && this.savedMainWindowPosition) {
      this.mainWindow.setPosition(this.savedMainWindowPosition.x, this.savedMainWindowPosition.y)
      console.log(`📍 메인 윈도우 위치 복원: x=${this.savedMainWindowPosition.x}, y=${this.savedMainWindowPosition.y}`)
    }
  }
  
  setMainWindowPosition(x: number, y: number): void {
    if (this.mainWindow) {
      this.mainWindow.setPosition(x, y)
      console.log(`📍 메인 윈도우 위치 설정: x=${x}, y=${y}`)
    }
  }
  
  // 정리
  cleanup(): void {
    if (this.mainWindow) {
      this.mainWindow.close()
      this.mainWindow = null
    }
    
    if (this.overlayWindow) {
      this.overlayWindow.close()
      this.overlayWindow = null
    }
    
    console.log('✅ WindowManager cleanup completed')
  }
  
  // 에러 처리
  private handleWindowError(windowType: string, error: Error): void {
    console.error(`${windowType} window error:`, error)
    // 에러 발생 시 윈도우 재생성 또는 다른 복구 로직
  }
}