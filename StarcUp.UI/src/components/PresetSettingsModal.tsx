import { useState, useEffect } from 'react';
import { X, Save, RotateCcw, Settings, Cog, Shield, Bot, Star, Home, Zap, Building2 } from 'lucide-react';
import { RaceType, RACE_NAMES } from '../types/enums';

interface PresetSettingsModalProps {
  isOpen: boolean;
  onClose: () => void;
  currentPreset: {
    id: string;
    name: string;
    description: string;
    featureStates: boolean[];
    selectedRace?: RaceType;
  };
  editingPresetData: {
    name: string;
    description: string;
    featureStates: boolean[];
    selectedRace: RaceType;
  } | null;
  onSave: () => void;
  onRaceChange?: (race: RaceType) => void;
  onEditingDataChange?: (updatedData: {
    name?: string;
    description?: string;
    featureStates?: boolean[];
    selectedRace?: RaceType;
  }) => void;
  onOpenPopulationSettings?: () => void;
  onOpenWorkerSettings?: () => void;
  onOpenUnitSettings?: () => void;
  onOpenUpgradeSettings?: () => void;
  onOpenBuildOrderSettings?: () => void;
  onOpenDevelopmentProgress?: (featureName: string, featureType: 'buildorder' | 'upgrade' | 'population' | 'unit') => void;
  detailChanges?: Record<number, boolean>;
  onReset?: () => void;
}

// 기능 이름 및 설명 매핑
const FEATURE_CONFIG = [
  {
    name: "일꾼",
    description: "일꾼 수 출력, 생산/사망 감지, 유휴 일꾼 체크 등 일꾼 관리 도구"
  },
  {
    name: "인구 수",
    description: "인구 수 출력, 부족 경고, 건물 기반 계산 등 인구 관리 도구"
  },
  {
    name: "유닛",
    description: "유닛 수 출력, 생산/사망 감지, 카테고리별 관리 등 유닛 관리 도구"
  },
  {
    name: "업그레이드",
    description: "업그레이드 진행 상황, 완료 알림, 카테고리별 관리 등 업그레이드 추적 도구"
  },
  {
    name: "빌드 오더",
    description: "단계별 빌드 가이드 및 타이밍 알림 (개발 중)"
  }
];

// 종족 정보 (enum 기반)
const RACES = {
  [RaceType.Protoss]: {
    name: RACE_NAMES[RaceType.Protoss],
    color: '#FFD700',
    icon: Shield,
    description: '첨단 기술과 사이오닉 능력'
  },
  [RaceType.Terran]: {
    name: RACE_NAMES[RaceType.Terran],
    color: '#0099FF',
    icon: Home,
    description: '다재다능한 인간 문명'
  },
  [RaceType.Zerg]: {
    name: RACE_NAMES[RaceType.Zerg],
    color: '#9932CC',
    icon: Building2,
    description: '진화와 적응의 외계 종족'
  }
} as const;

type RaceKey = RaceType;

export function PresetSettingsModal({ 
  isOpen, 
  onClose, 
  currentPreset, 
  editingPresetData,
  onSave,
  onRaceChange,
  onEditingDataChange,
  onOpenPopulationSettings,
  onOpenWorkerSettings,
  onOpenUnitSettings,
  onOpenUpgradeSettings,
  onOpenBuildOrderSettings,
  onOpenDevelopmentProgress,
  detailChanges = {},
  onReset
}: PresetSettingsModalProps) {
  // 편집 데이터가 없으면 현재 프리셋으로 초기화
  const editData = editingPresetData || {
    name: currentPreset.name,
    description: currentPreset.description,
    featureStates: [...currentPreset.featureStates],
    selectedRace: currentPreset.selectedRace ?? RaceType.Protoss
  };
  
  const [hasChanges, setHasChanges] = useState(false);

  // 변경사항 감지 (상세 설정 변경사항도 포함)
  useEffect(() => {
    const nameChanged = editData.name !== currentPreset.name;
    const descChanged = editData.description !== currentPreset.description;
    const featuresChanged = editData.featureStates.some((feature, index) => 
      feature !== currentPreset.featureStates[index]
    );
    const raceChanged = editData.selectedRace !== (currentPreset.selectedRace ?? RaceType.Protoss);
    const hasDetailChanges = Object.values(detailChanges).some(v => v === true);
    
    setHasChanges(nameChanged || descChanged || featuresChanged || raceChanged || hasDetailChanges);
  }, [editData, currentPreset, detailChanges]);

  const handleFeatureToggle = (index: number) => {
    // 유닛(2), 업그레이드(3), 빌드오더(4)는 비활성화 - 클릭 불가 (일꾼은 0번, 인구수는 1번으로 활성화)
    if (index === 2 || index === 3 || index === 4) {
      return;
    }
    
    const newFeatures = [...editData.featureStates];
    newFeatures[index] = !newFeatures[index];
    
    if (onEditingDataChange) {
      onEditingDataChange({ featureStates: newFeatures });
    }
  };

  const handleFeatureSettings = (index: number) => {
    const featureName = FEATURE_CONFIG[index].name;
    
    // 일꾼과 인구수 기능은 실제 상세 설정창으로 연결
    if (featureName === "일꾼" && onOpenWorkerSettings) {
      onOpenWorkerSettings();
    } else if (featureName === "인구 수" && onOpenPopulationSettings) {
      onOpenPopulationSettings();
    } else {
      // 나머지 기능들은 개발 중 페이지로 이동
      if (onOpenDevelopmentProgress) {
        let featureType: 'buildorder' | 'upgrade' | 'population' | 'unit';
        
        if (featureName === "유닛") {
          featureType = 'unit';
        } else if (featureName === "업그레이드") {
          featureType = 'upgrade';
        } else if (featureName === "빌드 오더") {
          featureType = 'buildorder';
        } else {
          featureType = 'buildorder'; // 기본값
        }
        
        onOpenDevelopmentProgress(featureName, featureType);
      }
    }
  };

  const handleSave = () => {
    onSave();
    onClose();
  };

  const handleReset = () => {
    if (onReset) {
      onReset();
    } else if (onEditingDataChange) {
      onEditingDataChange({
        name: currentPreset.name,
        description: currentPreset.description,
        featureStates: [...currentPreset.featureStates],
        selectedRace: currentPreset.selectedRace ?? RaceType.Protoss
      });
    }
  };

  const handleClose = () => {
    onClose();
  };

  const enabledCount = editData.featureStates.filter(f => f).length;

  if (!isOpen) return null;

  return (
    <div className="preset-settings-page w-full h-screen overflow-hidden border-2 shadow-2xl"
      style={{
        backgroundColor: 'var(--starcraft-bg)',
        background: 'linear-gradient(135deg, var(--starcraft-bg) 0%, rgba(0, 20, 0, 0.95) 100%)',
        borderColor: 'var(--starcraft-green)',
        boxShadow: '0 0 20px rgba(0, 255, 0, 0.3), inset 0 0 20px rgba(0, 255, 0, 0.1)'
      }}
    >
      {/* 전체 화면 컨테이너 */}
      <div 
        className="preset-settings-modal relative w-full h-full flex flex-col"
        style={{
          backgroundColor: 'var(--starcraft-bg)'
        }}
      >
        {/* 헤더 */}
        <div 
          className="flex items-center justify-between p-4 border-b draggable-titlebar"
          style={{ 
            backgroundColor: 'var(--starcraft-bg-secondary)',
            borderBottomColor: 'var(--starcraft-border)'
          }}
        >
          <div className="flex items-center gap-3">
            <Settings 
              className="w-6 h-6" 
              style={{ color: 'var(--starcraft-green)' }}
            />
            <div>
              <h2 
                className="font-semibold tracking-wide"
                style={{ 
                  color: 'var(--starcraft-green)',
                  textShadow: '0 0 8px rgba(0, 255, 0, 0.5)'
                }}
              >
                프리셋 설정
              </h2>
              <p 
                className="text-sm opacity-70"
                style={{ color: 'var(--starcraft-green)' }}
              >
                {currentPreset.name} 커스터마이징
              </p>
            </div>
          </div>
          
          <div className="flex items-center gap-2">
            {hasChanges && (
              <div 
                className="w-2 h-2 rounded-full animate-pulse"
                style={{ backgroundColor: 'var(--starcraft-yellow)' }}
                title="변경사항 있음"
              />
            )}
            <button
              onClick={handleClose}
              className="p-2 rounded-sm transition-all duration-300 hover:bg-red-500/20"
              style={{ color: 'var(--starcraft-red)' }}
            >
              <X className="w-5 h-5" />
            </button>
          </div>
        </div>

        {/* 스크롤 가능한 컨텐츠 영역 */}
        <div className="flex-1 overflow-y-auto starcraft-scrollbar">
          <div className="p-6 space-y-6">
          {/* 기본 정보 섹션 */}
          <div className="space-y-4">
              <h3 
                className="font-medium tracking-wide border-b pb-2"
                style={{ 
                  color: 'var(--starcraft-green)',
                  borderBottomColor: 'var(--starcraft-border)'
                }}
              >
                기본 정보
              </h3>
              
              <div className="space-y-3">
                <div>
                  <label 
                    className="block text-sm mb-1"
                    style={{ color: 'var(--starcraft-green)' }}
                  >
                    프리셋 이름
                  </label>
                  <input
                    type="text"
                    value={editData.name}
                    onChange={(e) => onEditingDataChange?.({ name: e.target.value })}
                    className="w-full p-3 rounded-sm border transition-all duration-300 focus:outline-none"
                    style={{
                      backgroundColor: 'var(--starcraft-bg-secondary)',
                      borderColor: 'var(--starcraft-border)',
                      color: 'var(--starcraft-green)',
                      outlineColor: 'var(--starcraft-green)'
                    }}
                    onFocus={(e) => {
                      e.target.style.boxShadow = '0 0 0 2px var(--starcraft-green)';
                    }}
                    onBlur={(e) => {
                      e.target.style.boxShadow = 'none';
                    }}
                    placeholder="프리셋 이름을 입력하세요"
                  />
                </div>

                <div>
                  <label 
                    className="block text-sm mb-1"
                    style={{ color: 'var(--starcraft-green)' }}
                  >
                    설명
                  </label>
                  <textarea
                    value={editData.description}
                    onChange={(e) => onEditingDataChange?.({ description: e.target.value })}
                    rows={2}
                    className="w-full p-3 rounded-sm border transition-all duration-300 focus:outline-none resize-none"
                    style={{
                      backgroundColor: 'var(--starcraft-bg-secondary)',
                      borderColor: 'var(--starcraft-border)',
                      color: 'var(--starcraft-green)',
                      outlineColor: 'var(--starcraft-green)'
                    }}
                    onFocus={(e) => {
                      e.target.style.boxShadow = '0 0 0 2px var(--starcraft-green)';
                    }}
                    onBlur={(e) => {
                      e.target.style.boxShadow = 'none';
                    }}
                    placeholder="프리셋에 대한 설명을 입력하세요"
                  />
                </div>
              </div>
            </div>

            {/* 종족 선택 섹션 */}
            <div className="space-y-4">
              <h3 
                className="font-medium tracking-wide border-b pb-2"
                style={{ 
                  color: 'var(--starcraft-green)',
                  borderBottomColor: 'var(--starcraft-border)'
                }}
              >
                종족 선택
              </h3>
              
              <div className="grid grid-cols-3 gap-3">
                {[RaceType.Protoss, RaceType.Terran, RaceType.Zerg].map((raceKey) => {
                  const race = RACES[raceKey];
                  const IconComponent = race.icon;
                  const isSelected = editData.selectedRace === raceKey;
                  return (
                    <button
                      key={raceKey}
                      onClick={() => {
                        onEditingDataChange?.({ selectedRace: raceKey });
                        // 종족 변경 시 즉시 부모 컴포넌트에 알림
                        if (onRaceChange) {
                          onRaceChange(raceKey);
                        }
                      }}
                      className={`p-4 rounded-lg border-2 transition-all duration-300 hover:bg-opacity-80 ${
                        isSelected ? 'border-current' : ''
                      }`}
                      style={{
                        backgroundColor: isSelected 
                          ? 'var(--starcraft-bg-active)' 
                          : 'var(--starcraft-bg-secondary)',
                        borderColor: isSelected 
                          ? race.color 
                          : 'var(--starcraft-border)',
                        boxShadow: isSelected 
                          ? `0 0 15px ${race.color}40` 
                          : 'none'
                      }}
                    >
                      <div className="flex flex-col items-center gap-2">
                        <div 
                          className="p-2 rounded-lg"
                          style={{ 
                            backgroundColor: isSelected 
                              ? race.color + '20' 
                              : 'var(--starcraft-bg)',
                            color: isSelected 
                              ? race.color 
                              : 'var(--starcraft-inactive-text)'
                          }}
                        >
                          <IconComponent className="w-6 h-6" />
                        </div>
                        <div 
                          className="font-semibold"
                          style={{ 
                            color: isSelected 
                              ? race.color 
                              : 'var(--starcraft-inactive-text)' 
                          }}
                        >
                          {race.name}
                        </div>
                        <div 
                          className="text-xs text-center opacity-80"
                          style={{ 
                            color: isSelected 
                              ? race.color 
                              : 'var(--starcraft-inactive-text)' 
                          }}
                        >
                          {race.description}
                        </div>
                      </div>
                    </button>
                  );
                })}
              </div>

            </div>

            {/* 기능 설정 섹션 */}
            <div className="space-y-4">
              <div className="flex items-center justify-between">
                <h3 
                  className="font-medium tracking-wide"
                  style={{ color: 'var(--starcraft-green)' }}
                >
                  기능 설정
                </h3>
                <div 
                  className="text-sm px-3 py-1 rounded-full border"
                  style={{
                    color: 'var(--starcraft-green)',
                    borderColor: 'var(--starcraft-border)',
                    backgroundColor: 'var(--starcraft-bg-secondary)'
                  }}
                >
                  {enabledCount}/{FEATURE_CONFIG.length} 활성화됨
                </div>
              </div>

              <div className="grid grid-cols-1 gap-3">
                {FEATURE_CONFIG.map((feature, index) => {
                  const isWorker = index === 0; // 일꾼은 인덱스 0 (맨위로 이동)
                  const isPopulation = index === 1; // 인구수는 인덱스 1
                  const isUnit = index === 2; // 유닛은 인덱스 2
                  const isUpgrade = index === 3; // 업그레이드는 인덱스 3
                  const isBuildOrder = index === 4; // 빌드오더는 인덱스 4
                  const isDisabled = isUnit || isUpgrade || isBuildOrder; // 일꾼과 인구수만 활성화
                  
                  return (
                  <div
                    key={index}
                    className={`flex items-center justify-between p-4 rounded-sm border transition-all duration-300 ${
                      isDisabled ? 'opacity-60' : 'hover:bg-opacity-50'
                    }`}
                    style={{
                      backgroundColor: isDisabled 
                        ? 'var(--starcraft-bg-secondary)' 
                        : editData.featureStates[index] 
                          ? 'var(--starcraft-bg-active)' 
                          : 'var(--starcraft-bg-secondary)',
                      borderColor: isDisabled 
                        ? 'var(--starcraft-inactive-border)' 
                        : editData.featureStates[index] 
                          ? 'var(--starcraft-green)' 
                          : 'var(--starcraft-border)'
                    }}
                  >
                    <div className="flex-1">
                      <div 
                        className="font-medium text-lg"
                        style={{ 
                          color: isDisabled 
                            ? 'var(--starcraft-inactive-text)' 
                            : editData.featureStates[index] 
                              ? 'var(--starcraft-green)' 
                              : 'var(--starcraft-inactive-text)' 
                        }}
                      >
                        {feature.name}
                      </div>
                      <div 
                        className="text-sm opacity-70 mt-1"
                        style={{ 
                          color: isDisabled 
                            ? 'var(--starcraft-inactive-text)' 
                            : editData.featureStates[index] 
                              ? 'var(--starcraft-green)' 
                              : 'var(--starcraft-inactive-text)' 
                        }}
                      >
                        {feature.description}
                      </div>
                    </div>
                    
                    <div className="flex items-center gap-3">
                      {/* 상세설정 버튼 및 변경사항 표시 */}
                      <div className="relative flex items-center">
                        {/* 변경사항 표시 - 해당 기능에 변경사항이 있을 때만 표시 */}
                        {detailChanges[index] && (
                          <div 
                            className="w-2 h-2 rounded-full animate-pulse mr-1"
                            style={{ backgroundColor: 'var(--starcraft-yellow)' }}
                            title="변경사항 있음"
                          />
                        )}
                        <button
                          onClick={() => handleFeatureSettings(index)}
                          className="feature-settings-btn p-2 rounded-sm transition-all duration-300"
                          style={{
                            color: isDisabled 
                              ? 'var(--starcraft-inactive-text)' 
                              : editData.featureStates[index] 
                                ? 'var(--starcraft-green)' 
                                : 'var(--starcraft-inactive-text)',
                            backgroundColor: 'transparent'
                          }}
                          title={`${feature.name} 상세 설정`}
                        >
                          <Cog className="cog-icon w-4 h-4 transition-transform duration-300" />
                        </button>
                      </div>

                      {/* On/Off 토글 버튼 */}
                      <button
                        onClick={() => handleFeatureToggle(index)}
                        disabled={isDisabled}
                        className={`
                          w-12 h-6 rounded-full border-2 transition-all duration-300 relative
                          ${editData.featureStates[index] ? 'justify-end' : 'justify-start'}
                          ${isDisabled ? 'cursor-not-allowed' : ''}
                        `}
                        style={{
                          backgroundColor: isDisabled 
                            ? 'var(--starcraft-inactive-bg)' 
                            : editData.featureStates[index] 
                              ? 'var(--starcraft-green)' 
                              : 'var(--starcraft-inactive-bg)',
                          borderColor: isDisabled 
                            ? 'var(--starcraft-inactive-border)' 
                            : editData.featureStates[index] 
                              ? 'var(--starcraft-green)' 
                              : 'var(--starcraft-inactive-border)'
                        }}
                        title={isDisabled 
                          ? `${feature.name} (개발 중 - 사용 불가)` 
                          : `${feature.name} ${editData.featureStates[index] ? '비활성화' : '활성화'}`
                        }
                      >
                        <div
                          className={`
                            w-4 h-4 rounded-full transition-all duration-300 absolute top-0.5
                            ${editData.featureStates[index] ? 'right-0.5' : 'left-0.5'}
                          `}
                          style={{
                            backgroundColor: isDisabled 
                              ? 'var(--starcraft-inactive-secondary)' 
                              : editData.featureStates[index] 
                                ? 'var(--starcraft-bg)' 
                                : 'var(--starcraft-inactive-secondary)'
                          }}
                        />
                      </button>
                    </div>
                  </div>
                );
                })}
              </div>
            </div>
          </div>
        </div>

        {/* 고정 푸터 */}
        <div 
          className="flex items-center justify-between p-4 border-t"
          style={{ 
            backgroundColor: 'var(--starcraft-bg-secondary)',
            borderTopColor: 'var(--starcraft-border)'
          }}
        >
          <button
            onClick={handleReset}
            disabled={!hasChanges}
            className={`
              flex items-center gap-2 px-4 py-2 rounded-sm transition-all duration-300
              ${hasChanges 
                ? 'hover:bg-yellow-500/20' 
                : 'opacity-50 cursor-not-allowed'
              }
            `}
            style={{ 
              color: hasChanges ? 'var(--starcraft-yellow)' : 'var(--starcraft-inactive-text)',
              borderColor: hasChanges ? 'var(--starcraft-yellow)' : 'var(--starcraft-inactive-border)'
            }}
          >
            <RotateCcw className="w-4 h-4" />
            초기화
          </button>

          <div className="flex gap-3">
            <button
              onClick={handleClose}
              className="px-6 py-2 rounded-sm border transition-all duration-300 hover:bg-red-500/20"
              style={{
                color: 'var(--starcraft-red)',
                borderColor: 'var(--starcraft-red)'
              }}
            >
              취소
            </button>
            
            <button
              onClick={handleSave}
              disabled={!hasChanges}
              className={`
                flex items-center gap-2 px-6 py-2 rounded-sm border transition-all duration-300
                ${hasChanges 
                  ? 'hover:bg-green-500/20' 
                  : 'opacity-50 cursor-not-allowed'
                }
              `}
              style={{
                color: hasChanges ? 'var(--starcraft-green)' : 'var(--starcraft-inactive-text)',
                borderColor: hasChanges ? 'var(--starcraft-green)' : 'var(--starcraft-inactive-border)',
                backgroundColor: hasChanges ? 'var(--starcraft-bg-active)' : 'transparent'
              }}
            >
              <Save className="w-4 h-4" />
              저장
            </button>
          </div>
        </div>
      </div>
    </div>
  );
}