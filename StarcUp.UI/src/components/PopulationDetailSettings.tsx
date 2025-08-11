import { useState, useEffect } from 'react';
import { ArrowLeft, Users, Building2, Clock, Settings2, Info, Plus, Minus, Shield, Bot, Star, Home, Cog, Zap } from 'lucide-react';
import { PopulationSettings, FixedModeSettings, BuildingModeSettings, TrackedBuilding, TimeLimitSettings } from '../utils/presetUtils';
import { RaceType, UnitType, RACE_BUILDINGS, UNIT_NAMES, RACE_NAMES } from '../types/enums';

interface PopulationDetailSettingsProps {
  isOpen: boolean;
  onClose: () => void;
  initialRace?: RaceType;
  currentPreset?: any;
  onSavePopulationSettings?: (presetId: string, populationSettings: PopulationSettings) => Promise<void>;
  tempPopulationSettings?: PopulationSettings | null;
  onTempSave?: (settings: PopulationSettings) => void;
}

// 종족 정보 (enum 기반)
const RACES = {
  [RaceType.Protoss]: {
    name: RACE_NAMES[RaceType.Protoss],
    color: '#FFD700',
    buildings: [
      { unitType: UnitType.ProtossGateway, name: UNIT_NAMES[UnitType.ProtossGateway], defaultMultiplier: 2, icon: Shield },
      { unitType: UnitType.ProtossRoboticsFacility, name: UNIT_NAMES[UnitType.ProtossRoboticsFacility], defaultMultiplier: 2, icon: Bot },
      { unitType: UnitType.ProtossStargate, name: UNIT_NAMES[UnitType.ProtossStargate], defaultMultiplier: 2, icon: Star }
    ]
  },
  [RaceType.Terran]: {
    name: RACE_NAMES[RaceType.Terran],
    color: '#0099FF',
    buildings: [
      { unitType: UnitType.TerranBarracks, name: UNIT_NAMES[UnitType.TerranBarracks], defaultMultiplier: 1, icon: Home },
      { unitType: UnitType.TerranFactory, name: UNIT_NAMES[UnitType.TerranFactory], defaultMultiplier: 2, icon: Cog },
      { unitType: UnitType.TerranStarport, name: UNIT_NAMES[UnitType.TerranStarport], defaultMultiplier: 2, icon: Zap }
    ]
  },
  [RaceType.Zerg]: {
    name: RACE_NAMES[RaceType.Zerg],
    color: '#9932CC',
    buildings: [
      { unitType: UnitType.ZergHatchery, name: UNIT_NAMES[UnitType.ZergHatchery], defaultMultiplier: 1, icon: Building2 }
    ]
  }
} as const;

type RaceKey = RaceType;
type BuildingSettings = Record<number, { enabled: boolean; multiplier: number }>;


export function PopulationDetailSettings({ 
  isOpen, 
  onClose, 
  initialRace, 
  currentPreset,
  onSavePopulationSettings,
  tempPopulationSettings,
  onTempSave
}: PopulationDetailSettingsProps) {
  // 초기 모드를 임시 설정이나 현재 프리셋 설정에서 가져오기
  const getInitialMode = (): 'building' | 'fixed' => {
    const settings = tempPopulationSettings || currentPreset?.populationSettings;
    return settings?.mode || 'fixed';
  };
  
  const [mode, setMode] = useState<'building' | 'fixed'>(getInitialMode);
  const [selectedRace, setSelectedRace] = useState<RaceKey>(initialRace || RaceType.Protoss);
  
  // 디버깅: 컴포넌트가 받는 props 확인
  console.log('PopulationDetailSettings props:', { isOpen, initialRace, selectedRace });
  
  // initialRace가 변경될 때마다 selectedRace 업데이트
  useEffect(() => {
    if (initialRace !== undefined) {
      const previousRace = selectedRace;
      setSelectedRace(initialRace);
      
      // 모드 B이고 종족이 변경될 때만 처리
      if (mode === 'building' && previousRace !== initialRace) {
        console.log(`🔄 종족 변경: ${RACES[previousRace]?.name || '없음'} → ${RACES[initialRace].name}`);
        
        // 임시 저장된 설정이 있고 해당 종족의 건물 설정이 있는 경우
        if (tempPopulationSettings?.buildingSettings?.race === initialRace) {
          // 같은 종족의 임시 설정 복원
          const buildingSettingsMap: BuildingSettings = {};
          tempPopulationSettings.buildingSettings.trackedBuildings.forEach((building: TrackedBuilding) => {
            buildingSettingsMap[building.buildingType] = {
              enabled: building.enabled,
              multiplier: building.multiplier
            };
          });
          setBuildingSettings(buildingSettingsMap);
          console.log(`✅ 종족 ${RACES[initialRace].name} 건물 설정 복원`);
        } else {
          // 다른 종족으로 변경 시 건물 설정 초기화
          setBuildingSettings({});
          console.log(`⚠️ 종족 ${RACES[initialRace].name} 건물 설정 초기화`);
        }
      }
    }
  }, [initialRace, mode, tempPopulationSettings]);

  // 초기 건물 설정을 임시 설정이나 현재 프리셋 설정에서 가져오기
  const getInitialBuildingSettings = (): BuildingSettings => {
    const settings = tempPopulationSettings || currentPreset?.populationSettings;
    if (settings?.mode === 'building' && settings.buildingSettings) {
      const buildingSettingsMap: BuildingSettings = {};
      settings.buildingSettings.trackedBuildings.forEach((building: any) => {
        buildingSettingsMap[building.buildingType] = {
          enabled: building.enabled,
          multiplier: building.multiplier
        };
      });
      return buildingSettingsMap;
    }
    return {};
  };

  const [buildingSettings, setBuildingSettings] = useState<BuildingSettings>(getInitialBuildingSettings);
  const [fixedValue, setFixedValue] = useState(() => {
    const settings = tempPopulationSettings || currentPreset?.populationSettings;
    return settings?.fixedSettings?.thresholdValue || 4;
  });
  const [timeLimitMinutes, setTimeLimitMinutes] = useState(() => {
    const settings = tempPopulationSettings || currentPreset?.populationSettings;
    return settings?.fixedSettings?.timeLimit?.minutes || 3;
  });
  const [timeLimitSeconds, setTimeLimitSeconds] = useState(() => {
    const settings = tempPopulationSettings || currentPreset?.populationSettings;
    return settings?.fixedSettings?.timeLimit?.seconds || 0;
  });
  const [isTimeLimitEnabled, setIsTimeLimitEnabled] = useState(() => {
    const settings = tempPopulationSettings || currentPreset?.populationSettings;
    return settings?.fixedSettings?.timeLimit?.enabled ?? true;
  });
  
  // 변경사항 감지 상태
  const [hasChanges, setHasChanges] = useState(false);

  // 현재 프리셋에서 인구수 설정 로드 (임시 저장값 우선) - 종족 변경 시에만 처리
  useEffect(() => {
    // 종족 변경 시에만 건물 설정 업데이트 (모드 B인 경우만)
    if (mode === 'building' && initialRace !== undefined) {
      const settings = tempPopulationSettings || currentPreset?.populationSettings;
      
      if (settings?.mode === 'building' && settings.buildingSettings) {
        console.log(`🎯 종족 변경: initialRace ${initialRace} vs settings.race ${settings.buildingSettings.race}`);
        
        if (initialRace === settings.buildingSettings.race) {
          // 같은 종족이면 건물 설정 로드
          const buildingSettingsMap: BuildingSettings = {};
          settings.buildingSettings.trackedBuildings.forEach((building: any) => {
            buildingSettingsMap[building.buildingType] = {
              enabled: building.enabled,
              multiplier: building.multiplier
            };
          });
          setBuildingSettings(buildingSettingsMap);
          console.log(`✅ 종족 ${RACES[initialRace].name} 건물 설정 로드`);
        } else {
          // 다른 종족이면 건물 설정 비우기
          setBuildingSettings({});
          console.log(`⚠️ 종족 불일치 - 건물 설정 초기화`);
        }
      }
    }
  }, [initialRace]);

  // 변경사항 감지 - 원본 프리셋 설정과 현재 설정 비교
  useEffect(() => {
    const originalSettings = currentPreset?.populationSettings;
    
    // 현재 설정을 PopulationSettings 형식으로 구성
    const currentSettings: PopulationSettings = {
      mode,
      ...(mode === 'fixed' && {
        fixedSettings: {
          thresholdValue: fixedValue,
          ...(isTimeLimitEnabled && {
            timeLimit: {
              enabled: isTimeLimitEnabled,
              minutes: timeLimitMinutes,
              seconds: timeLimitSeconds
            }
          })
        }
      }),
      ...(mode === 'building' && {
        buildingSettings: {
          race: selectedRace,
          trackedBuildings: Object.entries(buildingSettings).map(([unitTypeStr, settings]) => {
            const unitType = parseInt(unitTypeStr) as UnitType;
            return {
              buildingType: unitType,
              multiplier: settings.multiplier,
              enabled: settings.enabled
            };
          })
        }
      })
    };

    // 설정 비교 함수
    const isEqual = (original: PopulationSettings | undefined, current: PopulationSettings): boolean => {
      if (!original) return false;
      
      // 모드 비교
      if (original.mode !== current.mode) return false;
      
      // 모드 A 비교
      if (current.mode === 'fixed') {
        const origFixed = original.fixedSettings;
        const currFixed = current.fixedSettings;
        if (!origFixed && !currFixed) return true;
        if (!origFixed || !currFixed) return false;
        
        if (origFixed.thresholdValue !== currFixed.thresholdValue) return false;
        
        // 시간 제한 비교
        const origTime = origFixed.timeLimit;
        const currTime = currFixed.timeLimit;
        if (!origTime && !currTime) return true;
        if (!origTime || !currTime) return false;
        
        return origTime.enabled === currTime.enabled && 
               origTime.minutes === currTime.minutes && 
               origTime.seconds === currTime.seconds;
      }
      
      // 모드 B 비교
      if (current.mode === 'building') {
        const origBuilding = original.buildingSettings;
        const currBuilding = current.buildingSettings;
        if (!origBuilding && !currBuilding) return true;
        if (!origBuilding || !currBuilding) return false;
        
        if (origBuilding.race !== currBuilding.race) return false;
        
        // 건물 설정 비교
        return JSON.stringify(origBuilding.trackedBuildings) === JSON.stringify(currBuilding.trackedBuildings);
      }
      
      return true;
    };

    const hasAnyChanges = !isEqual(originalSettings, currentSettings);
    setHasChanges(hasAnyChanges);
  }, [
    mode,
    fixedValue,
    timeLimitMinutes,
    timeLimitSeconds,
    isTimeLimitEnabled,
    selectedRace,
    buildingSettings,
    currentPreset?.populationSettings
  ]);

  // 현재 활성화된 건물 개수 계산
  const getEnabledBuildingsCount = () => {
    return Object.values(buildingSettings).filter(setting => setting.enabled).length;
  };

  // 건물 설정 업데이트
  const toggleBuildingEnabled = (unitType: UnitType) => {
    setBuildingSettings(prev => {
      const current = prev[unitType];
      const isCurrentlyEnabled = current?.enabled || false;
      
      // 모드 B에서 마지막 활성화된 건물을 비활성화하려고 할 때 방지
      if (mode === 'building' && isCurrentlyEnabled && getEnabledBuildingsCount() === 1) {
        console.warn('⚠️ 모드 B에서는 최소 1개 건물이 활성화되어야 합니다.');
        return prev; // 변경 없이 현재 상태 유지
      }
      
      return {
        ...prev,
        [unitType]: {
          enabled: !isCurrentlyEnabled,
          multiplier: current?.multiplier || RACES[selectedRace].buildings.find(b => b.unitType === unitType)?.defaultMultiplier || 1
        }
      };
    });
  };

  const updateBuildingMultiplier = (unitType: UnitType, delta: number) => {
    setBuildingSettings(prev => {
      const current = prev[unitType];
      return {
        ...prev,
        [unitType]: {
          enabled: current?.enabled || false,
          multiplier: Math.max(1, Math.min(10, (current?.multiplier || 1) + delta))
        }
      };
    });
  };

  // 초기 건물 설정 생성
  const getBuildingConfig = (unitType: UnitType, defaultMultiplier: number) => {
    return buildingSettings[unitType] || { enabled: false, multiplier: defaultMultiplier };
  };

  // 총 경고 기준값 계산 (모드 A) - 프로그램이 자동으로 건물 개수 추적
  const calculateTotalThreshold = () => {
    const race = RACES[selectedRace];
    return race.buildings.reduce((total, building) => {
      const config = getBuildingConfig(building.unitType, building.defaultMultiplier);
      // 선택된 건물만 계산 (실제로는 프로그램이 해당 건물 개수를 자동 추적)
      return config.enabled ? total + config.multiplier : total;
    }, 0);
  };

  const handleConfirm = async () => {
    try {
      // 모드 B 유효성 검사: 최소 1개 건물 활성화 필수
      if (mode === 'building' && getEnabledBuildingsCount() === 0) {
        console.error('❌ 모드 B에서는 최소 1개 건물이 활성화되어야 합니다.');
        alert('모드 B에서는 최소 1개 건물이 활성화되어야 합니다.');
        return;
      }

      // 현재 UI 상태를 PopulationSettings 형식으로 변환
      const populationSettings: PopulationSettings = {
        mode,
        ...(mode === 'fixed' && {
          fixedSettings: {
            thresholdValue: fixedValue,
            ...(isTimeLimitEnabled && {
              timeLimit: {
                enabled: isTimeLimitEnabled,
                minutes: timeLimitMinutes,
                seconds: timeLimitSeconds
              }
            })
          }
        }),
        ...(mode === 'building' && {
          buildingSettings: {
            race: selectedRace, // RaceType enum 값 (int)
            trackedBuildings: Object.entries(buildingSettings).map(([unitTypeStr, settings]) => {
              const unitType = parseInt(unitTypeStr) as UnitType;
              return {
                buildingType: unitType, // UnitType enum 값 (int)
                multiplier: settings.multiplier,
                enabled: settings.enabled
              };
            })
          }
        })
      };
      
      console.log('🏘️ 인구수 설정 임시 저장:', populationSettings);
      
      // 임시 저장 함수가 있으면 임시 저장만 수행
      if (onTempSave) {
        onTempSave(populationSettings);
      } else if (currentPreset && onSavePopulationSettings) {
        // 임시 저장 함수가 없으면 기존처럼 직접 저장
        await onSavePopulationSettings(currentPreset.id, populationSettings);
        console.log('✅ 인구수 설정 저장 완료');
      }
      
      onClose();
    } catch (error) {
      console.error('❌ 인구수 설정 저장 실패:', error);
    }
  };

  if (!isOpen) return null;

  return (
    <div className="h-screen overflow-hidden border-2 shadow-2xl"
      style={{
        backgroundColor: 'var(--starcraft-bg)',
        border: '1px solid var(--main-container-border)',
      }}
    >
      {/* 전체 화면 컨테이너 */}
      <div 
        className="flex flex-col h-full">
        {/* 헤더 */}
        <div 
          className="flex items-center justify-between p-4 border-b draggable-titlebar"
          style={{ 
            backgroundColor: 'var(--starcraft-bg-secondary)',
            borderBottomColor: 'var(--starcraft-border)'
          }}
        >
          <div className="flex items-center gap-3">
            <button
              onClick={onClose}
              className="p-2 rounded-sm transition-all duration-300 hover:bg-green-500/20"
              style={{ color: 'var(--starcraft-green)' }}
            >
              <ArrowLeft className="w-5 h-5" />
            </button>
            
            <Users 
              className="w-6 h-6" 
              style={{ color: 'var(--starcraft-green)' }}
            />
            <div>
              <h1 
                className="text-xl font-semibold tracking-wide"
                style={{ 
                  color: 'var(--starcraft-green)',
                  textShadow: '0 0 8px rgba(0, 255, 0, 0.5)'
                }}
              >
                인구 수 설정
              </h1>
              <p 
                className="text-sm opacity-70"
                style={{ color: 'var(--starcraft-green)' }}
              >
                인구 수 경고 알림 방식을 설정하세요
              </p>
            </div>
          </div>
        </div>

        {/* 컨텐츠 - 스크롤 가능 */}
        <div className="flex-1 overflow-y-auto starcraft-scrollbar p-6 space-y-8">
            
            {/* 모드 선택 */}
            <div className="space-y-4">
              <h2 
                className="text-lg font-medium tracking-wide flex items-center gap-2"
                style={{ color: 'var(--starcraft-info)' }}
              >
                <Settings2 className="w-5 h-5" />
                경고 모드 선택
              </h2>
              
              <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                {/* 모드 A: 고정값 기반 */}
                <div
                  className={`p-4 rounded-lg border-2 cursor-pointer transition-all duration-300 ${
                    mode === 'fixed' ? 'border-current' : ''
                  }`}
                  style={{
                    backgroundColor: mode === 'fixed' 
                          ? 'var(--starcraft-inactive-bg)'
                          : 'var(--starcraft-bg-secondary)',
                    borderColor: mode === 'fixed' 
                          ? 'var(--starcraft-red-bright)' 
                          : 'var(--starcraft-red)',
                  }}
                  onClick={() => setMode('fixed')}
                >
                  <div className="flex items-center gap-3 mb-3">
                    <Clock 
                      className="w-6 h-6" 
                      style={{ color: 'var(--starcraft-green)' }}
                    />
                    <h3 
                      className="font-semibold"
                      style={{ color: 'var(--starcraft-green)' }}
                    >
                      모드 A: 고정값 기반
                    </h3>
                  </div>
                  <p 
                    className="text-sm opacity-80 mb-2"
                    style={{ color: 'var(--starcraft-green)' }}
                  >
                    단순히 고정된 숫자로 경고 기준 설정
                  </p>
                  <div 
                    className="text-xs p-2 rounded bg-black/20"
                    style={{ color: 'var(--starcraft-green)' }}
                  >
                    초보자용 간단 설정<br/>
                    예시: 항상 4명 여유분 유지
                  </div>
                </div>

                {/* 모드 B: 생산 건물 기반 */}
                <div
                  className={`p-4 rounded-lg border-2 cursor-pointer transition-all duration-300 ${
                    mode === 'building' ? 'border-current' : ''
                  }`}
                  style={{
                    backgroundColor: mode === 'building' 
                          ? 'var(--starcraft-inactive-bg)'
                          : 'var(--starcraft-bg-secondary)',
                    borderColor: mode === 'building' 
                          ? 'var(--starcraft-red-bright)' 
                          : 'var(--starcraft-red)'
                  }}
                  onClick={() => {
                    setMode('building');
                    // 모드 B로 전환 시 활성화된 건물이 없으면 첫 번째 건물 자동 활성화
                    if (getEnabledBuildingsCount() === 0) {
                      const firstBuilding = RACES[selectedRace].buildings[0];
                      if (firstBuilding) {
                        setBuildingSettings(prev => ({
                          ...prev,
                          [firstBuilding.unitType]: {
                            enabled: true,
                            multiplier: firstBuilding.defaultMultiplier
                          }
                        }));
                        console.log(`🏗️ 모드 B 전환 시 ${firstBuilding.name} 자동 활성화`);
                      }
                    }
                  }}
                >
                  <div className="flex items-center gap-3 mb-3">
                    <Building2 
                      className="w-6 h-6" 
                      style={{ color: 'var(--starcraft-green)' }}
                    />
                    <h3 
                      className="font-semibold"
                      style={{ color: 'var(--starcraft-green)' }}
                    >
                      모드 B: 생산 건물 기반
                    </h3>
                  </div>
                  <p 
                    className="text-sm opacity-80 mb-2"
                    style={{ color: 'var(--starcraft-green)' }}
                  >
                    추적할 건물을 선택하고 배수를 설정하세요
                  </p>
                  <p 
                    className="text-xs opacity-70 text-yellow-400"
                  >
                    ⚠️ 최소 1개 건물은 반드시 활성화되어야 합니다
                  </p>
                  <div 
                    className="text-xs p-2 rounded bg-black/20"
                    style={{ color: 'var(--starcraft-green)' }}
                  >
                    예시: 배럭(×1) + 팩토리(×2) 선택시<br/>
                    → (건물개수 × 배수)의 합이 경고 기준값
                  </div>
                </div>
              </div>
            </div>

            {/* 모드별 설정 UI */}
            {mode === 'building' && (
              <div className="space-y-6">
                {/* 종족 안내 정보 */}
                <div 
                  className="p-3 rounded-lg border"
                  style={{
                    backgroundColor: 'var(--starcraft-inactive-bg)',
                    borderColor: 'var(--starcraft-red )',
                    color: RACES[selectedRace].color
                  }}
                >
                  <div className="flex items-center gap-2 text-sm">
                    <Info className="w-4 h-4" />
                    <span><strong>{RACES[selectedRace].name}</strong> 종족이 프리셋 설정에서 선택되었습니다.</span>
                  </div>
                  <div className="text-xs mt-1 opacity-70">
                    {RACES[selectedRace].buildings.length}개의 생산 건물을 추적할 수 있습니다.
                  </div>
                </div>

                {/* 건물 선택 및 배수 설정 */}
                <div className="space-y-4">
                  <div className="flex items-center justify-between">
                    <div className="flex items-center gap-3">
                      <h3 
                        className="font-medium tracking-wide"
                        style={{ color: 'var(--starcraft-info)' }}
                      >
                        추적할 생산 건물
                      </h3>
                      <div 
                        className="px-3 py-1 rounded-full border-2 flex items-center gap-2"
                        style={{
                          color: RACES[selectedRace].color,
                          borderColor: RACES[selectedRace].color,
                          backgroundColor: 'var(--starcraft-inactive-bg)',
                          boxShadow: `0 0 8px ${RACES[selectedRace].color}30`
                        }}
                      >
                        <span className="text-sm font-semibold">{RACES[selectedRace].name}</span>
                      </div>
                    </div>
                    <div 
                      className="text-sm px-3 py-1 rounded-full border"
                      style={{
                        color: 'var(--starcraft-green)',
                        borderColor: 'var(--starcraft-red)',
                        backgroundColor: 'var(--starcraft-bg-secondary)'
                      }}
                    >
                      선택됨: {RACES[selectedRace].buildings.filter(building => 
                        getBuildingConfig(building.unitType, building.defaultMultiplier).enabled
                      ).length}/{RACES[selectedRace].buildings.length}
                    </div>
                  </div>

                  <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
                    {RACES[selectedRace].buildings.map((building) => {
                      const config = getBuildingConfig(building.unitType, building.defaultMultiplier);
                      const IconComponent = building.icon;
                      return (
                        <div
                          key={building.unitType}
                          className={`p-4 rounded-lg border-2 transition-all duration-300 ${
                            config.enabled ? 'border-current' : ''
                          }`}
                          style={{
                            backgroundColor: config.enabled 
                          ? 'var(--starcraft-inactive-bg)'
                          : 'var(--starcraft-bg-secondary)',
                            borderColor: config.enabled 
                              ? RACES[selectedRace].color 
                              : 'var(--starcraft-border)',
                            boxShadow: config.enabled 
                              ? `0 0 10px ${RACES[selectedRace].color}40` 
                              : 'none'
                          }}
                        >
                          {/* 건물 선택 헤더 */}
                          <div className="flex items-center gap-3 mb-4">
                            <div 
                              className="p-2 rounded-lg"
                              style={{ 
                                backgroundColor: config.enabled 
                                  ? RACES[selectedRace].color + '20' 
                                  : 'var(--starcraft-bg)',
                                color: config.enabled 
                                  ? RACES[selectedRace].color 
                                  : 'var(--starcraft-inactive-text)'
                              }}
                            >
                              <IconComponent className="w-6 h-6" />
                            </div>
                            <div className="flex-1">
                              <h4 
                                className="font-semibold"
                                style={{ 
                                  color: config.enabled 
                                    ? RACES[selectedRace].color 
                                    : 'var(--starcraft-inactive-text)' 
                                }}
                              >
                                {building.name}
                              </h4>

                            </div>
                            
                            {/* 체크박스 */}
                            <label className={`flex items-center ${
                              // 마지막 활성화된 건물일 때 커서 변경
                              config.enabled && getEnabledBuildingsCount() === 1 
                                ? 'cursor-not-allowed' 
                                : 'cursor-pointer'
                            }`}>
                              <input
                                type="checkbox"
                                checked={config.enabled}
                                onChange={() => toggleBuildingEnabled(building.unitType)}
                                className="sr-only"
                                disabled={config.enabled && getEnabledBuildingsCount() === 1}
                              />
                              <div
                                className={`
                                  w-6 h-6 rounded border-2 transition-all duration-300 flex items-center justify-center
                                  ${config.enabled ? 'border-current' : ''}
                                  ${config.enabled && getEnabledBuildingsCount() === 1 ? 'opacity-70' : ''}
                                `}
                                style={{
                                  backgroundColor: config.enabled 
                                    ? RACES[selectedRace].color 
                                    : 'transparent',
                                  borderColor: config.enabled 
                                    ? RACES[selectedRace].color 
                                    : 'var(--starcraft-border)'
                                }}
                                title={
                                  config.enabled && getEnabledBuildingsCount() === 1 
                                    ? '모드 B에서는 최소 1개 건물이 활성화되어야 합니다.' 
                                    : undefined
                                }
                              >
                                {config.enabled && (
                                  <div 
                                    className="w-3 h-3 rounded-sm"
                                    style={{ backgroundColor: 'var(--starcraft-bg)' }}
                                  />
                                )}
                              </div>
                            </label>
                          </div>

                          {/* 배수 설정 */}
                          {config.enabled && (
                            <div>
                              <label 
                                className="block text-sm mb-2"
                                style={{ color: RACES[selectedRace].color }}
                              >
                                배수 설정
                              </label>
                              <div className="flex items-center gap-2">
                                <button
                                  onClick={() => updateBuildingMultiplier(building.unitType, -1)}
                                  className="p-2 rounded transition-all duration-300 hover:bg-red-500/20"
                                  style={{ color: 'var(--starcraft-red)' }}
                                  disabled={config.multiplier <= 1}
                                >
                                  <Minus className="w-4 h-4" />
                                </button>
                                <div 
                                  className="flex-1 text-center py-3 px-4 font-semibold rounded border-2"
                                  style={{
                                    backgroundColor: 'var(--starcraft-bg)',
                                    borderColor: RACES[selectedRace].color,
                                    color: RACES[selectedRace].color
                                  }}
                                >
                                  ×{config.multiplier}
                                </div>
                                <button
                                  onClick={() => updateBuildingMultiplier(building.unitType, 1)}
                                  className="p-2 rounded transition-all duration-300 hover:bg-green-500/20"
                                  style={{ color: 'var(--starcraft-green)' }}
                                  disabled={config.multiplier >= 10}
                                >
                                  <Plus className="w-4 h-4" />
                                </button>
                              </div>
                              <div 
                                className="text-xs mt-2 opacity-70"
                                style={{ color: RACES[selectedRace].color }}
                              >
                                각 {building.name}당 {config.multiplier}의 경고값 기여
                              </div>
                            </div>
                          )}
                        </div>
                      );
                    })}
                  </div>

                  {/* 계산 공식 설명 */}
                  <div 
                    className="p-3 rounded-lg border"
                    style={{
                      backgroundColor: 'var(--starcraft-bg-active)',
                      borderColor: 'var(--starcraft-green)'
                    }}
                  >
                    <div className="flex items-center gap-2 mb-2">
                      <Info className="w-4 h-4" style={{ color: 'var(--starcraft-green)' }} />
                      <span 
                        className="text-sm font-medium"
                        style={{ color: 'var(--starcraft-green)' }}
                      >
                        계산 공식
                      </span>
                    </div>
                    <div 
                      className="text-xs opacity-90"
                      style={{ color: 'var(--starcraft-green)' }}
                    >
                      선택된 각 건물: (자동 추적된 건물 개수) × (설정된 배수)<br/>
                      최종 경고 기준: 모든 선택된 건물의 합계
                    </div>
                  </div>
                </div>
              </div>
            )}

            {mode === 'fixed' && (
              <div className="grid grid-cols-1 lg:grid-cols-2 gap-6 items-stretch">
                {/* 고정값 설정 */}
                <div className="flex flex-col h-full">
                  <h3 
                    className="font-medium tracking-wide mb-4"
                    style={{ color: 'var(--starcraft-info)' }}
                  >
                    경고 기준값 설정
                  </h3>
                  
                  <div className="flex-1 p-4 rounded-lg border"
                    style={{
                      backgroundColor: 'var(--starcraft-bg-secondary)',
                      borderColor: 'var(--starcraft-red)'
                    }}
                  >
                    <label 
                      className="block text-sm mb-3"
                      style={{ color: 'var(--starcraft-green)' }}
                    >
                      여유 인구 수 (최대인구 - 현재인구 ≤ 이 값일 때 경고)
                    </label>
                    <div className="flex items-center gap-3">
                      <button
                        onClick={() => setFixedValue(Math.max(1, fixedValue - 1))}
                        className="p-2 rounded transition-all duration-300 hover:bg-red-500/20"
                        style={{ color: 'var(--starcraft-red)' }}
                        disabled={fixedValue <= 1}
                      >
                        <Minus className="w-5 h-5" />
                      </button>
                      <input
                        type="number"
                        min="1"
                        max="50"
                        value={fixedValue}
                        onChange={(e) => {
                          const value = parseInt(e.target.value) || 1;
                          setFixedValue(Math.max(1, Math.min(50, value)));
                        }}
                        className="flex-1 text-center py-3 px-4 text-lg font-semibold rounded border transition-all duration-300 focus:outline-none focus:ring-2 focus:ring-green-400"
                        style={{
                          backgroundColor: 'var(--starcraft-bg)',
                          borderColor: 'var(--starcraft-border)',
                          color: 'var(--starcraft-green)',
                          MozAppearance: 'textfield',
                          WebkitAppearance: 'none',
                          appearance: 'none'
                        }}
                      />
                      {/* 스타일은 globals.css에서 관리 */}
                      <button
                        onClick={() => setFixedValue(Math.min(50, fixedValue + 1))}
                        className="p-2 rounded transition-all duration-300 hover:bg-blue-500/20"
                        style={{ color: '#4A90E2' }}
                        disabled={fixedValue >= 50}
                      >
                        <Plus className="w-5 h-5" />
                      </button>
                    </div>
                  </div>
                </div>

                {/* 시간 제한 설정 */}
                <div className="flex flex-col h-full">
                  <h3 
                    className="font-medium tracking-wide mb-4"
                    style={{ color: 'var(--starcraft-info)' }}
                  >
                    활성화 시간 설정
                  </h3>
                  
                  <div className="flex-1 p-4 rounded-lg border flex flex-col"
                    style={{
                      backgroundColor: 'var(--starcraft-bg-secondary)',
                      borderColor: 'var(--starcraft-red)'
                    }}
                  >
                    <div className="flex items-center gap-3 mb-3">
                      <input
                        type="checkbox"
                        checked={isTimeLimitEnabled}
                        onChange={(e) => setIsTimeLimitEnabled(e.target.checked)}
                        className="w-4 h-4"
                        style={{ accentColor: 'var(--starcraft-green)' }}
                      />
                      <label 
                        className="text-sm"
                        style={{ color: 'var(--starcraft-green)' }}
                      >
                        게임 시작 후 일정 시간 이후부터 경고 활성화
                      </label>
                    </div>
                    
                    {isTimeLimitEnabled && (
                      <div className="space-y-3 flex-1">
                        <div 
                          className="text-center py-2 px-3 rounded border"
                          style={{
                            backgroundColor: 'var(--starcraft-bg-active)',
                            borderColor: 'var(--starcraft-green)',
                            color: 'var(--starcraft-green)'
                          }}
                        >
                          {String(timeLimitMinutes).padStart(2, '0')}:{String(timeLimitSeconds).padStart(2, '0')} 후 활성화
                        </div>
                        
                        <div className="grid grid-cols-2 gap-4">
                          {/* 분 설정 */}
                          <div>
                            <label 
                              className="block text-sm mb-2 text-center"
                              style={{ color: 'var(--starcraft-green)' }}
                            >
                              분 (Minutes)
                            </label>
                            <div className="flex items-center gap-2">
                              <button
                                onClick={() => setTimeLimitMinutes(Math.max(0, timeLimitMinutes - 1))}
                                className="p-2 rounded transition-all duration-300 hover:bg-red-500/20"
                                style={{ color: 'var(--starcraft-red)' }}
                                disabled={timeLimitMinutes <= 0}
                              >
                                <Minus className="w-4 h-4" />
                              </button>
                              <input
                                type="number"
                                min="0"
                                max="59"
                                value={timeLimitMinutes}
                                onChange={(e) => {
                                  const value = parseInt(e.target.value) || 0;
                                  setTimeLimitMinutes(Math.max(0, Math.min(59, value)));
                                }}
                                className="flex-1 text-center py-3 px-3 font-mono text-lg rounded border-2 transition-all duration-300 focus:outline-none focus:ring-2 focus:ring-green-400"
                                style={{
                                  backgroundColor: 'var(--starcraft-bg)',
                                  borderColor: 'var(--starcraft-border)',
                                  color: 'var(--starcraft-green)',
                                  MozAppearance: 'textfield',
                                  WebkitAppearance: 'none',
                                  appearance: 'none'
                                }}
                                placeholder="00"
                              />
                              {/* 스타일은 globals.css에서 관리 */}
                              <button
                                onClick={() => setTimeLimitMinutes(Math.min(59, timeLimitMinutes + 1))}
                                className="p-2 rounded transition-all duration-300 hover:bg-blue-500/20"
                                style={{ color: '#4A90E2' }}
                                disabled={timeLimitMinutes >= 59}
                              >
                                <Plus className="w-4 h-4" />
                              </button>
                            </div>
                          </div>

                          {/* 초 설정 */}
                          <div>
                            <label 
                              className="block text-sm mb-2 text-center"
                              style={{ color: 'var(--starcraft-green)' }}
                            >
                              초 (Seconds)
                            </label>
                            <div className="flex items-center gap-2">
                              <button
                                onClick={() => setTimeLimitSeconds(Math.max(0, timeLimitSeconds - 1))}
                                className="p-2 rounded transition-all duration-300 hover:bg-red-500/20"
                                style={{ color: 'var(--starcraft-red)' }}
                                disabled={timeLimitSeconds <= 0}
                              >
                                <Minus className="w-4 h-4" />
                              </button>
                              <input
                                type="number"
                                min="0"
                                max="59"
                                value={timeLimitSeconds}
                                onChange={(e) => {
                                  const value = parseInt(e.target.value) || 0;
                                  setTimeLimitSeconds(Math.max(0, Math.min(59, value)));
                                }}
                                className="flex-1 text-center py-3 px-3 font-mono text-lg rounded border-2 transition-all duration-300 focus:outline-none focus:ring-2 focus:ring-green-400"
                                style={{
                                  backgroundColor: 'var(--starcraft-bg)',
                                  borderColor: 'var(--starcraft-border)',
                                  color: 'var(--starcraft-green)',
                                  MozAppearance: 'textfield',
                                  WebkitAppearance: 'none',
                                  appearance: 'none'
                                }}
                                placeholder="00"
                              />
                              <button
                                onClick={() => setTimeLimitSeconds(Math.min(59, timeLimitSeconds + 1))}
                                className="p-2 rounded transition-all duration-300 hover:bg-blue-500/20"
                                style={{ color: '#4A90E2' }}
                                disabled={timeLimitSeconds >= 59}
                              >
                                <Plus className="w-4 h-4" />
                              </button>
                            </div>
                          </div>
                        </div>

                        {/* 시간 설정 안내 */}
                        {timeLimitMinutes === 0 && timeLimitSeconds === 0 && (
                          <div 
                            className="text-xs text-center p-2 rounded"
                            style={{ 
                              color: 'var(--starcraft-green)',
                              backgroundColor: 'rgba(0, 255, 0, 0.1)'
                            }}
                          >
                            ℹ️ 게임 시작과 동시에 경고가 활성화됩니다
                          </div>
                        )}
                      </div>
                    )}
                  </div>
                </div>
              </div>
            )}


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
            onClick={onClose}
            className="px-6 py-2 rounded-sm border transition-all duration-300 hover:bg-red-500/20"
            style={{
              color: 'var(--starcup-red)',
              borderColor: 'var(--starcup-red)'
            }}
          >
            취소
          </button>
          
          <button
            onClick={handleConfirm}
            disabled={!hasChanges}
            className={`flex items-center gap-2 px-6 py-2 rounded-sm border transition-all duration-300 ${
              hasChanges 
                ? 'hover:bg-green-500/20' 
                : 'opacity-50 cursor-not-allowed'
            }`}
            style={{
              color: hasChanges ? 'var(--starcraft-green)' : 'var(--starcraft-inactive-text)',
              borderColor: hasChanges ? 'var(--starcraft-green)' : 'var(--starcraft-inactive-border)',
              backgroundColor: hasChanges ? 'var(--starcraft-bg-active)' : 'transparent'
            }}
          >
            <Settings2 className="w-4 h-4" />
            확인
          </button>
        </div>
      </div>
    </div>
  );
}