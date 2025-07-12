import { useState, useEffect } from 'react';
import { X, Save, RotateCcw, Settings, Cog, Shield, Bot, Star, Home, Zap, Building2 } from 'lucide-react';

interface PresetSettingsModalProps {
  isOpen: boolean;
  onClose: () => void;
  currentPreset: {
    id: string;
    name: string;
    description: string;
    featureStates: boolean[];
    selectedRace?: 'protoss' | 'terran' | 'zerg';
  };
  onSave: (updatedPreset: {
    id: string;
    name: string;
    description: string;
    featureStates: boolean[];
    selectedRace?: 'protoss' | 'terran' | 'zerg';
  }) => void;
  onRaceChange?: (race: 'protoss' | 'terran' | 'zerg') => void;
  onOpenPopulationSettings?: () => void;
  onOpenWorkerSettings?: () => void;
  onOpenUnitSettings?: () => void;
  onOpenUpgradeSettings?: () => void;
  onOpenBuildOrderSettings?: () => void;
}

// 기능 이름 및 설명 매핑
const FEATURE_CONFIG = [
  {
    name: "인구 수",
    description: "인구 수 출력, 부족 경고, 건물 기반 계산 등 인구 관리 도구"
  },
  {
    name: "일꾼",
    description: "일꾼 수 출력, 생산/사망 감지, 유휴 일꾼 체크 등 일꾼 관리 도구"
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

// 종족 정보
const RACES = {
  protoss: {
    name: '프로토스',
    color: '#FFD700',
    icon: Shield,
    description: '첨단 기술과 사이오닉 능력'
  },
  terran: {
    name: '테란',
    color: '#0099FF',
    icon: Home,
    description: '다재다능한 인간 문명'
  },
  zerg: {
    name: '저그',
    color: '#9932CC',
    icon: Building2,
    description: '진화와 적응의 외계 종족'
  }
} as const;

type RaceKey = keyof typeof RACES;

export function PresetSettingsModal({ 
  isOpen, 
  onClose, 
  currentPreset, 
  onSave,
  onRaceChange,
  onOpenPopulationSettings,
  onOpenWorkerSettings,
  onOpenUnitSettings,
  onOpenUpgradeSettings,
  onOpenBuildOrderSettings
}: PresetSettingsModalProps) {
  const [editedName, setEditedName] = useState(currentPreset.name);
  const [editedDescription, setEditedDescription] = useState(currentPreset.description);
  const [editedFeatures, setEditedFeatures] = useState(currentPreset.featureStates);
  const [selectedRace, setSelectedRace] = useState<RaceKey>(currentPreset.selectedRace || 'protoss');
  const [hasChanges, setHasChanges] = useState(false);

  // 프리셋이 변경될 때마다 초기화
  useEffect(() => {
    setEditedName(currentPreset.name);
    setEditedDescription(currentPreset.description);
    setEditedFeatures(currentPreset.featureStates);
    setSelectedRace(currentPreset.selectedRace || 'protoss');
    setHasChanges(false);
  }, [currentPreset]);

  // 변경사항 감지
  useEffect(() => {
    const nameChanged = editedName !== currentPreset.name;
    const descChanged = editedDescription !== currentPreset.description;
    const featuresChanged = editedFeatures.some((feature, index) => 
      feature !== currentPreset.featureStates[index]
    );
    const raceChanged = selectedRace !== (currentPreset.selectedRace || 'protoss');
    
    setHasChanges(nameChanged || descChanged || featuresChanged || raceChanged);
  }, [editedName, editedDescription, editedFeatures, selectedRace, currentPreset]);

  const handleFeatureToggle = (index: number) => {
    // 빌드오더(인덱스 4)는 비활성화 - 클릭 불가
    if (index === 4) {
      return;
    }
    
    const newFeatures = [...editedFeatures];
    newFeatures[index] = !newFeatures[index];
    setEditedFeatures(newFeatures);
  };

  const handleFeatureSettings = (index: number) => {
    const featureName = FEATURE_CONFIG[index].name;
    
    // 빌드오더는 미개발 기능이므로 설정창은 열되 기능 안내만 표시
    if (featureName === "빌드 오더" && onOpenBuildOrderSettings) {
      onOpenBuildOrderSettings();
      return;
    }
    
    if (featureName === "인구 수" && onOpenPopulationSettings) {
      onOpenPopulationSettings();
    } else if (featureName === "일꾼" && onOpenWorkerSettings) {
      onOpenWorkerSettings();
    } else if (featureName === "유닛" && onOpenUnitSettings) {
      onOpenUnitSettings();
    } else if (featureName === "업그레이드" && onOpenUpgradeSettings) {
      onOpenUpgradeSettings();
    } else {
      console.log(`${featureName} 상세 설정 열기`);
    }
  };

  const handleSave = () => {
    onSave({
      id: currentPreset.id,
      name: editedName,
      description: editedDescription,
      featureStates: editedFeatures,
      selectedRace: selectedRace
    });
    // 저장 시에는 초기화하지 않고 바로 닫기
    onClose();
  };

  const handleReset = () => {
    setEditedName(currentPreset.name);
    setEditedDescription(currentPreset.description);
    setEditedFeatures(currentPreset.featureStates);
    setSelectedRace(currentPreset.selectedRace || 'protoss');
  };

  const handleClose = () => {
    // 저장하지 않은 변경사항이 있으면 초기화
    if (hasChanges) {
      handleReset();
      console.log('프리셋 설정창 닫기: 저장하지 않은 변경사항 초기화됨');
    }
    onClose();
  };

  const enabledCount = editedFeatures.filter(f => f).length;

  if (!isOpen) return null;

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4">
      {/* 배경 오버레이 */}
      <div 
        className="absolute inset-0 backdrop-blur-sm cursor-pointer"
        style={{ backgroundColor: 'rgba(0, 0, 0, 0.7)' }}
        onClick={handleClose}
      />
      
      {/* 모달 컨테이너 */}
      <div 
        className="preset-settings-modal relative w-full max-w-2xl max-h-[90vh] overflow-hidden rounded-lg border-2 shadow-2xl"
        style={{
          backgroundColor: 'var(--starcraft-bg)',
          borderColor: 'var(--starcraft-green)',
          boxShadow: '0 0 20px rgba(0, 255, 0, 0.3), inset 0 0 20px rgba(0, 255, 0, 0.1)'
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

        {/* 스크롤 가능한 컨텐츠 */}
        <div className="overflow-y-auto max-h-[calc(90vh-140px)]">
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
                    value={editedName}
                    onChange={(e) => setEditedName(e.target.value)}
                    className="w-full p-3 rounded-sm border transition-all duration-300 focus:outline-none focus:ring-2"
                    style={{
                      backgroundColor: 'var(--starcraft-bg-secondary)',
                      borderColor: 'var(--starcraft-border)',
                      color: 'var(--starcraft-green)'
                    } as React.CSSProperties}
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
                    value={editedDescription}
                    onChange={(e) => setEditedDescription(e.target.value)}
                    rows={2}
                    className="w-full p-3 rounded-sm border transition-all duration-300 focus:outline-none focus:ring-2 resize-none"
                    style={{
                      backgroundColor: 'var(--starcraft-bg-secondary)',
                      borderColor: 'var(--starcraft-border)',
                      color: 'var(--starcraft-green)'
                    } as React.CSSProperties}
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
                {Object.entries(RACES).map(([raceKey, race]) => {
                  const IconComponent = race.icon;
                  const isSelected = selectedRace === raceKey;
                  return (
                    <button
                      key={raceKey}
                      onClick={() => {
                        setSelectedRace(raceKey as RaceKey);
                        // 종족 변경 시 즉시 부모 컴포넌트에 알림
                        if (onRaceChange) {
                          onRaceChange(raceKey as RaceKey);
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
                  const isBuildOrder = index === 4; // 빌드오더는 인덱스 4
                  const isDisabled = isBuildOrder; // 빌드오더만 비활성화
                  
                  return (
                  <div
                    key={index}
                    className={`flex items-center justify-between p-4 rounded-sm border transition-all duration-300 ${
                      isDisabled ? 'opacity-60' : 'hover:bg-opacity-50'
                    }`}
                    style={{
                      backgroundColor: isDisabled 
                        ? 'var(--starcraft-bg-secondary)' 
                        : editedFeatures[index] 
                          ? 'var(--starcraft-bg-active)' 
                          : 'var(--starcraft-bg-secondary)',
                      borderColor: isDisabled 
                        ? 'var(--starcraft-inactive-border)' 
                        : editedFeatures[index] 
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
                            : editedFeatures[index] 
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
                            : editedFeatures[index] 
                              ? 'var(--starcraft-green)' 
                              : 'var(--starcraft-inactive-text)' 
                        }}
                      >
                        {feature.description}
                      </div>
                    </div>
                    
                    <div className="flex items-center gap-3">
                      {/* 상세설정 버튼 */}
                      <button
                        onClick={() => handleFeatureSettings(index)}
                        className="feature-settings-btn p-2 rounded-sm transition-all duration-300"
                        style={{
                          color: isDisabled 
                            ? 'var(--starcraft-inactive-text)' 
                            : editedFeatures[index] 
                              ? 'var(--starcraft-green)' 
                              : 'var(--starcraft-inactive-text)',
                          backgroundColor: 'transparent'
                        }}
                        title={`${feature.name} 상세 설정`}
                      >
                        <Cog className="cog-icon w-4 h-4 transition-transform duration-300" />
                      </button>

                      {/* On/Off 토글 버튼 */}
                      <button
                        onClick={() => handleFeatureToggle(index)}
                        disabled={isDisabled}
                        className={`
                          w-12 h-6 rounded-full border-2 transition-all duration-300 relative
                          ${editedFeatures[index] ? 'justify-end' : 'justify-start'}
                          ${isDisabled ? 'cursor-not-allowed' : ''}
                        `}
                        style={{
                          backgroundColor: isDisabled 
                            ? 'var(--starcraft-inactive-bg)' 
                            : editedFeatures[index] 
                              ? 'var(--starcraft-green)' 
                              : 'var(--starcraft-inactive-bg)',
                          borderColor: isDisabled 
                            ? 'var(--starcraft-inactive-border)' 
                            : editedFeatures[index] 
                              ? 'var(--starcraft-green)' 
                              : 'var(--starcraft-inactive-border)'
                        }}
                        title={isDisabled 
                          ? `${feature.name} (개발 중 - 사용 불가)` 
                          : `${feature.name} ${editedFeatures[index] ? '비활성화' : '활성화'}`
                        }
                      >
                        <div
                          className={`
                            w-4 h-4 rounded-full transition-all duration-300 absolute top-0.5
                            ${editedFeatures[index] ? 'right-0.5' : 'left-0.5'}
                          `}
                          style={{
                            backgroundColor: isDisabled 
                              ? 'var(--starcraft-inactive-secondary)' 
                              : editedFeatures[index] 
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

        {/* 푸터 버튼들 */}
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