import { useState } from "react";

interface Preset {
  id: string;
  name: string;
  description: string;
  featureStates: boolean[];
}
interface PresetManagerProps {
  isOverlayActive: boolean;
  currentPresetIndex: number;
  presets: Preset[];
}

export function PresetManager({ isOverlayActive, currentPresetIndex, presets }: PresetManagerProps) {
  const currentPreset = presets[currentPresetIndex];

  return (
    <div className="flex items-center justify-between w-full">
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