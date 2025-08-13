import { 
  IPresetRepository, 
  StoredPreset, 
  PresetCollection, 
  CreatePresetRequest, 
  UpdatePresetRequest
} from './IPresetRepository'
import { WorkerSettings, PopulationSettings } from '../../../../../src/types/preset'
import { RaceType, UnitType } from '../../../../../src/types/game'
import { 
  sanitizePresetForNonPro, 
  sanitizePresetsForNonPro 
} from '../../../../../src/utils/proUtils'
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

  // 기본 인구수 설정 (종족별 모드 B 건물 설정, 인자 없으면 Fixed 모드)
  private getDefaultPopulationSettings(race?: RaceType): PopulationSettings {
    // 인자가 없으면 기존처럼 Fixed 모드로 반환
    if (race === undefined) {
      return {
        mode: 'fixed',                     // 고정값 모드가 기본
        fixedSettings: {
          thresholdValue: 4,               // 인구 부족 경고 기준값 4
          timeLimit: {
            enabled: true,                 // 시간 제한 기본 활성화
            minutes: 3,                    // 3분
            seconds: 0                     // 0초
          }
        }
      }
    }

    const getBuildingSettings = (race: RaceType) => {
      switch (race) {
        case RaceType.Protoss:
          return {
            race,
            trackedBuildings: [
              { 
                buildingType: UnitType.ProtossGateway, 
                enabled: true, 
                multiplier: 2 
              }
            ]
          }
        case RaceType.Terran:
          return {
            race,
            trackedBuildings: [
              { 
                buildingType: UnitType.TerranBarracks, 
                enabled: true, 
                multiplier: 1 
              }
            ]
          }
        case RaceType.Zerg:
          return {
            race,
            trackedBuildings: [
              { 
                buildingType: UnitType.ZergHatchery, 
                enabled: true, 
                multiplier: 2 
              }
            ]
          }
      }
    }

    return {
      mode: 'building',                  // 건물 기반 모드
      buildingSettings: getBuildingSettings(race)
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
      populationSettings: request.populationSettings || this.getDefaultPopulationSettings(request.selectedRace),
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
          name: 'Default Protoss Preset',
          description: '기본 프리셋 - 프로토스',
          featureStates: [true, true, false, false, false], // 일꾼만 활성화
          selectedRace: RaceType.Protoss,
          workerSettings: this.getDefaultWorkerSettings(),
          populationSettings: this.getDefaultPopulationSettings(RaceType.Protoss),
          createdAt: new Date(),
          updatedAt: new Date()
        },
        {
          id: this.generateId(),
          name: 'Default Terran Preset',
          description: '기본 프리셋 - 테란',
          featureStates: [true, true, false, false, false],
          selectedRace: RaceType.Terran,
          workerSettings: this.getDefaultWorkerSettings(),
          populationSettings: this.getDefaultPopulationSettings(RaceType.Terran),
          createdAt: new Date(),
          updatedAt: new Date()
        },
        {
          id: this.generateId(),
          name: 'Default Zerg Preset',
          description: '기본 프리셋 - 저그',
          featureStates: [true, true, false, false, false],
          selectedRace: RaceType.Zerg,
          workerSettings: this.getDefaultWorkerSettings(),
          populationSettings: this.getDefaultPopulationSettings(RaceType.Zerg),
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

  /**
   * 구독 기간 종료 시 모든 프리셋에서 Pro 기능을 해제하고 데이터베이스에 저장합니다.
   * 이 메소드는 사용자의 구독 상태가 Pro에서 Free로 변경될 때 호출되어야 합니다.
   */
  async sanitizeAllPresetsForNonPro(): Promise<void> {
    try {
      console.log('🔒 구독 기간 종료: 모든 프리셋에서 Pro 기능 해제 시작...');
      
      const collection = await this.loadAll();
      let hasChanges = false;

      // 모든 프리셋의 Pro 기능 해제
      const sanitizedPresets = collection.presets.map(preset => {
        const originalWorkerSettings = JSON.stringify(preset.workerSettings);
        const originalPopulationSettings = JSON.stringify(preset.populationSettings);
        
        const sanitizedPreset = sanitizePresetForNonPro(preset);
        
        // 변경사항이 있는지 확인
        const newWorkerSettings = JSON.stringify(sanitizedPreset.workerSettings);
        const newPopulationSettings = JSON.stringify(sanitizedPreset.populationSettings);
        
        if (originalWorkerSettings !== newWorkerSettings || originalPopulationSettings !== newPopulationSettings) {
          hasChanges = true;
          console.log(`✂️ 프리셋 "${preset.name}" Pro 기능 해제됨`);
        }
        
        return {
          ...sanitizedPreset,
          updatedAt: new Date() // 수정 시간 업데이트
        };
      });

      if (hasChanges) {
        // 변경된 프리셋 컬렉션 저장
        const updatedCollection: PresetCollection = {
          ...collection,
          presets: sanitizedPresets,
          lastUpdated: new Date()
        };

        await this.saveCollection(updatedCollection);
        console.log('💾 Pro 기능 해제된 프리셋들이 데이터베이스에 저장되었습니다.');
      } else {
        console.log('ℹ️ 해제할 Pro 기능이 없습니다. 모든 프리셋이 이미 Free 모드 호환입니다.');
      }
    } catch (error) {
      console.error('❌ Pro 기능 해제 중 오류 발생:', error);
      throw error;
    }
  }

  /**
   * 특정 프리셋의 Pro 기능을 해제하고 데이터베이스에 저장합니다.
   */
  async sanitizePresetForNonPro(presetId: string): Promise<StoredPreset> {
    try {
      console.log(`🔒 프리셋 "${presetId}" Pro 기능 해제 시작...`);
      
      const collection = await this.loadAll();
      const presetIndex = collection.presets.findIndex(p => p.id === presetId);
      
      if (presetIndex === -1) {
        throw new Error(`프리셋을 찾을 수 없습니다: ${presetId}`);
      }

      const originalPreset = collection.presets[presetIndex];
      const sanitizedPreset = sanitizePresetForNonPro(originalPreset);
      
      // 변경사항 확인
      const hasWorkerChanges = JSON.stringify(originalPreset.workerSettings) !== JSON.stringify(sanitizedPreset.workerSettings);
      const hasPopulationChanges = JSON.stringify(originalPreset.populationSettings) !== JSON.stringify(sanitizedPreset.populationSettings);
      
      if (hasWorkerChanges || hasPopulationChanges) {
        const updatedPreset: StoredPreset = {
          ...sanitizedPreset,
          updatedAt: new Date()
        };
        
        collection.presets[presetIndex] = updatedPreset;
        collection.lastUpdated = new Date();
        
        await this.saveCollection(collection);
        console.log(`✂️💾 프리셋 "${originalPreset.name}" Pro 기능 해제 및 저장 완료`);
        
        return updatedPreset;
      } else {
        console.log(`ℹ️ 프리셋 "${originalPreset.name}"에서 해제할 Pro 기능이 없습니다.`);
        return originalPreset;
      }
    } catch (error) {
      console.error(`❌ 프리셋 "${presetId}" Pro 기능 해제 중 오류 발생:`, error);
      throw error;
    }
  }
}