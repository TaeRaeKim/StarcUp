import { ArrowLeft, Construction, Info, Clock } from 'lucide-react';

interface BuildOrderDetailSettingsProps {
  isOpen: boolean;
  onClose: () => void;
}

export function BuildOrderDetailSettings({ isOpen, onClose }: BuildOrderDetailSettingsProps) {
  if (!isOpen) return null;

  return (
    <div className="h-screen overflow-hidden border-2 shadow-2xl"
      style={{
        backgroundColor: 'var(--starcraft-bg)',
        background: 'linear-gradient(135deg, var(--starcraft-bg) 0%, rgba(0, 20, 0, 0.95) 100%)',
        borderColor: 'var(--starcraft-green)',
        boxShadow: '0 0 30px rgba(0, 255, 0, 0.4), inset 0 0 30px rgba(0, 255, 0, 0.1)'
      }}
    >
      {/* 전체 화면 컨테이너 */}
      <div 
        className="flex flex-col h-full"
        style={{
          backgroundColor: 'var(--starcraft-bg)'
        }}
      >
        {/* 헤더 */}
        <div 
          className="flex items-center justify-between p-4 border-b"
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
            
            <Construction 
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
                빌드 오더 설정
              </h1>
              <p 
                className="text-sm opacity-70"
                style={{ color: 'var(--starcraft-inactive-text)' }}
              >
                현재 개발 중인 기능입니다
              </p>
            </div>
          </div>
        </div>

        {/* 컨텐츠 - 스크롤 가능 */}
        <div className="flex-1 overflow-y-auto starcraft-scrollbar p-8">
          <div className="text-center space-y-6">
            {/* 아이콘과 메인 메시지 */}
            <div className="space-y-4">
              <div 
                className="mx-auto w-20 h-20 rounded-full border-4 border-dashed flex items-center justify-center"
                style={{ 
                  borderColor: 'var(--starcraft-inactive-border)',
                  backgroundColor: 'var(--starcraft-bg-secondary)'
                }}
              >
                <Construction 
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
                  빌드 오더 기능은 현재 개발 중입니다.
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
                <li className="flex items-start gap-3">
                  <span className="text-xs mt-1">•</span>
                  <div>
                    <strong>단계별 빌드 가이드</strong>
                    <br />
                    <span className="opacity-70">각 단계별 최적 빌드 순서 안내</span>
                  </div>
                </li>
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
                  <strong>예상 출시:</strong> 2025년 상반기 예정
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