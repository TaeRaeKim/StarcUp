/// <reference types="vite-plugin-electron/electron-env" />

declare namespace NodeJS {
  interface ProcessEnv {
    /** /dist/ or /public/ */
    VITE_PUBLIC: string
  }
}

// Core API response type
interface ICoreResponse {
  success: boolean
  data?: any
  error?: string
}

// Used in Renderer process, expose in `preload.ts`
interface Window {
  ipcRenderer?: {
    on: (channel: string, listener: (...args: any[]) => void) => void
    off: (channel: string, listener: (...args: any[]) => void) => void
    send: (channel: string, ...args: any[]) => void
    invoke: (channel: string, ...args: any[]) => Promise<any>
  }
  electronAPI?: {
    minimizeWindow: () => Promise<void>
    maximizeWindow: () => Promise<void>
    closeWindow: () => Promise<void>
    dragWindow: () => Promise<void>
    toggleOverlay: () => Promise<void>
    showOverlay: () => Promise<void>
    hideOverlay: () => Promise<void>
    resizeWindow: (width: number, height: number) => Promise<void>
  }
  coreAPI?: {
    startDetection: () => Promise<ICoreResponse>
    stopDetection: () => Promise<ICoreResponse>
    getGameStatus: () => Promise<ICoreResponse>
    onGameStatusChanged: (callback: (data: { status: string }) => void) => (() => void)
    sendPresetInit: (message: any) => Promise<ICoreResponse>
    sendPresetUpdate: (message: any) => Promise<ICoreResponse>
  }
}
