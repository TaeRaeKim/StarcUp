import { ICoreCommunicationService, INamedPipeService, ICommandRegistry } from './interfaces'
import { ICoreCommand, ICoreResponse, WindowPositionData, WindowPositionEvent } from '../types'
import { NamedPipeService } from './NamedPipeService'
import { CommandRegistry } from './CommandRegistry'

export class CoreCommunicationService implements ICoreCommunicationService {
  private namedPipeService: INamedPipeService
  private commandRegistry: ICommandRegistry
  private gameStatusChangedCallback: ((status: string) => void) | null = null
  private gameDetectedCallback: ((gameInfo: any) => void) | null = null
  private gameEndedCallback: (() => void) | null = null
  private windowPositionChangedCallback: ((data: WindowPositionData) => void) | null = null
  
  constructor(namedPipeService?: INamedPipeService) {
    this.namedPipeService = namedPipeService || new NamedPipeService()
    this.commandRegistry = new CommandRegistry()
    
    this.setupDefaultCommands()
    this.setupEventHandlers()
  }
  
  // 게임 감지 관련
  async startGameDetection(): Promise<ICoreResponse> {
    return await this.sendCommand({ type: 'game:detect:start', payload: {} })
  }
  
  async stopGameDetection(): Promise<ICoreResponse> {
    return await this.sendCommand({ type: 'game:detect:stop', payload: {} })
  }
  
  async getGameStatus(): Promise<ICoreResponse> {
    return await this.sendCommand({ type: 'game:status', payload: {} })
  }
  
  // 인게임 데이터 관련
  async getUnitCounts(playerId?: number): Promise<ICoreResponse> {
    return await this.sendCommand({ 
      type: 'game:units:count', 
      payload: { playerId } 
    })
  }
  
  async getPlayerInfo(): Promise<ICoreResponse> {
    return await this.sendCommand({ type: 'game:player:info', payload: {} })
  }
  
  // 확장 가능한 명령 시스템
  async sendCommand<T>(command: ICoreCommand): Promise<ICoreResponse<T>> {
    return await this.commandRegistry.execute(command.type, command.payload)
  }
  
  get isConnected(): boolean {
    return this.namedPipeService.connected
  }
  
  // 연결 시작 메서드 추가
  async startConnection(isDevelopment: boolean = false): Promise<void> {
    try {
      // Named Pipe Server 시작 (Electron이 Server 역할)
      console.log('🔗 Named Pipe Server 시작 중...')
      await this.namedPipeService.startConnection(isDevelopment)
      
      console.log('✅ Core 통신 연결 완료')
    } catch (error) {
      console.error('❌ Core 통신 연결 실패:', error)
      throw error
    }
  }
  
  // 연결 종료 메서드 추가
  async stopConnection(): Promise<void> {
    try {
      // Named Pipe Server 종료
      await this.namedPipeService.stopConnection()
      
      console.log('✅ Core 통신 연결 종료 완료')
    } catch (error) {
      console.error('❌ Core 통신 연결 종료 중 오류:', error)
      throw error
    }
  }
  
  private setupDefaultCommands(): void {
    // 기본 명령어들 등록 (ping은 NamedPipeService에서 직접 처리)
    this.commandRegistry.register({
      name: 'game:detect:start',
      handler: async () => await this.namedPipeService.sendCommand('start-game-detect')
    })
    
    this.commandRegistry.register({
      name: 'game:detect:stop',
      handler: async () => await this.namedPipeService.sendCommand('stop-game-detect')
    })
    
    this.commandRegistry.register({
      name: 'game:status',
      handler: async () => await this.namedPipeService.sendCommand('get-game-status')
    })
    
    this.commandRegistry.register({
      name: 'game:units:count',
      requestValidator: (data): data is { playerId?: number } => 
        data.playerId === undefined || typeof data.playerId === 'number',
      handler: async (req) => {
        const args = req.playerId ? [req.playerId.toString()] : []
        return await this.namedPipeService.sendCommand('get-unit-counts', args)
      }
    })
    
    this.commandRegistry.register({
      name: 'game:player:info',
      handler: async () => await this.namedPipeService.sendCommand('get-player-info')
    })
    
    console.log('✅ 기본 Core 명령어 등록 완료')
  }

  // 이벤트 핸들러 설정
  private setupEventHandlers(): void {
    // 게임 프로세스 감지 이벤트 핸들러
    this.namedPipeService.onEvent('game-detected', (data: any) => {
      console.log('🔍 게임 프로세스 감지됨:', data)
      if (this.gameDetectedCallback && data.gameInfo) {
        this.gameDetectedCallback(data.gameInfo)
      }
      if (this.gameStatusChangedCallback) {
        this.gameStatusChangedCallback('waiting')
      }
    })

    // 게임 프로세스 종료 이벤트 핸들러
    this.namedPipeService.onEvent('game-ended', (data: any) => {
      console.log('🔚 게임 프로세스 종료됨:', data)
      if (this.gameEndedCallback) {
        this.gameEndedCallback()
      }
      if (this.gameStatusChangedCallback) {
        this.gameStatusChangedCallback('game-ended')
      }
    })


    // 인게임 상태 이벤트 핸들러
    this.namedPipeService.onEvent('in-game-status', (data: any) => {      
      if (this.gameStatusChangedCallback) {
        const isInGame = data?.inGameInfo?.isInGame === true
        const newStatus = isInGame ? 'playing' : 'waiting'
        this.gameStatusChangedCallback(newStatus)
      }
    })

    // 윈도우 위치 변경 이벤트 핸들러
    this.namedPipeService.onEvent('window-position-changed', (data: WindowPositionEvent) => {
      if (this.windowPositionChangedCallback) {
        this.windowPositionChangedCallback(data.windowPosition)
      }
    })
    
    this.namedPipeService.onEvent('window-size-changed', (data: WindowPositionEvent) => {
      if (this.windowPositionChangedCallback) {
        this.windowPositionChangedCallback(data.windowPosition)
      }
    })
    
    this.namedPipeService.onEvent('window-overlay-init', (data: WindowPositionEvent) => {
      if (this.windowPositionChangedCallback) {
        this.windowPositionChangedCallback(data.windowPosition)
      }
    })

    console.log('✅ Core 이벤트 핸들러 설정 완료')
  }

  // 게임 상태 변경 콜백 등록
  onGameStatusChanged(callback: (status: string) => void): void {
    this.gameStatusChangedCallback = callback
  }

  // 게임 상태 변경 콜백 제거
  offGameStatusChanged(): void {
    this.gameStatusChangedCallback = null
  }

  // 게임 감지 콜백 설정
  onGameDetected(callback: (gameInfo: any) => void): void {
    this.gameDetectedCallback = callback
  }

  // 게임 감지 콜백 제거
  offGameDetected(): void {
    this.gameDetectedCallback = null
  }

  // 게임 종료 콜백 설정
  onGameEnded(callback: () => void): void {
    this.gameEndedCallback = callback
  }

  // 게임 종료 콜백 제거
  offGameEnded(): void {
    this.gameEndedCallback = null
  }

  // 윈도우 위치 변경 콜백 등록
  onWindowPositionChanged(callback: (data: WindowPositionData) => void): void {
    this.windowPositionChangedCallback = callback
  }

  // 윈도우 위치 변경 콜백 제거
  offWindowPositionChanged(): void {
    this.windowPositionChangedCallback = null
  }
  
}