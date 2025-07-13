import { IOverlayAutoManager } from './interfaces'
import { IWindowManager } from '../window/interfaces'

export class OverlayAutoManager implements IOverlayAutoManager {
  private windowManager: IWindowManager
  
  // 자동 overlay 관리를 위한 상태
  private isAutoModeEnabled = false
  private isInGame = false
  private isStarcraftInForeground = false

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
      console.log('✅ 조건 만족 - overlay 표시 (InGame + Foreground)')
      this.windowManager.showOverlay()
    } else {
      console.log(`❌ 조건 불만족 - overlay 숨김 (InGame: ${this.isInGame}, Foreground: ${this.isStarcraftInForeground})`)
      this.windowManager.hideOverlay()
    }
  }

  /**
   * 리소스 정리
   */
  dispose(): void {
    this.disableAutoMode()
    console.log('🧹 OverlayAutoManager 정리 완료')
  }
}