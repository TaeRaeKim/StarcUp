import { useState } from "react";

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

interface PresetManagerProps {
  isOverlayActive: boolean;
  currentPresetIndex: number;
}

export function PresetManager({ isOverlayActive, currentPresetIndex }: PresetManagerProps) {
  const currentPreset = presets[currentPresetIndex];

  return (
    <div className="flex items-center justify-between w-full">
      {/* 좌측 프리셋 정보 */}
      <div className="flex flex-col">
        <div 
          className="text-sm transition-colors duration-300" 
          style={{ 
            color: isOverlayActive ? 'var(--starcraft-green)' : 'var(--starcraft-inactive-text)' 
          }}
        >
          {currentPreset.name}
        </div>
      </div>


    </div>
  );
}