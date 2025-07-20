import React, { useState, useCallback, useEffect } from 'react'

interface WorkerStatusProps {
  totalWorkers: number
  idleWorkers: number
  activeWorkers: number
  productionWorkers: number
  calculatedTotal: number
  position: { x: number; y: number }
  isEditMode: boolean
  onPositionChange?: (position: { x: number; y: number }) => void
}

export function WorkerStatus({
  totalWorkers,
  idleWorkers,
  activeWorkers,
  productionWorkers,
  calculatedTotal,
  position,
  isEditMode,
  onPositionChange
}: WorkerStatusProps) {
  const [isDragging, setIsDragging] = useState(false)
  const [dragOffset, setDragOffset] = useState({ x: 0, y: 0 })

  // 드래그 시작
  const handleMouseDown = (e: React.MouseEvent) => {
    if (!isEditMode) return
    
    e.preventDefault()
    setIsDragging(true)
    
    const overlayContainer = document.querySelector('.overlay-container') as HTMLElement
    if (overlayContainer) {
      const containerRect = overlayContainer.getBoundingClientRect()
      setDragOffset({
        x: e.clientX - containerRect.left - position.x,
        y: e.clientY - containerRect.top - position.y
      })
    }
  }

  // 드래그 중
  const handleMouseMove = useCallback((e: MouseEvent) => {
    if (!isDragging || !isEditMode) return
    
    const overlayContainer = document.querySelector('.overlay-container') as HTMLElement
    if (!overlayContainer || !onPositionChange) return
    
    const containerRect = overlayContainer.getBoundingClientRect()
    const newPosition = {
      x: e.clientX - containerRect.left - dragOffset.x,
      y: e.clientY - containerRect.top - dragOffset.y
    }
    
    // 경계 제한 (실제 컴포넌트 크기 계산)
    const workerStatusElement = document.querySelector('.worker-status') as HTMLElement
    const componentWidth = workerStatusElement ? workerStatusElement.offsetWidth : 100
    const componentHeight = workerStatusElement ? workerStatusElement.offsetHeight : 40
    
    const clampedX = Math.max(0, Math.min(containerRect.width - componentWidth, newPosition.x))
    const clampedY = Math.max(0, Math.min(containerRect.height - componentHeight, newPosition.y))
    
    onPositionChange({ x: clampedX, y: clampedY })
  }, [isDragging, isEditMode, dragOffset, onPositionChange])

  // 드래그 종료
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

  return (
    <div
      className="worker-status"
      onMouseDown={handleMouseDown}
      style={{
        position: 'absolute',
        left: `${position.x}px`,
        top: `${position.y}px`,
        backgroundColor: 'rgba(0, 0, 0, 0.85)',
        border: isEditMode ? '1px solid rgba(0, 153, 255, 0.5)' : 'none',
        borderRadius: '6px',
        padding: '8px 12px',
        display: 'flex',
        alignItems: 'center',
        gap: '10px',
        width: 'auto', // 컨텐츠에 맞게 자동 조정
        minWidth: '5.5em', // '99 (3)' 텍스트가 들어갈 수 있는 최소 크기 (폰트 크기 기반)
        zIndex: isEditMode ? 1001 : 1000,
        cursor: isEditMode ? 'move' : 'default',
        transition: isDragging ? 'none' : 'all 0.2s ease',
        boxShadow: isEditMode 
          ? '0 4px 20px rgba(0, 153, 255, 0.3)' 
          : 'none',
        pointerEvents: 'auto',
        userSelect: 'none'
      }}
    >
      {/* 일꾼 아이콘 */}
      <div
        style={{
          width: '27px',
          height: '27px',
          backgroundColor: '#FFB800',
          borderRadius: '3px',
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'center',
          fontSize: '14px',
          fontWeight: 'bold',
          color: '#000',
          flexShrink: 0
        }}
      >
        👷
      </div>

      {/* 일꾼 수 정보 */}
      <div style={{ display: 'flex', alignItems: 'center', gap: '4px' }}>
        <span
          style={{
            fontSize: '16px',
            fontWeight: '700',
            color: '#FFFFFF',
            lineHeight: '1'
          }}
        >
          {totalWorkers}
        </span>
        
        {idleWorkers > 0 && (
          <span
            style={{
              fontSize: '14px',
              fontWeight: '400',
              color: '#FFB800',
              lineHeight: '1'
            }}
          >
            ({idleWorkers})
          </span>
        )}
      </div>

      {/* 편집 모드일 때 추가 정보 표시 */}
      {isEditMode && (
        <div
          style={{
            fontSize: '10px',
            color: 'rgba(255, 255, 255, 0.7)',
            marginLeft: '4px'
          }}
        >
          A:{activeWorkers} P:{productionWorkers}
        </div>
      )}
    </div>
  )
}