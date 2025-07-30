/// <reference types="vite/client" />

// preload.ts의 실제 구현과 일치하는 통합 타입 정의
import type { CenterPositionData } from '../electron/src/services/types'

declare global {
  interface Window {
    // IPC Renderer (직접 사용하지 않음)
    ipcRenderer?: {
      on: (...args: any[]) => any
      off: (...args: any[]) => any
      send: (...args: any[]) => any
      invoke: (...args: any[]) => any
    }
    
    // Electron API (preload.ts의 electronAPI 구현과 완전 일치)
    electronAPI?: {
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
      
      // WorkerManager 이벤트 리스너들
      onWorkerStatusChanged: (callback: (data: any) => void) => () => void
      onGasBuildingAlert: (callback: () => void) => () => void
      onWorkerPresetChanged: (callback: (data: any) => void) => () => void
      
      // 오버레이 관련 이벤트 리스너 (통합)
      onUpdateCenterPosition: (callback: (data: CenterPositionData) => void) => () => void
      onToggleEditMode: (callback: (data: { isEditMode: boolean }) => void) => () => void
      
      // 프리셋 관리 API (preload.ts 구현과 완전 일치)
      savePreset: (userId: string, preset: any) => Promise<any>
      loadPreset: (userId: string, presetId: string) => Promise<any>
      getPresets: (userId: string) => Promise<any>
      deletePreset: (userId: string, presetId: string) => Promise<any>
      updatePreset: (userId: string, presetId: string, updates: any) => Promise<any>
      getSelectedPreset: (userId: string) => Promise<any>
      setSelectedPreset: (userId: string, index: number) => Promise<any>
      getPresetsWithSelection: (userId: string) => Promise<any>
    }
    
    // Core API (preload.ts의 coreAPI 구현과 완전 일치)
    coreAPI?: {
      // 게임 감지 제어
      startDetection: () => Promise<any>
      stopDetection: () => Promise<any>
      getGameStatus: () => Promise<any>
      
      // 프리셋 관련 API
      sendPresetInit: (message: any) => Promise<any>
      sendPresetUpdate: (message: any) => Promise<any>
      
      // 이벤트 리스너들
      onGameStatusChanged: (callback: (data: { status: string }) => void) => () => void
      onForegroundChanged: (callback: (data: { isStarcraftInForeground: boolean }) => void) => () => void
    }
  }
}

export {}
