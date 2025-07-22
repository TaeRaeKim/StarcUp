import React, { useState, useCallback, useEffect, useRef, useImperativeHandle, forwardRef } from 'react'
import { useEffectSystem, type EffectType } from '../hooks/useEffectSystem'
import { getIconStyle } from '../utils/iconUtils'
import { HDIcon } from './HDIcon'

// 정적 애셋 import
import probeIconUrl from '/resources/Icon/Protoss/Units/ProtossProbe.png'
import probeDiffuseUrl from '/resources/HD/Protoss/Units/ProtossProbe_diffuse.png'
import probeTeamColorUrl from '/resources/HD/Protoss/Units/ProtossProbe_teamcolor.png'

interface WorkerStatusProps {
  totalWorkers: number
  idleWorkers: number
  productionWorkers: number
  calculatedTotal: number
  position: { x: number; y: number }
  isEditMode: boolean
  onPositionChange?: (position: { x: number; y: number }) => void
  unitIconStyle?: 'default' | 'white' | 'yellow' | 'hd'
  teamColor?: string
}

export interface WorkerStatusRef {
  triggerEffect: (effectType: EffectType) => void
}

export const WorkerStatus = forwardRef<WorkerStatusRef, WorkerStatusProps>(({
  totalWorkers,
  idleWorkers,
  productionWorkers,
  calculatedTotal,
  position,
  isEditMode,
  onPositionChange,
  unitIconStyle = 'default',
  teamColor = '#0099FF'
}, ref) => {
  const [isDragging, setIsDragging] = useState(false)
  const [dragOffset, setDragOffset] = useState({ x: 0, y: 0 })
  const workerStatusRef = useRef<HTMLDivElement>(null)
  const { triggerEffect } = useEffectSystem()

  // ref를 통해 외부에서 효과를 트리거할 수 있도록 노출
  useImperativeHandle(ref, () => ({
    triggerEffect: (effectType: EffectType) => {
      triggerEffect(workerStatusRef.current, effectType)
    }
  }), [triggerEffect])

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
      ref={workerStatusRef}
      className={`worker-status ${isEditMode ? 'edit-mode' : ''} ${isDragging ? 'dragging' : ''}`}
      onMouseDown={handleMouseDown}
      style={{
        left: `${position.x}px`,
        top: `${position.y}px`
      }}
    >
      {/* 일꾼 아이콘 */}
      <div style={getIconStyle(unitIconStyle, teamColor)}>
        {unitIconStyle === 'hd' ? (
          <HDIcon
            diffuseSrc={probeDiffuseUrl}
            teamColorSrc={probeTeamColorUrl}
            teamColor={teamColor}
            width={27}
            height={27}
            alt="Protoss Probe HD"
          />
        ) : (
          <img 
            src={probeIconUrl}
            alt="Protoss Probe" 
            style={{ 
              width: '27px', 
              height: '27px',
              imageRendering: 'pixelated'
            }} 
          />
        )}
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
          {calculatedTotal}
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
          A:{totalWorkers} P:{productionWorkers}
        </div>
      )}
    </div>
  )
})