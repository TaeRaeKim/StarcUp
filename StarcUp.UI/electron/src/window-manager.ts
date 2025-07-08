import { BrowserWindow } from 'electron'
import path from 'node:path'
import { WINDOW_CONFIG, MAIN_DIST, VITE_DEV_SERVER_URL, RENDERER_DIST, DEV_TOOLS_CONFIG, OVERLAY_CONFIG } from './config'

export class WindowManager {
  private mainWindow: BrowserWindow | null = null
  private overlayWindow: BrowserWindow | null = null

  get main(): BrowserWindow | null {
    return this.mainWindow
  }

  get overlay(): BrowserWindow | null {
    return this.overlayWindow
  }

  createMainWindow(): void {
    this.mainWindow = new BrowserWindow({
      ...WINDOW_CONFIG.main,
      icon: path.join(process.env.VITE_PUBLIC!, 'electron-vite.svg'),
      webPreferences: {
        preload: path.join(MAIN_DIST, 'preload.mjs'),
        nodeIntegration: false,
        contextIsolation: true,
      },
    })

    // 오버레이 창 생성
    this.createOverlayWindow()

    // 메인 프로세스 메시지 전송
    this.mainWindow.webContents.on('did-finish-load', () => {
      this.mainWindow?.webContents.send('main-process-message', new Date().toLocaleString())
    })

    // 페이지 로드
    this.loadMainPage()
  }

  private createOverlayWindow(): void {
    this.overlayWindow = new BrowserWindow({
      ...WINDOW_CONFIG.overlay,
      webPreferences: {
        preload: path.join(MAIN_DIST, 'preload.mjs'),
        nodeIntegration: false,
        contextIsolation: true,
      },
    })

    this.overlayWindow.setIgnoreMouseEvents(true)
    this.overlayWindow.center()
    
    // 오버레이 페이지 로드
    this.loadOverlayPage()
    
    if (OVERLAY_CONFIG.defaultHidden) {
      this.overlayWindow.hide()
    }
  }

  private loadMainPage(): void {
    if (!this.mainWindow) return

    if (VITE_DEV_SERVER_URL) {
      // 개발 환경에서 Vite 개발 서버 URL로 로드
      this.mainWindow.loadURL(path.join(VITE_DEV_SERVER_URL, 'src', 'main-page', 'index.html'))
      // 개발 환경에서 개발자도구 자동 열기
      if (DEV_TOOLS_CONFIG.autoOpenInDev) {
        this.mainWindow.webContents.openDevTools()
      }
    } else {
      // 프로덕션 환경에서 빌드된 파일로 로드
      this.mainWindow.loadFile(path.join(RENDERER_DIST, 'src', 'main-page', 'index.html'))
    }
  }

  private loadOverlayPage(): void {
    if (!this.overlayWindow) return

    if (VITE_DEV_SERVER_URL) {
      // 개발 환경에서 Vite 개발 서버 URL로 로드
      this.overlayWindow.loadURL(path.join(VITE_DEV_SERVER_URL, 'src', 'overlay', 'index.html'))
    } else {
      // 프로덕션 환경에서 빌드된 파일로 로드
      this.overlayWindow.loadFile(path.join(RENDERER_DIST, 'src', 'overlay', 'index.html'))
    }
  }

  // 윈도우 제어 메서드
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

  toggleDevTools(): void {
    if (this.mainWindow) {
      this.mainWindow.webContents.toggleDevTools()
    }
  }

  // 오버레이 제어 메서드
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

  // 정리
  cleanup(): void {
    this.mainWindow = null
    this.overlayWindow = null
  }
}