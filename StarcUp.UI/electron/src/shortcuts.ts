import { globalShortcut } from 'electron'
import { WindowManager } from './window-manager'
import { DEV_TOOLS_CONFIG, OVERLAY_CONFIG } from './config'

export class ShortcutManager {
  private windowManager: WindowManager

  constructor(windowManager: WindowManager) {
    this.windowManager = windowManager
  }

  registerShortcuts(): void {
    this.registerDevToolsShortcuts()
    this.registerOverlayShortcuts()
  }

  private registerDevToolsShortcuts(): void {
    // 개발자도구 토글 단축키 등록
    DEV_TOOLS_CONFIG.shortcuts.forEach(shortcut => {
      globalShortcut.register(shortcut, () => {
        this.windowManager.toggleDevTools()
      })
    })
  }

  private registerOverlayShortcuts(): void {
    // 오버레이 토글 단축키 등록
    globalShortcut.register(OVERLAY_CONFIG.toggleShortcut, () => {
      this.windowManager.toggleOverlay()
    })
  }

  // 추가 단축키 등록 메서드 (필요시 확장 가능)
  registerCustomShortcut(accelerator: string, callback: () => void): boolean {
    return globalShortcut.register(accelerator, callback)
  }

  // 단축키 해제
  unregisterShortcut(accelerator: string): void {
    globalShortcut.unregister(accelerator)
  }

  // 모든 단축키 해제
  unregisterAllShortcuts(): void {
    globalShortcut.unregisterAll()
  }

  // 단축키 등록 상태 확인
  isRegistered(accelerator: string): boolean {
    return globalShortcut.isRegistered(accelerator)
  }
}