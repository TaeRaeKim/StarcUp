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
    console.log(`🔍 [sendCommand] 명령 시작: ${command}, 연결상태: ${this.isConnected}, 소켓존재: ${!!this.clientSocket}`)
    
    if (!this.isConnected || !this.clientSocket) {
      console.error(`❌ [sendCommand] 연결 실패 - 연결상태: ${this.isConnected}, 소켓: ${!!this.clientSocket}`)
      return {
        success: false,
        error: 'StarcUp.Core 클라이언트가 연결되지 않았습니다.'
      }
    }
    
    // 새로운 프로토콜을 사용하여 Request 메시지 생성
    const payload = args.length > 0 ? { args } : undefined
    const request = NamedPipeProtocol.createRequest(command, payload)
    const message = JSON.stringify(request)
    
    console.log(`📤 [sendCommand] 전송 준비 - ID: ${request.id}, 메시지: ${message}`)
    
    return new Promise((resolve, reject) => {
      try {
        console.log(`📤 StarcUp.Core에 명령 전송: ${command}`, args)
        
        // 타임아웃 설정 (15초)
        const timeout = setTimeout(() => {
          console.error(`⏰ [sendCommand] 타임아웃 - ID: ${request.id}, 명령: ${command}`)
          this.pendingCommands.delete(request.id)
          reject(new Error(`명령 실행 시간 초과: ${command}`))
        }, 15000)
        
        // 응답 대기 등록
        this.pendingCommands.set(request.id, { resolve, reject, timeout })
        console.log(`📋 [sendCommand] 대기중인 명령 등록 - ID: ${request.id}, 총 대기중: ${this.pendingCommands.size}`)
        
        // 메시지 전송
        const written = this.clientSocket!.write(message + '\n')
        console.log(`✉️ [sendCommand] 메시지 전송 완료 - written: ${written}, bytes: ${Buffer.byteLength(message + '\n')}`)
        
      } catch (error) {
        console.error('❌ [sendCommand] 명령 전송 실패:', error)
        reject(error)
      }
    })
  }
  
  private handleCoreConnection(socket: net.Socket): void {
    console.log(`🔗 [handleCoreConnection] Core 클라이언트 연결 처리 시작`)
    console.log(`🔗 [handleCoreConnection] 소켓 정보 - localAddress: ${socket.localAddress}, remoteAddress: ${socket.remoteAddress}`)
    
    // 기존 클라이언트 연결이 있다면 해제
    if (this.clientSocket) {
      console.log(`🔄 [handleCoreConnection] 기존 클라이언트 연결 해제`)
      this.clientSocket.removeAllListeners()
      this.clientSocket.destroy()
    }
    
    this.clientSocket = socket
    this.isConnected = true
    this.isReconnecting = false
    
    console.log(`✅ [handleCoreConnection] 연결 상태 업데이트 - 연결됨: ${this.isConnected}`)
    
    // 데이터 수신 핸들러
    socket.on('data', (data) => {
      console.log(`📨 [handleCoreConnection] 데이터 수신 - 크기: ${data.length} bytes`)
      console.log(`📨 [handleCoreConnection] 원본 데이터: ${data.toString()}`)
      this.handleIncomingData(data)
    })
    
    // 에러 핸들러
    socket.on('error', (error) => {
      console.error('❌ [handleCoreConnection] StarcUp.Core 소켓 에러:', error)
      this.handleCoreDisconnection()
    })
    
    // 연결 종료 핸들러
    socket.on('close', () => {
      console.log('🔌 [handleCoreConnection] StarcUp.Core 연결이 종료되었습니다')
      this.handleCoreDisconnection()
    })
    
    console.log(`🎉 [handleCoreConnection] Core 클라이언트 연결 처리 완료`)
  }
  
  private handleIncomingData(data: Buffer): void {
    console.log(`📥 [handleIncomingData] 메시지 처리 시작 - 크기: ${data.length} bytes`)
    
    try {
      const rawData = data.toString().trim()
      console.log(`📥 [handleIncomingData] 원본 데이터: "${rawData}"`)
      
      const messages = rawData.split('\n')
      console.log(`📥 [handleIncomingData] 분할된 메시지 수: ${messages.length}`)
      
      for (const messageText of messages) {
        if (!messageText) {
          console.log(`📥 [handleIncomingData] 빈 메시지 건너뜀`)
          continue
        }
        
        console.log(`📥 [handleIncomingData] 메시지 파싱 시도: "${messageText}"`)
        const message = JSON.parse(messageText)
        console.log(`📥 [handleIncomingData] 파싱된 메시지:`, message)
        
        // 새로운 프로토콜로 메시지 타입 확인
        if (NamedPipeProtocol.isRequest(message)) {
          console.log(`📥 [handleIncomingData] Core 요청 처리: ${message.command}`)
          this.handleIncomingRequest(message)
        }
        else if (NamedPipeProtocol.isResponse(message)) {
          console.log(`📥 [handleIncomingData] 응답 처리 - RequestID: ${message.requestId}`)
          this.handleIncomingResponse(message)
        }
        else if (NamedPipeProtocol.isEvent(message)) {
          console.log(`📥 [handleIncomingData] 이벤트 처리: ${message.event}`)
          this.handleIncomingEvent(message)
        }
        else {
          console.log('📨 [handleIncomingData] 알 수 없는 메시지 형식:', message)
        }
      }
    } catch (error) {
      console.error('❌ 메시지 파싱 실패:', error, 'Raw data:', data.toString())
    }
  }
  
  // 새로운 프로토콜 메시지 처리 메서드들
  private handleIncomingRequest(message: RequestMessage): void {
    // UI가 서버이므로 Core에서 요청이 오는 경우 (ping 등)
    console.log(`📨 [handleIncomingRequest] Core 요청: ${message.command}`)
    
    if (message.command === Commands.Ping) {
      const response = NamedPipeProtocol.createResponse(
        message.id, 
        true, 
        { message: 'pong', status: 'UI서버 정상' }
      )
      
      try {
        if (this.clientSocket) {
          this.clientSocket.write(JSON.stringify(response) + '\n')
          console.log(`📤 [handleIncomingRequest] Ping 응답 전송 완료`)
        }
      } catch (error) {
        console.error('❌ [handleIncomingRequest] Ping 응답 전송 실패:', error)
      }
    } else {
      console.log(`⚠️ [handleIncomingRequest] 처리되지 않은 요청: ${message.command}`)
    }
  }

  private handleIncomingResponse(message: ResponseMessage): void {
    // UI에서 Core로 보낸 요청의 응답 처리
    console.log(`📥 [handleIncomingResponse] 응답 처리 - RequestID: ${message.requestId}`)
    
    if (this.pendingCommands.has(message.requestId)) {
      const { resolve, timeout } = this.pendingCommands.get(message.requestId)!
      clearTimeout(timeout)
      this.pendingCommands.delete(message.requestId)
      
      const response: ICoreResponse = {
        success: message.success,
        data: message.data,
        error: message.error
      }
      
      console.log(`✅ [handleIncomingResponse] 응답 처리 완료 - ID: ${message.requestId}, 성공: ${response.success}`)
      resolve(response)
    } else {
      console.log(`⚠️ [handleIncomingResponse] 대기중인 요청을 찾을 수 없음 - ID: ${message.requestId}`)
    }
  }

  private handleIncomingEvent(message: EventMessage): void {
    console.log(`📢 [handleIncomingEvent] 이벤트 처리: ${message.event}`)
    
    // 기존 이벤트 핸들러 시스템 사용
    const handler = this.eventHandlers.get(message.event)
    if (handler) {
      try {
        handler(message.data)
      } catch (error) {
        console.error(`❌ [handleIncomingEvent] 이벤트 핸들러 실행 실패 (${message.event}):`, error)
      }
    } else {
      console.log(`⚠️ [handleIncomingEvent] 등록되지 않은 이벤트: ${message.event}`)
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
}