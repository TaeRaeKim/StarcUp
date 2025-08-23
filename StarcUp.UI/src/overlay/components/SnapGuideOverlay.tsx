/**
 * SnapGuideOverlay - 스냅 가이드라인을 시각적으로 표시하는 오버레이 컴포넌트
 */

import React, { useEffect, useState } from 'react'
import { snapManager, SnapGuide } from '../services/SnapManager'
import './SnapGuideOverlay.css'

interface SnapGuideOverlayProps {
  isEditMode: boolean
  isDraggingAny: boolean
}

export function SnapGuideOverlay({ isEditMode, isDraggingAny }: SnapGuideOverlayProps) {
  const [guides, setGuides] = useState<SnapGuide[]>([])
  const [containerSize, setContainerSize] = useState({ width: 0, height: 0 })

  // 컨테이너 크기 업데이트
  useEffect(() => {
    const updateContainerSize = () => {
      const container = document.querySelector('.overlay-container')
      if (container) {
        const rect = container.getBoundingClientRect()
        setContainerSize({ width: rect.width, height: rect.height })
      }
    }

    updateContainerSize()
    window.addEventListener('resize', updateContainerSize)
    
    return () => {
      window.removeEventListener('resize', updateContainerSize)
    }
  }, [])

  // SnapManager의 가이드 업데이트 콜백 등록
  useEffect(() => {
    if (isEditMode && isDraggingAny) {
      // 편집 모드이고 드래그 중일 때만 가이드 표시
      snapManager.setGuideUpdateCallback((newGuides) => {
        setGuides(newGuides)
      })
    } else {
      // 편집 모드가 아니거나 드래그 중이 아닐 때는 가이드 제거
      setGuides([])
      snapManager.clearGuides()
    }

    return () => {
      snapManager.clearGuides()
    }
  }, [isEditMode, isDraggingAny])

  // 편집 모드가 아니거나 드래그 중이 아니거나 가이드가 없으면 렌더링하지 않음
  if (!isEditMode || !isDraggingAny || guides.length === 0) {
    return null
  }

  return (
    <div className="snap-guide-overlay" style={{ width: containerSize.width, height: containerSize.height }}>
      {guides.map((guide, index) => (
        <div key={index} className="snap-guide-container">
          {/* 가이드라인 */}
          <div
            className={`snap-guide-line ${guide.type} ${guide.source || ''}`}
            style={
              guide.type === 'vertical'
                ? {
                    left: `${guide.position}px`,
                    top: 0,
                    width: '2px',
                    height: '100%'
                  }
                : {
                    left: 0,
                    top: `${guide.position}px`,
                    width: '100%',
                    height: '2px'
                  }
            }
          />
          
          {/* 가이드 라벨 */}
          {guide.label && (
            <div
              className="snap-guide-label"
              style={
                guide.type === 'vertical'
                  ? {
                      left: `${guide.position + 8}px`,
                      top: '20px'
                    }
                  : {
                      left: '20px',
                      top: `${guide.position + 8}px`
                    }
              }
            >
              {guide.label}
            </div>
          )}
        </div>
      ))}
    </div>
  )
}