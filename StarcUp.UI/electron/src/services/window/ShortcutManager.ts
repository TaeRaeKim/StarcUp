import { globalShortcut } from 'electron'
import { IWindowManager, IShortcutManager } from './interfaces'
import { IOverlayAutoManager } from '../overlay/interfaces'
import { DEV_TOOLS_CONFIG, OVERLAY_CONFIG } from './WindowConfiguration'

export class ShortcutManager implements IShortcutManager {
  private windowManager: IWindowManager
  private overlayAutoManager: IOverlayAutoManager | null = null
  private registeredShortcuts: Map<string, () => void> = new Map()

  constructor(windowManager: IWindowManager) {
    this.windowManager = windowManager
  }

  setOverlayAutoManager(overlayAutoManager: IOverlayAutoManager): void {
    this.overlayAutoManager = overlayAutoManager
  }

  registerShortcuts(): void {
    this.registerDevToolsShortcuts()
    this.registerOverlayShortcuts()
    console.log('⌨️ 단축키 등록 완료')
  }

  private registerDevToolsShortcuts(): void {
    // 메인 윈도우 개발자도구 토글 단축키 등록
    DEV_TOOLS_CONFIG.shortcuts.forEach(shortcut => {
      const callback = () => this.windowManager.toggleDevTools()
      
      if (globalShortcut.register(shortcut, callback)) {
        this.registeredShortcuts.set(shortcut, callback)
        console.log(`⌨️ 메인 개발자도구 단축키 등록: ${shortcut}`)
      } else {
        console.warn(`⚠️ 단축키 등록 실패: ${shortcut}`)
      }
    })

    // 오버레이 윈도우 개발자도구 토글 단축키 등록
    DEV_TOOLS_CONFIG.overlayShortcuts.forEach(shortcut => {
      const callback = () => this.windowManager.toggleOverlayDevTools()
      
      if (globalShortcut.register(shortcut, callback)) {
        this.registeredShortcuts.set(shortcut, callback)
        console.log(`⌨️ 오버레이 개발자도구 단축키 등록: ${shortcut}`)
      } else {
        console.warn(`⚠️ 오버레이 개발자도구 단축키 등록 실패: ${shortcut}`)
      }
    })
  }

  private registerOverlayShortcuts(): void {
    // 오버레이 토글 단축키 등록
    const callback = () => {
      this.windowManager.toggleOverlay()
      
      // 오버레이가 표시되는 경우 저장된 위치 적용
      if (this.windowManager.isOverlayWindowVisible() && this.overlayAutoManager) {
        this.overlayAutoManager.applyStoredPositionOnShow()
      }
    }
    
    if (globalShortcut.register(OVERLAY_CONFIG.toggleShortcut, callback)) {
      this.registeredShortcuts.set(OVERLAY_CONFIG.toggleShortcut, callback)
      console.log(`⌨️ 오버레이 토글 단축키 등록: ${OVERLAY_CONFIG.toggleShortcut}`)
    } else {
      console.warn(`⚠️ 오버레이 단축키 등록 실패: ${OVERLAY_CONFIG.toggleShortcut}`)
    }
  }

  // 추가 단축키 등록 메서드 (필요시 확장 가능)
  registerCustomShortcut(accelerator: string, callback: () => void): boolean {
    if (this.registeredShortcuts.has(accelerator)) {
      console.warn(`⚠️ 이미 등록된 단축키: ${accelerator}`)
      return false
    }

    if (globalShortcut.register(accelerator, callback)) {
      this.registeredShortcuts.set(accelerator, callback)
      console.log(`⌨️ 커스텀 단축키 등록: ${accelerator}`)
      return true
    } else {
      console.warn(`⚠️ 커스텀 단축키 등록 실패: ${accelerator}`)
      return false
    }
  }

  // 단축키 해제
  unregisterShortcut(accelerator: string): void {
    if (this.registeredShortcuts.has(accelerator)) {
      globalShortcut.unregister(accelerator)
      this.registeredShortcuts.delete(accelerator)
      console.log(`⌨️ 단축키 해제: ${accelerator}`)
    }
  }

  // 모든 단축키 해제
  unregisterAllShortcuts(): void {
    globalShortcut.unregisterAll()
    this.registeredShortcuts.clear()
    console.log('⌨️ 모든 단축키 해제')
  }

  // 단축키 등록 상태 확인
  isRegistered(accelerator: string): boolean {
    return globalShortcut.isRegistered(accelerator)
  }

  // 등록된 단축키 목록 조회
  getRegisteredShortcuts(): string[] {
    return Array.from(this.registeredShortcuts.keys())
  }
}