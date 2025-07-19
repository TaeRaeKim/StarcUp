/// <reference types="vite/client" />

declare global {
  interface Window {
    electronAPI: {
      minimizeWindow: () => void
      maximizeWindow: () => void
      closeWindow: () => void
      dragWindow: () => void
      toggleOverlay: () => void
      showOverlay: () => void
      hideOverlay: () => void
      resizeWindow: (width: number, height: number) => void
      // 위치 저장/복원 기능
      saveWindowPosition: () => Promise<any>
      restoreWindowPosition: () => Promise<any>
      setWindowPosition: (x: number, y: number) => Promise<any>
      // 오버레이 중앙 위치 업데이트 이벤트 리스너
      onUpdateCenterPosition: (callback: (data: any) => void) => () => void
    }
    coreAPI: {
      startDetection: () => Promise<any>
      stopDetection: () => Promise<any>
      getGameStatus: () => Promise<any>
      onGameStatusChanged: (callback: (data: { status: string }) => void) => () => void
      onForegroundChanged: (callback: (data: { isStarcraftInForeground: boolean }) => void) => () => void
    }
  }
}

export {}
