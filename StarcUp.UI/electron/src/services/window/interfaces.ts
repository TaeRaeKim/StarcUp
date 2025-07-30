import { IWindowConfiguration } from '../types'

export interface IWindowManager {
  // 윈도우 생성
  createMainWindow(): void
  createOverlayWindow(): void
  
  // 윈도우 접근자
  getMainWindow(): Electron.BrowserWindow | null
  getOverlayWindow(): Electron.BrowserWindow | null
  
  // 메인 윈도우 제어
  minimizeMain(): void
  maximizeMain(): void
  closeMain(): void
  focusMain(): void
  centerMain(): void
  resizeMain(width: number, height: number): void
  
  // 오버레이 제어
  toggleOverlay(): void
  showOverlay(): void
  hideOverlay(): void
  setOverlayPosition(x: number, y: number): void
  setOverlaySize(width: number, height: number): void
  setOverlayOpacity(opacity: number): void
  setOverlayMouseEvents(ignore: boolean): void
  
  // 개발 도구
  toggleDevTools(): void
  toggleOverlayDevTools(): void
  
  // 윈도우 상태 확인
  isMainWindowVisible(): boolean
  isOverlayWindowVisible(): boolean
  isMainWindowMaximized(): boolean
  isMainWindowMinimized(): boolean
  
  // 윈도우 정보 조회
  getMainWindowBounds(): Electron.Rectangle | null
  getOverlayWindowBounds(): Electron.Rectangle | null
  
  // 위치 저장/복원 기능
  saveMainWindowPosition(): void
  restoreMainWindowPosition(): void
  setMainWindowPosition(x: number, y: number): void
  
  // 윈도우 통신
  sendToMainWindow(channel: string, data: any): void
  sendToOverlayWindow(channel: string, data: any): void
  broadcastToAllWindows(channel: string, data: any): void
  
  // 윈도우 설정 업데이트
  updateWindowConfig(config: Partial<IWindowConfiguration>): void
  applyMainWindowConfig(config: Partial<Electron.BrowserWindowConstructorOptions>): void
  applyOverlayWindowConfig(config: Partial<Electron.BrowserWindowConstructorOptions>): void
  
  // 정리
  cleanup(): void
}

export interface IShortcutManager {
  registerShortcuts(): void
  registerCustomShortcut(accelerator: string, callback: () => void): boolean
  unregisterShortcut(accelerator: string): void
  unregisterAllShortcuts(): void
  isRegistered(accelerator: string): boolean
  getRegisteredShortcuts(): string[]
  setOverlayAutoManager(overlayAutoManager: import('../overlay/interfaces').IOverlayAutoManager): void
  getEditMode(): boolean
  setEditMode(editMode: boolean): void
}