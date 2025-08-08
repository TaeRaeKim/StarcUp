import { ipcRenderer, contextBridge } from 'electron'

// --------- Expose some API to the Renderer process ---------
contextBridge.exposeInMainWorld('ipcRenderer', {
  on(...args: Parameters<typeof ipcRenderer.on>) {
    const [channel, listener] = args
    return ipcRenderer.on(channel, (event, ...args) => listener(event, ...args))
  },
  off(...args: Parameters<typeof ipcRenderer.off>) {
    const [channel, ...omit] = args
    return ipcRenderer.off(channel, ...omit)
  },
  send(...args: Parameters<typeof ipcRenderer.send>) {
    const [channel, ...omit] = args
    return ipcRenderer.send(channel, ...omit)
  },
  invoke(...args: Parameters<typeof ipcRenderer.invoke>) {
    const [channel, ...omit] = args
    return ipcRenderer.invoke(channel, ...omit)
  },

  // You can expose other APTs you need here.
  // ...
})

// --------- Expose window controls to the Renderer process ---------
contextBridge.exposeInMainWorld('electronAPI', {
  minimizeWindow: () => ipcRenderer.invoke('window:minimize'),
  maximizeWindow: () => ipcRenderer.invoke('window:maximize'),
  closeWindow: () => ipcRenderer.invoke('window:close'),
  dragWindow: () => ipcRenderer.invoke('window:drag'),
  toggleOverlay: () => ipcRenderer.invoke('window:toggle-overlay'),
  showOverlay: () => ipcRenderer.invoke('window:show-overlay'),
  hideOverlay: () => ipcRenderer.invoke('window:hide-overlay'),
  resizeWindow: (width: number, height: number) => ipcRenderer.invoke('window:resize', { width, height }),
  
  // 위치 저장/복원 기능
  saveWindowPosition: () => ipcRenderer.invoke('window:save-position'),
  restoreWindowPosition: () => ipcRenderer.invoke('window:restore-position'),
  setWindowPosition: (x: number, y: number) => ipcRenderer.invoke('window:set-position', { x, y }),

  // 오버레이 중앙 위치 업데이트 이벤트 리스너
  onUpdateCenterPosition: (callback: (data: any) => void) => {
    const listener = (_event: any, data: any) => callback(data)
    ipcRenderer.on('update-center-position', listener)
    
    // 리스너 정리 함수 반환
    return () => ipcRenderer.off('update-center-position', listener)
  },

  // WorkerManager 이벤트 리스너들
  onWorkerStatusChanged: (callback: (data: any) => void) => {
    const listener = (_event: any, data: any) => callback(data)
    ipcRenderer.on('worker-status-changed', listener)
    
    // 리스너 정리 함수 반환
    return () => ipcRenderer.off('worker-status-changed', listener)
  },

  onGasBuildingAlert: (callback: () => void) => {
    const listener = (_event: any, data: any) => callback()
    ipcRenderer.on('gas-building-alert', listener)
    
    // 리스너 정리 함수 반환
    return () => ipcRenderer.off('gas-building-alert', listener)
  },

  onWorkerPresetChanged: (callback: (data: any) => void) => {
    const listener = (_event: any, data: any) => callback(data)
    ipcRenderer.on('worker-preset-changed', listener)
    
    // 리스너 정리 함수 반환
    return () => ipcRenderer.off('worker-preset-changed', listener)
  },

  // 오버레이 편집 모드 토글 이벤트 리스너
  onToggleEditMode: (callback: (data: { isEditMode: boolean }) => void) => {
    const listener = (_event: any, data: { isEditMode: boolean }) => callback(data)
    ipcRenderer.on('toggle-edit-mode', listener)
    
    // 리스너 정리 함수 반환
    return () => ipcRenderer.off('toggle-edit-mode', listener)
  },

  // 프리셋 관리 API
  savePreset: (userId: string, preset: any) => 
    ipcRenderer.invoke('data:save-preset', { userId, preset }),
  
  loadPreset: (userId: string, presetId: string) => 
    ipcRenderer.invoke('data:load-preset', { userId, presetId }),
  
  getPresets: (userId: string) => 
    ipcRenderer.invoke('data:get-presets', { userId }),
  
  deletePreset: (userId: string, presetId: string) => 
    ipcRenderer.invoke('data:delete-preset', { userId, presetId }),
  
  updatePreset: (userId: string, presetId: string, updates: any) => 
    ipcRenderer.invoke('data:update-preset', { userId, presetId, updates }),
  
  getSelectedPreset: (userId: string) => 
    ipcRenderer.invoke('data:get-selected-preset', { userId }),
  
  setSelectedPreset: (userId: string, index: number) => 
    ipcRenderer.invoke('data:set-selected-preset', { userId, index }),
  
  getPresetsWithSelection: (userId: string) => 
    ipcRenderer.invoke('data:get-presets-with-selection', { userId }),
})

// --------- Expose Core API to the Renderer process ---------
contextBridge.exposeInMainWorld('coreAPI', {
  startDetection: () => ipcRenderer.invoke('core:start-detection'),
  stopDetection: () => ipcRenderer.invoke('core:stop-detection'),
  getGameStatus: () => ipcRenderer.invoke('core:get-game-status'),
  
  // 프리셋 관련 API
  sendPresetInit: (message: any) => ipcRenderer.invoke('core:send-preset-init', message),
  sendPresetUpdate: (message: any) => ipcRenderer.invoke('core:send-preset-update', message),
  
  // 게임 상태 변경 이벤트 리스너
  onGameStatusChanged: (callback: (data: { status: string }) => void) => {
    const listener = (_event: any, data: { status: string }) => callback(data)
    ipcRenderer.on('game-status-changed', listener)
    
    // 리스너 정리 함수 반환
    return () => ipcRenderer.off('game-status-changed', listener)
  },

  // foreground 상태 변경 이벤트 리스너
  onForegroundChanged: (callback: (data: { isStarcraftInForeground: boolean }) => void) => {
    const listener = (_event: any, data: { isStarcraftInForeground: boolean }) => callback(data)
    ipcRenderer.on('foreground-changed', listener)
    
    // 리스너 정리 함수 반환
    return () => ipcRenderer.off('foreground-changed', listener)
  }
})

// --------- Expose Preset API to the Renderer process ---------
contextBridge.exposeInMainWorld('presetAPI', {
  // 상태 조회 (기본 - Main Page용)
  getCurrent: () => ipcRenderer.invoke('preset:get-current'),
  getState: () => ipcRenderer.invoke('preset:get-state'),
  getAll: () => ipcRenderer.invoke('preset:get-all'),
  
  // Overlay 전용 성능 최적화 메서드
  getFeaturesOnly: () => ipcRenderer.invoke('preset:get-features-only'),
  
  // 프리셋 관리
  switch: (presetId: string) => 
    ipcRenderer.invoke('preset:switch', { presetId }),
  
  updateSettings: (presetType: string, settings: any) => 
    ipcRenderer.invoke('preset:update-settings', { presetType, settings }),
  
  toggleFeature: (featureIndex: number, enabled: boolean) => 
    ipcRenderer.invoke('preset:toggle-feature', { featureIndex, enabled }),
  
  // 프리셋 상태 변경 이벤트 리스너 (기본 - Main Page용)
  onStateChanged: (callback: (data: any) => void) => {
    const listener = (_event: any, data: any) => callback(data)
    ipcRenderer.on('preset:state-changed', listener)
    
    // 리스너 정리 함수 반환
    return () => ipcRenderer.off('preset:state-changed', listener)
  },
  
  // Overlay 전용 기능 상태 변경 이벤트 리스너 (성능 최적화)
  onFeaturesChanged: (callback: (data: { featureStates: boolean[], timestamp: Date }) => void) => {
    const listener = (_event: any, data: { featureStates: boolean[], timestamp: Date }) => callback(data)
    ipcRenderer.on('preset:features-changed', listener)
    
    // 리스너 정리 함수 반환
    return () => ipcRenderer.off('preset:features-changed', listener)
  }
})
