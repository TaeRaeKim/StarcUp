# Electron 로컬 애플리케이션을 위한 단일 프로세스 최적화 가이드

## 개요

HTML/CSS/JavaScript로 프론트엔드를 구현하고 외부 네트워크 통신이 없는 Electron 애플리케이션에서는 단일 프로세스 사용이 더 효율적이고 실용적입니다. 이 가이드는 보안 위험을 최소화하면서도 개발 편의성과 성능을 극대화하는 방법을 제시합니다.

## 1. 단일 프로세스 vs 다중 프로세스 결정 기준

### 단일 프로세스가 적합한 경우

```javascript
// ✅ 이런 애플리케이션에 최적
const app = {
  contentSource: 'local',        // 로컬 HTML/CSS/JS만 사용
  networkCommunication: false,   // 외부 네트워크 통신 없음
  userInput: 'trusted',          // 신뢰할 수 있는 사용자 입력만
  externalProcess: true          // .NET DLL 등 외부 프로세스와만 통신
};
```

**적합한 애플리케이션 유형:**
- 게임 오버레이 및 모니터링 도구
- 시스템 리소스 모니터
- 로컬 파일 관리 도구
- 메모리 에디터 및 디버깅 도구
- 데스크톱 위젯 및 HUD

### 다중 프로세스가 필요한 경우

```javascript
// ❌ 이런 경우에는 프로세스 분리 필수
const riskyApp = {
  contentSource: 'web',          // 외부 웹 콘텐츠 로드
  networkCommunication: true,    // 인터넷 통신
  userInput: 'untrusted',        // 사용자가 임의 스크립트 입력
  thirdPartyContent: true        // 서드파티 콘텐츠 렌더링
};
```

## 2. Electron 보안 설정 분석

### 주요 보안 옵션들의 의미

#### `nodeIntegration` 설정

```javascript
// 기본값: false (보안 우선)
nodeIntegration: true   // Renderer에서 Node.js API 직접 사용
```

**영향 분석:**
- **허용 시**: `require()`, `fs`, `path` 등 Node.js 모듈 직접 사용 가능
- **보안 위험**: 웹 콘텐츠에서 시스템 파일 접근 가능
- **로컬 앱에서**: 외부 스크립트 없으면 위험도 낮음

#### `contextIsolation` 설정

```javascript
// 기본값: true (격리 유지)
contextIsolation: false  // Main world와 Isolated world 통합
```

**영향 분석:**
- **비활성화 시**: 웹 페이지와 Electron API가 같은 컨텍스트 공유
- **장점**: 직접적인 API 접근으로 개발 편의성 증대
- **위험**: 외부 스크립트가 Electron API 조작 가능

#### `additionalArguments` 설정

```javascript
additionalArguments: ['--renderer-process-limit=1']
```

**영향 분석:**
- **효과**: 모든 BrowserWindow가 단일 Renderer 프로세스 공유
- **장점**: 메모리 절약, 직접 변수 공유 가능
- **단점**: 한 윈도우 크래시 시 모든 윈도우 영향

### 위험도 평가 매트릭스

| 설정 조합 | 외부 콘텐츠 | 로컬 콘텐츠 | 추천도 |
|-----------|-------------|-------------|--------|
| nodeIntegration: false, contextIsolation: true | ✅ 안전 | ✅ 안전 | 높음 |
| nodeIntegration: true, contextIsolation: true | ⚠️ 위험 | ✅ 안전 | 중간 |
| nodeIntegration: true, contextIsolation: false | ❌ 매우 위험 | ✅ 안전 | 로컬 전용 |

## 3. 로컬 애플리케이션 최적화 전략

### 권장 설정 (로컬 콘텐츠 전용)

```javascript
// 로컬 애플리케이션을 위한 최적화된 설정
const optimizedConfig = {
  webPreferences: {
    nodeIntegration: true,                    // 개발 편의성
    contextIsolation: false,                  // 직접 API 접근
    enableRemoteModule: false,                // 성능 향상
    additionalArguments: ['--renderer-process-limit=1'], // 메모리 효율
    webSecurity: false                        // 로컬 파일 간 제약 해제
  }
};
```

### 개발 환경별 설정 분리

```javascript
const isDev = process.env.NODE_ENV === 'development';
const isProd = process.env.NODE_ENV === 'production';

const createWindow = () => {
  const baseConfig = {
    width: 1200,
    height: 800,
    webPreferences: {
      nodeIntegration: true,
      contextIsolation: false,
      additionalArguments: ['--renderer-process-limit=1']
    }
  };

  // 개발 환경 추가 설정
  if (isDev) {
    baseConfig.webPreferences.devTools = true;
    baseConfig.webPreferences.allowRunningInsecureContent = true;
  }

  // 프로덕션 환경 최적화
  if (isProd) {
    baseConfig.webPreferences.devTools = false;
    baseConfig.show = false; // 초기화 완료 후 표시
  }

  return new BrowserWindow(baseConfig);
};
```

## 4. 단일 프로세스에서 윈도우 간 통신

### 직접 변수 공유 방식

```javascript
// main.js - 전역 데이터 관리
class GlobalDataManager {
  constructor() {
    // 전역 상태 저장소
    global.appState = {
      memoryData: null,
      gameState: {},
      overlaySettings: {
        visible: true,
        opacity: 0.8,
        position: { x: 100, y: 100 }
      },
      systemInfo: {}
    };
  }

  updateMemoryData(data) {
    global.appState.memoryData = data;
    this.notifyAllWindows('memory-updated', data);
  }

  notifyAllWindows(event, data) {
    // 모든 윈도우에 즉시 알림
    BrowserWindow.getAllWindows().forEach(window => {
      window.webContents.executeJavaScript(`
        if (window.handleGlobalEvent) {
          window.handleGlobalEvent('${event}', ${JSON.stringify(data)});
        }
      `);
    });
  }
}
```

### 윈도우별 역할 분담

```javascript
// 메인 애플리케이션 윈도우
class MainWindow {
  constructor() {
    this.window = new BrowserWindow({
      width: 1200,
      height: 800,
      webPreferences: {
        nodeIntegration: true,
        contextIsolation: false
      }
    });

    this.window.loadFile('main.html');
    this.setupDLLCommunication();
  }

  async setupDLLCommunication() {
    const { ConnectionBuilder } = require('electron-cgi');
    
    this.dllConnection = new ConnectionBuilder()
      .connectTo('dotnet', 'MemoryReader.dll')
      .build();

    // 정기적 데이터 수집
    this.startDataCollection();
  }

  startDataCollection() {
    setInterval(async () => {
      try {
        const memoryData = await this.dllConnection.send('scan-memory', {
          processName: 'target_process.exe',
          addresses: [0x1000000, 0x2000000]
        });

        // 전역 상태 업데이트
        global.appState.memoryData = memoryData;
        
        // 오버레이 윈도우에 실시간 알림
        this.notifyOverlay(memoryData);
        
      } catch (error) {
        console.error('Memory reading failed:', error);
      }
    }, 100); // 100ms 간격
  }

  notifyOverlay(data) {
    const overlayWindow = global.overlayWindow;
    if (overlayWindow && !overlayWindow.isDestroyed()) {
      overlayWindow.webContents.executeJavaScript(`
        if (window.updateMemoryDisplay) {
          window.updateMemoryDisplay(${JSON.stringify(data)});
        }
      `);
    }
  }
}

// 오버레이 윈도우
class OverlayWindow {
  constructor() {
    this.window = new BrowserWindow({
      width: 300,
      height: 200,
      frame: false,
      transparent: true,
      alwaysOnTop: true,
      skipTaskbar: true,
      webPreferences: {
        nodeIntegration: true,
        contextIsolation: false
      }
    });

    global.overlayWindow = this.window; // 전역 참조 저장
    this.window.loadFile('overlay.html');
    this.setupGlobalShortcuts();
  }

  setupGlobalShortcuts() {
    const { globalShortcut } = require('electron');
    
    globalShortcut.register('F1', () => {
      this.toggle();
    });

    globalShortcut.register('F2', () => {
      this.cycleOpacity();
    });
  }

  toggle() {
    if (this.window.isVisible()) {
      this.window.hide();
    } else {
      this.window.show();
      this.window.focus();
    }
  }

  cycleOpacity() {
    const current = global.appState.overlaySettings.opacity;
    const next = current >= 1.0 ? 0.3 : current + 0.1;
    
    global.appState.overlaySettings.opacity = next;
    this.window.setOpacity(next);
  }
}
```

## 5. 실제 구현 예제

### 완전한 메모리 모니터링 애플리케이션

```javascript
// main.js
const { app, BrowserWindow, globalShortcut } = require('electron');
const { ConnectionBuilder } = require('electron-cgi');

class MemoryMonitorApp {
  constructor() {
    this.mainWindow = null;
    this.overlayWindow = null;
    this.settingsWindow = null;
    this.dllConnection = null;
    
    this.initializeGlobalState();
  }

  initializeGlobalState() {
    global.appState = {
      monitoring: false,
      targetProcess: null,
      memoryData: {
        health: 0,
        mana: 0,
        experience: 0,
        position: { x: 0, y: 0, z: 0 }
      },
      settings: {
        updateInterval: 100,
        overlayVisible: true,
        overlayOpacity: 0.8,
        monitoringAddresses: {
          health: '0x1000000',
          mana: '0x1000004',
          experience: '0x1000008'
        }
      }
    };
  }

  async initialize() {
    await this.createWindows();
    await this.setupDLLConnection();
    this.setupGlobalShortcuts();
    this.startMonitoring();
  }

  async createWindows() {
    // 메인 컨트롤 윈도우
    this.mainWindow = new BrowserWindow({
      width: 800,
      height: 600,
      webPreferences: {
        nodeIntegration: true,
        contextIsolation: false
      }
    });

    // 오버레이 윈도우
    this.overlayWindow = new BrowserWindow({
      width: 250,
      height: 150,
      frame: false,
      transparent: true,
      alwaysOnTop: true,
      skipTaskbar: true,
      resizable: false,
      webPreferences: {
        nodeIntegration: true,
        contextIsolation: false
      }
    });

    // 설정 윈도우 (숨김 상태로 생성)
    this.settingsWindow = new BrowserWindow({
      width: 400,
      height: 500,
      show: false,
      parent: this.mainWindow,
      modal: true,
      webPreferences: {
        nodeIntegration: true,
        contextIsolation: false
      }
    });

    // HTML 로드
    this.mainWindow.loadFile('pages/main.html');
    this.overlayWindow.loadFile('pages/overlay.html');
    this.settingsWindow.loadFile('pages/settings.html');

    // 전역 참조 저장
    global.windows = {
      main: this.mainWindow,
      overlay: this.overlayWindow,
      settings: this.settingsWindow
    };
  }

  async setupDLLConnection() {
    try {
      this.dllConnection = new ConnectionBuilder()
        .connectTo('dotnet', 'MemoryReader.dll')
        .build();

      console.log('DLL connection established');
    } catch (error) {
      console.error('Failed to connect to DLL:', error);
    }
  }

  setupGlobalShortcuts() {
    // 오버레이 토글
    globalShortcut.register('F1', () => {
      this.toggleOverlay();
    });

    // 모니터링 시작/중지
    globalShortcut.register('F2', () => {
      this.toggleMonitoring();
    });

    // 설정 창 열기
    globalShortcut.register('F3', () => {
      this.openSettings();
    });
  }

  toggleOverlay() {
    const isVisible = this.overlayWindow.isVisible();
    if (isVisible) {
      this.overlayWindow.hide();
    } else {
      this.overlayWindow.show();
    }
    
    global.appState.settings.overlayVisible = !isVisible;
    this.notifyAllWindows('overlay-toggled', !isVisible);
  }

  toggleMonitoring() {
    global.appState.monitoring = !global.appState.monitoring;
    this.notifyAllWindows('monitoring-toggled', global.appState.monitoring);
  }

  openSettings() {
    this.settingsWindow.show();
    this.settingsWindow.focus();
  }

  startMonitoring() {
    setInterval(async () => {
      if (!global.appState.monitoring || !this.dllConnection) {
        return;
      }

      try {
        const addresses = global.appState.settings.monitoringAddresses;
        
        const memoryData = await this.dllConnection.send('read-multiple-addresses', {
          processName: global.appState.targetProcess,
          addresses: [
            { name: 'health', address: addresses.health, type: 'int32' },
            { name: 'mana', address: addresses.mana, type: 'int32' },
            { name: 'experience', address: addresses.experience, type: 'int64' }
          ]
        });

        // 전역 상태 업데이트
        global.appState.memoryData = {
          ...global.appState.memoryData,
          ...memoryData
        };

        // 모든 윈도우에 알림
        this.notifyAllWindows('memory-updated', memoryData);

      } catch (error) {
        console.error('Memory reading error:', error);
      }
    }, global.appState.settings.updateInterval);
  }

  notifyAllWindows(event, data) {
    Object.values(global.windows).forEach(window => {
      if (window && !window.isDestroyed()) {
        window.webContents.executeJavaScript(`
          if (window.handleAppEvent) {
            window.handleAppEvent('${event}', ${JSON.stringify(data)});
          }
        `).catch(err => {
          // 윈도우가 아직 로드되지 않은 경우 무시
        });
      }
    });
  }
}

// 앱 시작
app.whenReady().then(() => {
  const memoryApp = new MemoryMonitorApp();
  memoryApp.initialize();
});

app.on('before-quit', () => {
  globalShortcut.unregisterAll();
});
```

### 오버레이 윈도우 구현

```html
<!-- pages/overlay.html -->
<!DOCTYPE html>
<html>
<head>
  <meta charset="UTF-8">
  <style>
    body {
      margin: 0;
      padding: 10px;
      background: rgba(0, 0, 0, 0.7);
      color: #00ff00;
      font-family: 'Courier New', monospace;
      font-size: 12px;
      border: 1px solid rgba(0, 255, 0, 0.3);
      border-radius: 5px;
      user-select: none;
    }
    
    .data-row {
      display: flex;
      justify-content: space-between;
      margin: 2px 0;
      padding: 2px 5px;
      background: rgba(0, 0, 0, 0.3);
      border-radius: 3px;
    }
    
    .label {
      color: #ffffff;
    }
    
    .value {
      color: #00ff00;
      font-weight: bold;
    }
    
    .status {
      text-align: center;
      margin-top: 5px;
      font-size: 10px;
      color: #ffff00;
    }
  </style>
</head>
<body>
  <div id="memory-display">
    <div class="data-row">
      <span class="label">Health:</span>
      <span class="value" id="health">0</span>
    </div>
    <div class="data-row">
      <span class="label">Mana:</span>
      <span class="value" id="mana">0</span>
    </div>
    <div class="data-row">
      <span class="label">EXP:</span>
      <span class="value" id="experience">0</span>
    </div>
    <div class="status" id="status">Waiting...</div>
  </div>

  <script>
    const { remote } = require('electron');
    
    // 전역 이벤트 핸들러
    window.handleAppEvent = (event, data) => {
      switch (event) {
        case 'memory-updated':
          updateMemoryDisplay(data);
          break;
        case 'monitoring-toggled':
          updateStatus(data ? 'Monitoring...' : 'Stopped');
          break;
      }
    };
    
    function updateMemoryDisplay(data) {
      document.getElementById('health').textContent = data.health || 0;
      document.getElementById('mana').textContent = data.mana || 0;
      document.getElementById('experience').textContent = data.experience || 0;
      updateStatus('Active');
    }
    
    function updateStatus(status) {
      document.getElementById('status').textContent = status;
    }
    
    // 초기 데이터 로드
    const appState = remote.getGlobal('appState');
    if (appState && appState.memoryData) {
      updateMemoryDisplay(appState.memoryData);
    }
    
    // 오버레이 클릭 시 투명도 조절
    document.body.addEventListener('dblclick', () => {
      const currentOpacity = remote.getCurrentWindow().getOpacity();
      const newOpacity = currentOpacity > 0.5 ? 0.3 : 1.0;
      remote.getCurrentWindow().setOpacity(newOpacity);
    });
  </script>
</body>
</html>
```

## 6. 성능 최적화 기법

### 메모리 사용량 최적화

```javascript
// 객체 풀링으로 GC 압박 감소
class DataPool {
  constructor(size = 100) {
    this.pool = [];
    this.index = 0;
    
    // 미리 객체 생성
    for (let i = 0; i < size; i++) {
      this.pool.push({
        health: 0,
        mana: 0,
        experience: 0,
        timestamp: 0
      });
    }
  }
  
  getObject() {
    const obj = this.pool[this.index];
    this.index = (this.index + 1) % this.pool.length;
    return obj;
  }
}

const memoryDataPool = new DataPool(50);

// 사용 예
function createMemoryData(health, mana, exp) {
  const obj = memoryDataPool.getObject();
  obj.health = health;
  obj.mana = mana;
  obj.experience = exp;
  obj.timestamp = Date.now();
  return obj;
}
```

### 업데이트 주기 최적화

```javascript
class AdaptiveUpdateManager {
  constructor() {
    this.baseInterval = 100;
    this.currentInterval = 100;
    this.lastUpdateTime = 0;
    this.changeThreshold = 0.01; // 1% 변화량
  }
  
  shouldUpdate(newData, oldData) {
    if (!oldData) return true;
    
    // 변화량 계산
    const healthChange = Math.abs(newData.health - oldData.health) / oldData.health;
    const manaChange = Math.abs(newData.mana - oldData.mana) / oldData.mana;
    
    return healthChange > this.changeThreshold || manaChange > this.changeThreshold;
  }
  
  adjustInterval(hasChanged) {
    if (hasChanged) {
      // 변화가 있으면 빠르게 업데이트
      this.currentInterval = Math.max(50, this.currentInterval - 10);
    } else {
      // 변화가 없으면 느리게 업데이트
      this.currentInterval = Math.min(500, this.currentInterval + 5);
    }
  }
}
```

## 7. 디버깅 및 개발 도구

### 개발 환경 설정

```javascript
// 개발 모드에서 추가 기능
if (process.env.NODE_ENV === 'development') {
  // 핫 리로드 설정
  require('electron-reload')(__dirname, {
    electron: path.join(__dirname, '..', 'node_modules', '.bin', 'electron'),
    hardResetMethod: 'exit'
  });
  
  // 개발자 도구 자동 열기
  mainWindow.webContents.openDevTools();
  
  // 글로벌 디버그 함수
  global.debug = {
    getAppState: () => global.appState,
    setMemoryData: (data) => {
      global.appState.memoryData = data;
      notifyAllWindows('memory-updated', data);
    },
    simulateMemoryChange: () => {
      const fakeData = {
        health: Math.floor(Math.random() * 100),
        mana: Math.floor(Math.random() * 100),
        experience: Math.floor(Math.random() * 10000)
      };
      global.debug.setMemoryData(fakeData);
    }
  };
}
```

### 로깅 시스템

```javascript
class Logger {
  constructor() {
    this.logs = [];
    this.maxLogs = 1000;
  }
  
  log(level, message, data = null) {
    const logEntry = {
      timestamp: new Date().toISOString(),
      level,
      message,
      data: data ? JSON.stringify(data) : null
    };
    
    this.logs.push(logEntry);
    
    // 로그 크기 제한
    if (this.logs.length > this.maxLogs) {
      this.logs.shift();
    }
    
    // 콘솔 출력
    console.log(`[${level}] ${message}`, data || '');
    
    // 메인 윈도우에 로그 전송
    if (global.windows && global.windows.main) {
      global.windows.main.webContents.executeJavaScript(`
        if (window.addLogEntry) {
          window.addLogEntry(${JSON.stringify(logEntry)});
        }
      `);
    }
  }
  
  error(message, data) { this.log('ERROR', message, data); }
  warn(message, data) { this.log('WARN', message, data); }
  info(message, data) { this.log('INFO', message, data); }
  debug(message, data) { this.log('DEBUG', message, data); }
}

global.logger = new Logger();
```

## 8. 배포 최적화

### Electron Builder 설정

```json
{
  "build": {
    "appId": "com.yourcompany.memorymonitor",
    "productName": "Memory Monitor",
    "directories": {
      "output": "dist"
    },
    "files": [
      "main.js",
      "pages/**/*",
      "assets/**/*",
      "node_modules/**/*"
    ],
    "extraResources": [
      {
        "from": "backend/bin/Release/net6.0/win-x64/publish",
        "to": "backend"
      }
    ],
    "win": {
      "target": "nsis",
      "icon": "assets/icon.ico"
    },
    "nsis": {
      "oneClick": false,
      "allowToChangeInstallationDirectory": true,
      "createDesktopShortcut": true,
      "createStartMenuShortcut": true
    }
  }
}
```

### 리소스 최적화

```javascript
// 프로덕션 빌드에서 불필요한 모듈 제거
const isDev = process.env.NODE_ENV === 'development';

if (!isDev) {
  // 개발 도구 비활성화
  const originalConsole = console;
  console = {
    log: () => {},
    error: originalConsole.error,
    warn: originalConsole.warn,
    info: () => {},
    debug: () => {}
  };
}

// 메모리 사용량 모니터링
function monitorMemoryUsage() {
  setInterval(() => {
    const usage = process.memoryUsage();
    if (usage.heapUsed > 100 * 1024 * 1024) { // 100MB 초과 시
      global.logger.warn('High memory usage detected', usage);
      
      // 강제 GC (개발 모드에서만)
      if (isDev && global.gc) {
        global.gc();
      }
    }
  }, 30000); // 30초마다 체크
}
```

## 결론

로컬 HTML/CSS/JavaScript 기반 Electron 애플리케이션에서는 단일 프로세스 사용이 다음과 같은 이점을 제공합니다:

### 주요 장점
- **성능 향상**: 프로세스 간 통신 오버헤드 제거
- **메모리 효율성**: 단일 힙에서 모든 데이터 관리
- **개발 편의성**: 직접적인 변수 공유 및 함수 호출
- **단순한 아키텍처**: 복잡한 IPC 로직 불필요

### 권장 사용 사례
- 게임 오버레이 및 모니터링 도구
- 시스템 리소스 모니터
- 메모리 에디터 및 디버깅 도구
- 로컬 파일 관리 도구
- 데스크톱 위젯 및 HUD

### 핵심 설정
```javascript
const optimizedConfig = {
  webPreferences: {
    nodeIntegration: true,
    contextIsolation: false,
    additionalArguments: ['--renderer-process-limit=1']
  }
};
```

외부 네트워크 통신이 없는 신뢰할 수 있는 로컬 애플리케이션에서는 보안보다 성능과 개발 편의성을 우선시하는 것이 합리적입니다.