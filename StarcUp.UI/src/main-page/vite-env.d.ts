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
    }
    coreAPI: {
      startDetection: () => Promise<any>
      stopDetection: () => Promise<any>
      getGameStatus: () => Promise<any>
      onGameStatusChanged: (callback: (data: { status: string }) => void) => () => void
    }
  }
}

export {}
