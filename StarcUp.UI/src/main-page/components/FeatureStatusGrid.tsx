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
  isPro?: boolean;
}

export function FeatureStatusGrid({ 
  isOverlayActive, 
  gameStatus, 
  onPresetChange,
  currentPresetIndex = 0,
  onPresetIndexChange,
  presets,
  isPro = false
}: FeatureStatusGridProps) {
  const currentPreset = presets[currentPresetIndex];
  
  // 항상 5개 점으로 고정 - 현재 선택된 프리셋으로 초기화
  const [featureStates, setFeatureStates] = useState(currentPreset?.featureStates || [false, false, false, false, false]);

  // 프리셋 변경시 및 featureStates 변경시 상태 업데이트
  useEffect(() => {
    setFeatureStates(currentPreset.featureStates);
  }, [currentPreset, currentPreset.featureStates]);

  // Pro 모드 무지개 색상 (빨강, 주황, 노랑, 초록, 파랑 순서)
  const rainbowColors = ['#ff4757', '#ff6348', '#ffa502', '#2ed573', '#3742fa'];

  return (
    <div className="flex flex-col items-center justify-center py-4">
      {/* 5개 점 일렬 배치 */}
      <div className="grid grid-cols-5 gap-3">
        {featureStates.map((isActive, index) => {
          // 색상 결정 로직 개선
          let backgroundColor, boxShadow, opacity;
          
          if (isPro) {
            // Pro 모드: 항상 무지개 색상 사용
            backgroundColor = rainbowColors[index];
            if (isActive) {
              // ON 상태: 발광 효과와 완전 불투명
              const color = rainbowColors[index];
              const r = parseInt(color.slice(1, 3), 16);
              const g = parseInt(color.slice(3, 5), 16);
              const b = parseInt(color.slice(5, 7), 16);
              boxShadow = `0 0 8px rgba(${r}, ${g}, ${b}, 0.8), 0 0 16px rgba(${r}, ${g}, ${b}, 0.4)`;
              opacity = 1;
            } else {
              // OFF 상태: 발광 없이 반투명
              boxShadow = 'none';
              opacity = 0.3;
            }
          } else {
            // Free 모드: 기존 로직 유지
            if (isActive) {
              backgroundColor = '#ffffff';
              boxShadow = '0 0 8px rgba(255, 255, 255, 0.6), 0 0 16px rgba(255, 255, 255, 0.3)';
              opacity = 1;
            } else {
              backgroundColor = '#666666';
              boxShadow = 'none';
              opacity = 0.4;
            }
          }
          
          return (
            <div
              key={index}
              className="w-3 h-3 rounded-full transition-all duration-300"
              style={{
                backgroundColor,
                boxShadow,
                opacity
              }}
            />
          );
        })}
      </div>
    </div>
  );
}