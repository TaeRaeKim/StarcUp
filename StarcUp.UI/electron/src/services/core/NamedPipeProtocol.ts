/**
 * Named Pipe 통신 프로토콜 정의
 */

export enum MessageType {
  Request = 0,
  Response = 1, 
  Event = 2
}

/**
 * 기본 메시지 구조
 */
export interface BaseMessage {
  id: string
  type: MessageType
  timestamp: number
}

/**
 * 요청 메시지
 */
export interface RequestMessage extends BaseMessage {
  type: MessageType.Request
  command: string
  payload?: any
}

/**
 * 응답 메시지
 */
export interface ResponseMessage extends BaseMessage {
  type: MessageType.Response
  requestId: string
  success: boolean
  data?: any
  error?: string
}

/**
 * 이벤트 메시지
 */
export interface EventMessage extends BaseMessage {
  type: MessageType.Event
  event: string
  data?: any
}

/**
 * 모든 메시지 타입의 유니온
 */
export type NamedPipeMessage = RequestMessage | ResponseMessage | EventMessage

/**
 * 메시지 생성 헬퍼 함수들
 */
export class NamedPipeProtocol {
  
  /**
   * 고유 ID 생성
   */
  static generateId(): string {
    const timestamp = Date.now()
    const random = Math.random().toString(36).substring(2, 10)
    return `msg_${timestamp}_${random}`
  }

  /**
   * 요청 메시지 생성
   */
  static createRequest(command: string, payload?: any): RequestMessage {
    return {
      id: this.generateId(),
      type: MessageType.Request,
      timestamp: Date.now(),
      command,
      payload
    }
  }

  /**
   * 응답 메시지 생성
   */
  static createResponse(requestId: string, success: boolean, data?: any, error?: string): ResponseMessage {
    return {
      id: this.generateId(),
      type: MessageType.Response,
      timestamp: Date.now(),
      requestId,
      success,
      data,
      error
    }
  }

  /**
   * 이벤트 메시지 생성
   */
  static createEvent(event: string, data?: any): EventMessage {
    return {
      id: this.generateId(),
      type: MessageType.Event,
      timestamp: Date.now(),
      event,
      data
    }
  }

  /**
   * 메시지 타입 확인 헬퍼
   */
  static isRequest(message: any): message is RequestMessage {
    return message.type === MessageType.Request && typeof message.command === 'string'
  }

  static isResponse(message: any): message is ResponseMessage {
    return message.type === MessageType.Response && typeof message.requestId === 'string'
  }

  static isEvent(message: any): message is EventMessage {
    return message.type === MessageType.Event && typeof message.event === 'string'
  }
}

/**
 * 지원되는 명령 목록
 */
export const Commands = {
  Ping: 'ping',
  StartGameDetect: 'start-game-detect',
  StopGameDetect: 'stop-game-detect',
  GetGameStatus: 'get-game-status',
  GetUnitCounts: 'get-unit-counts',
  GetPlayerInfo: 'get-player-info',
  
  // 프리셋 관련 명령
  PresetInit: 'preset-init',
  PresetUpdate: 'preset-update'
} as const

/**
 * 지원되는 이벤트 목록
 */
export const Events = {
  GameDetected: 'game-detected',
  GameEnded: 'game-ended',
  GameStatusChanged: 'game-status-changed',
} as const