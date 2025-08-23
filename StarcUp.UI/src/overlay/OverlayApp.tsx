import React, { useState, useEffect, useCallback, useRef } from 'react'
import { CenterPositionData } from '../../electron/src/services/types'
import { WorkerStatus, type WorkerStatusRef } from './components/WorkerStatus'
import { PopulationWarning } from './components/PopulationWarning'
import { UpgradeProgress, type UpgradeProgressRef } from './components/UpgradeProgress'
import { OverlaySettingsPanel, type OverlaySettings } from './components/OverlaySettings'
import { DraggableWrapper } from './components/DraggableWrapper'
import { SnapGuideOverlay } from './components/SnapGuideOverlay'
import { snapManager } from './services/SnapManager'
import { type EffectType } from './hooks/useEffectSystem'
import { RaceType } from '../types/game'
import {
  UpgradeCategory,
  UpgradeItemData
} from './types/upgrade'
import { WorkerStatusData, WorkerPresetData, UpgradeEventData } from '../types/preset'
import './styles/OverlayApp.css'
import { WorkerPresetFlags } from '@/utils/presetUtils'

// 환경 변수로 개발 모드 확인
const isDevelopment = process.env.NODE_ENV === 'development'

/**
 * OverlayApp - 스타크래프트 게임 위에 표시되는 오버레이 컴포넌트들의 메인 컨테이너
 * 
 * 새로운 오버레이 컴포넌트 추가 시 주의사항:
 * 1. gameStatus가 'playing'이 아닐 때 즉시 사라져야 하는 컴포넌트는 resetAllOverlayStates() 함수에 추가
 * 2. WorkerStatus처럼 gameStatus 조건으로 표시 여부가 결정되는 컴포넌트는 별도 처리 불필요
 * 3. PopulationWarning처럼 타이머로 관리되는 컴포넌트는 반드시 resetAllOverlayStates()에서 상태 초기화 필요
 */

export function OverlayApp() {
  const [centerPosition, setCenterPosition] = useState<CenterPositionData | null>(null)
  const [connectionStatus, setConnectionStatus] = useState<'connecting' | 'connected' | 'disconnected'>('connecting')

  // WorkerManager 이벤트 상태
  const [workerStatus, setWorkerStatus] = useState<WorkerStatusData | null>(null)
  const [gameStatus, setGameStatus] = useState<string>('waiting') // 'waiting', 'playing', 'game-ended'
  const [showIdleWorkers, setShowIdleWorkers] = useState(true) // Idle 플래그에 따른 표시 여부

  // PopulationManager 이벤트 상태
  const [showSupplyAlert, setShowSupplyAlert] = useState(false)

  // UpgradeManager 이벤트 상태
  const [upgradeCategories, setUpgradeCategories] = useState<UpgradeCategory[]>([])

  // 오버레이 컴포넌트들의 활성 상태를 관리하는 통합 함수
  const resetAllOverlayStates = useCallback(() => {
    // PopulationWarning 즉시 숨기기
    setShowSupplyAlert(false)

    // 향후 추가될 다른 오버레이 컴포넌트들의 상태도 여기서 초기화
    // 예시:
    // setBuildOrderAlert(false)
    // setUnitCountAlert(false) 
    // setUpgradeAlert(false)
    // setResourceAlert(false)

    // WorkerStatus는 gameStatus 조건에 의해 자동으로 숨겨지므로 별도 처리 불필요
  }, [])
  const [isEditMode, setIsEditMode] = useState(false)
  const [workerPosition, setWorkerPosition] = useState({ x: 2439, y: 59.2 })
  const [populationWarningPosition, setPopulationWarningPosition] = useState({ x: 1193.1, y: 960 })
  const [upgradeProgressPosition, setUpgradeProgressPosition] = useState({ x: 20, y: 642.5 })
  const [previousContainerSize, setPreviousContainerSize] = useState<{ width: number; height: number } | null>(null)
  const [isDraggingAny, setIsDraggingAny] = useState(false) // 전역 드래그 상태
  const [isSettingsOpen, setIsSettingsOpen] = useState(false)

  // 드래그 상태 관리 콜백
  const handleDragStateChange = useCallback((isDragging: boolean) => {
    setIsDraggingAny(isDragging)
  }, [])
  const workerStatusRef = useRef<WorkerStatusRef>(null)
  const upgradeProgressRef = useRef<UpgradeProgressRef>(null)

  // 프리셋 기능 상태 (presetAPI 연동) - 초기값은 null로 설정하여 로딩 상태 구분
  const [presetFeatures, setPresetFeatures] = useState<boolean[] | null>(null)

  // 프리셋의 선택된 종족
  const [selectedRace, setSelectedRace] = useState<RaceType>(RaceType.Protoss)

  // 프리셋의 업그레이드 설정
  const [presetUpgradeSettings, setPresetUpgradeSettings] = useState<any>(null)

  // 기본 프리셋 기능 상태 (로딩 실패 시 사용)
  const getDefaultFeatureStates = (): boolean[] => [
    false,   // 일꾼 기능 (Worker) - 기본적으로 활성화
    false,  // 인구수 기능 (Population)
    false,  // 유닛 기능 (Unit)
    false,  // 업그레이드 기능 (Upgrade)
    false   // 빌드오더 기능 (BuildOrder)
  ]

  // 안전한 프리셋 기능 상태 접근 (null 체크 포함)
  const safePresetFeatures = presetFeatures || getDefaultFeatureStates()

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


  useEffect(() => {
    // Electron API가 사용 가능한지 확인
    if (typeof window !== 'undefined' && window.electronAPI) {
      // Electron 메인 프로세스로부터 중앙 위치 정보 수신
      const electronAPI = window.electronAPI as any
      if (electronAPI.onUpdateCenterPosition) {
        setConnectionStatus('connected')
        const unsubscribe = electronAPI.onUpdateCenterPosition((data: CenterPositionData) => {
          setCenterPosition(data)
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

  // 프리셋 기능 상태와 종족 초기화 (presetAPI 전용)
  useEffect(() => {
    const initializePresetData = async () => {
      try {
        if (!window.presetAPI?.getFeaturesOnly) {
          console.error('❌ [Overlay] presetAPI.getFeaturesOnly를 사용할 수 없습니다')
          return
        }

        const result = await window.presetAPI.getFeaturesOnly()

        if (result?.success && result.data) {
          if (isDevelopment) console.log('✅ [Overlay] 프리셋 데이터 로드 성공:', result.data)
          
          if (result.data.featureStates && Array.isArray(result.data.featureStates)) {
            setPresetFeatures(result.data.featureStates)
          } else {
            if (isDevelopment) console.warn('⚠️ [Overlay] featureStates가 없거나 배열이 아님, 기본값 사용')
            setPresetFeatures(getDefaultFeatureStates())
          }

          if (result.data.selectedRace !== undefined) {
            setSelectedRace(result.data.selectedRace)
          }

          if (result.data.upgradeSettings) {
            setPresetUpgradeSettings(result.data.upgradeSettings)
          } else {
            setPresetUpgradeSettings(null)
          }
        } else {
          console.error('❌ [Overlay] 프리셋 데이터 로드 실패:', result?.error || 'API 호출 실패')
          setPresetFeatures(getDefaultFeatureStates())
          setPresetUpgradeSettings(null)
        }
      } catch (error) {
        console.error('❌ [Overlay] 프리셋 데이터 초기화 실패:', error)
        setPresetFeatures(getDefaultFeatureStates())
        setPresetUpgradeSettings(null)
      }
    }

    initializePresetData()
  }, [])

  // 실시간 프리셋 기능 상태 동기화 (presetAPI 전용)
  useEffect(() => {
    if (!window.presetAPI?.onFeaturesChanged) {
      console.error('❌ [Overlay] presetAPI 이벤트 리스너를 사용할 수 없습니다')
      return
    }
    // Overlay 전용 최적화 이벤트 (빠른 응답)
    const unsubscribeFeaturesChanged = window.presetAPI.onFeaturesChanged((data) => {

      if (data.featureStates && Array.isArray(data.featureStates)) {
        setPresetFeatures(data.featureStates)
      } else if (isDevelopment) {
        console.warn('⚠️ [Overlay] 실시간 동기화: featureStates가 없거나 배열이 아님')
      }

      // 종족 정보가 있는 경우 업데이트
      if (data.selectedRace !== undefined) {
        if (isDevelopment) {
          const raceNames = ['Zerg', 'Terran', 'Protoss']
          console.log('🔄 [Overlay] 종족 변경:', raceNames[data.selectedRace] || 'Unknown')
        }
        setSelectedRace(data.selectedRace)
      }

      // 업그레이드 설정 업데이트
      if (data.upgradeSettings !== undefined) {
        if (isDevelopment) console.log('🔄 [Overlay] 업그레이드 설정 변경:', data.upgradeSettings)
        setPresetUpgradeSettings(data.upgradeSettings)
      }
    })

    return () => {
      unsubscribeFeaturesChanged()
    }
  }, [])

  // 프리셋 기능 상태에 따른 overlaySettings 자동 업데이트
  useEffect(() => {
    const features = safePresetFeatures

    setOverlaySettings(prev => ({
      ...prev,
      showWorkerStatus: features[0] || false,
      showPopulationWarning: features[1] || false,
      showUnitCount: features[2] || false,
      showUpgradeProgress: features[3] || false,
      showBuildOrder: features[4] || false,
    }))
  }, [presetFeatures, safePresetFeatures])

  // WorkerManager 이벤트 구독
  useEffect(() => {
    if (typeof window !== 'undefined' && window.electronAPI) {
      const electronAPI = window.electronAPI as any

      // WorkerManager 이벤트 리스너들
      const removeWorkerStatusListener = electronAPI.onWorkerStatusChanged && electronAPI.onWorkerStatusChanged((data: WorkerStatusData) => {
        if (isDevelopment) console.log('👷 [Overlay] 일꾼 상태 변경:', data)
        setWorkerStatus(data)

        // eventType에 따른 효과 트리거
        if (data.eventType && workerStatusRef.current) {
          const effectType = data.eventType as EffectType
          if (effectType === 'ProductionCompleted' || effectType === 'WorkerDied') {
            if (isDevelopment) console.log(`✨ [Overlay] ${effectType} 효과 트리거`)
            workerStatusRef.current.triggerEffect(effectType)
          }
        }
      })

      const removeGasAlertListener = electronAPI.onGasBuildingAlert && electronAPI.onGasBuildingAlert(() => {
        if (isDevelopment) console.log('⛽ [Overlay] 가스 건물 채취 중단 알림')
      })

      const removePresetChangedListener = electronAPI.onWorkerPresetChanged && electronAPI.onWorkerPresetChanged((data: WorkerPresetData) => {
        if (isDevelopment) console.log('⚙️ [Overlay] 일꾼 프리셋 변경:', data)

        // Idle 플래그 확인 (flags 배열 또는 mask 값으로 체크)
        if (data?.success && data?.currentPreset) {
          const preset = data.currentPreset
          if (preset.mask !== undefined) {
            const hasIdleFlag = (preset.mask & WorkerPresetFlags.Idle) !== 0  // Idle 플래그는 비트 2 (값 4)
            setShowIdleWorkers(hasIdleFlag)
          }
        }
      })

      // PopulationManager supply-alert 이벤트 리스너
      const removeSupplyAlertListener = electronAPI.onSupplyAlert && electronAPI.onSupplyAlert(() => {
        setShowSupplyAlert(true)

        // 3초 후 알림 자동 해제
        setTimeout(() => {
          setShowSupplyAlert(false)
        }, 3000)
      })

      // 업그레이드 이벤트 리스너들 (Core에서 직접 전달되는 이벤트들)
      const removeUpgradeInitListener = electronAPI.onUpgradeInit && electronAPI.onUpgradeInit((data: UpgradeEventData) => {
        if (isDevelopment) {
          console.log('🚀 [Overlay] 업그레이드 카테고리 변경:', {
            categories: data.categories?.length || 0
          })
        }

        // 현재 데이터로 새롭게 초기화 (기존 데이터와 병합하지 않음)
        if (data.categories) {
          setUpgradeCategories(data.categories)
        }
      })

      const removeUpgradeDataUpdatedListener = electronAPI.onUpgradeDataUpdated && electronAPI.onUpgradeDataUpdated((data: UpgradeEventData) => {
        if (isDevelopment) {
          console.log('🔧 [Overlay] 업그레이드 데이터 업데이트:', {
            categories: data.categories?.length || 0
          })
        }

        // 개별 아이템 단위로 업데이트 (카테고리 전체 덮어쓰기 방지)
        if (data.categories) {
          setUpgradeCategories(prevCategories => {
            const updatedCategories = [...prevCategories];

            // 각 업데이트된 카테고리에 대해 처리
            data.categories!.forEach((updatedCategory) => {
              const categoryIndex = updatedCategories.findIndex(cat => cat.id === updatedCategory.id);

              if (categoryIndex !== -1) {
                // 기존 카테고리가 있으면 개별 아이템만 업데이트
                const existingCategory = updatedCategories[categoryIndex];
                const updatedItems = [...existingCategory.items];

                // 업데이트된 아이템들만 처리 (기존 아이템은 그대로 유지)
                updatedCategory.items.forEach((updatedItem: UpgradeItemData) => {
                  const itemIndex = updatedItems.findIndex((item) =>
                    item.item.type === updatedItem.item.type && item.item.value === updatedItem.item.value
                  );

                  if (itemIndex !== -1) {
                    // 기존 아이템의 데이터만 업데이트 (부분 업데이트)
                    updatedItems[itemIndex] = {
                      ...updatedItems[itemIndex],
                      ...updatedItem
                    };
                  }
                  // 새 아이템은 추가하지 않음 (데이터 업데이트 이벤트에서는 기존 아이템만 수정)
                });

                updatedCategories[categoryIndex] = {
                  ...existingCategory,
                  items: updatedItems
                };
              }
              // 새 카테고리도 추가하지 않음 (데이터 업데이트 이벤트에서는 기존 카테고리만 수정)
            });

            return updatedCategories;
          });
        }
      })

      const removeUpgradeCancelListener = electronAPI.onUpgradeCancelled && electronAPI.onUpgradeCancelled((data: UpgradeEventData) => {
        console.log('🎯 [Overlay] 업그레이드 취소 이벤트 수신:', data)
        if (isDevelopment) {
          console.log('❌ [Overlay] 업그레이드 취소:', data.item)
        }

        // 취소된 업그레이드 아이템의 상태를 개별 업데이트 (진행중 -> 비활성)
        if (data.item && data.categoryId !== undefined) {
          setUpgradeCategories(prevCategories => {
            const updatedCategories = [...prevCategories];
            const categoryIndex = updatedCategories.findIndex(cat => cat.id === String(data.categoryId));

            if (categoryIndex !== -1) {
              const category = updatedCategories[categoryIndex];
              const updatedItems = [...category.items];
              const itemIndex = updatedItems.findIndex((item) =>
                item.item.type === data.item!.type && item.item.value === data.item!.value
              );

              if (itemIndex !== -1) {
                // 개별 아이템만 업데이트: 진행 상태 해제 (level은 기존 완료 레벨 유지)
                updatedItems[itemIndex] = {
                  ...updatedItems[itemIndex],
                  remainingFrames: 0,
                  currentUpgradeLevel: 0 // 진행 중인 업그레이드 해제
                  // level은 그대로 유지 (이미 완료된 레벨은 변경하지 않음)
                };

                updatedCategories[categoryIndex] = {
                  ...category,
                  items: updatedItems
                };
              }
            }

            return updatedCategories;
          });
        }

        // 효과 트리거
        if (upgradeProgressRef.current) {
          upgradeProgressRef.current.triggerEffect('UpgradeCanceled')
        }
      })

      const removeUpgradeCompleteListener = electronAPI.onUpgradeCompleted && electronAPI.onUpgradeCompleted((data: UpgradeEventData) => {
        console.log('🎯 [Overlay] 업그레이드 완료 이벤트 수신:', data)
        if (isDevelopment) {
          console.log('✅ [Overlay] 업그레이드 완료:', data.item, 'level:', data.level)
        }

        // 완료된 업그레이드 아이템의 상태를 개별 업데이트 (즉시 완료 또는 진행중 -> 완료)
        if (data.item && data.categoryId !== undefined) {
          setUpgradeCategories(prevCategories => {
            const updatedCategories = [...prevCategories];
            const categoryIndex = updatedCategories.findIndex(cat => cat.id === String(data.categoryId));

            if (categoryIndex !== -1) {
              const category = updatedCategories[categoryIndex];
              const updatedItems = [...category.items];
              const itemIndex = updatedItems.findIndex((item) =>
                item.item.type === data.item!.type && item.item.value === data.item!.value
              );

              if (itemIndex !== -1) {
                const currentItem = updatedItems[itemIndex];

                // 개별 아이템만 업데이트: 진행 상태 해제, 완료 레벨 반영
                // 진행 중이지 않았더라도 즉시 완료되는 경우를 처리
                updatedItems[itemIndex] = {
                  ...currentItem,
                  remainingFrames: 0,
                  currentUpgradeLevel: 0, // 진행 중인 업그레이드 해제
                  level: data.level !== undefined ? data.level : currentItem.level // 완료된 레벨로 업데이트
                };

                if (isDevelopment) {
                  console.log('📝 [Overlay] 업그레이드 상태 업데이트:', {
                    item: data.item!.type,
                    newLevel: data.level !== undefined ? data.level : currentItem.level
                  })
                }

                updatedCategories[categoryIndex] = {
                  ...category,
                  items: updatedItems
                };
              }
            }

            return updatedCategories;
          });
        }

        // 효과 트리거
        if (upgradeProgressRef.current) {
          upgradeProgressRef.current.triggerEffect('UpgradeCompleted')
        }
      })

      return () => {
        if (removeWorkerStatusListener) removeWorkerStatusListener()
        if (removeGasAlertListener) removeGasAlertListener()
        if (removePresetChangedListener) removePresetChangedListener()
        if (removeSupplyAlertListener) removeSupplyAlertListener()
        if (removeUpgradeInitListener) removeUpgradeInitListener()
        if (removeUpgradeDataUpdatedListener) removeUpgradeDataUpdatedListener()
        if (removeUpgradeCancelListener) removeUpgradeCancelListener()
        if (removeUpgradeCompleteListener) removeUpgradeCompleteListener()
      }
    }
  }, [])


  // Electron IPC를 통한 편집 모드 토글
  useEffect(() => {
    if (typeof window !== 'undefined' && window.electronAPI) {
      const electronAPI = window.electronAPI as any

      // 편집 모드 토글 이벤트 리스너
      if (electronAPI.onToggleEditMode) {
        if (isDevelopment) console.log('🎯 편집 모드 IPC 리스너 등록')
        const unsubscribeEditMode = electronAPI.onToggleEditMode((data: { isEditMode: boolean }) => {
          if (isDevelopment) console.log('🎯 편집 모드 토글:', data.isEditMode)
          setIsEditMode(data.isEditMode)
        })

        // 게임 상태 변경 이벤트 리스너 추가 (coreAPI에서 가져오기)
        const coreAPI = (window as any).coreAPI
        const unsubscribeGameStatus = coreAPI && coreAPI.onGameStatusChanged && coreAPI.onGameStatusChanged((data: { status: string }) => {
          if (isDevelopment) console.log('🎮 [Overlay] 게임 상태 변경:', data.status)
          setGameStatus(data.status)

          // InGame 상태에서 벗어나면 모든 오버레이 컴포넌트 즉시 숨기기
          // 이렇게 하면 PopulationWarning처럼 타이머로 관리되는 컴포넌트들도 즉시 사라집니다
          if (data.status !== 'playing') {
            resetAllOverlayStates()
          }
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
  }, [resetAllOverlayStates, workerStatus])


  // 편집모드가 해제될 때 설정창 자동 닫기
  useEffect(() => {
    if (!isEditMode && isSettingsOpen) {
      setIsSettingsOpen(false)
    }
  }, [isEditMode, isSettingsOpen])

  // 화면 크기 변경 시 컴포넌트 위치 조정 (전체화면 ↔ 창모드 대응)
  useEffect(() => {
    if (!centerPosition || isDraggingAny) return // 드래그 중일 때는 위치 조정하지 않음

    const currentContainerSize = {
      width: centerPosition.gameAreaBounds.width,
      height: centerPosition.gameAreaBounds.height
    }

    // 화면 크기가 실제로 변경되었는지 확인
    if (previousContainerSize &&
      previousContainerSize.width === currentContainerSize.width &&
      previousContainerSize.height === currentContainerSize.height) {
      return // 크기가 변경되지 않았으면 조정하지 않음
    }

    const adjustPositionWithRatio = (
      currentPosition: { x: number; y: number },
      elementSelector: string,
      setPosition: (pos: { x: number; y: number }) => void,
      overlayId: string
    ) => {
      // 요소의 크기 추정 (실제 DOM 요소가 없을 경우 기본값 사용)
      const element = document.querySelector(elementSelector) as HTMLElement
      const elementWidth = element ? element.offsetWidth : 100
      const elementHeight = element ? element.offsetHeight : 50

      // SnapManager를 사용하여 위치 조정 (이전 크기 정보 포함)
      const adjustedPosition = snapManager.adjustPositionForScreenSize(
        overlayId,
        currentPosition,
        { width: elementWidth, height: elementHeight },
        currentContainerSize,
        previousContainerSize || undefined
      )

      // 위치가 변경되었으면 적용
      if (adjustedPosition.x !== currentPosition.x || adjustedPosition.y !== currentPosition.y) {
        setPosition(adjustedPosition)
        return true
      }

      return false
    }

    // 각 컴포넌트 위치 조정
    setTimeout(() => {
      if (isDevelopment) {
        console.log('📏 [OverlayApp] 화면 크기 변경으로 인한 위치 조정:',
          `${currentContainerSize.width}x${currentContainerSize.height}`)
      }

      adjustPositionWithRatio(workerPosition, '.worker-status', setWorkerPosition, 'workerStatus')
      adjustPositionWithRatio(populationWarningPosition, '.population-warning', setPopulationWarningPosition, 'populationWarning')
      adjustPositionWithRatio(upgradeProgressPosition, '.upgrade-progress-container', setUpgradeProgressPosition, 'upgradeProgress')

      // 현재 컨테이너 크기를 이전 크기로 저장
      setPreviousContainerSize(currentContainerSize)
    }, 100) // DOM 업데이트 후 실행

  }, [centerPosition?.gameAreaBounds.width, centerPosition?.gameAreaBounds.height, isDraggingAny])

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

    if (isDevelopment) console.log('🔧 [Body 크기 조정]', { width, height })
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
            onClick={() => {
              // SnapManager를 사용하여 모든 위치를 기본값으로 리셋
              const defaultPositions = snapManager.resetAllToDefaultPositions()
              setWorkerPosition(defaultPositions.workerStatus || { x: 50, y: 50 })
              setPopulationWarningPosition(defaultPositions.populationWarning || { x: 300, y: 50 })
              setUpgradeProgressPosition(defaultPositions.upgradeProgress || { x: 50, y: 200 })
            }}
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
          <DraggableWrapper
            id="workerStatus"
            position={workerPosition}
            onPositionChange={setWorkerPosition}
            isEditMode={isEditMode}
            className="worker-status-wrapper"
            onDragStateChange={handleDragStateChange}
          >
            <WorkerStatus
              ref={workerStatusRef}
              totalWorkers={workerData.totalWorkers}
              idleWorkers={workerData.idleWorkers}
              productionWorkers={workerData.productionWorkers}
              calculatedTotal={workerData.calculatedTotal}
              position={{ x: 0, y: 0 }} // DraggableWrapper가 위치를 처리하므로 0,0으로 설정
              isEditMode={false} // DraggableWrapper가 편집 모드를 처리
              onPositionChange={() => { }} // DraggableWrapper가 위치 변경을 처리
              unitIconStyle={overlaySettings.unitIconStyle}
              teamColor={overlaySettings.teamColor}
              opacity={overlaySettings.opacity}
              isPreview={isEditMode && !workerStatus}
              selectedRace={selectedRace}
              showIdleWorkers={showIdleWorkers}
            />
          </DraggableWrapper>
        ) : null
      })()}

      {/* 인구 경고 알람 - PopulationWarning 컴포넌트 사용 */}
      {(() => {
        const shouldShow = overlaySettings.showPopulationWarning
        // 편집 모드에서는 미리보기로 표시, 일반 모드에서는 실제 알림 상태에 따라 표시
        const isVisibleState = isEditMode ? true : showSupplyAlert

        return shouldShow ? (
          <DraggableWrapper
            id="populationWarning"
            position={populationWarningPosition}
            onPositionChange={setPopulationWarningPosition}
            isEditMode={isEditMode}
            className="population-warning-wrapper"
            onDragStateChange={handleDragStateChange}
          >
            <PopulationWarning
              isVisible={isVisibleState}
              message="인구수 한계 도달!"
              opacity={overlaySettings.opacity}
              position={{ x: 0, y: 0 }} // DraggableWrapper가 위치를 처리하므로 0,0으로 설정
              isEditMode={false} // DraggableWrapper가 편집 모드를 처리
              onPositionChange={() => { }} // DraggableWrapper가 위치 변경을 처리
              isPreview={isEditMode}
            />
          </DraggableWrapper>
        ) : null
      })()}

      {/* 업그레이드 진행 상태 - UpgradeProgress 컴포넌트 사용 */}
      {(() => {
        // WorkerStatus와 동일한 패턴: InGame 상태 또는 편집 모드일 때 항상 표시
        const shouldShow = ((gameStatus === 'playing') || isEditMode) && overlaySettings.showUpgradeProgress

        return shouldShow ? (
          <DraggableWrapper
            id="upgradeProgress"
            position={upgradeProgressPosition}
            onPositionChange={setUpgradeProgressPosition}
            isEditMode={isEditMode}
            className="upgrade-progress-wrapper"
            onDragStateChange={handleDragStateChange}
          >
            <UpgradeProgress
              ref={upgradeProgressRef}
              categories={upgradeCategories}
              position={{ x: 0, y: 0 }} // DraggableWrapper가 위치를 처리하므로 0,0으로 설정
              isEditMode={false} // DraggableWrapper가 편집 모드를 처리
              onPositionChange={() => { }} // DraggableWrapper가 위치 변경을 처리
              unitIconStyle={overlaySettings.upgradeIconStyle}
              opacity={overlaySettings.opacity}
              isPreview={isEditMode && upgradeCategories.length === 0}
              isInGame={gameStatus === 'playing'}
              presetUpgradeSettings={presetUpgradeSettings}
            />
          </DraggableWrapper>
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

      {/* 스냅 가이드 오버레이 */}
      <SnapGuideOverlay isEditMode={isEditMode} isDraggingAny={isDraggingAny} />

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

