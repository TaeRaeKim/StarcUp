export interface IOverlayAutoManager {
  /**
   * 자동 overlay 관리 모드 활성화
   */
  enableAutoMode(): void

  /**
   * 자동 overlay 관리 모드 비활성화
   */
  disableAutoMode(): void

  /**
   * InGame 상태 업데이트
   */
  updateInGameStatus(inGame: boolean): void

  /**
   * Foreground 상태 업데이트
   */
  updateForegroundStatus(foreground: boolean): void

  /**
   * 리소스 정리
   */
  dispose(): void
}