import { ipcMain } from 'electron'
import { IIPCService } from './interfaces'
import { IIPCChannels } from '../types'

export class IPCService implements IIPCService {
  private handlers = new Map<string, Function>()
  
  registerHandler<K extends keyof IIPCChannels>(
    channel: K,
    handler: (data: IIPCChannels[K]['request']) => Promise<IIPCChannels[K]['response']>
  ): void {
    // 이미 등록된 핸들러가 있으면 먼저 제거
    if (this.handlers.has(channel)) {
      console.log(`⚠️ 기존 핸들러 제거: ${channel}`)
      this.removeHandler(channel)
    }
    
    // Electron의 ipcMain에서도 혹시 모를 중복 제거
    try {
      ipcMain.removeHandler(channel)
    } catch (error) {
      // 핸들러가 없으면 무시
    }
    
    this.handlers.set(channel, handler)
    
    ipcMain.handle(channel, async (event, data) => {
      try {
        return await handler(data)
      } catch (error) {
        const errorMessage = error instanceof Error ? error.message : String(error)
        console.error(`IPC Handler Error [${channel}]:`, errorMessage)
        return { success: false, error: errorMessage }
      }
    })
    
    console.log(`📡 IPC 핸들러 등록: ${channel}`)
  }
  
  sendToRenderer<K extends keyof IIPCChannels>(
    webContents: Electron.WebContents,
    channel: K,
    data: IIPCChannels[K]['response']
  ): void {
    webContents.send(channel, data)
  }
  
  broadcastToAllRenderers<K extends keyof IIPCChannels>(
    channel: K,
    data: IIPCChannels[K]['response']
  ): void {
    // 모든 열린 윈도우에 브로드캐스트
    const { webContents } = require('electron')
    webContents.getAllWebContents().forEach((wc: Electron.WebContents) => {
      wc.send(channel, data)
    })
  }
  
  removeHandler(channel: string): void {
    this.handlers.delete(channel)
    ipcMain.removeHandler(channel)
    console.log(`📡 IPC 핸들러 제거: ${channel}`)
  }
  
  removeAllHandlers(): void {
    // 등록된 모든 핸들러를 개별적으로 제거
    for (const channel of this.handlers.keys()) {
      ipcMain.removeHandler(channel)
    }
    this.handlers.clear()
    console.log('📡 모든 IPC 핸들러 제거')
  }
}