import { WorkerSettings, PopulationSettings } from '../types/preset';

// WorkerPresetEnum과 동일한 값들을 사용 (Core와 동기화)
export const WorkerPresetFlags = {
  None: 0,                    // 0b0000_0000
  Default: 1,                 // 0b0000_0001 - 일꾼 수 출력
  IncludeProduction: 2,       // 0b0000_0010 - 생산 중인 일꾼 수 포함
  Idle: 4,                    // 0b0000_0100 - 유휴 일꾼 수 출력
  DetectProduction: 8,        // 0b0000_1000 - 일꾼 생산 감지
  DetectDeath: 16,            // 0b0001_0000 - 일꾼 사망 감지
  CheckGas: 32,               // 0b0010_0000 - 가스 일꾼 체크
} as const;

// 프리셋 타입 정의
export interface WorkerPreset {
  enabled: boolean;           // 일꾼 기능 전체 활성화 여부
  settingsMask: number;       // 8bit 비트마스크 (0-255)
}

export interface PopulationPreset {
  enabled: boolean;
  settingsMask?: number;          // 선택적 (인구수는 사용하지 않음)
  settings?: PopulationSettings;  // 전체 설정 객체 (주로 사용)
}

// PopulationSettings 관련 인터페이스들은 중앙 타입 정의에서 import
// 참고: TrackedBuilding의 buildingType은 UnitType을 사용하지만 중앙 정의에서는 string 사용

export interface UnitPreset {
  enabled: boolean;
  settingsMask: number;
}

export interface UpgradePreset {
  enabled: boolean;
  settingsMask: number;
}

export interface BuildOrderPreset {
  enabled: boolean;
  settingsMask: number;
}

// 프리셋 초기화 메시지
export interface PresetInitMessage {
  type: 'preset-init';
  timestamp: number;
  presets: {
    worker?: WorkerPreset;
    population?: PopulationPreset;
    unit?: UnitPreset;
    upgrade?: UpgradePreset;
    buildOrder?: BuildOrderPreset;
  };
}

// 프리셋 업데이트 메시지
export interface PresetUpdateMessage {
  type: 'preset-update';
  timestamp: number;
  presetType: 'worker' | 'population' | 'unit' | 'upgrade' | 'buildOrder';
  data: WorkerPreset | PopulationPreset | UnitPreset | UpgradePreset | BuildOrderPreset;
}

// 일꾼 설정을 WorkerPresetEnum 호환 비트마스크로 변환하는 함수
export function calculateWorkerSettingsMask(settings: WorkerSettings): number {
  let mask = 0;
  
  if (settings.workerCountDisplay) mask |= WorkerPresetFlags.Default;
  if (settings.includeProducingWorkers) mask |= WorkerPresetFlags.IncludeProduction;
  if (settings.idleWorkerDisplay) mask |= WorkerPresetFlags.Idle;
  if (settings.workerProductionDetection) mask |= WorkerPresetFlags.DetectProduction;
  if (settings.workerDeathDetection) mask |= WorkerPresetFlags.DetectDeath;
  if (settings.gasWorkerCheck) mask |= WorkerPresetFlags.CheckGas;
  
  return mask;
}

// 비트마스크를 일꾼 설정으로 변환하는 함수 (역변환, 디버깅용)
export function parseWorkerSettingsMask(mask: number): WorkerSettings {
  return {
    workerCountDisplay: (mask & WorkerPresetFlags.Default) !== 0,
    includeProducingWorkers: (mask & WorkerPresetFlags.IncludeProduction) !== 0,
    idleWorkerDisplay: (mask & WorkerPresetFlags.Idle) !== 0,
    workerProductionDetection: (mask & WorkerPresetFlags.DetectProduction) !== 0,
    workerDeathDetection: (mask & WorkerPresetFlags.DetectDeath) !== 0,
    gasWorkerCheck: (mask & WorkerPresetFlags.CheckGas) !== 0
  };
}

// 비트마스크를 이진 문자열로 표시하는 유틸리티 함수 (디버깅용)
export function maskToBinaryString(mask: number): string {
  return '0b' + mask.toString(2).padStart(8, '0').replace(/(.{4})(.{4})/, '$1_$2');
}

// 비트마스크를 16진수 문자열로 표시하는 유틸리티 함수 (디버깅용)
export function maskToHexString(mask: number): string {
  return '0x' + mask.toString(16).padStart(2, '0').toUpperCase();
}

// 설정 상태를 콘솔에 출력하는 디버깅 함수
export function debugWorkerSettings(settings: WorkerSettings): void {
  const mask = calculateWorkerSettingsMask(settings);
  console.log('🔧 일꾼 설정 디버깅:');
  console.log(`비트마스크: ${mask} (${maskToBinaryString(mask)}, ${maskToHexString(mask)})`);
  console.log('설정 상태:');
  console.log(`  일꾼 수 출력: ${settings.workerCountDisplay ? '✅' : '❌'} (Default = ${WorkerPresetFlags.Default})`);
  console.log(`  생산 중인 일꾼 수 포함: ${settings.includeProducingWorkers ? '✅' : '❌'} (IncludeProduction = ${WorkerPresetFlags.IncludeProduction})`);
  console.log(`  유휴 일꾼 수 출력: ${settings.idleWorkerDisplay ? '✅' : '❌'} (Idle = ${WorkerPresetFlags.Idle})`);
  console.log(`  일꾼 생산 감지: ${settings.workerProductionDetection ? '✅' : '❌'} (DetectProduction = ${WorkerPresetFlags.DetectProduction})`);
  console.log(`  일꾼 사망 감지: ${settings.workerDeathDetection ? '✅' : '❌'} (DetectDeath = ${WorkerPresetFlags.DetectDeath})`);
  console.log(`  가스 일꾼 체크: ${settings.gasWorkerCheck ? '✅' : '❌'} (CheckGas = ${WorkerPresetFlags.CheckGas})`);
}

// 프리셋 플래그 이름을 문자열로 변환하는 유틸리티 함수
export function getWorkerPresetFlagNames(mask: number): string[] {
  const flags: string[] = [];
  
  if (mask & WorkerPresetFlags.Default) flags.push('Default');
  if (mask & WorkerPresetFlags.IncludeProduction) flags.push('IncludeProduction');
  if (mask & WorkerPresetFlags.Idle) flags.push('Idle');
  if (mask & WorkerPresetFlags.DetectProduction) flags.push('DetectProduction');
  if (mask & WorkerPresetFlags.DetectDeath) flags.push('DetectDeath');
  if (mask & WorkerPresetFlags.CheckGas) flags.push('CheckGas');
  
  return flags.length > 0 ? flags : ['None'];
}