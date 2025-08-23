import React, { useEffect, useRef, useImperativeHandle, forwardRef } from 'react'
import { useEffectSystem, type EffectType } from '../hooks/useEffectSystem'
import { getIconStyle } from '../../utils/iconUtils'
import { HDIcon } from './HDIcon'
import { getWorkerImagesByRace, getWorkerNameByRace } from '../../utils/workerImageUtils'
import { RaceType } from '../../types/game'

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
  selectedRace?: RaceType // 선택된 종족
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
  isPreview = false,
  selectedRace = RaceType.Protoss
}, ref) => {
  const workerStatusRef = useRef<HTMLDivElement>(null)
  const { triggerEffect } = useEffectSystem()
  
  // 종족에 따른 일꾼 이미지 가져오기
  const workerImages = getWorkerImagesByRace(selectedRace)
  const workerName = getWorkerNameByRace(selectedRace)
  
  // 디버깅용 로그
  useEffect(() => {
    console.log('🎮 [WorkerStatus] 종족 정보:', {
      selectedRace,
      raceName: selectedRace === 0 ? 'Zerg' : selectedRace === 1 ? 'Terran' : 'Protoss',
      workerName,
      iconUrl: workerImages.iconUrl
    })
  }, [selectedRace, workerName, workerImages.iconUrl])

  // ref를 통해 외부에서 효과를 트리거할 수 있도록 노출
  useImperativeHandle(ref, () => ({
    triggerEffect: (effectType: EffectType) => {
      triggerEffect(workerStatusRef.current, effectType)
    }
  }), [triggerEffect])


  return (
    <div style={{ position: 'relative' }}>
      <div
        ref={workerStatusRef}
        className={`worker-status ${isEditMode ? 'edit-mode' : ''} ${isPreview ? 'preview-mode' : ''}`}
        style={{
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
      <div style={{
        ...getIconStyle(unitIconStyle, teamColor),
        // HD 모드일 때 스케일 조정
        ...(unitIconStyle === 'hd' ? {
          transform: 'scale(0.8)',
          transformOrigin: 'center'
        } : {})
      }}>
        {unitIconStyle === 'hd' && workerImages.diffuseUrl && workerImages.teamColorUrl ? (
          <HDIcon
            diffuseSrc={workerImages.diffuseUrl}
            teamColorSrc={workerImages.teamColorUrl}
            teamColor={teamColor}
            width={27}
            height={27}
            alt={`${workerName} HD`}
          />
        ) : (
          <img 
            src={workerImages.iconUrl}
            alt={workerName} 
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