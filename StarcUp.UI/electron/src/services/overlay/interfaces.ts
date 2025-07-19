import { WindowPositionData } from '../types'

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
   * 스타크래프트 윈도우 위치 변경 처리
   */
  updateStarCraftWindowPosition(position: WindowPositionData): void

  /**
   * 위치 동기화 활성화/비활성화
   */
  enablePositionSyncMode(): void
  disablePositionSyncMode(): void

  /**
   * 현재 스타크래프트 윈도우 정보 조회
   */
  getCurrentStarCraftPosition(): WindowPositionData | null

  /**
   * 리소스 정리
   */
  dispose(): void
}