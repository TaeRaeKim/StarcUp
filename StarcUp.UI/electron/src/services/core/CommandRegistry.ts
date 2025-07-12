import { ICommandRegistry } from './interfaces'
import { ICommandDefinition } from '../types'

export class CommandRegistry implements ICommandRegistry {
  private commands = new Map<string, ICommandDefinition>()
  
  register<TReq, TRes>(definition: ICommandDefinition<TReq, TRes>): void {
    this.commands.set(definition.name, definition)
    console.log(`📋 명령어 등록: ${definition.name}`)
  }
  
  async execute<TReq, TRes>(commandName: string, request: TReq): Promise<TRes> {
    const command = this.commands.get(commandName)
    if (!command) {
      throw new Error(`Command ${commandName} not found`)
    }
    
    // 요청 유효성 검사
    if (command.requestValidator && !command.requestValidator(request)) {
      throw new Error(`Invalid request for command ${commandName}`)
    }
    
    try {
      const response = await command.handler(request)
      
      // 응답 유효성 검사
      if (command.responseValidator && !command.responseValidator(response)) {
        throw new Error(`Invalid response for command ${commandName}`)
      }
      
      return response
    } catch (error) {
      const errorMessage = error instanceof Error ? error.message : String(error)
      throw new Error(`Command ${commandName} failed: ${errorMessage}`)
    }
  }
  
  getRegisteredCommands(): string[] {
    return Array.from(this.commands.keys())
  }
}