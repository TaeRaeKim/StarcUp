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
  }
})
