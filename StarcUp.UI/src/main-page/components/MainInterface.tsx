import { useState, useEffect } from "react";
import { ScrollingText } from "./ScrollingText";
import { FeatureStatusGrid } from "./FeatureStatusGrid";
import { Switch } from "./ui/switch";
import { SlidersHorizontal, Power, WifiOff, Clock, Zap, ChevronLeft, ChevronRight, Crown } from "lucide-react";
import { RaceType } from "../../types/game";

const starcraftTips = [
  "일꾼은 게임의 핵심! 항상 일꾼 생산을 우선하세요.",
  "미네랄과 가스의 비율을 2:1로 유지하는 것이 효율적입니다.",
  "정찰은 승리의 열쇠! 상대의 전략을 파악하세요.",
  "컨트롤 그룹(1~9)을 활용해 유닛을 빠르게 선택하세요.",
  "건물 배치는 방어와 효율성을 모두 고려해야 합니다.",
  "업그레이드는 유닛 수량보다 우선할 때가 많습니다.",
  "멀티 확장 타이밍이 경기의 흐름을 좌우합니다.",
  "상성을 고려한 유닛 조합이 승부의 관건입니다.",
  "자원 관리: 미네랄과 가스를 남기지 마세요!",
  "게임 센스: 상대의 패턴을 읽고 대응하세요."
];

// 게임 상태 타입 정의
type GameStatus = 'playing' | 'waiting' | 'error';

// 게임 상태별 정보 객체
const gameStatusInfo = {
  playing: {
    text: '게임 중',
    activeText: 'Active',
    color: 'var(--starcraft-green)',
    activeClass: 'starcraft-active-playing starcraft-glow-playing starcraft-bass-vibration',
    statusText: '&gt; OVERLAY ACTIVE &lt;',
    icon: Zap
  },
  waiting: {
    text: '대기 중',
    activeText: 'In-game waiting',
    color: 'var(--starcraft-yellow)',
    activeClass: 'starcraft-active-waiting starcraft-glow-waiting starcraft-gentle-wave',
    statusText: '&gt; OVERLAY ACTIVE &lt;',
    icon: Clock
  },
  error: {
    text: '게임 감지 안됨',
    activeText: 'Game not detected',
    color: 'var(--starcraft-red)',
    activeClass: 'starcraft-active-error starcraft-glow-error',
    statusText: '&gt; OVERLAY ACTIVE &lt;',
    icon: WifiOff
  }
};

interface MainInterfaceProps {
  presets: Array<{
    id: string;
    name: string;
    description: string;
    featureStates: boolean[];
    selectedRace?: RaceType;
  }>;
  currentPresetIndex: number;
  onPresetIndexChange: (index: number) => void;
  onOpenPresetSettings: () => void;
  isActive: boolean;
  gameStatus: GameStatus;
  onToggleOverlay: () => void;
  isPro?: boolean;
}

export function MainInterface({ 
  presets, 
  currentPresetIndex, 
  onPresetIndexChange, 
  onOpenPresetSettings,
  isActive,
  gameStatus,
  onToggleOverlay,
  isPro = false
}: MainInterfaceProps) {
  const handlePresetChange = (preset: any) => {
    // FeatureStatusGrid에서 프리셋 변경을 처리하므로 여기서는 비워둠
  };

  // 프리셋 네비게이션 핸들러
  const handlePrevPreset = () => {
    const newIndex = currentPresetIndex === 0 ? presets.length - 1 : currentPresetIndex - 1;
    onPresetIndexChange(newIndex);
  };

  const handleNextPreset = () => {
    const newIndex = currentPresetIndex === presets.length - 1 ? 0 : currentPresetIndex + 1;
    onPresetIndexChange(newIndex);
  };

  const currentStatusInfo = gameStatusInfo[gameStatus];
  const IconComponent = currentStatusInfo.icon;
  const currentPreset = presets[currentPresetIndex];

  return (
    <div 
      className="h-screen w-screen flex overflow-hidden relative" 
      style={{ 
        backgroundColor: 'var(--starcraft-bg)',
        border: '1px solid var(--main-container-border)',
      }}
    >
      
      {/* 메인 애플리케이션 컨테이너 - 전체 화면 채우기 */}
      <div 
        className="h-full w-full flex flex-col justify-between relative z-10">
        {/* 상단 타이틀 바 - 프리셋 네비게이션이 중앙에 위치 */}
        <div 
          className="flex items-center p-3 draggable-titlebar relative"
          style={{ 
            backgroundColor: 'var(--starcraft-bg-secondary)', 
            borderBottom: '1px solid var(--starcraft-border)'
          }}
        >
          {/* 중앙 프리셋 네비게이션 - 절대 중앙 위치 */}
          <div className="absolute left-1/2 top-1/2 transform -translate-x-1/2 -translate-y-1/2 flex items-center gap-3">
            <button
              onClick={handlePrevPreset}
              className="p-2 rounded-full transition-all duration-300 preset-nav-active hover:bg-[color:var(--starcraft-bg-secondary)]"
            >
              <ChevronLeft 
                className="w-4 h-4" 
                style={{ 
                  color: 'var(--starcraft-green)'
                }} 
              />
            </button>

            <div className="flex gap-1">
              {presets.map((_, index) => (
                <div
                  key={index}
                  className={`
                    w-2 h-2 rounded-full transition-all duration-300
                    ${index === currentPresetIndex 
                      ? 'preset-indicator-active' 
                      : 'preset-indicator-inactive'
                    }
                  `}
                  style={{
                    backgroundColor: index === currentPresetIndex 
                      ? 'var(--starcraft-green)'
                      : 'var(--starcraft-inactive-border)'
                  }}
                />
              ))}
            </div>

            <button
              onClick={handleNextPreset}
              className="p-2 rounded-full transition-all duration-300 preset-nav-active hover:bg-[color:var(--starcraft-bg-secondary)]"
            >
              <ChevronRight 
                className="w-4 h-4" 
                style={{ 
                  color: 'var(--starcraft-green)'
                }} 
              />
            </button>
          </div>

          {/* 우측 영역 - Pro 뱃지와 창 컨트롤 버튼들 */}
          <div className="flex items-center ml-auto">
            {/* Pro 뱃지 - isPro가 true일 때만 표시 */}
            {isPro && (
              <div className="flex items-center gap-1 px-2 py-1 rounded-full border border-yellow-500/30 bg-yellow-500/10 backdrop-blur-sm">
                <Crown 
                  className="w-3 h-3" 
                  style={{ 
                    color: '#fbbf24'
                  }} 
                />
                <span 
                  className="text-xs font-medium tracking-wide"
                  style={{ 
                    color: '#fbbf24',
                    textShadow: '0 0 4px rgba(251, 191, 36, 0.5)'
                  }}
                >
                  PRO
                </span>
              </div>
            )}
            
            {/* 시각적 구분선 (Pro 뱃지가 있을 때만) - Pro 뱃지와 버튼 사이 중앙에 위치 */}
            {isPro && (
              <div 
                className="h-4 w-px mx-4 opacity-60"
                style={{ backgroundColor: 'var(--starcraft-divider)' }}
              />
            )}
            
            {/* 창 컨트롤 버튼들 */}
            <div className="flex gap-1">
              <button 
                className="w-4 h-4 bg-yellow-500 rounded-full hover:bg-yellow-400 transition-colors"
                onClick={() => window.electronAPI?.minimizeWindow()}
              ></button>
              <button 
                className="w-4 h-4 bg-red-500 rounded-full hover:bg-red-400 transition-colors"
                onClick={() => window.electronAPI?.closeWindow()}
              ></button>
            </div>
          </div>
        </div>

        {/* 메인 컨텐츠 영역 */}
        <div className="flex-1 flex flex-col items-center justify-between px-6 py-4 relative">
          {/* 프리셋 이름과 설정 버튼이 함께 있는 상단 영역 */}
          <div className="w-full flex items-center justify-between mb-3">
            {/* 왼쪽 빈 공간 (균형을 위해) */}
            <div className="w-12"></div>
            
            {/* 중앙 프리셋 이름 */}
            <div className="flex-1 text-center">
              <h2 
                className="font-medium tracking-wide transition-all duration-300"
                style={{ 
                  color: 'var(--starcraft-green)',
                  textShadow: '0 0 8px rgba(0, 255, 0, 0.5)'
                }}
              >
                {currentPreset.name}
              </h2>
            </div>
            
            {/* 우측 설정 버튼 */}
            <div className="w-12 flex justify-end pr-2">
              <button 
                onClick={onOpenPresetSettings}
                className="p-3 rounded-sm transition-all duration-300 preset-settings-button-clean"
                onMouseDown={(e) => e.currentTarget.blur()}
              >
                <SlidersHorizontal 
                  className="w-5 h-5" 
                  style={{ 
                    color: 'var(--starcraft-green)'
                  }} 
                />
              </button>
            </div>
          </div>

          {/* 기능 상태 표시 그리드 */}
          <div className="mb-4">
            <FeatureStatusGrid 
              isOverlayActive={isActive}
              gameStatus={gameStatus}
              onPresetChange={handlePresetChange}
              currentPresetIndex={currentPresetIndex}
              onPresetIndexChange={onPresetIndexChange}
              presets={presets}
              isPro={isPro}
            />
          </div>
          
          {/* 메인 버튼 */}
          <div className="relative flex items-center justify-center my-8">
            <button
              onClick={onToggleOverlay}
              className={`
                w-84 h-84 rounded-full border-3 flex flex-col items-center justify-center
                transition-all duration-500 transform active:scale-95
                ${isActive ? currentStatusInfo.activeClass : 'starcraft-inactive'}
              `}
            >
              <div 
                className="mb-1 transition-colors duration-500 relative z-10"
                style={{ 
                  color: isActive ? currentStatusInfo.color : 'var(--starcraft-inactive-primary)' 
                }}
              >
                {isActive ? (
                  <IconComponent size={48} />
                ) : (
                  <Power size={48} />
                )}
              </div>
              <div 
                className="text-base text-center px-2 relative z-10 mt-2 transition-colors duration-500"
                style={{ 
                  color: isActive ? 'var(--starcraft-inactive-text)' : 'var(--starcraft-inactive-text)' 
                }}
              >
                {isActive ? currentStatusInfo.activeText : '모든 기능 비활성화됨'}
              </div>
            </button>
          </div>

          {/* OVERLAY 상태 표시 */}
          <div className="w-full text-center py-4 h-8 flex items-center justify-center mb-8">
            <div className="relative">
              <Switch
                checked={isActive}
                onCheckedChange={onToggleOverlay}
                className="starcraft-switch"
                style={{
                  '--switch-bg-active': currentStatusInfo.color,
                  '--switch-bg-inactive': 'var(--starcraft-inactive-border)',
                  '--switch-glow': isActive 
                    ? `0 0 8px ${currentStatusInfo.color}40`
                    : 'none'
                } as React.CSSProperties}
              />
            </div>
          </div>
        </div>

        {/* 하단 영역 - 뉴스 스타일 스크롤링 팁만 - 하단에 완전히 붙이기 */}
        <div className="w-full">
          <ScrollingText 
            tips={starcraftTips}
            className="w-full"
          />
        </div>

        {/* 전체 화면 활성화 효과 */}
        {isActive && (
          <div className="absolute inset-0 pointer-events-none z-0">
            <div 
              className="absolute inset-0 opacity-2"
              style={{ 
                backgroundColor: currentStatusInfo.color 
              }}
            ></div>
          </div>
        )}
      </div>
    </div>
  );
}