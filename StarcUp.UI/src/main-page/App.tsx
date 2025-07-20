import { useState, useEffect } from "react";
import { MainInterface } from "@/components/MainInterface";
import { PresetSettingsModal } from "@/components/PresetSettingsModal";
import { PopulationDetailSettings } from "@/components/PopulationDetailSettings";
import { WorkerDetailSettings } from "@/components/WorkerDetailSettings";
import { UnitDetailSettings } from "@/components/UnitDetailSettings";
import { UpgradeDetailSettings } from "@/components/UpgradeDetailSettings";
import { BuildOrderDetailSettings } from "@/components/BuildOrderDetailSettings";
import { DevelopmentModal } from "@/components/DevelopmentModal";
import { 
  calculateWorkerSettingsMask, 
  type PresetInitMessage, 
  type WorkerPreset,
  type WorkerSettings 
} from "../utils/presetUtils";

// 게임 상태 타입 정의
type GameStatus = 'playing' | 'waiting' | 'error';

// 현재 뷰 타입 정의
type CurrentView = 'main' | 'preset-settings' | 'population-settings' | 'worker-settings' | 'unit-settings' | 'upgrade-settings' | 'build-order-settings' | 'development-progress';

// 뷰별 윈도우 크기 정의 (실제 윈도우는 40px씩 더 크고, DOM은 기존 크기로 중앙 배치)
const VIEW_WINDOW_SIZES = {
  'main': { width: 540, height: 790 },  // 500x750 + 40px 여유
  'preset-settings': { width: 740, height: 840 },      // 700x800 + 40px 여유
  'population-settings': { width: 840, height: 840 },  // 800x800 + 40px 여유
  'worker-settings': { width: 740, height: 840 },      // 700x800 + 40px 여유
  'unit-settings': { width: 740, height: 840 },        // 700x800 + 40px 여유
  'upgrade-settings': { width: 740, height: 840 },     // 700x800 + 40px 여유
  'build-order-settings': { width: 740, height: 840 }, // 700x800 + 40px 여유
  'development-progress': { width: 740, height: 840 }  // 700x800 + 40px 여유
} as const;

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
    name: "Default Preset",
    description: "아직 프리셋 구현 안됨",
    featureStates: [true, false, false, false, false], // 일꾼, 인구수(비활성화), 유닛(비활성화), 업그레이드(비활성화), 빌드오더(비활성화)
    selectedRace: 'protoss'
  },
  {
    id: "preset2", 
    name: "커공발-운영",
    description: "커세어 + 공중 발업 운영 빌드",
    featureStates: [true, false, false, false, false], // 일꾼, 인구수(비활성화), 유닛(비활성화), 업그레이드(비활성화), 빌드오더(비활성화)
    selectedRace: 'terran'
  },
  {
    id: "preset3",
    name: "패닼아비터",
    description: "패스트 다크템플러 + 아비터 전략",
    featureStates: [true, false, false, false, false], // 일꾼, 인구수(비활성화), 유닛(비활성화), 업그레이드(비활성화), 빌드오더(비활성화)
    selectedRace: 'protoss'
  }
];

export default function App() {
  const [isActive, setIsActive] = useState(false);
  const [gameStatus, setGameStatus] = useState<GameStatus>('error');
  const [currentPresetIndex, setCurrentPresetIndex] = useState(0);
  const [presets, setPresets] = useState(initialPresets);
  
  // 현재 뷰 상태 관리 (모달 대신 페이지 전환 방식)
  const [currentView, setCurrentView] = useState<CurrentView>('main');

  // 현재 편집 중인 종족 상태 (실시간 동기화용)
  const [currentEditingRace, setCurrentEditingRace] = useState<'protoss' | 'terran' | 'zerg' | null>(null);

  // 개발 중 기능 상태
  const [developmentFeatureName, setDevelopmentFeatureName] = useState('');
  const [developmentFeatureType, setDevelopmentFeatureType] = useState<'buildorder' | 'upgrade' | 'population' | 'unit'>('buildorder');

  // 윈도우 크기 변경 함수
  const changeWindowSize = (view: CurrentView) => {
    const size = VIEW_WINDOW_SIZES[view];
    if (window.electronAPI?.resizeWindow) {
      window.electronAPI.resizeWindow(size.width, size.height);
      console.log(`윈도우 크기 변경: ${view} → ${size.width}x${size.height}`);
    }
  };

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

  // 자동 overlay 관리는 이제 메인 프로세스에서 처리됩니다

  // 프리셋 초기화 함수 (Named Pipe 연결 후 호출)
  const sendPresetInit = async () => {
    try {
      // 현재 일꾼 설정을 기본값으로 구성 (실제로는 저장된 설정을 불러와야 함)
      const currentWorkerSettings: WorkerSettings = {
        workerCountDisplay: true,           // 일꾼 수 출력 기본 활성화
        includeProducingWorkers: false,     // 생산 중인 일꾼 수 포함 기본 비활성화
        idleWorkerDisplay: true,            // 유휴 일꾼 수 출력 기본 활성화
        workerProductionDetection: true,    // 일꾼 생산 감지 기본 활성화
        workerDeathDetection: true,         // 일꾼 사망 감지 기본 활성화
        gasWorkerCheck: true                // 가스 일꾼 체크 기본 활성화
      };

      const workerMask = calculateWorkerSettingsMask(currentWorkerSettings);
      
      const initMessage: PresetInitMessage = {
        type: 'preset-init',
        timestamp: Date.now(),
        presets: {
          worker: {
            enabled: currentPreset.featureStates[0], // 일꾼 기능 활성화 여부
            settingsMask: workerMask
          } as WorkerPreset
          // 향후 다른 프리셋들도 여기에 추가
        }
      };

      console.log('🚀 프리셋 초기화 메시지 전송:', initMessage);
      
      if (window.coreAPI?.sendPresetInit) {
        const response = await window.coreAPI.sendPresetInit(initMessage);
        
        if (response?.success) {
          console.log('✅ 프리셋 초기화 성공:', response.data);
        } else {
          console.error('❌ 프리셋 초기화 실패:', response?.error);
        }
      } else {
        console.warn('⚠️ coreAPI.sendPresetInit 함수가 사용 불가능합니다');
      }
    } catch (error) {
      console.error('💥 프리셋 초기화 중 오류 발생:', error);
    }
  };

  const toggleOverlay = async () => {
    const newState = !isActive;
    
    if (newState) {
      // 활성화 상태로 변경
      setIsActive(true);
      setGameStatus('error'); // 게임 감지 안됨 상태 (초기 상태)
      
      // 백그라운드에서 Core 게임 감지 시작
      try {
        const response = await window.coreAPI?.startDetection();
        if (response?.success) {
          console.log('Core 게임 감지 시작됨:', response.data);
          
          // Core 연결 성공 후 프리셋 초기화 메시지 전송
          await sendPresetInit();
          
          // 자동 overlay 관리가 메인 프로세스에서 처리됩니다
        } else {
          console.error('Core 게임 감지 시작 실패:', response?.error);
          // 실패 시 버튼 비활성화
          setIsActive(false);
          setGameStatus('error');
        }
      } catch (error) {
        console.error('Core 통신 실패:', error);
        // 통신 실패 시 버튼 비활성화
        setIsActive(false);
        setGameStatus('error');
      }
    } else {
      // 비활성화 상태로 변경
      setIsActive(false);
      setGameStatus('error'); // 게임 감지 안됨 상태
      
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

  // 뷰 전환 핸들러
  const handleOpenPresetSettings = () => {
    // 프리셋 설정을 열 때 현재 프리셋의 종족으로 편집 상태 초기화
    setCurrentEditingRace(currentPreset.selectedRace || 'protoss');
    setCurrentView('preset-settings');
    changeWindowSize('preset-settings');
  };

  const handleBackToMain = () => {
    // 메인으로 돌아갈 때 편집 중인 종족 상태 초기화
    setCurrentEditingRace(null);
    setCurrentView('main');
    changeWindowSize('main');
  };

  const handleBackToPresetSettings = () => {
    // 프리셋 설정으로 돌아가기 (종족 상태는 유지)
    setCurrentView('preset-settings');
    changeWindowSize('preset-settings');
  };

  // 종족 실시간 변경 핸들러
  const handleRaceChange = (race: 'protoss' | 'terran' | 'zerg') => {
    console.log('실시간 종족 변경:', race);
    setCurrentEditingRace(race);
  };

  // 설정 페이지 전환 핸들러들
  const handleOpenPopulationSettings = () => {
    setCurrentView('population-settings');
    changeWindowSize('population-settings');
  };

  const handleOpenWorkerSettings = () => {
    setCurrentView('worker-settings');
    changeWindowSize('worker-settings');
  };

  const handleOpenUnitSettings = () => {
    setCurrentView('unit-settings');
    changeWindowSize('unit-settings');
  };

  const handleOpenUpgradeSettings = () => {
    setCurrentView('upgrade-settings');
    changeWindowSize('upgrade-settings');
  };

  const handleOpenBuildOrderSettings = () => {
    setCurrentView('build-order-settings');
    changeWindowSize('build-order-settings');
  };

  const handleOpenDevelopmentProgress = (featureName: string, featureType: 'buildorder' | 'upgrade' | 'population' | 'unit') => {
    setDevelopmentFeatureName(featureName);
    setDevelopmentFeatureType(featureType);
    setCurrentView('development-progress');
    changeWindowSize('development-progress');
  };

  // 현재 뷰에 따라 렌더링할 컴포넌트 결정
  const renderCurrentView = () => {
    switch (currentView) {
      case 'main':
        return (
          <MainInterface
            presets={presets}
            currentPresetIndex={currentPresetIndex}
            onPresetIndexChange={handlePresetIndexChange}
            onOpenPresetSettings={handleOpenPresetSettings}
            isActive={isActive}
            gameStatus={gameStatus}
            onToggleOverlay={toggleOverlay}
          />
        );

      case 'preset-settings':
        return (
          <PresetSettingsModal
            isOpen={true}
            onClose={handleBackToMain}
            currentPreset={currentPreset}
            onSave={handleSavePreset}
            onRaceChange={handleRaceChange}
            onOpenPopulationSettings={handleOpenPopulationSettings}
            onOpenWorkerSettings={handleOpenWorkerSettings}
            onOpenUnitSettings={handleOpenUnitSettings}
            onOpenUpgradeSettings={handleOpenUpgradeSettings}
            onOpenBuildOrderSettings={handleOpenBuildOrderSettings}
            onOpenDevelopmentProgress={handleOpenDevelopmentProgress}
          />
        );

      case 'population-settings':
        return (
          <PopulationDetailSettings
            isOpen={true}
            onClose={handleBackToPresetSettings}
            initialRace={currentEditingRace || currentPreset.selectedRace}
          />
        );

      case 'worker-settings':
        return (
          <WorkerDetailSettings
            isOpen={true}
            onClose={handleBackToPresetSettings}
          />
        );

      case 'unit-settings':
        return (
          <UnitDetailSettings
            isOpen={true}
            onClose={handleBackToPresetSettings}
            initialRace={currentEditingRace || currentPreset.selectedRace}
          />
        );

      case 'upgrade-settings':
        return (
          <UpgradeDetailSettings
            isOpen={true}
            onClose={handleBackToPresetSettings}
            initialRace={currentEditingRace || currentPreset.selectedRace}
          />
        );

      case 'build-order-settings':
        return (
          <BuildOrderDetailSettings
            isOpen={true}
            onClose={handleBackToPresetSettings}
          />
        );

      case 'development-progress':
        return (
          <DevelopmentModal
            isOpen={true}
            onClose={handleBackToPresetSettings}
            featureName={developmentFeatureName}
            featureType={developmentFeatureType}
          />
        );

      default:
        return null;
    }
  };

  return (
    <div className={`app-container window-centered-container ${currentView}`}>
      {renderCurrentView()}
    </div>
  );
}