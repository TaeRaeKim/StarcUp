import React, { useState, useEffect } from 'react';
import { ArrowLeft, Zap, Plus, X, Clock, BarChart, Target, Info, Search, Shield, Home, Building2, Bell } from 'lucide-react';
import { RaceType, RACE_NAMES, UpgradeType, TechType } from '../../types/game';
import { UpgradeSettings, UpgradeCategory } from '../../types/preset';
import { getBuildingsByRace, getUpgradesByBuilding } from '../data/buildingUpgrades';
import { 
  getUpgradeIconPath, 
  getTechIconPath, 
  getBuildingIconPath, 
  getUpgradeName, 
  getTechName,
  UpgradeItemInfo,
  createUpgradeItemInfo,
  createTechItemInfo 
} from '../../utils/upgradeImageUtils';
import { getIconStyle } from '../../utils/iconUtils';

interface UpgradeDetailSettingsProps {
  isOpen: boolean;
  onClose: () => void;
  initialRace?: RaceType;
  currentPreset?: {
    id: string;
    name: string;
    description: string;
    upgradeSettings: UpgradeSettings;
  };
  onSaveUpgradeSettings?: (presetId: string, upgradeSettings: UpgradeSettings) => void;
  tempUpgradeSettings?: UpgradeSettings | null;
  onTempSave?: (settings: UpgradeSettings) => void;
}

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

export function UpgradeDetailSettings({ 
  isOpen, 
  onClose, 
  initialRace,
  currentPreset,
  onSaveUpgradeSettings,
  tempUpgradeSettings,
  onTempSave
}: UpgradeDetailSettingsProps) {
  // 종족 상태 관리
  const [selectedRace, setSelectedRace] = useState<RaceKey>(initialRace ?? RaceType.Protoss);
  
  // 기본 설정값
  const getDefaultUpgradeSettings = (): UpgradeSettings => ({
    categories: [{
      id: 'default_category',
      name: '기본 카테고리',
      upgrades: [],
      techs: []
    }],
    showRemainingTime: true,
    showProgressPercentage: true,
    showProgressBar: true,
    upgradeCompletionAlert: true,
    upgradeStateTracking: true
  });

  // 임시 저장된 값이 있으면 사용, 없으면 프리셋값 사용 (PopulationDetailSettings, WorkerDetailSettings와 동일한 패턴)
  const initialSettings = tempUpgradeSettings || currentPreset?.upgradeSettings || getDefaultUpgradeSettings();

  // 업그레이드 설정 상태
  const [settings, setSettings] = useState<UpgradeSettings>(initialSettings);

  // UI 상태
  const [showBuildingSelector, setShowBuildingSelector] = useState(false);
  const [showUpgradeSelector, setShowUpgradeSelector] = useState(false);
  const [selectedCategoryId, setSelectedCategoryId] = useState<string>('');
  const [selectedBuildingId, setSelectedBuildingId] = useState<string>('');
  const [newCategoryName, setNewCategoryName] = useState('');
  const [showAddCategory, setShowAddCategory] = useState(false);
  const [editingCategoryId, setEditingCategoryId] = useState<string>('');
  const [editingCategoryName, setEditingCategoryName] = useState('');
  const [searchTerm, setSearchTerm] = useState('');
  
  // 변경사항 감지 상태
  const [hasChanges, setHasChanges] = useState(false);

  // 진행률 표기 설정
  const progressDisplaySettings = [
    {
      id: 'remainingTime',
      title: '잔여 시간 표기',
      description: '업그레이드가 언제 끝날지 남은 시간을 숫자로 보여드려요',
      state: settings.showRemainingTime,
      setState: (value: boolean) => setSettings(prev => ({ ...prev, showRemainingTime: value })),
      icon: Clock
    },
    {
      id: 'progressPercentage',
      title: '진행률 표기',
      description: '업그레이드가 얼마나 진행됐는지 퍼센트로 알려드려요',
      state: settings.showProgressPercentage,
      setState: (value: boolean) => setSettings(prev => ({ ...prev, showProgressPercentage: value })),
      icon: Target
    },
    {
      id: 'progressBar',
      title: '프로그레스바 표기',
      description: '업그레이드 진행 상황을 예쁜 막대그래프로 보여드려요',
      state: settings.showProgressBar,
      setState: (value: boolean) => setSettings(prev => ({ ...prev, showProgressBar: value })),
      icon: BarChart
    },
    {
      id: 'completionAlert',
      title: '업그레이드 완료 알림',
      description: '업그레이드가 끝나면 반짝여서 완료됐다고 알려드려요',
      state: settings.upgradeCompletionAlert,
      setState: (value: boolean) => setSettings(prev => ({ ...prev, upgradeCompletionAlert: value })),
      icon: Bell
    },
    {
      id: 'stateTracking',
      title: '업그레이드 상태 추적',
      description: '업그레이드 상태를 실시간으로 추적하고 관리해요',
      state: settings.upgradeStateTracking,
      setState: (value: boolean) => setSettings(prev => ({ ...prev, upgradeStateTracking: value })),
      icon: Zap
    }
  ];

  // 카테고리 관리 함수들
  const handleAddCategory = () => {
    if (newCategoryName.trim()) {
      const newCategory: UpgradeCategory = {
        id: Date.now().toString(),
        name: newCategoryName.trim(),
        upgrades: [],
        techs: []
      };
      setSettings(prev => ({
        ...prev,
        categories: [...prev.categories, newCategory]
      }));
      setNewCategoryName('');
      setShowAddCategory(false);
    }
  };

  const handleDeleteCategory = (categoryId: string) => {
    setSettings(prev => ({
      ...prev,
      categories: prev.categories.filter(cat => cat.id !== categoryId)
    }));
  };

  const handleEditCategory = (categoryId: string, newName: string) => {
    if (newName.trim()) {
      setSettings(prev => ({
        ...prev,
        categories: prev.categories.map(cat => 
          cat.id === categoryId ? { ...cat, name: newName.trim() } : cat
        )
      }));
    }
    setEditingCategoryId('');
    setEditingCategoryName('');
  };

  const handleCategoryDoubleClick = (category: UpgradeCategory) => {
    setEditingCategoryId(category.id);
    setEditingCategoryName(category.name);
  };

  // 업그레이드 추가 플로우
  const handleAddUpgradeToCategory = (categoryId: string) => {
    setSelectedCategoryId(categoryId);
    setShowBuildingSelector(true);
  };

  const handleSelectBuilding = (buildingId: string) => {
    setSelectedBuildingId(buildingId);
    setShowBuildingSelector(false);
    setShowUpgradeSelector(true);
  };

  const handleSelectUpgradeItem = (item: UpgradeItemInfo) => {
    if (selectedCategoryId) {
      setSettings(prev => ({
        ...prev,
        categories: prev.categories.map(cat => {
          if (cat.id === selectedCategoryId) {
            if (item.type === 'upgrade') {
              const upgradeId = item.id as UpgradeType;
              return {
                ...cat,
                upgrades: cat.upgrades.includes(upgradeId) 
                  ? cat.upgrades 
                  : [...cat.upgrades, upgradeId]
              };
            } else {
              const techId = item.id as TechType;
              return {
                ...cat,
                techs: cat.techs.includes(techId) 
                  ? cat.techs 
                  : [...cat.techs, techId]
              };
            }
          }
          return cat;
        })
      }));
      
      setShowUpgradeSelector(false);
      setSelectedCategoryId('');
      setSelectedBuildingId('');
      setSearchTerm('');
    }
  };

  const handleRemoveUpgradeFromCategory = (categoryId: string, item: UpgradeItemInfo) => {
    setSettings(prev => ({
      ...prev,
      categories: prev.categories.map(cat => {
        if (cat.id === categoryId) {
          if (item.type === 'upgrade') {
            const upgradeId = item.id as UpgradeType;
            return {
              ...cat,
              upgrades: cat.upgrades.filter(u => u !== upgradeId)
            };
          } else {
            const techId = item.id as TechType;
            return {
              ...cat,
              techs: cat.techs.filter(t => t !== techId)
            };
          }
        }
        return cat;
      })
    }));
  };

  // 현재 선택된 건물의 업그레이드/테크 가져오기
  const getAvailableUpgradeItems = (): UpgradeItemInfo[] => {
    if (!selectedBuildingId) return [];
    
    const { upgrades, techs } = getUpgradesByBuilding(selectedRace, selectedBuildingId);
    const items: UpgradeItemInfo[] = [];
    
    // 업그레이드 추가
    upgrades.forEach(upgrade => {
      items.push(createUpgradeItemInfo(upgrade));
    });
    
    // 테크 추가
    techs.forEach(tech => {
      items.push(createTechItemInfo(tech));
    });
    
    // 검색 필터링
    return items.filter(item =>
      item.name.toLowerCase().includes(searchTerm.toLowerCase())
    );
  };

  // 카테고리의 모든 아이템 가져오기
  const getCategoryItems = (category: UpgradeCategory): UpgradeItemInfo[] => {
    const items: UpgradeItemInfo[] = [];
    
    category.upgrades.forEach(upgrade => {
      items.push(createUpgradeItemInfo(upgrade));
    });
    
    category.techs.forEach(tech => {
      items.push(createTechItemInfo(tech));
    });
    
    return items;
  };

  // 프리셋 변경 시 업그레이드 설정 업데이트 (PopulationDetailSettings, WorkerDetailSettings와 동일한 패턴)
  useEffect(() => {
    // 임시 저장된 값이 있으면 사용, 없으면 프리셋값 사용
    const newSettings = tempUpgradeSettings || currentPreset?.upgradeSettings || getDefaultUpgradeSettings();
    setSettings(newSettings);
  }, [currentPreset, tempUpgradeSettings]);

  // initialRace가 변경될 때 selectedRace 업데이트
  useEffect(() => {
    if (initialRace !== undefined) {
      setSelectedRace(initialRace);
    }
  }, [initialRace]);

  // 변경사항 감지 - 원본 프리셋 설정과 현재 설정 비교 (PopulationDetailSettings, WorkerDetailSettings와 동일한 패턴)
  useEffect(() => {
    const originalSettings = currentPreset?.upgradeSettings || getDefaultUpgradeSettings();
    
    // JSON 직렬화를 통한 깊은 비교
    const currentSettingsStr = JSON.stringify(settings);
    const originalSettingsStr = JSON.stringify(originalSettings);
    
    const hasAnyChanges = currentSettingsStr !== originalSettingsStr;
    setHasChanges(hasAnyChanges);
  }, [settings, currentPreset?.upgradeSettings]);

  const handleSave = async () => {
    try {
      // 임시 저장 함수가 있으면 임시 저장만 수행
      if (onTempSave) {
        onTempSave(settings);
      } else if (currentPreset && onSaveUpgradeSettings) {
        // 임시 저장 함수가 없으면 기존처럼 직접 저장
        onSaveUpgradeSettings(currentPreset.id, settings);
      }

      onClose();
    } catch (error) {
      console.error('❌ 업그레이드 설정 저장 실패:', error);
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
      <div className="flex flex-col h-full">
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
            
            <Zap 
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
                업그레이드 설정
              </h1>
              <p 
                className="text-sm opacity-70"
                style={{ color: 'var(--starcraft-green)' }}
              >
                업그레이드 카테고리와 진행률 표시를 설정하세요
              </p>
            </div>
          </div>
          
          {/* 종족 표시 */}
          <div 
            className="px-4 py-2 rounded-full border-2 flex items-center gap-2"
            style={{
              color: RACES[selectedRace].color,
              borderColor: RACES[selectedRace].color,
              backgroundColor: 'var(--starcraft-bg-active)',
              boxShadow: `0 0 8px ${RACES[selectedRace].color}30`
            }}
          >
            {React.createElement(RACES[selectedRace].icon, { className: "w-4 h-4" })}
            <span className="text-sm font-semibold">{RACES[selectedRace].name}</span>
          </div>
        </div>

        {/* 컨텐츠 - 스크롤 가능 */}
        <div className="flex-1 overflow-y-auto starcraft-scrollbar p-6 space-y-8">
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
              {RACES[selectedRace].name} 업그레이드만 선택할 수 있습니다.
            </div>
          </div>

          {/* 카테고리 관리 */}
          <div className="space-y-4">
            <div className="flex items-center justify-between">
              <h2 
                className="text-lg font-medium tracking-wide flex items-center gap-2"
                style={{ color: 'var(--starcraft-green)' }}
              >
                <Zap className="w-5 h-5" />
                업그레이드 카테고리
              </h2>
              
              <button
                onClick={() => setShowAddCategory(true)}
                className="flex items-center gap-2 px-4 py-2 rounded-sm border transition-all duration-300 hover:bg-green-500/20"
                style={{
                  color: 'var(--starcraft-green)',
                  borderColor: 'var(--starcraft-green)'
                }}
              >
                <Plus className="w-4 h-4" />
                카테고리 추가
              </button>
            </div>

            {/* 새 카테고리 추가 입력 */}
            {showAddCategory && (
              <div className="p-4 rounded-lg border"
                style={{
                  backgroundColor: 'var(--starcraft-bg-active)',
                  borderColor: 'var(--starcraft-green)'
                }}
              >
                <div className="flex items-center gap-2">
                  <input
                    type="text"
                    value={newCategoryName}
                    onChange={(e) => setNewCategoryName(e.target.value)}
                    placeholder="카테고리 이름 입력"
                    className="flex-1 px-3 py-2 rounded-sm border"
                    style={{
                      backgroundColor: 'var(--starcraft-bg)',
                      borderColor: 'var(--starcraft-border)',
                      color: 'var(--starcraft-green)'
                    }}
                    onKeyPress={(e) => e.key === 'Enter' && handleAddCategory()}
                    autoFocus
                  />
                  <button
                    onClick={handleAddCategory}
                    className="px-4 py-2 rounded-sm border transition-all duration-300 hover:bg-green-500/20"
                    style={{
                      color: 'var(--starcraft-green)',
                      borderColor: 'var(--starcraft-green)'
                    }}
                  >
                    추가
                  </button>
                  <button
                    onClick={() => {
                      setShowAddCategory(false);
                      setNewCategoryName('');
                    }}
                    className="px-4 py-2 rounded-sm border transition-all duration-300 hover:bg-red-500/20"
                    style={{
                      color: 'var(--starcraft-red)',
                      borderColor: 'var(--starcraft-red)'
                    }}
                  >
                    취소
                  </button>
                </div>
              </div>
            )}

            {/* 카테고리 목록 */}
            <div className="space-y-6">
              {settings.categories.map((category) => {
                const categoryItems = getCategoryItems(category);
                
                return (
                  <div key={category.id} 
                    className="p-4 rounded-lg border"
                    style={{
                      backgroundColor: 'var(--starcraft-bg-secondary)',
                      borderColor: 'var(--starcraft-border)'
                    }}
                  >
                    {/* 카테고리 헤더 */}
                    <div className="flex items-center justify-between mb-4">
                      {editingCategoryId === category.id ? (
                        <div className="flex items-center gap-2 flex-1">
                          <input
                            type="text"
                            value={editingCategoryName}
                            onChange={(e) => setEditingCategoryName(e.target.value)}
                            className="flex-1 px-3 py-1 rounded-sm border"
                            style={{
                              backgroundColor: 'var(--starcraft-bg)',
                              borderColor: 'var(--starcraft-border)',
                              color: 'var(--starcraft-green)'
                            }}
                            onKeyPress={(e) => e.key === 'Enter' && handleEditCategory(category.id, editingCategoryName)}
                            onBlur={() => handleEditCategory(category.id, editingCategoryName)}
                            autoFocus
                          />
                        </div>
                      ) : (
                        <h3 
                          className="font-medium tracking-wide cursor-pointer hover:opacity-80 transition-opacity"
                          style={{ color: 'var(--starcraft-green)' }}
                          onDoubleClick={() => handleCategoryDoubleClick(category)}
                          title="더블클릭하여 편집"
                        >
                          {category.name}
                        </h3>
                      )}
                      
                      <button
                        onClick={() => handleDeleteCategory(category.id)}
                        className="p-2 rounded-sm transition-all duration-300 hover:bg-red-500/20"
                        style={{ color: 'var(--starcraft-red)' }}
                      >
                        <X className="w-4 h-4" />
                      </button>
                    </div>

                    {/* 업그레이드 그리드 */}
                    <div className="grid grid-cols-6 sm:grid-cols-8 md:grid-cols-10 gap-3">
                      {categoryItems.map((item) => (
                        <div key={`${item.type}_${item.id}`} 
                          className="relative group flex flex-col items-center justify-center rounded-lg border-2 cursor-pointer transition-all duration-300 hover:border-opacity-60 p-2 min-h-[80px]"
                          style={{
                            backgroundColor: 'var(--starcraft-bg-active)',
                            borderColor: RACES[selectedRace].color
                          }}
                        >
                          <div className="flex flex-col items-center justify-center flex-1">
                            <div style={getIconStyle('yellow', undefined, { width: '32px', height: '32px', marginBottom: '4px' })}>
                              <img 
                                src={item.iconPath} 
                                alt={item.name}
                                className="w-full h-full object-contain"
                                onError={(e) => {
                                  // 이미지 로드 실패시 기본 아이콘 표시
                                  e.currentTarget.style.display = 'none';
                                }}
                              />
                            </div>
                            <span className="text-xs text-center leading-tight font-medium" 
                              style={{ color: RACES[selectedRace].color }}>
                              {item.name}
                            </span>
                          </div>
                          
                          {/* 호버 시 삭제 버튼 */}
                          <button
                            onClick={() => handleRemoveUpgradeFromCategory(category.id, item)}
                            className="absolute -top-2 -right-2 w-6 h-6 rounded-full flex items-center justify-center opacity-0 group-hover:opacity-100 transition-opacity duration-200 border"
                            style={{
                              backgroundColor: 'var(--starcraft-red)',
                              borderColor: 'var(--starcraft-red)',
                              color: 'white'
                            }}
                          >
                            <X className="w-3 h-3" />
                          </button>
                        </div>
                      ))}
                      
                      {/* + 버튼 */}
                      <button
                        onClick={() => handleAddUpgradeToCategory(category.id)}
                        className="flex flex-col items-center justify-center rounded-lg border-2 border-dashed transition-all duration-300 hover:border-opacity-60 hover:bg-opacity-10 p-2 min-h-[80px]"
                        style={{
                          borderColor: RACES[selectedRace].color,
                          color: RACES[selectedRace].color,
                          '--hover-bg': `${RACES[selectedRace].color}10`
                        } as React.CSSProperties}
                      >
                        <Plus className="w-8 h-8" />
                      </button>
                    </div>

                    {categoryItems.length === 0 && (
                      <p className="text-sm opacity-60 mt-4 text-center" style={{ color: RACES[selectedRace].color }}>
                        + 버튼을 눌러 {RACES[selectedRace].name} 업그레이드를 추가하세요
                      </p>
                    )}
                  </div>
                );
              })}
            </div>
          </div>

          {/* 진행률 표기 설정들 */}
          <div className="space-y-4">
            <h2 
              className="text-lg font-medium tracking-wide flex items-center gap-2"
              style={{ color: 'var(--starcraft-green)' }}
            >
              <BarChart className="w-5 h-5" />
              진행률 표기 설정
            </h2>
            
            <div className="space-y-4">
              {progressDisplaySettings.map((item) => {
                const IconComponent = item.icon;
                return (
                  <div key={item.id} 
                    className="p-4 rounded-lg border transition-all duration-300 hover:border-opacity-80"
                    style={{
                      backgroundColor: item.state 
                        ? 'var(--starcraft-bg-active)' 
                        : 'var(--starcraft-bg-secondary)',
                      borderColor: item.state 
                        ? 'var(--starcraft-green)' 
                        : 'var(--starcraft-inactive-border)'
                    }}
                  >
                    <div className="flex items-start justify-between gap-4">
                      <div className="flex items-start gap-3 flex-1">
                        <div className="p-2 rounded-lg mt-1"
                          style={{ 
                            backgroundColor: item.state 
                              ? 'var(--starcraft-bg-active)' 
                              : 'var(--starcraft-bg)',
                            color: item.state 
                              ? 'var(--starcraft-green)' 
                              : 'var(--starcraft-inactive-text)'
                          }}
                        >
                          <IconComponent className="w-5 h-5" />
                        </div>
                        <div className="flex-1">
                          <h3 className="font-medium mb-2 tracking-wide"
                            style={{ 
                              color: item.state 
                                ? 'var(--starcraft-green)' 
                                : 'var(--starcraft-inactive-text)' 
                            }}
                          >
                            {item.title}
                          </h3>
                          <p className="text-sm opacity-80 leading-relaxed"
                            style={{ 
                              color: item.state 
                                ? 'var(--starcraft-green)' 
                                : 'var(--starcraft-inactive-text)' 
                            }}
                          >
                            {item.description}
                          </p>
                        </div>
                      </div>
                      
                      <div className="flex items-center">
                        <button
                          onClick={() => item.setState(!item.state)}
                          className={`relative inline-flex h-6 w-11 items-center rounded-full transition-colors duration-300 focus:outline-none focus:ring-2 focus:ring-offset-2 ${
                            item.state 
                              ? 'focus:ring-green-500' 
                              : 'focus:ring-gray-500'
                          }`}
                          style={{
                            backgroundColor: item.state 
                              ? 'var(--starcraft-green)' 
                              : 'var(--starcraft-inactive-bg)'
                          }}
                        >
                          <span
                            className={`inline-block h-4 w-4 transform rounded-full transition-transform duration-300 ${
                              item.state ? 'translate-x-6' : 'translate-x-1'
                            }`}
                            style={{
                              backgroundColor: item.state 
                                ? 'var(--starcraft-bg)' 
                                : 'var(--starcraft-inactive-secondary)'
                            }}
                          />
                        </button>
                      </div>
                    </div>
                  </div>
                );
              })}
            </div>
          </div>

          {/* 안내 정보 */}
          <div className="p-4 rounded-lg border"
            style={{
              backgroundColor: 'var(--starcraft-bg-active)',
              borderColor: 'var(--starcraft-green)',
              color: 'var(--starcraft-green)'
            }}
          >
            <div className="flex items-center gap-2 mb-2">
              <Info className="w-4 h-4" />
              <span className="text-sm font-medium">설정 안내</span>
            </div>
            <ul className="text-xs space-y-1 opacity-90 pl-6">
              <li>• + 버튼을 클릭하면 건물 선택 후 업그레이드를 추가할 수 있습니다</li>
              <li>• 카테고리 이름을 더블클릭하면 편집할 수 있습니다</li>
              <li>• 업그레이드 아이콘에 마우스를 올리면 삭제 버튼이 나타납니다</li>
              <li>• 진행률 표기는 실시간으로 게임 상태와 동기화됩니다</li>
            </ul>
          </div>
        </div>

        {/* 하단 버튼 */}
        <div 
          className="flex items-center justify-end p-4 border-t gap-3"
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
            확인
          </button>
        </div>
      </div>

      {/* 건물 선택 모달 */}
      {showBuildingSelector && (
        <div className="fixed inset-0 z-60 flex items-center justify-center p-4">
          <div 
            className="absolute inset-0 backdrop-blur-sm"
            style={{ backgroundColor: 'rgba(0, 0, 0, 0.9)' }}
            onClick={() => setShowBuildingSelector(false)}
          />
          
          <div 
            className="relative w-full max-w-4xl max-h-[80vh] overflow-hidden rounded-lg border-2 shadow-2xl"
            style={{
              backgroundColor: 'var(--starcraft-bg)',
              borderColor: 'var(--starcraft-green)',
              boxShadow: '0 0 30px rgba(0, 255, 0, 0.4)'
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
                <h2 
                  className="text-lg font-semibold tracking-wide"
                  style={{ 
                    color: 'var(--starcraft-green)',
                    textShadow: '0 0 8px rgba(0, 255, 0, 0.5)'
                  }}
                >
                  건물 선택
                </h2>
                <div 
                  className="px-3 py-1 rounded-full border flex items-center gap-2"
                  style={{
                    color: RACES[selectedRace].color,
                    borderColor: RACES[selectedRace].color,
                    backgroundColor: 'var(--starcraft-bg-active)'
                  }}
                >
                  {React.createElement(RACES[selectedRace].icon, { className: "w-3 h-3" })}
                  <span className="text-xs font-medium">{RACES[selectedRace].name}</span>
                </div>
              </div>
              
              <button
                onClick={() => setShowBuildingSelector(false)}
                className="p-2 rounded-sm transition-all duration-300 hover:bg-red-500/20"
                style={{ color: 'var(--starcraft-red)' }}
              >
                <X className="w-5 h-5" />
              </button>
            </div>

            {/* 건물 선택 그리드 */}
            <div className="p-4 overflow-y-auto max-h-[calc(80vh-120px)]">
              <div className="grid grid-cols-3 sm:grid-cols-4 md:grid-cols-6 gap-4">
                {getBuildingsByRace(selectedRace).map((building) => (
                  <button
                    key={building.id}
                    onClick={() => handleSelectBuilding(building.id)}
                    className="flex flex-col items-center justify-center p-4 rounded-lg border-2 transition-all duration-300 hover:border-opacity-80 hover:scale-105 min-h-[120px]"
                    style={{
                      backgroundColor: 'var(--starcraft-bg-secondary)',
                      borderColor: RACES[selectedRace].color,
                      color: RACES[selectedRace].color
                    }}
                  >
                    <div className="flex flex-col items-center justify-center flex-1">
                      <div style={getIconStyle('yellow', undefined, { width: '64px', height: '64px', marginBottom: '12px' })}>
                        <img 
                          src={getBuildingIconPath(building.iconPath)} 
                          alt={building.name}
                          className="w-full h-full object-contain"
                          onError={(e) => {
                            // 이미지 로드 실패시 기본 아이콘 표시
                            e.currentTarget.style.display = 'none';
                          }}
                        />
                      </div>
                      <span className="text-sm text-center leading-tight font-medium mb-1">
                        {building.name}
                      </span>
                      <span className="text-xs opacity-70 text-center">
                        {building.upgrades.length + building.techs.length}개 항목
                      </span>
                    </div>
                  </button>
                ))}
              </div>
            </div>
          </div>
        </div>
      )}

      {/* 업그레이드 선택 모달 */}
      {showUpgradeSelector && (
        <div className="fixed inset-0 z-60 flex items-center justify-center p-4">
          <div 
            className="absolute inset-0 backdrop-blur-sm"
            style={{ backgroundColor: 'rgba(0, 0, 0, 0.9)' }}
            onClick={() => setShowUpgradeSelector(false)}
          />
          
          <div 
            className="relative w-full max-w-4xl max-h-[80vh] overflow-hidden rounded-lg border-2 shadow-2xl"
            style={{
              backgroundColor: 'var(--starcraft-bg)',
              borderColor: 'var(--starcraft-green)',
              boxShadow: '0 0 30px rgba(0, 255, 0, 0.4)'
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
                <h2 
                  className="text-lg font-semibold tracking-wide"
                  style={{ 
                    color: 'var(--starcraft-green)',
                    textShadow: '0 0 8px rgba(0, 255, 0, 0.5)'
                  }}
                >
                  업그레이드 선택
                </h2>
                <div 
                  className="px-3 py-1 rounded-full border flex items-center gap-2"
                  style={{
                    color: RACES[selectedRace].color,
                    borderColor: RACES[selectedRace].color,
                    backgroundColor: 'var(--starcraft-bg-active)'
                  }}
                >
                  {React.createElement(RACES[selectedRace].icon, { className: "w-3 h-3" })}
                  <span className="text-xs font-medium">{RACES[selectedRace].name}</span>
                </div>
              </div>
              
              <button
                onClick={() => setShowUpgradeSelector(false)}
                className="p-2 rounded-sm transition-all duration-300 hover:bg-red-500/20"
                style={{ color: 'var(--starcraft-red)' }}
              >
                <X className="w-5 h-5" />
              </button>
            </div>

            {/* 검색바 */}
            <div className="p-4 border-b" style={{ borderBottomColor: 'var(--starcraft-border)' }}>
              <div className="relative">
                <Search 
                  className="absolute left-3 top-1/2 transform -translate-y-1/2 w-4 h-4"
                  style={{ color: 'var(--starcraft-green)' }}
                />
                <input
                  type="text"
                  value={searchTerm}
                  onChange={(e) => setSearchTerm(e.target.value)}
                  placeholder="업그레이드 검색..."
                  className="w-full pl-10 pr-4 py-2 rounded-sm border"
                  style={{
                    backgroundColor: 'var(--starcraft-bg-secondary)',
                    borderColor: 'var(--starcraft-border)',
                    color: 'var(--starcraft-green)'
                  }}
                />
              </div>
            </div>

            {/* 업그레이드 선택 그리드 */}
            <div className="p-4 overflow-y-auto max-h-[calc(80vh-180px)]">
              <div className="grid grid-cols-4 sm:grid-cols-6 md:grid-cols-8 gap-4">
                {getAvailableUpgradeItems().map((item) => (
                  <button
                    key={`${item.type}_${item.id}`}
                    onClick={() => handleSelectUpgradeItem(item)}
                    className="flex flex-col items-center justify-center rounded-lg border-2 transition-all duration-300 hover:border-opacity-80 hover:scale-105 p-3 min-h-[100px]"
                    style={{
                      backgroundColor: 'var(--starcraft-bg-secondary)',
                      borderColor: RACES[selectedRace].color,
                      color: RACES[selectedRace].color
                    }}
                  >
                    <div className="flex flex-col items-center justify-center flex-1">
                      <div style={getIconStyle('yellow', undefined, { width: '40px', height: '40px', marginBottom: '8px' })}>
                        <img 
                          src={item.iconPath} 
                          alt={item.name}
                          className="w-full h-full object-contain"
                          onError={(e) => {
                            // 이미지 로드 실패시 숨김
                            e.currentTarget.style.display = 'none';
                          }}
                        />
                      </div>
                      <span className="text-xs text-center leading-tight font-medium">
                        {item.name}
                      </span>
                    </div>
                  </button>
                ))}
              </div>
              
              {getAvailableUpgradeItems().length === 0 && (
                <div className="text-center py-8">
                  <p className="text-sm opacity-60" style={{ color: 'var(--starcraft-green)' }}>
                    {searchTerm ? '검색 결과가 없습니다' : '이 건물에서 제공하는 업그레이드가 없습니다'}
                  </p>
                </div>
              )}
            </div>
          </div>
        </div>
      )}
    </div>
  );
}