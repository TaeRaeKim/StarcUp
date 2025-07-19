import { IOverlayAutoManager } from './interfaces'
import { IWindowManager } from '../window/interfaces'
import { WindowPositionData } from '../types'

export class OverlayAutoManager implements IOverlayAutoManager {
  private windowManager: IWindowManager
  
  // 자동 overlay 관리를 위한 상태
  private isAutoModeEnabled = false
  private isInGame = false
  private isStarcraftInForeground = false
  
  // 윈도우 위치 동기화 관련 상태
  private currentStarCraftPosition: WindowPositionData | null = null
  private enablePositionSync = true
  private lastSyncTime = 0
  private readonly syncThrottleMs = 16 // 60fps (16ms)

  constructor(windowManager: IWindowManager) {
    this.windowManager = windowManager
  }

  /**
   * 자동 overlay 관리 모드 활성화
   */
  enableAutoMode(): void {
    this.isAutoModeEnabled = true
    console.log('🎯 자동 overlay 관리 활성화')
    this.updateOverlayVisibility()
  }

  /**
   * 자동 overlay 관리 모드 비활성화
   */
  disableAutoMode(): void {
    this.isAutoModeEnabled = false
    this.isInGame = false
    this.isStarcraftInForeground = false
    console.log('🎯 자동 overlay 관리 비활성화')
    
    // 자동 관리 비활성화 시 overlay 숨김
    this.windowManager.hideOverlay()
  }

  /**
   * InGame 상태 업데이트
   */
  updateInGameStatus(inGame: boolean): void {
    if (this.isInGame !== inGame) {
      this.isInGame = inGame
      console.log(`🎮 InGame 상태 변경: ${inGame ? 'InGame' : 'OutGame'}`)
      this.updateOverlayVisibility()
    }
  }

  /**
   * Foreground 상태 업데이트
   */
  updateForegroundStatus(foreground: boolean): void {
    if (this.isStarcraftInForeground !== foreground) {
      this.isStarcraftInForeground = foreground
      this.updateOverlayVisibility()
    }
  }

  /**
   * overlay 표시 여부 결정 및 적용
   */
  private updateOverlayVisibility(): void {
    if (!this.isAutoModeEnabled) {
      return
    }

    const shouldShowOverlay = this.isInGame && this.isStarcraftInForeground

    if (shouldShowOverlay) {
      //console.log('✅ 조건 만족 - overlay 표시 (InGame + Foreground)')
      this.windowManager.showOverlay()
      
      // 오버레이 표시 시 현재 스타크래프트 위치로 동기화
      if (this.currentStarCraftPosition && this.enablePositionSync) {
        this.syncOverlayPosition(this.currentStarCraftPosition)
      }
    } else {
      //console.log(`❌ 조건 불만족 - overlay 숨김 (InGame: ${this.isInGame}, Foreground: ${this.isStarcraftInForeground})`)
      this.windowManager.hideOverlay()
    }
  }

  /**
   * 스타크래프트 윈도우 위치 변경 처리
   */
  updateStarCraftWindowPosition(position: WindowPositionData): void {
    this.currentStarCraftPosition = position
    
    //console.log(`🪟 스타크래프트 윈도우 업데이트: ${position.clientX},${position.clientY} ${position.clientWidth}x${position.clientHeight}`)
    
    // 오버레이가 표시 중이고 위치 동기화가 활성화된 경우에만 동기화
    if (this.shouldSyncPosition()) {
      this.syncOverlayPosition(position)
    }
  }

  /**
   * 위치 동기화 여부 결정
   */
  private shouldSyncPosition(): boolean {
    if (!this.enablePositionSync) return false
    if (!this.isAutoModeEnabled) return false
    if (!this.windowManager.isOverlayWindowVisible()) return false
    if (!this.currentStarCraftPosition) return false
    
    // 스타크래프트가 최소화되었거나 보이지 않으면 동기화 안함
    if (this.currentStarCraftPosition.isMinimized || !this.currentStarCraftPosition.isVisible) {
      return false
    }
    
    // Throttling 체크
    const now = Date.now()
    if (now - this.lastSyncTime < this.syncThrottleMs) {
      return false
    }
    
    return true
  }

  /**
   * 오버레이 위치 동기화 실행
   */
  private syncOverlayPosition(position: WindowPositionData): void {
    try {
      // 클라이언트 영역 기준으로 오버레이 위치 설정
      this.windowManager.setOverlayPosition(position.clientX, position.clientY)
      this.windowManager.setOverlaySize(position.clientWidth, position.clientHeight)
      
      this.lastSyncTime = Date.now()
      
      //console.log(`✅ 오버레이 동기화 완료: [${position.clientX}, ${position.clientY}] ${position.clientWidth}x${position.clientHeight}`)
      
      // 오버레이 내부 React 컴포넌트에 중앙 위치 정보 전달
      const centerX = position.clientWidth / 2
      const centerY = position.clientHeight / 2
      
      this.windowManager.sendToOverlayWindow('update-center-position', {
        x: centerX,
        y: centerY,
        gameAreaBounds: {
          x: position.clientX,
          y: position.clientY,
          width: position.clientWidth,
          height: position.clientHeight
        }
      })
      
    } catch (error) {
      console.error('❌ 오버레이 위치 동기화 실패:', error)
    }
  }

  /**
   * 위치 동기화 활성화/비활성화
   */
  enablePositionSyncMode(): void {
    this.enablePositionSync = true
    console.log('🎯 오버레이 위치 동기화 활성화')
    
    // 현재 스타크래프트 위치가 있으면 즉시 동기화
    if (this.currentStarCraftPosition && this.shouldSyncPosition()) {
      this.syncOverlayPosition(this.currentStarCraftPosition)
    }
  }
  
  disablePositionSyncMode(): void {
    this.enablePositionSync = false
    console.log('🎯 오버레이 위치 동기화 비활성화')
  }

  /**
   * 현재 스타크래프트 윈도우 정보 조회
   */
  getCurrentStarCraftPosition(): WindowPositionData | null {
    return this.currentStarCraftPosition ? { ...this.currentStarCraftPosition } : null
  }

  /**
   * 리소스 정리
   */
  dispose(): void {
    this.disableAutoMode()
    this.currentStarCraftPosition = null
    console.log('🧹 OverlayAutoManager 정리 완료')
  }
}