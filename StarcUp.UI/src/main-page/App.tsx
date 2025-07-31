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
  const [currentPresetIndex, setCurrentPresetIndex] = useState(0);
  const [presets, setPresets] = useState<Preset[]>([]);
  const [presetsLoaded, setPresetsLoaded] = useState(false);
  
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

  // 프리셋 자동 로드 (앱 시작 시)
  useEffect(() => {
    const loadPresets = async () => {
      try {
        console.log('📋 프리셋 자동 로드 시작...');
        
        if (!window.electronAPI?.getPresetsWithSelection) {
          console.warn('⚠️ electronAPI.getPresetsWithSelection이 준비되지 않았습니다');
          return;
        }

        const response = await window.electronAPI.getPresetsWithSelection('default-user');
        
        if (response.success && response.data) {
          const { presets: loadedPresets, selectedIndex } = response.data;
          
          // IPreset을 UI Preset 형태로 변환
          const uiPresets: Preset[] = loadedPresets.map((p: any) => ({
            id: p.id,
            name: p.name,
            description: p.data.description,
            featureStates: p.data.featureStates,
            selectedRace: p.data.selectedRace,
            workerSettings: p.data.workerSettings
          }));

          setPresets(uiPresets);
          setCurrentPresetIndex(selectedIndex);
          setPresetsLoaded(true);
          
          console.log('✅ 프리셋 자동 로드 완료:', {
            count: uiPresets.length,
            selected: selectedIndex,
            selectedName: uiPresets[selectedIndex]?.name
          });
        } else {
          console.warn('⚠️ 프리셋 로드 실패');
        }
      } catch (error) {
        console.error('❌ 프리셋 로드 실패', error);
      }
    };

    loadPresets();
  }, []);

  // 자동 overlay 관리는 이제 메인 프로세스에서 처리됩니다

  // 프리셋 초기화 함수 (Named Pipe 연결 후 호출)
  const sendPresetInit = async () => {
    try {
      // 현재 선택된 프리셋의 일꾼 설정 사용 (완전한 데이터 보장)
      const currentWorkerSettings: WorkerSettings = currentPreset.workerSettings;

      const workerMask = calculateWorkerSettingsMask(currentWorkerSettings as PresetUtilsWorkerSettings);
      
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
          
          // 프리셋 로딩이 완료된 경우에만 프리셋 초기화 메시지 전송
          if (presetsLoaded && presets.length > 0) {
            await sendPresetInit();
          } else {
            console.warn('⚠️ 프리셋이 아직 로드되지 않아 초기화 메시지 전송을 건너뜁니다');
          }
          
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

  const handleSavePreset = async (updatedPreset: {
    id: string;
    name: string;
    description: string;
    featureStates: boolean[];
    selectedRace?: 'protoss' | 'terran' | 'zerg';
  }) => {
    try {
      console.log('📝 프리셋 저장 시작:', updatedPreset.name, '종족:', updatedPreset.selectedRace);
      
      // 로컬 상태 업데이트
      setPresets(prev => prev.map(preset => 
        preset.id === updatedPreset.id ? { ...preset, ...updatedPreset } : preset
      ));
      
      // 파일에 저장
      if (window.electronAPI?.updatePreset) {
        const updates = {
          name: updatedPreset.name,
          description: updatedPreset.description,
          featureStates: updatedPreset.featureStates,
          selectedRace: updatedPreset.selectedRace
        };
        
        const result = await window.electronAPI.updatePreset('default-user', updatedPreset.id, updates);
        
        if (result.success) {
          console.log('✅ 프리셋 파일 저장 완료:', updatedPreset.name);
        } else {
          console.error('❌ 프리셋 파일 저장 실패');
        }
      } else {
        console.warn('⚠️ electronAPI가 준비되지 않아 로컬 상태만 업데이트됨');
      }
      
      // 저장 후 편집 중인 종족 상태 초기화
      setCurrentEditingRace(null);
    } catch (error) {
      console.error('❌ 프리셋 저장 중 오류:', error);
    }
  };

  // 일꾼 설정 저장 핸들러
  const handleSaveWorkerSettings = async (presetId: string, workerSettings: WorkerSettings) => {
    try {
      console.log('🔧 일꾼 설정 저장:', presetId, workerSettings);
      
      // 로컬 상태 업데이트
      setPresets(prev => prev.map(preset => 
        preset.id === presetId 
          ? { ...preset, workerSettings } 
          : preset
      ));
      
      // 파일에 저장
      if (window.electronAPI?.updatePreset) {
        const result = await window.electronAPI.updatePreset('default-user', presetId, {
          workerSettings
        });
        
        if (result.success) {
          console.log('✅ 일꾼 설정 파일 저장 완료');
        } else {
          console.error('❌ 일꾼 설정 파일 저장 실패');
        }
      } else {
        console.warn('⚠️ electronAPI가 준비되지 않아 로컬 상태만 업데이트됨');
      }
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