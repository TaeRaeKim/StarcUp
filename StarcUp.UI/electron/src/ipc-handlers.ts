import { ipcMain } from 'electron'
import { WindowManager } from './window-manager'

export class IPCHandlers {
  private windowManager: WindowManager

  constructor(windowManager: WindowManager) {
    this.windowManager = windowManager
  }

  setupHandlers(): void {
    this.setupWindowHandlers()
    this.setupOverlayHandlers()
  }

  private setupWindowHandlers(): void {
    // 메인 윈도우 제어
    ipcMain.on('minimize-window', () => {
      this.windowManager.minimizeMain()
    })

    ipcMain.on('maximize-window', () => {
      this.windowManager.maximizeMain()
    })

    ipcMain.on('close-window', () => {
      this.windowManager.closeMain()
    })

    ipcMain.on('drag-window', () => {
      // 드래그는 CSS의 -webkit-app-region: drag; 로 처리됨
      // 이 핸들러는 필요시 추가 드래그 로직을 위해 보관
    })
  }

  private setupOverlayHandlers(): void {
    // 오버레이 관련 IPC 핸들러
    ipcMain.on('toggle-overlay', () => {
      this.windowManager.toggleOverlay()
    })

    ipcMain.on('show-overlay', () => {
      this.windowManager.showOverlay()
    })

    ipcMain.on('hide-overlay', () => {
      this.windowManager.hideOverlay()
    })
  }

  // 핸들러 정리
  removeHandlers(): void {
    // 메인 윈도우 핸들러 제거
    ipcMain.removeAllListeners('minimize-window')
    ipcMain.removeAllListeners('maximize-window')
    ipcMain.removeAllListeners('close-window')
    ipcMain.removeAllListeners('drag-window')

    // 오버레이 핸들러 제거
    ipcMain.removeAllListeners('toggle-overlay')
    ipcMain.removeAllListeners('show-overlay')
    ipcMain.removeAllListeners('hide-overlay')
  }
}