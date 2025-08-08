import React, { useState, useEffect } from 'react';
import { ArrowLeft, Zap, Plus, X, Clock, BarChart, Target, Info, Search, Shield, Home, Building2, Bell } from 'lucide-react';
import { RaceType, RACE_NAMES } from '../types/enums';

interface UpgradeDetailSettingsProps {
  isOpen: boolean;
  onClose: () => void;
  initialRace?: RaceType;
}

interface UpgradeCategory {
  id: string;
  name: string;
  upgrades: Upgrade[];
}

interface Upgrade {
  id: string;
  name: string;
  icon: string;
  race: RaceType;
  category: 'combat' | 'economic' | 'defensive' | 'special';
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

export function UpgradeDetailSettings({ isOpen, onClose, initialRace }: UpgradeDetailSettingsProps) {
  // 종족 상태 관리
  const [selectedRace, setSelectedRace] = useState<RaceKey>(initialRace || RaceType.Protoss);
  
  // 진행률 표기 관련 설정들
  const [showRemainingTime, setShowRemainingTime] = useState(true);
  const [showProgressPercentage, setShowProgressPercentage] = useState(true);
  const [showProgressBar, setShowProgressBar] = useState(true);
  const [upgradeCompletionAlert, setUpgradeCompletionAlert] = useState(true);

  // 카테고리 및 업그레이드 관리 상태 - 선택된 종족에 맞는 기본 업그레이드로 초기화
  const getDefaultCategory = (race: RaceKey): UpgradeCategory => {
    switch (race) {
      case RaceType.Protoss:
        return {
          id: 'combat_upgrades',
          name: '전투 업그레이드',
          upgrades: [
            { id: 'ground_weapons', name: '지상 무기', icon: '⚔️', race: RaceType.Protoss, category: 'combat' },
            { id: 'ground_armor', name: '지상 방어', icon: '🛡️', race: RaceType.Protoss, category: 'combat' }
          ]
        };
      case RaceType.Terran:
        return {
          id: 'combat_upgrades',
          name: '전투 업그레이드',
          upgrades: [
            { id: 'infantry_weapons', name: '보병 무기', icon: '🔫', race: RaceType.Terran, category: 'combat' },
            { id: 'infantry_armor', name: '보병 방어', icon: '🛡️', race: RaceType.Terran, category: 'combat' }
          ]
        };
      case RaceType.Zerg:
        return {
          id: 'combat_upgrades',
          name: '전투 업그레이드',
          upgrades: [
            { id: 'melee_attacks', name: '근접 공격', icon: '🦷', race: RaceType.Zerg, category: 'combat' },
            { id: 'missile_attacks', name: '미사일 공격', icon: '🏹', race: RaceType.Zerg, category: 'combat' }
          ]
        };
    }
  };

  const [categories, setCategories] = useState<UpgradeCategory[]>([getDefaultCategory(selectedRace)]);

  const [showUpgradeSelector, setShowUpgradeSelector] = useState(false);
  const [selectedCategoryId, setSelectedCategoryId] = useState<string>('');
  const [newCategoryName, setNewCategoryName] = useState('');
  const [showAddCategory, setShowAddCategory] = useState(false);
  const [editingCategoryId, setEditingCategoryId] = useState<string>('');
  const [editingCategoryName, setEditingCategoryName] = useState('');
  const [searchTerm, setSearchTerm] = useState('');

  // 사용 가능한 모든 업그레이드들
  const availableUpgrades: Upgrade[] = [
    // Protoss
    { id: 'ground_weapons_1', name: '지상 무기 1단계', icon: '⚔️', race: RaceType.Protoss, category: 'combat' },
    { id: 'ground_weapons_2', name: '지상 무기 2단계', icon: '⚔️', race: RaceType.Protoss, category: 'combat' },
    { id: 'ground_weapons_3', name: '지상 무기 3단계', icon: '⚔️', race: RaceType.Protoss, category: 'combat' },
    { id: 'ground_armor_1', name: '지상 방어 1단계', icon: '🛡️', race: RaceType.Protoss, category: 'combat' },
    { id: 'ground_armor_2', name: '지상 방어 2단계', icon: '🛡️', race: RaceType.Protoss, category: 'combat' },
    { id: 'ground_armor_3', name: '지상 방어 3단계', icon: '🛡️', race: RaceType.Protoss, category: 'combat' },
    { id: 'air_weapons_1', name: '공중 무기 1단계', icon: '✈️', race: RaceType.Protoss, category: 'combat' },
    { id: 'air_weapons_2', name: '공중 무기 2단계', icon: '✈️', race: RaceType.Protoss, category: 'combat' },
    { id: 'air_weapons_3', name: '공중 무기 3단계', icon: '✈️', race: RaceType.Protoss, category: 'combat' },
    { id: 'shields_1', name: '실드 1단계', icon: '⚡', race: RaceType.Protoss, category: 'defensive' },
    { id: 'shields_2', name: '실드 2단계', icon: '⚡', race: RaceType.Protoss, category: 'defensive' },
    { id: 'shields_3', name: '실드 3단계', icon: '⚡', race: RaceType.Protoss, category: 'defensive' },
    { id: 'leg_enhancement', name: '질럿 다리 강화', icon: '🦵', race: RaceType.Protoss, category: 'special' },
    { id: 'range_upgrade', name: '드라군 사정거리', icon: '🎯', race: RaceType.Protoss, category: 'special' },
    
    // Terran
    { id: 'infantry_weapons_1', name: '보병 무기 1단계', icon: '🔫', race: RaceType.Terran, category: 'combat' },
    { id: 'infantry_weapons_2', name: '보병 무기 2단계', icon: '🔫', race: RaceType.Terran, category: 'combat' },
    { id: 'infantry_weapons_3', name: '보병 무기 3단계', icon: '🔫', race: RaceType.Terran, category: 'combat' },
    { id: 'infantry_armor_1', name: '보병 방어 1단계', icon: '🛡️', race: RaceType.Terran, category: 'combat' },
    { id: 'infantry_armor_2', name: '보병 방어 2단계', icon: '🛡️', race: RaceType.Terran, category: 'combat' },
    { id: 'infantry_armor_3', name: '보병 방어 3단계', icon: '🛡️', race: RaceType.Terran, category: 'combat' },
    { id: 'vehicle_weapons_1', name: '차량 무기 1단계', icon: '🚗', race: RaceType.Terran, category: 'combat' },
    { id: 'vehicle_weapons_2', name: '차량 무기 2단계', icon: '🚗', race: RaceType.Terran, category: 'combat' },
    { id: 'vehicle_weapons_3', name: '차량 무기 3단계', icon: '🚗', race: RaceType.Terran, category: 'combat' },
    { id: 'ship_weapons_1', name: '함선 무기 1단계', icon: '🚢', race: RaceType.Terran, category: 'combat' },
    { id: 'stim_packs', name: '스팀팩', icon: '💉', race: RaceType.Terran, category: 'special' },
    { id: 'siege_mode', name: '시즈 모드', icon: '🎯', race: RaceType.Terran, category: 'special' },
    
    // Zerg
    { id: 'melee_attacks_1', name: '근접 공격 1단계', icon: '🦷', race: RaceType.Zerg, category: 'combat' },
    { id: 'melee_attacks_2', name: '근접 공격 2단계', icon: '🦷', race: RaceType.Zerg, category: 'combat' },
    { id: 'melee_attacks_3', name: '근접 공격 3단계', icon: '🦷', race: RaceType.Zerg, category: 'combat' },
    { id: 'missile_attacks_1', name: '미사일 공격 1단계', icon: '🏹', race: RaceType.Zerg, category: 'combat' },
    { id: 'missile_attacks_2', name: '미사일 공격 2단계', icon: '🏹', race: RaceType.Zerg, category: 'combat' },
    { id: 'missile_attacks_3', name: '미사일 공격 3단계', icon: '🏹', race: RaceType.Zerg, category: 'combat' },
    { id: 'carapace_1', name: '갑피 1단계', icon: '🛡️', race: RaceType.Zerg, category: 'defensive' },
    { id: 'carapace_2', name: '갑피 2단계', icon: '🛡️', race: RaceType.Zerg, category: 'defensive' },
    { id: 'carapace_3', name: '갑피 3단계', icon: '🛡️', race: RaceType.Zerg, category: 'defensive' },
    { id: 'metabolic_boost', name: '대사 촉진', icon: '⚡', race: RaceType.Zerg, category: 'special' },
    { id: 'adrenal_glands', name: '부신', icon: '💪', race: RaceType.Zerg, category: 'special' },
    { id: 'burrow', name: '굴파기', icon: '🕳️', race: RaceType.Zerg, category: 'special' }
  ];

  const progressDisplaySettings = [
    {
      id: 'remainingTime',
      title: '잔여 시간 표기',
      description: '업그레이드가 언제 끝날지 남은 시간을 숫자로 보여드려요',
      state: showRemainingTime,
      setState: setShowRemainingTime,
      icon: Clock
    },
    {
      id: 'progressPercentage',
      title: '진행률 표기',
      description: '업그레이드가 얼마나 진행됐는지 퍼센트로 알려드려요',
      state: showProgressPercentage,
      setState: setShowProgressPercentage,
      icon: Target
    },
    {
      id: 'progressBar',
      title: '프로그레스바 표기',
      description: '업그레이드 진행 상황을 예쁜 막대그래프로 보여드려요',
      state: showProgressBar,
      setState: setShowProgressBar,
      icon: BarChart
    },
    {
      id: 'completionAlert',
      title: '업그레이드 완료 알림',
      description: '업그레이드가 끝나면 반짝여서 완료됐다고 알려드려요',
      state: upgradeCompletionAlert,
      setState: setUpgradeCompletionAlert,
      icon: Bell
    }
  ];

  const handleAddCategory = () => {
    if (newCategoryName.trim()) {
      const newCategory: UpgradeCategory = {
        id: Date.now().toString(),
        name: newCategoryName.trim(),
        upgrades: []
      };
      setCategories([...categories, newCategory]);
      setNewCategoryName('');
      setShowAddCategory(false);
    }
  };

  const handleDeleteCategory = (categoryId: string) => {
    setCategories(categories.filter(cat => cat.id !== categoryId));
  };

  const handleEditCategory = (categoryId: string, newName: string) => {
    if (newName.trim()) {
      setCategories(categories.map(cat => 
        cat.id === categoryId ? { ...cat, name: newName.trim() } : cat
      ));
    }
    setEditingCategoryId('');
    setEditingCategoryName('');
  };

  const handleCategoryDoubleClick = (category: UpgradeCategory) => {
    setEditingCategoryId(category.id);
    setEditingCategoryName(category.name);
  };

  const handleAddUpgradeToCategory = (categoryId: string) => {
    setSelectedCategoryId(categoryId);
    setShowUpgradeSelector(true);
  };

  const handleSelectUpgrade = (upgrade: Upgrade) => {
    if (selectedCategoryId) {
      setCategories(categories.map(cat => 
        cat.id === selectedCategoryId 
          ? { ...cat, upgrades: [...cat.upgrades.filter(u => u.id !== upgrade.id), upgrade] }
          : cat
      ));
      setShowUpgradeSelector(false);
      setSelectedCategoryId('');
      setSearchTerm('');
    }
  };

  const handleRemoveUpgradeFromCategory = (categoryId: string, upgradeId: string) => {
    setCategories(categories.map(cat =>
      cat.id === categoryId
        ? { ...cat, upgrades: cat.upgrades.filter(u => u.id !== upgradeId) }
        : cat
    ));
  };

  // 선택된 종족의 업그레이드만 필터링하고 검색어도 적용
  const filteredUpgrades = availableUpgrades.filter(upgrade =>
    upgrade.race === selectedRace && 
    upgrade.name.toLowerCase().includes(searchTerm.toLowerCase())
  );

  // initialRace가 변경될 때마다 selectedRace 업데이트 및 카테고리 초기화
  useEffect(() => {
    if (initialRace && initialRace !== selectedRace) {
      const previousRace = selectedRace;
      setSelectedRace(initialRace);
      // 종족이 변경되면 카테고리를 새 종족의 기본 카테고리로 초기화
      setCategories([getDefaultCategory(initialRace)]);
      console.log(`업그레이드 설정 종족 변경: ${RACES[previousRace]?.name || '없음'} → ${RACES[initialRace].name}, 카테고리 초기화`);
    }
  }, [initialRace, selectedRace]);

  const handleSave = () => {
    const settingsToSave = {
      categories,
      showRemainingTime,
      showProgressPercentage,
      showProgressBar,
      upgradeCompletionAlert
    };
    
    console.log('업그레이드 설정 저장:', settingsToSave);
    onClose();
  };

  if (!isOpen) return null;

  return (
    <div className="h-screen overflow-hidden border-2 shadow-2xl"
      style={{
        backgroundColor: 'var(--starcraft-bg)',
        background: 'linear-gradient(135deg, var(--starcraft-bg) 0%, rgba(0, 20, 0, 0.95) 100%)',
        borderColor: 'var(--starcraft-green)',
        boxShadow: '0 0 30px rgba(0, 255, 0, 0.4), inset 0 0 30px rgba(0, 255, 0, 0.1)'
      }}
    >
      {/* 전체 화면 컨테이너 */}
      <div 
        className="flex flex-col h-full"
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
                {categories.map((category) => (
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
                      {category.upgrades.map((upgrade) => (
                        <div key={upgrade.id} 
                          className="relative group aspect-square flex flex-col items-center justify-center rounded-lg border-2 cursor-pointer transition-all duration-300 hover:border-opacity-60 p-1"
                          style={{
                            backgroundColor: 'var(--starcraft-bg-active)',
                            borderColor: RACES[selectedRace].color
                          }}
                        >
                          <span className="text-lg mb-1">{upgrade.icon}</span>
                          <span className="text-xs text-center leading-tight" 
                            style={{ color: RACES[selectedRace].color }}>
                            {upgrade.name}
                          </span>
                          
                          {/* 호버 시 삭제 버튼 */}
                          <button
                            onClick={() => handleRemoveUpgradeFromCategory(category.id, upgrade.id)}
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
                        className="aspect-square flex items-center justify-center rounded-lg border-2 border-dashed transition-all duration-300 hover:border-opacity-60 hover:bg-opacity-10"
                        style={{
                          borderColor: RACES[selectedRace].color,
                          color: RACES[selectedRace].color,
                          '--hover-bg': `${RACES[selectedRace].color}10`
                        } as React.CSSProperties}
                      >
                        <Plus className="w-8 h-8" />
                      </button>
                    </div>

                    {category.upgrades.length === 0 && (
                      <p className="text-sm opacity-60 mt-4 text-center" style={{ color: RACES[selectedRace].color }}>
                        + 버튼을 눌러 {RACES[selectedRace].name} 업그레이드를 추가하세요
                      </p>
                    )}
                  </div>
                ))}
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
                <li>• 카테고리 이름을 더블클릭하면 편집할 수 있습니다</li>
                <li>• 업그레이드 아이콘에 마우스를 올리면 삭제 버튼이 나타납니다</li>
                <li>• 진행률 표기는 실시간으로 게임 상태와 동기화됩니다</li>
                <li>• 완료 알림은 업그레이드 완료 시 화면에 표시됩니다</li>
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
            className="flex items-center gap-2 px-6 py-2 rounded-sm border transition-all duration-300 hover:bg-green-500/20"
            style={{
              color: 'var(--starcraft-green)',
              borderColor: 'var(--starcraft-green)',
              backgroundColor: 'var(--starcraft-bg-active)'
            }}
          >
            설정 완료
          </button>
        </div>
      </div>

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
                  placeholder={`${RACES[selectedRace].name} 업그레이드 검색...`}
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
              <div className="grid grid-cols-6 sm:grid-cols-8 md:grid-cols-10 gap-3">
                {filteredUpgrades.map((upgrade) => (
                  <button
                    key={upgrade.id}
                    onClick={() => handleSelectUpgrade(upgrade)}
                    className="aspect-square flex flex-col items-center justify-center rounded-lg border-2 transition-all duration-300 hover:border-opacity-80 p-2"
                    style={{
                      backgroundColor: 'var(--starcraft-bg-secondary)',
                      borderColor: RACES[selectedRace].color,
                      color: RACES[selectedRace].color
                    }}
                  >
                    <span className="text-xl mb-1">{upgrade.icon}</span>
                    <span className="text-xs text-center leading-tight">
                      {upgrade.name}
                    </span>
                  </button>
                ))}
              </div>
              
              {filteredUpgrades.length === 0 && (
                <div className="text-center py-8">
                  <p className="text-sm opacity-60" style={{ color: 'var(--starcraft-green)' }}>
                    검색 결과가 없습니다
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