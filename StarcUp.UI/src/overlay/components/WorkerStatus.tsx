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
  opacity?: number // 오버레이 설정의 투명도 (0-100)
  isPreview?: boolean // 편집 모드에서 미리보기로 표시되는지 여부
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
  teamColor = '#0099FF',
  opacity = 90,
  isPreview = false
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
    <div style={{ position: 'relative' }}>
      <div
        ref={workerStatusRef}
        className={`worker-status ${isEditMode ? 'edit-mode' : ''} ${isDragging ? 'dragging' : ''} ${isPreview ? 'preview-mode' : ''}`}
        onMouseDown={handleMouseDown}
        style={{
          left: `${position.x}px`,
          top: `${position.y}px`,
          // 편집 모드일 때는 미리보기 투명도만 적용, 일반 모드일 때는 배경색 opacity만 조절
          opacity: isEditMode && isPreview ? 0.75 : 1,
          backgroundColor: isEditMode ? 'var(--color-overlay-bg)' : `rgba(0, 0, 0, ${opacity / 100})`,
          filter: isPreview ? 'brightness(0.9)' : 'none',
          boxShadow: isPreview ? '0 0 0 2px rgba(0, 153, 255, 0.3), inset 0 0 0 1px rgba(0, 153, 255, 0.2)' : 'none',
          // 편집모드일 때는 width를 자동으로, 일반 모드일 때는 고정 width 적용
          width: isEditMode ? 'auto' : '99px',
          minWidth: isEditMode ? '99px' : 'auto',
          minHeight: '36px'
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
            color: 'var(--color-text-primary)',
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
              color: 'var(--color-warning)',
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
              marginLeft: '4px',
              display: 'flex',
              alignItems: 'center',
              gap: '4px'
            }}
          >
            <span>A:{totalWorkers} P:{productionWorkers}</span>
          </div>
        )}
      </div>

      {/* 미리보기 라벨을 컴포넌트 외부 좌측상단에 표시 */}
      {isPreview && (
        <div
          style={{
            position: 'absolute',
            top: `${position.y - 20}px`,
            left: `${position.x}px`,
            fontSize: '9px',
            color: 'rgba(0, 153, 255, 0.9)',
            backgroundColor: 'rgba(0, 153, 255, 0.1)',
            padding: '2px 6px',
            borderRadius: '3px',
            fontWeight: '600',
            whiteSpace: 'nowrap',
            zIndex: 1002,
            pointerEvents: 'none'
          }}
        >
          미리보기
        </div>
      )}
    </div>
  )
})