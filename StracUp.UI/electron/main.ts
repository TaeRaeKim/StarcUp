import { app, BrowserWindow, ipcMain, globalShortcut } from 'electron'
import { createRequire } from 'node:module'
import { fileURLToPath } from 'node:url'
import path from 'node:path'

const require = createRequire(import.meta.url)
const __dirname = path.dirname(fileURLToPath(import.meta.url))

// The built directory structure
//
// ├─┬─┬ dist
// │ │ └── index.html
// │ │
// │ ├─┬ dist-electron
// │ │ ├── main.js
// │ │ └── preload.mjs
// │
process.env.APP_ROOT = path.join(__dirname, '..')

// 🚧 Use ['ENV_NAME'] avoid vite:define plugin - Vite@2.x
export const VITE_DEV_SERVER_URL = process.env['VITE_DEV_SERVER_URL']
export const MAIN_DIST = path.join(process.env.APP_ROOT, 'dist-electron')
export const RENDERER_DIST = path.join(process.env.APP_ROOT, 'dist')

process.env.VITE_PUBLIC = VITE_DEV_SERVER_URL ? path.join(process.env.APP_ROOT, 'public') : RENDERER_DIST

let win: BrowserWindow | null
let overlayWin: BrowserWindow | null

function createWindow() {
  win = new BrowserWindow({
    width: 500,        // 컨텐츠에 맞춘 최적화된 크기
    height: 750,       // 높이 증가
    minWidth: 500,     // 최소 크기 고정
    minHeight: 750,    // 최소 크기 고정
    maxWidth: 500,     // 최대 크기 고정 (리사이징 방지)
    maxHeight: 750,    // 최대 크기 고정 (리사이징 방지)
    resizable: false,  // 창 크기 조절 비활성화
    frame: false,      // 기본 타이틀바 제거
    titleBarStyle: 'hidden', // 타이틀바 완전 숨김
    icon: path.join(process.env.VITE_PUBLIC, 'electron-vite.svg'),
    webPreferences: {
      preload: path.join(__dirname, 'preload.mjs'),
      nodeIntegration: false,
      contextIsolation: true,
    },
  })

  // 오버레이 창 생성
  createOverlayWindow()

  // Test active push message to Renderer-process.
  win.webContents.on('did-finish-load', () => {
    win?.webContents.send('main-process-message', (new Date).toLocaleString())
  })

  if (VITE_DEV_SERVER_URL) {
    win.loadURL(VITE_DEV_SERVER_URL)
    // 개발 환경에서 개발자도구 자동 열기
    win.webContents.openDevTools()
  } else {
    // win.loadFile('dist/index.html')
    win.loadFile(path.join(RENDERER_DIST, 'index.html'))
  }
}

function createOverlayWindow() {
  overlayWin = new BrowserWindow({
    width: 400,
    height: 200,
    frame: false,              // 프레임 없음
    transparent: true,         // 투명 창
    alwaysOnTop: true,         // 항상 최상위
    skipTaskbar: true,         // 작업표시줄에 표시 안함
    resizable: false,          // 크기 조절 불가
    focusable: false,          // 포커스 불가 (클릭 통과)
    webPreferences: {
      preload: path.join(__dirname, 'preload.mjs'),
      nodeIntegration: false,
      contextIsolation: true,
    },
  })

  // 클릭 통과 설정
  overlayWin.setIgnoreMouseEvents(true)
  
  // 화면 중앙에 배치
  overlayWin.center()

  // 오버레이용 HTML 페이지 로드
  const overlayHtml = `
    <!DOCTYPE html>
    <html>
    <head>
      <meta charset="UTF-8">
      <title>Overlay</title>
      <style>
        body {
          margin: 0;
          padding: 0;
          background: transparent;
          font-family: Arial, sans-serif;
          display: flex;
          justify-content: center;
          align-items: center;
          height: 100vh;
          overflow: hidden;
        }
        .overlay-content {
          background: rgba(0, 0, 0, 0.8);
          color: white;
          padding: 20px;
          border-radius: 10px;
          text-align: center;
          font-size: 24px;
          font-weight: bold;
          text-shadow: 2px 2px 4px rgba(0, 0, 0, 0.5);
          border: 2px solid #00ff00;
          box-shadow: 0 0 20px rgba(0, 255, 0, 0.5);
        }
      </style>
    </head>
    <body>
      <div class="overlay-content">
        Hello World!
      </div>
    </body>
    </html>
  `
  
  overlayWin.loadURL(`data:text/html;charset=utf-8,${encodeURIComponent(overlayHtml)}`)
  
  // 초기에는 오버레이 창을 숨김
  overlayWin.hide()
}

// Quit when all windows are closed, except on macOS. There, it's common
// for applications and their menu bar to stay active until the user quits
// explicitly with Cmd + Q.
app.on('window-all-closed', () => {
  if (process.platform !== 'darwin') {
    app.quit()
    win = null
    overlayWin = null
  }
})

app.on('activate', () => {
  // On OS X it's common to re-create a window in the app when the
  // dock icon is clicked and there are no other windows open.
  if (BrowserWindow.getAllWindows().length === 0) {
    createWindow()
  }
})

app.whenReady().then(() => {
  createWindow()
  setupIPC()
  
  // 개발자도구 토글 단축키 등록 (F12 또는 Ctrl+Shift+I)
  globalShortcut.register('F12', () => {
    if (win) {
      win.webContents.toggleDevTools()
    }
  })
  
  globalShortcut.register('CommandOrControl+Shift+I', () => {
    if (win) {
      win.webContents.toggleDevTools()
    }
  })

  // 오버레이 토글 단축키 (F1)
  globalShortcut.register('F1', () => {
    if (overlayWin) {
      if (overlayWin.isVisible()) {
        overlayWin.hide()
      } else {
        overlayWin.show()
      }
    }
  })
})

// IPC 핸들러 설정
function setupIPC() {
  ipcMain.on('minimize-window', () => {
    if (win) {
      win.minimize()
    }
  })

  ipcMain.on('maximize-window', () => {
    if (win) {
      if (win.isMaximized()) {
        win.unmaximize()
      } else {
        win.maximize()
      }
    }
  })

  ipcMain.on('close-window', () => {
    if (win) {
      win.close()
    }
  })

  ipcMain.on('drag-window', () => {
    // 드래그는 CSS의 -webkit-app-region: drag; 로 처리됨
    // 이 핸들러는 필요시 추가 드래그 로직을 위해 보관
  })

  // 오버레이 관련 IPC 핸들러
  ipcMain.on('toggle-overlay', () => {
    if (overlayWin) {
      if (overlayWin.isVisible()) {
        overlayWin.hide()
      } else {
        overlayWin.show()
      }
    }
  })

  ipcMain.on('show-overlay', () => {
    if (overlayWin) {
      overlayWin.show()
    }
  })

  ipcMain.on('hide-overlay', () => {
    if (overlayWin) {
      overlayWin.hide()
    }
  })
}
