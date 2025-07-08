import { ipcMain } from 'electron'
import { WindowManager } from './window-manager'
import { CoreProcessManager } from './core-process-manager'

export class IPCHandlers {
  private windowManager: WindowManager
  private coreProcessManager: CoreProcessManager

  constructor(windowManager: WindowManager, coreProcessManager: CoreProcessManager) {
    this.windowManager = windowManager
    this.coreProcessManager = coreProcessManager
  }

  setupHandlers(): void {
    this.setupWindowHandlers()
    this.setupOverlayHandlers()
    this.setupCoreProcessHandlers()
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

  private setupCoreProcessHandlers(): void {
    // Core 프로세스 상태 확인
    ipcMain.handle('core-process-status', () => {
      return {
        connected: this.coreProcessManager.connected
      }
    })

    // Core 프로세스 시작
    ipcMain.handle('start-core-process', async () => {
      try {
        await this.coreProcessManager.startCoreProcess()
        return { success: true }
      } catch (error) {
        return { success: false, error: String(error) }
      }
    })

    // Core 프로세스 중지
    ipcMain.handle('stop-core-process', async () => {
      try {
        await this.coreProcessManager.stopCoreProcess()
        return { success: true }
      } catch (error) {
        return { success: false, error: String(error) }
      }
    })

    // 게임 감지 시작
    ipcMain.handle('start-game-detect', async () => {
      try {
        const response = await this.coreProcessManager.sendCommand('start-game-detect')
        return response
      } catch (error) {
        return { success: false, error: String(error) }
      }
    })

    // 게임 감지 중지
    ipcMain.handle('stop-game-detect', async () => {
      try {
        const response = await this.coreProcessManager.sendCommand('stop-game-detect')
        return response
      } catch (error) {
        return { success: false, error: String(error) }
      }
    })

    // 게임 상태 조회
    ipcMain.handle('get-game-status', async () => {
      try {
        const response = await this.coreProcessManager.sendCommand('get-game-status')
        return response
      } catch (error) {
        return { success: false, error: String(error) }
      }
    })

    // 일반 명령 전송
    ipcMain.handle('send-core-command', async (event, command: string, args?: string[]) => {
      try {
        const response = await this.coreProcessManager.sendCommand(command, args)
        return response
      } catch (error) {
        return { success: false, error: String(error) }
      }
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

    // Core 프로세스 핸들러 제거
    ipcMain.removeHandler('core-process-status')
    ipcMain.removeHandler('start-core-process')
    ipcMain.removeHandler('stop-core-process')
    ipcMain.removeHandler('start-game-detect')
    ipcMain.removeHandler('stop-game-detect')
    ipcMain.removeHandler('get-game-status')
    ipcMain.removeHandler('send-core-command')
  }
}