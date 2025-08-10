import { useState } from 'react';
import { Shield, Crown, Zap, Star, ChevronRight } from 'lucide-react';

interface ModeSelectionLoginProps {
  onModeSelect: (isPro: boolean) => void;
}

/**
 * Free/Pro 모드 선택 로그인 컴포넌트
 * 애플리케이션 시작 시 사용자에게 모드를 선택하게 하는 초기 화면
 */
export function ModeSelectionLogin({ onModeSelect }: ModeSelectionLoginProps) {
  const [selectedMode, setSelectedMode] = useState<boolean | null>(null);
  const [isAnimating, setIsAnimating] = useState(false);

  const handleModeSelect = (isPro: boolean) => {
    if (isAnimating) return;
    
    setSelectedMode(isPro);
    setIsAnimating(true);
    
    // 선택 애니메이션 후 다음 단계로 진행
    setTimeout(() => {
      onModeSelect(isPro);
    }, 800);
  };

  return (
    <div className="h-screen w-screen flex items-center justify-center relative overflow-hidden" style={{ backgroundColor: 'var(--starcraft-bg)' }}>
      {/* 배경 그라데이션 효과 */}
      <div className="absolute inset-0 opacity-30">
        <div 
          className="absolute inset-0 bg-gradient-radial from-transparent via-transparent to-black"
          style={{ 
            background: `radial-gradient(circle at center, transparent 0%, rgba(0, 255, 146, 0.1) 40%, transparent 80%)`
          }}
        ></div>
      </div>
      
      {/* 메인 로그인 컨테이너 */}
      <div className="relative z-10 text-center max-w-lg mx-auto px-8">
        {/* StarcUp 로고/제목 */}
        <div className="mb-12">
          <div 
            className="text-4xl font-bold tracking-wide mb-3"
            style={{ color: 'var(--starcraft-green)' }}
          >
            STARCUP
          </div>
          <div 
            className="text-lg font-mono opacity-80"
            style={{ color: 'var(--starcraft-green)' }}
          >
            스타크래프트 오버레이 도구
          </div>
          <div 
            className="text-sm font-mono mt-2 opacity-60"
            style={{ color: 'var(--starcraft-inactive-text)' }}
          >
            시작할 모드를 선택해주세요
          </div>
        </div>
        
        {/* 모드 선택 버튼들 */}
        <div className="space-y-4">
          {/* Free 모드 버튼 */}
          <button
            onClick={() => handleModeSelect(false)}
            disabled={isAnimating}
            className={`w-full p-6 rounded-lg border-2 transition-all duration-300 group hover:scale-105 hover:shadow-lg ${
              selectedMode === false ? 'scale-105' : ''
            } ${isAnimating && selectedMode !== false ? 'opacity-50' : ''}`}
            style={{
              background: selectedMode === false 
                ? 'linear-gradient(135deg, var(--starcraft-bg-active) 0%, rgba(0, 255, 146, 0.1) 100%)'
                : 'var(--starcraft-bg-active)',
              borderColor: selectedMode === false ? 'var(--starcraft-green)' : 'var(--starcraft-border)',
              boxShadow: selectedMode === false
                ? '0 8px 24px rgba(0, 255, 0, 0.3)'
                : '0 4px 12px rgba(0, 255, 0, 0.1)'
            }}
          >
            <div className="flex items-center justify-between">
              <div className="flex items-center gap-4">
                <div 
                  className="w-12 h-12 rounded-full flex items-center justify-center"
                  style={{
                    background: 'linear-gradient(135deg, var(--starcraft-green) 0%, rgba(0, 255, 146, 0.8) 100%)',
                    boxShadow: '0 4px 12px rgba(0, 255, 0, 0.3)'
                  }}
                >
                  <Shield className="w-6 h-6 text-black" />
                </div>
                <div className="text-left">
                  <div 
                    className="text-xl font-bold"
                    style={{ color: 'var(--starcraft-green)' }}
                  >
                    Free 모드로 시작
                  </div>
                  <div 
                    className="text-sm opacity-80"
                    style={{ color: 'var(--starcraft-inactive-text)' }}
                  >
                    기본 기능을 무료로 사용
                  </div>
                </div>
              </div>
              <ChevronRight 
                className={`w-6 h-6 transition-transform duration-300 ${
                  selectedMode === false ? 'translate-x-2' : 'group-hover:translate-x-1'
                }`}
                style={{ color: 'var(--starcraft-green)' }}
              />
            </div>
            
            {/* 선택 시 로딩 애니메이션 */}
            {selectedMode === false && isAnimating && (
              <div className="mt-4 flex items-center justify-center gap-2">
                <div 
                  className="w-4 h-4 rounded-full animate-spin border-2 border-transparent"
                  style={{ borderTopColor: 'var(--starcraft-green)' }}
                ></div>
                <span 
                  className="text-sm font-mono"
                  style={{ color: 'var(--starcraft-green)' }}
                >
                  Free 모드 초기화 중...
                </span>
              </div>
            )}
          </button>
          
          {/* Pro 모드 버튼 */}
          <button
            onClick={() => handleModeSelect(true)}
            disabled={isAnimating}
            className={`w-full p-6 rounded-lg border-2 transition-all duration-300 group hover:scale-105 hover:shadow-lg ${
              selectedMode === true ? 'scale-105' : ''
            } ${isAnimating && selectedMode !== true ? 'opacity-50' : ''}`}
            style={{
              background: selectedMode === true 
                ? 'linear-gradient(135deg, #ffd700 0%, #ffed4e 50%, #ffa000 100%)'
                : 'linear-gradient(135deg, rgba(255, 215, 0, 0.1) 0%, rgba(255, 237, 78, 0.05) 50%, rgba(255, 160, 0, 0.1) 100%)',
              borderColor: selectedMode === true ? '#ffa000' : '#ffd700',
              boxShadow: selectedMode === true
                ? '0 8px 24px rgba(255, 215, 0, 0.4)'
                : '0 4px 12px rgba(255, 215, 0, 0.2)'
            }}
          >
            <div className="flex items-center justify-between">
              <div className="flex items-center gap-4">
                <div 
                  className="w-12 h-12 rounded-full flex items-center justify-center relative"
                  style={{
                    background: 'linear-gradient(135deg, #ffd700 0%, #ffed4e 100%)',
                    boxShadow: '0 4px 12px rgba(255, 215, 0, 0.4)'
                  }}
                >
                  <Crown className="w-6 h-6 text-black" />
                  <Zap className="w-3 h-3 absolute -top-1 -right-1 text-orange-600" />
                </div>
                <div className="text-left">
                  <div className="flex items-center gap-2">
                    <span 
                      className="text-xl font-bold"
                      style={{ color: selectedMode === true ? '#8b5a00' : '#ffd700' }}
                    >
                      Pro 모드로 시작
                    </span>
                    <Star 
                      className="w-5 h-5"
                      style={{ color: selectedMode === true ? '#8b5a00' : '#ffd700' }}
                    />
                  </div>
                  <div 
                    className="text-sm opacity-80"
                    style={{ color: selectedMode === true ? '#8b5a00' : 'rgba(255, 215, 0, 0.8)' }}
                  >
                    모든 프리미엄 기능 사용 가능
                  </div>
                </div>
              </div>
              <ChevronRight 
                className={`w-6 h-6 transition-transform duration-300 ${
                  selectedMode === true ? 'translate-x-2' : 'group-hover:translate-x-1'
                }`}
                style={{ color: selectedMode === true ? '#8b5a00' : '#ffd700' }}
              />
            </div>
            
            {/* 선택 시 로딩 애니메이션 */}
            {selectedMode === true && isAnimating && (
              <div className="mt-4 flex items-center justify-center gap-2">
                <div 
                  className="w-4 h-4 rounded-full animate-spin border-2 border-transparent"
                  style={{ borderTopColor: '#8b5a00' }}
                ></div>
                <span 
                  className="text-sm font-mono"
                  style={{ color: '#8b5a00' }}
                >
                  Pro 모드 초기화 중...
                </span>
              </div>
            )}
          </button>
        </div>
        
        {/* 하단 안내 텍스트 */}
        <div className="mt-8">
          <div 
            className="text-xs font-mono opacity-60"
            style={{ color: 'var(--starcraft-inactive-text)' }}
          >
            선택한 모드는 언제든지 설정에서 변경할 수 있습니다
          </div>
        </div>
      </div>
    </div>
  );
}