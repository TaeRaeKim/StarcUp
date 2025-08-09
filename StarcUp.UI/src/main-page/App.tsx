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
import { RaceType, RACE_NAMES } from "../types/enums";

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
  selectedRace: RaceType;
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

  // 현재 편집 중인 프리셋 상태 (실시간 동기화용)
  const [currentEditingRace, setCurrentEditingRace] = useState<RaceType | null>(null);
  const [editingPresetData, setEditingPresetData] = useState<{
    name: string;
    description: string;
    featureStates: boolean[];
    selectedRace: RaceType;
  } | null>(null);

  // 개발 중 기능 상태
  const [developmentFeatureName, setDevelopmentFeatureName] = useState('');
  const [developmentFeatureType, setDevelopmentFeatureType] = useState<'buildorder' | 'upgrade' | 'population' | 'unit'>('buildorder');
  
  // 임시 저장 상태 (상세 설정에서 저장하기 전 임시 데이터)
  const [tempWorkerSettings, setTempWorkerSettings] = useState<WorkerSettings | null>(null);
  const [tempPopulationSettings, setTempPopulationSettings] = useState<any | null>(null);
  
  // 기능별 변경사항 상태 (0: 일꾼, 1: 인구수, 2: 유닛, 3: 업그레이드, 4: 빌드오더)
  const [detailChanges, setDetailChanges] = useState<Record<number, boolean>>({});
  
  // 종족별 인구수 설정 백업 (종족 변경 시 복원용)
  const [populationSettingsBackup, setPopulationSettingsBackup] = useState<Map<RaceType, any>>(new Map());
  const [originalRace, setOriginalRace] = useState<RaceType | null>(null);
  
  // 인구수 설정 비교 유틸리티 함수
  const isPopulationSettingsEqual = (settings1: any, settings2: any): boolean => {
    if (!settings1 && !settings2) return true;
    if (!settings1 || !settings2) return false;
    
    // 기본적인 비교
    if (settings1.mode !== settings2.mode) return false;
    
    // 모드 A 비교
    if (settings1.mode === 'fixed') {
      const fixed1 = settings1.fixedSettings;
      const fixed2 = settings2.fixedSettings;
      if (!fixed1 && !fixed2) return true;
      if (!fixed1 || !fixed2) return false;
      
      if (fixed1.thresholdValue !== fixed2.thresholdValue) return false;
      
      // 시간 제한 비교
      const time1 = fixed1.timeLimit;
      const time2 = fixed2.timeLimit;
      if (!time1 && !time2) return true;
      if (!time1 || !time2) return false;
      
      return time1.enabled === time2.enabled && 
             time1.minutes === time2.minutes && 
             time1.seconds === time2.seconds;
    }
    
    // 모드 B 비교
    if (settings1.mode === 'building') {
      const building1 = settings1.buildingSettings;
      const building2 = settings2.buildingSettings;
      if (!building1 && !building2) return true;
      if (!building1 || !building2) return false;
      
      if (building1.race !== building2.race) return false;
      
      // 건물 설정 비교 (간단하게 JSON 문자열로 비교)
      return JSON.stringify(building1.trackedBuildings) === JSON.stringify(building2.trackedBuildings);
    }
    
    return true;
  };

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

  const handleSavePreset = async () => {
    try {
      if (!editingPresetData || !currentPreset) {
        console.error('❌ 편집 데이터 또는 현재 프리셋이 없습니다.');
        return;
      }

      console.log('📝 프리셋 배치 저장 시작:', editingPresetData.name, '종족:', editingPresetData.selectedRace);
      
      if (!window.presetAPI?.updateBatch) {
        console.error('❌ presetAPI.updateBatch를 사용할 수 없습니다.');
        return;
      }

      // 임시 저장된 상세 설정들도 함께 저장
      const batchUpdate: any = {
        name: editingPresetData.name,
        description: editingPresetData.description,
        featureStates: editingPresetData.featureStates,
        selectedRace: editingPresetData.selectedRace
      };
      
      // 임시 저장된 일꾼 설정이 있으면 포함
      if (tempWorkerSettings) {
        batchUpdate.workerSettings = tempWorkerSettings;
      }
      
      // 임시 저장된 인구수 설정이 있으면 포함
      if (tempPopulationSettings) {
        batchUpdate.populationSettings = tempPopulationSettings;
      }

      // 모든 변경사항을 한 번에 배치 업데이트
      await window.presetAPI.updateBatch(batchUpdate);
      
      console.log('✅ 프리셋 배치 저장 완료');
      
      // 저장 후 편집 상태 및 임시 저장 데이터 초기화
      setCurrentEditingRace(null);
      setEditingPresetData(null);
      setTempWorkerSettings(null);
      setTempPopulationSettings(null);
      setDetailChanges({});
      setPopulationSettingsBackup(new Map());
      setOriginalRace(null);
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

  // 인구수 설정 저장 핸들러 (presetAPI 전용)
  const handleSavePopulationSettings = async (presetId: string, populationSettings: any) => {
    try {
      console.log('🏘️ 인구수 설정 저장:', presetId, populationSettings);
      
      if (!window.presetAPI?.updateSettings) {
        console.error('❌ presetAPI.updateSettings를 사용할 수 없습니다.');
        return;
      }

      await window.presetAPI.updateSettings('population', populationSettings);
      console.log('✅ presetAPI 인구수 설정 업데이트 완료');
      // 나머지는 이벤트로 자동 처리됨
    } catch (error) {
      console.error('❌ 인구수 설정 저장 중 오류:', error);
    }
  };

  // 뷰 전환 핸들러
  const handleOpenPresetSettings = () => {
    // 프리셋 설정을 열 때 편집 중인 데이터가 없으면 현재 프리셋으로 초기화
    if (editingPresetData === null) {
      setEditingPresetData({
        name: currentPreset.name,
        description: currentPreset.description,
        featureStates: [...currentPreset.featureStates],
        selectedRace: currentPreset.selectedRace ?? RaceType.Protoss
      });
      setCurrentEditingRace(currentPreset.selectedRace ?? RaceType.Protoss);
    }
    setCurrentView('preset-settings');
    changeWindowSize('preset-settings');
  };
  
  // 프리셋 설정 초기화 핸들러
  const handleResetPreset = () => {
    // 편집 중인 데이터를 현재 프리셋으로 초기화
    setEditingPresetData({
      name: currentPreset.name,
      description: currentPreset.description,
      featureStates: [...currentPreset.featureStates],
      selectedRace: currentPreset.selectedRace ?? RaceType.Protoss
    });
    setCurrentEditingRace(currentPreset.selectedRace ?? RaceType.Protoss);
    
    // 임시 저장 데이터도 초기화 (원래 설정으로 복원)
    setTempWorkerSettings(null);
    setTempPopulationSettings(null); // 원래 프리셋 설정을 사용
    setDetailChanges({});
    setPopulationSettingsBackup(new Map());
    setOriginalRace(null);
  };

  const handleBackToMain = () => {
    // 메인으로 돌아갈 때 편집 중인 상태 모두 초기화
    setCurrentEditingRace(null);
    setEditingPresetData(null);
    setTempWorkerSettings(null);
    setTempPopulationSettings(null);
    setDetailChanges({});
    setPopulationSettingsBackup(new Map());
    setOriginalRace(null);
    setCurrentView('main');
    changeWindowSize('main');
  };

  const handleBackToPresetSettings = () => {
    // 프리셋 설정으로 돌아가기 (종족 상태는 유지)
    setCurrentView('preset-settings');
    changeWindowSize('preset-settings');
  };

  // 종족 실시간 변경 핸들러
  const handleRaceChange = (race: RaceType) => {
    console.log('실시간 종족 변경:', race);
    
    const currentRace = currentEditingRace ?? (currentPreset.selectedRace ?? RaceType.Protoss);
    
    // 최초 종족 저장 및 최초 인구수 설정 백업 (복원용)
    if (originalRace === null) {
      const originalRaceValue = currentPreset.selectedRace ?? RaceType.Protoss;
      setOriginalRace(originalRaceValue);
      
      // 최초 인구수 설정도 백업 (원래 프리셋 설정)
      if (currentPreset.populationSettings) {
        const backup = new Map(populationSettingsBackup);
        backup.set(originalRaceValue, currentPreset.populationSettings);
        setPopulationSettingsBackup(backup);
        console.log(`💾 최초 종족 ${originalRaceValue} 인구수 설정 백업:`, currentPreset.populationSettings);
      }
    }
    
    // 현재 편집 중인 종족의 인구수 설정 백업 (임시 설정이 있는 경우만)
    if (tempPopulationSettings && currentRace !== race) {
      const backup = new Map(populationSettingsBackup);
      backup.set(currentRace, tempPopulationSettings);
      setPopulationSettingsBackup(backup);
      console.log(`💾 종족 ${currentRace} 인구수 설정 백업:`, tempPopulationSettings);
    }
    
    setCurrentEditingRace(race);
    
    // 편집 데이터도 업데이트
    if (editingPresetData) {
      setEditingPresetData({
        ...editingPresetData,
        selectedRace: race
      });
    }
    
    // 종족 변경에 따른 인구수 설정 처리
    const currentPopulationSettings = tempPopulationSettings || currentPreset.populationSettings;
    
    // 1. 백업된 설정이 있는지 확인 (이미 방문한 종족 또는 원래 종족)
    const backup = populationSettingsBackup.get(race);
    if (backup) {
      console.log(`✅ 종족 ${race} 인구수 설정 복원:`, backup);
      setTempPopulationSettings(backup);
      
      // 변경사항 플래그 설정 로직
      if (race === originalRace) {
        // 원래 종족으로 돌아왔으면 변경사항 플래그 해제
        setDetailChanges(prev => ({ ...prev, 1: false }));
        console.log(`🎆 원래 종족 ${race}로 복귀 - 변경사항 플래그 해제`);
      } else {
        // 다른 종족(임시값 복원)으로 갈 때는 변경사항 플래그 유지
        // 백업된 설정이 원래 프리셋 설정과 다른지 확인
        const originalPopulationSettings = currentPreset.populationSettings;
        const isBackupDifferentFromOriginal = !isPopulationSettingsEqual(backup, originalPopulationSettings);
        if (isBackupDifferentFromOriginal) {
          setDetailChanges(prev => ({ ...prev, 1: true }));
          console.log(`🟡 종족 ${race} 임시값 복원 - 변경사항 플래그 유지`);
        } else {
          setDetailChanges(prev => ({ ...prev, 1: false }));
          console.log(`✅ 종족 ${race} 백업값이 원래와 동일 - 변경사항 플래그 해제`);
        }
      }
      return; // 복원되었으면 추가 처리 없이 종료
    }
    
    // 2. 현재 인구수 설정이 모드 B(건물 기반)인 경우만 처리
    if (currentPopulationSettings?.mode === 'building') {
      console.log(`⚠️ 모드 B에서 종족 ${race}로 변경 - 모드 A로 초기화`);
      const defaultSettings = {
        mode: 'fixed' as const,
        fixedSettings: {
          thresholdValue: 4,
          timeLimit: {
            enabled: true,
            minutes: 3,
            seconds: 0
          }
        }
      };
      setTempPopulationSettings(defaultSettings);
      setDetailChanges(prev => ({ ...prev, 1: true })); // 인구수 변경사항 표시
    } else if (!currentPopulationSettings) {
      // 3. 인구수 설정이 아예 없는 경우 기본값 설정
      console.log(`🏘️ 인구수 설정 없음 - 기본 모드 A 설정`);
      const defaultSettings = {
        mode: 'fixed' as const,
        fixedSettings: {
          thresholdValue: 4,
          timeLimit: {
            enabled: true,
            minutes: 3,
            seconds: 0
          }
        }
      };
      setTempPopulationSettings(defaultSettings);
      setDetailChanges(prev => ({ ...prev, 1: true }));
    }
  };

  // 편집 데이터 업데이트 핸들러들
  const handleEditingDataChange = (updatedData: {
    name?: string;
    description?: string;
    featureStates?: boolean[];
    selectedRace?: RaceType;
  }) => {
    if (editingPresetData) {
      setEditingPresetData({
        ...editingPresetData,
        ...updatedData
      });
    }
  };

  // 임시 저장 핸들러
  const handleTempSaveWorkerSettings = (settings: WorkerSettings) => {
    console.log('💾 일꾼 설정 임시 저장:', settings);
    setTempWorkerSettings(settings);
    setDetailChanges(prev => ({ ...prev, 0: true })); // 일꾼은 인덱스 0
  };
  
  const handleTempSavePopulationSettings = (settings: any) => {
    console.log('💾 인구수 설정 임시 저장:', settings);
    setTempPopulationSettings(settings);
    setDetailChanges(prev => ({ ...prev, 1: true })); // 인구수는 인덱스 1
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
            editingPresetData={editingPresetData}
            onSave={handleSavePreset}
            onRaceChange={handleRaceChange}
            onEditingDataChange={handleEditingDataChange}
            onOpenPopulationSettings={handleOpenPopulationSettings}
            onOpenWorkerSettings={handleOpenWorkerSettings}
            onOpenUnitSettings={handleOpenUnitSettings}
            onOpenUpgradeSettings={handleOpenUpgradeSettings}
            onOpenBuildOrderSettings={handleOpenBuildOrderSettings}
            onOpenDevelopmentProgress={handleOpenDevelopmentProgress}
            detailChanges={detailChanges}
            onReset={handleResetPreset}
          />
        );

      case 'population-settings':
        return (
          <PopulationDetailSettings
            isOpen={true}
            onClose={handleBackToPresetSettings}
            initialRace={editingPresetData?.selectedRace ?? currentPreset.selectedRace}
            currentPreset={currentPreset}
            onSavePopulationSettings={handleSavePopulationSettings}
            tempPopulationSettings={tempPopulationSettings}
            onTempSave={handleTempSavePopulationSettings}
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
            tempWorkerSettings={tempWorkerSettings}
            onTempSave={handleTempSaveWorkerSettings}
          />
        );

      case 'unit-settings':
        return (
          <UnitDetailSettings
            isOpen={true}
            onClose={handleBackToPresetSettings}
            initialRace={editingPresetData?.selectedRace ?? currentPreset.selectedRace}
          />
        );

      case 'upgrade-settings':
        return (
          <UpgradeDetailSettings
            isOpen={true}
            onClose={handleBackToPresetSettings}
            initialRace={editingPresetData?.selectedRace ?? currentPreset.selectedRace}
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