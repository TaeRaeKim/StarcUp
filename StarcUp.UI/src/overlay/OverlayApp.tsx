import React, { useState, useEffect, useCallback, useRef } from 'react'
import { CenterPositionData } from '../../electron/src/services/types'
import { WorkerStatus, type WorkerStatusRef } from './components/WorkerStatus'
import { PopulationWarning } from './components/PopulationWarning'
import { OverlaySettingsPanel, type OverlaySettings } from './components/OverlaySettings'
import { type EffectType } from './hooks/useEffectSystem'
import './styles/OverlayApp.css'

export function OverlayApp() {
  const [centerPosition, setCenterPosition] = useState<CenterPositionData | null>(null)
  const [isVisible, setIsVisible] = useState(true)
  const [connectionStatus, setConnectionStatus] = useState<'connecting' | 'connected' | 'disconnected'>('connecting')
  const [lastUpdateTime, setLastUpdateTime] = useState<Date | null>(null)
  const [updateCount, setUpdateCount] = useState(0)
  const [frameRate, setFrameRate] = useState(0)
  const [lastEventType, setLastEventType] = useState<'immediate' | 'debounced' | null>(null)
  
  // WorkerManager 이벤트 상태
  const [workerStatus, setWorkerStatus] = useState<any>(null)
  const [lastWorkerEvent, setLastWorkerEvent] = useState<string | null>(null)
  const [gameStatus, setGameStatus] = useState<string>('waiting') // 'waiting', 'playing', 'game-ended'
  
  // PopulationManager 이벤트 상태
  const [showSupplyAlert, setShowSupplyAlert] = useState(false)
  const [isEditMode, setIsEditMode] = useState(false)
  const [workerPosition, setWorkerPosition] = useState({ x: 50, y: 50 })
  const [populationWarningPosition, setPopulationWarningPosition] = useState({ x: 100, y: 60 })
  const [isSettingsOpen, setIsSettingsOpen] = useState(false)
  const workerStatusRef = useRef<WorkerStatusRef>(null)
  
  // 프리셋 기능 상태 (presetAPI 연동)
  const [presetFeatures, setPresetFeatures] = useState<boolean[]>([
    true,   // 일꾼 기능 (Worker)
    false,  // 인구수 기능 (Population)
    false,  // 유닛 기능 (Unit)
    false,  // 업그레이드 기능 (Upgrade)
    false   // 빌드오더 기능 (BuildOrder)
  ])

  // 오버레이 설정 상태 (프리셋 기능 상태와 연동)
  const [overlaySettings, setOverlaySettings] = useState<OverlaySettings>({
    showWorkerStatus: true,
    showBuildOrder: false,
    showUnitCount: false,
    showUpgradeProgress: false,
    showPopulationWarning: false,
    opacity: 90,
    unitIconStyle: 'default',
    upgradeIconStyle: 'default',
    teamColor: '#0099FF'
  })

  // 기본 위치로 리셋하는 함수 (오버레이 컨테이너 기준)
  const resetToCenter = () => {
    const overlayContainer = document.querySelector('.overlay-container') as HTMLElement
    
    if (!overlayContainer) {
      console.warn('⚠️ 오버레이 컨테이너를 찾을 수 없습니다')
      return
    }
    
    const containerRect = overlayContainer.getBoundingClientRect()
    
    // WorkerStatus 위치 리셋
    const workerStatusElement = document.querySelector('.worker-status') as HTMLElement
    if (workerStatusElement) {
      const workerRect = workerStatusElement.getBoundingClientRect()
      const centerX = (containerRect.width - workerRect.width) / 2
      const centerY = (containerRect.height - workerRect.height) / 2
      
      setWorkerPosition({ x: centerX, y: centerY })
      console.log('🎯 WorkerStatus 위치 중앙으로 리셋:', { x: centerX, y: centerY })
    }

    // PopulationWarning 위치 리셋
    const populationWarningElement = document.querySelector('.population-warning') as HTMLElement
    if (populationWarningElement) {
      const warningRect = populationWarningElement.getBoundingClientRect()
      const centerX = (containerRect.width - warningRect.width) / 2
      const centerY = 60 // 상단에서 60px 떨어진 위치
      
      setPopulationWarningPosition({ x: centerX, y: centerY })
      console.log('🎯 PopulationWarning 위치 리셋:', { x: centerX, y: centerY })
    }
  }

  useEffect(() => {
    // Electron API가 사용 가능한지 확인
    if (typeof window !== 'undefined' && window.electronAPI) {
      // Electron 메인 프로세스로부터 중앙 위치 정보 수신
      const electronAPI = window.electronAPI as any
      if (electronAPI.onUpdateCenterPosition) {
        setConnectionStatus('connected')
        const unsubscribe = electronAPI.onUpdateCenterPosition((data: CenterPositionData) => {
          console.log('🎯 오버레이 중앙 위치 업데이트:', data)
          setCenterPosition(data)
          setLastUpdateTime(new Date())
          setUpdateCount(prev => prev + 1)
          
          // 이벤트 타입 감지 (콘솔 로그 기반 추정)
          if (data.x && data.y) {
            setLastEventType('immediate') // 실제로는 더 정확한 방법이 필요하지만 일단 immediate로 설정
          }
        })
        
        return unsubscribe
      } else {
        setConnectionStatus('disconnected')
        console.warn('⚠️ onUpdateCenterPosition 메서드를 찾을 수 없습니다')
      }
    } else {
      setConnectionStatus('disconnected')
      console.warn('⚠️ Electron API를 찾을 수 없습니다')
    }
  }, [])

  // 프리셋 기능 상태 초기화 (presetAPI 전용)
  useEffect(() => {
    const initializePresetFeatures = async () => {
      try {
        if (!window.presetAPI?.getFeaturesOnly) {
          console.error('❌ [Overlay] presetAPI.getFeaturesOnly를 사용할 수 없습니다')
          return
        }

        const result = await window.presetAPI.getFeaturesOnly()
        if (result?.success && result.data?.featureStates) {
          console.log('🎯 [Overlay] 초기 프리셋 기능 상태 로드:', result.data.featureStates)
          setPresetFeatures(result.data.featureStates)
        } else {
          console.error('❌ [Overlay] 프리셋 기능 상태 로드 실패:', result?.error)
        }
      } catch (error) {
        console.error('❌ [Overlay] 프리셋 기능 상태 초기화 실패:', error)
      }
    }

    initializePresetFeatures()
  }, [])

  // 실시간 프리셋 기능 상태 동기화 (presetAPI 전용)
  useEffect(() => {
    if (!window.presetAPI?.onFeaturesChanged || !window.presetAPI?.onStateChanged) {
      console.error('❌ [Overlay] presetAPI 이벤트 리스너를 사용할 수 없습니다')
      return
    }

    // Overlay 전용 최적화 이벤트 (빠른 응답)
    const unsubscribeFeaturesChanged = window.presetAPI.onFeaturesChanged((data) => {
      console.log('🔄 [Overlay] 프리셋 기능 상태 변경 수신 (최적화):', data.featureStates, '| 시간:', data.timestamp)
      setPresetFeatures(data.featureStates)
    })

    // Main 페이지 변경사항 감지용 포괄적 이벤트
    const unsubscribeStateChanged = window.presetAPI.onStateChanged((event) => {
      console.log('🔄 [Overlay] 프리셋 상태 변경 수신:', event.type, event)
      
      if (event.type === 'feature-toggled' || event.type === 'settings-updated') {
        // Main 페이지에서 변경된 경우 현재 프리셋의 기능 상태 동기화
        if (event.preset?.featureStates) {
          console.log('🔄 [Overlay] Main 페이지 변경사항으로 기능 상태 업데이트:', event.preset.featureStates)
          setPresetFeatures(event.preset.featureStates)
        }
      }
    })

    return () => {
      unsubscribeFeaturesChanged()
      unsubscribeStateChanged()
    }
  }, [])

  // 프리셋 기능 상태에 따른 overlaySettings 자동 업데이트
  useEffect(() => {
    console.log('🔄 [Overlay] overlaySettings 업데이트:', {
      showWorkerStatus: presetFeatures[0] || false,
      showPopulationWarning: presetFeatures[1] || false,
      showUnitCount: presetFeatures[2] || false,
      showUpgradeProgress: presetFeatures[3] || false,
      showBuildOrder: presetFeatures[4] || false,
    })
    
    setOverlaySettings(prev => ({
      ...prev,
      showWorkerStatus: presetFeatures[0] || false,
      showPopulationWarning: presetFeatures[1] || false,
      showUnitCount: presetFeatures[2] || false,
      showUpgradeProgress: presetFeatures[3] || false,
      showBuildOrder: presetFeatures[4] || false,
    }))
  }, [presetFeatures])

  // WorkerManager 이벤트 구독
  useEffect(() => {
    if (typeof window !== 'undefined' && window.electronAPI) {
      const electronAPI = window.electronAPI as any

      // WorkerManager 이벤트 리스너들
      const removeWorkerStatusListener = electronAPI.onWorkerStatusChanged && electronAPI.onWorkerStatusChanged((data: any) => {
        console.log('👷 [Overlay] 일꾼 상태 변경:', data)
        setWorkerStatus(data)
        setLastWorkerEvent('status-changed')
        
        // eventType에 따른 효과 트리거
        if (data.eventType && workerStatusRef.current) {
          const effectType = data.eventType as EffectType
          if (effectType === 'ProductionCompleted' || effectType === 'WorkerDied') {
            console.log(`✨ [Overlay] ${effectType} 효과 트리거`)
            workerStatusRef.current.triggerEffect(effectType)
          }
        }
      })

      const removeGasAlertListener = electronAPI.onGasBuildingAlert && electronAPI.onGasBuildingAlert(() => {
        console.log('⛽ [Overlay] 가스 건물 채취 중단 알림')
        setLastWorkerEvent('gas-alert')
      })

      const removePresetChangedListener = electronAPI.onWorkerPresetChanged && electronAPI.onWorkerPresetChanged((data: any) => {
        console.log('⚙️ [Overlay] 일꾼 프리셋 변경:', data)
        setLastWorkerEvent('preset-changed')
      })

      // PopulationManager supply-alert 이벤트 리스너
      const removeSupplyAlertListener = electronAPI.onSupplyAlert && electronAPI.onSupplyAlert(() => {
        console.log('⚠️ [Overlay] 인구 경고 알림 수신')
        setShowSupplyAlert(true)
        
        // 3초 후 알림 자동 해제
        setTimeout(() => {
          setShowSupplyAlert(false)
        }, 3000)
      })

      return () => {
        if (removeWorkerStatusListener) removeWorkerStatusListener()
        if (removeGasAlertListener) removeGasAlertListener()
        if (removePresetChangedListener) removePresetChangedListener()
        if (removeSupplyAlertListener) removeSupplyAlertListener()
      }
    }
  }, [])

  // 프레임 레이트 계산
  useEffect(() => {
    const interval = setInterval(() => {
      const now = Date.now()
      if (lastUpdateTime) {
        const timeDiff = now - lastUpdateTime.getTime()
        if (timeDiff < 5000) { // 5초 이내 업데이트가 있었다면
          setFrameRate(Math.round(1000 / 16)) // 16ms throttling 기준 예상 FPS
        } else {
          setFrameRate(0)
        }
      }
    }, 1000) // 1초마다 계산

    return () => clearInterval(interval)
  }, [lastUpdateTime])

  // Electron IPC를 통한 편집 모드 토글
  useEffect(() => {
    if (typeof window !== 'undefined' && window.electronAPI) {
      const electronAPI = window.electronAPI as any
      
      // 편집 모드 토글 이벤트 리스너
      if (electronAPI.onToggleEditMode) {
        console.log('🎯 편집 모드 IPC 리스너 등록')
        const unsubscribeEditMode = electronAPI.onToggleEditMode((data: { isEditMode: boolean }) => {
          console.log('🎯 편집 모드 토글 IPC 이벤트 수신:', data.isEditMode)
          setIsEditMode(data.isEditMode)
        })
        
        // 게임 상태 변경 이벤트 리스너 추가 (coreAPI에서 가져오기)
        const coreAPI = (window as any).coreAPI
        const unsubscribeGameStatus = coreAPI && coreAPI.onGameStatusChanged && coreAPI.onGameStatusChanged((data: { status: string }) => {
          console.log('🎮 [Overlay] 게임 상태 변경:', data.status, '| 현재 workerStatus:', workerStatus ? 'EXISTS' : 'NULL')
          setGameStatus(data.status)
        })
        
        return () => {
          unsubscribeEditMode()
          if (unsubscribeGameStatus) unsubscribeGameStatus()
        }
      } else {
        console.warn('⚠️ onToggleEditMode 메서드를 찾을 수 없습니다')
      }
    } else {
      console.warn('⚠️ Electron API를 찾을 수 없습니다')
    }
  }, [])


  // 편집모드가 해제될 때 설정창 자동 닫기
  useEffect(() => {
    if (!isEditMode && isSettingsOpen) {
      setIsSettingsOpen(false)
    }
  }, [isEditMode, isSettingsOpen])

  // 윈도우 위치/크기 변경 감지 및 아이템 위치 조정 (window-position-changed 이벤트 기반)
  useEffect(() => {
    if (!centerPosition) return

    const adjustItemPositions = () => {
      // 오버레이 윈도우의 실제 크기 (게임 영역 크기)
      const overlayWidth = centerPosition.gameAreaBounds.width
      const overlayHeight = centerPosition.gameAreaBounds.height
      
      console.log('🔧 [위치 조정] 오버레이 크기:', { width: overlayWidth, height: overlayHeight })
      
      // WorkerStatus 위치 조정
      const workerStatusElement = document.querySelector('.worker-status') as HTMLElement
      if (workerStatusElement) {
        const workerRect = workerStatusElement.getBoundingClientRect()
        const newWorkerX = Math.max(0, Math.min(overlayWidth - workerRect.width, workerPosition.x))
        const newWorkerY = Math.max(0, Math.min(overlayHeight - workerRect.height, workerPosition.y))
        
        if (newWorkerX !== workerPosition.x || newWorkerY !== workerPosition.y) {
          console.log('🔧 [위치 조정] WorkerStatus:', { from: workerPosition, to: { x: newWorkerX, y: newWorkerY } })
          setWorkerPosition({ x: newWorkerX, y: newWorkerY })
        }
      }

      // PopulationWarning 위치 조정
      const populationWarningElement = document.querySelector('.population-warning') as HTMLElement
      if (populationWarningElement) {
        const warningRect = populationWarningElement.getBoundingClientRect()
        const newWarningX = Math.max(0, Math.min(overlayWidth - warningRect.width, populationWarningPosition.x))
        const newWarningY = Math.max(0, Math.min(overlayHeight - warningRect.height, populationWarningPosition.y))
        
        if (newWarningX !== populationWarningPosition.x || newWarningY !== populationWarningPosition.y) {
          console.log('🔧 [위치 조정] PopulationWarning:', { from: populationWarningPosition, to: { x: newWarningX, y: newWarningY } })
          setPopulationWarningPosition({ x: newWarningX, y: newWarningY })
        }
      }
    }

    // centerPosition이 변경될 때마다 위치 조정 실행
    setTimeout(adjustItemPositions, 100) // DOM 업데이트 후 실행하기 위해 지연
    
  }, [centerPosition, workerPosition, populationWarningPosition])

  // 윈도우 크기에 따른 body 크기 동적 조정
  useEffect(() => {
    if (typeof document === 'undefined') return

    let dynamicBodyStyleElement = document.getElementById('dynamic-body-styles') as HTMLStyleElement
    
    if (!dynamicBodyStyleElement) {
      dynamicBodyStyleElement = document.createElement('style')
      dynamicBodyStyleElement.id = 'dynamic-body-styles'
      document.head.appendChild(dynamicBodyStyleElement)
    }

    const width = centerPosition?.gameAreaBounds.width
    const height = centerPosition?.gameAreaBounds.height
    
    dynamicBodyStyleElement.textContent = createDynamicBodyStyles(width, height)
    
    console.log('🔧 [Body 크기 조정]', { width, height })
  }, [centerPosition])


  return (
    <div 
      className="overlay-container"
      style={{
        width: centerPosition ? `${centerPosition.gameAreaBounds.width}px` : '100vw',
        height: centerPosition ? `${centerPosition.gameAreaBounds.height}px` : '100vh'
      }}
    >
      {/* 편집 모드 배경 효과 - 시각적 집중을 위한 오버레이 */}
      {isEditMode && (
        <div 
          className="edit-mode-backdrop"
          style={{
            position: 'absolute',
            top: 0,
            left: 0,
            width: '100%',
            height: '100%',
            background: `
              radial-gradient(ellipse at center, 
                rgba(0, 0, 0, 0.2) 0%, 
                rgba(0, 0, 0, 0.6) 70%, 
                rgba(0, 0, 0, 0.8) 100%
              ),
              linear-gradient(
                45deg,
                rgba(0, 153, 255, 0.05) 0%,
                transparent 50%,
                rgba(0, 153, 255, 0.05) 100%
              )
            `,
            pointerEvents: 'none',
            zIndex: 100,
            transition: 'all 0.3s ease-out',
            boxShadow: 'inset 0 0 100px rgba(0, 153, 255, 0.2)',
            filter: 'saturate(1.1)'
          }}
        />
      )}

      {/* 편집 모드 상태 표시 헤더 */}
      {isEditMode && (
        <div 
          className="edit-mode-header"
          style={{
            position: 'absolute',
            top: '20px',
            left: '50%',
            transform: 'translateX(-50%)',
            backgroundColor: '#0099ff',
            color: 'white',
            padding: '8px 16px',
            borderRadius: '6px',
            display: 'flex',
            alignItems: 'center',
            gap: '12px',
            boxShadow: '0 4px 20px rgba(0, 153, 255, 0.4)',
            zIndex: 15000,
            fontSize: '14px',
            fontWeight: '600',
            transition: 'all 0.3s ease-out',
            pointerEvents: 'auto'  // 헤더는 클릭 가능하도록
          }}
        >
          <span>편집 모드 활성화</span>
          <button
            onClick={resetToCenter}
            style={{
              backgroundColor: 'rgba(255, 255, 255, 0.2)',
              color: 'white',
              border: 'none',
              padding: '4px 8px',
              borderRadius: '4px',
              fontSize: '12px',
              cursor: 'pointer',
              transition: 'all 0.2s ease',
              pointerEvents: 'auto'  // 버튼 클릭 가능
            }}
            onMouseEnter={(e) => {
              e.currentTarget.style.backgroundColor = 'rgba(255, 255, 255, 0.3)'
            }}
            onMouseLeave={(e) => {
              e.currentTarget.style.backgroundColor = 'rgba(255, 255, 255, 0.2)'
            }}
          >
            모두 리셋
          </button>
        </div>
      )}

      {/* 오버레이 설정 버튼 - 편집모드에서만 표시 */}
      {isEditMode && (
        <button
          onClick={() => setIsSettingsOpen(true)}
          style={{
            position: 'absolute',
            top: '20px',
            right: '20px',
            width: '40px',
            height: '40px',
            borderRadius: '50%',
            border: 'none',
            backgroundColor: 'rgba(0, 0, 0, 0.7)',
            color: 'white',
            cursor: 'pointer',
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
            fontSize: '18px',
            zIndex: 15000,
            transition: 'all 0.2s ease',
            pointerEvents: 'auto',
            boxShadow: '0 4px 12px rgba(0, 0, 0, 0.3)'
          }}
          onMouseEnter={(e) => {
            e.currentTarget.style.backgroundColor = 'rgba(0, 153, 255, 0.8)'
            e.currentTarget.style.transform = 'scale(1.1)'
          }}
          onMouseLeave={(e) => {
            e.currentTarget.style.backgroundColor = 'rgba(0, 0, 0, 0.7)'
            e.currentTarget.style.transform = 'scale(1)'
          }}
        >
          ⚙️
        </button>
      )}

      {/* 일꾼 상태 오버레이 - InGame 상태 또는 편집 모드일 때 표시 */}
      {(() => {
        const shouldShow = ((gameStatus === 'playing' && workerStatus) || isEditMode) && overlaySettings.showWorkerStatus
        
        // 편집 모드용 더미 데이터 (InGame이 아닌 상태에서 4(4) 형태로 표시)
        const dummyWorkerData = {
          totalWorkers: 4,
          idleWorkers: 4,
          productionWorkers: 0,
          calculatedTotal: 4
        }
        
        // InGame 상태일 때는 실제 데이터, 아닐 때는 더미 데이터 4(4) 사용
        const workerData = gameStatus === 'playing' ? {
          totalWorkers: workerStatus?.totalWorkers || 0,
          idleWorkers: workerStatus?.idleWorkers || 0,
          productionWorkers: workerStatus?.productionWorkers || 0,
          calculatedTotal: workerStatus?.calculatedTotal || 0
        } : dummyWorkerData
        
        return shouldShow ? (
          <WorkerStatus
            ref={workerStatusRef}
            totalWorkers={workerData.totalWorkers}
            idleWorkers={workerData.idleWorkers}
            productionWorkers={workerData.productionWorkers}
            calculatedTotal={workerData.calculatedTotal}
            position={workerPosition}
            isEditMode={isEditMode}
            onPositionChange={setWorkerPosition}
            unitIconStyle={overlaySettings.unitIconStyle}
            teamColor={overlaySettings.teamColor}
            opacity={overlaySettings.opacity}
            isPreview={isEditMode && !workerStatus}
          />
        ) : null
      })()}

      {/* 인구 경고 알람 - PopulationWarning 컴포넌트 사용 */}
      {(() => {
        const shouldShow = overlaySettings.showPopulationWarning
        // 편집 모드에서는 미리보기로 표시, 일반 모드에서는 실제 알림 상태에 따라 표시
        const isVisibleState = isEditMode ? true : showSupplyAlert
        
        return shouldShow ? (
          <PopulationWarning
            isVisible={isVisibleState}
            message="인구수 한계 도달!"
            opacity={overlaySettings.opacity}
            position={populationWarningPosition}
            isEditMode={isEditMode}
            onPositionChange={setPopulationWarningPosition}
            isPreview={isEditMode}
          />
        ) : null
      })()}


      {/* 위치 정보가 없을 때 안내 */}
      {!centerPosition && (
        <div style={{
          position: 'absolute',
          top: '50%',
          left: '50%',
          transform: 'translate(-50%, -50%)',
          color: 'rgba(255,255,255,0.7)',
          fontSize: '16px',
          textAlign: 'center',
          fontFamily: 'Arial, sans-serif'
        }}>
          스타크래프트 윈도우 위치를 대기 중...
        </div>
      )}

      {/* 오버레이 설정 패널 */}
      <OverlaySettingsPanel
        isOpen={isSettingsOpen}
        onClose={() => setIsSettingsOpen(false)}
        settings={overlaySettings}
        onSettingsChange={setOverlaySettings}
      />
    </div>
  )
}


// 동적 body 크기 조정 스타일
const createDynamicBodyStyles = (width?: number, height?: number) => `
  html, body {
    width: ${width ? `${width}px` : '100vw'} !important;
    height: ${height ? `${height}px` : '100vh'} !important;
    background: transparent;
    overflow: hidden;
    margin: 0;
    padding: 0;
  }

  #root {
    width: ${width ? `${width}px` : '100vw'} !important;
    height: ${height ? `${height}px` : '100vh'} !important;
    background: transparent;
  }
`

