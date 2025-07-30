import { app, BrowserWindow } from 'electron'
import { serviceContainer } from './src/services/ServiceContainer'
import { IWindowManager, IShortcutManager } from './src/services/window'
import { ICoreCommunicationService } from './src/services/core'

// 애플리케이션 종료 처리
app.on('window-all-closed', async () => {
  if (process.platform !== 'darwin') {
    await serviceContainer.dispose()
    app.quit()
  }
})

app.on('activate', () => {
  // macOS에서 dock 아이콘 클릭 시 윈도우 재생성
  if (BrowserWindow.getAllWindows().length === 0) {
    initializeApp()
  }
})

app.on('before-quit', async () => {
  // 애플리케이션 종료 전 서비스 정리
  await serviceContainer.dispose()
})

app.whenReady().then(() => {
  initializeApp()
})

async function initializeApp(): Promise<void> {
  try {
    // 환경 감지
    const isDevelopment = process.env.NODE_ENV === 'development' || !app.isPackaged
    console.log(`🏗️ 애플리케이션 모드: ${isDevelopment ? '개발' : '프로덕션'}`)

    // 서비스 컨테이너 초기화
    serviceContainer.initialize()

    // 서비스 해결
    const windowManager = serviceContainer.resolve<IWindowManager>('windowManager')
    const shortcutManager = serviceContainer.resolve<IShortcutManager>('shortcutManager')
    const coreService = serviceContainer.resolve<ICoreCommunicationService>('coreCommunicationService')
    
    // IPC 핸들러는 ServiceContainer.initialize()에서 자동으로 등록됨

    // 윈도우 생성
    windowManager.createMainWindow()
    windowManager.createOverlayWindow()

    // 단축키 등록
    shortcutManager.registerShortcuts()

    // Core 프로세스 연결 시도
    try {
      if (isDevelopment) {
        console.log('🔧 개발 모드: 기존 StarcUp.Core 프로세스에 연결 시도...')
      } else {
        console.log('🚀 프로덕션 모드: StarcUp.Core 프로세스 시작...')
      }
      
      await coreService.startConnection(isDevelopment)
      
      console.log('✅ StarcUp.Core 초기화 완료')
    } catch (error) {
      console.error('❌ Core 프로세스 연결 실패:', error)
      // Core 연결 실패는 치명적이지 않으므로 계속 진행
    }

    console.log('🎉 애플리케이션 초기화 완료')

  } catch (error) {
    console.error('❌ 애플리케이션 초기화 실패:', error)
    
    // 초기화 실패 시 정리
    await serviceContainer.dispose()
    app.quit()
  }
}

// 개발 모드에서 핫 리로드 지원
if (process.env.NODE_ENV === 'development') {
  // 개발 모드 전용 기능들
  console.log('🔧 개발 모드 활성화')
}