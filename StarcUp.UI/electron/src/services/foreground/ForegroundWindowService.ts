import activeWindow from 'active-win';
import { EventEmitter } from 'events';
import { IForegroundWindowService, GameProcess, ForegroundChangedEvent } from './interfaces';

export class ForegroundWindowService extends EventEmitter implements IForegroundWindowService {
  private monitoringInterval?: NodeJS.Timeout;
  private currentGameProcess?: GameProcess;
  private lastForegroundState: boolean = false;
  private readonly MONITORING_INTERVAL_MS = 1000; // 1초마다 확인

  constructor() {
    super();
  }

  /**
   * 특정 프로세스 ID가 현재 foreground 윈도우인지 확인
   */
  async isProcessInForeground(processId: number): Promise<boolean> {
    try {
      const activeWin = await activeWindow();
      return activeWin?.owner.processId === processId;
    } catch (error) {
      console.error('Error checking foreground window:', error);
      return false;
    }
  }


  /**
   * 스타크래프트 프로세스가 foreground인지 주기적으로 확인하고 이벤트 발생
   */
  startMonitoring(gameProcess: GameProcess): void {
    if (this.monitoringInterval) {
      this.stopMonitoring();
    }

    this.currentGameProcess = gameProcess;
    console.log(`📱 스타크래프트 foreground 모니터링 시작 - PID: ${gameProcess.processId}`);

    this.monitoringInterval = setInterval(async () => {
      await this.checkForegroundState();
    }, this.MONITORING_INTERVAL_MS);

    // 즉시 한 번 확인
    this.checkForegroundState();
  }

  /**
   * 모니터링 중지
   */
  stopMonitoring(): void {
    if (this.monitoringInterval) {
      clearInterval(this.monitoringInterval);
      this.monitoringInterval = undefined;
      console.log('📱 스타크래프트 foreground 모니터링 중지');
    }
    this.currentGameProcess = undefined;
    this.lastForegroundState = false;
  }

  /**
   * 현재 모니터링 상태 확인
   */
  isMonitoring(): boolean {
    return this.monitoringInterval !== undefined;
  }

  /**
   * 현재 foreground 상태를 확인하고 변경사항이 있으면 이벤트 발생
   */
  private async checkForegroundState(): Promise<void> {
    if (!this.currentGameProcess) {
      return;
    }

    try {
      const isInForeground = await this.isProcessInForeground(this.currentGameProcess.processId);
      
      // 상태가 변경된 경우에만 이벤트 발생
      if (isInForeground !== this.lastForegroundState) {
        this.lastForegroundState = isInForeground;
        
        const activeWin = await activeWindow();
        
        const event: ForegroundChangedEvent = {
          isStarcraftInForeground: isInForeground,
          activeWindow: activeWin ? {
            title: activeWin.title,
            processId: activeWin.owner.processId,
            processName: activeWin.owner.name
          } : undefined
        };

        console.log(`🔄 스타크래프트 foreground 상태 변경: ${isInForeground ? 'FOREGROUND' : 'BACKGROUND'}`);
        if (activeWin && !isInForeground) {
          console.log(`   현재 활성 윈도우: ${activeWin.title} (PID: ${activeWin.owner.processId})`);
        }

        this.emit('foreground-changed', event);
      }
    } catch (error) {
      console.error('Error checking foreground state:', error);
    }
  }

  /**
   * 리소스 정리
   */
  dispose(): void {
    this.stopMonitoring();
    this.removeAllListeners();
  }
}