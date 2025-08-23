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

export interface SnapState {
  // 수직 가이드라인 (X축 스냅 상태)
  verticalSnap?: {
    type: 'left' | 'center' | 'right'
    source: 'screen-edge' | 'overlay-item'
    targetId?: string // overlay-item인 경우 타겟 오버레이 ID
  }
  // 수평 가이드라인 (Y축 스냅 상태)
  horizontalSnap?: {
    type: 'top' | 'center' | 'bottom'
    source: 'screen-edge' | 'overlay-item'
    targetId?: string // overlay-item인 경우 타겟 오버레이 ID
  }
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
   * 화면 경계 스냅 포인트 계산
   */
  private getScreenSnapPoints(containerSize: Size, elementSize: Size) {
    const { edgeMargin } = this.config
    
    return {
      vertical: [
        { pos: edgeMargin, type: 'left' as const, label: '좌측' },
        { pos: containerSize.width / 2 - elementSize.width / 2, type: 'center' as const, label: '중앙' },
        { pos: containerSize.width - edgeMargin - elementSize.width, type: 'right' as const, label: '우측' }
      ],
      horizontal: [
        { pos: edgeMargin, type: 'top' as const, label: '상단' },
        { pos: containerSize.height / 2 - elementSize.height / 2, type: 'center' as const, label: '중앙' },
        { pos: containerSize.height - edgeMargin - elementSize.height, type: 'bottom' as const, label: '하단' }
      ]
    }
  }

  /**
   * 오버레이 스냅 포인트 계산
   */
  private getOverlaySnapPoints(overlay: OverlayInfo) {
    const displayName = this.getDisplayName(overlay.id)
    
    return {
      vertical: [
        { pos: overlay.position.x, type: 'left' as const, label: `${displayName} 좌측` },
        { pos: overlay.position.x + overlay.size.width / 2, type: 'center' as const, label: `${displayName} 중앙` },
        { pos: overlay.position.x + overlay.size.width, type: 'right' as const, label: `${displayName} 우측` }
      ],
      horizontal: [
        { pos: overlay.position.y, type: 'top' as const, label: `${displayName} 상단` },
        { pos: overlay.position.y + overlay.size.height / 2, type: 'center' as const, label: `${displayName} 중앙` },
        { pos: overlay.position.y + overlay.size.height, type: 'bottom' as const, label: `${displayName} 하단` }
      ]
    }
  }

  /**
   * 현재 요소의 에지 포인트 계산
   */
  private getCurrentEdgePoints(position: Position, size: Size) {
    return {
      vertical: [
        { pos: position.x, type: 'left' as const, offset: 0 },
        { pos: position.x + size.width / 2, type: 'center' as const, offset: -size.width / 2 },
        { pos: position.x + size.width, type: 'right' as const, offset: -size.width }
      ],
      horizontal: [
        { pos: position.y, type: 'top' as const, offset: 0 },
        { pos: position.y + size.height / 2, type: 'center' as const, offset: -size.height / 2 },
        { pos: position.y + size.height, type: 'bottom' as const, offset: -size.height }
      ]
    }
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

    const snapPoints = this.getScreenSnapPoints(containerSize, size)

    // X축 스냅 검사
    for (const point of snapPoints.vertical) {
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
    for (const point of snapPoints.horizontal) {
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
   * 두 축의 에지 점들 간 스냅 검사
   */
  private checkEdgeSnap(
    currentEdges: Array<{ pos: number; type: string; offset: number }>,
    targetPoints: Array<{ pos: number; type: string; label: string }>,
    snapThreshold: number,
    axis: 'vertical' | 'horizontal'
  ) {
    for (const point of targetPoints) {
      for (const edge of currentEdges) {
        if (Math.abs(edge.pos - point.pos) < snapThreshold) {
          return {
            snapped: true,
            position: point.pos + edge.offset,
            guide: {
              type: axis,
              position: point.pos,
              label: point.label,
              source: 'overlay-item' as const
            }
          }
        }
      }
    }
    return { snapped: false }
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
    const currentEdges = this.getCurrentEdgePoints(position, size)

    for (const other of otherOverlays) {
      if (!other.element) continue

      const otherSnapPoints = this.getOverlaySnapPoints(other)

      // X축 스냅 검사
      if (!snapped) {
        const xSnapResult = this.checkEdgeSnap(
          currentEdges.vertical,
          otherSnapPoints.vertical,
          snapThreshold,
          'vertical'
        )
        
        if (xSnapResult.snapped) {
          snappedX = xSnapResult.position!
          guides.push(xSnapResult.guide!)
          snapped = true
        }
      }

      // Y축 스냅 검사  
      if (!snapped) {
        const ySnapResult = this.checkEdgeSnap(
          currentEdges.horizontal,
          otherSnapPoints.horizontal,
          snapThreshold,
          'horizontal'
        )
        
        if (ySnapResult.snapped) {
          snappedY = ySnapResult.position!
          guides.push(ySnapResult.guide!)
          snapped = true
        }
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
   * 스마트 위치 조정 - 스냅 상태에 따라 가이드라인 또는 비율 유지
   */
  adjustPositionForScreenSize(
    id: string,
    currentPosition: Position,
    elementSize: Size,
    containerSize: Size,
    oldContainerSize?: Size
  ): Position {
    // 이전 컨테이너 크기가 없으면 경계 체크만 수행
    if (!oldContainerSize || oldContainerSize.width <= 0 || oldContainerSize.height <= 0) {
      return this.adjustPositionWithBoundaryCheck(id, currentPosition, elementSize, containerSize)
    }

    // 현재 위치의 스냅 상태 감지
    const snapState = this.detectSnapState(id, currentPosition, elementSize, oldContainerSize)
    
    // 비율 기반 fallback 위치 계산
    const proportionalPosition = this.calculateProportionalPosition(
      currentPosition,
      elementSize,
      oldContainerSize,
      containerSize
    )

    // 스냅 상태가 있으면 가이드라인 기반으로 계산
    if (snapState.verticalSnap || snapState.horizontalSnap) {
      const guidelinePosition = this.calculateGuidelineBasedPosition(
        id,
        snapState,
        elementSize,
        oldContainerSize,
        containerSize,
        proportionalPosition
      )

      const snapInfo = []
      if (snapState.verticalSnap) {
        const source = snapState.verticalSnap.source === 'screen-edge' ? '화면' : this.getDisplayName(snapState.verticalSnap.targetId || '')
        snapInfo.push(`X축: ${source} ${snapState.verticalSnap.type}`)
      } else {
        snapInfo.push('X축: 비율 유지')
      }
      
      if (snapState.horizontalSnap) {
        const source = snapState.horizontalSnap.source === 'screen-edge' ? '화면' : this.getDisplayName(snapState.horizontalSnap.targetId || '')
        snapInfo.push(`Y축: ${source} ${snapState.horizontalSnap.type}`)
      } else {
        snapInfo.push('Y축: 비율 유지')
      }

      console.log(`🎯 [SnapManager] ${this.getDisplayName(id)} 가이드라인 기반 조정:`, 
                 `${currentPosition.x}, ${currentPosition.y} → ${guidelinePosition.x.toFixed(1)}, ${guidelinePosition.y.toFixed(1)}`,
                 `(${snapInfo.join(', ')})`)
      
      return guidelinePosition
    }

    // 스냅 상태가 없으면 비율 유지로 조정
    console.log(`📐 [SnapManager] ${this.getDisplayName(id)} 비율 유지 조정:`, 
               `${currentPosition.x}, ${currentPosition.y} → ${proportionalPosition.x.toFixed(1)}, ${proportionalPosition.y.toFixed(1)}`,
               `비율: ${(currentPosition.x / oldContainerSize.width * 100).toFixed(1)}%, ${(currentPosition.y / oldContainerSize.height * 100).toFixed(1)}%`)
    
    return proportionalPosition
  }

  /**
   * 경계 체크 기반 위치 조정 (기존 로직)
   */
  private adjustPositionWithBoundaryCheck(
    id: string,
    currentPosition: Position,
    elementSize: Size,
    containerSize: Size
  ): Position {
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
   * 스냅 상태 감지를 위한 헬퍼 함수
   */
  private detectAxisSnapState(
    currentPos: number,
    targetPoints: Array<{ pos: number; type: string }>,
    snapThreshold: number,
    source: 'screen-edge' | 'overlay-item',
    targetId?: string
  ) {
    for (const point of targetPoints) {
      if (Math.abs(currentPos - point.pos) <= snapThreshold) {
        return {
          type: point.type as any,
          source,
          ...(targetId && { targetId })
        }
      }
    }
    return undefined
  }

  /**
   * 현재 위치가 어떤 가이드라인에 붙어있는지 감지
   */
  detectSnapState(
    overlayId: string,
    position: Position,
    size: Size,
    containerSize: Size
  ): SnapState {
    const { snapThreshold } = this.config
    const snapState: SnapState = {}
    const screenSnapPoints = this.getScreenSnapPoints(containerSize, size)
    const currentEdges = this.getCurrentEdgePoints(position, size)

    // 화면 경계 스냅 상태 감지
    snapState.verticalSnap = this.detectAxisSnapState(
      position.x,
      screenSnapPoints.vertical,
      snapThreshold,
      'screen-edge'
    )

    snapState.horizontalSnap = this.detectAxisSnapState(
      position.y,
      screenSnapPoints.horizontal,
      snapThreshold,
      'screen-edge'
    )

    // 다른 오버레이와의 스냅 상태 감지 (화면 스냅이 없는 경우만)
    if (!snapState.verticalSnap || !snapState.horizontalSnap) {
      const otherOverlays = this.getOtherOverlays(overlayId)
      
      for (const other of otherOverlays) {
        if (!other.element) continue
        const otherSnapPoints = this.getOverlaySnapPoints(other)

        // X축 (수직) 스냅 상태 감지
        if (!snapState.verticalSnap) {
          for (const point of otherSnapPoints.vertical) {
            for (const edge of currentEdges.vertical) {
              if (Math.abs(edge.pos - point.pos) <= snapThreshold) {
                snapState.verticalSnap = {
                  type: edge.type as any,
                  source: 'overlay-item',
                  targetId: other.id
                }
                break
              }
            }
            if (snapState.verticalSnap) break
          }
        }

        // Y축 (수평) 스냅 상태 감지
        if (!snapState.horizontalSnap) {
          for (const point of otherSnapPoints.horizontal) {
            for (const edge of currentEdges.horizontal) {
              if (Math.abs(edge.pos - point.pos) <= snapThreshold) {
                snapState.horizontalSnap = {
                  type: edge.type as any,
                  source: 'overlay-item',
                  targetId: other.id
                }
                break
              }
            }
            if (snapState.horizontalSnap) break
          }
        }

        if (snapState.verticalSnap && snapState.horizontalSnap) break
      }
    }

    return snapState
  }

  /**
   * 축별 가이드라인 기반 위치 계산
   */
  private calculateAxisPosition(
    snapInfo: { type: string; source: 'screen-edge' | 'overlay-item'; targetId?: string },
    elementSize: Size,
    containerSize: Size,
    axis: 'x' | 'y'
  ): number {
    const { type, source, targetId } = snapInfo
    const isVertical = axis === 'x'
    const dimension = isVertical ? 'width' : 'height'
    const containerDim = containerSize[dimension]
    const elementDim = elementSize[dimension]

    if (source === 'screen-edge') {
      switch (type) {
        case isVertical ? 'left' : 'top':
          return this.config.edgeMargin
        case 'center':
          return containerDim / 2 - elementDim / 2
        case isVertical ? 'right' : 'bottom':
          return containerDim - this.config.edgeMargin - elementDim
      }
    } else if (source === 'overlay-item' && targetId) {
      const targetOverlay = this.overlays.get(targetId)
      if (targetOverlay) {
        const targetPos = isVertical ? targetOverlay.position.x : targetOverlay.position.y
        const targetSize = isVertical ? targetOverlay.size.width : targetOverlay.size.height
        
        switch (type) {
          case isVertical ? 'left' : 'top':
            return targetPos
          case 'center':
            return targetPos + targetSize / 2 - elementDim / 2
          case isVertical ? 'right' : 'bottom':
            return targetPos + targetSize - elementDim
        }
      }
    }
    
    return 0 // fallback
  }

  /**
   * 스냅 상태에 따른 가이드라인 기반 위치 계산
   */
  calculateGuidelineBasedPosition(
    overlayId: string,
    snapState: SnapState,
    elementSize: Size,
    oldContainerSize: Size,
    newContainerSize: Size,
    fallbackPosition: Position
  ): Position {
    let newX = fallbackPosition.x
    let newY = fallbackPosition.y

    // X축 처리 (수직 가이드라인)
    if (snapState.verticalSnap) {
      newX = this.calculateAxisPosition(
        snapState.verticalSnap,
        elementSize,
        newContainerSize,
        'x'
      )
    }

    // Y축 처리 (수평 가이드라인)
    if (snapState.horizontalSnap) {
      newY = this.calculateAxisPosition(
        snapState.horizontalSnap,
        elementSize,
        newContainerSize,
        'y'
      )
    }

    // 경계 제한 적용
    newX = Math.max(0, Math.min(newContainerSize.width - elementSize.width, newX))
    newY = Math.max(0, Math.min(newContainerSize.height - elementSize.height, newY))

    return { x: newX, y: newY }
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