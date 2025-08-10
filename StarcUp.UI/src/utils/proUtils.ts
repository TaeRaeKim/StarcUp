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