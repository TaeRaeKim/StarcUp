import { useState, useEffect } from "react";

interface Preset {
  id: string;
  name: string;
  description: string;
  featureStates: boolean[];
}

interface FeatureStatusGridProps {
  isOverlayActive: boolean;
  gameStatus: 'playing' | 'waiting' | 'error';
  onPresetChange?: (preset: Preset) => void;
  currentPresetIndex?: number;
  onPresetIndexChange?: (index: number) => void;
  presets: Preset[]; // 외부에서 프리셋 데이터 전달받기 (필수)
}

export function FeatureStatusGrid({ 
  isOverlayActive, 
  gameStatus, 
  onPresetChange,
  currentPresetIndex = 0,
  onPresetIndexChange,
  presets
}: FeatureStatusGridProps) {
  // 외부 프리셋 사용 (항상 존재함이 보장됨)
  const activePresets = presets;
  
  // 항상 5개 점으로 고정
  const [featureStates, setFeatureStates] = useState(activePresets[0].featureStates);
  
  const currentPreset = activePresets[currentPresetIndex];

  // 프리셋 변경시에만 상태 업데이트 (중앙버튼 상태와 독립)
  useEffect(() => {
    setFeatureStates(currentPreset.featureStates);
  }, [currentPreset]);

  return (
    <div className="flex flex-col items-center justify-center py-4">
      {/* 고정 5x1 기능 상태 그리드 */}
      <div className="grid grid-cols-5 gap-3">
        {featureStates.map((isActive, index) => {
          const isWorker = index === 0; // 일꾼은 인덱스 0 (맨위로 이동)
          const isPopulation = index === 1; // 인구수는 인덱스 1
          const isUnit = index === 2; // 유닛은 인덱스 2
          const isUpgrade = index === 3; // 업그레이드는 인덱스 3
          const isBuildOrder = index === 4; // 빌드오더는 인덱스 4
          const isDisabled = isPopulation || isUnit || isUpgrade || isBuildOrder; // 일꾼만 활성화
          
          return (
          <div
            key={index}
            className={`
              w-3 h-3 rounded-full transition-all duration-500
              ${isDisabled 
                ? 'feature-dot-disabled' 
                : isActive 
                  ? 'feature-dot-active' 
                  : 'feature-dot-inactive'
              }
            `}
            style={{
              backgroundColor: isDisabled 
                ? 'var(--starcraft-inactive-border)' 
                : isActive 
                  ? '#ffffff' 
                  : 'var(--starcraft-inactive-secondary)',
              boxShadow: isDisabled 
                ? '0 0 2px rgba(128, 128, 128, 0.2)' 
                : isActive 
                  ? '0 0 8px rgba(255, 255, 255, 0.8), 0 0 4px rgba(255, 255, 255, 0.6)' 
                  : '0 0 2px rgba(64, 64, 64, 0.4)',
              opacity: isDisabled ? 0.4 : 1
            }}
          />
        );
        })}
      </div>
    </div>
  );
}