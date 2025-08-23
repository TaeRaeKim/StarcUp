import React, { useState, useEffect, useCallback } from 'react'
import { AlertTriangle } from 'lucide-react'

interface PopulationWarningProps {
  isVisible: boolean
  message?: string
  opacity?: number
  position?: { x: number; y: number }
  isEditMode?: boolean
  onPositionChange?: (position: { x: number; y: number }) => void
  isPreview?: boolean
  onHide?: () => void
  onRenderingChange?: (isRendering: boolean) => void
}

export function PopulationWarning({
  isVisible,
  message = "Supply Alert!",
  opacity = 90,
  position = { x: 100, y: 60 },
  isEditMode = false,
  onPositionChange,
  isPreview = false,
  onHide,
  onRenderingChange
}: PopulationWarningProps) {
  const [isAnimating, setIsAnimating] = useState(false)
  const [shouldRender, setShouldRender] = useState(false)
  const [isDragging, setIsDragging] = useState(false)
  const [dragOffset, setDragOffset] = useState({ x: 0, y: 0 })

  // isVisible 변경 감지 및 애니메이션 관리
  useEffect(() => {
    if (isVisible) {
      // 나타날 때: 즉시 렌더링하고 페이드인
      setShouldRender(true)
      onRenderingChange?.(true)
      // 다음 프레임에서 애니메이션 시작 (DOM 업데이트 후)
      requestAnimationFrame(() => {
        setIsAnimating(true)
      })
    } else if (!isPreview) {
      // 미리보기 모드가 아닐 때만 사라짐 애니메이션 실행
      // 사라질 때: 페이드아웃 시작
      setIsAnimating(false)
    }
    
    // 미리보기 모드일 때는 항상 렌더링 상태 유지
    if (isPreview && !shouldRender) {
      setShouldRender(true)
      setIsAnimating(true)
    }
  }, [isVisible, isPreview, shouldRender, onRenderingChange])

  // 페이드아웃 애니메이션 완료 처리
  useEffect(() => {
    if (!isVisible && !isAnimating && shouldRender) {
      // 페이드아웃 완료 후 일정 시간 대기 후 렌더링 중단
      const hideTimer = setTimeout(() => {
        setShouldRender(false)
        onRenderingChange?.(false)
        onHide?.()
      }, 600) // 0.5초 애니메이션 + 0.1초 여유시간

      return () => clearTimeout(hideTimer)
    }
  }, [isVisible, isAnimating, shouldRender, onRenderingChange, onHide])

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
    const populationWarningElement = document.querySelector('.population-warning') as HTMLElement
    const componentWidth = populationWarningElement ? populationWarningElement.offsetWidth : 180
    const componentHeight = populationWarningElement ? populationWarningElement.offsetHeight : 60
    
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

  // 미리보기 모드가 아니고 렌더링하지 않을 때는 null 반환
  if (!shouldRender && !isPreview) {
    return null
  }

  return (
    <div style={{ position: 'relative' }}>
      <div
        className={`population-warning ${isEditMode ? 'edit-mode' : ''} ${isDragging ? 'dragging' : ''} ${isPreview ? 'preview-mode' : ''}`}
        onMouseDown={handleMouseDown}
        style={{
          backgroundColor: 'var(--color-error)',
          color: 'var(--color-text-primary)',
          padding: '12px 16px',
          borderRadius: '8px',
          display: 'flex',
          alignItems: 'center',
          gap: '8px',
          fontSize: '14px',
          fontWeight: '600',
          boxShadow: isPreview 
            ? '0 4px 20px rgba(255, 59, 48, 0.4), 0 0 0 2px rgba(0, 153, 255, 0.3), inset 0 0 0 1px rgba(0, 153, 255, 0.2)'
            : '0 4px 20px rgba(255, 59, 48, 0.4)',
          zIndex: 1000,
          minWidth: '140px',
          fontFamily: 'Arial, sans-serif',
          userSelect: 'none',
          // Prototype의 정확한 opacity 계산 방식 적용
          opacity: (isAnimating || isPreview) ? (opacity / 100) : 0,
          // 부드러운 전환 애니메이션
          transition: isDragging ? 'none' : 'opacity 0.5s ease-out, transform 0.5s ease-out',
          // 나타날 때는 아래에서 위로, 사라질 때는 위로 살짝 이동
          transform: (isAnimating || isPreview) ? 'translateY(0)' : 'translateY(10px)',
          // 포인터 이벤트는 완전히 보일 때만 활성화 (편집 모드 제외)
          pointerEvents: (isAnimating || isEditMode) ? 'auto' : 'none',
          cursor: isEditMode ? 'move' : 'default',
          // 미리보기 모드일 때 추가 시각적 효과
          filter: isPreview ? 'brightness(0.9)' : 'none'
        }}
      >
      <AlertTriangle 
        style={{ 
          width: '20px',
          height: '20px',
          color: 'white',
          // 아이콘도 함께 애니메이션
          transition: 'transform 0.5s ease-out',
          transform: isAnimating ? 'scale(1)' : 'scale(0.9)'
        }}
      />
        <span 
          style={{ 
            // 텍스트도 부드럽게 애니메이션
            transition: 'opacity 0.5s ease-out',
            opacity: (isAnimating || isPreview) ? 1 : 0.8
          }}
        >
          {message}
        </span>
      </div>

      {/* 미리보기 라벨을 컴포넌트 외부 좌측상단에 표시 */}
      {isPreview && (
        <div
          style={{
            position: 'absolute',
            top: `${position.y - 20}px`,
            left: `${position.x}px`,
            fontSize: '10px',
            color: 'rgba(0, 153, 255, 1)',
            backgroundColor: 'rgba(0, 153, 255, 0.15)',
            padding: '2px 6px',
            borderRadius: '3px',
            fontWeight: '600',
            whiteSpace: 'nowrap',
            zIndex: 1001,
            pointerEvents: 'none'
          }}
        >
          미리보기
        </div>
      )}
    </div>
  )
}