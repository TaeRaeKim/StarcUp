import React, { useState, useEffect } from 'react';
import { ArrowLeft, Users, Plus, X, Skull, Zap, Clock, Info, Search, Shield, Home, Building2 } from 'lucide-react';
import { RaceType, RACE_NAMES } from '../../types/enums';

interface UnitDetailSettingsProps {
  isOpen: boolean;
  onClose: () => void;
  initialRace?: RaceType;
}

interface UnitCategory {
  id: string;
  name: string;
  units: Unit[];
}

interface Unit {
  id: string;
  name: string;
  icon: string;
  race: RaceType;
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

export function UnitDetailSettings({ isOpen, onClose, initialRace }: UnitDetailSettingsProps) {
  // 종족 상태 관리
  const [selectedRace, setSelectedRace] = useState<RaceKey>(initialRace || RaceType.Protoss);
  
  // 기본 설정 상태들
  const [unitDeathDetection, setUnitDeathDetection] = useState(true);
  const [unitProductionDetection, setUnitProductionDetection] = useState(true);
  const [includeUnitsInProgress, setIncludeUnitsInProgress] = useState(false);

  // 카테고리 및 유닛 관리 상태 - 선택된 종족에 맞는 기본 유닛으로 초기화
  const getDefaultCategory = (race: RaceKey): UnitCategory => {
    switch (race) {
      case RaceType.Protoss:
        return {
          id: 'main_army',
          name: '주력 부대',
          units: [
            { id: 'zealot', name: '질럿', icon: '⚔️', race: RaceType.Protoss },
            { id: 'dragoon', name: '드라군', icon: '🔫', race: RaceType.Protoss }
          ]
        };
      case RaceType.Terran:
        return {
          id: 'main_army',
          name: '주력 부대',
          units: [
            { id: 'marine', name: '마린', icon: '🎯', race: RaceType.Terran },
            { id: 'tank', name: '탱크', icon: '🚗', race: RaceType.Terran }
          ]
        };
      case RaceType.Zerg:
        return {
          id: 'main_army',
          name: '주력 부대',
          units: [
            { id: 'zergling', name: '저글링', icon: '🦎', race: RaceType.Zerg },
            { id: 'hydralisk', name: '히드라리스크', icon: '🐍', race: RaceType.Zerg }
          ]
        };
    }
  };

  const [categories, setCategories] = useState<UnitCategory[]>([getDefaultCategory(selectedRace)]);

  const [showUnitSelector, setShowUnitSelector] = useState(false);
  const [selectedCategoryId, setSelectedCategoryId] = useState<string>('');
  const [newCategoryName, setNewCategoryName] = useState('');
  const [showAddCategory, setShowAddCategory] = useState(false);
  const [editingCategoryId, setEditingCategoryId] = useState<string>('');
  const [editingCategoryName, setEditingCategoryName] = useState('');
  const [searchTerm, setSearchTerm] = useState('');

  // 사용 가능한 모든 유닛들
  const availableUnits: Unit[] = [
    // Protoss
    { id: 'probe', name: '탐사정', icon: '🔧', race: RaceType.Protoss },
    { id: 'zealot', name: '질럿', icon: '⚔️', race: RaceType.Protoss },
    { id: 'dragoon', name: '드라군', icon: '🔫', race: RaceType.Protoss },
    { id: 'high_templar', name: '하이템플러', icon: '⚡', race: RaceType.Protoss },
    { id: 'dark_templar', name: '다크템플러', icon: '🗡️', race: RaceType.Protoss },
    { id: 'archon', name: '아콘', icon: '🔮', race: RaceType.Protoss },
    { id: 'reaver', name: '리버', icon: '💥', race: RaceType.Protoss },
    { id: 'observer', name: '옵저버', icon: '👁️', race: RaceType.Protoss },
    { id: 'shuttle', name: '셔틀', icon: '🚁', race: RaceType.Protoss },
    { id: 'scout', name: '스카우트', icon: '✈️', race: RaceType.Protoss },
    { id: 'corsair', name: '커세어', icon: '🛩️', race: RaceType.Protoss },
    { id: 'carrier', name: '캐리어', icon: '🚢', race: RaceType.Protoss },
    { id: 'arbiter', name: '아비터', icon: '🌀', race: RaceType.Protoss },
    
    // Terran
    { id: 'scv', name: 'SCV', icon: '🔨', race: RaceType.Terran },
    { id: 'marine', name: '마린', icon: '🎯', race: RaceType.Terran },
    { id: 'firebat', name: '파이어뱃', icon: '🔥', race: RaceType.Terran },
    { id: 'ghost', name: '고스트', icon: '👻', race: RaceType.Terran },
    { id: 'vulture', name: '벌처', icon: '🏍️', race: RaceType.Terran },
    { id: 'tank', name: '탱크', icon: '🚗', race: RaceType.Terran },
    { id: 'goliath', name: '골리앗', icon: '🤖', race: RaceType.Terran },
    { id: 'wraith', name: '레이스', icon: '👤', race: RaceType.Terran },
    { id: 'dropship', name: '드랍쉽', icon: '🚁', race: RaceType.Terran },
    { id: 'valkyrie', name: '발키리', icon: '💫', race: RaceType.Terran },
    { id: 'battlecruiser', name: '배틀크루저', icon: '⚓', race: RaceType.Terran },
    
    // Zerg
    { id: 'drone', name: '드론', icon: '🐛', race: RaceType.Zerg },
    { id: 'zergling', name: '저글링', icon: '🦎', race: RaceType.Zerg },
    { id: 'hydralisk', name: '히드라리스크', icon: '🐍', race: RaceType.Zerg },
    { id: 'lurker', name: '러커', icon: '🕷️', race: RaceType.Zerg },
    { id: 'ultralisk', name: '울트라리스크', icon: '🦏', race: RaceType.Zerg },
    { id: 'defiler', name: '디파일러', icon: '🦠', race: RaceType.Zerg },
    { id: 'mutalisk', name: '뮤탈리스크', icon: '🦇', race: RaceType.Zerg },
    { id: 'scourge', name: '스커지', icon: '💀', race: RaceType.Zerg },
    { id: 'queen', name: '퀸', icon: '👑', race: RaceType.Zerg },
    { id: 'guardian', name: '가디언', icon: '🐉', race: RaceType.Zerg },
    { id: 'devourer', name: '디바우어러', icon: '🦈', race: RaceType.Zerg }
  ];

  const basicSettings = [
    {
      id: 'unitProduction',
      title: '유닛 생산 감지',
      description: '새로운 유닛이 태어날 때마다 파란색으로 반짝여서 알려드려요',
      state: unitProductionDetection,
      setState: setUnitProductionDetection,
      icon: Zap
    },
    {
      id: 'unitDeath',
      title: '유닛 사망 감지',
      description: '소중한 유닛이 전사했을 때 빨간색으로 경고해드려요',
      state: unitDeathDetection,
      setState: setUnitDeathDetection,
      icon: Skull
    },
    {
      id: 'unitsInProgress',
      title: '생산 중인 유닛 수 포함',
      description: '아직 완성되지 않은 유닛도 숫자에 포함해서 계산해요',
      state: includeUnitsInProgress,
      setState: setIncludeUnitsInProgress,
      icon: Clock
    }
  ];

  const handleAddCategory = () => {
    if (newCategoryName.trim()) {
      const newCategory: UnitCategory = {
        id: Date.now().toString(),
        name: newCategoryName.trim(),
        units: []
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

  const handleCategoryDoubleClick = (category: UnitCategory) => {
    setEditingCategoryId(category.id);
    setEditingCategoryName(category.name);
  };

  const handleAddUnitToCategory = (categoryId: string) => {
    setSelectedCategoryId(categoryId);
    setShowUnitSelector(true);
  };

  const handleSelectUnit = (unit: Unit) => {
    if (selectedCategoryId) {
      setCategories(categories.map(cat => 
        cat.id === selectedCategoryId 
          ? { ...cat, units: [...cat.units.filter(u => u.id !== unit.id), unit] }
          : cat
      ));
      setShowUnitSelector(false);
      setSelectedCategoryId('');
      setSearchTerm('');
    }
  };

  const handleRemoveUnitFromCategory = (categoryId: string, unitId: string) => {
    setCategories(categories.map(cat =>
      cat.id === categoryId
        ? { ...cat, units: cat.units.filter(u => u.id !== unitId) }
        : cat
    ));
  };

  // 선택된 종족의 유닛만 필터링하고 검색어도 적용
  const filteredUnits = availableUnits.filter(unit =>
    unit.race === selectedRace && 
    unit.name.toLowerCase().includes(searchTerm.toLowerCase())
  );

  // initialRace가 변경될 때마다 selectedRace 업데이트 및 카테고리 초기화
  useEffect(() => {
    if (initialRace && initialRace !== selectedRace) {
      const previousRace = selectedRace;
      setSelectedRace(initialRace);
      // 종족이 변경되면 카테고리를 새 종족의 기본 카테고리로 초기화
      setCategories([getDefaultCategory(initialRace)]);
      console.log(`유닛 설정 종족 변경: ${RACES[previousRace]?.name || '없음'} → ${RACES[initialRace].name}, 카테고리 초기화`);
    }
  }, [initialRace, selectedRace]);

  const handleSave = () => {
    const settingsToSave = {
      categories,
      unitDeathDetection,
      unitProductionDetection,
      includeUnitsInProgress
    };
    
    console.log('유닛 설정 저장:', settingsToSave);
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
                유닛 수 설정
              </h1>
              <p 
                className="text-sm opacity-70"
                style={{ color: 'var(--starcraft-green)' }}
              >
                유닛 카테고리와 관련 기능들을 설정하세요
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
                {RACES[selectedRace].name} 유닛만 선택할 수 있습니다.
              </div>
            </div>

            {/* 카테고리 관리 */}
            <div className="space-y-4">
              <div className="flex items-center justify-between">
                <h2 
                  className="text-lg font-medium tracking-wide flex items-center gap-2"
                  style={{ color: 'var(--starcraft-green)' }}
                >
                  <Users className="w-5 h-5" />
                  유닛 카테고리
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

                    {/* 유닛 그리드 */}
                    <div className="grid grid-cols-6 sm:grid-cols-8 md:grid-cols-10 gap-3">
                      {category.units.map((unit) => (
                        <div key={unit.id} 
                          className="relative group aspect-square flex flex-col items-center justify-center rounded-lg border-2 cursor-pointer transition-all duration-300 hover:border-opacity-60 p-1"
                          style={{
                            backgroundColor: 'var(--starcraft-bg-active)',
                            borderColor: RACES[selectedRace].color
                          }}
                        >
                          <span className="text-lg mb-1">{unit.icon}</span>
                          <span className="text-xs text-center leading-tight" 
                            style={{ color: RACES[selectedRace].color }}>
                            {unit.name}
                          </span>
                          
                          {/* 호버 시 삭제 버튼 */}
                          <button
                            onClick={() => handleRemoveUnitFromCategory(category.id, unit.id)}
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
                        onClick={() => handleAddUnitToCategory(category.id)}
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

                    {category.units.length === 0 && (
                      <p className="text-sm opacity-60 mt-4 text-center" style={{ color: RACES[selectedRace].color }}>
                        + 버튼을 눌러 {RACES[selectedRace].name} 유닛을 추가하세요
                      </p>
                    )}
                  </div>
                ))}
              </div>
            </div>

            {/* 기본 설정들 */}
            <div className="space-y-4">
              <h2 
                className="text-lg font-medium tracking-wide flex items-center gap-2"
                style={{ color: 'var(--starcraft-green)' }}
              >
                <Users className="w-5 h-5" />
                기본 설정
              </h2>
              
              <div className="space-y-4">
                {basicSettings.map((item) => {
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
                <li>• 유닛 아이콘에 마우스를 올리면 삭제 버튼이 나타납니다</li>
                <li>• 사망 감지는 붉은색, 생산 감지는 파란색으로 표시됩니다</li>
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

      {/* 유닛 선택 모달 */}
      {showUnitSelector && (
        <div className="fixed inset-0 z-60 flex items-center justify-center p-4">
          <div 
            className="absolute inset-0 backdrop-blur-sm"
            style={{ backgroundColor: 'rgba(0, 0, 0, 0.9)' }}
            onClick={() => setShowUnitSelector(false)}
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
                  유닛 선택
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
                onClick={() => setShowUnitSelector(false)}
                className="p-2 rounded-sm transition-all duration-300 hover:bg-red-500/20"
                style={{ color: 'var(--starcraft-red)' }}
              >
                <X className="w-5 h-5" />
              </button>
            </div>

            {/* 검색 */}
            <div className="p-4 border-b" style={{ borderBottomColor: 'var(--starcraft-border)' }}>
              <div className="relative">
                <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 w-4 h-4" 
                  style={{ color: 'var(--starcraft-green)' }} 
                />
                <input
                  type="text"
                  value={searchTerm}
                  onChange={(e) => setSearchTerm(e.target.value)}
                  placeholder={`${RACES[selectedRace].name} 유닛 검색...`}
                  className="w-full pl-10 pr-4 py-2 rounded-sm border"
                  style={{
                    backgroundColor: 'var(--starcraft-bg)',
                    borderColor: 'var(--starcraft-border)',
                    color: 'var(--starcraft-green)'
                  }}
                />
              </div>
              
              {/* 유닛 개수 표시 */}
              <div className="mt-2 text-xs opacity-70" style={{ color: 'var(--starcraft-green)' }}>
                {RACES[selectedRace].name} 유닛: {filteredUnits.length}개 
                {searchTerm && ` (검색된: ${filteredUnits.length}개)`}
              </div>
            </div>

            {/* 유닛 그리드 */}
            <div className="p-4 overflow-y-auto max-h-96">
              {filteredUnits.length > 0 ? (
                <div className="grid grid-cols-6 sm:grid-cols-8 md:grid-cols-10 gap-3">
                  {filteredUnits.map((unit) => (
                    <button
                      key={unit.id}
                      onClick={() => handleSelectUnit(unit)}
                      className="aspect-square flex flex-col items-center justify-center rounded-lg border-2 transition-all duration-300 hover:border-opacity-80 p-1"
                      style={{
                        backgroundColor: 'var(--starcraft-bg-secondary)',
                        borderColor: RACES[selectedRace].color,
                        color: RACES[selectedRace].color
                      }}
                      title={unit.name}
                    >
                      <span className="text-lg mb-1">{unit.icon}</span>
                      <span className="text-xs text-center leading-tight" 
                        style={{ color: RACES[selectedRace].color }}>
                        {unit.name}
                      </span>
                    </button>
                  ))}
                </div>
              ) : (
                <div className="text-center py-8">
                  <div className="text-sm opacity-70" style={{ color: 'var(--starcraft-green)' }}>
                    {searchTerm 
                      ? `"${searchTerm}"에 해당하는 ${RACES[selectedRace].name} 유닛이 없습니다.`
                      : `${RACES[selectedRace].name} 유닛이 없습니다.`
                    }
                  </div>
                </div>
              )}
            </div>
          </div>
        </div>
      )}
    </div>
  );
}