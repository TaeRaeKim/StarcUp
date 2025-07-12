import { useState, useEffect } from "react";

interface Preset {
  id: string;
  name: string;
  description: string;
  featureStates: boolean[];
}

const presets: Preset[] = [
  {
    id: "preset1",
    name: "공발질-8겟뽕",
    description: "공중 발업 질럿 러쉬 + 8마리 겟뽕",
    featureStates: [true, true, true, false, false] // 5개: 인구수, 일꾼, 유닛, 업그레이드, 빌드오더(비활성화)
  },
  {
    id: "preset2", 
    name: "커공발-운영",
    description: "커세어 + 공중 발업 운영 빌드",
    featureStates: [true, true, false, true, false] // 5개: 인구수, 일꾼, 유닛, 업그레이드, 빌드오더(비활성화)
  },
  {
    id: "preset3",
    name: "패닼아비터",
    description: "패스트 다크템플러 + 아비터 전략",
    featureStates: [false, true, true, true, false] // 5개: 인구수, 일꾼, 유닛, 업그레이드, 빌드오더(비활성화)
  }
];

interface FeatureStatusGridProps {
  isOverlayActive: boolean;
  gameStatus: 'playing' | 'waiting' | 'error';
  onPresetChange?: (preset: Preset) => void;
  currentPresetIndex?: number;
  onPresetIndexChange?: (index: number) => void;
  presets?: Preset[]; // 외부에서 프리셋 데이터 전달받기
}

export function FeatureStatusGrid({ 
  isOverlayActive, 
  gameStatus, 
  onPresetChange,
  currentPresetIndex = 0,
  onPresetIndexChange,
  presets: externalPresets
}: FeatureStatusGridProps) {
  // 외부 프리셋이 있으면 사용, 없으면 내부 기본 프리셋 사용
  const activePresets = externalPresets || presets;
  
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
          const isBuildOrder = index === 4; // 빌드오더는 인덱스 4
          const isDisabled = isBuildOrder; // 빌드오더만 비활성화 표시
          
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