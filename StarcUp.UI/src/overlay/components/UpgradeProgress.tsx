import React, { useState, useEffect, useCallback, forwardRef, useImperativeHandle, useRef } from 'react';
import { useEffectSystem, type EffectType } from '../hooks/useEffectSystem';
import { getUpgradeIconPath, getTechIconPath, getUpgradeName, getTechName } from '../../utils/upgradeImageUtils';
import { UpgradeType, TechType, getUpgradeFrameInfo, getUpgradeFramesForLevel, getTechFrameInfo } from '../../types/game';
import { UpgradeItemType, UpgradeSettings } from '../../types/preset/PresetSettings';
import { CheckCircle } from 'lucide-react';
import { 
  UpgradeItemData, 
  UpgradeCategory, 
  UpgradeDisplayStatus,
  framesToTimeString,
  calculateProgress 
} from '../types/upgrade';
import { getIconFilterWithStatus, type DisplayStatusType } from '../../utils/iconUtils';

interface UpgradeProgressProps {
  categories: UpgradeCategory[];
  position: { x: number; y: number };
  isEditMode: boolean;
  onPositionChange?: (position: { x: number; y: number }) => void;
  unitIconStyle?: 'default' | 'white' | 'yellow';
  opacity?: number;
  isPreview?: boolean;
  isInGame?: boolean; // 인게임 상태 여부
  presetUpgradeSettings?: UpgradeSettings; // 프리셋 업그레이드 설정
}

export interface UpgradeProgressRef {
  triggerEffect: (effectType: EffectType) => void;
}

// 레벨별 색상 시스템
const LEVEL_COLORS = {
  1: {
    background: '#FFB800', // Warning 색상 (노란색)
    text: '#1A1A1A'        // 대비를 위한 어두운 텍스트
  },
  2: {
    background: '#FF6B35', // Brand Secondary 색상 (오렌지)
    text: '#FFFFFF'        // 흰색 텍스트
  },
  3: {
    background: '#00D084', // Success 색상 (초록)
    text: '#FFFFFF'        // 흰색 텍스트
  },
  check: {
    background: '#00D084', // Success 색상 (초록) - 완료용
    text: '#FFFFFF'        // 흰색 텍스트
  }
};

// 업그레이드 상태 계산 함수
const getUpgradeDisplayState = (item: UpgradeItemData, maxLevel: number) => {
  // 진행 중인 경우
  if (item.remainingFrames > 0 && item.currentUpgradeLevel > 0) {
    return {
      displayStatus: 'progress' as const,
      showTag: false,
      isActive: true
    };
  }
  
  // 완료된 경우 (현재 레벨이 최대 레벨과 같음)
  if (item.level > 0 && item.level >= maxLevel) {
    return {
      displayStatus: 'completed' as const,
      showTag: true,
      tagType: 'check' as const,
      tagValue: null,
      tagColor: LEVEL_COLORS.check,
      isActive: true
    };
  }
  
  // 부분 완료된 경우 (현재 레벨 > 0이지만 최대 레벨보다 작음)
  if (item.level > 0 && item.level < maxLevel) {
    const currentLevel = item.level;
    const currentLevelColor = currentLevel <= 3 ? LEVEL_COLORS[currentLevel as 1|2|3] : LEVEL_COLORS.check;
    return {
      displayStatus: 'partial' as const,
      showTag: true,
      tagType: 'level' as const,
      tagValue: currentLevel,
      tagColor: currentLevelColor,
      isActive: true
    };
  }
  
  // 비활성 상태 (level = 0)
  return {
    displayStatus: 'inactive' as const,
    showTag: false,
    isActive: false
  };
};


// 상태별 스타일 정의
const getStatusStyles = (displayStatus: string) => {
  switch (displayStatus) {
    case 'progress':
      return {
        containerOpacity: 1,
        nameColor: 'var(--color-text-primary)',
        timeColor: 'var(--color-text-secondary)',
        progressColor: 'var(--color-brand-primary)',
        statusIndicator: 'var(--color-brand-primary)'
      };
    case 'completed':
      return {
        containerOpacity: 0.9,
        nameColor: 'var(--color-success)',
        timeColor: 'var(--color-success)',
        progressColor: 'var(--color-success)',
        statusIndicator: 'var(--color-success)'
      };
    case 'partial':
      return {
        containerOpacity: 0.8,
        nameColor: 'var(--color-text-primary)',
        timeColor: 'var(--color-text-secondary)',
        progressColor: 'var(--color-warning)',
        statusIndicator: 'var(--color-warning)'
      };
    case 'inactive':
    default:
      return {
        containerOpacity: 0.6,
        nameColor: 'var(--color-gray-400)',
        timeColor: 'var(--color-gray-500)',
        progressColor: 'var(--color-gray-400)',
        statusIndicator: 'var(--color-gray-500)'
      };
  }
};

// 업그레이드 정보 가져오기
const getUpgradeInfo = (type: UpgradeItemType, value: UpgradeType | TechType, currentLevel: number = 1) => {
  if (type === UpgradeItemType.Upgrade) {
    const frameInfo = getUpgradeFrameInfo(value as UpgradeType);
    const totalFrames = getUpgradeFramesForLevel(value as UpgradeType, currentLevel);
    return {
      name: getUpgradeName(value as UpgradeType),
      iconPath: getUpgradeIconPath(value as UpgradeType),
      maxLevel: frameInfo.maxLevel,
      totalFrames: totalFrames
    };
  } else {
    const frameInfo = getTechFrameInfo(value as TechType);
    return {
      name: getTechName(value as TechType),
      iconPath: getTechIconPath(value as TechType),
      maxLevel: frameInfo.maxLevel,
      totalFrames: frameInfo.frames
    };
  }
};

// 개별 업그레이드 아이콘 컴포넌트
function UpgradeIcon({ 
  iconPath, 
  name,
  iconStyle = 'default',
  displayStatus = 'inactive',
  size = 24
}: { 
  iconPath: string;
  name: string;
  iconStyle?: 'default' | 'white' | 'yellow';
  displayStatus?: string;
  size?: number;
}) {
  return (
    <div 
      className="flex items-center justify-center"
      style={{
        width: `${size}px`,
        height: `${size}px`,
        position: 'relative',
        flexShrink: 0
      }}
    >
      <img
        src={iconPath}
        alt={`${name} icon`}
        style={{
          width: `${size}px`,
          height: `${size}px`,
          objectFit: 'contain',
          filter: getIconFilterWithStatus(iconStyle, displayStatus as DisplayStatusType),
          transition: 'filter 0.2s ease-out',
          imageRendering: 'pixelated',
          display: 'block',
          backgroundColor: 'black'          
        }}
      />
    </div>
  );
}

// 진행 중인 업그레이드 행 컴포넌트 (세로 레이아웃)
function ActiveUpgradeRow({ 
  item,
  upgradeInfo,
  iconStyle,
  isCompleting = false,
  showRemainingTime = true,
  showProgressPercentage = true,
  showProgressBar = true
}: { 
  item: UpgradeItemData;
  upgradeInfo: any;
  iconStyle?: 'default' | 'white' | 'yellow';
  isCompleting?: boolean;
  showRemainingTime?: boolean;
  showProgressPercentage?: boolean;
  showProgressBar?: boolean;
}) {
  const displayState = getUpgradeDisplayState(item, upgradeInfo.maxLevel);
  const styles = getStatusStyles(displayState.displayStatus);
  
  // 진행 중인 업그레이드의 목표 레벨은 currentUpgradeLevel
  const targetLevel = item.currentUpgradeLevel;
  const isTech = upgradeInfo.maxLevel === 1 && targetLevel === 1;
  const targetLevelColor = isTech ? { text: '#fff', background: '#4A90E2' } : (targetLevel <= 3 ? LEVEL_COLORS[targetLevel as 1|2|3] : LEVEL_COLORS.check);
  
  // 진행률과 시간 계산
  const progress = calculateProgress(item.remainingFrames, upgradeInfo.totalFrames);
  const timeRemaining = framesToTimeString(item.remainingFrames);
  
  return (
    <div 
      className={`flex items-center mb-2 last:mb-0 ${isCompleting ? 'upgrade-completing' : ''}`}
      style={{ 
        position: 'relative',
        opacity: isCompleting ? 1 : styles.containerOpacity,
        transition: isCompleting ? 'none' : 'opacity 0.2s ease-out',
        background: isCompleting ? 'rgba(0, 153, 255, 0.05)' : 'transparent',
        borderRadius: '4px',
        overflow: 'hidden'
      }}
    >
      {/* 좌측: 아이콘 */}
      <UpgradeIcon 
        iconPath={upgradeInfo.iconPath}
        name={upgradeInfo.name}
        iconStyle={iconStyle}
        displayStatus={displayState.displayStatus}
        size={32}
      />
      
      {/* 우측: 업그레이드 정보 (두 라인) */}
      <div 
        className="flex flex-col flex-1"
        style={{
          marginLeft: '6px',
          minWidth: 0
        }}
      >
        {/* 첫 번째 라인: 업그레이드 이름 + (+레벨 태그) */}
        <div 
          className="flex items-center justify-between"
          style={{ width: '100%', marginBottom: '2px' }}
        >
          <span 
            style={{ 
              fontSize: '12px',
              fontWeight: '600',
              color: styles.nameColor,
              whiteSpace: 'nowrap',
              overflow: 'hidden',
              textOverflow: 'ellipsis',
              transition: 'color 0.2s ease-out',
              flex: 1
            }}
          >
            {upgradeInfo.name}
          </span>
          
          {/* +레벨 태그 또는 Tech 태그 */}
          {item.remainingFrames > 0 && item.currentUpgradeLevel > 0 && (
            <span 
              style={{
                fontSize: '8px',
                fontWeight: '600',    
                color: targetLevelColor.text,
                backgroundColor: targetLevelColor.background,
                opacity: 0.75,
                padding: '1px 3px',  
                borderRadius: '6px',  
                minWidth: '16px',     
                textAlign: 'center',
                transition: 'all 0.2s ease-out',
                boxShadow: `0 0 3px ${targetLevelColor.background}30`,
                flexShrink: 0,
                marginLeft: '8px'
              }}
            >
              {isTech ? 'T' : `+${targetLevel}`}
            </span>
          )}
        </div>
        
        {/* 두 번째 라인: (잔여시간) + 프로그레스바 + (진행률) */}
        {item.remainingFrames > 0 && (showRemainingTime || showProgressBar || showProgressPercentage) && (
          <div 
            className="flex items-center"
            style={{ width: '100%', gap: '4px'}}
          >
            {/* 잔여시간 */}
            {showRemainingTime && (
              <span 
              className="flex justify-center"
                style={{ 
                  fontSize: '11px',
                  fontWeight: '600',
                  color: styles.timeColor,
                  minWidth: '32px'
                }}
              >
                {timeRemaining}
              </span>
            )}
            
            {/* 프로그레스바 */}
            {showProgressBar && (
              <div 
                style={{ 
                  flex: 1,
                  height: '3px',
                  backgroundColor: 'rgba(255, 255, 255, 0.3)',
                  borderRadius: '2px',
                  overflow: 'hidden',
                  minWidth: '40px'
                }}
              >
                <div 
                  style={{ 
                    width: `${progress}%`,
                    height: '100%',
                    backgroundColor: styles.progressColor,
                    borderRadius: '2px',
                    transition: 'width 0.3s ease-out, background-color 0.3s ease-out'
                  }}
                />
              </div>
            )}
            
            {/* 진행률 */}
            {showProgressPercentage && (
              <span 
              className="flex justify-center"
                style={{ 
                  fontSize: '10px',
                  fontWeight: '400',
                  color: styles.timeColor,
                  minWidth: '28px'
                }}
              >
                {progress}%
              </span>
            )}
          </div>
        )}
      </div>
    </div>
  );
}

// 비활성 업그레이드 아이콘 컴포넌트 (가로 레이아웃용)
function InactiveUpgradeIcon({ 
  item,
  upgradeInfo,
  iconStyle 
}: { 
  item: UpgradeItemData;
  upgradeInfo: any;
  iconStyle?: 'default' | 'white' | 'yellow';
}) {
  const displayState = getUpgradeDisplayState(item, upgradeInfo.maxLevel);
  
  return (
    <div 
      className="flex items-center justify-center"
      style={{
        marginRight: '8px',
        position: 'relative'
      }}
      title={`${upgradeInfo.name} (Lv.${item.level}/${upgradeInfo.maxLevel})`}
    >
      <UpgradeIcon 
        iconPath={upgradeInfo.iconPath}
        name={upgradeInfo.name}
        iconStyle={iconStyle}
        displayStatus={displayState.displayStatus}
        size={32}
      />

      {/* 상태별 태그 표시 */}
      {displayState.showTag && displayState.tagColor && (
        <div
          className="absolute -bottom-1 -right-1"
          style={{
            minWidth: '12px',
            height: '12px',
            borderRadius: '50%',
            backgroundColor: displayState.tagColor.background,
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
            fontSize: '8px',
            fontWeight: '700',
            color: displayState.tagColor.text,
            border: '1px solid var(--color-overlay-bg)',
            boxShadow: `0 0 4px ${displayState.tagColor.background}40`
          }}
        >
          {/* 완료된 경우 체크 아이콘, 부분 완료의 경우 다음 레벨 숫자 */}
          {displayState.tagType === 'check' ? (
            <CheckCircle
              style={{
                width: '8px',
                height: '8px',
                color: displayState.tagColor.text
              }}
            />
          ) : displayState.tagType === 'level' && displayState.tagValue ? (
            <span style={{ fontSize: '8px', fontWeight: '700' }}>
              +{displayState.tagValue}
            </span>
          ) : null}
        </div>
      )}
    </div>
  );
}

const UpgradeProgress = forwardRef<UpgradeProgressRef, UpgradeProgressProps>(
  ({ categories, position, isEditMode, onPositionChange, unitIconStyle = 'default', opacity = 1, isPreview = false, isInGame = false, presetUpgradeSettings }, ref) => {
    
  const [isDragging, setIsDragging] = useState(false);
  const [dragOffset, setDragOffset] = useState({ x: 0, y: 0 });
  const upgradeProgressRef = useRef<HTMLDivElement>(null);
  
  // 완료 애니메이션을 추적하기 위한 상태
  const [completingUpgrades, setCompletingUpgrades] = useState<Set<string>>(new Set());
  
  // 이전 상태를 추적하여 완료 감지 - useRef로 변경
  const previousItemsRef = useRef<Map<string, UpgradeItemData>>(new Map());

  // 애니메이션 트리거 함수
  const triggerUpgradeAnimation = useCallback((key: string) => {
    setCompletingUpgrades(prev => {
      const newSet = new Set(prev);
      newSet.add(key);
      return newSet;
    });
    
    setTimeout(() => {
      setCompletingUpgrades(prev => {
        const newSet = new Set(prev);
        newSet.delete(key);
        return newSet;
      });
    }, 1200);
  }, []);

  // 효과 시스템 연결
  const { triggerEffect } = useEffectSystem();
  
  // ref를 통해 외부에서 효과 트리거 가능하도록 설정
  useImperativeHandle(ref, () => ({
    triggerEffect: (effectType: EffectType) => {
      triggerEffect(upgradeProgressRef.current, effectType);
    }
  }), [triggerEffect]);

  // displayCategories를 useMemo로 최적화
  const displayCategories = React.useMemo(() => {
    // 실제 업그레이드 데이터가 있으면 그대로 사용
    if (categories.length > 0) {
      return categories;
    }

    // 편집 모드에서 실제 업그레이드가 없을 때 프리셋 데이터 사용
    if (presetUpgradeSettings?.categories) {
      // 프리셋 데이터를 UpgradeItemData 형태로 변환
      return presetUpgradeSettings.categories.map((category: any) => ({
        id: category.id,
        name: category.name,
        items: category.items.map((item: any) => ({
          item: {
            type: item.type, // UpgradeItemType (0: Upgrade, 1: Tech)
            value: item.value // UpgradeType 또는 TechType 값
          },
          level: 0, // 편집 모드/대기 상태에서는 레벨 0
          remainingFrames: 0, // 진행 중이 아님
          currentUpgradeLevel: 0 // 현재 업그레이드 레벨 0
        }))
      }));
    }

    // 프리셋 데이터도 없으면 빈 배열 반환
    return [];
  }, [categories, presetUpgradeSettings]);

  // 드래그 시작
  const handleMouseDown = (e: React.MouseEvent) => {
    if (!isEditMode) return;
    
    e.preventDefault();
    setIsDragging(true);
    
    const overlayContainer = document.querySelector('.overlay-container') as HTMLElement;
    if (overlayContainer) {
      const containerRect = overlayContainer.getBoundingClientRect();
      setDragOffset({
        x: e.clientX - containerRect.left - position.x,
        y: e.clientY - containerRect.top - position.y
      });
    }
  };

  // 드래그 중
  const handleMouseMove = useCallback((e: MouseEvent) => {
    if (!isDragging || !isEditMode) return;
    
    const overlayContainer = document.querySelector('.overlay-container') as HTMLElement;
    if (!overlayContainer || !onPositionChange) return;
    
    const containerRect = overlayContainer.getBoundingClientRect();
    const newPosition = {
      x: e.clientX - containerRect.left - dragOffset.x,
      y: e.clientY - containerRect.top - dragOffset.y
    };
    
    // 경계 제한
    const upgradeElement = upgradeProgressRef.current;
    const componentWidth = upgradeElement ? upgradeElement.offsetWidth : 200;
    const componentHeight = upgradeElement ? upgradeElement.offsetHeight : 100;
    
    const clampedX = Math.max(0, Math.min(containerRect.width - componentWidth, newPosition.x));
    const clampedY = Math.max(0, Math.min(containerRect.height - componentHeight, newPosition.y));
    
    onPositionChange({ x: clampedX, y: clampedY });
  }, [isDragging, isEditMode, dragOffset, onPositionChange]);

  // 드래그 종료
  const handleMouseUp = useCallback(() => {
    if (!isDragging) return;
    setIsDragging(false);
  }, [isDragging]);

  useEffect(() => {
    if (isDragging) {
      document.addEventListener('mousemove', handleMouseMove);
      document.addEventListener('mouseup', handleMouseUp);
      
      return () => {
        document.removeEventListener('mousemove', handleMouseMove);
        document.removeEventListener('mouseup', handleMouseUp);
      };
    }
  }, [isDragging, handleMouseMove, handleMouseUp]);

  // 업그레이드 완료 감지 및 애니메이션 트리거
  useEffect(() => {
    if (!isInGame) return; // 인게임 상태일 때만 동작

    const currentItems = new Map<string, UpgradeItemData>();
    
    // 현재 상태 수집
    displayCategories.forEach((category: any) => {
      category.items.forEach((item: any) => {
        const key = `${item.item.type}_${item.item.value}`;
        currentItems.set(key, item);
      });
    });
    
    // 업그레이드 완료 감지
    const previousItems = previousItemsRef.current;
    if (previousItems.size > 0) {
      currentItems.forEach((currentItem: any, key: string) => {
        const previousItem = previousItems.get(key);
        
        // 진행 중이던 업그레이드가 완료된 순간
        if (previousItem && 
            previousItem.remainingFrames > 0 && 
            currentItem.remainingFrames === 0 && 
            currentItem.currentUpgradeLevel > 0) {
          
          // upgradeCompletionAlert가 true일 때만 완료 애니메이션 실행
          if (presetUpgradeSettings?.upgradeCompletionAlert) {
            triggerUpgradeAnimation(key);
          }
        }
        
        // 진행 중이던 업그레이드가 취소된 순간
        else if (previousItem && 
                 previousItem.remainingFrames > 0 && 
                 previousItem.currentUpgradeLevel > 0 &&
                 currentItem.remainingFrames === 0 && 
                 currentItem.currentUpgradeLevel === 0) {
          
          // upgradeCompletionAlert가 true일 때만 취소 애니메이션 실행
          if (presetUpgradeSettings?.upgradeCompletionAlert) {
            triggerUpgradeAnimation(key + '_cancelled');
          }
        }
      });
    }
    
    // 이전 상태 업데이트 - useRef 사용
    previousItemsRef.current = currentItems;
  }, [displayCategories, isInGame, presetUpgradeSettings?.upgradeCompletionAlert, triggerUpgradeAnimation]);

  // displayCategories가 없으면 렌더링하지 않음
  if (!displayCategories || displayCategories.length === 0) {
    return null;
  }

  return (
    <div 
      ref={upgradeProgressRef}
      className="upgrade-progress-container"
      onMouseDown={handleMouseDown}
      style={{
        position: 'absolute',
        left: `${position.x}px`,
        top: `${position.y}px`,
        opacity: opacity,
        backgroundColor: 'transparent',
        borderRadius: '8px',
        padding: '4px',
        minWidth: '100px',
        maxWidth: '300px',
        cursor: isEditMode ? 'move' : 'default',
        userSelect: 'none',
        zIndex: isEditMode ? 1002 : 1000,
        transition: isDragging ? 'none' : 'all 0.2s ease',
        pointerEvents: 'auto'
      }}
    >
      <div className="space-y-1">
        {displayCategories.map((category: any) => {
          // 활성 업그레이드와 비활성 업그레이드 분리
          const activeItems = category.items.filter((item: any) => {
            const key = `${item.item.type}_${item.item.value}`;
            const cancelledKey = key + '_cancelled';
            return (item.remainingFrames > 0 && item.currentUpgradeLevel > 0) || 
                   completingUpgrades.has(key) || 
                   completingUpgrades.has(cancelledKey);
          });
          
          const inactiveItems = category.items.filter((item: any) => 
            !(item.remainingFrames > 0 && item.currentUpgradeLevel > 0)
          );

          // 비활성 업그레이드 정렬 (완료 > 비활성)
          const sortedInactiveItems = [...inactiveItems].sort((a: any, b: any) => {
            if (a.level > 0 && b.level === 0) return -1;
            if (a.level === 0 && b.level > 0) return 1;
            return b.level - a.level;
          });

          // upgradeStateTracking이 false이고 진행 중인 업그레이드가 없으면 카테고리 숨김
          const shouldHideCategory = (presetUpgradeSettings?.upgradeStateTracking === false) && activeItems.length === 0;
          if (shouldHideCategory) {
            return null;
          }

          return (
            <div key={category.id} className="mb-1">
              {/* 카테고리 제목 */}
              <div 
                className="text-xs mb-1"
                style={{ 
                  fontSize: '12px',
                  fontWeight: '400',
                  color: 'var(--color-gray-400)',
                  marginBottom: '4px'
                }}
              >
                {category.name}
              </div>
              
              {/* 통합된 업그레이드 컨테이너 */}
              <div 
                className="rounded-md px-3 py-2 mb-1"
                style={{ 
                  backgroundColor: isEditMode ? 'var(--color-overlay-bg)' : `rgba(0, 0, 0, ${(opacity || 90) / 100})`,
                  borderRadius: '6px',
                  padding: '8px 12px',
                  marginBottom: '4px'
                }}
              >
                {/* 진행 중인 업그레이드 영역 */}
                {activeItems.length > 0 && (
                  <div style={{ marginBottom: sortedInactiveItems.length > 0 ? '8px' : '0' }}>
                    {activeItems.map((item: any, index: number) => {
                      const key = `${item.item.type}_${item.item.value}`;
                      const cancelledKey = key + '_cancelled';
                      const isCompleting = completingUpgrades.has(key) || completingUpgrades.has(cancelledKey);
                      const upgradeInfo = getUpgradeInfo(item.item.type, item.item.value, item.currentUpgradeLevel || 1);
                      
                      return (
                        <ActiveUpgradeRow
                          key={`${item.item.type}-${item.item.value}-${index}`}
                          item={item}
                          upgradeInfo={upgradeInfo}
                          iconStyle={unitIconStyle}
                          isCompleting={isCompleting}
                          showRemainingTime={presetUpgradeSettings?.showRemainingTime ?? true}
                          showProgressPercentage={presetUpgradeSettings?.showProgressPercentage ?? true}
                          showProgressBar={presetUpgradeSettings?.showProgressBar ?? true}
                        />
                      );
                    })}
                  </div>
                )}

                {/* 구분선 (진행 중인 업그레이드와 비활성 업그레이드가 모두 있을 때) */}
                {activeItems.length > 0 && sortedInactiveItems.length > 0 && (presetUpgradeSettings?.upgradeStateTracking ?? true) && (
                  <div 
                    style={{
                      height: '1px',
                      backgroundColor: 'rgba(255, 255, 255, 0.1)',
                      margin: '6px 0 8px 0'
                    }}
                  />
                )}

                {/* 비활성 업그레이드 영역 (가로 배치) */}
                {sortedInactiveItems.length > 0 && (presetUpgradeSettings?.upgradeStateTracking ?? true) && (
                  <div 
                    className="flex items-center"
                    style={{
                      display: 'flex',
                      alignItems: 'center',
                      flexWrap: 'wrap',
                      gap: '4px',
                      opacity: activeItems.length > 0 ? 0.8 : 1
                    }}
                  >
                    {sortedInactiveItems.map((item: any, index: number) => {
                      const upgradeInfo = getUpgradeInfo(item.item.type, item.item.value, item.currentUpgradeLevel || 1);
                      
                      return (
                        <InactiveUpgradeIcon
                          key={`${item.item.type}-${item.item.value}-${index}`}
                          item={item}
                          upgradeInfo={upgradeInfo}
                          iconStyle={unitIconStyle}
                        />
                      );
                    })}
                  </div>
                )}
              </div>
            </div>
          );
        })}
      </div>
    </div>
  );
});

UpgradeProgress.displayName = 'UpgradeProgress';

export { UpgradeProgress };