import { useState, ReactNode } from 'react';
import { Lock, Crown, Unlock, Star } from 'lucide-react';
import { ProBadge } from './ProBadge';

interface ProFeatureWrapperProps {
  children: ReactNode;
  isPro?: boolean;
  feature: string;
  description?: string;
  className?: string;
  disabled?: boolean;
}

export function ProFeatureWrapper({ 
  children, 
  isPro = false, 
  feature, 
  description,
  className = '',
  disabled = false 
}: ProFeatureWrapperProps) {
  const [isHovered, setIsHovered] = useState(false);
  
  const isLocked = !isPro;
  const isDisabled = disabled || isLocked;

  return (
    <div 
      className={`relative transition-all duration-300 ${className}`}
      onMouseEnter={() => setIsHovered(true)}
      onMouseLeave={() => setIsHovered(false)}
    >
      {/* 메인 컨텐츠 */}
      <div 
        className={`relative transition-all duration-300 ${
          isLocked 
            ? 'opacity-60 cursor-not-allowed' 
            : isDisabled 
              ? 'opacity-50 cursor-not-allowed' 
              : 'cursor-pointer'
        }`}
        style={{
          filter: isLocked ? 'grayscale(30%) brightness(0.8)' : 'none',
          pointerEvents: isDisabled ? 'none' : 'auto'
        }}
      >
        {children}
      </div>

      {/* Pro 배지 */}
      <div className="absolute -top-2 -right-2 z-10">
        <ProBadge variant="small" />
      </div>

      {/* 잠금 아이콘 오버레이 */}
      {isLocked && (
        <div 
          className="absolute inset-0 flex items-center justify-center bg-black/40 rounded-lg transition-all duration-300"
          style={{
            backdropFilter: 'blur(1px)',
            opacity: isHovered ? 1 : 0.8
          }}
        >
          <div className="text-center">
            <div 
              className="p-3 rounded-full mb-2 mx-auto inline-flex"
              style={{
                background: 'linear-gradient(135deg, #ffd700 0%, #ffed4e 50%, #ffa000 100%)',
                boxShadow: '0 0 20px rgba(255, 215, 0, 0.6)'
              }}
            >
              <Lock className="w-6 h-6" style={{ color: '#8b5a00' }} />
            </div>
            <div className="text-xs font-medium" style={{ color: '#ffd700' }}>
              Pro 전용
            </div>
          </div>
        </div>
      )}

      {/* 호버 시 정보 툴팁 */}
      {isHovered && isLocked && (
        <div 
          className="absolute top-full left-1/2 transform -translate-x-1/2 mt-2 p-3 rounded-lg border z-20 min-w-64"
          style={{
            background: 'linear-gradient(135deg, var(--starcraft-bg-active) 0%, var(--starcraft-bg-secondary) 100%)',
            borderColor: '#ffd700',
            boxShadow: '0 4px 20px rgba(255, 215, 0, 0.3), 0 0 30px rgba(255, 215, 0, 0.1)'
          }}
        >
          {/* 툴팁 화살표 */}
          <div 
            className="absolute -top-2 left-1/2 transform -translate-x-1/2 w-0 h-0"
            style={{
              borderLeft: '8px solid transparent',
              borderRight: '8px solid transparent',
              borderBottom: '8px solid #ffd700'
            }}
          />
          
          <div className="flex items-start gap-3">
            <div 
              className="p-2 rounded-full flex-shrink-0 mt-1"
              style={{
                background: 'linear-gradient(135deg, #ffd700 0%, #ffed4e 50%, #ffa000 100%)',
                boxShadow: '0 0 10px rgba(255, 215, 0, 0.4)'
              }}
            >
              <Crown className="w-4 h-4" style={{ color: '#8b5a00' }} />
            </div>
            <div className="flex-1">
              <h4 
                className="text-sm font-bold mb-1 flex items-center gap-2"
                style={{ color: '#ffd700' }}
              >
                {feature}
                <ProBadge variant="icon" />
              </h4>
              <p 
                className="text-xs mb-2 leading-relaxed"
                style={{ color: 'var(--starcraft-green)' }}
              >
                {description || '이 기능은 Pro 구독자만 사용할 수 있습니다.'}
              </p>
              <div className="flex items-center gap-2">
                <button
                  className="flex items-center gap-1.5 px-3 py-1.5 text-xs font-medium rounded transition-all duration-300 hover:scale-105"
                  style={{
                    background: 'linear-gradient(135deg, #ffd700 0%, #ffed4e 50%, #ffa000 100%)',
                    color: '#8b5a00',
                    boxShadow: '0 2px 8px rgba(255, 215, 0, 0.3)'
                  }}
                  onClick={(e) => {
                    e.stopPropagation();
                    // Pro 업그레이드 페이지로 이동
                    console.log('Pro 업그레이드 요청');
                  }}
                >
                  <Star className="w-3 h-3" />
                  Pro로 업그레이드
                  <Unlock className="w-3 h-3" />
                </button>
              </div>
            </div>
          </div>
        </div>
      )}

      {/* Pro 사용자용 특수 효과 */}
      {isPro && !disabled && (
        <div 
          className="absolute inset-0 rounded-lg pointer-events-none opacity-20 transition-all duration-1000 pro-shimmer-animation"
          style={{
            background: 'linear-gradient(45deg, transparent 30%, rgba(255, 215, 0, 0.1) 50%, transparent 70%)'
          }}
        />
      )}
    </div>
  );
}