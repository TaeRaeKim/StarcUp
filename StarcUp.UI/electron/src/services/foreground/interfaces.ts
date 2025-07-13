export interface GameProcess {
  processId: number;
  processName: string;
  detectedAt: string;
}

export interface GameDetectedEvent {
  eventType: 'game-detected';
  gameInfo: GameProcess;
}

export interface IForegroundWindowService {
  /**
   * 특정 프로세스 ID가 현재 foreground 윈도우인지 확인
   */
  isProcessInForeground(processId: number): Promise<boolean>;

  /**
   * 스타크래프트 프로세스가 foreground인지 주기적으로 확인하고 이벤트 발생
   */
  startMonitoring(gameProcess: GameProcess): void;

  /**
   * 모니터링 중지
   */
  stopMonitoring(): void;

  /**
   * 현재 모니터링 상태 확인
   */
  isMonitoring(): boolean;

  /**
   * 이벤트 리스너 등록
   */
  on(event: 'foreground-changed', listener: (event: ForegroundChangedEvent) => void): this;

  /**
   * 리소스 정리
   */
  dispose(): void;
}

export interface ForegroundChangedEvent {
  isStarcraftInForeground: boolean;
  activeWindow?: {
    title: string;
    processId: number;
    processName: string;
  };
}