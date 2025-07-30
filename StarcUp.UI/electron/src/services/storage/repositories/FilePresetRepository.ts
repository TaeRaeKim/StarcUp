import { 
  IPresetRepository, 
  StoredPreset, 
  PresetCollection, 
  CreatePresetRequest, 
  UpdatePresetRequest,
  WorkerSettings
} from './IPresetRepository'
import * as fs from 'fs/promises'
import * as path from 'path'
import { app } from 'electron'

/**
 * 파일 기반 프리셋 Repository 구현체
 * JSON 파일을 사용하여 프리셋 데이터를 저장/로드합니다
 */
export class FilePresetRepository implements IPresetRepository {
  private readonly configPath: string
  private readonly presetsFilePath: string
  private readonly MAX_PRESETS = 3
  private readonly DATA_VERSION = '1.0.0'
  
  // 메모리 캐시 (성능 최적화)
  private cachedData: PresetCollection | null = null
  private lastLoadTime: number = 0
  private readonly CACHE_TTL = 5000 // 5초 캐시
  
  constructor() {
    this.configPath = path.join(app.getPath('userData'), 'StarcUp')
    this.presetsFilePath = path.join(this.configPath, 'presets.json')
    this.ensureDirectories()
  }
  
  // 기본 일꾼 설정
  private getDefaultWorkerSettings(): WorkerSettings {
    return {
      workerCountDisplay: true,           // 일꾼 수 출력 기본 활성화
      includeProducingWorkers: false,     // 생산 중인 일꾼 수 포함 기본 비활성화
      idleWorkerDisplay: true,            // 유휴 일꾼 수 출력 기본 활성화
      workerProductionDetection: true,    // 일꾼 생산 감지 기본 활성화
      workerDeathDetection: true,         // 일꾼 사망 감지 기본 활성화
      gasWorkerCheck: true                // 가스 일꾼 체크 기본 활성화
    }
  }
  
  async loadAll(): Promise<PresetCollection> {
    // 캐시 확인
    if (this.cachedData && Date.now() - this.lastLoadTime < this.CACHE_TTL) {
      return this.cachedData
    }
    
    try {
      await fs.access(this.presetsFilePath)
      const fileContent = await fs.readFile(this.presetsFilePath, 'utf-8')
      const data = JSON.parse(fileContent)
      
      // Date 객체 복원
      const collection: PresetCollection = {
        ...data,
        lastUpdated: new Date(data.lastUpdated),
        presets: data.presets.map((preset: any) => ({
          ...preset,
          createdAt: new Date(preset.createdAt),
          updatedAt: new Date(preset.updatedAt)
        }))
      }
      
      this.cachedData = collection
      this.lastLoadTime = Date.now()
      
      console.log('📂 프리셋 데이터 로드 완료:', {
        count: collection.presets.length,
        selected: collection.selectedPresetIndex
      })
      
      return collection
    } catch (error) {
      console.log('📂 프리셋 파일이 없음, 기본 데이터 생성')
      return await this.createDefaultData()
    }
  }
  
  async findById(id: string): Promise<StoredPreset | null> {
    const collection = await this.loadAll()
    return collection.presets.find(p => p.id === id) || null
  }
  
  async create(request: CreatePresetRequest): Promise<StoredPreset> {
    const collection = await this.loadAll()
    
    if (collection.presets.length >= this.MAX_PRESETS) {
      throw new Error(`최대 ${this.MAX_PRESETS}개의 프리셋만 생성할 수 있습니다`)
    }
    
    const newPreset: StoredPreset = {
      id: this.generateId(),
      ...request,
      workerSettings: request.workerSettings || this.getDefaultWorkerSettings(),
      createdAt: new Date(),
      updatedAt: new Date()
    }
    
    collection.presets.push(newPreset)
    collection.lastUpdated = new Date()
    
    await this.saveCollection(collection)
    
    console.log('💾 새 프리셋 생성:', newPreset.name)
    return newPreset
  }
  
  async update(request: UpdatePresetRequest): Promise<StoredPreset> {
    const collection = await this.loadAll()
    const presetIndex = collection.presets.findIndex(p => p.id === request.id)
    
    if (presetIndex === -1) {
      throw new Error(`프리셋을 찾을 수 없습니다: ${request.id}`)
    }
    
    const existingPreset = collection.presets[presetIndex]
    const updatedPreset: StoredPreset = {
      ...existingPreset,
      ...Object.fromEntries(
        Object.entries(request).filter(([key, value]) => 
          key !== 'id' && value !== undefined
        )
      ),
      updatedAt: new Date()
    }
    
    collection.presets[presetIndex] = updatedPreset
    collection.lastUpdated = new Date()
    
    await this.saveCollection(collection)
    
    console.log('💾 프리셋 업데이트:', updatedPreset.name)
    return updatedPreset
  }
  
  async delete(id: string): Promise<void> {
    const collection = await this.loadAll()
    const presetIndex = collection.presets.findIndex(p => p.id === id)
    
    if (presetIndex === -1) {
      throw new Error(`프리셋을 찾을 수 없습니다: ${id}`)
    }
    
    const deletedPreset = collection.presets[presetIndex]
    collection.presets.splice(presetIndex, 1)
    
    // 선택된 인덱스 조정
    if (collection.selectedPresetIndex >= presetIndex) {
      collection.selectedPresetIndex = Math.max(0, collection.selectedPresetIndex - 1)
    }
    
    collection.lastUpdated = new Date()
    await this.saveCollection(collection)
    
    console.log('🗑️ 프리셋 삭제:', deletedPreset.name)
  }
  
  async updateSelectedIndex(index: number): Promise<void> {
    const collection = await this.loadAll()
    
    if (index < 0 || index >= collection.presets.length) {
      throw new Error(`잘못된 프리셋 인덱스: ${index}`)
    }
    
    collection.selectedPresetIndex = index
    collection.lastUpdated = new Date()
    
    await this.saveCollection(collection)
    
    console.log('🎯 선택된 프리셋 변경:', collection.presets[index].name)
  }
  
  async getSelected(): Promise<StoredPreset | null> {
    const collection = await this.loadAll()
    const selectedPreset = collection.presets[collection.selectedPresetIndex]
    return selectedPreset || null
  }
  
  async count(): Promise<number> {
    const collection = await this.loadAll()
    return collection.presets.length
  }
  
  async initialize(): Promise<void> {
    try {
      await this.loadAll()
      console.log('✅ 프리셋 Repository 초기화 완료')
    } catch (error) {
      console.error('❌ 프리셋 Repository 초기화 실패:', error)
      throw error
    }
  }
  
  // Private helper methods
  
  private async createDefaultData(): Promise<PresetCollection> {
    const defaultCollection: PresetCollection = {
      version: this.DATA_VERSION,
      maxPresets: this.MAX_PRESETS,
      selectedPresetIndex: 0,
      lastUpdated: new Date(),
      presets: [
        {
          id: this.generateId(),
          name: 'Default Preset',
          description: '기본 프리셋 - 일꾼 기능만 활성화됨',
          featureStates: [true, false, false, false, false], // 일꾼만 활성화
          selectedRace: 'protoss',
          workerSettings: this.getDefaultWorkerSettings(),
          createdAt: new Date(),
          updatedAt: new Date()
        },
        {
          id: this.generateId(),
          name: '커공발-운영',
          description: '커세어 + 공중 발업 운영 빌드',
          featureStates: [true, false, false, false, false],
          selectedRace: 'terran',
          workerSettings: this.getDefaultWorkerSettings(),
          createdAt: new Date(),
          updatedAt: new Date()
        },
        {
          id: this.generateId(),
          name: '패닼아비터',
          description: '패스트 다크템플러 + 아비터 전략',
          featureStates: [true, false, false, false, false],
          selectedRace: 'protoss',
          workerSettings: this.getDefaultWorkerSettings(),
          createdAt: new Date(),
          updatedAt: new Date()
        }
      ]
    }
    
    await this.saveCollection(defaultCollection)
    console.log('📁 기본 프리셋 데이터 생성 완료')
    
    return defaultCollection
  }
  
  private async saveCollection(collection: PresetCollection): Promise<void> {
    await this.ensureDirectories()
    
    // JSON으로 저장하되, Date 객체는 ISO 문자열로 변환
    const jsonData = JSON.stringify(collection, null, 2)
    await fs.writeFile(this.presetsFilePath, jsonData, 'utf-8')
    
    // 캐시 업데이트
    this.cachedData = collection
    this.lastLoadTime = Date.now()
  }
  
  private async ensureDirectories(): Promise<void> {
    try {
      await fs.access(this.configPath)
    } catch (error) {
      console.log('📁 데이터 디렉토리 생성:', this.configPath)
      await fs.mkdir(this.configPath, { recursive: true })
    }
  }
  
  private generateId(): string {
    return `preset-${Date.now()}-${Math.random().toString(36).substr(2, 9)}`
  }
}