import { IIPCChannels } from '../types'

export interface IIPCService {
  registerHandler<K extends keyof IIPCChannels>(
    channel: K,
    handler: (data: IIPCChannels[K]['request']) => Promise<IIPCChannels[K]['response']>
  ): void
  
  sendToRenderer<K extends keyof IIPCChannels>(
    webContents: Electron.WebContents,
    channel: K,
    data: IIPCChannels[K]['response']
  ): void
  
  broadcastToAllRenderers<K extends keyof IIPCChannels>(
    channel: K,
    data: IIPCChannels[K]['response']
  ): void
  
  removeHandler(channel: string): void
  removeAllHandlers(): void
}