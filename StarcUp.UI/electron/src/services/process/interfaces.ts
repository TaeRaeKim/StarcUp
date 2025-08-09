export interface ICoreProcessService {
  /**
   * StarcUp.Core 프로세스 시작
   * @param isDevelopment 개발 모드 여부
   */
  startCoreProcess(isDevelopment?: boolean): Promise<void>
  
  /**
   * StarcUp.Core 프로세스 중지
   */
  stopCoreProcess(): Promise<void>
  
  /**
   * 프로세스 실행 상태 확인
   */
  readonly isRunning: boolean
}