import React, { useState, useEffect, useCallback } from 'react'
import { CenterPositionData } from '../../electron/src/services/types'

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
  const [isEditMode, setIsEditMode] = useState(false)
  const [debugPosition, setDebugPosition] = useState({ x: 10, y: 10 })
  const [isDragging, setIsDragging] = useState(false)
  const [dragOffset, setDragOffset] = useState({ x: 0, y: 0 })

  // 기본 위치로 리셋하는 함수 (오버레이 컨테이너 기준)
  const resetToCenter = () => {
    const overlayContainer = document.querySelector('.overlay-container') as HTMLElement
    if (overlayContainer) {
      const containerRect = overlayContainer.getBoundingClientRect()
      const centerX = containerRect.width / 2 - 160 // 패널 너비의 절반 정도
      const centerY = containerRect.height / 2 - 100 // 패널 높이의 절반 정도
      setDebugPosition({ x: centerX, y: centerY })
      console.log('🎯 디버그 패널 위치 중앙으로 리셋:', { x: centerX, y: centerY })
    } else {
      console.warn('⚠️ 오버레이 컨테이너를 찾을 수 없습니다')
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

  // WorkerManager 이벤트 구독
  useEffect(() => {
    if (typeof window !== 'undefined' && window.electronAPI) {
      const electronAPI = window.electronAPI as any

      // WorkerManager 이벤트 리스너들
      const removeWorkerStatusListener = electronAPI.onWorkerStatusChanged && electronAPI.onWorkerStatusChanged((data: any) => {
        console.log('👷 [Overlay] 일꾼 상태 변경:', data)
        setWorkerStatus(data)
        setLastWorkerEvent('status-changed')
      })

      const removeGasAlertListener = electronAPI.onGasBuildingAlert && electronAPI.onGasBuildingAlert(() => {
        console.log('⛽ [Overlay] 가스 건물 채취 중단 알림')
        setLastWorkerEvent('gas-alert')
      })

      const removePresetChangedListener = electronAPI.onWorkerPresetChanged && electronAPI.onWorkerPresetChanged((data: any) => {
        console.log('⚙️ [Overlay] 일꾼 프리셋 변경:', data)
        setLastWorkerEvent('preset-changed')
      })

      return () => {
        if (removeWorkerStatusListener) removeWorkerStatusListener()
        if (removeGasAlertListener) removeGasAlertListener()
        if (removePresetChangedListener) removePresetChangedListener()
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
      if (electronAPI.onToggleEditMode) {
        console.log('🎯 편집 모드 IPC 리스너 등록')
        const unsubscribe = electronAPI.onToggleEditMode((data: { isEditMode: boolean }) => {
          console.log('🎯 편집 모드 토글 IPC 이벤트 수신:', data.isEditMode)
          setIsEditMode(data.isEditMode)
        })
        
        return unsubscribe
      } else {
        console.warn('⚠️ onToggleEditMode 메서드를 찾을 수 없습니다')
      }
    } else {
      console.warn('⚠️ Electron API를 찾을 수 없습니다')
    }
  }, [])

  // 드래그 관련 이벤트 핸들러
  const handleMouseDown = (e: React.MouseEvent) => {
    if (!isEditMode) return
    
    e.preventDefault()
    setIsDragging(true)
    
    const rect = (e.target as HTMLElement).getBoundingClientRect()
    setDragOffset({
      x: e.clientX - rect.left,
      y: e.clientY - rect.top
    })
  }

  const handleMouseMove = useCallback((e: MouseEvent) => {
    if (!isDragging || !isEditMode) return
    
    const overlayContainer = document.querySelector('.overlay-container') as HTMLElement
    if (!overlayContainer) return
    
    const containerRect = overlayContainer.getBoundingClientRect()
    const newPosition = {
      x: e.clientX - containerRect.left - dragOffset.x,
      y: e.clientY - containerRect.top - dragOffset.y
    }
    
    // 오버레이 컨테이너 경계 제한
    const clampedX = Math.max(0, Math.min(containerRect.width - 320, newPosition.x))
    const clampedY = Math.max(0, Math.min(containerRect.height - 200, newPosition.y))
    
    setDebugPosition({ x: clampedX, y: clampedY })
  }, [isDragging, isEditMode, dragOffset])

  const handleMouseUp = useCallback(() => {
    if (!isDragging) return
    setIsDragging(false)
  }, [isDragging])

  useEffect(() => {
    if (isDragging) {
      document.addEventListener('mousemove', handleMouseMove)
      document.addEventListener('mouseup', handleMouseUp)
      
      return () => {
        document.removeEventListener('mousemove', handleMouseMove)
        document.removeEventListener('mouseup', handleMouseUp)
      }
    }
  }, [isDragging, handleMouseMove, handleMouseUp])

  // 개발 환경에서만 디버그 정보 표시
  const showDebugInfo = process.env.NODE_ENV === 'development'

  return (
    <div className="overlay-container">
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

      
      {/* 디버그 정보 (개발 환경에서만) */}
      {showDebugInfo && (
        <div 
          className={`debug-info ${isEditMode ? 'cursor-move' : ''}`}
          style={{
            position: 'absolute',
            top: `${debugPosition.y}px`,
            left: `${debugPosition.x}px`,
            cursor: isEditMode ? 'move' : 'default',
          background: 'rgba(0,0,0,0.9)',
          color: '#00ff00',
          padding: '12px',
          fontSize: '11px',
          borderRadius: '6px',
          fontFamily: 'monospace',
          border: `2px solid ${isEditMode ? '#0099ff' : connectionStatus === 'connected' ? '#00ff00' : connectionStatus === 'connecting' ? '#ffaa00' : '#ff0000'}`,
          zIndex: 10000,
          maxWidth: '320px',
          minWidth: '280px',
          transition: isDragging ? 'none' : 'all 0.2s ease',
          boxShadow: isEditMode ? '0 4px 20px rgba(0, 153, 255, 0.3)' : 'none'
        }}
          onMouseDown={handleMouseDown}
        >
          {isEditMode && (
            <div style={{ 
              marginBottom: '8px', 
              padding: '4px 8px',
              backgroundColor: 'rgba(0, 153, 255, 0.2)',
              borderRadius: '4px',
              fontSize: '10px',
              textAlign: 'center',
              color: '#0099ff'
            }}>
              📝 편집 모드 - 드래그하여 위치 조정
            </div>
          )}
          <div><strong>🔗 Connection Status:</strong></div>
          <div style={{ color: connectionStatus === 'connected' ? '#00ff00' : connectionStatus === 'connecting' ? '#ffaa00' : '#ff0000' }}>
            {connectionStatus === 'connected' ? '✅ Connected' : connectionStatus === 'connecting' ? '🔄 Connecting...' : '❌ Disconnected'}
          </div>
          <br />
          
          {centerPosition ? (
            <>
              <div><strong>🎯 Center Position:</strong></div>
              <div>X: {centerPosition.x.toFixed(1)}, Y: {centerPosition.y.toFixed(1)}</div>
              <br />
              <div><strong>🖼️ Game Area:</strong></div>
              <div>Size: {centerPosition.gameAreaBounds.width} × {centerPosition.gameAreaBounds.height}</div>
              <div>Position: ({centerPosition.gameAreaBounds.x}, {centerPosition.gameAreaBounds.y})</div>
              <br />
            </>
          ) : (
            <>
              <div><strong>⚠️ No Position Data</strong></div>
              <div style={{ color: '#ffaa00' }}>Waiting for StarcUp.Core data...</div>
              <br />
            </>
          )}
          
          <div><strong>⏰ Status:</strong></div>
          <div>Overlay: {isVisible ? '👁️ Visible' : '🙈 Hidden'}</div>
          <div>Last Update: {lastUpdateTime ? lastUpdateTime.toLocaleTimeString() : 'Never'}</div>
          <div>Current Time: {new Date().toLocaleTimeString()}</div>
          <br />
          <div><strong>📊 Performance:</strong></div>
          <div>Updates: {updateCount}</div>
          <div>FPS: {frameRate > 0 ? `~${frameRate}` : 'N/A'}</div>
          <div>Throttling: 16ms (60fps target)</div>
          <div>Debounce Delay: 50ms (last event guarantee)</div>
          <div>Last Event: {lastEventType || 'N/A'}</div>
          <br />
          <div><strong>👷 WorkerManager Events:</strong></div>
          <div>Last Event: {lastWorkerEvent || 'None'}</div>
          {workerStatus && (
            <>
              <div>Total: {workerStatus.totalWorkers} / Calc: {workerStatus.calculatedTotal}</div>
              <div>Idle: {workerStatus.idleWorkers} / Prod: {workerStatus.productionWorkers}</div>
              <div>Active: {workerStatus.activeWorkers}</div>
            </>
          )}
        </div>
      )}

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
    </div>
  )
}

// 전역 스타일
const overlayStyles = `
  .overlay-container {
    width: 100vw;
    height: 100vh;
    position: relative;
    overflow: hidden;
    background: transparent;
    pointer-events: none;
  }

  .debug-info {
    transition: opacity 0.3s ease;
  }

  .edit-mode-backdrop {
    backdrop-filter: blur(8px) saturate(1.2);
    -webkit-backdrop-filter: blur(8px) saturate(1.2);
  }
`

// 스타일 주입
if (typeof document !== 'undefined') {
  const styleElement = document.createElement('style')
  styleElement.textContent = overlayStyles
  document.head.appendChild(styleElement)
}