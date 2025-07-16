import { useState } from 'react';
import { ArrowLeft, Users, Wrench, AlertTriangle, Skull, Fuel, Info, Zap, Clock } from 'lucide-react';

interface WorkerDetailSettingsProps {
  isOpen: boolean;
  onClose: () => void;
}

export function WorkerDetailSettings({ isOpen, onClose }: WorkerDetailSettingsProps) {
  // 일꾼 관련 설정 상태들
  const [workerCountDisplay, setWorkerCountDisplay] = useState(true);
  const [idleWorkerDisplay, setIdleWorkerDisplay] = useState(true);
  const [workerProductionDetection, setWorkerProductionDetection] = useState(true);
  const [workerDeathDetection, setWorkerDeathDetection] = useState(true);
  const [gasWorkerCheck, setGasWorkerCheck] = useState(true);
  const [includeProducingWorkers, setIncludeProducingWorkers] = useState(false);

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
      id: 'idleWorker',
      title: '유휴 일꾼 수 출력',
      description: '놀고 있는 일꾼들을 찾아서 효율적으로 관리하세요',
      state: idleWorkerDisplay,
      setState: setIdleWorkerDisplay,
      icon: AlertTriangle
    },
    {
      id: 'workerProduction',
      title: '일꾼 생산 감지',
      description: '새로운 일꾼이 만들어질 때마다 파란색으로 알려드려요',
      state: workerProductionDetection,
      setState: setWorkerProductionDetection,
      icon: Zap
    },
    {
      id: 'workerDeath',
      title: '일꾼 사망 감지',
      description: '소중한 일꾼이 죽었을 때 빨간색으로 경고해드려요',
      state: workerDeathDetection,
      setState: setWorkerDeathDetection,
      icon: Skull
    },
    {
      id: 'gasWorker',
      title: '가스 일꾼 체크',
      description: '가스 건물마다 3마리씩 일하고 있는지 자동으로 체크해요',
      state: gasWorkerCheck,
      setState: setGasWorkerCheck,
      icon: Fuel
    },
    {
      id: 'includeProducing',
      title: '생산 중인 일꾼 수 포함',
      description: '아직 완성되지 않은 일꾼도 숫자에 포함해서 계산해요',
      state: includeProducingWorkers,
      setState: setIncludeProducingWorkers,
      icon: Clock
    }
  ];

  const handleSave = () => {
    // TODO: 설정 저장 로직
    const settingsToSave = {
      workerCountDisplay,
      idleWorkerDisplay,
      workerProductionDetection,
      workerDeathDetection,
      gasWorkerCheck,
      includeProducingWorkers
    };
    
    console.log('일꾼 설정 저장:', settingsToSave);
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
        </div>

        {/* 컨텐츠 - 스크롤 가능 */}
        <div className="flex-1 overflow-y-auto starcraft-scrollbar p-6 space-y-8">
            {/* 안내 문구 */}
            <div className="space-y-4">
              <h2 
                className="text-lg font-medium tracking-wide flex items-center gap-2"
                style={{ color: 'var(--starcraft-green)' }}
              >
                <Wrench className="w-5 h-5" />
                일꾼 기능 설정
              </h2>
              <div className="flex items-center gap-2">
                <Info className="w-4 h-4" style={{ color: 'var(--starcraft-blue)' }} />
                <p className="text-sm opacity-80" style={{ color: 'var(--starcraft-green)' }}>
                  각 기능을 개별적으로 활성화하거나 비활성화할 수 있습니다
                </p>
              </div>
            </div>

            {/* 설정 항목들 */}
            <div className="space-y-4">

          {settingItems.map((item) => {
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
                
                {/* 상태 표시 */}

              </div>
            );
          })}
        </div>

            {/* 하단 정보 */}
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
                <li>• 설정 변경사항은 즉시 적용됩니다</li>
                <li>• 게임 중에도 실시간으로 설정을 변경할 수 있습니다</li>
                <li>• 성능에 민감한 기능들은 필요시에만 활성화하세요</li>
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
    </div>
  );
}