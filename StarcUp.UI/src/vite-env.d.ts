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
      
      // 업그레이드 이벤트 리스너들
      onUpgradeInit: (callback: (data: any) => void) => () => void
      onUpgradeDataUpdated: (callback: (data: any) => void) => () => void
      onUpgradeCompleted: (callback: (data: any) => void) => () => void
      onUpgradeCancelled: (callback: (data: any) => void) => () => void
      
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
    
    // Preset API (새로운 중앙 관리형 프리셋 API)
    presetAPI?: {
      // 상태 조회 (기본 - Main Page용)
      getCurrent: () => Promise<{ success: boolean; data?: any; error?: string }>
      getState: () => Promise<{ success: boolean; data?: any; error?: string }>
      getAll: () => Promise<{ success: boolean; data?: any[]; error?: string }>
      
      // Overlay 전용 성능 최적화 메서드
      getFeaturesOnly: () => Promise<{ success: boolean; data?: { featureStates: boolean[]; selectedRace: number; upgradeSettings?: any }; error?: string }>
      
      // 프리셋 관리
      switch: (presetId: string) => Promise<{ success: boolean; error?: string }>
      updateSettings: (presetType: string, settings: any) => Promise<{ success: boolean; error?: string }>
      updateBatch: (updates: any) => Promise<{ success: boolean; error?: string }>
      toggleFeature: (featureIndex: number, enabled: boolean) => Promise<{ success: boolean; error?: string }>
      
      // Pro 기능 해제 (구독 기간 종료 시 사용)
      sanitizeAllPresetsForNonPro: () => Promise<{ success: boolean; data?: any; error?: string }>
      sanitizePresetForNonPro: (presetId: string) => Promise<{ success: boolean; data?: any; error?: string }>
      refreshState: () => Promise<{ success: boolean; error?: string }>
      
      // 이벤트 리스너 (기본 - Main Page용)
      onStateChanged: (callback: (data: {
        type: 'preset-switched' | 'settings-updated' | 'feature-toggled' | 'presets-loaded'
        preset: any | null
        state: {
          currentPreset: any | null
          allPresets: any[]
          selectedPresetIndex: number
          isLoading: boolean
          lastUpdated: Date
        }
        timestamp: Date
      }) => void) => () => void
      
      // Overlay 전용 기능 상태 변경 이벤트 리스너 (성능 최적화)
      onFeaturesChanged: (callback: (data: { 
        featureStates: boolean[]
        selectedRace: number
        upgradeSettings?: any
        timestamp: Date 
      }) => void) => () => void
    }
  }
}

export {}
