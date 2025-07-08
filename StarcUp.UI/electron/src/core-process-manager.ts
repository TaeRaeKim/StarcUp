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

export class CoreProcessManager {
  private coreProcess: ChildProcess | null = null
  private namedPipeClient: NamedPipeClient | null = null
  private isConnected = false
  private pipeName = 'StarcUp.Core'
  private healthCheckInterval: NodeJS.Timeout | null = null
  private lastHealthCheck = Date.now()
  private autoReconnect = true

  /**
   * StarcUp.Core 프로세스 시작 및 Named Pipe 통신 연결
   */
  async startCoreProcess(): Promise<void> {
    if (this.coreProcess) {
      console.log('StarcUp.Core 프로세스가 이미 실행 중입니다.')
      return
    }

    try {
      console.log('🚀 StarcUp.Core 프로세스 시작 중 (Named Pipe 모드)...')

      // StarcUp.Core 실행 파일 경로 찾기
      const coreExePath = this.findCoreExecutable()
      if (!coreExePath) {
        throw new Error('StarcUp.Core.exe를 찾을 수 없습니다.')
      }

      console.log(`📂 StarcUp.Core 실행: ${coreExePath}`)

      // 실행 파일 위치를 작업 디렉토리로 설정
      const coreWorkingDir = path.dirname(coreExePath)
      
      console.log(`📂 작업 디렉토리: ${coreWorkingDir}`)
      
      // data 폴더 확인 (실행 파일과 같은 위치)
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

      // Named Pipe 모드로 StarcUp.Core 실행
      this.coreProcess = spawn(coreExePath, [this.pipeName], {
        stdio: ['ignore', 'pipe', 'pipe'],
        detached: false,
        windowsHide: true,
        cwd: coreWorkingDir
      })

      // Named Pipe 클라이언트 생성 및 연결
      this.namedPipeClient = new NamedPipeClient({
        pipeName: this.pipeName,
        reconnectInterval: 3000,
        maxReconnectAttempts: 5,
        responseTimeout: 10000
      })

      // Named Pipe 이벤트 핸들러 설정
      this.setupNamedPipeEventHandlers()

      // 프로세스 시작 후 잠시 대기
      await new Promise(resolve => setTimeout(resolve, 2000))

      // Named Pipe 연결
      await this.namedPipeClient.connect()

      this.isConnected = true
      this.startHealthCheck()
      console.log('✅ StarcUp.Core 프로세스 연결 완료 (Named Pipe)')

      // 프로세스 이벤트 핸들러 설정
      this.setupProcessEventHandlers()

    } catch (error) {
      console.error('❌ StarcUp.Core 프로세스 시작 실패:', error)
      await this.stopCoreProcess()
      throw error
    }
  }

  /**
   * StarcUp.Core 프로세스 중지
   */
  async stopCoreProcess(): Promise<void> {
    if (!this.coreProcess) {
      return
    }

    try {
      console.log('🔌 StarcUp.Core 프로세스 종료 중...')

      this.isConnected = false
      this.autoReconnect = false

      // 헬스 체크 중지
      this.stopHealthCheck()

      // Named Pipe 클라이언트 종료
      if (this.namedPipeClient) {
        this.namedPipeClient.disconnect()
        this.namedPipeClient = null
      }

      // 프로세스 종료
      if (this.coreProcess) {
        this.coreProcess.kill('SIGTERM')
        this.coreProcess = null
      }


      console.log('✅ StarcUp.Core 프로세스 종료 완료')
    } catch (error) {
      console.error('⚠️ StarcUp.Core 프로세스 종료 중 오류:', error)
    }
  }

  /**
   * 명령 전송
   */
  async sendCommand(command: string, args: string[] = []): Promise<CoreProcessResponse> {
    if (!this.isConnected || !this.namedPipeClient) {
      return { success: false, error: 'Named Pipe 클라이언트가 연결되지 않았습니다.' }
    }

    const response = await this.namedPipeClient.sendCommand(command, args)
    return {
      success: response.success,
      data: response.data,
      error: response.error
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
      // Development 환경에서 직접 실행
      path.join(process.env.APP_ROOT, '..', '..', 'StarcUp.Core', 'bin', 'Debug', 'net8.0-windows', 'StarcUp.Core.exe'),
      path.join(process.env.APP_ROOT, '..', '..', 'StarcUp.Core', 'bin', 'Release', 'net8.0-windows', 'StarcUp.Core.exe')
    ]

    for (const exePath of possiblePaths) {
      if (fs.existsSync(exePath)) {
        console.log(`📂 StarcUp.Core 발견: ${exePath}`)
        return exePath
      }
    }

    console.error('❌ StarcUp.Core.exe를 다음 경로에서 찾을 수 없습니다:')
    possiblePaths.forEach(p => console.error(`   - ${p}`))
    return null
  }

  /**
   * 프로세스 이벤트 핸들러 설정
   */
  private setupProcessEventHandlers(): void {
    if (!this.coreProcess) return

    this.coreProcess.stderr?.on('data', (data) => {
      console.error(`[StarcUp.Core STDERR] ${data.toString()}`)
    })

    this.coreProcess.on('exit', (code, signal) => {
      console.log(`🔌 StarcUp.Core 프로세스 종료 (코드: ${code}, 시그널: ${signal})`)
      this.isConnected = false
      this.coreProcess = null
      
      // 자동 재연결 시도
      if (this.autoReconnect) {
        this.attemptReconnect()
      }
    })

    this.coreProcess.on('error', (error) => {
      console.error('❌ StarcUp.Core 프로세스 오류:', error)
      this.isConnected = false
      
      // 자동 재연결 시도
      if (this.autoReconnect) {
        this.attemptReconnect()
      }
    })
  }

  /**
   * Named Pipe 이벤트 핸들러 설정
   */
  private setupNamedPipeEventHandlers(): void {
    if (!this.namedPipeClient) return

    this.namedPipeClient.on('connected', () => {
      console.log('✅ Named Pipe 클라이언트 연결됨')
    })

    this.namedPipeClient.on('disconnected', () => {
      console.log('🔌 Named Pipe 클라이언트 연결 해제됨')
      this.isConnected = false
      
      // 자동 재연결 시도
      if (this.autoReconnect) {
        this.attemptReconnect()
      }
    })

    this.namedPipeClient.on('error', (error) => {
      console.error('❌ Named Pipe 클라이언트 오류:', error)
      this.isConnected = false
      
      // 자동 재연결 시도
      if (this.autoReconnect) {
        this.attemptReconnect()
      }
    })
  }

  /**
   * 연결 상태 확인
   */
  get connected(): boolean {
    return this.isConnected && this.namedPipeClient !== null && this.namedPipeClient.connected
  }

  /**
   * 헬스 체크 시작
   */
  private startHealthCheck(): void {
    if (this.healthCheckInterval) {
      clearInterval(this.healthCheckInterval)
    }

    this.healthCheckInterval = setInterval(async () => {
      try {
        // 마지막 헬스 체크로부터 30초가 지났으면 핑 전송
        if (Date.now() - this.lastHealthCheck > 30000) {
          const response = await this.sendCommand('ping')
          
          if (response.success) {
            this.lastHealthCheck = Date.now()
            console.log('💓 StarcUp.Core 헬스 체크 성공')
          } else {
            console.warn('⚠️ StarcUp.Core 헬스 체크 실패')
            if (this.autoReconnect) {
              this.attemptReconnect()
            }
          }
        }
      } catch (error) {
        console.error('❌ 헬스 체크 중 오류:', error)
        if (this.autoReconnect) {
          this.attemptReconnect()
        }
      }
    }, 30000) // 30초마다 헬스 체크
  }

  /**
   * 헬스 체크 중지
   */
  private stopHealthCheck(): void {
    if (this.healthCheckInterval) {
      clearInterval(this.healthCheckInterval)
      this.healthCheckInterval = null
    }
  }

  /**
   * 재연결 시도
   */
  private async attemptReconnect(): Promise<void> {
    if (!this.autoReconnect || this.isConnected) {
      return
    }

    console.log('🔄 StarcUp.Core 재연결 시도 중...')

    try {
      // 기존 연결 정리
      if (this.namedPipeClient) {
        this.namedPipeClient.disconnect()
      }

      // 프로세스가 종료되었는지 확인
      if (this.coreProcess && this.coreProcess.killed) {
        console.log('🚀 StarcUp.Core 프로세스 재시작 중...')
        await this.startCoreProcess()
      } else {
        // Named Pipe 재연결 시도
        this.namedPipeClient = new NamedPipeClient({
          pipeName: this.pipeName,
          reconnectInterval: 3000,
          maxReconnectAttempts: 5,
          responseTimeout: 10000
        })

        this.setupNamedPipeEventHandlers()
        await this.namedPipeClient.connect()
        
        this.isConnected = true
        this.startHealthCheck()
        console.log('✅ StarcUp.Core 재연결 성공')
      }
    } catch (error) {
      console.error('❌ StarcUp.Core 재연결 실패:', error)
      
      // 5초 후 다시 시도
      setTimeout(() => {
        if (this.autoReconnect) {
          this.attemptReconnect()
        }
      }, 5000)
    }
  }

  /**
   * 자동 재연결 활성화/비활성화
   */
  setAutoReconnect(enabled: boolean): void {
    this.autoReconnect = enabled
    console.log(`🔄 자동 재연결: ${enabled ? '활성화' : '비활성화'}`)
  }
}