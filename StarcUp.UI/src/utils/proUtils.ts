/**
 * Pro 구독 상태 및 기능 관리 유틸리티
 */

export interface ProStatus {
  isPro: boolean;
  subscriptionType: 'free' | 'pro' | 'premium';
  expiresAt?: Date;
  features: ProFeature[];
}

export interface ProFeature {
  id: string;
  name: string;
  description: string;
  category: 'worker' | 'unit' | 'upgrade' | 'population' | 'buildorder';
  isEnabled: boolean;
  requiredTier: 'pro' | 'premium';
}

// Pro 전용 기능 ID 정의
export const PRO_FEATURES = {
  WORKER_PRODUCTION_DETECTION: 'worker_production_detection',
  WORKER_DEATH_DETECTION: 'worker_death_detection',
  GAS_WORKER_CHECK: 'gas_worker_check',
  ADVANCED_UNIT_TRACKING: 'advanced_unit_tracking',
  UPGRADE_OPTIMIZATION: 'upgrade_optimization',
  BUILD_ORDER_AI: 'build_order_ai',
  POPULATION_FORECASTING: 'population_forecasting',
  BUILDING_BASED_POPULATION: 'building_based_population',
} as const;

// 기본 Pro 기능 설정
export const DEFAULT_PRO_FEATURES: ProFeature[] = [
  {
    id: PRO_FEATURES.WORKER_PRODUCTION_DETECTION,
    name: '일꾼 생산 감지',
    description: '새로운 일꾼이 생산될 때마다 실시간으로 알림을 받을 수 있습니다.',
    category: 'worker',
    isEnabled: true,
    requiredTier: 'pro'
  },
  {
    id: PRO_FEATURES.WORKER_DEATH_DETECTION,
    name: '일꾼 사망 감지',
    description: '일꾼이 사망할 때 즉시 경고를 받아 빠르게 대응할 수 있습니다.',
    category: 'worker',
    isEnabled: true,
    requiredTier: 'pro'
  },
  {
    id: PRO_FEATURES.GAS_WORKER_CHECK,
    name: '가스 일꾼 체크',
    description: '가스 건물마다 최적의 일꾼 수가 배치되어 있는지 자동으로 확인합니다.',
    category: 'worker',
    isEnabled: true,
    requiredTier: 'pro'
  },
  {
    id: PRO_FEATURES.ADVANCED_UNIT_TRACKING,
    name: '고급 유닛 추적',
    description: '유닛별 세부 통계와 효율성 분석을 제공합니다.',
    category: 'unit',
    isEnabled: true,
    requiredTier: 'pro'
  },
  {
    id: PRO_FEATURES.UPGRADE_OPTIMIZATION,
    name: '업그레이드 최적화',
    description: 'AI가 현재 상황에 최적화된 업그레이드 순서를 추천합니다.',
    category: 'upgrade',
    isEnabled: true,
    requiredTier: 'premium'
  },
  {
    id: PRO_FEATURES.BUILD_ORDER_AI,
    name: 'AI 빌드오더',
    description: '상대방 종족과 전략에 맞는 최적 빌드오더를 AI가 실시간 분석합니다.',
    category: 'buildorder',
    isEnabled: true,
    requiredTier: 'premium'
  },
  {
    id: PRO_FEATURES.POPULATION_FORECASTING,
    name: '인구수 예측',
    description: '현재 생산 계획을 바탕으로 미래 인구수 변화를 예측합니다.',
    category: 'population',
    isEnabled: true,
    requiredTier: 'pro'
  },
  {
    id: PRO_FEATURES.BUILDING_BASED_POPULATION,
    name: '생산 건물 기반 인구 관리',
    description: '생산 건물 수를 기반으로 한 지능형 인구 관리 시스템으로 더욱 정확한 전략적 플레이가 가능합니다.',
    category: 'population',
    isEnabled: true,
    requiredTier: 'pro'
  }
];

// 개발 모드 Pro 상태 저장용 (실제 배포에서는 제거)
let _devProStatus: boolean | null = null;

/**
 * 개발 모드에서 Pro 상태를 설정합니다 (실제 배포에서는 제거)
 */
export function setDevProStatus(isPro: boolean): void {
  _devProStatus = isPro;
}

/**
 * 사용자의 Pro 상태를 확인합니다.
 * 실제 구현에서는 서버나 로컬 스토리지에서 데이터를 가져올 것입니다.
 */
export function getProStatus(): ProStatus {
  // 개발 모드에서는 동적으로 변경 가능
  const isPro = _devProStatus !== null ? _devProStatus : false;
  
  // 개발용 임시 데이터 - 실제로는 API 호출이나 로컬 스토리지에서 가져와야 함
  const mockProStatus: ProStatus = {
    isPro: isPro,
    subscriptionType: isPro ? 'pro' : 'free',
    features: DEFAULT_PRO_FEATURES
  };

  return mockProStatus;
}

/**
 * 특정 기능이 Pro 전용인지 확인합니다.
 */
export function isProFeature(featureId: string): boolean {
  return Object.values(PRO_FEATURES).includes(featureId as any);
}

/**
 * 사용자가 특정 기능을 사용할 수 있는지 확인합니다.
 */
export function canUseFeature(featureId: string, proStatus: ProStatus = getProStatus()): boolean {
  if (!isProFeature(featureId)) {
    return true; // Pro 기능이 아니면 모든 사용자가 사용 가능
  }

  if (!proStatus.isPro) {
    return false; // Pro가 아니면 Pro 기능 사용 불가
  }

  const feature = proStatus.features.find(f => f.id === featureId);
  if (!feature) {
    return false;
  }

  // 구독 타입에 따른 기능 사용 가능 여부 확인
  if (feature.requiredTier === 'premium') {
    return proStatus.subscriptionType === 'premium';
  }

  return proStatus.subscriptionType === 'pro' || proStatus.subscriptionType === 'premium';
}

/**
 * 카테고리별 Pro 기능 목록을 가져옵니다.
 */
export function getProFeaturesByCategory(category: ProFeature['category']): ProFeature[] {
  return DEFAULT_PRO_FEATURES.filter(feature => feature.category === category);
}

/**
 * 일꾼 관련 Pro 기능 ID 배열을 반환합니다.
 */
export function getWorkerProFeatures(): string[] {
  return [
    PRO_FEATURES.WORKER_PRODUCTION_DETECTION,
    PRO_FEATURES.WORKER_DEATH_DETECTION,
    PRO_FEATURES.GAS_WORKER_CHECK
  ];
}

/**
 * Pro 업그레이드 페이지로 리디렉션합니다.
 */
export function redirectToProUpgrade(): void {
  console.log('Pro 업그레이드 페이지로 이동 - 실제 구현 필요');
  // 실제 구현에서는 Pro 구독 페이지나 모달을 여는 로직이 들어갈 것입니다.
}

/**
 * 구독 기간 종료 시 Pro 기능을 해제합니다.
 * 일꾼 설정에서 Pro 전용 기능들을 비활성화합니다.
 */
export function sanitizeWorkerSettingsForNonPro(settings: any): any {
  if (!settings) return settings;
  
  return {
    ...settings,
    // Pro 전용 기능들을 false로 설정
    workerProductionDetection: false,  // 일꾼 생산 감지
    workerDeathDetection: false,       // 일꾼 사망 감지
    gasWorkerCheck: false              // 가스 일꾼 체크
  };
}

/**
 * 구독 기간 종료 시 인구수 설정에서 Pro 기능을 해제합니다.
 * 모드 B(생산 건물 기반)를 모드 A(고정값 기반)로 변경합니다.
 */
export function sanitizePopulationSettingsForNonPro(settings: any): any {
  if (!settings) return settings;
  
  // 모드 B(building)인 경우 모드 A(fixed)로 변경
  if (settings.mode === 'building') {
    return {
      mode: 'fixed',
      fixedSettings: {
        thresholdValue: 4,  // 기본값
        timeLimit: {
          enabled: true,
          minutes: 3,
          seconds: 0
        }
      }
    };
  }
  
  // 이미 모드 A인 경우 그대로 반환
  return settings;
}

/**
 * 프리셋 데이터를 Free 모드에 맞게 정리합니다.
 * Pro 전용 기능들을 모두 해제하여 Free 사용자도 프리셋을 로드할 수 있게 합니다.
 */
export function sanitizePresetForNonPro(preset: any): any {
  if (!preset) return preset;
  
  return {
    ...preset,
    workerSettings: sanitizeWorkerSettingsForNonPro(preset.workerSettings),
    populationSettings: sanitizePopulationSettingsForNonPro(preset.populationSettings)
  };
}

/**
 * 여러 프리셋을 한번에 정리합니다.
 */
export function sanitizePresetsForNonPro(presets: any[]): any[] {
  if (!Array.isArray(presets)) return presets;
  
  return presets.map(preset => sanitizePresetForNonPro(preset));
}

/**
 * 구독 상태가 Pro에서 Free로 변경되었을 때 호출되는 함수입니다.
 * 데이터베이스에서 모든 프리셋의 Pro 기능을 영구적으로 해제합니다.
 */
export async function handleSubscriptionDowngrade(): Promise<void> {
  try {
    console.log('📉 구독 상태 변경 감지: Pro → Free');
    
    // PresetAPI를 통해 데이터베이스에서 Pro 기능 해제
    if (window.presetAPI?.sanitizeAllPresetsForNonPro) {
      console.log('🔒 데이터베이스에서 모든 프리셋 Pro 기능 해제 시작...');
      
      const result = await window.presetAPI.sanitizeAllPresetsForNonPro();
      
      if (result?.success) {
        console.log('✅ 데이터베이스 Pro 기능 해제 완료:', result.data);
        
        // UI 상태도 다시 로드하여 동기화
        if (window.presetAPI?.refreshState) {
          await window.presetAPI.refreshState();
          console.log('🔄 UI 상태 동기화 완료');
        }
      } else {
        console.error('❌ 데이터베이스 Pro 기능 해제 실패:', result?.error);
        throw new Error(result?.error || 'Pro 기능 해제 실패');
      }
    } else {
      console.warn('⚠️ presetAPI.sanitizeAllPresetsForNonPro 메소드가 없습니다.');
    }
  } catch (error) {
    console.error('❌ 구독 상태 변경 처리 중 오류:', error);
    throw error;
  }
}

/**
 * 구독 상태가 변경되었는지 확인하고 필요시 Pro 기능 해제를 수행합니다.
 * 이 함수는 앱 시작 시나 정기적으로 호출되어야 합니다.
 */
export async function checkAndHandleSubscriptionChange(currentProStatus: boolean): Promise<void> {
  try {
    // 이전 구독 상태를 로컬 스토리지에서 확인
    const previousProStatus = localStorage.getItem('lastProStatus');
    const wasProBefore = previousProStatus === 'true';
    
    console.log('🔍 구독 상태 체크:', {
      이전상태: wasProBefore ? 'Pro' : 'Free',
      현재상태: currentProStatus ? 'Pro' : 'Free',
      변경여부: wasProBefore !== currentProStatus
    });
    
    // Pro에서 Free로 변경된 경우
    if (wasProBefore && !currentProStatus) {
      console.log('📉 구독 다운그레이드 감지: Pro → Free');
      await handleSubscriptionDowngrade();
    }
    
    // 현재 상태를 저장
    localStorage.setItem('lastProStatus', currentProStatus.toString());
    
  } catch (error) {
    console.error('❌ 구독 상태 체크 중 오류:', error);
    // 오류가 발생해도 앱 실행을 중단하지 않음
  }
}