/**
 * DraggableWrapper - 오버레이 컴포넌트를 드래그 가능하고 스냅 기능을 제공하는 래퍼 컴포넌트
 * 
 * 프로토타입의 DraggableOverlay를 기반으로 StarcUp.UI 환경에 최적화
 */

import React, { useState, useRef, useEffect, useCallback, ReactNode } from 'react'
import { snapManager, Position, Size, SnapGuide } from '../services/SnapManager'
import './DraggableWrapper.css'

interface DraggableWrapperProps {
  id: string
  children: ReactNode
  position: Position
  onPositionChange: (position: Position) => void
  isEditMode: boolean
  className?: string
  style?: React.CSSProperties
  snapEnabled?: boolean
  showControls?: boolean
}

export function DraggableWrapper({
  id,
  children,
  position,
  onPositionChange,
  isEditMode,
  className = '',
  style = {},
  snapEnabled = true,
  showControls = true
}: DraggableWrapperProps) {
  const [isDragging, setIsDragging] = useState(false)
  const [dragOffset, setDragOffset] = useState({ x: 0, y: 0 })
  const [snapPosition, setSnapPosition] = useState<Position | null>(null)
  const [elementSize, setElementSize] = useState<Size>({ width: 0, height: 0 })
  
  const elementRef = useRef<HTMLDivElement>(null)
  const animationFrameRef = useRef<number>()
  const containerRef = useRef<HTMLElement | null>(null)

  // 요소 크기 업데이트
  useEffect(() => {
    const updateSize = () => {
      if (elementRef.current) {
        const rect = elementRef.current.getBoundingClientRect()
        const newSize = { width: rect.width, height: rect.height }
        setElementSize(newSize)
        
        // SnapManager에 등록/업데이트
        snapManager.updateOverlay(id, {
          id,
          position,
          size: newSize,
          element: elementRef.current
        })
      }
    }

    updateSize()
    
    // ResizeObserver로 크기 변경 감지
    const resizeObserver = new ResizeObserver(updateSize)
    if (elementRef.current) {
      resizeObserver.observe(elementRef.current)
    }

    return () => {
      resizeObserver.disconnect()
    }
  }, [id, position])

  // SnapManager에 컴포넌트 등록
  useEffect(() => {
    if (elementRef.current) {
      snapManager.registerOverlay(id, {
        id,
        position,
        size: elementSize,
        element: elementRef.current
      })
    }

    return () => {
      snapManager.unregisterOverlay(id)
    }
  }, [id])

  // 위치 변경 시 SnapManager 업데이트
  useEffect(() => {
    snapManager.updateOverlay(id, { position })
  }, [id, position])

  // 컨테이너 참조 가져오기
  useEffect(() => {
    containerRef.current = document.querySelector('.overlay-container')
  }, [])

  // 마우스 다운 핸들러
  const handleMouseDown = useCallback((e: React.MouseEvent) => {
    if (!isEditMode) return
    
    // 컨트롤 버튼 클릭 시 드래그 시작하지 않음
    if ((e.target as HTMLElement).closest('.draggable-controls')) {
      return
    }

    e.preventDefault()
    e.stopPropagation()
    
    console.log(`🚀 [DraggableWrapper] ${id} 드래그 시작 - 현재 위치:`, position)
    setIsDragging(true)
    
    const rect = elementRef.current?.getBoundingClientRect()
    const containerRect = containerRef.current?.getBoundingClientRect()
    
    if (rect && containerRect) {
      // 컨테이너 기준 상대 좌표로 변환
      const relativeMouseX = e.clientX - containerRect.left
      const relativeMouseY = e.clientY - containerRect.top
      
      setDragOffset({
        x: relativeMouseX - position.x,
        y: relativeMouseY - position.y
      })
    }
  }, [isEditMode, position])

  // 드래그 위치 업데이트 (requestAnimationFrame 사용)
  const updatePosition = useCallback((clientX: number, clientY: number) => {
    if (!containerRef.current) return

    const containerRect = containerRef.current.getBoundingClientRect()
    
    // 마우스 좌표를 컨테이너 기준 상대 좌표로 변환
    const relativeMouseX = clientX - containerRect.left
    const relativeMouseY = clientY - containerRect.top
    
    // 새 위치 계산 (드래그 오프셋 적용)
    let newX = relativeMouseX - dragOffset.x
    let newY = relativeMouseY - dragOffset.y
    
    // 경계 제한
    const originalX = newX
    const originalY = newY
    newX = Math.max(0, Math.min(containerRect.width - elementSize.width, newX))
    newY = Math.max(0, Math.min(containerRect.height - elementSize.height, newY))
    
    // 경계 제한이 적용되었는지 로그 출력
    if (originalX !== newX || originalY !== newY) {
      console.log(`🚧 [DraggableWrapper] ${id} 경계 제한 적용:`, 
                 `원본 (${originalX.toFixed(1)}, ${originalY.toFixed(1)}) → 제한 (${newX.toFixed(1)}, ${newY.toFixed(1)})`,
                 `컨테이너 크기: ${containerRect.width}x${containerRect.height}, 요소 크기: ${elementSize.width}x${elementSize.height}`)
    }
    
    const newPosition = { x: newX, y: newY }
    
    // 스냅 계산
    if (snapEnabled) {
      const snapResult = snapManager.calculateSnap(
        id,
        newPosition,
        elementSize,
        { width: containerRect.width, height: containerRect.height }
      )
      
      if (snapResult.snapped) {
        console.log(`🎯 [DraggableWrapper] ${id} 스냅 위치:`, snapResult.position)
        setSnapPosition(snapResult.position)
        onPositionChange(snapResult.position)
      } else {
        console.log(`📍 [DraggableWrapper] ${id} 일반 위치:`, newPosition)
        setSnapPosition(null)
        onPositionChange(newPosition)
      }
    } else {
      console.log(`📍 [DraggableWrapper] ${id} 스냅 비활성 위치:`, newPosition)
      onPositionChange(newPosition)
    }
  }, [id, dragOffset, elementSize, snapEnabled, onPositionChange])

  // 마우스 이동 핸들러
  const handleMouseMove = useCallback((e: MouseEvent) => {
    if (!isDragging || !isEditMode) return
    
    // 이전 애니메이션 프레임 취소
    if (animationFrameRef.current) {
      cancelAnimationFrame(animationFrameRef.current)
    }
    
    // requestAnimationFrame으로 부드러운 업데이트
    animationFrameRef.current = requestAnimationFrame(() => {
      updatePosition(e.clientX, e.clientY)
    })
  }, [isDragging, isEditMode, updatePosition])

  // 마우스 업 핸들러
  const handleMouseUp = useCallback(() => {
    if (!isDragging) return
    
    console.log(`🛑 [DraggableWrapper] ${id} 드래그 종료 - 최종 위치:`, position)
    setIsDragging(false)
    setSnapPosition(null)
    
    // 스냅 가이드 제거
    snapManager.clearGuides()
    
    // 애니메이션 프레임 정리
    if (animationFrameRef.current) {
      cancelAnimationFrame(animationFrameRef.current)
      animationFrameRef.current = undefined
    }
  }, [isDragging])

  // 마우스 이벤트 리스너 등록
  useEffect(() => {
    if (isDragging) {
      document.addEventListener('mousemove', handleMouseMove)
      document.addEventListener('mouseup', handleMouseUp)
      
      return () => {
        document.removeEventListener('mousemove', handleMouseMove)
        document.removeEventListener('mouseup', handleMouseUp)
        
        if (animationFrameRef.current) {
          cancelAnimationFrame(animationFrameRef.current)
        }
      }
    }
  }, [isDragging, handleMouseMove, handleMouseUp])

  // 컴포넌트 언마운트 시 정리
  useEffect(() => {
    return () => {
      if (animationFrameRef.current) {
        cancelAnimationFrame(animationFrameRef.current)
      }
    }
  }, [])

  // 실제 표시 위치 (스냅 중일 때는 스냅 위치 사용)
  const displayPosition = isDragging && snapPosition ? snapPosition : position

  return (
    <div
      ref={elementRef}
      className={`draggable-wrapper ${className} ${isDragging ? 'dragging' : ''} ${isEditMode ? 'edit-mode' : ''}`}
      style={{
        ...style,
        position: 'absolute',
        left: `${displayPosition.x}px`,
        top: `${displayPosition.y}px`,
        cursor: isEditMode ? (isDragging ? 'grabbing' : 'grab') : 'default',
        userSelect: 'none',
        zIndex: isDragging ? 15000 : isEditMode ? 1000 : 'auto',
        transition: isDragging ? 'none' : 'all 0.2s ease-out',
        transform: isDragging ? 'scale(1.02)' : 'scale(1)',
        pointerEvents: 'auto'
      }}
      onMouseDown={handleMouseDown}
    >
      {/* 편집 모드 컨트롤 버튼 */}
      {isEditMode && showControls && (
        <div className="draggable-controls">
          <button
            className="control-btn move-btn"
            title="드래그하여 이동"
            onMouseDown={(e) => e.stopPropagation()}
          >
            <svg width="12" height="12" viewBox="0 0 24 24" fill="currentColor">
              <path d="M10 9h4V6h3l-5-5-5 5h3v3zm-1 1H6V7l-5 5 5 5v-3h3v-4zm14 2l-5-5v3h-3v4h3v3l5-5zm-9 3h-4v3H7l5 5 5-5h-3v-3z"/>
            </svg>
          </button>
          
          <button
            className="control-btn reset-btn"
            title="기본 위치로 리셋"
            onClick={(e) => {
              e.stopPropagation()
              const defaultPos = snapManager.resetToDefaultPosition(id)
              onPositionChange(defaultPos)
            }}
            onMouseDown={(e) => e.stopPropagation()}
          >
            <svg width="12" height="12" viewBox="0 0 24 24" fill="currentColor">
              <path d="M12 5V1L7 6l5 5V7c3.31 0 6 2.69 6 6s-2.69 6-6 6-6-2.69-6-6H4c0 4.42 3.58 8 8 8s8-3.58 8-8-3.58-8-8-8z"/>
            </svg>
          </button>
        </div>
      )}

      {/* 편집 모드 테두리 */}
      {isEditMode && (
        <div 
          className={`edit-mode-border ${isDragging && snapPosition ? 'snapping' : ''}`}
          style={{
            position: 'absolute',
            inset: 0,
            border: '2px dashed',
            borderColor: isDragging && snapPosition ? 'var(--color-success, #00d084)' : 'var(--color-primary, #0099ff)',
            backgroundColor: isDragging && snapPosition ? 'rgba(0, 208, 132, 0.1)' : 'rgba(0, 153, 255, 0.1)',
            pointerEvents: 'none',
            borderRadius: '4px',
            transition: 'all 0.2s ease-out'
          }}
        />
      )}

      {/* 자식 컴포넌트 */}
      <div className="draggable-content">
        {children}
      </div>
    </div>
  )
}