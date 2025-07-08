import { app, BrowserWindow } from 'electron'
import { WindowManager } from './src/window-manager'
import { IPCHandlers } from './src/ipc-handlers'
import { ShortcutManager } from './src/shortcuts'
import { CoreProcessManager } from './src/core-process-manager'

let windowManager: WindowManager
let ipcHandlers: IPCHandlers
let shortcutManager: ShortcutManager
let coreProcessManager: CoreProcessManager

// Quit when all windows are closed, except on macOS. There, it's common
// for applications and their menu bar to stay active until the user quits
// explicitly with Cmd + Q.
app.on('window-all-closed', async () => {
  if (process.platform !== 'darwin') {
    // Core 프로세스 정리
    if (coreProcessManager) {
      await coreProcessManager.stopCoreProcess()
    }
    
    app.quit()
    windowManager?.cleanup()
    ipcHandlers?.removeHandlers()
    shortcutManager?.unregisterAllShortcuts()
  }
})

app.on('activate', () => {
  // On OS X it's common to re-create a window in the app when the
  // dock icon is clicked and there are no other windows open.
  if (BrowserWindow.getAllWindows().length === 0) {
    initializeApp()
  }
})

app.whenReady().then(() => {
  initializeApp()
})

async function initializeApp(): Promise<void> {
  try {
    // 환경 감지 (개발 모드 vs 프로덕션 모드)
    const isDevelopment = process.env.NODE_ENV === 'development' || !app.isPackaged
    
    console.log(`🏗️ 애플리케이션 모드: ${isDevelopment ? '개발' : '프로덕션'}`)

    // 모듈 초기화
    windowManager = new WindowManager()
    coreProcessManager = new CoreProcessManager()
    ipcHandlers = new IPCHandlers(windowManager, coreProcessManager)
    shortcutManager = new ShortcutManager(windowManager)

    // 윈도우 생성
    windowManager.createMainWindow()

    // IPC 핸들러 설정
    ipcHandlers.setupHandlers()

    // 단축키 등록
    shortcutManager.registerShortcuts()

    // StarcUp.Core 연결/시작
    if (isDevelopment) {
      console.log('🔧 개발 모드: 기존 StarcUp.Core 프로세스에 연결 시도...')
    } else {
      console.log('🚀 프로덕션 모드: StarcUp.Core 프로세스 시작...')
    }
    
    await coreProcessManager.startCoreProcess(isDevelopment)
    console.log('✅ StarcUp.Core 초기화 완료')

  } catch (error) {
    console.error('❌ 애플리케이션 초기화 실패:', error)
    // Core 프로세스 시작 실패는 치명적이지 않으므로 계속 진행
  }
}
