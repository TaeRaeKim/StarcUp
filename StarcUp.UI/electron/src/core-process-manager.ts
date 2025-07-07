import { spawn, ChildProcess } from 'child_process'
import { app } from 'electron'
import * as path from 'path'
import * as fs from 'fs'

export interface CoreProcessResponse {
  success: boolean
  data?: string
  error?: string
}

export class CoreProcessManager {
  private coreProcess: ChildProcess | null = null
  private isConnected = false
  private responseQueue: Array<(response: CoreProcessResponse) => void> = []

  /**
   * StarcUp.Core 프로세스 시작 및 stdio 통신 연결
   */
  async startCoreProcess(): Promise<void> {
    if (this.coreProcess) {
      console.log('StarcUp.Core 프로세스가 이미 실행 중입니다.')
      return
    }

    try {
      console.log('🚀 StarcUp.Core 프로세스 시작 중...')

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

      // StarcUp.Core 프로세스 실행 (stdio를 통한 통신)
      this.coreProcess = spawn(coreExePath, ['stdio', 'stdio'], {
        stdio: ['pipe', 'pipe', 'pipe'],
        detached: false,
        windowsHide: true,
        cwd: coreWorkingDir  // 실행 파일 위치를 작업 디렉토리로 설정
      })

      // 응답 리스너 설정
      this.setupResponseListener()

      // 프로세스 이벤트 핸들러 설정
      this.setupProcessEventHandlers()

      this.isConnected = true
      console.log('✅ StarcUp.Core 프로세스 연결 완료')

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

      // 프로세스 종료
      if (this.coreProcess) {
        this.coreProcess.kill('SIGTERM')
        this.coreProcess = null
      }

      // 대기 중인 응답 정리
      this.responseQueue.forEach(resolve => 
        resolve({ success: false, error: 'Core 프로세스가 종료되었습니다.' })
      )
      this.responseQueue = []

      console.log('✅ StarcUp.Core 프로세스 종료 완료')
    } catch (error) {
      console.error('⚠️ StarcUp.Core 프로세스 종료 중 오류:', error)
    }
  }

  /**
   * 명령 전송
   */
  async sendCommand(command: string, args: string[] = []): Promise<CoreProcessResponse> {
    if (!this.isConnected || !this.coreProcess || !this.coreProcess.stdin) {
      return { success: false, error: 'Core 프로세스가 연결되지 않았습니다.' }
    }

    return new Promise((resolve) => {
      const fullCommand = [command, ...args].join(' ')

      // 응답 콜백 등록
      this.responseQueue.push(resolve)

      // 타임아웃 설정 (10초)
      setTimeout(() => {
        const index = this.responseQueue.indexOf(resolve)
        if (index > -1) {
          this.responseQueue.splice(index, 1)
          resolve({ success: false, error: '명령 실행 시간 초과' })
        }
      }, 10000)

      try {
        // 명령 전송
        this.coreProcess!.stdin!.write(fullCommand + '\n')
        console.log(`📤 명령 전송: ${fullCommand}`)
      } catch (error) {
        const index = this.responseQueue.indexOf(resolve)
        if (index > -1) {
          this.responseQueue.splice(index, 1)
        }
        resolve({ success: false, error: `명령 전송 실패: ${error}` })
      }
    })
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
   * 응답 리스너 설정
   */
  private setupResponseListener(): void {
    if (!this.coreProcess || !this.coreProcess.stdout) return

    let buffer = ''

    this.coreProcess.stdout.on('data', (chunk: Buffer) => {
      buffer += chunk.toString()
      
      // 줄 단위로 처리
      const lines = buffer.split('\n')
      buffer = lines.pop() || '' // 마지막 불완전한 줄은 버퍼에 보관

      for (const line of lines) {
        if (line.trim()) {
          this.handleResponse(line.trim())
        }
      }
    })

    this.coreProcess.stdout.on('error', (error) => {
      console.error('❌ stdout 스트림 오류:', error)
    })
  }

  /**
   * 응답 처리
   */
  private handleResponse(response: string): void {
    console.log(`📥 응답 수신: ${response}`)

    try {
      // 응답 파싱 (SUCCESS:data 또는 ERROR:message 형식)
      const isSuccess = response.startsWith('SUCCESS:')
      const isError = response.startsWith('ERROR:')
      
      let result: CoreProcessResponse
      
      if (isSuccess) {
        const data = response.substring(8) // 'SUCCESS:' 제거
        result = { success: true, data }
      } else if (isError) {
        const error = response.substring(6) // 'ERROR:' 제거
        result = { success: false, error }
      } else {
        // 알 수 없는 응답 형식 - 원시 데이터로 처리
        result = { success: true, data: response }
      }

      // 대기 중인 첫 번째 명령의 응답 해결
      if (this.responseQueue.length > 0) {
        const resolver = this.responseQueue.shift()
        resolver!(result)
      }
    } catch (error) {
      console.error('❌ 응답 처리 중 오류:', error)
      if (this.responseQueue.length > 0) {
        const resolver = this.responseQueue.shift()
        resolver!({ success: false, error: `응답 처리 오류: ${error}` })
      }
    }
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
      
      // 대기 중인 모든 응답 처리
      this.responseQueue.forEach(resolve => 
        resolve({ success: false, error: 'Core 프로세스가 예상치 못하게 종료되었습니다.' })
      )
      this.responseQueue = []
    })

    this.coreProcess.on('error', (error) => {
      console.error('❌ StarcUp.Core 프로세스 오류:', error)
      this.isConnected = false
    })
  }

  /**
   * 연결 상태 확인
   */
  get connected(): boolean {
    return this.isConnected && this.coreProcess !== null
  }
}