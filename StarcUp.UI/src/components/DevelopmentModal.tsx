import { ArrowLeft, Construction, Info, Clock, Star, Target, Users, Cog } from 'lucide-react';

interface DevelopmentModalProps {
  isOpen: boolean;
  onClose: () => void;
  featureName: string;
  featureType: 'buildorder' | 'upgrade' | 'population' | 'unit';
}

// 기능별 상세 정보 매핑
const FEATURE_CONFIG = {
  buildorder: {
    icon: Construction,
    title: '빌드 오더 설정',
    subtitle: '현재 개발 중인 기능입니다',
    mainMessage: '빌드 오더 기능은 현재 개발 중입니다.',
    features: [
      {
        title: '단계별 빌드 가이드',
        description: '각 단계별 최적 빌드 순서 안내'
      }
    ],
    releaseDate: '2025년 상반기 예정'
  },
  upgrade: {
    icon: Star,
    title: '업그레이드 설정',
    subtitle: '현재 개발 중인 기능입니다',
    mainMessage: '업그레이드 기능은 현재 개발 중입니다.',
    features: [
      {
        title: '업그레이드 진행 추적',
        description: '실시간 업그레이드 상태 모니터링'
      }
    ],
    releaseDate: '2025년 상반기 예정'
  },
  population: {
    icon: Users,
    title: '인구 수 설정',
    subtitle: '현재 개발 중인 기능입니다',
    mainMessage: '인구 수 기능은 현재 개발 중입니다.',
    features: [
      {
        title: '인구 수 관리 도구',
        description: '인구 수 출력, 부족 경고, 건물 기반 계산'
      }
    ],
    releaseDate: '2025년 상반기 예정'
  },
  unit: {
    icon: Target,
    title: '유닛 설정',
    subtitle: '현재 개발 중인 기능입니다',
    mainMessage: '유닛 기능은 현재 개발 중입니다.',
    features: [
      {
        title: '유닛 관리 도구',
        description: '유닛 수 출력, 생산/사망 감지, 카테고리별 관리'
      }
    ],
    releaseDate: '2025년 상반기 예정'
  }
};

export function DevelopmentModal({ isOpen, onClose, featureName, featureType }: DevelopmentModalProps) {
  if (!isOpen) return null;

  const config = FEATURE_CONFIG[featureType];
  const IconComponent = config.icon;

  return (
    <div className="preset-settings-page w-full h-screen overflow-hidden border-2 shadow-2xl"
      style={{
        backgroundColor: 'var(--starcraft-bg)',
        borderColor: 'var(--starcraft-inactive-border)',
        boxShadow: '0 0 30px rgba(128, 128, 128, 0.3), inset 0 0 30px rgba(128, 128, 128, 0.1)'
      }}
    >
      {/* 전체 화면 컨테이너 */}
      <div 
        className="development-modal relative w-full h-full flex flex-col"
        style={{
          backgroundColor: 'var(--starcraft-bg)'
        }}
      >
        {/* 헤더 */}
        <div 
          className="flex items-center justify-between p-4 border-b draggable-titlebar"
          style={{ 
            backgroundColor: 'var(--starcraft-bg-secondary)',
            borderBottomColor: 'var(--starcraft-border)'
          }}
        >
          <div className="flex items-center gap-3">
            <button
              onClick={onClose}
              className="p-2 rounded-sm transition-all duration-300 hover:bg-gray-500/20"
              style={{ color: 'var(--starcraft-inactive-text)' }}
            >
              <ArrowLeft className="w-5 h-5" />
            </button>
            
            <IconComponent 
              className="w-6 h-6" 
              style={{ color: 'var(--starcraft-inactive-text)' }}
            />
            <div>
              <h1 
                className="text-xl font-semibold tracking-wide"
                style={{ 
                  color: 'var(--starcraft-inactive-text)',
                  textShadow: '0 0 8px rgba(128, 128, 128, 0.3)'
                }}
              >
                {config.title}
              </h1>
              <p 
                className="text-sm opacity-70"
                style={{ color: 'var(--starcraft-inactive-text)' }}
              >
                {config.subtitle}
              </p>
            </div>
          </div>
        </div>

        {/* 컨텐츠 */}
        <div className="flex-1 flex items-center justify-center p-8">
          <div className="text-center space-y-6 max-w-md">
            {/* 아이콘과 메인 메시지 */}
            <div className="space-y-4">
              <div 
                className="mx-auto w-20 h-20 rounded-full border-4 border-dashed flex items-center justify-center"
                style={{ 
                  borderColor: 'var(--starcraft-inactive-border)',
                  backgroundColor: 'var(--starcraft-bg-secondary)'
                }}
              >
                <IconComponent 
                  className="w-10 h-10" 
                  style={{ color: 'var(--starcraft-inactive-text)' }}
                />
              </div>
              
              <div className="space-y-2">
                <h2 
                  className="text-2xl font-semibold tracking-wide"
                  style={{ color: 'var(--starcraft-inactive-text)' }}
                >
                  기능 개발 중
                </h2>
                <p 
                  className="text-base opacity-80 leading-relaxed"
                  style={{ color: 'var(--starcraft-inactive-text)' }}
                >
                  {config.mainMessage}
                </p>
              </div>
            </div>

            {/* 개발 예정 기능들 안내 */}
            <div 
              className="p-6 rounded-lg border text-left"
              style={{
                backgroundColor: 'var(--starcraft-bg-secondary)',
                borderColor: 'var(--starcraft-inactive-border)'
              }}
            >
              <div className="flex items-center gap-2 mb-4">
                <Info 
                  className="w-5 h-5" 
                  style={{ color: 'var(--starcraft-inactive-text)' }}
                />
                <h3 
                  className="font-medium tracking-wide"
                  style={{ color: 'var(--starcraft-inactive-text)' }}
                >
                  개발 예정 기능
                </h3>
              </div>
              
              <ul className="space-y-3 text-sm opacity-80" style={{ color: 'var(--starcraft-inactive-text)' }}>
                {config.features.map((feature, index) => (
                  <li key={index} className="flex items-start gap-3">
                    <span className="text-xs mt-1">•</span>
                    <div>
                      <strong>{feature.title}</strong>
                      <br />
                      <span className="opacity-70">{feature.description}</span>
                    </div>
                  </li>
                ))}
              </ul>
            </div>

            {/* 예상 출시 시기 */}
            <div 
              className="p-4 rounded-lg border"
              style={{
                backgroundColor: 'var(--starcraft-bg-active)',
                borderColor: 'var(--starcraft-inactive-border)'
              }}
            >
              <div className="flex items-center justify-center gap-2 text-sm">
                <Clock 
                  className="w-4 h-4" 
                  style={{ color: 'var(--starcraft-inactive-text)' }}
                />
                <span style={{ color: 'var(--starcraft-inactive-text)' }}>
                  <strong>예상 출시:</strong> {config.releaseDate}
                </span>
              </div>
            </div>
          </div>
        </div>

        {/* 하단 버튼 */}
        <div 
          className="flex items-center justify-center p-4 border-t"
          style={{ 
            backgroundColor: 'var(--starcraft-bg-secondary)',
            borderTopColor: 'var(--starcraft-border)'
          }}
        >
          <button
            onClick={onClose}
            className="px-8 py-2 rounded-sm border transition-all duration-300 hover:bg-gray-500/20"
            style={{
              color: 'var(--starcraft-inactive-text)',
              borderColor: 'var(--starcraft-inactive-border)'
            }}
          >
            닫기
          </button>
        </div>
      </div>
    </div>
  );
}