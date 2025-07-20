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
})

// --------- Expose Core API to the Renderer process ---------
contextBridge.exposeInMainWorld('coreAPI', {
  startDetection: () => ipcRenderer.invoke('core:start-detection'),
  stopDetection: () => ipcRenderer.invoke('core:stop-detection'),
  getGameStatus: () => ipcRenderer.invoke('core:get-game-status'),
  
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
