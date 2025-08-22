// 아이콘 스타일 관련 유틸리티 함수들

export type IconStyleType = 'default' | 'white' | 'yellow' | 'hd'
export type DisplayStatusType = 'inactive' | 'completed' | 'inProgress' | 'normal'

// 아이콘 스타일에 따른 CSS 필터 생성
export const getIconFilter = (iconStyle: IconStyleType): string => {
  switch (iconStyle) {
    case 'white':
      return 'grayscale(1) brightness(1.4) contrast(1.3) saturate(0.8)'
    case 'yellow':
      return 'sepia(1) hue-rotate(20deg) saturate(3) brightness(1.3)'
    case 'hd':
      // HD 모드에서는 HDIcon 컴포넌트가 직접 팀 컬러 합성을 처리하므로 필터 없음
      return 'none'
    default:
      return 'brightness(1.1)'
  }
}

// 개선된 필터 스타일 생성 함수 (상태별 추가 효과 포함)
export const getIconFilterWithStatus = (
  iconStyle: IconStyleType, 
  displayStatus: 'inactive' | 'completed' | 'inProgress' | 'normal' = 'normal'
): string => {
  let baseFilter = ''
  
  // 기본 아이콘 스타일 필터
  switch (iconStyle) {
    case 'white':
      baseFilter = 'grayscale(1) brightness(1.4) contrast(1.3) saturate(0.8)'
      break
    case 'yellow':
      baseFilter = 'sepia(1) hue-rotate(20deg) saturate(3) brightness(1.3)'
      break
    case 'hd':
      // HD 모드에서는 HDIcon 컴포넌트가 직접 팀 컬러 합성을 처리하므로 필터 없음
      return 'none'
    case 'default':
    default:
      baseFilter = 'brightness(1.1)'
      break
  }

  // 상태별 추가 필터
  if (displayStatus === 'inactive') {
    baseFilter += ' grayscale(0.7) opacity(0.6)'
  } else if (displayStatus === 'completed') {
    baseFilter += ' saturate(1.3) brightness(1.2)'
  }
  // 진행 중(inProgress)이나 일반(normal) 상태는 추가 필터 없음

  return baseFilter
}

// 아이콘 컨테이너 스타일 생성 (공통 스타일 + 필터 적용)
export const getIconStyle = (
  iconStyle: IconStyleType,
  teamColor?: string, // HD 모드에서는 사용하지 않지만 호환성을 위해 유지
  additionalStyles: React.CSSProperties = {}
): React.CSSProperties => {
  const baseStyle: React.CSSProperties = {
    width: '27px',
    height: '27px',
    backgroundColor: 'transparent',
    borderRadius: '3px',
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'center',
    flexShrink: 0,
    filter: getIconFilter(iconStyle)
  }

  return {
    ...baseStyle,
    ...additionalStyles
  }
}