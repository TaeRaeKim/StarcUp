import { useState, useEffect } from "react";
import { MainInterface } from "@/main-page/components/MainInterface";
import { PresetSettingsModal } from "@/main-page/components/PresetSettingsModal";
import { PopulationDetailSettings } from "@/main-page/components/PopulationDetailSettings";
import { WorkerDetailSettings } from "@/main-page/components/WorkerDetailSettings";
import { UnitDetailSettings } from "@/main-page/components/UnitDetailSettings";
import { UpgradeDetailSettings } from "@/main-page/components/UpgradeDetailSettings";
import { BuildOrderDetailSettings } from "@/main-page/components/BuildOrderDetailSettings";
import { DevelopmentModal } from "@/main-page/components/DevelopmentModal";
import { ModeSelectionLogin } from "@/main-page/components/ModeSelectionLogin";
import { 
  calculateWorkerSettingsMask, 
  type PresetInitMessage, 
  type WorkerPreset
} from "../utils/presetUtils";
import { 
  getProStatus, 
  sanitizePresetForNonPro, 
  sanitizePresetsForNonPro,
  sanitizeWorkerSettingsForNonPro,
  sanitizePopulationSettingsForNonPro,
  checkAndHandleSubscriptionChange
} from "../utils/proUtils";
import { RaceType } from "../types/game";
import { UpgradeSettings, WorkerSettings } from "../types/preset";

// 게임 상태 타입 정의
type GameStatus = 'playing' | 'waiting' | 'error';

// 애플리케이션 단계 타입 정의
type AppStage = 'login' | 'loading' | 'main';

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

// WorkerSettings는 중앙 타입 정의에서 import

// 프리셋 타입 정의 (완전한 데이터 보장)
interface Preset {
  id: string;
  name: string;
  description: string;
  featureStates: boolean[];
  selectedRace: RaceType;
  workerSettings: WorkerSettings;
  upgradeSettings?: UpgradeSettings;
}

// 기본 업그레이드 설정 생성 함수
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

// 참고: upgradeSettings 타입이 IPreset과 StoredPreset에 추가되어 더 이상 기본값 주입이 불필요함

export default function App() {
  // 애플리케이션 단계 관리 (로그인 → 로딩 → 메인)
  const [appStage, setAppStage] = useState<AppStage>('login');
  const [isPro, setIsPro] = useState<boolean>(false);
  
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
  const [tempUpgradeSettings, setTempUpgradeSettings] = useState<UpgradeSettings | null>(null);
  
  // 기능별 변경사항 상태 (0: 일꾼, 1: 인구수, 2: 유닛, 3: 업그레이드, 4: 빌드오더)
  const [detailChanges, setDetailChanges] = useState<Record<number, boolean>>({});
  
  // 종족별 인구수 설정 백업 (종족 변경 시 복원용)
  const [populationSettingsBackup, setPopulationSettingsBackup] = useState<Map<RaceType, any>>(new Map());
  // 종족별 업그레이드 설정 백업 (종족 변경 시 복원용)
  const [upgradeSettingsBackup, setUpgradeSettingsBackup] = useState<Map<RaceType, UpgradeSettings>>(new Map());
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

  // 업그레이드 설정 비교 유틸리티 함수
  const isUpgradeSettingsEqual = (settings1: UpgradeSettings | null | undefined, settings2: UpgradeSettings | null | undefined): boolean => {
    if (!settings1 && !settings2) return true;
    if (!settings1 || !settings2) return false;
    
    // JSON 직렬화를 통한 깊은 비교
    return JSON.stringify(settings1) === JSON.stringify(settings2);
  };

  // 모드 선택 핸들러
  const handleModeSelect = async (selectedProMode: boolean) => {
    console.log('🔐 모드 선택됨:', selectedProMode ? 'Pro' : 'Free');
    setIsPro(selectedProMode);
    
    // 구독 상태 변경 체크 및 처리 (비동기)
    try {
      await checkAndHandleSubscriptionChange(selectedProMode);
    } catch (error) {
      console.error('⚠️ 구독 상태 체크 중 오류 (계속 진행):', error);
    }
    
    setAppStage('loading'); // 로딩 단계로 진행
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

  // presetAPI를 통한 프리셋 상태 초기화 (로그인 완료 후)
  useEffect(() => {
    // 로그인 단계에서는 프리셋 초기화하지 않음
    if (appStage !== 'loading') {
      return;
    }

    const initializePresetData = async () => {
      try {
        console.log('🚀 presetAPI를 통한 프리셋 초기화 시작...');
        
        // 2초 딜레이 시뮬레이션 (DB 로딩 시뮬레이션)
        await new Promise(resolve => setTimeout(resolve, 2000));
        
        if (!window.presetAPI?.getState) {
          console.error('❌ presetAPI가 준비되지 않았습니다.');
          setPresetState(prev => ({ ...prev, isLoading: false }));
          setAppStage('main'); // 실패해도 메인으로 진행
          return;
        }

        // presetAPI를 통한 현재 상태 조회
        const stateResult = await window.presetAPI.getState();
        console.log('📦 presetAPI 전체 응답:', stateResult);
        
        if (stateResult?.success && stateResult.data) {
          const state = stateResult.data;
          
          // 현재 프리셋의 세부 정보 로그
          if (state.currentPreset) {
            const preset = state.currentPreset;
            console.log('📦 현재 프리셋 세부 정보:', {
              name: preset.name,
              id: preset.id,
              keys: Object.keys(preset),
              hasWorkerSettings: !!preset.workerSettings,
              hasPopulationSettings: !!preset.populationSettings,
              hasUpgradeSettings: !!preset.upgradeSettings,
              workerSettings: preset.workerSettings,
              populationSettings: preset.populationSettings,
              upgradeSettings: preset.upgradeSettings
            });
          }
          
          // Pro 상태에 따라 프리셋 데이터 정리
          let sanitizedCurrentPreset = state.currentPreset;
          let sanitizedAllPresets = state.allPresets;
          
          if (!isPro) {
            console.log('🔒 Free 모드: Pro 기능 해제 중...');
            
            // 현재 프리셋의 Pro 기능 해제
            if (sanitizedCurrentPreset) {
              sanitizedCurrentPreset = sanitizePresetForNonPro(sanitizedCurrentPreset);
              console.log('✂️ 현재 프리셋 Pro 기능 해제 완료:', sanitizedCurrentPreset.name);
            }
            
            // 모든 프리셋의 Pro 기능 해제
            if (sanitizedAllPresets && Array.isArray(sanitizedAllPresets)) {
              sanitizedAllPresets = sanitizePresetsForNonPro(sanitizedAllPresets);
              console.log('✂️ 전체 프리셋 Pro 기능 해제 완료:', sanitizedAllPresets.length, '개');
            }
          }
          
          setPresetState({
            currentPreset: sanitizedCurrentPreset,
            allPresets: sanitizedAllPresets,
            isLoading: false,
            selectedIndex: state.selectedPresetIndex || 0
          });
          
          console.log('✅ presetAPI 프리셋 초기화 완료:', {
            count: state.allPresets?.length || 0,
            selected: state.selectedPresetIndex,
            currentName: state.currentPreset?.name,
            proMode: isPro,
            hasUpgradeSettings: !!state.currentPreset?.upgradeSettings,
            upgradeSettings: state.currentPreset?.upgradeSettings
          });
          
          // 프리셋 초기화 완료 후 메인 단계로 진행
          setAppStage('main');
        } else {
          console.error('❌ presetAPI 상태 조회 실패:', stateResult?.error);
          setPresetState(prev => ({ ...prev, isLoading: false }));
          setAppStage('main'); // 실패해도 메인으로 진행
        }
      } catch (error) {
        console.error('❌ presetAPI 초기화 실패:', error);
        setPresetState(prev => ({ ...prev, isLoading: false }));
        setAppStage('main'); // 실패해도 메인으로 진행
      }
    };

    initializePresetData();
  }, [appStage, isPro]);

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
              // Pro 상태에 따라 프리셋 데이터 정리
              let sanitizedCurrentPreset = event.state.currentPreset;
              let sanitizedAllPresets = event.state.allPresets || [];
              
              if (!isPro) {
                console.log('🔒 Free 모드: 이벤트 프리셋 Pro 기능 해제 중...');
                
                // 현재 프리셋의 Pro 기능 해제
                if (sanitizedCurrentPreset) {
                  sanitizedCurrentPreset = sanitizePresetForNonPro(sanitizedCurrentPreset);
                }
                
                // 모든 프리셋의 Pro 기능 해제
                if (Array.isArray(sanitizedAllPresets)) {
                  sanitizedAllPresets = sanitizePresetsForNonPro(sanitizedAllPresets);
                }
              }
              
              setPresetState({
                currentPreset: sanitizedCurrentPreset,
                allPresets: sanitizedAllPresets,
                isLoading: event.state.isLoading || false,
                selectedIndex: event.state.selectedPresetIndex || 0
              });
              
              console.log('✅ 프리셋 상태 동기화 완료:', event.type, {
                hasUpgradeSettings: !!event.state.currentPreset?.upgradeSettings,
                upgradeSettings: event.state.currentPreset?.upgradeSettings
              });
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
  }, [isPro]); // isPro 값이 변경되면 이벤트 리스너 재등록

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

      // 임시 저장된 업그레이드 설정이 있으면 포함
      if (tempUpgradeSettings) {
        batchUpdate.upgradeSettings = tempUpgradeSettings;
      }

      // 모든 변경사항을 한 번에 배치 업데이트
      await window.presetAPI.updateBatch(batchUpdate);
      
      console.log('✅ 프리셋 배치 저장 완료');
      
      // 저장 후 편집 상태 및 임시 저장 데이터 초기화
      setCurrentEditingRace(null);
      setEditingPresetData(null);
      setTempWorkerSettings(null);
      setTempPopulationSettings(null);
      setTempUpgradeSettings(null);
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

  // 업그레이드 설정 저장 핸들러 (presetAPI 전용)
  const handleSaveUpgradeSettings = async (presetId: string, upgradeSettings: UpgradeSettings) => {
    try {
      console.log('⚡ 업그레이드 설정 저장:', presetId, upgradeSettings);
      
      if (!window.presetAPI?.updateSettings) {
        console.error('❌ presetAPI.updateSettings를 사용할 수 없습니다.');
        return;
      }

      console.log('💾 presetAPI에 전송할 업그레이드 설정:', upgradeSettings);
      await window.presetAPI.updateSettings('upgrade', upgradeSettings);
      console.log('✅ presetAPI 업그레이드 설정 업데이트 완료');
      // 나머지는 이벤트로 자동 처리됨
    } catch (error) {
      console.error('❌ 업그레이드 설정 저장 중 오류:', error);
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
    setTempUpgradeSettings(null);
    setDetailChanges({});
    setPopulationSettingsBackup(new Map());
    setUpgradeSettingsBackup(new Map());
    setOriginalRace(null);
  };

  const handleBackToMain = () => {
    // 메인으로 돌아갈 때 편집 중인 상태 모두 초기화
    setCurrentEditingRace(null);
    setEditingPresetData(null);
    setTempWorkerSettings(null);
    setTempPopulationSettings(null);
    setTempUpgradeSettings(null);
    setDetailChanges({});
    setPopulationSettingsBackup(new Map());
    setUpgradeSettingsBackup(new Map());
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
    
    const currentRace = currentEditingRace ?? (currentPreset.selectedRace ?? RaceType.Protoss);
    
    // 현재 함수 실행 중의 최신 백업 맵을 추적
    let currentUpgradeBackupMap = new Map(upgradeSettingsBackup);
    
    // 최초 종족 저장 및 최초 인구수 설정 백업 (복원용)
    if (originalRace === null) {
      const originalRaceValue = currentPreset.selectedRace ?? RaceType.Protoss;
      setOriginalRace(originalRaceValue);
      
      // 최초 인구수 설정도 백업 (원래 프리셋 설정)
      if (currentPreset.populationSettings) {
        const backup = new Map(populationSettingsBackup);
        backup.set(originalRaceValue, currentPreset.populationSettings);
        setPopulationSettingsBackup(backup);
      }
      
      // 최초 업그레이드 설정도 백업 (원래 프리셋 설정)
      if (currentPreset.upgradeSettings) {
        currentUpgradeBackupMap.set(originalRaceValue, currentPreset.upgradeSettings);
        setUpgradeSettingsBackup(prev => {
          const newBackup = new Map(prev);
          newBackup.set(originalRaceValue, currentPreset.upgradeSettings);
          return newBackup;
        });
      }
    }
    
    // 현재 편집 중인 종족의 인구수 설정 백업 (임시 설정이 있는 경우만)
    if (tempPopulationSettings && currentRace !== race) {
      const backup = new Map(populationSettingsBackup);
      backup.set(currentRace, tempPopulationSettings);
      setPopulationSettingsBackup(backup);
    }
    
    // 현재 편집 중인 종족의 업그레이드 설정 백업 (임시 설정이 있는 경우만)
    if (tempUpgradeSettings && currentRace !== race) {
      currentUpgradeBackupMap.set(currentRace, tempUpgradeSettings);
      setUpgradeSettingsBackup(prev => {
        const newBackup = new Map(prev);
        newBackup.set(currentRace, tempUpgradeSettings);
        return newBackup;
      });
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
      setTempPopulationSettings(backup);
      
      // 변경사항 플래그 설정 로직
      const effectiveOriginalRace = originalRace ?? (currentPreset.selectedRace ?? RaceType.Protoss);
      if (race === effectiveOriginalRace) {
        // 원래 종족으로 돌아왔어도 백업된 설정이 원본과 다르면 변경사항 플래그 유지
        const originalPopulationSettings = currentPreset.populationSettings;
        const isBackupDifferentFromOriginal = !isPopulationSettingsEqual(backup, originalPopulationSettings);
        setDetailChanges(prev => ({ ...prev, 1: isBackupDifferentFromOriginal }));
      } else {
        // 다른 종족(임시값 복원)으로 갈 때는 변경사항 플래그 유지
        // 백업된 설정이 원래 프리셋 설정과 다른지 확인
        const originalPopulationSettings = currentPreset.populationSettings;
        const isBackupDifferentFromOriginal = !isPopulationSettingsEqual(backup, originalPopulationSettings);
        if (isBackupDifferentFromOriginal) {
          setDetailChanges(prev => ({ ...prev, 1: true }));
        } else {
          setDetailChanges(prev => ({ ...prev, 1: false }));
        }
      }
      // return 제거 - 업그레이드 설정 처리도 계속 진행해야 함
    }
    
    // 2. 현재 인구수 설정이 모드 B(건물 기반)인 경우만 처리
    if (currentPopulationSettings?.mode === 'building') {
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
    
    // 종족 변경에 따른 업그레이드 설정 처리
    const currentUpgradeSettings = tempUpgradeSettings || currentPreset.upgradeSettings;
    
    // 1. 백업된 업그레이드 설정이 있는지 확인 (이미 방문한 종족 또는 원래 종족)
    let upgradeBackup = currentUpgradeBackupMap.get(race);
    
    // 원래 종족으로 돌아가는데 백업이 없다면, 현재 프리셋 설정을 직접 사용
    const effectiveOriginalRace = originalRace ?? (currentPreset.selectedRace ?? RaceType.Protoss);
    if (!upgradeBackup && race === effectiveOriginalRace && currentPreset.upgradeSettings) {
      upgradeBackup = currentPreset.upgradeSettings;
      currentUpgradeBackupMap.set(race, upgradeBackup);
    }
    
    if (upgradeBackup) {
      setTempUpgradeSettings(upgradeBackup);
      
      // 변경사항 플래그 설정 로직
      const effectiveOriginalRace = originalRace ?? (currentPreset.selectedRace ?? RaceType.Protoss);
      if (race === effectiveOriginalRace) {
        // 원래 종족으로 돌아왔어도 백업된 설정이 원본과 다르면 변경사항 플래그 유지
        const originalUpgradeSettings = currentPreset.upgradeSettings;
        const isUpgradeBackupDifferentFromOriginal = !isUpgradeSettingsEqual(upgradeBackup, originalUpgradeSettings);
        setDetailChanges(prev => ({ ...prev, 3: isUpgradeBackupDifferentFromOriginal }));
      } else {
        // 다른 종족(임시값 복원)으로 갈 때는 변경사항 플래그 유지
        // 백업된 설정이 원래 프리셋 설정과 다른지 확인
        const originalUpgradeSettings = currentPreset.upgradeSettings;
        const isUpgradeBackupDifferentFromOriginal = !isUpgradeSettingsEqual(upgradeBackup, originalUpgradeSettings);
        if (isUpgradeBackupDifferentFromOriginal) {
          setDetailChanges(prev => ({ ...prev, 3: true }));
        } else {
          setDetailChanges(prev => ({ ...prev, 3: false }));
        }
      }
    } else if (currentUpgradeSettings && currentRace !== race) {
      // 2. 백업된 설정이 없고 다른 종족으로 변경되는 경우 기본값으로 초기화
      const defaultUpgradeSettings: UpgradeSettings = {
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
      };
      setTempUpgradeSettings(defaultUpgradeSettings);
      setDetailChanges(prev => ({ ...prev, 3: true })); // 업그레이드 변경사항 표시
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

  const handleTempSaveUpgradeSettings = (settings: UpgradeSettings) => {
    console.log('💾 업그레이드 설정 임시 저장:', settings);
    setTempUpgradeSettings(settings);
    setDetailChanges(prev => ({ ...prev, 3: true })); // 업그레이드는 인덱스 3
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

  // 현재 단계에 따라 렌더링할 컴포넌트 결정
  const renderCurrentStage = () => {
    // 1. 로그인 단계
    if (appStage === 'login') {
      return <ModeSelectionLogin onModeSelect={handleModeSelect} />;
    }

    // 2. 로딩 단계
    if (appStage === 'loading') {
      return (
        <div className="h-screen w-screen flex items-center justify-center relative overflow-hidden" style={{ backgroundColor: 'var(--starcraft-bg)' }}>
          {/* 배경 그라데이션 효과 */}
          <div className="absolute inset-0 opacity-20">
            <div 
              className="absolute inset-0 bg-gradient-radial from-transparent via-transparent to-black"
              style={{ 
                background: `radial-gradient(circle at center, transparent 0%, rgba(0, 255, 146, 0.1) 40%, transparent 80%)`
              }}
            ></div>
          </div>
          
          {/* 메인 로딩 컨테이너 */}
          <div className="relative z-10 text-center max-w-md mx-auto px-8">
            {/* 스타크래프트 스타일 로딩 스피너 */}
            <div className="relative mb-8">
              <div className="w-16 h-16 mx-auto relative">
                {/* 외부 회전링 */}
                <div 
                  className="absolute inset-0 border-2 border-transparent rounded-full animate-spin"
                  style={{ 
                    borderTopColor: isPro ? '#ffd700' : 'var(--starcraft-green)',
                    borderRightColor: isPro ? '#ffd700' : 'var(--starcraft-green)',
                    animationDuration: '2s'
                  }}
                ></div>
                {/* 내부 회전링 */}
                <div 
                  className="absolute inset-2 border-2 border-transparent rounded-full animate-spin"
                  style={{ 
                    borderLeftColor: isPro ? '#ffd700' : 'var(--starcraft-green)',
                    borderBottomColor: isPro ? '#ffd700' : 'var(--starcraft-green)',
                    animationDuration: '1.5s',
                    animationDirection: 'reverse'
                  }}
                ></div>
                {/* 중앙 펄스 도트 */}
                <div 
                  className="absolute inset-6 rounded-full animate-pulse"
                  style={{ backgroundColor: isPro ? '#ffd700' : 'var(--starcraft-green)' }}
                ></div>
              </div>
            </div>
            
            {/* 로딩 텍스트 */}
            <div className="space-y-3">
              <div 
                className="text-2xl font-bold tracking-wide"
                style={{ color: isPro ? '#ffd700' : 'var(--starcraft-green)' }}
              >
                {isPro ? 'PRO MODE' : 'FREE MODE'} 초기화 중
              </div>
              <div 
                className="text-sm font-mono opacity-80"
                style={{ color: isPro ? '#ffd700' : 'var(--starcraft-green)' }}
              >
                프리셋 데이터 로딩...
              </div>
            </div>
            
            {/* 하단 프로그레스 바 */}
            <div className="mt-8">
              <div 
                className="w-full h-1 rounded-full overflow-hidden"
                style={{ backgroundColor: 'var(--starcraft-border)' }}
              >
                <div 
                  className="h-full"
                  style={{ 
                    background: `linear-gradient(90deg, transparent 0%, ${isPro ? '#ffd700' : 'var(--starcraft-green)'} 50%, transparent 100%)`,
                    animation: 'loadingBar 2s ease-in-out infinite'
                  }}
                ></div>
              </div>
              <div 
                className="text-xs font-mono mt-2 opacity-60"
                style={{ color: 'var(--starcraft-inactive-text)' }}
              >
                {isPro ? 'Pro 기능' : '기본 기능'} 준비 중...
              </div>
            </div>
          </div>
        </div>
      );
    }

    // 3. 메인 단계 - 기존 로직 유지
    return renderMainView();
  };

  // 메인 뷰 렌더링 (기존 로직)
  const renderMainView = () => {
    switch (currentView) {
      case 'main':
        // preset이 로드되지 않았으면 로딩 화면 표시
        if (!presetsLoaded || presets.length === 0) {
          return (
            <div className="h-screen w-screen flex items-center justify-center relative overflow-hidden" style={{ backgroundColor: 'var(--starcraft-bg)' }}>
              {/* 배경 그라데이션 효과 */}
              <div className="absolute inset-0 opacity-20">
                <div 
                  className="absolute inset-0 bg-gradient-radial from-transparent via-transparent to-black"
                  style={{ 
                    background: `radial-gradient(circle at center, transparent 0%, rgba(0, 255, 146, 0.1) 40%, transparent 80%)`
                  }}
                ></div>
              </div>
              
              {/* 메인 로딩 컨테이너 */}
              <div className="relative z-10 text-center max-w-md mx-auto px-8">
                {/* 스타크래프트 스타일 로딩 스피너 */}
                <div className="relative mb-8">
                  <div className="w-16 h-16 mx-auto relative">
                    {/* 외부 회전링 */}
                    <div 
                      className="absolute inset-0 border-2 border-transparent rounded-full animate-spin"
                      style={{ 
                        borderTopColor: 'var(--starcraft-green)',
                        borderRightColor: 'var(--starcraft-green)',
                        animationDuration: '2s'
                      }}
                    ></div>
                    {/* 내부 회전링 */}
                    <div 
                      className="absolute inset-2 border-2 border-transparent rounded-full animate-spin"
                      style={{ 
                        borderLeftColor: 'var(--starcraft-green)',
                        borderBottomColor: 'var(--starcraft-green)',
                        animationDuration: '1.5s',
                        animationDirection: 'reverse'
                      }}
                    ></div>
                    {/* 중앙 펄스 도트 */}
                    <div 
                      className="absolute inset-6 rounded-full animate-pulse"
                      style={{ backgroundColor: 'var(--starcraft-green)' }}
                    ></div>
                  </div>
                </div>
                
                {/* 로딩 텍스트 */}
                <div className="space-y-3">
                  <div 
                    className="text-2xl font-bold tracking-wide"
                    style={{ color: 'var(--starcraft-green)' }}
                  >
                    STARCUP 초기화 중
                  </div>
                  <div 
                    className="text-sm font-mono opacity-80"
                    style={{ color: 'var(--starcraft-green)' }}
                  >
                    프리셋 데이터 로딩...
                  </div>
                </div>
                
                {/* 하단 프로그레스 바 */}
                <div className="mt-8">
                  <div 
                    className="w-full h-1 rounded-full overflow-hidden"
                    style={{ backgroundColor: 'var(--starcraft-border)' }}
                  >
                    <div 
                      className="h-full"
                      style={{ 
                        background: `linear-gradient(90deg, transparent 0%, var(--starcraft-green) 50%, transparent 100%)`,
                        animation: 'loadingBar 2s ease-in-out infinite'
                      }}
                    ></div>
                  </div>
                  <div 
                    className="text-xs font-mono mt-2 opacity-60"
                    style={{ color: 'var(--starcraft-inactive-text)' }}
                  >
                    시스템 연결 대기 중...
                  </div>
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
            isPro={isPro}
          />
        );

      case 'preset-settings':
        if (!presetsLoaded || presets.length === 0 || !currentPreset) {
          return (
            <div className="h-screen w-screen flex items-center justify-center relative overflow-hidden" style={{ backgroundColor: 'var(--starcraft-bg)' }}>
              {/* 배경 그라데이션 효과 */}
              <div className="absolute inset-0 opacity-20">
                <div 
                  className="absolute inset-0 bg-gradient-radial from-transparent via-transparent to-black"
                  style={{ 
                    background: `radial-gradient(circle at center, transparent 0%, rgba(0, 255, 146, 0.1) 40%, transparent 80%)`
                  }}
                ></div>
              </div>
              
              {/* 메인 로딩 컨테이너 */}
              <div className="relative z-10 text-center max-w-md mx-auto px-8">
                {/* 스타크래프트 스타일 로딩 스피너 */}
                <div className="relative mb-8">
                  <div className="w-16 h-16 mx-auto relative">
                    {/* 외부 회전링 */}
                    <div 
                      className="absolute inset-0 border-2 border-transparent rounded-full animate-spin"
                      style={{ 
                        borderTopColor: 'var(--starcraft-green)',
                        borderRightColor: 'var(--starcraft-green)',
                        animationDuration: '2s'
                      }}
                    ></div>
                    {/* 내부 회전링 */}
                    <div 
                      className="absolute inset-2 border-2 border-transparent rounded-full animate-spin"
                      style={{ 
                        borderLeftColor: 'var(--starcraft-green)',
                        borderBottomColor: 'var(--starcraft-green)',
                        animationDuration: '1.5s',
                        animationDirection: 'reverse'
                      }}
                    ></div>
                    {/* 중앙 펄스 도트 */}
                    <div 
                      className="absolute inset-6 rounded-full animate-pulse"
                      style={{ backgroundColor: 'var(--starcraft-green)' }}
                    ></div>
                  </div>
                </div>
                
                {/* 로딩 텍스트 */}
                <div className="space-y-3">
                  <div 
                    className="text-2xl font-bold tracking-wide"
                    style={{ color: 'var(--starcraft-green)' }}
                  >
                    프리셋 설정 로딩 중
                  </div>
                  <div 
                    className="text-sm font-mono opacity-80"
                    style={{ color: 'var(--starcraft-green)' }}
                  >
                    설정 데이터 준비...
                  </div>
                </div>
                
                {/* 하단 프로그레스 바 */}
                <div className="mt-8">
                  <div 
                    className="w-full h-1 rounded-full overflow-hidden"
                    style={{ backgroundColor: 'var(--starcraft-border)' }}
                  >
                    <div 
                      className="h-full"
                      style={{ 
                        background: `linear-gradient(90deg, transparent 0%, var(--starcraft-green) 50%, transparent 100%)`,
                        animation: 'loadingBar 2s ease-in-out infinite'
                      }}
                    ></div>
                  </div>
                  <div 
                    className="text-xs font-mono mt-2 opacity-60"
                    style={{ color: 'var(--starcraft-inactive-text)' }}
                  >
                    설정 인터페이스 준비 중...
                  </div>
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
            isPro={isPro}
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
            isPro={isPro}
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
            currentPreset={currentPreset}
            onSaveUpgradeSettings={handleSaveUpgradeSettings}
            tempUpgradeSettings={tempUpgradeSettings}
            onTempSave={handleTempSaveUpgradeSettings}
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
      {renderCurrentStage()}
    </div>
  );
}