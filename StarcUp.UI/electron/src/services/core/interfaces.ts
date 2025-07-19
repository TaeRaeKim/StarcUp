import { ICoreCommand, ICoreResponse, ICommandDefinition, WindowPositionData } from '../types'

export interface INamedPipeService {
  connect(): Promise<void>
  disconnect(): void
  sendCommand(command: string, args?: string[]): Promise<ICoreResponse>
  get connected(): boolean
  get reconnecting(): boolean
  
  // 연결 관리 메서드
  startConnection(isDevelopment?: boolean): Promise<void>
  stopConnection(): Promise<void>
  
  // 이벤트 핸들러 관리
  onEvent(eventType: string, handler: (data: any) => void): void
  offEvent(eventType: string): void
}

export interface ICommandRegistry {
  register<TReq, TRes>(definition: ICommandDefinition<TReq, TRes>): void
  execute<TReq, TRes>(commandName: string, request: TReq): Promise<TRes>
  getRegisteredCommands(): string[]
}

export interface ICoreCommunicationService {
  // 게임 감지 관련
  startGameDetection(): Promise<ICoreResponse>
  stopGameDetection(): Promise<ICoreResponse>
  getGameStatus(): Promise<ICoreResponse>
  
  // 인게임 데이터 관련
  getUnitCounts(playerId?: number): Promise<ICoreResponse>
  getPlayerInfo(): Promise<ICoreResponse>
  
  // 확장 가능한 명령 시스템
  sendCommand<T>(command: ICoreCommand): Promise<ICoreResponse<T>>
  
  // 연결 상태
  get isConnected(): boolean
  
  // 연결 관리
  startConnection(isDevelopment?: boolean): Promise<void>
  stopConnection(): Promise<void>
  
  // 게임 상태 변경 콜백
  onGameStatusChanged(callback: (status: string) => void): void
  offGameStatusChanged(): void
  
  // 게임 감지/종료 콜백
  onGameDetected(callback: (gameInfo: any) => void): void
  offGameDetected(): void
  onGameEnded(callback: () => void): void
  offGameEnded(): void

  // 윈도우 위치 변경 콜백
  onWindowPositionChanged(callback: (data: WindowPositionData) => void): void
  offWindowPositionChanged(): void
}