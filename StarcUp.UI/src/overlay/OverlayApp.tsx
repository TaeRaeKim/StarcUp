import React, { useState, useEffect } from 'react'
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

  // 개발 환경에서만 디버그 정보 표시
  const showDebugInfo = process.env.NODE_ENV === 'development'

  return (
    <div className="overlay-container">
      {/* Hello World 중앙 배치 */}
      {isVisible && centerPosition && (
        <div 
          className="hello-world"
          style={{
            position: 'absolute',
            left: `${centerPosition.x}px`,
            top: `${centerPosition.y}px`,
            transform: 'translate(-50%, -50%)',
            color: 'white',
            fontSize: '32px',
            fontWeight: 'bold',
            textShadow: '2px 2px 4px rgba(0,0,0,0.8)',
            zIndex: 9999,
            pointerEvents: 'none',
            userSelect: 'none',
            fontFamily: 'Arial, sans-serif'
          }}
        >
          Hello World
        </div>
      )}
      
      {/* 디버그 정보 (개발 환경에서만) */}
      {showDebugInfo && (
        <div className="debug-info" style={{
          position: 'absolute',
          top: '10px',
          left: '10px',
          background: 'rgba(0,0,0,0.9)',
          color: '#00ff00',
          padding: '12px',
          fontSize: '11px',
          borderRadius: '6px',
          fontFamily: 'monospace',
          border: `2px solid ${connectionStatus === 'connected' ? '#00ff00' : connectionStatus === 'connecting' ? '#ffaa00' : '#ff0000'}`,
          zIndex: 10000,
          maxWidth: '320px',
          minWidth: '280px'
        }}>
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

  .hello-world {
    transition: all 0.1s ease-out;
  }

  .debug-info {
    transition: opacity 0.3s ease;
  }
`

// 스타일 주입
if (typeof document !== 'undefined') {
  const styleElement = document.createElement('style')
  styleElement.textContent = overlayStyles
  document.head.appendChild(styleElement)
}