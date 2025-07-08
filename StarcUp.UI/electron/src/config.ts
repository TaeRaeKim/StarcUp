import { createRequire } from 'node:module'
import { fileURLToPath } from 'node:url'
import path from 'node:path'

const require = createRequire(import.meta.url)
const __dirname = path.dirname(fileURLToPath(import.meta.url))

// 환경 변수 및 경로 설정
process.env.APP_ROOT = path.join(__dirname, '..')

export const VITE_DEV_SERVER_URL = process.env['VITE_DEV_SERVER_URL']
export const MAIN_DIST = path.join(process.env.APP_ROOT, 'dist-electron')
export const RENDERER_DIST = path.join(process.env.APP_ROOT, 'dist')

process.env.VITE_PUBLIC = VITE_DEV_SERVER_URL ? path.join(process.env.APP_ROOT, 'public') : RENDERER_DIST

// 윈도우 설정
export const WINDOW_CONFIG = {
  main: {
    width: 500,
    height: 750,
    minWidth: 500,
    minHeight: 750,
    maxWidth: 500,
    maxHeight: 750,
    resizable: false,
    frame: false,
    titleBarStyle: 'hidden' as const,
  },
  overlay: {
    width: 400,
    height: 200,
    frame: false,
    transparent: true,
    alwaysOnTop: true,
    skipTaskbar: true,
    resizable: false,
    focusable: false,
  }
}

// 개발 도구 설정
export const DEV_TOOLS_CONFIG = {
  shortcuts: ['F12', 'CommandOrControl+Shift+I'],
  autoOpenInDev: true,
}

// 오버레이 설정
export const OVERLAY_CONFIG = {
  toggleShortcut: 'F1',
  defaultHidden: true,
}