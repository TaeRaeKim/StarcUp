/**
 * SnapManager - 오버레이 컴포넌트의 스냅 기능을 관리하는 중앙 서비스
 * 
 * 주요 기능:
 * - 오버레이 컴포넌트 등록 및 관리
 * - 스냅 계산 조율
 * - 스냅 가이드 표시 제어
 */

export interface Position {
  x: number
  y: number
}

export interface Size {
  width: number
  height: number
}

export interface OverlayInfo {
  id: string
  position: Position
  size: Size
  element: HTMLElement | null
}

export interface SnapGuide {
  type: 'vertical' | 'horizontal'
  position: number
  label?: string
  source?: string // 스냅 소스 (예: 'screen-edge', 'overlay-item')
}

export interface SnapResult {
  position: Position
  guides: SnapGuide[]
  snapped: boolean
}

export interface SnapManagerConfig {
  snapThreshold: number // 스냅이 작동하는 거리 (픽셀)
  edgeMargin: number // 가장자리 여백
  showGuides: boolean // 가이드라인 표시 여부
  snapToScreen: boolean // 화면 경계 스냅 활성화
  snapToItems: boolean // 다른 아이템과의 스냅 활성화
}

export interface DefaultPositions {
  [key: string]: Position
}


class SnapManager {
  private overlays: Map<string, OverlayInfo> = new Map()
  private config: SnapManagerConfig = {
    snapThreshold: 20,
    edgeMargin: 20,
    showGuides: true,
    snapToScreen: true,
    snapToItems: true
  }
  private guideUpdateCallback: ((guides: SnapGuide[]) => void) | null = null
  private defaultPositions: DefaultPositions = {
    workerStatus: { x: 50, y: 50 },
    populationWarning: { x: 300, y: 50 },
    upgradeProgress: { x: 50, y: 200 },
    buildOrder: { x: 50, y: 120 },
    unitCount: { x: 500, y: 50 }
  }

  /**
   * ID를 사용자 친화적인 이름으로 변환
   */
  private getDisplayName(id: string): string {
    const displayNames: Record<string, string> = {
      'workerStatus': '일꾼 상태',
      'populationWarning': '인구 경고',
      'upgradeProgress': '업그레이드 진행도',
      'buildOrder': '빌드 오더',
      'unitCount': '유닛 수'
    }
    return displayNames[id] || id
  }

  /**
   * 오버레이 등록
   */
  registerOverlay(id: string, info: OverlayInfo): void {
    this.overlays.set(id, info)
    console.log(`[SnapManager] 오버레이 등록: ${this.getDisplayName(id)}`, info)
  }

  /**
   * 오버레이 등록 해제
   */
  unregisterOverlay(id: string): void {
    this.overlays.delete(id)
    console.log(`[SnapManager] 오버레이 등록 해제: ${id}`)
  }

  /**
   * 오버레이 정보 업데이트
   */
  updateOverlay(id: string, info: Partial<OverlayInfo>): void {
    const existing = this.overlays.get(id)
    if (existing) {
      this.overlays.set(id, { ...existing, ...info })
    }
  }

  /**
   * 다른 오버레이 목록 가져오기 (특정 오버레이 제외)
   */
  getOtherOverlays(excludeId: string): OverlayInfo[] {
    const others: OverlayInfo[] = []
    this.overlays.forEach((overlay, id) => {
      if (id !== excludeId) {
        others.push(overlay)
      }
    })
    return others
  }

  /**
   * 스냅 계산
   */
  calculateSnap(
    overlayId: string,
    currentPosition: Position,
    overlaySize: Size,
    containerSize: Size
  ): SnapResult {
    const guides: SnapGuide[] = []
    let snappedX = currentPosition.x
    let snappedY = currentPosition.y
    let isSnapped = false

    // 화면 경계 스냅 계산
    if (this.config.snapToScreen) {
      const screenSnap = this.calculateScreenSnap(
        currentPosition,
        overlaySize,
        containerSize
      )
      if (screenSnap.snapped) {
        snappedX = screenSnap.position.x
        snappedY = screenSnap.position.y
        guides.push(...screenSnap.guides)
        isSnapped = true
      }
    }

    // 다른 아이템과의 스냅 계산 (화면 스냅이 안 된 경우만)
    if (this.config.snapToItems && !isSnapped) {
      const itemSnap = this.calculateItemSnap(
        overlayId,
        currentPosition,
        overlaySize
      )
      if (itemSnap.snapped) {
        snappedX = itemSnap.position.x
        snappedY = itemSnap.position.y
        guides.push(...itemSnap.guides)
        isSnapped = true
      }
    }

    // 가이드 업데이트 콜백 호출
    if (this.guideUpdateCallback && this.config.showGuides) {
      this.guideUpdateCallback(guides)
    }

    return {
      position: { x: snappedX, y: snappedY },
      guides,
      snapped: isSnapped
    }
  }

  /**
   * 화면 경계 스냅 계산
   */
  private calculateScreenSnap(
    position: Position,
    size: Size,
    containerSize: Size
  ): SnapResult {
    const { snapThreshold, edgeMargin } = this.config
    const guides: SnapGuide[] = []
    let snappedX = position.x
    let snappedY = position.y
    let snapped = false

    // 수직 스냅 포인트 (X축)
    const verticalSnapPoints = [
      { pos: edgeMargin, label: '좌측', type: 'left' as const },
      { pos: containerSize.width / 2 - size.width / 2, label: '중앙', type: 'center' as const },
      { pos: containerSize.width - edgeMargin - size.width, label: '우측', type: 'right' as const }
    ]

    // 수평 스냅 포인트 (Y축)
    const horizontalSnapPoints = [
      { pos: edgeMargin, label: '상단', type: 'top' as const },
      { pos: containerSize.height / 2 - size.height / 2, label: '중앙', type: 'center' as const },
      { pos: containerSize.height - edgeMargin - size.height, label: '하단', type: 'bottom' as const }
    ]

    // X축 스냅 검사
    for (const point of verticalSnapPoints) {
      if (Math.abs(position.x - point.pos) < snapThreshold) {
        snappedX = point.pos
        guides.push({
          type: 'vertical',
          position: point.type === 'left' ? edgeMargin :
                   point.type === 'right' ? containerSize.width - edgeMargin :
                   containerSize.width / 2,
          label: point.label,
          source: 'screen-edge'
        })
        snapped = true
        break
      }
    }

    // Y축 스냅 검사
    for (const point of horizontalSnapPoints) {
      if (Math.abs(position.y - point.pos) < snapThreshold) {
        snappedY = point.pos
        guides.push({
          type: 'horizontal',
          position: point.type === 'top' ? edgeMargin :
                   point.type === 'bottom' ? containerSize.height - edgeMargin :
                   containerSize.height / 2,
          label: point.label,
          source: 'screen-edge'
        })
        snapped = true
        break
      }
    }

    return {
      position: { x: snappedX, y: snappedY },
      guides,
      snapped
    }
  }

  /**
   * 다른 아이템과의 스냅 계산
   */
  private calculateItemSnap(
    overlayId: string,
    position: Position,
    size: Size
  ): SnapResult {
    const { snapThreshold } = this.config
    const guides: SnapGuide[] = []
    let snappedX = position.x
    let snappedY = position.y
    let snapped = false

    const otherOverlays = this.getOtherOverlays(overlayId)

    for (const other of otherOverlays) {
      if (!other.element) continue

      // 다른 오버레이의 스냅 포인트
      const displayName = this.getDisplayName(other.id)
      const otherSnapPoints = {
        vertical: [
          { pos: other.position.x, label: `${displayName} 좌측`, type: 'left' },
          { pos: other.position.x + other.size.width / 2, label: `${displayName} 중앙`, type: 'center' },
          { pos: other.position.x + other.size.width, label: `${displayName} 우측`, type: 'right' }
        ],
        horizontal: [
          { pos: other.position.y, label: `${displayName} 상단`, type: 'top' },
          { pos: other.position.y + other.size.height / 2, label: `${displayName} 중앙`, type: 'center' },
          { pos: other.position.y + other.size.height, label: `${displayName} 하단`, type: 'bottom' }
        ]
      }

      // X축 스냅 검사 (좌측, 중앙, 우측 정렬)
      for (const point of otherSnapPoints.vertical) {
        // 현재 아이템의 각 에지와 다른 아이템의 스냅 포인트 비교
        const currentEdges = [
          { pos: position.x, offset: 0 }, // 좌측
          { pos: position.x + size.width / 2, offset: -size.width / 2 }, // 중앙
          { pos: position.x + size.width, offset: -size.width } // 우측
        ]

        for (const edge of currentEdges) {
          if (Math.abs(edge.pos - point.pos) < snapThreshold) {
            snappedX = point.pos + edge.offset
            guides.push({
              type: 'vertical',
              position: point.pos,
              label: point.label,
              source: 'overlay-item'
            })
            snapped = true
            break
          }
        }
        if (snapped) break
      }

      // Y축 스냅 검사 (상단, 중앙, 하단 정렬)
      for (const point of otherSnapPoints.horizontal) {
        const currentEdges = [
          { pos: position.y, offset: 0 }, // 상단
          { pos: position.y + size.height / 2, offset: -size.height / 2 }, // 중앙
          { pos: position.y + size.height, offset: -size.height } // 하단
        ]

        for (const edge of currentEdges) {
          if (Math.abs(edge.pos - point.pos) < snapThreshold) {
            snappedY = point.pos + edge.offset
            guides.push({
              type: 'horizontal',
              position: point.pos,
              label: point.label,
              source: 'overlay-item'
            })
            snapped = true
            break
          }
        }
        if (snapped) break
      }

      if (snapped) break // 첫 번째 스냅만 적용
    }

    return {
      position: { x: snappedX, y: snappedY },
      guides,
      snapped
    }
  }

  /**
   * 설정 업데이트
   */
  updateConfig(config: Partial<SnapManagerConfig>): void {
    this.config = { ...this.config, ...config }
  }

  /**
   * 가이드 업데이트 콜백 설정
   */
  setGuideUpdateCallback(callback: (guides: SnapGuide[]) => void): void {
    this.guideUpdateCallback = callback
  }

  /**
   * 가이드 초기화
   */
  clearGuides(): void {
    if (this.guideUpdateCallback) {
      this.guideUpdateCallback([])
    }
  }

  /**
   * 기본 위치 설정
   */
  setDefaultPosition(id: string, position: Position): void {
    this.defaultPositions[id] = position
  }

  /**
   * 기본 위치 가져오기
   */
  getDefaultPosition(id: string): Position {
    return this.defaultPositions[id] || { x: 50, y: 50 }
  }

  /**
   * 오버레이 위치를 기본값으로 리셋
   */
  resetToDefaultPosition(id: string): Position {
    const defaultPos = this.getDefaultPosition(id)
    console.log(`[SnapManager] ${this.getDisplayName(id)} 위치 리셋:`, defaultPos)
    return defaultPos
  }

  /**
   * 모든 오버레이 위치를 기본값으로 리셋
   */
  resetAllToDefaultPositions(): DefaultPositions {
    console.log('[SnapManager] 모든 오버레이 위치 리셋')
    return { ...this.defaultPositions }
  }

  /**
   * 비율 기반으로 새 화면 크기에 맞는 위치 계산
   */
  calculateProportionalPosition(
    currentPosition: Position,
    elementSize: Size,
    oldContainerSize: Size,
    newContainerSize: Size
  ): Position {
    // 기존 화면에서의 비율 계산 (0.0 ~ 1.0)
    const xRatio = oldContainerSize.width > 0 ? currentPosition.x / oldContainerSize.width : 0
    const yRatio = oldContainerSize.height > 0 ? currentPosition.y / oldContainerSize.height : 0
    
    // 새 화면 크기에 비율 적용
    let newX = xRatio * newContainerSize.width
    let newY = yRatio * newContainerSize.height
    
    // 경계 제한 (요소가 화면 밖으로 나가지 않도록)
    newX = Math.max(0, Math.min(newContainerSize.width - elementSize.width, newX))
    newY = Math.max(0, Math.min(newContainerSize.height - elementSize.height, newY))
    
    return { x: newX, y: newY }
  }

  /**
   * 화면 크기 변경 시 위치를 경계 내로 조정
   */
  adjustPositionForScreenSize(
    id: string,
    currentPosition: Position,
    elementSize: Size,
    containerSize: Size,
    oldContainerSize?: Size
  ): Position {
    // 이전 컨테이너 크기가 제공된 경우 비율 기반 계산 사용
    if (oldContainerSize && oldContainerSize.width > 0 && oldContainerSize.height > 0) {
      const proportionalPosition = this.calculateProportionalPosition(
        currentPosition,
        elementSize,
        oldContainerSize,
        containerSize
      )
      
      console.log(`📐 [SnapManager] ${this.getDisplayName(id)} 비율 유지 조정:`, 
                 `${currentPosition.x}, ${currentPosition.y} → ${proportionalPosition.x.toFixed(1)}, ${proportionalPosition.y.toFixed(1)}`,
                 `비율: ${(currentPosition.x / oldContainerSize.width * 100).toFixed(1)}%, ${(currentPosition.y / oldContainerSize.height * 100).toFixed(1)}%`)
      
      return proportionalPosition
    }

    // 이전 크기 정보가 없으면 기존 경계 체크 로직 사용
    let newX = currentPosition.x
    let newY = currentPosition.y
    let adjusted = false

    // 우측 경계 초과 시 조정
    if (currentPosition.x + elementSize.width > containerSize.width) {
      newX = containerSize.width - elementSize.width - this.config.edgeMargin
      adjusted = true
    }

    // 하단 경계 초과 시 조정
    if (currentPosition.y + elementSize.height > containerSize.height) {
      newY = containerSize.height - elementSize.height - this.config.edgeMargin
      adjusted = true
    }

    // 좌측 경계 미만 시 조정
    if (currentPosition.x < 0) {
      newX = this.config.edgeMargin
      adjusted = true
    }

    // 상단 경계 미만 시 조정
    if (currentPosition.y < 0) {
      newY = this.config.edgeMargin
      adjusted = true
    }

    const finalPosition = { x: Math.max(0, newX), y: Math.max(0, newY) }
    
    if (adjusted) {
      console.log(`📐 [SnapManager] ${this.getDisplayName(id)} 경계 기반 조정:`, 
                 `${currentPosition.x}, ${currentPosition.y} → ${finalPosition.x}, ${finalPosition.y}`)
    }

    return finalPosition
  }

  /**
   * 모든 오버레이 초기화
   */
  clear(): void {
    this.overlays.clear()
    this.clearGuides()
  }
}

// 싱글톤 인스턴스 export
export const snapManager = new SnapManager()