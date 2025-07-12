import { IWindowConfiguration } from '../types'

// 윈도우 설정 상수
export const WINDOW_CONFIG: IWindowConfiguration = {
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
    show: false, // ready-to-show 이벤트를 위해 false로 설정
    webPreferences: {
      nodeIntegration: false,
      contextIsolation: true,
    },
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
    show: false,
    webPreferences: {
      nodeIntegration: false,
      contextIsolation: true,
    },
  }
}

// 개발 도구 설정
export const DEV_TOOLS_CONFIG = {
  shortcuts: ['CommandOrControl+Shift+I'],
  autoOpenInDev: true,
}

// 오버레이 설정
export const OVERLAY_CONFIG = {
  toggleShortcut: 'F1',
  defaultHidden: true,
}