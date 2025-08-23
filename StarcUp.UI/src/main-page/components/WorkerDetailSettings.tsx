import { useState, useEffect } from 'react';
import { ArrowLeft, Users, Wrench, AlertTriangle, Skull, Fuel, Info, Zap, Clock, Settings2 } from 'lucide-react';
import {
  debugWorkerSettings
} from '../../utils/presetUtils';
import { WorkerSettings } from '../../types/preset';
import { ProFeatureWrapper } from './ProFeatureWrapper';
import { ProBadge } from './ProBadge';
import { 
  getProStatus, 
  canUseFeature, 
  PRO_FEATURES, 
  getWorkerProFeatures,
  setDevProStatus,
  sanitizeWorkerSettingsForNonPro,
  type ProStatus 
} from '../../utils/proUtils';

// 일꾼 설정 인터페이스 (완전한 데이터 보장)
interface WorkerSettingsData {
  workerCountDisplay: boolean;
  includeProducingWorkers: boolean;
  idleWorkerDisplay: boolean;
  workerProductionDetection: boolean;
  workerDeathDetection: boolean;
  gasWorkerCheck: boolean;
}

interface WorkerDetailSettingsProps {
  isOpen: boolean;
  onClose: () => void;
  currentPreset: {
    id: string;
    name: string;
    description: string;
    workerSettings: WorkerSettingsData;
  };
  onSaveWorkerSettings: (presetId: string, workerSettings: WorkerSettings) => void;
  tempWorkerSettings?: WorkerSettings | null;
  onTempSave?: (settings: WorkerSettings) => void;
  isPro?: boolean;
}

export function WorkerDetailSettings({
  isOpen,
  onClose,
  currentPreset,
  onSaveWorkerSettings,
  tempWorkerSettings,
  onTempSave,
  isPro = false
}: WorkerDetailSettingsProps) {
  // 임시 저장된 값이 있으면 사용, 없으면 프리셋값 사용
  const initialSettings = tempWorkerSettings || currentPreset.workerSettings;
  
  // 일꾼 관련 설정 상태들 (프리셋값으로 초기화 - 완전한 데이터 보장)
  const [workerCountDisplay, setWorkerCountDisplay] = useState(() =>
    initialSettings.workerCountDisplay
  );
  const [idleWorkerDisplay, setIdleWorkerDisplay] = useState(() =>
    initialSettings.idleWorkerDisplay
  );
  const [workerProductionDetection, setWorkerProductionDetection] = useState(() =>
    initialSettings.workerProductionDetection
  );
  const [workerDeathDetection, setWorkerDeathDetection] = useState(() =>
    initialSettings.workerDeathDetection
  );
  const [gasWorkerCheck, setGasWorkerCheck] = useState(() =>
    initialSettings.gasWorkerCheck
  );
  const [includeProducingWorkers, setIncludeProducingWorkers] = useState(() =>
    initialSettings.includeProducingWorkers
  );

  // 변경사항 감지 상태
  const [hasChanges, setHasChanges] = useState(false);

  // Pro 상태 관리 (앱에서 전달받은 값 사용)
  const workerProFeatures = getWorkerProFeatures();
  
  // 간단한 ProStatus 객체 생성
  const createProStatus = (isPro: boolean): ProStatus => ({
    isPro,
    subscriptionType: isPro ? 'pro' : 'free',
    features: isPro ? workerProFeatures.map(id => ({
      id,
      name: '',
      description: '',
      category: 'worker' as const,
      isEnabled: true,
      requiredTier: 'pro' as const
    })) : []
  });


  // 프리셋 변경 시 일꾼 설정 업데이트 (완전한 데이터 보장)
  useEffect(() => {
    console.log('🔧 WorkerDetailSettings 프리셋 변경:', currentPreset);

    // 임시 저장된 값이 있으면 사용, 없으면 프리셋값 사용
    let settings = tempWorkerSettings || currentPreset.workerSettings;
    
    // Pro가 아닌 경우에만 Pro 기능 해제
    // Pro 사용자라면 원본 설정 그대로 사용
    if (!isPro) {
      settings = sanitizeWorkerSettingsForNonPro(settings);
      console.log('🔒 Free 모드: 일꾼 Pro 기능 해제 적용됨');
    }
    
    console.log('🔧 일꾼 설정 업데이트:', settings);
    setWorkerCountDisplay(settings.workerCountDisplay);
    setIncludeProducingWorkers(settings.includeProducingWorkers);
    setIdleWorkerDisplay(settings.idleWorkerDisplay);
    setWorkerProductionDetection(settings.workerProductionDetection);
    setWorkerDeathDetection(settings.workerDeathDetection);
    setGasWorkerCheck(settings.gasWorkerCheck);
  }, [currentPreset, tempWorkerSettings, isPro]);

  // 변경사항 감지 - 원본 프리셋 설정과 현재 설정 비교
  useEffect(() => {
    const originalSettings = currentPreset.workerSettings;
    const currentSettings = {
      workerCountDisplay,
      includeProducingWorkers,
      idleWorkerDisplay,
      workerProductionDetection,
      workerDeathDetection,
      gasWorkerCheck
    };

    const hasAnyChanges = (
      originalSettings.workerCountDisplay !== currentSettings.workerCountDisplay ||
      originalSettings.includeProducingWorkers !== currentSettings.includeProducingWorkers ||
      originalSettings.idleWorkerDisplay !== currentSettings.idleWorkerDisplay ||
      originalSettings.workerProductionDetection !== currentSettings.workerProductionDetection ||
      originalSettings.workerDeathDetection !== currentSettings.workerDeathDetection ||
      originalSettings.gasWorkerCheck !== currentSettings.gasWorkerCheck
    );

    setHasChanges(hasAnyChanges);
  }, [
    workerCountDisplay,
    includeProducingWorkers,
    idleWorkerDisplay,
    workerProductionDetection,
    workerDeathDetection,
    gasWorkerCheck,
    currentPreset.workerSettings
  ]);

  const settingItems = [
    {
      id: 'workerCount',
      title: '일꾼 수 출력',
      description: '게임 중 일꾼이 몇 마리인지 한눈에 확인할 수 있어요',
      state: workerCountDisplay,
      setState: setWorkerCountDisplay,
      icon: Users
    },
    {
      id: 'includeProducing',
      title: '생산 중인 일꾼 수 포함',
      description: '아직 완성되지 않은 일꾼도 숫자에 포함해서 계산해요',
      state: includeProducingWorkers,
      setState: setIncludeProducingWorkers,
      icon: Clock
    },
    {
      id: 'idleWorker',
      title: '유휴 일꾼 수 출력',
      description: '놀고 있는 일꾼들을 찾아서 효율적으로 관리하세요',
      state: idleWorkerDisplay,
      setState: setIdleWorkerDisplay,
      icon: AlertTriangle
    },
    {
      id: PRO_FEATURES.WORKER_PRODUCTION_DETECTION,
      title: '일꾼 생산 감지',
      description: '새로운 일꾼이 만들어질 때마다 파란색으로 알려드려요',
      state: workerProductionDetection,
      setState: setWorkerProductionDetection,
      icon: Zap
    },
    {
      id: PRO_FEATURES.WORKER_DEATH_DETECTION,
      title: '일꾼 사망 감지',
      description: '소중한 일꾼이 죽었을 때 빨간색으로 경고해드려요',
      state: workerDeathDetection,
      setState: setWorkerDeathDetection,
      icon: Skull
    },
    {
      id: PRO_FEATURES.GAS_WORKER_CHECK,
      title: '가스 일꾼 체크',
      description: '가스 건물마다 3마리씩 일하고 있는지 자동으로 체크해요',
      state: gasWorkerCheck,
      setState: setGasWorkerCheck,
      icon: Fuel
    }
  ];

  const handleConfirm = () => {
    // 일꾼 설정 정보 구성
    const settingsToSave: WorkerSettings = {
      workerCountDisplay,
      includeProducingWorkers,
      idleWorkerDisplay,
      workerProductionDetection,
      workerDeathDetection,
      gasWorkerCheck
    };

    console.log('💾 일꾼 설정 임시 저장:', settingsToSave);

    // 임시 저장 함수가 있으면 임시 저장만 수행
    if (onTempSave) {
      onTempSave(settingsToSave);
    } else {
      // 임시 저장 함수가 없으면 기존처럼 직접 저장
      onSaveWorkerSettings(currentPreset.id, settingsToSave);
    }

    // 디버깅 정보 출력
    debugWorkerSettings(settingsToSave);

    // 설정 창 닫기
    onClose();
  };

  if (!isOpen) return null;

  return (
    <>
      <div className="h-screen overflow-hidden border-2 shadow-2xl"
      style={{
        backgroundColor: 'var(--starcraft-bg)',
        border: '1px solid var(--main-container-border)',
      }}
    >
      {/* 전체 화면 컨테이너 */}
      <div
        className="flex flex-col h-full">
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

            <Wrench
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
                일꾼 설정
              </h1>
              <p
                className="text-sm opacity-70"
                style={{ color: 'var(--starcraft-green)' }}
              >
                일꾼 관련 기능들을 개별적으로 설정하세요
              </p>
            </div>
          </div>
          
          {/* Pro 상태 표시 */}
          <div className="flex items-center gap-2">
            {isPro ? (
              <div className="flex items-center gap-2 px-3 py-1 rounded-full"
                style={{
                  background: 'linear-gradient(135deg, #ffd700 0%, #ffed4e 50%, #ffa000 100%)',
                  color: '#8b5a00',
                  boxShadow: '0 0 8px rgba(255, 215, 0, 0.3)'
                }}
              >
                <Zap className="w-3 h-3" />
                <span className="text-xs font-bold">Pro 활성화</span>
              </div>
            ) : (
              <ProBadge />
            )}
          </div>
        </div>

        {/* 컨텐츠 - 스크롤 가능 */}
        <div className="flex-1 overflow-y-auto starcraft-scrollbar p-6 space-y-8">
          {/* 안내 문구 */}
          <div className="space-y-4">
            <h2
              className="text-lg font-medium tracking-wide flex items-center gap-2"
              style={{ color: 'var(--starcraft-info)' }}
            >
              {/* <Wrench className="w-5 h-5" style={{ color: 'var(--starcraft-blue)' }}/> */}
              일꾼 기능 설정
            </h2>
            <div className="flex items-center gap-2">
              <Info className="w-4 h-4" style={{ color: 'var(--starcraft-blue)' }} />
              <p className="text-sm opacity-80" style={{ color: 'var(--starcraft-info)' }}>
                각 기능을 개별적으로 활성화하거나 비활성화할 수 있습니다
                {!isPro && (
                  <span className="ml-2 text-xs" style={{ color: '#ffd700' }}>
                    💎 일부 고급 기능은 Pro 구독이 필요합니다
                  </span>
                )}
              </p>
            </div>
          </div>

          {/* 설정 항목들 */}
          <div className="space-y-4">

            {settingItems.map((item) => {
              const IconComponent = item.icon;
              const isProFeature = workerProFeatures.includes(item.id);
              const canUse = canUseFeature(item.id, createProStatus(isPro));
              const isDisabled = isProFeature && !canUse;
              
              // Pro 기능별 설명 매핑
              const proDescriptions: Record<string, string> = {
                [PRO_FEATURES.WORKER_PRODUCTION_DETECTION]: '일꾼 생산을 실시간으로 감지하여 경제 관리의 정확성을 높입니다.',
                [PRO_FEATURES.WORKER_DEATH_DETECTION]: '일꾼 손실을 즉시 감지하여 빠른 대응이 가능합니다.',
                [PRO_FEATURES.GAS_WORKER_CHECK]: '가스 효율성을 최적화하기 위한 고급 분석 기능입니다.'
              };

              const SettingContent = (
                <div 
                  className={`p-4 rounded-lg border transition-all duration-300 hover:border-opacity-80 ${isDisabled ? 'cursor-not-allowed' : ''}`}
                  style={{
                    backgroundColor: item.state
                          ? 'var(--starcraft-inactive-bg)'
                          : 'var(--starcraft-bg-secondary)',
                    borderColor: item.state 
                          ? 'var(--starcraft-red-bright)' 
                          : 'var(--starcraft-red)'
                  }}
                >
                  <div className="flex items-start justify-between gap-4">
                    <div className="flex items-start gap-3 flex-1">
                      <div className="p-2 rounded-lg mt-1 relative"
                        style={{
                          backgroundColor: item.state && !isDisabled
                            ? 'var(--starcraft-bg-active)'
                            : 'var(--starcraft-bg)',
                          color: item.state && !isDisabled
                            ? 'var(--starcraft-green)'
                            : 'var(--starcraft-inactive-text)'
                        }}
                      >
                        <IconComponent className="w-5 h-5" />
                        {/* Pro 아이콘 표시 */}
                        {isProFeature && (
                          <div className="absolute -top-1 -left-1">
                            <ProBadge variant="icon" className="scale-75" />
                          </div>
                        )}
                      </div>
                      <div className="flex-1">
                        <div className="flex items-center gap-2 mb-2">
                          <h3 className="font-medium tracking-wide"
                            style={{
                              color: item.state && !isDisabled
                                ? 'var(--starcraft-green)'
                                : isDisabled
                                  ? 'var(--starcraft-inactive-text)'
                                  : 'var(--starcraft-inactive-text)'
                            }}
                          >
                            {item.title}
                          </h3>
                          {isProFeature && (
                            <ProBadge variant="small" className="scale-90" />
                          )}
                        </div>
                        <p className="text-sm opacity-80 leading-relaxed mb-1"
                          style={{
                            color: item.state && !isDisabled
                              ? 'var(--starcraft-green)'
                              : isDisabled
                                ? 'var(--starcraft-inactive-text)'
                                : 'var(--starcraft-inactive-text)'
                          }}
                        >
                          {item.description}
                        </p>
                        {/* Pro 기능 추가 설명 */}
                        {isProFeature && isDisabled && (
                          <p className="text-xs opacity-60 mt-1" style={{ color: '#ffd700' }}>
                            💎 {proDescriptions[item.id] || 'Pro 구독으로 이 고급 기능을 이용하세요.'}
                          </p>
                        )}
                      </div>
                    </div>

                    <div className="flex items-center">
                      <button
                        onClick={() => !isDisabled && item.setState(!item.state)}
                        disabled={isDisabled}
                        className={`relative inline-flex h-6 w-11 items-center rounded-full transition-colors duration-300 focus:outline-none focus:ring-2 focus:ring-offset-2 ${item.state && !isDisabled
                            ? 'focus:ring-green-500'
                            : 'focus:ring-gray-500'
                          } ${isDisabled ? 'cursor-not-allowed opacity-50' : ''}`}
                        style={{
                          backgroundColor: item.state && !isDisabled
                            ? 'var(--starcraft-green)'
                            : isDisabled
                              ? 'var(--starcraft-inactive-secondary)'
                              : 'var(--starcraft-inactive-bg)'
                        }}
                      >
                        <span
                          className={`inline-block h-4 w-4 transform rounded-full transition-transform duration-300 ${item.state && !isDisabled ? 'translate-x-6' : 'translate-x-1'
                            }`}
                          style={{
                            backgroundColor: item.state && !isDisabled
                              ? 'var(--starcraft-bg)'
                              : 'var(--starcraft-inactive-secondary)'
                          }}
                        />
                      </button>
                    </div>
                  </div>
                </div>
              );

              // Pro 기능인 경우 ProFeatureWrapper로 감싸기
              if (isProFeature) {
                return (
                  <ProFeatureWrapper
                    key={item.id}
                    isPro={isPro}
                    feature={item.title}
                    description={proDescriptions[item.id]}
                    disabled={!canUse}
                  >
                    {SettingContent}
                  </ProFeatureWrapper>
                );
              }

              // 일반 기능인 경우 그대로 렌더링
              return (
                <div key={item.id}>
                  {SettingContent}
                </div>
              );
            })}
          </div>

          {/* 하단 정보 */}
          <div className="p-4 rounded-lg border"
            style={{
              backgroundColor: isPro ? 'var(--starcraft-bg-active)' : 'var(--starcraft-bg-secondary)',
              borderColor: isPro ? 'var(--starcraft-green)' : '#ffd700',
              color: isPro ? 'var(--starcraft-green)' : 'var(--starcraft-green)'
            }}
          >
            <div className="flex items-center gap-2 mb-2">
              <Info className="w-4 h-4" />
              <span className="text-sm font-medium">
                {isPro ? '설정 안내' : 'Pro 기능 안내'}
              </span>
              {!isPro && (
                <ProBadge variant="small" className="ml-auto" />
              )}
            </div>
            
            {isPro ? (
              <ul className="text-xs space-y-1 opacity-90 pl-6">
                <li>• 모든 Pro 기능이 활성화되어 있습니다</li>
                <li>• 설정 변경사항은 저장 시 적용됩니다</li>
                <li>• 게임 중에도 실시간으로 설정을 변경할 수 있습니다</li>
                <li>• 고급 기능들로 더욱 정확한 게임 분석이 가능합니다</li>
              </ul>
            ) : (
              <div className="space-y-3">
                <ul className="text-xs space-y-1 opacity-90 pl-6">
                  <li>• <span style={{ color: '#ffd700' }}>일꾼 생산 감지</span>: 실시간 생산 알림으로 정확한 타이밍 관리</li>
                  <li>• <span style={{ color: '#ffd700' }}>일꾼 사망 감지</span>: 즉각적인 손실 파악으로 빠른 대응</li>
                  <li>• <span style={{ color: '#ffd700' }}>가스 일꾼 체크</span>: 자동 효율성 분석으로 최적화된 경제 관리</li>
                </ul>
                <button
                  className="w-full mt-3 py-2 px-4 rounded-lg font-medium text-sm transition-all duration-300 hover:scale-105 flex items-center justify-center gap-2"
                  style={{
                    background: 'linear-gradient(135deg, #ffd700 0%, #ffed4e 50%, #ffa000 100%)',
                    color: '#8b5a00',
                    boxShadow: '0 4px 12px rgba(255, 215, 0, 0.3)'
                  }}
                  onClick={() => {
                    console.log('Pro 업그레이드 페이지로 이동');
                    // 실제 구현에서는 Pro 구독 모달이나 페이지를 열 것입니다.
                  }}
                >
                  <Zap className="w-4 h-4" />
                  Pro로 업그레이드하고 모든 기능 사용하기
                  <ArrowLeft className="w-4 h-4 rotate-180" />
                </button>
              </div>
            )}
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
              color: 'var(--starcup-red)',
              borderColor: 'var(--starcup-red)'
            }}
          >
            취소
          </button>

          <button
            onClick={handleConfirm}
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
            <Settings2 className="w-4 h-4" />
            확인
          </button>
        </div>
      </div>
    </div>
    </>
  );
}