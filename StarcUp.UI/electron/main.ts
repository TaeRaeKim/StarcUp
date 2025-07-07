import { app, BrowserWindow } from 'electron'
import { WindowManager } from './src/window-manager'
import { IPCHandlers } from './src/ipc-handlers'
import { ShortcutManager } from './src/shortcuts'

let windowManager: WindowManager
let ipcHandlers: IPCHandlers
let shortcutManager: ShortcutManager

// Quit when all windows are closed, except on macOS. There, it's common
// for applications and their menu bar to stay active until the user quits
// explicitly with Cmd + Q.
app.on('window-all-closed', () => {
  if (process.platform !== 'darwin') {
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

function initializeApp(): void {
  // 모듈 초기화
  windowManager = new WindowManager()
  ipcHandlers = new IPCHandlers(windowManager)
  shortcutManager = new ShortcutManager(windowManager)

  // 윈도우 생성
  windowManager.createMainWindow()

  // IPC 핸들러 설정
  ipcHandlers.setupHandlers()

  // 단축키 등록
  shortcutManager.registerShortcuts()
}
