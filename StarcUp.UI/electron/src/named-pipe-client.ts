import { createConnection, Socket } from 'net'
import { EventEmitter } from 'events'

export interface NamedPipeResponse {
  success: boolean
  data?: string
  error?: string
}

export interface NamedPipeClientOptions {
  pipeName: string
  reconnectInterval?: number
  maxReconnectAttempts?: number
  responseTimeout?: number
}

export class NamedPipeClient extends EventEmitter {
  private socket: Socket | null = null
  private pipeName: string
  private reconnectInterval: number
  private maxReconnectAttempts: number
  private responseTimeout: number
  private reconnectAttempts = 0
  private isConnected = false
  private isReconnecting = false
  private responseQueue: Array<(response: NamedPipeResponse) => void> = []
  private buffer = ''

  constructor(options: NamedPipeClientOptions) {
    super()
    this.pipeName = options.pipeName
    this.reconnectInterval = options.reconnectInterval ?? 3000
    this.maxReconnectAttempts = options.maxReconnectAttempts ?? 5
    this.responseTimeout = options.responseTimeout ?? 10000
  }

  /**
   * Named Pipe 서버에 연결
   */
  async connect(): Promise<void> {
    if (this.isConnected) {
      return
    }

    return new Promise<void>((resolve, reject) => {
      try {
        console.log(`🔗 Named Pipe 연결 시도: ${this.pipeName}`)

        // Windows Named Pipe 경로 생성
        const pipePath = `\\\\.\\pipe\\${this.pipeName}`
        
        this.socket = createConnection(pipePath)
        
        this.socket.on('connect', () => {
          console.log(`✅ Named Pipe 연결 성공: ${this.pipeName}`)
          this.isConnected = true
          this.isReconnecting = false
          this.reconnectAttempts = 0
          this.setupDataListener()
          this.emit('connected')
          resolve()
        })

        this.socket.on('error', (error) => {
          console.error(`❌ Named Pipe 연결 오류: ${error.message}`)
          this.isConnected = false
          
          if (!this.isReconnecting) {
            this.emit('error', error)
            reject(error)
          }
        })

        this.socket.on('close', () => {
          console.log(`🔌 Named Pipe 연결 닫힘: ${this.pipeName}`)
          this.isConnected = false
          this.emit('disconnected')
          
          // 재연결이 비활성화되어 있으면 재연결 시도하지 않음
          if (!this.isReconnecting && this.maxReconnectAttempts > 0) {
            this.startReconnect()
          }
        })

      } catch (error) {
        console.error(`❌ Named Pipe 연결 실패: ${error}`)
        reject(error)
      }
    })
  }

  /**
   * 연결 해제
   */
  disconnect(): void {
    if (this.socket) {
      this.isReconnecting = false
      this.socket.destroy()
      this.socket = null
    }
    this.isConnected = false
    
    // 대기 중인 응답들 정리
    this.responseQueue.forEach(resolve => 
      resolve({ success: false, error: 'Connection disconnected' })
    )
    this.responseQueue = []
  }

  /**
   * 명령 전송
   */
  async sendCommand(command: string, args: string[] = []): Promise<NamedPipeResponse> {
    if (!this.isConnected || !this.socket) {
      return { success: false, error: 'Not connected to Named Pipe' }
    }

    return new Promise<NamedPipeResponse>((resolve) => {
      const fullCommand = [command, ...args].join(' ')

      // 응답 콜백 등록
      this.responseQueue.push(resolve)

      // 타임아웃 설정
      const timeoutId = setTimeout(() => {
        const index = this.responseQueue.indexOf(resolve)
        if (index > -1) {
          this.responseQueue.splice(index, 1)
          resolve({ success: false, error: 'Command timeout' })
        }
      }, this.responseTimeout)

      try {
        // 명령 전송
        this.socket!.write(fullCommand + '\n')
        console.log(`📤 Named Pipe 명령 전송: ${fullCommand}`)
      } catch (error) {
        clearTimeout(timeoutId)
        const index = this.responseQueue.indexOf(resolve)
        if (index > -1) {
          this.responseQueue.splice(index, 1)
        }
        resolve({ success: false, error: `Send failed: ${error}` })
      }
    })
  }

  /**
   * 데이터 리스너 설정
   */
  private setupDataListener(): void {
    if (!this.socket) return

    this.socket.on('data', (chunk: Buffer) => {
      this.buffer += chunk.toString()
      
      // 줄 단위로 처리
      const lines = this.buffer.split('\n')
      this.buffer = lines.pop() || '' // 마지막 불완전한 줄은 버퍼에 보관

      for (const line of lines) {
        if (line.trim()) {
          this.handleResponse(line.trim())
        }
      }
    })
  }

  /**
   * 응답 처리
   */
  private handleResponse(response: string): void {
    console.log(`📥 Named Pipe 응답 수신: ${response}`)

    try {
      // 응답 파싱 (SUCCESS:data 또는 ERROR:message 형식)
      const isSuccess = response.startsWith('SUCCESS:')
      const isError = response.startsWith('ERROR:')
      
      let result: NamedPipeResponse
      
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
      console.error('❌ Named Pipe 응답 처리 중 오류:', error)
      if (this.responseQueue.length > 0) {
        const resolver = this.responseQueue.shift()
        resolver!({ success: false, error: `Response processing error: ${error}` })
      }
    }
  }

  /**
   * 재연결 시작
   */
  private startReconnect(): void {
    if (this.isReconnecting || this.reconnectAttempts >= this.maxReconnectAttempts) {
      console.log(`🔄 Named Pipe 재연결 중단: 최대 재시도 횟수 초과 (${this.maxReconnectAttempts})`)
      return
    }

    this.isReconnecting = true
    this.reconnectAttempts++

    console.log(`🔄 Named Pipe 재연결 시도 ${this.reconnectAttempts}/${this.maxReconnectAttempts} (${this.reconnectInterval}ms 후)`)

    setTimeout(async () => {
      try {
        await this.connect()
      } catch (error) {
        console.error(`❌ Named Pipe 재연결 실패: ${error}`)
        // 재연결 시도는 connect() 메서드에서 처리됨
      }
    }, this.reconnectInterval)
  }

  /**
   * 연결 상태 확인
   */
  get connected(): boolean {
    return this.isConnected && this.socket !== null
  }

  /**
   * 재연결 중인지 확인
   */
  get reconnecting(): boolean {
    return this.isReconnecting
  }
}