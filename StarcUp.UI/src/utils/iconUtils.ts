// 아이콘 스타일 관련 유틸리티 함수들

export type IconStyleType = 'default' | 'white' | 'yellow' | 'hd'

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