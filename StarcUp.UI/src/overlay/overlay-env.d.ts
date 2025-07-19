/// <reference types="vite/client" />

import type { CenterPositionData } from '../../electron/src/services/types'

declare global {
  interface Window {
    electronAPI: {
      // 윈도우 제어
      minimizeWindow: () => Promise<void>
      maximizeWindow: () => Promise<void>
      closeWindow: () => Promise<void>
      dragWindow: () => Promise<void>
      toggleOverlay: () => Promise<void>
      showOverlay: () => Promise<void>
      hideOverlay: () => Promise<void>
      resizeWindow: (width: number, height: number) => Promise<void>
      // 위치 저장/복원 기능
      saveWindowPosition: () => Promise<any>
      restoreWindowPosition: () => Promise<any>
      setWindowPosition: (x: number, y: number) => Promise<any>
      // 오버레이 관련 API
      onUpdateCenterPosition: (callback: (data: CenterPositionData) => void) => () => void
    }
  }
}

export {}