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
    featureStates: [true, true, false, true, true, false, true, false, true, false] // 10개로 통일
  },
  {
    id: "preset2", 
    name: "커공발-운영",
    description: "커세어 + 공중 발업 운영 빌드",
    featureStates: [true, false, true, true, false, true, false, true, false, true] // 10개로 통일
  },
  {
    id: "preset3",
    name: "패닼아비터",
    description: "패스트 다크템플러 + 아비터 전략",
    featureStates: [false, true, true, false, true, true, false, true, false, true] // 10개 유지
  }
];

interface FeatureStatusGridProps {
  isOverlayActive: boolean;
  gameStatus: 'playing' | 'waiting' | 'error';
  onPresetChange?: (preset: Preset) => void;
  currentPresetIndex?: number;
  onPresetIndexChange?: (index: number) => void;
}

export function FeatureStatusGrid({ 
  isOverlayActive, 
  gameStatus, 
  onPresetChange,
  currentPresetIndex = 0,
  onPresetIndexChange
}: FeatureStatusGridProps) {
  // 항상 10개 점으로 고정
  const [featureStates, setFeatureStates] = useState(presets[0].featureStates);
  
  const currentPreset = presets[currentPresetIndex];

  // 프리셋 변경시에만 상태 업데이트 (중앙버튼 상태와 독립)
  useEffect(() => {
    setFeatureStates(currentPreset.featureStates);
  }, [currentPreset]);

  return (
    <div className="flex flex-col items-center justify-center py-4">
      {/* 고정 5x2 기능 상태 그리드 */}
      <div className="grid grid-cols-5 gap-3">
        {featureStates.map((isActive, index) => (
          <div
            key={index}
            className={`
              w-3 h-3 rounded-full transition-all duration-500
              ${isActive 
                ? 'feature-dot-active' 
                : 'feature-dot-inactive'
              }
            `}
            style={{
              backgroundColor: isActive ? '#ffffff' : 'var(--starcraft-inactive-secondary)',
              boxShadow: isActive 
                ? '0 0 8px rgba(255, 255, 255, 0.8), 0 0 4px rgba(255, 255, 255, 0.6)' 
                : '0 0 2px rgba(64, 64, 64, 0.4)'
            }}
          />
        ))}
      </div>
    </div>
  );
}