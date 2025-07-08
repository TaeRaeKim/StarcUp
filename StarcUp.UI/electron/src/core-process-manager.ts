import { spawn, ChildProcess } from 'child_process'
import { app } from 'electron'
import * as path from 'path'
import * as fs from 'fs'
import { NamedPipeClient, NamedPipeResponse } from './named-pipe-client'

export interface CoreProcessResponse {
  success: boolean
  data?: string
  error?: string
}

// 연결 상태 열거형
enum ConnectionState {
  DISCONNECTED = 'disconnected',
  CONNECTING = 'connecting',
  CONNECTED = 'connected',
  RECONNECTING = 'reconnecting',
  FAILED = 'failed'
}

export class CoreProcessManager {
  private coreProcess: ChildProcess | null = null
  private namedPipeClient: NamedPipeClient | null = null
  private connectionState: ConnectionState = ConnectionState.DISCONNECTED
  private pipeName = process.env.NODE_ENV === 'development'
    ? 'StarcUp.Core.Dev'
    : 'StarcUp.Core'
  
  // 재연결 관리
  private autoReconnect = true
  private isDevelopment = false
  private reconnectAttempts = 0
  private maxReconnectAttempts = 5
  private baseReconnectDelay = 3000 // 3초
  private maxReconnectDelay = 30000 // 30초
  private reconnectTimer: NodeJS.Timeout | null = null
  
  // 헬스체크
  private healthCheckInterval: NodeJS.Timeout | null = null
  private lastHealthCheck = Date.now()

  /**
   * StarcUp.Core 프로세스 시작 및 Named Pipe 통신 연결
   */
  async startCoreProcess(isDevelopment: boolean = false): Promise<void> {
    this.isDevelopment = isDevelopment
    
    if (this.connectionState === ConnectionState.CONNECTING || 
        this.connectionState === ConnectionState.CONNECTED) {
      console.log('⚠️ 이미 연결 중이거나 연결된 상태입니다.')
      return
    }

    this.connectionState = ConnectionState.CONNECTING
    console.log(`🚀 StarcUp.Core 연결 시작 (${isDevelopment ? '개발' : '프로덕션'} 모드)`)

    try {
      // Named Pipe 클라이언트 생성
      await this.createNamedPipeClient()
      
      // 프로덕션 모드에서는 프로세스 시작
      if (!this.isDevelopment) {
        await this.startCoreProcessInternal()
      }
      
      // 연결 시도
      await this.connectToPipe()
      
      this.connectionState = ConnectionState.CONNECTED
      this.reconnectAttempts = 0
      this.startHealthCheck()
      
      console.log('✅ StarcUp.Core 연결 완료')
    } catch (error) {
      console.error('❌ StarcUp.Core 연결 실패:', error)
      this.connectionState = ConnectionState.FAILED
      
      if (this.autoReconnect) {
        this.scheduleReconnect()
      }
      throw error
    }
  }

  /**
   * StarcUp.Core 프로세스 중지
   */
  async stopCoreProcess(): Promise<void> {
    console.log('🔌 StarcUp.Core 연결 해제 중...')
    
    this.autoReconnect = false
    this.connectionState = ConnectionState.DISCONNECTED
    
    // 재연결 타이머 정리
    if (this.reconnectTimer) {
      clearTimeout(this.reconnectTimer)
      this.reconnectTimer = null
    }
    
    // 헬스체크 중지
    this.stopHealthCheck()
    
    // Named Pipe 클라이언트 정리
    if (this.namedPipeClient) {
      this.namedPipeClient.disconnect()
      this.namedPipeClient = null
    }
    
    // 프로세스 종료 (프로덕션 모드에서만)
    if (!this.isDevelopment && this.coreProcess) {
      this.coreProcess.kill('SIGTERM')
      this.coreProcess = null
    }
    
    console.log('✅ StarcUp.Core 연결 해제 완료')
  }

  /**
   * 명령 전송
   */
  async sendCommand(command: string, args: string[] = []): Promise<CoreProcessResponse> {
    if (this.connectionState !== ConnectionState.CONNECTED || !this.namedPipeClient) {
      return { success: false, error: 'StarcUp.Core가 연결되지 않았습니다.' }
    }

    try {
      const response = await this.namedPipeClient.sendCommand(command, args)
      return {
        success: response.success,
        data: response.data,
        error: response.error
      }
    } catch (error) {
      console.error('❌ 명령 전송 실패:', error)
      return { success: false, error: error.message }
    }
  }

  /**
   * 연결 상태 확인
   */
  get connected(): boolean {
    return this.connectionState === ConnectionState.CONNECTED && 
           this.namedPipeClient !== null && 
           this.namedPipeClient.connected
  }

  /**
   * 자동 재연결 활성화/비활성화
   */
  setAutoReconnect(enabled: boolean): void {
    this.autoReconnect = enabled
    console.log(`🔄 자동 재연결: ${enabled ? '활성화' : '비활성화'}`)
  }

  // ========== Private Methods ==========

  /**
   * Named Pipe 클라이언트 생성
   */
  private async createNamedPipeClient(): Promise<void> {
    if (this.namedPipeClient) {
      this.namedPipeClient.disconnect()
    }

    this.namedPipeClient = new NamedPipeClient({
      pipeName: this.pipeName,
      reconnectInterval: 0, // CoreProcessManager에서 재연결 관리
      maxReconnectAttempts: 0, // CoreProcessManager에서 재연결 관리
      responseTimeout: 10000
    })

    // 이벤트 핸들러 설정 (최소한만)
    this.namedPipeClient.on('disconnected', () => {
      console.log('🔌 Named Pipe 연결 해제됨')
      this.handleDisconnection()
    })

    this.namedPipeClient.on('error', (error) => {
      console.error('❌ Named Pipe 오류:', error)
      this.handleDisconnection()
    })
  }

  /**
   * Core 프로세스 시작 (프로덕션 모드만)
   */
  private async startCoreProcessInternal(): Promise<void> {
    const coreExePath = this.findCoreExecutable()
    if (!coreExePath) {
      throw new Error('StarcUp.Core.exe를 찾을 수 없습니다.')
    }

    console.log(`📂 StarcUp.Core 실행: ${coreExePath}`)

    const coreWorkingDir = path.dirname(coreExePath)
    
    // 필수 파일 확인
    this.validateRequiredFiles(coreWorkingDir)

    // 프로세스 시작
    this.coreProcess = spawn(coreExePath, [this.pipeName], {
      stdio: ['ignore', 'pipe', 'pipe'],
      detached: false,
      windowsHide: true,
      cwd: coreWorkingDir
    })

    // 프로세스 이벤트 핸들러
    this.coreProcess.on('exit', (code, signal) => {
      console.log(`🔌 StarcUp.Core 프로세스 종료 (코드: ${code})`)
      this.coreProcess = null
      this.handleDisconnection()
    })

    this.coreProcess.on('error', (error) => {
      console.error('❌ StarcUp.Core 프로세스 오류:', error)
      this.handleDisconnection()
    })

    // 프로세스 시작 대기
    await new Promise(resolve => setTimeout(resolve, 2000))
  }

  /**
   * Named Pipe 연결
   */
  private async connectToPipe(): Promise<void> {
    if (!this.namedPipeClient) {
      throw new Error('Named Pipe 클라이언트가 생성되지 않았습니다.')
    }

    await this.namedPipeClient.connect()
    console.log('✅ Named Pipe 연결 완료')
  }

  /**
   * 연결 해제 처리
   */
  private handleDisconnection(): void {
    if (this.connectionState === ConnectionState.DISCONNECTED || 
        this.connectionState === ConnectionState.RECONNECTING) {
      return // 이미 해제 처리됨 또는 재연결 중
    }

    console.log('🔌 연결 해제 감지')
    this.connectionState = ConnectionState.DISCONNECTED
    this.stopHealthCheck()

    if (this.autoReconnect) {
      this.scheduleReconnect()
    }
  }

  /**
   * 재연결 스케줄링
   */
  private scheduleReconnect(): void {
    if (this.connectionState === ConnectionState.RECONNECTING) {
      return // 이미 재연결 예약됨
    }

    this.reconnectAttempts++
    
    if (this.reconnectAttempts > this.maxReconnectAttempts) {
      console.error(`❌ 최대 재연결 시도 횟수 초과 (${this.maxReconnectAttempts}회)`)
      this.connectionState = ConnectionState.FAILED
      return
    }

    // 지수 백오프로 대기 시간 계산
    const delay = Math.min(
      this.baseReconnectDelay * Math.pow(2, this.reconnectAttempts - 1),
      this.maxReconnectDelay
    )

    console.log(`🔄 ${delay/1000}초 후 재연결 시도 (${this.reconnectAttempts}/${this.maxReconnectAttempts})`)
    this.connectionState = ConnectionState.RECONNECTING

    this.reconnectTimer = setTimeout(async () => {
      try {
        await this.performReconnect()
      } catch (error) {
        console.error('❌ 재연결 실패:', error)
        this.connectionState = ConnectionState.DISCONNECTED
        this.scheduleReconnect() // 다음 재시도 예약
      }
    }, delay)
  }

  /**
   * 실제 재연결 수행
   */
  private async performReconnect(): Promise<void> {
    console.log('🔄 재연결 시도 중...')
    
    // 기존 연결 정리
    if (this.namedPipeClient) {
      this.namedPipeClient.disconnect()
      this.namedPipeClient = null
    }

    // 새로운 연결 시도
    await this.createNamedPipeClient()
    
    // 프로덕션 모드에서 프로세스가 죽었으면 재시작
    if (!this.isDevelopment && (!this.coreProcess || this.coreProcess.killed)) {
      await this.startCoreProcessInternal()
    }
    
    await this.connectToPipe()
    
    this.connectionState = ConnectionState.CONNECTED
    this.reconnectAttempts = 0
    this.startHealthCheck()
    
    console.log('✅ 재연결 성공')
  }

  /**
   * 헬스체크 시작
   */
  private startHealthCheck(): void {
    this.stopHealthCheck()
    
    this.healthCheckInterval = setInterval(async () => {
      try {
        // 60초마다 핑 전송
        if (Date.now() - this.lastHealthCheck > 60000) {
          const response = await this.sendCommand('ping')
          
          if (response.success) {
            this.lastHealthCheck = Date.now()
            console.log('💓 헬스체크 성공')
          } else {
            console.warn('⚠️ 헬스체크 실패')
            this.handleDisconnection()
          }
        }
      } catch (error) {
        console.error('❌ 헬스체크 오류:', error)
        this.handleDisconnection()
      }
    }, 60000) // 60초마다
  }

  /**
   * 헬스체크 중지
   */
  private stopHealthCheck(): void {
    if (this.healthCheckInterval) {
      clearInterval(this.healthCheckInterval)
      this.healthCheckInterval = null
    }
  }

  /**
   * 필수 파일 확인
   */
  private validateRequiredFiles(coreWorkingDir: string): void {
    const dataFolderPath = path.join(coreWorkingDir, 'data')
    const requiredFiles = [
      path.join(dataFolderPath, 'starcraft_units.json'),
      path.join(dataFolderPath, 'all_race_unit_offsets.json')
    ]
    
    for (const filePath of requiredFiles) {
      if (!fs.existsSync(filePath)) {
        console.error(`❌ 필수 파일 없음: ${filePath}`)
      } else {
        console.log(`✅ 파일 확인: ${filePath}`)
      }
    }
  }

  /**
   * StarcUp.Core 실행 파일 찾기
   */
  private findCoreExecutable(): string | null {
    const appPath = app.getAppPath()
    const possiblePaths = [
      path.join(appPath, '..', '..', 'StarcUp.Core', 'bin', 'Debug', 'net8.0-windows', 'StarcUp.Core.exe'),
      path.join(appPath, '..', '..', 'StarcUp.Core', 'bin', 'Release', 'net8.0-windows', 'StarcUp.Core.exe'),
      path.join(process.cwd(), 'StarcUp.Core', 'bin', 'Debug', 'net8.0-windows', 'StarcUp.Core.exe'),
      path.join(process.cwd(), 'StarcUp.Core', 'bin', 'Release', 'net8.0-windows', 'StarcUp.Core.exe'),
      path.join(process.cwd(), '..', 'StarcUp.Core', 'bin', 'Debug', 'net8.0-windows', 'StarcUp.Core.exe'),
      path.join(process.cwd(), '..', 'StarcUp.Core', 'bin', 'Release', 'net8.0-windows', 'StarcUp.Core.exe'),
      path.join(process.env.APP_ROOT || '', '..', '..', 'StarcUp.Core', 'bin', 'Debug', 'net8.0-windows', 'StarcUp.Core.exe'),
      path.join(process.env.APP_ROOT || '', '..', '..', 'StarcUp.Core', 'bin', 'Release', 'net8.0-windows', 'StarcUp.Core.exe')
    ]

    for (const exePath of possiblePaths) {
      if (fs.existsSync(exePath)) {
        console.log(`📂 StarcUp.Core 발견: ${exePath}`)
        return exePath
      }
    }

    console.error('❌ StarcUp.Core.exe를 찾을 수 없습니다.')
    return null
  }
}