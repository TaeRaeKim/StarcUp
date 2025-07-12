import { useState, useEffect } from 'react';
import { ArrowLeft, Users, Building2, Clock, Settings2, Info, Plus, Minus, Shield, Bot, Star, Home, Cog, Zap } from 'lucide-react';

interface PopulationDetailSettingsProps {
  isOpen: boolean;
  onClose: () => void;
  initialRace?: 'protoss' | 'terran' | 'zerg';
}

// 종족 정보
const RACES = {
  protoss: {
    name: '프로토스',
    color: '#FFD700',
    buildings: [
      { key: 'gateway', name: '게이트웨이', defaultMultiplier: 1, icon: Shield },
      { key: 'robotics', name: '로보틱스', defaultMultiplier: 2, icon: Bot },
      { key: 'stargate', name: '스타게이트', defaultMultiplier: 2, icon: Star }
    ]
  },
  terran: {
    name: '테란',
    color: '#0099FF',
    buildings: [
      { key: 'barracks', name: '배럭', defaultMultiplier: 1, icon: Home },
      { key: 'factory', name: '팩토리', defaultMultiplier: 2, icon: Cog },
      { key: 'starport', name: '스타포트', defaultMultiplier: 2, icon: Zap }
    ]
  },
  zerg: {
    name: '저그',
    color: '#9932CC',
    buildings: [
      { key: 'hatchery', name: '해처리', defaultMultiplier: 3, icon: Building2 }
    ]
  }
} as const;

type RaceKey = keyof typeof RACES;
type BuildingSettings = Record<string, { enabled: boolean; multiplier: number }>;

export function PopulationDetailSettings({ isOpen, onClose, initialRace }: PopulationDetailSettingsProps) {
  const [mode, setMode] = useState<'building' | 'fixed'>('fixed');
  const [selectedRace, setSelectedRace] = useState<RaceKey>(initialRace || 'protoss');
  
  // 디버깅: 컴포넌트가 받는 props 확인
  console.log('PopulationDetailSettings props:', { isOpen, initialRace, selectedRace });
  
  // initialRace가 변경될 때마다 selectedRace 업데이트 및 buildingSettings 초기화
  useEffect(() => {
    if (initialRace) {
      const previousRace = selectedRace;
      setSelectedRace(initialRace);
      // 종족이 변경되면 이전 건물 설정을 초기화
      if (previousRace !== initialRace) {
        setBuildingSettings({});
        console.log(`실시간 종족 변경: ${RACES[previousRace]?.name || '없음'} → ${RACES[initialRace].name}, 건물 설정 초기화`);
      }
    }
  }, [initialRace]);
  const [buildingSettings, setBuildingSettings] = useState<BuildingSettings>({});
  const [fixedValue, setFixedValue] = useState(4);
  const [timeLimitMinutes, setTimeLimitMinutes] = useState(3);
  const [timeLimitSeconds, setTimeLimitSeconds] = useState(0); // 게임 시작 후 n분
  const [isTimeLimitEnabled, setIsTimeLimitEnabled] = useState(true);

  // 건물 설정 업데이트
  const toggleBuildingEnabled = (buildingKey: string) => {
    setBuildingSettings(prev => {
      const current = prev[buildingKey];
      return {
        ...prev,
        [buildingKey]: {
          enabled: !(current?.enabled || false),
          multiplier: current?.multiplier || RACES[selectedRace].buildings.find(b => b.key === buildingKey)?.defaultMultiplier || 1
        }
      };
    });
  };

  const updateBuildingMultiplier = (buildingKey: string, delta: number) => {
    setBuildingSettings(prev => {
      const current = prev[buildingKey];
      return {
        ...prev,
        [buildingKey]: {
          enabled: current?.enabled || false,
          multiplier: Math.max(1, Math.min(10, (current?.multiplier || 1) + delta))
        }
      };
    });
  };

  // 초기 건물 설정 생성
  const getBuildingConfig = (buildingKey: string, defaultMultiplier: number) => {
    return buildingSettings[buildingKey] || { enabled: false, multiplier: defaultMultiplier };
  };

  // 총 경고 기준값 계산 (모드 A) - 프로그램이 자동으로 건물 개수 추적
  const calculateTotalThreshold = () => {
    const race = RACES[selectedRace];
    return race.buildings.reduce((total, building) => {
      const config = getBuildingConfig(building.key, building.defaultMultiplier);
      // 선택된 건물만 계산 (실제로는 프로그램이 해당 건물 개수를 자동 추적)
      return config.enabled ? total + config.multiplier : total;
    }, 0);
  };

  const handleSave = () => {
    // TODO: 설정 저장 로직
    const settingsToSave = {
      mode,
      ...(mode === 'building' && {
        selectedRace,
        trackedBuildings: Object.entries(buildingSettings)
          .filter(([_, config]) => config.enabled)
          .map(([buildingKey, config]) => ({
            buildingKey,
            name: RACES[selectedRace].buildings.find(b => b.key === buildingKey)?.name,
            multiplier: config.multiplier
          }))
      }),
      ...(mode === 'fixed' && {
        fixedValue,
        ...(isTimeLimitEnabled && { 
          timeLimitMinutes, 
          timeLimitSeconds,
          totalSeconds: timeLimitMinutes * 60 + timeLimitSeconds 
        })
      })
    };
    
    console.log('인구수 설정 저장:', settingsToSave);
    onClose();
  };

  if (!isOpen) return null;

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4">
      {/* 배경 오버레이 */}
      <div 
        className="absolute inset-0 backdrop-blur-sm"
        style={{ backgroundColor: 'rgba(0, 0, 0, 0.8)' }}
      />
      
      {/* 메인 컨테이너 */}
      <div 
        className="relative w-full max-w-4xl max-h-[95vh] overflow-hidden rounded-lg border-2 shadow-2xl"
        style={{
          backgroundColor: 'var(--starcraft-bg)',
          borderColor: 'var(--starcraft-green)',
          boxShadow: '0 0 30px rgba(0, 255, 0, 0.4), inset 0 0 30px rgba(0, 255, 0, 0.1)'
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

        {/* 스크롤 가능한 컨텐츠 */}
        <div className="overflow-y-auto max-h-[calc(95vh-140px)]">
          <div className="p-6 space-y-8">
            
            {/* 모드 선택 */}
            <div className="space-y-4">
              <h2 
                className="text-lg font-medium tracking-wide flex items-center gap-2"
                style={{ color: 'var(--starcraft-green)' }}
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
                      ? 'var(--starcraft-bg-active)' 
                      : 'var(--starcraft-bg-secondary)',
                    borderColor: mode === 'fixed' 
                      ? 'var(--starcraft-green)' 
                      : 'var(--starcraft-border)',
                    boxShadow: mode === 'fixed' 
                      ? '0 0 10px rgba(0, 255, 0, 0.3)' 
                      : 'none'
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
                      ? 'var(--starcraft-bg-active)' 
                      : 'var(--starcraft-bg-secondary)',
                    borderColor: mode === 'building' 
                      ? 'var(--starcraft-green)' 
                      : 'var(--starcraft-border)',
                    boxShadow: mode === 'building' 
                      ? '0 0 10px rgba(0, 255, 0, 0.3)' 
                      : 'none'
                  }}
                  onClick={() => setMode('building')}
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
                    backgroundColor: 'var(--starcraft-bg-active)',
                    borderColor: RACES[selectedRace].color,
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
                        style={{ color: 'var(--starcraft-green)' }}
                      >
                        추적할 생산 건물
                      </h3>
                      <div 
                        className="px-3 py-1 rounded-full border-2 flex items-center gap-2"
                        style={{
                          color: RACES[selectedRace].color,
                          borderColor: RACES[selectedRace].color,
                          backgroundColor: 'var(--starcraft-bg-active)',
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
                        borderColor: 'var(--starcraft-border)',
                        backgroundColor: 'var(--starcraft-bg-secondary)'
                      }}
                    >
                      선택됨: {RACES[selectedRace].buildings.filter(building => 
                        getBuildingConfig(building.key, building.defaultMultiplier).enabled
                      ).length}/{RACES[selectedRace].buildings.length}
                    </div>
                  </div>

                  <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
                    {RACES[selectedRace].buildings.map((building) => {
                      const config = getBuildingConfig(building.key, building.defaultMultiplier);
                      const IconComponent = building.icon;
                      return (
                        <div
                          key={building.key}
                          className={`p-4 rounded-lg border-2 transition-all duration-300 ${
                            config.enabled ? 'border-current' : ''
                          }`}
                          style={{
                            backgroundColor: config.enabled 
                              ? 'var(--starcraft-bg-active)' 
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
                            <label className="flex items-center cursor-pointer">
                              <input
                                type="checkbox"
                                checked={config.enabled}
                                onChange={() => toggleBuildingEnabled(building.key)}
                                className="sr-only"
                              />
                              <div
                                className={`
                                  w-6 h-6 rounded border-2 transition-all duration-300 flex items-center justify-center
                                  ${config.enabled ? 'border-current' : ''}
                                `}
                                style={{
                                  backgroundColor: config.enabled 
                                    ? RACES[selectedRace].color 
                                    : 'transparent',
                                  borderColor: config.enabled 
                                    ? RACES[selectedRace].color 
                                    : 'var(--starcraft-border)'
                                }}
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
                                  onClick={() => updateBuildingMultiplier(building.key, -1)}
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
                                  onClick={() => updateBuildingMultiplier(building.key, 1)}
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
                    style={{ color: 'var(--starcraft-green)' }}
                  >
                    경고 기준값 설정
                  </h3>
                  
                  <div className="flex-1 p-4 rounded-lg border"
                    style={{
                      backgroundColor: 'var(--starcraft-bg-secondary)',
                      borderColor: 'var(--starcraft-border)'
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
                    style={{ color: 'var(--starcraft-green)' }}
                  >
                    활성화 시간 설정
                  </h3>
                  
                  <div className="flex-1 p-4 rounded-lg border flex flex-col"
                    style={{
                      backgroundColor: 'var(--starcraft-bg-secondary)',
                      borderColor: 'var(--starcraft-border)'
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

                        {/* 시간 유효성 체크 */}
                        {timeLimitMinutes === 0 && timeLimitSeconds === 0 && (
                          <div 
                            className="text-xs text-center p-2 rounded"
                            style={{ 
                              color: 'var(--starcraft-red)',
                              backgroundColor: 'rgba(255, 0, 0, 0.1)'
                            }}
                          >
                            ⚠️ 시간을 0분 0초보다 크게 설정해주세요
                          </div>
                        )}
                      </div>
                    )}
                  </div>
                </div>
              </div>
            )}


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
            onClick={onClose}
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
            className="flex items-center gap-2 px-6 py-2 rounded-sm border transition-all duration-300 hover:bg-green-500/20"
            style={{
              color: 'var(--starcraft-green)',
              borderColor: 'var(--starcraft-green)',
              backgroundColor: 'var(--starcraft-bg-active)'
            }}
          >
            <Settings2 className="w-4 h-4" />
            저장
          </button>
        </div>
      </div>
    </div>
  );
}