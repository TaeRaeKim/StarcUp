import { ICoreCommunicationService, INamedPipeService, ICommandRegistry } from './interfaces'
import { ICoreCommand, ICoreResponse, WindowPositionData, WindowPositionEvent, WorkerStatusChangedEvent, GasBuildingAlertEvent, WorkerPresetChangedEvent } from '../types'
import { NamedPipeService } from './NamedPipeService'
import { CommandRegistry } from './CommandRegistry'
import { Commands, Events } from './NamedPipeProtocol'

export class CoreCommunicationService implements ICoreCommunicationService {
  private namedPipeService: INamedPipeService
  private commandRegistry: ICommandRegistry
  private gameStatusChangedCallback: ((status: string) => void) | null = null
  private gameDetectedCallback: ((gameInfo: any) => void) | null = null
  private gameEndedCallback: (() => void) | null = null
  private windowPositionChangedCallback: ((data: WindowPositionData) => void) | null = null
  
  // WorkerManager 이벤트 콜백들
  private workerStatusChangedCallback: ((data: WorkerStatusChangedEvent) => void) | null = null
  private gasBuildingAlertCallback: (() => void) | null = null
  private workerPresetChangedCallback: ((data: WorkerPresetChangedEvent) => void) | null = null
  
  // PopulationManager 이벤트 콜백들
  private supplyAlertCallback: (() => void) | null = null
  
  constructor(namedPipeService?: INamedPipeService) {
    this.namedPipeService = namedPipeService || new NamedPipeService()
    this.commandRegistry = new CommandRegistry()
    
    this.setupDefaultCommands()
    this.setupEventHandlers()
    this.setupConnectionCallbacks()
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
  
  // 프리셋 관련
  async sendPresetInit(message: any): Promise<ICoreResponse> {
    return await this.sendCommand({ 
      type: 'preset:init', 
      payload: message 
    })
  }
  
  async sendPresetUpdate(message: any): Promise<ICoreResponse> {
    return await this.sendCommand({ 
      type: 'preset:update', 
      payload: message 
    })
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

  // 연결 성공 시 콜백 등록/해제 메서드 추가
  private connectionEstablishedCallback: (() => void) | null = null

  onConnectionEstablished(callback: () => void): void {
    this.connectionEstablishedCallback = callback
  }

  offConnectionEstablished(): void {
    this.connectionEstablishedCallback = null
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
      handler: async () => await this.namedPipeService.sendCommand(Commands.StartGameDetect)
    })
    
    this.commandRegistry.register({
      name: 'game:detect:stop',
      handler: async () => await this.namedPipeService.sendCommand(Commands.StopGameDetect)
    })
    
    this.commandRegistry.register({
      name: 'game:status',
      handler: async () => await this.namedPipeService.sendCommand(Commands.GetGameStatus)
    })
    
    this.commandRegistry.register({
      name: 'game:units:count',
      requestValidator: (data): data is { playerId?: number } => 
        data.playerId === undefined || typeof data.playerId === 'number',
      handler: async (req) => {
        // 새로운 프로토콜: payload에 직접 데이터 전송
        return await this.namedPipeService.sendCommand(Commands.GetUnitCounts, req.playerId ? { playerId: req.playerId } : undefined)
      }
    })
    
    this.commandRegistry.register({
      name: 'game:player:info',
      handler: async () => await this.namedPipeService.sendCommand(Commands.GetPlayerInfo)
    })
    
    // 프리셋 관련 명령
    this.commandRegistry.register({
      name: 'preset:init',
      handler: async (req: any) => {
        // 새로운 프로토콜: payload에 직접 데이터 전송
        return await this.namedPipeService.sendCommand(Commands.PresetInit, req)
      }
    })
    
    this.commandRegistry.register({
      name: 'preset:update',
      handler: async (req: any) => {
        // 새로운 프로토콜: payload에 직접 데이터 전송
        return await this.namedPipeService.sendCommand(Commands.PresetUpdate, req)
      }
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

    // WorkerManager 이벤트 핸들러들
    this.namedPipeService.onEvent(Events.WorkerStatusChanged, (data: WorkerStatusChangedEvent) => {
      if (this.workerStatusChangedCallback) {
        this.workerStatusChangedCallback(data)
      }
    })

    this.namedPipeService.onEvent(Events.GasBuildingAlert, (data: GasBuildingAlertEvent) => {
      if (this.gasBuildingAlertCallback) {
        this.gasBuildingAlertCallback()
      }
    })

    this.namedPipeService.onEvent(Events.WorkerPresetChanged, (data: WorkerPresetChangedEvent) => {
      if (this.workerPresetChangedCallback) {
        this.workerPresetChangedCallback(data)
      }
    })

    // PopulationManager 이벤트 핸들러들
    this.namedPipeService.onEvent(Events.SupplyAlert, (data: any) => {
      if (this.supplyAlertCallback) {
        this.supplyAlertCallback()
      }
    })

    // 업그레이드 관련 이벤트 핸들러
    this.namedPipeService.onEvent(Events.UpgradeDataUpdated, (data: any) => {
      //console.log('🔧 업그레이드 데이터 업데이트:', data)
      // 필요시 콜백 추가
    })

    this.namedPipeService.onEvent(Events.UpgradeStateChanged, (data: any) => {
      //console.log('⚡ 업그레이드 상태 변경:', data)
      // 필요시 콜백 추가
    })

    this.namedPipeService.onEvent(Events.UpgradeCompleted, (data: any) => {
      //console.log('✅ 업그레이드 완료:', data)
      // 필요시 콜백 추가
    })

    this.namedPipeService.onEvent(Events.UpgradeCancelled, (data: any) => {
      //console.log('❌ 업그레이드 취소:', data)
      // 필요시 콜백 추가
    })

    console.log('✅ Core 이벤트 핸들러 설정 완료')
  }

  // 연결 콜백 설정
  private setupConnectionCallbacks(): void {
    // NamedPipeService의 연결 성공 콜백 설정
    if (this.namedPipeService.setConnectionEstablishedCallback) {
      this.namedPipeService.setConnectionEstablishedCallback(() => {
        console.log('🔗 Core와 연결 성공 - 프리셋 전송 콜백 호출')
        
        // 연결 성공 콜백 호출
        if (this.connectionEstablishedCallback) {
          try {
            this.connectionEstablishedCallback()
          } catch (error) {
            console.error('❌ 연결 성공 콜백 실행 실패:', error)
          }
        }
      })
    }
    
    console.log('✅ 연결 콜백 설정 완료')
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

  // WorkerManager 이벤트 콜백 등록/해제 메서드들
  onWorkerStatusChanged(callback: (data: WorkerStatusChangedEvent) => void): void {
    this.workerStatusChangedCallback = callback
  }

  offWorkerStatusChanged(): void {
    this.workerStatusChangedCallback = null
  }

  onGasBuildingAlert(callback: () => void): void {
    this.gasBuildingAlertCallback = callback
  }

  offGasBuildingAlert(): void {
    this.gasBuildingAlertCallback = null
  }

  onWorkerPresetChanged(callback: (data: WorkerPresetChangedEvent) => void): void {
    this.workerPresetChangedCallback = callback
  }

  offWorkerPresetChanged(): void {
    this.workerPresetChangedCallback = null
  }

  // PopulationManager 이벤트 콜백 등록/해제 메서드들
  onSupplyAlert(callback: () => void): void {
    this.supplyAlertCallback = callback
  }

  offSupplyAlert(): void {
    this.supplyAlertCallback = null
  }
  
}