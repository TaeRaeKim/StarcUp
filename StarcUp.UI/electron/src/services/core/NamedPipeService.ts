import * as net from 'net'
import { INamedPipeService } from './interfaces'
import { ICoreResponse } from '../types'

export class NamedPipeService implements INamedPipeService {
  private pipeName: string
  private pipePath: string
  private server: net.Server | null = null
  private clientSocket: net.Socket | null = null
  private isConnected: boolean = false
  private isReconnecting: boolean = false
  private pendingCommands: Map<string, { resolve: (value: ICoreResponse) => void; reject: (reason?: any) => void; timeout: NodeJS.Timeout }> = new Map()
  private eventHandlers: Map<string, (data: any) => void> = new Map()
  
  constructor(pipeName: string = 'StarcUp') {
    this.pipeName = pipeName
    this.pipePath = `\\\\.\\pipe\\${pipeName}`
  }
  
  async connect(): Promise<void> {

    console.log(`🎧 Named Pipe 서버 시작: ${this.pipePath}`)
    
    return new Promise((resolve, reject) => {
      this.server = net.createServer((socket) => {
        console.log('✅ StarcUp.Core 클라이언트가 연결되었습니다')
        this.handleCoreConnection(socket)
      })
      
      this.server.listen(this.pipePath, () => {
        console.log(`🎧 Named Pipe 서버가 ${this.pipePath}에서 대기 중입니다`)
        resolve()
      })
      
      this.server.on('error', (error: any) => {
        console.error('❌ Named Pipe 서버 에러:', error)
        
        // 파이프가 이미 사용 중인 경우
        if (error.code === 'EADDRINUSE') {
          console.log('🔄 기존 서버를 정리하고 재시도합니다...')
          this.cleanup()
          setTimeout(() => this.connect().then(resolve).catch(reject), 1000)
          return
        }
        reject(error)
      })
    })
  }
  
  disconnect(): void {
    console.log(`🔌 Named Pipe 서버 종료: ${this.pipeName}`)
    this.cleanup()
  }
  
  private cleanup(): void {
    // 대기 중인 명령들 취소
    this.pendingCommands.forEach(({ reject, timeout }) => {
      clearTimeout(timeout)
      reject(new Error('서버가 종료되었습니다'))
    })
    this.pendingCommands.clear()
    
    // 클라이언트 소켓 정리
    if (this.clientSocket) {
      this.clientSocket.removeAllListeners()
      this.clientSocket.destroy()
      this.clientSocket = null
    }
    
    // 서버 정리
    if (this.server) {
      this.server.close()
      this.server.removeAllListeners()
      this.server = null
    }
    
    this.isConnected = false
    this.isReconnecting = false
  }
  
  async sendCommand(command: string, args: string[] = []): Promise<ICoreResponse> {
    if (!this.isConnected || !this.clientSocket) {
      return {
        success: false,
        error: 'StarcUp.Core 클라이언트가 연결되지 않았습니다.'
      }
    }
    
    const commandId = `cmd_${Date.now()}_${Math.random().toString(36).substr(2, 9)}`
    const message = JSON.stringify({ 
      id: commandId,
      command, 
      args, 
      timestamp: Date.now() 
    })
    
    return new Promise((resolve, reject) => {
      try {
        console.log(`📤 StarcUp.Core에 명령 전송: ${command}`, args)
        
        // 타임아웃 설정 (15초)
        const timeout = setTimeout(() => {
          this.pendingCommands.delete(commandId)
          reject(new Error(`명령 실행 시간 초과: ${command}`))
        }, 15000)
        
        // 응답 대기 등록
        this.pendingCommands.set(commandId, { resolve, reject, timeout })
        
        // 메시지 전송
        this.clientSocket!.write(message + '\n')
        
      } catch (error) {
        console.error('❌ 명령 전송 실패:', error)
        reject(error)
      }
    })
  }
  
  private handleCoreConnection(socket: net.Socket): void {
    // 기존 클라이언트 연결이 있다면 해제
    if (this.clientSocket) {
      this.clientSocket.removeAllListeners()
      this.clientSocket.destroy()
    }
    
    this.clientSocket = socket
    this.isConnected = true
    this.isReconnecting = false
    
    // 데이터 수신 핸들러
    socket.on('data', (data) => {
      this.handleIncomingData(data)
    })
    
    // 에러 핸들러
    socket.on('error', (error) => {
      console.error('❌ StarcUp.Core 소켓 에러:', error)
      this.handleCoreDisconnection()
    })
    
    // 연결 종료 핸들러
    socket.on('close', () => {
      console.log('🔌 StarcUp.Core 연결이 종료되었습니다')
      this.handleCoreDisconnection()
    })
  }
  
  private handleIncomingData(data: Buffer): void {
    try {
      const rawData = data.toString().trim()
      const messages = rawData.split('\n')
      
      for (const messageText of messages) {
        if (!messageText) continue
        
        const message = JSON.parse(messageText)
        
        // Core에서 서버로 보내는 명령인 경우 (ping 등)
        if (message.id && message.command) {
          this.handleCommand(message)
        }
        // 명령 응답인 경우
        else if (message.id && this.pendingCommands.has(message.id)) {
          const { resolve, timeout } = this.pendingCommands.get(message.id)!
          clearTimeout(timeout)
          this.pendingCommands.delete(message.id)
          
          const response: ICoreResponse = {
            success: message.success !== false,
            data: message.data,
            error: message.error
          }
          
          resolve(response)
        }
        // StarcUp.Core에서 보내는 이벤트나 알림인 경우
        else if (message.type === 'event') {
          console.log('📨 StarcUp.Core 이벤트 수신:', message)
          this.handleCoreEvent(message)
        }
        // 기타 메시지
        else {
          console.log('📨 StarcUp.Core 메시지 수신:', message)
        }
      }
    } catch (error) {
      console.error('❌ 메시지 파싱 실패:', error, 'Raw data:', data.toString())
    }
  }
  
  private handleCommand(message: any): void {
    const { id, command, args } = message
    
    // ping 명령 처리
    if (command === 'ping') {
      const status = args && args[0] ? args[0] : 'unknown'
      
      const response = {
        id: id,
        success: true,
        data: {
          message: 'pong',
          timestamp: Date.now(),
          status: 'UI서버 정상',
          received: status
        }
      }
      
      try {
        if (this.clientSocket) {
          this.clientSocket.write(JSON.stringify(response) + '\n')
        }
      } catch (error) {
        console.error('❌ ping 응답 전송 실패:', error)
      }
    }
    // 향후 다른 명령들 처리 가능
    else {
      console.log(`⚠️ 처리되지 않은 명령: ${command}`)
      
      const errorResponse = {
        id: id,
        success: false,
        error: `Unknown command: ${command}`
      }
      
      try {
        if (this.clientSocket) {
          this.clientSocket.write(JSON.stringify(errorResponse) + '\n')
        }
      } catch (error) {
        console.error('❌ 오류 응답 전송 실패:', error)
      }
    }
  }

  private handleCoreDisconnection(): void {
    this.isConnected = false
    
    if (this.clientSocket) {
      this.clientSocket.removeAllListeners()
      this.clientSocket = null
    }
    
    // 대기 중인 명령들에 에러 응답
    this.pendingCommands.forEach(({ reject, timeout }) => {
      clearTimeout(timeout)
      reject(new Error('StarcUp.Core 연결이 끊어졌습니다'))
    })
    this.pendingCommands.clear()
    
    console.log('⏳ StarcUp.Core 재연결을 대기합니다...')
  }
  
  get connected(): boolean {
    return this.isConnected
  }
  
  get reconnecting(): boolean {
    return this.isReconnecting
  }
  
  // 연결 상태 확인 및 서버 시작
  async startConnection(isDevelopment: boolean = false): Promise<void> {
    try {
      await this.connect()
      console.log('🎧 Named Pipe 서버가 StarcUp.Core 연결을 대기합니다')
      
      if (!isDevelopment) {
        console.log('📋 프로덕션 모드: StarcUp.Core 프로세스를 시작해주세요')
        // 참고: Core 프로세스 관리는 CoreProcessManager에서 담당
      }
    } catch (error) {
      console.error('❌ Named Pipe 서버 시작 실패:', error)
      throw error
    }
  }
  
  async stopConnection(): Promise<void> {
    this.disconnect()
    console.log('✅ Named Pipe 서버가 정상적으로 종료되었습니다')
  }

  // 이벤트 핸들러 등록
  onEvent(eventType: string, handler: (data: any) => void): void {
    this.eventHandlers.set(eventType, handler)
  }

  // 이벤트 핸들러 제거
  offEvent(eventType: string): void {
    this.eventHandlers.delete(eventType)
  }

  // Core 이벤트 처리
  private handleCoreEvent(message: any): void {
    const { event, data } = message
    
    if (event) {
      const handler = this.eventHandlers.get(event)
      if (handler) {
        try {
          handler(data)
        } catch (error) {
          console.error(`❌ 이벤트 핸들러 실행 실패 (${event}):`, error)
        }
      } else {
        console.log(`⚠️ 등록되지 않은 이벤트: ${event}`)
      }
    }
  }
}