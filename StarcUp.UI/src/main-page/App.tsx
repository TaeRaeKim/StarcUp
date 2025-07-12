import { useState, useEffect } from "react";
import { MainInterface } from "@/components/MainInterface";
import { PresetSettingsModal } from "@/components/PresetSettingsModal";
import { PopulationDetailSettings } from "@/components/PopulationDetailSettings";
import { WorkerDetailSettings } from "@/components/WorkerDetailSettings";
import { UnitDetailSettings } from "@/components/UnitDetailSettings";
import { UpgradeDetailSettings } from "@/components/UpgradeDetailSettings";
import { BuildOrderDetailSettings } from "@/components/BuildOrderDetailSettings";

// 게임 상태 타입 정의
type GameStatus = 'playing' | 'waiting' | 'error';

// 프리셋 타입 정의
interface Preset {
  id: string;
  name: string;
  description: string;
  featureStates: boolean[];
  selectedRace: 'protoss' | 'terran' | 'zerg';
}

// 프리셋 데이터 (상태로 관리하여 수정 가능하게 함)
const initialPresets: Preset[] = [
  {
    id: "preset1",
    name: "공발질-8겟뽕",
    description: "공중 발업 질럿 러쉬 + 8마리 겟뽕",
    featureStates: [true, true, true, false, false], // 인구수, 일꾼, 유닛, 업그레이드, 빌드오더(비활성화)
    selectedRace: 'protoss'
  },
  {
    id: "preset2", 
    name: "커공발-운영",
    description: "커세어 + 공중 발업 운영 빌드",
    featureStates: [true, true, false, true, false], // 인구수, 일꾼, 유닛, 업그레이드, 빌드오더(비활성화)
    selectedRace: 'terran'
  },
  {
    id: "preset3",
    name: "패닼아비터",
    description: "패스트 다크템플러 + 아비터 전략",
    featureStates: [false, true, true, true, false], // 인구수, 일꾼, 유닛, 업그레이드, 빌드오더(비활성화)
    selectedRace: 'protoss'
  }
];

export default function App() {
  const [isActive, setIsActive] = useState(false);
  const [gameStatus, setGameStatus] = useState<GameStatus>('error');
  const [currentPresetIndex, setCurrentPresetIndex] = useState(0);
  const [presets, setPresets] = useState(initialPresets);
  
  // 모달 상태 관리
  const [showPresetSettings, setShowPresetSettings] = useState(false);
  const [showPopulationSettings, setShowPopulationSettings] = useState(false);
  const [showWorkerSettings, setShowWorkerSettings] = useState(false);
  const [showUnitSettings, setShowUnitSettings] = useState(false);
  const [showUpgradeSettings, setShowUpgradeSettings] = useState(false);
  const [showBuildOrderSettings, setShowBuildOrderSettings] = useState(false);

  // 현재 편집 중인 종족 상태 (실시간 동기화용)
  const [currentEditingRace, setCurrentEditingRace] = useState<'protoss' | 'terran' | 'zerg' | null>(null);

  // 게임 상태 이벤트 리스너
  useEffect(() => {
    if (!window.coreAPI?.onGameStatusChanged) {
      console.log('⚠️ coreAPI가 준비되지 않았습니다');
      return;
    }

    console.log('🎮 게임 상태 이벤트 리스너 등록');
    
    const unsubscribe = window.coreAPI.onGameStatusChanged((data) => {
      console.log('📡 게임 상태 변경 수신:', data.status);
      
      // Core 상태를 UI 상태로 매핑
      switch (data.status) {
        case 'playing':
          setGameStatus('playing');
          break;
        case 'waiting':
          setGameStatus('waiting');
          break;
        default:
          setGameStatus('error');
          break;
      }
    });

    // 컴포넌트 언마운트 시 이벤트 리스너 정리
    return () => {
      console.log('🧹 게임 상태 이벤트 리스너 정리');
      unsubscribe();
    };
  }, []);

  const toggleOverlay = async () => {
    const newState = !isActive;
    
    if (newState) {
      // 즉시 활성화 상태로 변경
      setIsActive(true);
      setGameStatus('error'); // 게임 감지 안됨 상태 (초기 상태)
      window.electronAPI?.showOverlay();
      
      // 백그라운드에서 Core 게임 감지 시작
      try {
        const response = await window.coreAPI?.startDetection();
        if (response?.success) {
          console.log('Core 게임 감지 시작됨:', response.data);
        } else {
          console.error('Core 게임 감지 시작 실패:', response?.error);
          // 실패 시 버튼 비활성화
          setIsActive(false);
          setGameStatus('error');
          window.electronAPI?.hideOverlay();
        }
      } catch (error) {
        console.error('Core 통신 실패:', error);
        // 통신 실패 시 버튼 비활성화
        setIsActive(false);
        setGameStatus('error');
        window.electronAPI?.hideOverlay();
      }
    } else {
      // 즉시 비활성화 상태로 변경
      setIsActive(false);
      setGameStatus('error'); // 게임 감지 안됨 상태
      window.electronAPI?.hideOverlay();
      
      // 백그라운드에서 Core 게임 감지 중지
      try {
        const response = await window.coreAPI?.stopDetection();
        if (response?.success) {
          console.log('Core 게임 감지 중지됨:', response.data);
        } else {
          console.error('Core 게임 감지 중지 실패:', response?.error);
        }
      } catch (error) {
        console.error('Core 통신 실패:', error);
      }
    }
  };

  const currentPreset = presets[currentPresetIndex];

  // 프리셋 관련 핸들러
  const handlePresetIndexChange = (index: number) => {
    setCurrentPresetIndex(index);
    // 프리셋 변경 시 편집 중인 종족 상태 초기화
    setCurrentEditingRace(null);
  };

  const handleSavePreset = (updatedPreset: {
    id: string;
    name: string;
    description: string;
    featureStates: boolean[];
    selectedRace?: 'protoss' | 'terran' | 'zerg';
  }) => {
    console.log('프리셋 저장 완료:', updatedPreset.name, '종족:', updatedPreset.selectedRace);
    setPresets(prev => prev.map(preset => 
      preset.id === updatedPreset.id ? { ...preset, ...updatedPreset } : preset
    ));
    // 저장 후 편집 중인 종족 상태 초기화
    setCurrentEditingRace(null);
  };

  // 프리셋 설정 모달 핸들러
  const handleOpenPresetSettings = () => {
    // 프리셋 설정을 열 때 현재 프리셋의 종족으로 편집 상태 초기화
    setCurrentEditingRace(currentPreset.selectedRace || 'protoss');
    setShowPresetSettings(true);
  };

  const handleClosePresetSettings = () => {
    // 프리셋 설정을 닫을 때 편집 중인 종족 상태 초기화
    setCurrentEditingRace(null);
    setShowPresetSettings(false);
  };

  // 종족 실시간 변경 핸들러
  const handleRaceChange = (race: 'protoss' | 'terran' | 'zerg') => {
    console.log('실시간 종족 변경:', race);
    setCurrentEditingRace(race);
  };

  // 인구수 설정 모달 핸들러
  const handleOpenPopulationSettings = () => {
    setShowPopulationSettings(true);
  };

  const handleClosePopulationSettings = () => {
    setShowPopulationSettings(false);
  };

  // 일꾼 설정 모달 핸들러
  const handleOpenWorkerSettings = () => {
    setShowWorkerSettings(true);
  };

  const handleCloseWorkerSettings = () => {
    setShowWorkerSettings(false);
  };

  // 유닛 설정 모달 핸들러
  const handleOpenUnitSettings = () => {
    setShowUnitSettings(true);
  };

  const handleCloseUnitSettings = () => {
    setShowUnitSettings(false);
  };

  // 업그레이드 설정 모달 핸들러
  const handleOpenUpgradeSettings = () => {
    setShowUpgradeSettings(true);
  };

  const handleCloseUpgradeSettings = () => {
    setShowUpgradeSettings(false);
  };

  // 빌드 오더 설정 모달 핸들러
  const handleOpenBuildOrderSettings = () => {
    setShowBuildOrderSettings(true);
  };

  const handleCloseBuildOrderSettings = () => {
    setShowBuildOrderSettings(false);
  };

  return (
    <>
      {/* 메인 인터페이스 */}
      <MainInterface
        presets={presets}
        currentPresetIndex={currentPresetIndex}
        onPresetIndexChange={handlePresetIndexChange}
        onOpenPresetSettings={handleOpenPresetSettings}
        isActive={isActive}
        gameStatus={gameStatus}
        onToggleOverlay={toggleOverlay}
      />

      {/* 프리셋 설정 모달 */}
      <PresetSettingsModal
        isOpen={showPresetSettings}
        onClose={handleClosePresetSettings}
        currentPreset={currentPreset}
        onSave={handleSavePreset}
        onRaceChange={handleRaceChange}
        onOpenPopulationSettings={handleOpenPopulationSettings}
        onOpenWorkerSettings={handleOpenWorkerSettings}
        onOpenUnitSettings={handleOpenUnitSettings}
        onOpenUpgradeSettings={handleOpenUpgradeSettings}
        onOpenBuildOrderSettings={handleOpenBuildOrderSettings}
      />

      {/* 기능별 상세 설정 모달들 */}
      <PopulationDetailSettings
        isOpen={showPopulationSettings}
        onClose={handleClosePopulationSettings}
        initialRace={currentEditingRace || currentPreset.selectedRace}
      />

      <WorkerDetailSettings
        isOpen={showWorkerSettings}
        onClose={handleCloseWorkerSettings}
      />

      <UnitDetailSettings
        isOpen={showUnitSettings}
        onClose={handleCloseUnitSettings}
        initialRace={currentEditingRace || currentPreset.selectedRace}
      />

      <UpgradeDetailSettings
        isOpen={showUpgradeSettings}
        onClose={handleCloseUpgradeSettings}
        initialRace={currentEditingRace || currentPreset.selectedRace}
      />

      <BuildOrderDetailSettings
        isOpen={showBuildOrderSettings}
        onClose={handleCloseBuildOrderSettings}
      />
    </>
  );
}