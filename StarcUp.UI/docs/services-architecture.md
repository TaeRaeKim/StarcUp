# StarcUp.UI Electron 메인 프로세스 서비스 아키텍처

## 📋 개요

StarcUp.UI의 메인 프로세스에서 필요한 서비스들을 응집도 높게 설계한 아키텍처 문서입니다.

### 🎯 설계 목표
- **높은 응집도**: 관련 기능들을 논리적으로 그룹화
- **낮은 결합도**: 인터페이스를 통한 의존성 주입
- **확장성**: 새로운 기능을 쉽게 추가 가능
- **타입 안전성**: TypeScript를 활용한 강타입 시스템

## 🏗️ 서비스 아키텍처

### 📁 디렉토리 구조
```
StarcUp.UI/electron/src/
├── services/
│   ├── core-communication/
│   │   ├── CoreCommunicationService.ts     # Core 통신 총괄 서비스
│   │   ├── NamedPipeService.ts              # Named Pipes 통신 전용
│   │   └── CommandRegistry.ts               # 명령어 레지스트리
│   ├── ipc/
│   │   ├── IPCService.ts                    # Renderer 통신 총괄
│   │   └── ChannelHandlers.ts               # 채널별 핸들러
│   ├── auth/
│   │   ├── AuthService.ts                   # 인증 서비스
│   │   └── SessionManager.ts                # 세션 관리
│   ├── storage/
│   │   ├── DataStorageService.ts            # 데이터 저장 총괄
│   │   ├── UserDataRepository.ts            # 사용자 데이터
│   │   └── PresetRepository.ts              # 프리셋 데이터
│   ├── window/
│   │   ├── WindowManager.ts                 # 윈도우 관리 서비스
│   │   └── WindowConfiguration.ts           # 윈도우 설정
│   └── ServiceContainer.ts                  # 서비스 컨테이너 (DI)
```

## 🔧 핵심 서비스 설계

### 1. CoreCommunicationService (Core 통신 총괄)

**목적**: StarcUp.Core와의 모든 통신을 추상화하고 관리

```typescript
interface ICoreCommand {
  type: string
  payload: any
}

interface ICoreResponse {
  success: boolean
  data?: any
  error?: string
}

class CoreCommunicationService {
  private namedPipeService: NamedPipeService
  private commandRegistry: CommandRegistry
  
  constructor(namedPipeService: NamedPipeService) {
    this.namedPipeService = namedPipeService
    this.commandRegistry = new CommandRegistry()
    this.setupDefaultCommands()
  }
  
  // 게임 감지 관련
  async startGameDetection(): Promise<ICoreResponse> {
    return await this.sendCommand({ type: 'game:detect:start', payload: {} })
  }
  
  async stopGameDetection(): Promise<ICoreResponse> {
    return await this.sendCommand({ type: 'game:detect:stop', payload: {} })
  }
  
  async getGameStatus(): Promise<ICoreResponse> {
    return await this.sendCommand({ type: 'game:status', payload: {} })
  }
  
  // 인게임 데이터 관련
  async getUnitCounts(playerId?: number): Promise<ICoreResponse> {
    return await this.sendCommand({ 
      type: 'game:units:count', 
      payload: { playerId } 
    })
  }
  
  async getPlayerInfo(): Promise<ICoreResponse> {
    return await this.sendCommand({ type: 'game:player:info', payload: {} })
  }
  
  // 확장 가능한 명령 시스템
  async sendCommand<T>(command: ICoreCommand): Promise<ICoreResponse<T>> {
    return await this.commandRegistry.execute(command.type, command.payload)
  }
  
  get isConnected(): boolean {
    return this.namedPipeService.connected
  }
  
  private setupDefaultCommands(): void {
    this.commandRegistry.register({
      name: 'game:detect:start',
      handler: async () => await this.namedPipeService.sendCommand('start-game-detect')
    })
    
    this.commandRegistry.register({
      name: 'game:detect:stop',
      handler: async () => await this.namedPipeService.sendCommand('stop-game-detect')
    })
    
    this.commandRegistry.register({
      name: 'game:status',
      handler: async () => await this.namedPipeService.sendCommand('get-game-status')
    })
    
    this.commandRegistry.register({
      name: 'game:units:count',
      requestValidator: (data): data is { playerId?: number } => 
        data.playerId === undefined || typeof data.playerId === 'number',
      handler: async (req) => {
        const args = req.playerId ? [req.playerId.toString()] : []
        return await this.namedPipeService.sendCommand('get-unit-counts', args)
      }
    })
  }
}
```

### 2. CommandRegistry (확장 가능한 명령어 시스템)

**목적**: Core 명령어를 체계적으로 관리하고 확장성 제공

```typescript
interface ICommandDefinition<TRequest = any, TResponse = any> {
  name: string
  requestValidator?: (data: any) => data is TRequest
  responseValidator?: (data: any) => data is TResponse
  handler: (request: TRequest) => Promise<TResponse>
}

class CommandRegistry {
  private commands = new Map<string, ICommandDefinition>()
  
  register<TReq, TRes>(definition: ICommandDefinition<TReq, TRes>): void {
    this.commands.set(definition.name, definition)
  }
  
  async execute<TReq, TRes>(
    commandName: string, 
    request: TReq
  ): Promise<TRes> {
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
      throw new Error(`Command ${commandName} failed: ${error.message}`)
    }
  }
  
  getRegisteredCommands(): string[] {
    return Array.from(this.commands.keys())
  }
}

// 사용 예시
const commandRegistry = new CommandRegistry()

// 게임 감지 명령 등록
commandRegistry.register({
  name: 'game:detect:start',
  handler: async () => await coreService.startGameDetection()
})

// 유닛 개수 조회 명령 등록 (타입 안전성 포함)
commandRegistry.register({
  name: 'game:units:count',
  requestValidator: (data): data is { playerId: number } => 
    typeof data.playerId === 'number',
  responseValidator: (data): data is { success: boolean; units: any[] } =>
    typeof data.success === 'boolean' && Array.isArray(data.units),
  handler: async (req) => await coreService.getUnitCounts(req.playerId)
})
```

### 3. IPCService (Renderer 통신 총괄)

**목적**: Renderer 프로세스와의 모든 IPC 통신을 중앙 집중화

```typescript
interface IIPCChannel {
  request: any
  response: any
}

interface IIPCChannels {
  // Core 관련
  'core:status': { request: void; response: { connected: boolean } }
  'core:start-detection': { request: void; response: ICoreResponse }
  'core:stop-detection': { request: void; response: ICoreResponse }
  'core:get-game-status': { request: void; response: ICoreResponse }
  'core:get-unit-counts': { request: { playerId?: number }; response: ICoreResponse }
  
  // 인증 관련
  'auth:login': { request: { username: string; password: string }; response: { success: boolean; token?: string; user?: IUser } }
  'auth:logout': { request: void; response: { success: boolean } }
  'auth:get-session': { request: void; response: { user?: IUser } }
  
  // 데이터 관련
  'data:save-preset': { request: { userId: string; preset: IPreset }; response: { success: boolean; id?: string } }
  'data:load-preset': { request: { userId: string; presetId: string }; response: { success: boolean; data?: any } }
  'data:get-presets': { request: { userId: string }; response: { presets: IPreset[] } }
  'data:delete-preset': { request: { userId: string; presetId: string }; response: { success: boolean } }
  
  // 윈도우 관리
  'window:minimize': { request: void; response: void }
  'window:maximize': { request: void; response: void }
  'window:close': { request: void; response: void }
  'window:toggle-overlay': { request: void; response: void }
  'window:show-overlay': { request: void; response: void }
  'window:hide-overlay': { request: void; response: void }
  'window:get-window-bounds': { request: void; response: { main: Electron.Rectangle | null; overlay: Electron.Rectangle | null } }
  'window:set-overlay-position': { request: { x: number; y: number }; response: void }
  'window:set-overlay-size': { request: { width: number; height: number }; response: void }
  'window:set-overlay-opacity': { request: { opacity: number }; response: void }
  'window:toggle-dev-tools': { request: void; response: void }
}

class IPCService {
  private handlers = new Map<string, Function>()
  
  registerHandler<K extends keyof IIPCChannels>(
    channel: K,
    handler: (data: IIPCChannels[K]['request']) => Promise<IIPCChannels[K]['response']>
  ): void {
    this.handlers.set(channel, handler)
    
    ipcMain.handle(channel, async (event, data) => {
      try {
        return await handler(data)
      } catch (error) {
        console.error(`IPC Handler Error [${channel}]:`, error)
        return { success: false, error: error.message }
      }
    })
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
    webContents.getAllWebContents().forEach(wc => {
      wc.send(channel, data)
    })
  }
  
  removeHandler(channel: string): void {
    this.handlers.delete(channel)
    ipcMain.removeHandler(channel)
  }
  
  removeAllHandlers(): void {
    this.handlers.clear()
    ipcMain.removeAllListeners()
  }
}
```

### 4. AuthService (인증 서비스)

**목적**: 사용자 인증 및 세션 관리

```typescript
interface IUser {
  id: string
  username: string
  email: string
  settings: IUserSettings
  createdAt: Date
  lastLoginAt: Date
}

interface IUserSettings {
  theme: 'light' | 'dark'
  language: 'ko' | 'en'
  autoStart: boolean
  overlaySettings: IOverlaySettings
  hotkeySettings: IHotkeySettings
}

interface IOverlaySettings {
  enabled: boolean
  position: { x: number; y: number }
  size: { width: number; height: number }
  opacity: number
}

interface IHotkeySettings {
  toggleOverlay: string
  startDetection: string
  stopDetection: string
}

class AuthService {
  private currentUser: IUser | null = null
  private sessionToken: string | null = null
  private dataStorageService: DataStorageService
  
  constructor(dataStorageService: DataStorageService) {
    this.dataStorageService = dataStorageService
  }
  
  async login(username: string, password: string): Promise<{ success: boolean; token?: string; user?: IUser }> {
    try {
      const user = await this.validateCredentials(username, password)
      if (user) {
        this.currentUser = user
        this.sessionToken = this.generateToken(user)
        
        // 로그인 시간 업데이트
        user.lastLoginAt = new Date()
        await this.dataStorageService.saveUserData(user.id, { lastLoginAt: user.lastLoginAt })
        
        await this.saveSession()
        
        return { success: true, token: this.sessionToken, user }
      }
      
      return { success: false }
    } catch (error) {
      console.error('Login failed:', error)
      return { success: false }
    }
  }
  
  async logout(): Promise<{ success: boolean }> {
    try {
      this.currentUser = null
      this.sessionToken = null
      await this.clearSession()
      return { success: true }
    } catch (error) {
      console.error('Logout failed:', error)
      return { success: false }
    }
  }
  
  async getCurrentUser(): Promise<IUser | null> {
    if (!this.currentUser) {
      await this.loadSession()
    }
    return this.currentUser
  }
  
  async updateUserSettings(userId: string, settings: Partial<IUserSettings>): Promise<{ success: boolean }> {
    try {
      const user = await this.getCurrentUser()
      if (!user || user.id !== userId) {
        return { success: false }
      }
      
      user.settings = { ...user.settings, ...settings }
      await this.dataStorageService.saveUserData(userId, { settings: user.settings })
      
      return { success: true }
    } catch (error) {
      console.error('Update settings failed:', error)
      return { success: false }
    }
  }
  
  private async validateCredentials(username: string, password: string): Promise<IUser | null> {
    // 실제 인증 로직 구현
    // 예: API 호출, 데이터베이스 조회, OAuth 등
    
    // 임시 구현 (실제로는 암호화된 패스워드 검증)
    if (username === 'admin' && password === 'password') {
      return {
        id: 'user-1',
        username: 'admin',
        email: 'admin@starcup.com',
        settings: {
          theme: 'dark',
          language: 'ko',
          autoStart: true,
          overlaySettings: {
            enabled: true,
            position: { x: 100, y: 100 },
            size: { width: 400, height: 300 },
            opacity: 0.8
          },
          hotkeySettings: {
            toggleOverlay: 'Ctrl+Alt+O',
            startDetection: 'Ctrl+Alt+S',
            stopDetection: 'Ctrl+Alt+D'
          }
        },
        createdAt: new Date(),
        lastLoginAt: new Date()
      }
    }
    
    return null
  }
  
  private generateToken(user: IUser): string {
    // JWT 토큰 생성 또는 세션 ID 생성
    const payload = {
      userId: user.id,
      username: user.username,
      timestamp: Date.now()
    }
    
    // 실제로는 JWT 라이브러리 사용
    return Buffer.from(JSON.stringify(payload)).toString('base64')
  }
  
  private async saveSession(): Promise<void> {
    if (!this.currentUser || !this.sessionToken) return
    
    const sessionData = {
      user: this.currentUser,
      token: this.sessionToken,
      expiresAt: Date.now() + (24 * 60 * 60 * 1000) // 24시간
    }
    
    // 암호화하여 저장
    const { app } = require('electron')
    const fs = require('fs').promises
    const path = require('path')
    
    const sessionPath = path.join(app.getPath('userData'), 'session.json')
    await fs.writeFile(sessionPath, JSON.stringify(sessionData))
  }
  
  private async loadSession(): Promise<void> {
    try {
      const { app } = require('electron')
      const fs = require('fs').promises
      const path = require('path')
      
      const sessionPath = path.join(app.getPath('userData'), 'session.json')
      const sessionData = JSON.parse(await fs.readFile(sessionPath, 'utf-8'))
      
      // 만료 시간 확인
      if (Date.now() > sessionData.expiresAt) {
        await this.clearSession()
        return
      }
      
      this.currentUser = sessionData.user
      this.sessionToken = sessionData.token
    } catch (error) {
      // 세션 파일이 없거나 손상된 경우
      await this.clearSession()
    }
  }
  
  private async clearSession(): Promise<void> {
    try {
      const { app } = require('electron')
      const fs = require('fs').promises
      const path = require('path')
      
      const sessionPath = path.join(app.getPath('userData'), 'session.json')
      await fs.unlink(sessionPath)
    } catch (error) {
      // 파일이 없는 경우 무시
    }
  }
}
```

### 5. DataStorageService (데이터 저장 서비스)

**목적**: 모든 데이터 저장을 중앙 집중화

```typescript
interface IPreset {
  id: string
  name: string
  type: 'overlay' | 'hotkey' | 'ui' | 'game'
  data: any
  createdAt: Date
  updatedAt: Date
}

interface IUserData {
  userId: string
  settings: IUserSettings
  presets: IPreset[]
  gameHistory: IGameHistory[]
  lastModified: Date
}

interface IGameHistory {
  id: string
  gameMode: string
  race: string
  opponent: string
  result: 'win' | 'lose' | 'draw'
  duration: number
  playedAt: Date
  unitStats: any
}

class DataStorageService {
  private userDataRepository: UserDataRepository
  private presetRepository: PresetRepository
  private gameHistoryRepository: GameHistoryRepository
  private configPath: string
  
  constructor() {
    const { app } = require('electron')
    this.configPath = path.join(app.getPath('userData'), 'StarcUp')
    
    this.userDataRepository = new UserDataRepository(this.configPath)
    this.presetRepository = new PresetRepository(this.configPath)
    this.gameHistoryRepository = new GameHistoryRepository(this.configPath)
    
    this.ensureDirectories()
  }
  
  // 사용자 데이터 관리
  async saveUserData(userId: string, data: Partial<IUserData>): Promise<{ success: boolean }> {
    try {
      await this.userDataRepository.save(userId, data)
      return { success: true }
    } catch (error) {
      console.error('Save user data failed:', error)
      return { success: false }
    }
  }
  
  async loadUserData(userId: string): Promise<IUserData | null> {
    try {
      return await this.userDataRepository.load(userId)
    } catch (error) {
      console.error('Load user data failed:', error)
      return null
    }
  }
  
  // 프리셋 관리
  async savePreset(userId: string, preset: Omit<IPreset, 'id' | 'createdAt' | 'updatedAt'>): Promise<{ success: boolean; id?: string }> {
    try {
      const id = await this.presetRepository.save(userId, preset)
      return { success: true, id }
    } catch (error) {
      console.error('Save preset failed:', error)
      return { success: false }
    }
  }
  
  async loadPresets(userId: string): Promise<IPreset[]> {
    try {
      return await this.presetRepository.loadAll(userId)
    } catch (error) {
      console.error('Load presets failed:', error)
      return []
    }
  }
  
  async loadPreset(userId: string, presetId: string): Promise<IPreset | null> {
    try {
      return await this.presetRepository.load(userId, presetId)
    } catch (error) {
      console.error('Load preset failed:', error)
      return null
    }
  }
  
  async deletePreset(userId: string, presetId: string): Promise<{ success: boolean }> {
    try {
      await this.presetRepository.delete(userId, presetId)
      return { success: true }
    } catch (error) {
      console.error('Delete preset failed:', error)
      return { success: false }
    }
  }
  
  // 게임 히스토리 관리
  async saveGameHistory(userId: string, gameData: Omit<IGameHistory, 'id' | 'playedAt'>): Promise<{ success: boolean; id?: string }> {
    try {
      const id = await this.gameHistoryRepository.save(userId, gameData)
      return { success: true, id }
    } catch (error) {
      console.error('Save game history failed:', error)
      return { success: false }
    }
  }
  
  async loadGameHistory(userId: string, limit?: number): Promise<IGameHistory[]> {
    try {
      return await this.gameHistoryRepository.loadAll(userId, limit)
    } catch (error) {
      console.error('Load game history failed:', error)
      return []
    }
  }
  
  // 데이터 백업 및 복원
  async exportUserData(userId: string): Promise<{ success: boolean; data?: any }> {
    try {
      const userData = await this.loadUserData(userId)
      const presets = await this.loadPresets(userId)
      const gameHistory = await this.loadGameHistory(userId)
      
      const exportData = {
        userData,
        presets,
        gameHistory,
        exportedAt: new Date()
      }
      
      return { success: true, data: exportData }
    } catch (error) {
      console.error('Export user data failed:', error)
      return { success: false }
    }
  }
  
  async importUserData(userId: string, importData: any): Promise<{ success: boolean }> {
    try {
      // 기존 데이터 백업
      const backupData = await this.exportUserData(userId)
      
      // 새 데이터 임포트
      if (importData.userData) {
        await this.saveUserData(userId, importData.userData)
      }
      
      if (importData.presets) {
        for (const preset of importData.presets) {
          await this.savePreset(userId, preset)
        }
      }
      
      if (importData.gameHistory) {
        for (const game of importData.gameHistory) {
          await this.saveGameHistory(userId, game)
        }
      }
      
      return { success: true }
    } catch (error) {
      console.error('Import user data failed:', error)
      return { success: false }
    }
  }
  
  private async ensureDirectories(): Promise<void> {
    const fs = require('fs').promises
    const directories = [
      this.configPath,
      path.join(this.configPath, 'users'),
      path.join(this.configPath, 'presets'),
      path.join(this.configPath, 'history')
    ]
    
    for (const dir of directories) {
      try {
        await fs.mkdir(dir, { recursive: true })
      } catch (error) {
        // 디렉토리가 이미 존재하는 경우 무시
      }
    }
  }
}

// Repository 클래스들
class UserDataRepository {
  constructor(private basePath: string) {}
  
  async save(userId: string, data: Partial<IUserData>): Promise<void> {
    const fs = require('fs').promises
    const filePath = path.join(this.basePath, 'users', `${userId}.json`)
    
    const existing = await this.load(userId) || { 
      userId, 
      settings: {}, 
      presets: [], 
      gameHistory: [],
      lastModified: new Date()
    }
    
    const merged = { 
      ...existing, 
      ...data, 
      lastModified: new Date() 
    }
    
    await fs.writeFile(filePath, JSON.stringify(merged, null, 2))
  }
  
  async load(userId: string): Promise<IUserData | null> {
    const fs = require('fs').promises
    const filePath = path.join(this.basePath, 'users', `${userId}.json`)
    
    try {
      const data = await fs.readFile(filePath, 'utf-8')
      return JSON.parse(data)
    } catch {
      return null
    }
  }
}

class PresetRepository {
  constructor(private basePath: string) {}
  
  async save(userId: string, preset: Omit<IPreset, 'id' | 'createdAt' | 'updatedAt'>): Promise<string> {
    const fs = require('fs').promises
    const id = this.generateId()
    const now = new Date()
    
    const fullPreset: IPreset = {
      id,
      ...preset,
      createdAt: now,
      updatedAt: now
    }
    
    const userDir = path.join(this.basePath, 'presets', userId)
    await fs.mkdir(userDir, { recursive: true })
    
    const filePath = path.join(userDir, `${id}.json`)
    await fs.writeFile(filePath, JSON.stringify(fullPreset, null, 2))
    
    return id
  }
  
  async load(userId: string, presetId: string): Promise<IPreset | null> {
    const fs = require('fs').promises
    const filePath = path.join(this.basePath, 'presets', userId, `${presetId}.json`)
    
    try {
      const data = await fs.readFile(filePath, 'utf-8')
      return JSON.parse(data)
    } catch {
      return null
    }
  }
  
  async loadAll(userId: string): Promise<IPreset[]> {
    const fs = require('fs').promises
    const userDir = path.join(this.basePath, 'presets', userId)
    
    try {
      const files = await fs.readdir(userDir)
      const presets: IPreset[] = []
      
      for (const file of files) {
        if (file.endsWith('.json')) {
          const filePath = path.join(userDir, file)
          const data = await fs.readFile(filePath, 'utf-8')
          presets.push(JSON.parse(data))
        }
      }
      
      return presets.sort((a, b) => b.updatedAt.getTime() - a.updatedAt.getTime())
    } catch {
      return []
    }
  }
  
  async delete(userId: string, presetId: string): Promise<void> {
    const fs = require('fs').promises
    const filePath = path.join(this.basePath, 'presets', userId, `${presetId}.json`)
    
    await fs.unlink(filePath)
  }
  
  private generateId(): string {
    return `preset-${Date.now()}-${Math.random().toString(36).substr(2, 9)}`
  }
}

class GameHistoryRepository {
  constructor(private basePath: string) {}
  
  async save(userId: string, gameData: Omit<IGameHistory, 'id' | 'playedAt'>): Promise<string> {
    const fs = require('fs').promises
    const id = this.generateId()
    const now = new Date()
    
    const fullGameData: IGameHistory = {
      id,
      ...gameData,
      playedAt: now
    }
    
    const userDir = path.join(this.basePath, 'history', userId)
    await fs.mkdir(userDir, { recursive: true })
    
    const filePath = path.join(userDir, `${id}.json`)
    await fs.writeFile(filePath, JSON.stringify(fullGameData, null, 2))
    
    return id
  }
  
  async loadAll(userId: string, limit?: number): Promise<IGameHistory[]> {
    const fs = require('fs').promises
    const userDir = path.join(this.basePath, 'history', userId)
    
    try {
      const files = await fs.readdir(userDir)
      const games: IGameHistory[] = []
      
      for (const file of files) {
        if (file.endsWith('.json')) {
          const filePath = path.join(userDir, file)
          const data = await fs.readFile(filePath, 'utf-8')
          games.push(JSON.parse(data))
        }
      }
      
      const sorted = games.sort((a, b) => b.playedAt.getTime() - a.playedAt.getTime())
      return limit ? sorted.slice(0, limit) : sorted
    } catch {
      return []
    }
  }
  
  private generateId(): string {
    return `game-${Date.now()}-${Math.random().toString(36).substr(2, 9)}`
  }
}
```

### 6. WindowManager (윈도우 관리 서비스)

**목적**: 모든 Electron 윈도우를 체계적으로 관리

```typescript
interface IWindowConfiguration {
  main: Electron.BrowserWindowConstructorOptions
  overlay: Electron.BrowserWindowConstructorOptions
}

interface IWindowManager {
  // 윈도우 생성
  createMainWindow(): void
  createOverlayWindow(): void
  
  // 윈도우 접근자
  getMainWindow(): BrowserWindow | null
  getOverlayWindow(): BrowserWindow | null
  
  // 메인 윈도우 제어
  minimizeMain(): void
  maximizeMain(): void
  closeMain(): void
  
  // 오버레이 제어
  toggleOverlay(): void
  showOverlay(): void
  hideOverlay(): void
  
  // 개발 도구
  toggleDevTools(): void
  
  // 정리
  cleanup(): void
}

class WindowManager implements IWindowManager {
  private mainWindow: BrowserWindow | null = null
  private overlayWindow: BrowserWindow | null = null
  private windowConfig: IWindowConfiguration
  
  constructor(windowConfig: IWindowConfiguration) {
    this.windowConfig = windowConfig
  }
  
  createMainWindow(): void {
    if (this.mainWindow) {
      console.warn('Main window already exists')
      return
    }
    
    this.mainWindow = new BrowserWindow({
      ...this.windowConfig.main,
      icon: path.join(process.env.VITE_PUBLIC!, 'electron-vite.svg'),
      webPreferences: {
        preload: path.join(MAIN_DIST, 'preload.mjs'),
        nodeIntegration: false,
        contextIsolation: true,
      },
    })
    
    // 윈도우 이벤트 처리
    this.setupMainWindowEvents()
    
    // 페이지 로드
    this.loadMainPage()
    
    console.log('✅ Main window created')
  }
  
  createOverlayWindow(): void {
    if (this.overlayWindow) {
      console.warn('Overlay window already exists')
      return
    }
    
    this.overlayWindow = new BrowserWindow({
      ...this.windowConfig.overlay,
      webPreferences: {
        preload: path.join(MAIN_DIST, 'preload.mjs'),
        nodeIntegration: false,
        contextIsolation: true,
      },
    })
    
    // 오버레이 특성 설정
    this.overlayWindow.setIgnoreMouseEvents(true)
    this.overlayWindow.center()
    
    // 오버레이 이벤트 처리
    this.setupOverlayWindowEvents()
    
    // 페이지 로드
    this.loadOverlayPage()
    
    // 기본적으로 숨김
    if (OVERLAY_CONFIG.defaultHidden) {
      this.overlayWindow.hide()
    }
    
    console.log('✅ Overlay window created')
  }
  
  getMainWindow(): BrowserWindow | null {
    return this.mainWindow
  }
  
  getOverlayWindow(): BrowserWindow | null {
    return this.overlayWindow
  }
  
  // 메인 윈도우 제어
  minimizeMain(): void {
    if (this.mainWindow) {
      this.mainWindow.minimize()
    }
  }
  
  maximizeMain(): void {
    if (this.mainWindow) {
      if (this.mainWindow.isMaximized()) {
        this.mainWindow.unmaximize()
      } else {
        this.mainWindow.maximize()
      }
    }
  }
  
  closeMain(): void {
    if (this.mainWindow) {
      this.mainWindow.close()
    }
  }
  
  focusMain(): void {
    if (this.mainWindow) {
      this.mainWindow.focus()
    }
  }
  
  centerMain(): void {
    if (this.mainWindow) {
      this.mainWindow.center()
    }
  }
  
  // 오버레이 제어
  toggleOverlay(): void {
    if (this.overlayWindow) {
      if (this.overlayWindow.isVisible()) {
        this.overlayWindow.hide()
      } else {
        this.overlayWindow.show()
      }
    }
  }
  
  showOverlay(): void {
    if (this.overlayWindow) {
      this.overlayWindow.show()
    }
  }
  
  hideOverlay(): void {
    if (this.overlayWindow) {
      this.overlayWindow.hide()
    }
  }
  
  setOverlayPosition(x: number, y: number): void {
    if (this.overlayWindow) {
      this.overlayWindow.setPosition(x, y)
    }
  }
  
  setOverlaySize(width: number, height: number): void {
    if (this.overlayWindow) {
      this.overlayWindow.setSize(width, height)
    }
  }
  
  setOverlayOpacity(opacity: number): void {
    if (this.overlayWindow) {
      this.overlayWindow.setOpacity(Math.max(0, Math.min(1, opacity)))
    }
  }
  
  setOverlayMouseEvents(ignore: boolean): void {
    if (this.overlayWindow) {
      this.overlayWindow.setIgnoreMouseEvents(ignore)
    }
  }
  
  // 개발 도구
  toggleDevTools(): void {
    if (this.mainWindow) {
      this.mainWindow.webContents.toggleDevTools()
    }
  }
  
  toggleOverlayDevTools(): void {
    if (this.overlayWindow) {
      this.overlayWindow.webContents.toggleDevTools()
    }
  }
  
  // 윈도우 상태 확인
  isMainWindowVisible(): boolean {
    return this.mainWindow ? this.mainWindow.isVisible() : false
  }
  
  isOverlayWindowVisible(): boolean {
    return this.overlayWindow ? this.overlayWindow.isVisible() : false
  }
  
  isMainWindowMaximized(): boolean {
    return this.mainWindow ? this.mainWindow.isMaximized() : false
  }
  
  isMainWindowMinimized(): boolean {
    return this.mainWindow ? this.mainWindow.isMinimized() : false
  }
  
  // 윈도우 정보 조회
  getMainWindowBounds(): Electron.Rectangle | null {
    return this.mainWindow ? this.mainWindow.getBounds() : null
  }
  
  getOverlayWindowBounds(): Electron.Rectangle | null {
    return this.overlayWindow ? this.overlayWindow.getBounds() : null
  }
  
  // 윈도우 통신
  sendToMainWindow(channel: string, data: any): void {
    if (this.mainWindow) {
      this.mainWindow.webContents.send(channel, data)
    }
  }
  
  sendToOverlayWindow(channel: string, data: any): void {
    if (this.overlayWindow) {
      this.overlayWindow.webContents.send(channel, data)
    }
  }
  
  broadcastToAllWindows(channel: string, data: any): void {
    this.sendToMainWindow(channel, data)
    this.sendToOverlayWindow(channel, data)
  }
  
  // 페이지 로드
  private loadMainPage(): void {
    if (!this.mainWindow) return
    
    if (VITE_DEV_SERVER_URL) {
      // 개발 환경
      this.mainWindow.loadURL(path.join(VITE_DEV_SERVER_URL, 'src', 'main-page', 'index.html'))
      
      if (DEV_TOOLS_CONFIG.autoOpenInDev) {
        this.mainWindow.webContents.openDevTools()
      }
    } else {
      // 프로덕션 환경
      this.mainWindow.loadFile(path.join(RENDERER_DIST, 'src', 'main-page', 'index.html'))
    }
  }
  
  private loadOverlayPage(): void {
    if (!this.overlayWindow) return
    
    if (VITE_DEV_SERVER_URL) {
      // 개발 환경
      this.overlayWindow.loadURL(path.join(VITE_DEV_SERVER_URL, 'src', 'overlay', 'index.html'))
    } else {
      // 프로덕션 환경
      this.overlayWindow.loadFile(path.join(RENDERER_DIST, 'src', 'overlay', 'index.html'))
    }
  }
  
  // 윈도우 이벤트 설정
  private setupMainWindowEvents(): void {
    if (!this.mainWindow) return
    
    this.mainWindow.on('closed', () => {
      this.mainWindow = null
      console.log('Main window closed')
    })
    
    this.mainWindow.on('ready-to-show', () => {
      console.log('Main window ready to show')
    })
    
    this.mainWindow.webContents.on('did-finish-load', () => {
      this.mainWindow?.webContents.send('main-process-message', new Date().toLocaleString())
    })
    
    this.mainWindow.on('minimize', () => {
      console.log('Main window minimized')
    })
    
    this.mainWindow.on('maximize', () => {
      console.log('Main window maximized')
    })
    
    this.mainWindow.on('unmaximize', () => {
      console.log('Main window unmaximized')
    })
  }
  
  private setupOverlayWindowEvents(): void {
    if (!this.overlayWindow) return
    
    this.overlayWindow.on('closed', () => {
      this.overlayWindow = null
      console.log('Overlay window closed')
    })
    
    this.overlayWindow.on('show', () => {
      console.log('Overlay window shown')
    })
    
    this.overlayWindow.on('hide', () => {
      console.log('Overlay window hidden')
    })
    
    this.overlayWindow.on('blur', () => {
      // 오버레이가 포커스를 잃으면 다시 최상위로
      if (this.overlayWindow) {
        this.overlayWindow.setAlwaysOnTop(true)
      }
    })
  }
  
  // 윈도우 설정 업데이트
  updateWindowConfig(config: Partial<IWindowConfiguration>): void {
    this.windowConfig = { ...this.windowConfig, ...config }
  }
  
  applyMainWindowConfig(config: Partial<Electron.BrowserWindowConstructorOptions>): void {
    if (this.mainWindow) {
      // 런타임에 변경 가능한 설정들 적용
      if (config.width !== undefined && config.height !== undefined) {
        this.mainWindow.setSize(config.width, config.height)
      }
      
      if (config.minWidth !== undefined && config.minHeight !== undefined) {
        this.mainWindow.setMinimumSize(config.minWidth, config.minHeight)
      }
      
      if (config.maxWidth !== undefined && config.maxHeight !== undefined) {
        this.mainWindow.setMaximumSize(config.maxWidth, config.maxHeight)
      }
      
      if (config.resizable !== undefined) {
        this.mainWindow.setResizable(config.resizable)
      }
      
      if (config.alwaysOnTop !== undefined) {
        this.mainWindow.setAlwaysOnTop(config.alwaysOnTop)
      }
    }
  }
  
  applyOverlayWindowConfig(config: Partial<Electron.BrowserWindowConstructorOptions>): void {
    if (this.overlayWindow) {
      // 런타임에 변경 가능한 설정들 적용
      if (config.width !== undefined && config.height !== undefined) {
        this.overlayWindow.setSize(config.width, config.height)
      }
      
      if (config.alwaysOnTop !== undefined) {
        this.overlayWindow.setAlwaysOnTop(config.alwaysOnTop)
      }
      
      if (config.skipTaskbar !== undefined) {
        this.overlayWindow.setSkipTaskbar(config.skipTaskbar)
      }
    }
  }
  
  // 정리
  cleanup(): void {
    if (this.mainWindow) {
      this.mainWindow.close()
      this.mainWindow = null
    }
    
    if (this.overlayWindow) {
      this.overlayWindow.close()
      this.overlayWindow = null
    }
    
    console.log('✅ WindowManager cleanup completed')
  }
  
  // 에러 처리
  private handleWindowError(windowType: string, error: Error): void {
    console.error(`${windowType} window error:`, error)
    // 에러 발생 시 윈도우 재생성 또는 다른 복구 로직
  }
}
```

### 7. ServiceContainer (의존성 주입 컨테이너)

**목적**: 모든 서비스를 통합 관리하고 의존성 주입

```typescript
class ServiceContainer {
  private services = new Map<string, any>()
  private singletons = new Map<string, any>()
  private initialized = false
  
  // 서비스 등록
  register<T>(name: string, factory: () => T): void {
    this.services.set(name, factory)
  }
  
  registerSingleton<T>(name: string, factory: () => T): void {
    this.register(name, factory)
    this.singletons.set(name, null)
  }
  
  // 서비스 해결
  resolve<T>(name: string): T {
    if (this.singletons.has(name)) {
      let instance = this.singletons.get(name)
      if (!instance) {
        instance = this.services.get(name)()
        this.singletons.set(name, instance)
      }
      return instance
    }
    
    const factory = this.services.get(name)
    if (!factory) {
      throw new Error(`Service ${name} not found`)
    }
    
    return factory()
  }
  
  // 서비스 초기화
  initialize(): void {
    if (this.initialized) {
      throw new Error('ServiceContainer already initialized')
    }
    
    this.registerServices()
    this.setupServices()
    this.initialized = true
  }
  
  private registerServices(): void {
    // 기본 서비스 등록
    this.registerSingleton('namedPipeService', () => new NamedPipeService())
    this.registerSingleton('dataStorageService', () => new DataStorageService())
    this.registerSingleton('windowManager', () => new WindowManager(WINDOW_CONFIG))
    
    // 의존성이 있는 서비스 등록
    this.registerSingleton('coreCommunicationService', () => 
      new CoreCommunicationService(this.resolve('namedPipeService'))
    )
    
    this.registerSingleton('authService', () => 
      new AuthService(this.resolve('dataStorageService'))
    )
    
    this.registerSingleton('ipcService', () => new IPCService())
  }
  
  private setupServices(): void {
    // 서비스 초기화 및 설정
    const ipcService = this.resolve<IPCService>('ipcService')
    const coreService = this.resolve<CoreCommunicationService>('coreCommunicationService')
    const authService = this.resolve<AuthService>('authService')
    const dataService = this.resolve<DataStorageService>('dataStorageService')
    const windowManager = this.resolve<WindowManager>('windowManager')
    
    // IPC 핸들러 설정
    this.setupIPCHandlers(ipcService, coreService, authService, dataService, windowManager)
    
    console.log('✅ All services initialized successfully')
  }
  
  private setupIPCHandlers(
    ipcService: IPCService,
    coreService: CoreCommunicationService,
    authService: AuthService,
    dataService: DataStorageService,
    windowManager: WindowManager
  ): void {
    // Core 관련 핸들러
    ipcService.registerHandler('core:status', async () => ({
      connected: coreService.isConnected
    }))
    
    ipcService.registerHandler('core:start-detection', async () => 
      await coreService.startGameDetection()
    )
    
    ipcService.registerHandler('core:stop-detection', async () => 
      await coreService.stopGameDetection()
    )
    
    ipcService.registerHandler('core:get-game-status', async () => 
      await coreService.getGameStatus()
    )
    
    ipcService.registerHandler('core:get-unit-counts', async (data) => 
      await coreService.getUnitCounts(data.playerId)
    )
    
    // 인증 관련 핸들러
    ipcService.registerHandler('auth:login', async (data) => 
      await authService.login(data.username, data.password)
    )
    
    ipcService.registerHandler('auth:logout', async () => 
      await authService.logout()
    )
    
    ipcService.registerHandler('auth:get-session', async () => ({
      user: await authService.getCurrentUser()
    }))
    
    // 데이터 관련 핸들러
    ipcService.registerHandler('data:save-preset', async (data) => 
      await dataService.savePreset(data.userId, data.preset)
    )
    
    ipcService.registerHandler('data:load-preset', async (data) => ({
      success: true,
      data: await dataService.loadPreset(data.userId, data.presetId)
    }))
    
    ipcService.registerHandler('data:get-presets', async (data) => ({
      presets: await dataService.loadPresets(data.userId)
    }))
    
    ipcService.registerHandler('data:delete-preset', async (data) => 
      await dataService.deletePreset(data.userId, data.presetId)
    )
    
    // 윈도우 관리 핸들러
    ipcService.registerHandler('window:minimize', async () => {
      windowManager.minimizeMain()
    })
    
    ipcService.registerHandler('window:maximize', async () => {
      windowManager.maximizeMain()
    })
    
    ipcService.registerHandler('window:close', async () => {
      windowManager.closeMain()
    })
    
    ipcService.registerHandler('window:toggle-overlay', async () => {
      windowManager.toggleOverlay()
    })
    
    // 추가 윈도우 관리 핸들러
    ipcService.registerHandler('window:show-overlay', async () => {
      windowManager.showOverlay()
    })
    
    ipcService.registerHandler('window:hide-overlay', async () => {
      windowManager.hideOverlay()
    })
    
    ipcService.registerHandler('window:get-window-bounds', async () => ({
      main: windowManager.getMainWindowBounds(),
      overlay: windowManager.getOverlayWindowBounds()
    }))
    
    ipcService.registerHandler('window:set-overlay-position', async (data) => {
      windowManager.setOverlayPosition(data.x, data.y)
    })
    
    ipcService.registerHandler('window:set-overlay-size', async (data) => {
      windowManager.setOverlaySize(data.width, data.height)
    })
    
    ipcService.registerHandler('window:set-overlay-opacity', async (data) => {
      windowManager.setOverlayOpacity(data.opacity)
    })
    
    ipcService.registerHandler('window:toggle-dev-tools', async () => {
      windowManager.toggleDevTools()
    })
  }
  
  // 서비스 정리
  async dispose(): Promise<void> {
    if (!this.initialized) return
    
    // 싱글톤 서비스들 정리
    for (const [name, instance] of this.singletons) {
      if (instance && typeof instance.dispose === 'function') {
        await instance.dispose()
      }
    }
    
    // IPC 핸들러 정리
    const ipcService = this.resolve<IPCService>('ipcService')
    ipcService.removeAllHandlers()
    
    this.services.clear()
    this.singletons.clear()
    this.initialized = false
    
    console.log('✅ ServiceContainer disposed')
  }
}

// 전역 서비스 컨테이너
export const serviceContainer = new ServiceContainer()
```

## 🔄 통합 사용 예시

### main.ts에서의 사용
```typescript
import { serviceContainer } from './services/ServiceContainer'

app.whenReady().then(() => {
  // 서비스 컨테이너 초기화
  serviceContainer.initialize()
  
  // 윈도우 매니저를 통한 윈도우 생성
  const windowManager = serviceContainer.resolve<WindowManager>('windowManager')
  windowManager.createMainWindow()
  windowManager.createOverlayWindow()
})

app.on('before-quit', async () => {
  // 애플리케이션 종료 시 서비스 정리
  await serviceContainer.dispose()
})
```

### 새로운 기능 추가 시
```typescript
// 새로운 Core 명령어 추가
const coreService = serviceContainer.resolve<CoreCommunicationService>('coreCommunicationService')

// 새로운 명령어 등록
coreService.registerCommand({
  name: 'game:buildings:count',
  requestValidator: (data): data is { raceType: string } => 
    typeof data.raceType === 'string',
  handler: async (req) => {
    // 새로운 Core 명령어 실행
    return await namedPipeService.sendCommand('get-building-counts', [req.raceType])
  }
})

// 새로운 IPC 핸들러 추가
const ipcService = serviceContainer.resolve<IPCService>('ipcService')
ipcService.registerHandler('game:get-building-counts', async (data) => 
  await coreService.sendCommand({ type: 'game:buildings:count', payload: data })
)
```

## 🎯 설계의 주요 장점

### 1. **높은 응집도**
- 각 서비스는 단일 책임을 가짐
- 관련 기능들이 논리적으로 그룹화됨
- 코드 수정 시 영향 범위가 명확함

### 2. **낮은 결합도**
- 인터페이스를 통한 의존성 주입
- 서비스 간 직접 참조 최소화
- 테스트 가능한 구조

### 3. **확장성**
- 새로운 Core 명령어 쉽게 추가 가능
- IPC 채널 타입 안전하게 확장 가능
- 새로운 데이터 타입 유연하게 추가 가능

### 4. **유지보수성**
- 명확한 파일 구조와 네이밍 컨벤션
- 타입 안전성을 통한 런타임 오류 방지
- 중앙 집중화된 서비스 관리

### 5. **타입 안전성**
- TypeScript를 활용한 컴파일 타임 검증
- IPC 채널 타입 정의를 통한 안전한 통신
- 명령어 요청/응답 타입 검증

## 📝 구현 순서

1. **기본 인터페이스 정의** - 각 서비스의 인터페이스 먼저 정의
2. **ServiceContainer 구현** - 의존성 주입 시스템 구축
3. **WindowManager 구현** - 윈도우 관리 기반 구축
4. **DataStorageService 구현** - 데이터 저장 기반 구축
5. **AuthService 구현** - 인증 시스템 구축
6. **CoreCommunicationService 구현** - Core 통신 시스템 구축
7. **IPCService 구현** - Renderer 통신 시스템 구축
8. **통합 테스트** - 모든 서비스 통합 테스트
9. **기존 코드 마이그레이션** - 기존 코드를 새 구조로 이전

이 설계는 기존 코드를 최대한 활용하면서 응집도를 높이고 확장성을 보장하는 구조입니다.