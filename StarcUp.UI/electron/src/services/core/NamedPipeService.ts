import * as net from 'net'
import { INamedPipeService } from './interfaces'
import { ICoreResponse } from '../types'
import { NamedPipeProtocol, MessageType, RequestMessage, ResponseMessage, EventMessage, Commands } from './NamedPipeProtocol'

export class NamedPipeService implements INamedPipeService {
  private pipeName: string
  private pipePath: string
  private server: net.Server | null = null
  private clientSocket: net.Socket | null = null
  private isConnected: boolean = false
  private isReconnecting: boolean = false
  private connectionEstablishedCallback: (() => void) | null = null
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
  
  async sendCommand(command: string, payload?: any): Promise<ICoreResponse> {
    if (!this.isConnected || !this.clientSocket) {
      return {
        success: false,
        error: 'StarcUp.Core 클라이언트가 연결되지 않았습니다.'
      }
    }
    
    // 새로운 프로토콜을 사용하여 Request 메시지 생성
    const request = NamedPipeProtocol.createRequest(command, payload)
    const message = JSON.stringify(request)
    
    return new Promise((resolve, reject) => {
      try {
            console.log(`📤 Request: { type: "${MessageType[request.type]}", command: "${command}", id: "${request.id}", timestamp: ${request.timestamp}, payload: ${JSON.stringify(payload)} }`)
        
        // 타임아웃 설정 (15초)
        const timeout = setTimeout(() => {
          console.error(`⏰ 타임아웃 - ID: ${request.id}, 명령: ${command}`)
          this.pendingCommands.delete(request.id)
          reject(new Error(`명령 실행 시간 초과: ${command}`))
        }, 15000)
        
        // 응답 대기 등록
        this.pendingCommands.set(request.id, { resolve, reject, timeout })
        
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

    // 연결 성공 콜백 호출
    if (this.connectionEstablishedCallback) {
      console.log('📞 연결 성공 콜백 호출')
      try {
        this.connectionEstablishedCallback()
      } catch (error) {
        console.error('❌ 연결 성공 콜백 실행 실패:', error)
      }
    }
    
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
        if (!messageText) {
          continue
        }
        
        const message = JSON.parse(messageText)
        
        // 새로운 프로토콜로 메시지 타입 확인
        if (NamedPipeProtocol.isRequest(message)) {
          this.handleIncomingRequest(message)
        }
        else if (NamedPipeProtocol.isResponse(message)) {
          this.handleIncomingResponse(message)
        }
        else if (NamedPipeProtocol.isEvent(message)) {
          this.handleIncomingEvent(message)
        }
        else {
          console.log('📨 알 수 없는 메시지 형식:', message)
        }
      }
    } catch (error) {
      console.error('❌ 메시지 파싱 실패:', error, 'Raw data:', data.toString())
    }
  }
  
  // 새로운 프로토콜 메시지 처리 메서드들
  private handleIncomingRequest(message: RequestMessage): void {
    console.log(`📨 Request: { type: "${MessageType[message.type]}", command: "${message.command}", id: "${message.id}", timestamp: ${message.timestamp} }`)
    
    if (message.command === Commands.Ping) {
      const response = NamedPipeProtocol.createResponse(
        message.id, 
        true, 
        { message: 'pong', status: 'UI서버 정상' }
      )
      
      try {
        if (this.clientSocket) {
          this.clientSocket.write(JSON.stringify(response) + '\n')
          console.log(`📤 Response: { type: "${MessageType[response.type]}", id: "${response.id}", requestId: "${message.id}", success: true, timestamp: ${response.timestamp}, data: ${JSON.stringify(response.data)} }`)
        }
      } catch (error) {
        console.error('❌ Ping 응답 전송 실패:', error)
      }
    } else {
      console.log(`⚠️ 처리되지 않은 요청: ${message.command}`)
    }
  }

  private handleIncomingResponse(message: ResponseMessage): void {
    console.log(`📥 Response: { type: "${MessageType[message.type]}", id: "${message.id}", requestId: "${message.requestId}", success: ${message.success}, timestamp: ${message.timestamp}, data: ${JSON.stringify(message.data)} }`)
    
    if (this.pendingCommands.has(message.requestId)) {
      const { resolve, timeout } = this.pendingCommands.get(message.requestId)!
      clearTimeout(timeout)
      this.pendingCommands.delete(message.requestId)
      
      const response: ICoreResponse = {
        success: message.success,
        data: message.data,
        error: message.error
      }
      
      resolve(response)
    } else {
      console.log(`⚠️ 대기중인 요청을 찾을 수 없음 - ID: ${message.requestId}`)
    }
  }

  private handleIncomingEvent(message: EventMessage): void {
    console.log(`📢 Event: { type: "${MessageType[message.type]}", id: "${message.id}", event: "${message.event}", timestamp: ${message.timestamp}, data: ${JSON.stringify(message.data)} }`)
    
    // 기존 이벤트 핸들러 시스템 사용
    const handler = this.eventHandlers.get(message.event)
    if (handler) {
      try {
        handler(message.data)
      } catch (error) {
        console.error(`❌ 이벤트 핸들러 실행 실패 (${message.event}):`, error)
      }
    } else {
      console.log(`⚠️ 등록되지 않은 이벤트: ${message.event}`)
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

  // 연결 성공 콜백 설정
  setConnectionEstablishedCallback(callback: () => void): void {
    this.connectionEstablishedCallback = callback
  }

  // 연결 성공 콜백 제거
  clearConnectionEstablishedCallback(): void {
    this.connectionEstablishedCallback = null
  }
}