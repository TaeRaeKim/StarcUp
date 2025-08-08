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
  type WorkerSettings as PresetUtilsWorkerSettings
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

// 일꾼 설정 인터페이스 (완전한 데이터 보장)
interface WorkerSettings {
  workerCountDisplay: boolean;
  includeProducingWorkers: boolean;
  idleWorkerDisplay: boolean;
  workerProductionDetection: boolean;
  workerDeathDetection: boolean;
  gasWorkerCheck: boolean;
}

// 프리셋 타입 정의 (완전한 데이터 보장)
interface Preset {
  id: string;
  name: string;
  description: string;
  featureStates: boolean[];
  selectedRace: 'protoss' | 'terran' | 'zerg';
  workerSettings: WorkerSettings;
}

export default function App() {
  const [isActive, setIsActive] = useState(false);
  const [gameStatus, setGameStatus] = useState<GameStatus>('error');
  
  // presetAPI 기반 상태 관리 (단순화)
  const [presetState, setPresetState] = useState<{
    currentPreset: any | null
    allPresets: any[]
    isLoading: boolean
    selectedIndex: number
  }>({
    currentPreset: null,
    allPresets: [],
    isLoading: true,
    selectedIndex: 0
  });
  
  // 기존 호환성 유지를 위한 computed 값들
  const presets = presetState.allPresets;
  const currentPresetIndex = presetState.selectedIndex;
  const presetsLoaded = !presetState.isLoading && presetState.allPresets.length > 0;
  
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

  // presetAPI를 통한 프리셋 상태 초기화 (단순화)
  useEffect(() => {
    const initializePresetData = async () => {
      try {
        console.log('🚀 presetAPI를 통한 프리셋 초기화 시작...');
        
        if (!window.presetAPI?.getState) {
          console.error('❌ presetAPI가 준비되지 않았습니다.');
          setPresetState(prev => ({ ...prev, isLoading: false }));
          return;
        }

        // presetAPI를 통한 현재 상태 조회
        const stateResult = await window.presetAPI.getState();
        
        if (stateResult?.success && stateResult.data) {
          const state = stateResult.data;
          
          setPresetState({
            currentPreset: state.currentPreset,
            allPresets: state.allPresets,
            isLoading: false,
            selectedIndex: state.selectedPresetIndex || 0
          });
          
          console.log('✅ presetAPI 프리셋 초기화 완료:', {
            count: state.allPresets?.length || 0,
            selected: state.selectedPresetIndex,
            currentName: state.currentPreset?.name
          });
        } else {
          console.error('❌ presetAPI 상태 조회 실패:', stateResult?.error);
          setPresetState(prev => ({ ...prev, isLoading: false }));
        }
      } catch (error) {
        console.error('❌ presetAPI 초기화 실패:', error);
        setPresetState(prev => ({ ...prev, isLoading: false }));
      }
    };

    initializePresetData();
  }, []);

  // presetAPI 이벤트 리스너 설정 (실시간 동기화)
  useEffect(() => {
    if (!window.presetAPI?.onStateChanged) {
      console.log('⚠️ presetAPI 이벤트 리스너가 준비되지 않았습니다');
      return;
    }

    console.log('👂 presetAPI 이벤트 리스너 등록');
    
    const unsubscribe = window.presetAPI.onStateChanged((event) => {
      console.log('📡 프리셋 상태 변경 수신:', event.type, event);
      
      // 이벤트 타입에 따른 상태 업데이트
      try {
        switch (event.type) {
          case 'presets-loaded':
          case 'preset-switched':
          case 'settings-updated':
            // 전체 상태를 다시 조회하여 동기화
            if (event.state) {
              setPresetState({
                currentPreset: event.state.currentPreset,
                allPresets: event.state.allPresets || [],
                isLoading: event.state.isLoading || false,
                selectedIndex: event.state.selectedPresetIndex || 0
              });
              
              console.log('✅ 프리셋 상태 동기화 완료:', event.type);
            } else {
              console.warn('⚠️ 이벤트에 상태 정보가 없습니다:', event);
            }
            break;
          
          default:
            console.log('📡 알 수 없는 프리셋 이벤트 타입:', event.type);
            break;
        }
      } catch (error) {
        console.error('❌ 프리셋 이벤트 처리 실패:', error, event);
      }
    });

    // 컴포넌트 언마운트 시 이벤트 리스너 정리
    return () => {
      console.log('🧹 presetAPI 이벤트 리스너 정리');
      unsubscribe();
    };
  }, []);

  // 자동 overlay 관리는 이제 메인 프로세스에서 처리됩니다

  // 프리셋 초기화 함수 (presetAPI에서 자동 관리)
  const sendPresetInit = async () => {
    try {
      // presetAPI 중앙 관리 시스템에서 자동으로 현재 프리셋이 Core에 전송됨
      console.log('🚀 프리셋 초기화: presetAPI에서 자동 관리됨');
      
      // presetAPI가 자동으로 Core와 동기화를 처리하므로 별도 작업 불필요
      console.log('ℹ️ presetAPI가 프리셋 상태를 자동으로 관리합니다');
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
          
          // 프리셋 초기화 (presetAPI에서 자동 관리)
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

  const currentPreset = presetState.currentPreset || presets[currentPresetIndex];

  // 프리셋 관련 핸들러 (presetAPI 전용)
  const handlePresetIndexChange = async (index: number) => {
    try {
      const targetPreset = presets[index];
      if (!targetPreset) {
        console.error('❌ 대상 프리셋을 찾을 수 없습니다:', index);
        return;
      }

      console.log('🔄 프리셋 전환 시작:', targetPreset.name);

      if (!window.presetAPI?.switch) {
        console.error('❌ presetAPI.switch를 사용할 수 없습니다.');
        return;
      }

      await window.presetAPI.switch(targetPreset.id);
      console.log('✅ presetAPI 프리셋 전환 완료');
      // 나머지는 이벤트로 자동 처리됨
      
      // 프리셋 변경 시 편집 중인 종족 상태 초기화
      setCurrentEditingRace(null);
    } catch (error) {
      console.error('❌ 프리셋 전환 실패:', error);
    }
  };

  const handleSavePreset = async (updatedPreset: {
    id: string;
    name: string;
    description: string;
    featureStates: boolean[];
    selectedRace?: 'protoss' | 'terran' | 'zerg';
  }) => {
    try {
      console.log('📝 프리셋 저장 시작:', updatedPreset.name, '종족:', updatedPreset.selectedRace);
      
      if (!window.presetAPI?.toggleFeature || !window.presetAPI?.updateSettings) {
        console.error('❌ presetAPI를 사용할 수 없습니다.');
        return;
      }

      // 1. 프리셋 기본 정보 업데이트 (이름, 설명)
      if (currentPreset?.name !== updatedPreset.name || currentPreset?.description !== updatedPreset.description) {
        console.log('📝 프리셋 기본 정보 업데이트:', {
          name: updatedPreset.name,
          description: updatedPreset.description
        });
        
        await window.presetAPI.updateSettings('basic', {
          name: updatedPreset.name,
          description: updatedPreset.description
        });
      }

      // 2. 기능 상태 업데이트
      const currentFeatureStates = currentPreset?.featureStates || [];
      
      for (let i = 0; i < updatedPreset.featureStates.length; i++) {
        if (currentFeatureStates[i] !== updatedPreset.featureStates[i]) {
          console.log('🎛️ 기능 토글:', i, updatedPreset.featureStates[i]);
          await window.presetAPI.toggleFeature(i, updatedPreset.featureStates[i]);
        }
      }

      // 3. 종족 변경이 있는 경우 설정 업데이트
      if (currentPreset?.selectedRace !== updatedPreset.selectedRace && updatedPreset.selectedRace) {
        console.log('🏁 종족 업데이트:', updatedPreset.selectedRace);
        await window.presetAPI.updateSettings('race', { 
          selectedRace: updatedPreset.selectedRace 
        });
      }
      
      console.log('✅ 프리셋 저장 완료');
      
      // 저장 후 편집 중인 종족 상태 초기화
      setCurrentEditingRace(null);
    } catch (error) {
      console.error('❌ 프리셋 저장 중 오류:', error);
    }
  };

  // 일꾼 설정 저장 핸들러 (presetAPI 전용)
  const handleSaveWorkerSettings = async (presetId: string, workerSettings: WorkerSettings) => {
    try {
      console.log('🔧 일꾼 설정 저장:', presetId, workerSettings);
      
      if (!window.presetAPI?.updateSettings) {
        console.error('❌ presetAPI.updateSettings를 사용할 수 없습니다.');
        return;
      }

      await window.presetAPI.updateSettings('worker', workerSettings);
      console.log('✅ presetAPI 일꾼 설정 업데이트 완료');
      // 나머지는 이벤트로 자동 처리됨
    } catch (error) {
      console.error('❌ 일꾼 설정 저장 중 오류:', error);
    }
  };

  // 뷰 전환 핸들러
  const handleOpenPresetSettings = () => {
    // 프리셋 설정을 열 때 현재 프리셋의 종족으로 편집 상태 초기화
    setCurrentEditingRace(currentPreset.selectedRace);
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
        // preset이 로드되지 않았으면 로딩 화면 표시
        if (!presetsLoaded || presets.length === 0) {
          return (
            <div className="h-screen w-screen flex items-center justify-center" style={{ backgroundColor: 'var(--starcraft-bg)' }}>
              <div className="text-center">
                <div className="text-xl mb-4" style={{ color: 'var(--starcraft-green)' }}>
                  프리셋 로딩 중...
                </div>
                <div className="animate-pulse text-sm" style={{ color: 'var(--starcraft-green)' }}>
                  잠시만 기다려주세요
                </div>
              </div>
            </div>
          );
        }
        
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
        if (!presetsLoaded || presets.length === 0 || !currentPreset) {
          return (
            <div className="h-screen w-screen flex items-center justify-center" style={{ backgroundColor: 'var(--starcraft-bg)' }}>
              <div className="text-center">
                <div className="text-xl mb-4" style={{ color: 'var(--starcraft-green)' }}>
                  프리셋 로딩 중...
                </div>
              </div>
            </div>
          );
        }
        
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
        console.log('🔧 WorkerDetailSettings 렌더링 - currentPreset:', currentPreset);
        return (
          <WorkerDetailSettings
            isOpen={true}
            onClose={handleBackToPresetSettings}
            currentPreset={currentPreset}
            onSaveWorkerSettings={handleSaveWorkerSettings}
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