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
    }
  }
}

export {}
