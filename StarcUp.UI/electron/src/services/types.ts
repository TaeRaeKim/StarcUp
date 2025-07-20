// 공통 타입 정의

export interface ICoreCommand {
  type: string
  payload: any
}

export interface ICoreResponse<T = any> {
  success: boolean
  data?: T
  error?: string
}

export interface IUser {
  id: string
  username: string
  email: string
  settings: IUserSettings
  createdAt: Date
  lastLoginAt: Date
}

export interface IUserSettings {
  theme: 'light' | 'dark'
  language: 'ko' | 'en'
  autoStart: boolean
  overlaySettings: IOverlaySettings
  hotkeySettings: IHotkeySettings
}

export interface IOverlaySettings {
  enabled: boolean
  position: { x: number; y: number }
  size: { width: number; height: number }
  opacity: number
}

export interface IHotkeySettings {
  toggleOverlay: string
  startDetection: string
  stopDetection: string
}

export interface IPreset {
  id: string
  name: string
  type: 'overlay' | 'hotkey' | 'ui' | 'game'
  data: any
  createdAt: Date
  updatedAt: Date
}

export interface IUserData {
  userId: string
  settings: IUserSettings
  presets: IPreset[]
  gameHistory: IGameHistory[]
  lastModified: Date
}

export interface IGameHistory {
  id: string
  gameMode: string
  race: string
  opponent: string
  result: 'win' | 'lose' | 'draw'
  duration: number
  playedAt: Date
  unitStats: any
}

export interface IWindowConfiguration {
  main: Electron.BrowserWindowConstructorOptions
  overlay: Electron.BrowserWindowConstructorOptions
}

// IPC 채널 타입 정의
export interface IIPCChannel {
  request: any
  response: any
}

export interface IIPCChannels {
  // Core 관련
  'core:status': { request: void; response: { connected: boolean } }
  'core:start-detection': { request: void; response: ICoreResponse }
  'core:stop-detection': { request: void; response: ICoreResponse }
  'core:get-game-status': { request: void; response: ICoreResponse }
  'core:get-unit-counts': { request: { playerId?: number }; response: ICoreResponse }
  'core:send-preset-init': { request: any; response: ICoreResponse }
  'core:send-preset-update': { request: any; response: ICoreResponse }
  
  // 인증 관련
  'auth:login': { request: { username: string; password: string }; response: { success: boolean; token?: string; user?: IUser } }
  'auth:logout': { request: void; response: { success: boolean } }
  'auth:get-session': { request: void; response: { user?: IUser } }
  
  // 데이터 관련
  'data:save-preset': { request: { userId: string; preset: IPreset }; response: { success: boolean; id?: string } }
  'data:load-preset': { request: { userId: string; presetId: string }; response: { success: boolean; data?: any } }
  'data:get-presets': { request: { userId: string }; response: { presets: IPreset[] } }
  'data:delete-preset': { request: { userId: string; presetId: string }; response: { success: boolean } }
  
  // 윈도우 관리
  'window:minimize': { request: void; response: void }
  'window:maximize': { request: void; response: void }
  'window:close': { request: void; response: void }
  'window:drag': { request: void; response: void }
  'window:toggle-overlay': { request: void; response: void }
  'window:show-overlay': { request: void; response: void }
  'window:hide-overlay': { request: void; response: void }
  'window:resize': { request: { width: number; height: number }; response: void }
  'window:get-window-bounds': { request: void; response: { main: Electron.Rectangle | null; overlay: Electron.Rectangle | null } }
  'window:set-overlay-position': { request: { x: number; y: number }; response: void }
  'window:set-overlay-size': { request: { width: number; height: number }; response: void }
  'window:set-overlay-opacity': { request: { opacity: number }; response: void }
  'window:toggle-dev-tools': { request: void; response: void }
  'window:save-position': { request: void; response: void }
  'window:restore-position': { request: void; response: void }
  'window:set-position': { request: { x: number; y: number }; response: void }
  
  // 단축키 관리
  'shortcut:register': { request: { accelerator: string; action: string }; response: { success: boolean } }
  'shortcut:unregister': { request: { accelerator: string }; response: { success: boolean } }
  'shortcut:list': { request: void; response: { shortcuts: string[] } }
  
  // 메시지 전송 (renderer에서 수신만 가능)
  'main-process-message': { request: never; response: string }
}

// Command Registry 타입
export interface ICommandDefinition<TRequest = any, TResponse = any> {
  name: string
  requestValidator?: (data: any) => data is TRequest
  responseValidator?: (data: any) => data is TResponse
  handler: (request: TRequest) => Promise<TResponse>
}

// 윈도우 위치 동기화 관련 타입
export interface WindowPositionData {
  x: number
  y: number
  width: number
  height: number
  clientX: number
  clientY: number
  clientWidth: number
  clientHeight: number
  isMinimized: boolean
  isMaximized: boolean
  isVisible: boolean
  timestamp: string
}

export interface WindowPositionEvent {
  eventType: 'window-position-changed' | 'window-size-changed'
  windowPosition: WindowPositionData
}

export interface CenterPositionData {
  x: number
  y: number
  gameAreaBounds: {
    x: number
    y: number
    width: number
    height: number
  }
}

export interface OverlaySyncSettings {
  enablePositionSync: boolean      // 위치 동기화 활성화
  syncThrottleMs: number          // 동기화 Throttling 간격
  offsetX: number                 // X축 오프셋
  offsetY: number                 // Y축 오프셋
  scaleX: number                  // X축 스케일 (1.0 = 100%)
  scaleY: number                  // Y축 스케일 (1.0 = 100%)
}

// WorkerManager 이벤트 타입 정의
export interface WorkerStatusChangedEvent {
  totalWorkers: number
  calculatedTotal: number
  idleWorkers: number
  productionWorkers: number
  activeWorkers: number
}

export interface GasBuildingAlertEvent {
  // 빈 객체 - 알림만 필요
}

export interface PresetInfo {
  mask: number
  flags: string[]
}

export interface WorkerPresetChangedEvent {
  success: boolean
  previousPreset: PresetInfo
  currentPreset: PresetInfo
}