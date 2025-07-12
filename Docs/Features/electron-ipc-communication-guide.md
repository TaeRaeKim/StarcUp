# Electron 윈도우 간 통신 및 외부 프로세스 연동 가이드

## 개요

Electron 애플리케이션에서 다중 윈도우 간 통신과 .NET DLL과 같은 외부 프로세스와의 연동을 위한 최적화된 방법들을 다룹니다. IPC와 ElectronCGI의 차이점을 이해하고, 메인 프레임과 오버레이 윈도우 구성까지 포괄적으로 설명합니다.

## 1. 통신 방법 비교 분석

### IPC vs CGI vs ElectronCGI

**전통적인 CGI**
- 웹서버를 통한 HTTP 요청/응답 기반 통신
- 높은 오버헤드와 보안 위험성
- Electron 환경에는 부적합

**IPC (Inter-Process Communication)**
- 프로세스 간 직접 통신
- 낮은 지연시간과 높은 보안성
- Electron 기본 제공 방식

**ElectronCGI**
- stdin/stdout 파이프를 통한 IPC의 구현체
- 실제로는 IPC 통신의 한 형태
- 외부 프로세스(.NET DLL 등)와의 통신에 최적화

### 성능 및 특성 비교

| 방식 | 지연시간 | 보안성 | 구현 복잡도 | 적용 범위 |
|------|----------|--------|-------------|-----------|
| Electron IPC | 1-2ms | 높음 | 낮음 | 내부 윈도우 간 |
| Named Pipes | 1-2ms | 높음 | 중간 | 로컬 프로세스 간 |
| ElectronCGI | 2-5ms | 높음 | 낮음 | 외부 프로세스 |
| TCP Sockets | 5-10ms | 중간 | 중간 | 크로스 플랫폼 |
| 웹 CGI | 50-100ms | 낮음 | 높음 | 권장하지 않음 |

## 2. Electron 프로세스 아키텍처

### 기본 프로세스 구조

```javascript
// main.js - Main Process
const { BrowserWindow, app } = require('electron');

// 각 BrowserWindow는 별도의 Renderer Process 생성
const mainWindow = new BrowserWindow({ ... });    // Renderer Process 1
const subWindow = new BrowserWindow({ ... });     // Renderer Process 2
```

### 프로세스 확인 방법

```javascript
// 각 윈도우에서 실행하여 PID 확인
console.log('Process ID:', process.pid);
console.log('Window Type:', process.type); // 'renderer' 또는 'browser'
```

### 같은 프로세스로 윈도우 생성

```javascript
const mainWindow = new BrowserWindow({
  webPreferences: {
    nodeIntegration: true,
    contextIsolation: false
  }
});

const subWindow = new BrowserWindow({
  webPreferences: {
    nodeIntegration: true,
    contextIsolation: false,
    additionalArguments: ['--renderer-process-limit=1']
  },
  parent: mainWindow  // 부모 관계 설정
});
```

## 3. 윈도우 간 통신 방법

### Electron 기본 IPC (권장)

```javascript
// Main Process
const { ipcMain } = require('electron');

ipcMain.on('window-a-to-b', (event, data) => {
  // B 윈도우로 데이터 전달
  subWindow.webContents.send('data-from-a', data);
});

// Renderer Process (Window A)
const { ipcRenderer } = require('electron');
ipcRenderer.send('window-a-to-b', messageData);

// Renderer Process (Window B)
ipcRenderer.on('data-from-a', (event, data) => {
  console.log('Received:', data);
});
```

### ElectronCGI를 이용한 윈도우 간 통신

```javascript
// Window A에서 ElectronCGI 객체 관리
class WindowCommunicationManager {
  constructor() {
    // 1. B 윈도우와의 통신용
    this.windowConnection = new ConnectionBuilder()
      .connectTo('node', 'window-b-handler.js')
      .build();
    
    // 2. .NET DLL과의 통신용  
    this.dllConnection = new ConnectionBuilder()
      .connectTo('dotnet', 'MyMemoryReader.dll')
      .build();
  }
  
  async forwardDataToWindowB(data) {
    return await this.windowConnection.send('update-display', data);
  }
  
  async readMemoryFromDLL(params) {
    return await this.dllConnection.send('read-memory', params);
  }
}
```

## 4. .NET DLL과의 통신

### ElectronCGI 기반 통신 (권장)

```javascript
// Electron 측
const { ConnectionBuilder } = require('electron-cgi');

const dllConnection = new ConnectionBuilder()
  .connectTo('dotnet', 'MyMemoryReader.dll')
  .build();

// 메모리 읽기 요청
const result = await dllConnection.send('read-memory', {
  address: 0x1000000,
  size: 4
});
```

```csharp
// C# DLL 측
using ElectronCgi.DotNet;

[ElectronCgiMethod]
public ReadMemoryResponse ReadMemory(ReadMemoryRequest request)
{
    byte[] buffer = new byte[request.Size];
    IntPtr bytesRead;
    
    bool success = ReadProcessMemory(processHandle, 
        new IntPtr(request.Address), buffer, request.Size, out bytesRead);

    return new ReadMemoryResponse
    {
        Success = success,
        Data = success ? buffer : null,
        BytesRead = (int)bytesRead
    };
}
```

### Named Pipes를 이용한 고성능 통신

```javascript
// Node.js 측 Named Pipe 클라이언트
const net = require('net');
const client = net.createConnection('\\\\.\\pipe\\myapp');

client.on('data', (data) => {
  const response = JSON.parse(data.toString());
  console.log('Received:', response);
});

client.write(JSON.stringify({
  action: 'read-memory',
  address: 0x1000000,
  size: 4
}));
```

```csharp
// C# Named Pipe 서버
using System.IO.Pipes;

class MemoryReaderPipeServer
{
    public async Task StartServer()
    {
        using (var server = new NamedPipeServerStream("myapp"))
        {
            await server.WaitForConnectionAsync();
            
            var reader = new StreamReader(server);
            var writer = new StreamWriter(server);
            
            string request = await reader.ReadLineAsync();
            var requestObj = JsonSerializer.Deserialize<MemoryRequest>(request);
            
            var response = ProcessMemoryRequest(requestObj);
            await writer.WriteLineAsync(JsonSerializer.Serialize(response));
            await writer.FlushAsync();
        }
    }
}
```

## 5. 메인 프레임 + 오버레이 윈도우 구성

### 기본 윈도우 설정

```javascript
// 메인 애플리케이션 윈도우
const mainWindow = new BrowserWindow({
  width: 1200,
  height: 800,
  webPreferences: {
    nodeIntegration: true,
    contextIsolation: false
  }
});

// 오버레이 윈도우 (게임 오버레이, HUD 등)
const overlayWindow = new BrowserWindow({
  width: 400,
  height: 300,
  frame: false,           // 프레임 없음
  transparent: true,      // 투명 배경
  alwaysOnTop: true,      // 항상 위
  skipTaskbar: true,      // 작업표시줄에 숨김
  resizable: false,       // 크기 조정 불가
  webPreferences: {
    nodeIntegration: true,
    contextIsolation: false
  }
});
```

### 오버레이 고급 기능

```javascript
// 마우스 이벤트 통과 설정
overlayWindow.setIgnoreMouseEvents(true);

// 필요시 클릭 활성화
function toggleOverlayInteraction() {
  const isIgnoring = overlayWindow.isIgnoreMouseEvents();
  overlayWindow.setIgnoreMouseEvents(!isIgnoring);
}

// 전역 단축키로 오버레이 토글
const { globalShortcut } = require('electron');

globalShortcut.register('F1', () => {
  if (overlayWindow.isVisible()) {
    overlayWindow.hide();
  } else {
    overlayWindow.show();
    overlayWindow.focus();
  }
});

// 메인 윈도우 위치에 따른 오버레이 자동 조정
mainWindow.on('move', () => {
  const [x, y] = mainWindow.getPosition();
  overlayWindow.setPosition(x + 50, y + 50);
});
```

### 같은 프로세스에서 데이터 공유

```javascript
// 전역 데이터 저장소
global.sharedData = {
  memoryData: null,
  gameState: {},
  overlaySettings: {
    visible: true,
    opacity: 0.8
  }
};

// 메인 윈도우에서 데이터 업데이트
async function updateMemoryData() {
  const memoryData = await dllConnection.send('read-memory', params);
  global.sharedData.memoryData = memoryData;
  
  // 오버레이 윈도우에 실시간 업데이트 알림
  overlayWindow.webContents.send('memory-updated', memoryData);
}

// 오버레이 윈도우에서 데이터 접근
const { remote } = require('electron');
const sharedData = remote.getGlobal('sharedData');

// 실시간 업데이트 수신
const { ipcRenderer } = require('electron');
ipcRenderer.on('memory-updated', (event, data) => {
  updateOverlayDisplay(data);
});
```

## 6. 실전 구현 예제

### 완전한 메모리 리더 애플리케이션

```javascript
// main.js
const { app, BrowserWindow, globalShortcut } = require('electron');
const { ConnectionBuilder } = require('electron-cgi');

class MemoryReaderApp {
  constructor() {
    this.mainWindow = null;
    this.overlayWindow = null;
    this.dllConnection = null;
  }

  async initialize() {
    // 메인 윈도우 생성
    this.mainWindow = new BrowserWindow({
      width: 1200,
      height: 800,
      webPreferences: {
        nodeIntegration: true,
        contextIsolation: false
      }
    });

    // 오버레이 윈도우 생성
    this.overlayWindow = new BrowserWindow({
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

    // .NET DLL 연결
    this.dllConnection = new ConnectionBuilder()
      .connectTo('dotnet', 'MemoryReader.dll')
      .build();

    // 전역 단축키 등록
    this.setupGlobalShortcuts();
    
    // 실시간 메모리 모니터링 시작
    this.startMemoryMonitoring();
  }

  setupGlobalShortcuts() {
    globalShortcut.register('F1', () => {
      this.toggleOverlay();
    });
  }

  toggleOverlay() {
    if (this.overlayWindow.isVisible()) {
      this.overlayWindow.hide();
    } else {
      this.overlayWindow.show();
    }
  }

  async startMemoryMonitoring() {
    setInterval(async () => {
      try {
        const memoryData = await this.dllConnection.send('scan-memory', {
          processName: 'target_game.exe',
          pattern: [0x90, 0x90, 0x90]
        });

        // 메인 윈도우에 데이터 전송
        this.mainWindow.webContents.send('memory-data', memoryData);
        
        // 오버레이에 요약 정보 전송
        this.overlayWindow.webContents.send('overlay-update', {
          health: memoryData.playerHealth,
          mana: memoryData.playerMana,
          position: memoryData.playerPosition
        });
      } catch (error) {
        console.error('Memory reading failed:', error);
      }
    }, 100); // 100ms 간격으로 업데이트
  }
}

app.whenReady().then(() => {
  const memoryApp = new MemoryReaderApp();
  memoryApp.initialize();
});
```

## 7. 성능 최적화 권장사항

### 통신 최적화

1. **빈번한 데이터 교환**: Electron IPC 사용
2. **외부 프로세스 통신**: ElectronCGI 또는 Named Pipes
3. **실시간 스트리밍**: WebSocket 또는 Named Pipes
4. **대용량 데이터**: 파일 기반 공유 또는 메모리 맵핑

### 메모리 사용량 최적화

```javascript
// 데이터 풀링으로 GC 압박 감소
class DataPool {
  constructor(size = 1000) {
    this.pool = new Array(size).fill(null).map(() => ({}));
    this.index = 0;
  }

  getObject() {
    const obj = this.pool[this.index];
    this.index = (this.index + 1) % this.pool.length;
    return obj;
  }
}

// 버퍼 재사용
const bufferPool = new DataPool(100);
```

## 8. 보안 고려사항

### 프로세스 분리의 장점

- **격리**: 한 윈도우 크래시가 다른 윈도우에 영향 없음
- **보안**: 각 프로세스별 권한 제어 가능
- **안정성**: 메모리 오염 방지

### 같은 프로세스 사용 시 주의점

- **전역 변수 오염**: 네임스페이스 충돌 방지 필요
- **메모리 누수**: 한 윈도우의 누수가 전체에 영향
- **에러 전파**: 예외 처리 철저히 필요

## 결론

Electron에서 다중 윈도우와 외부 프로세스 통신을 위한 최적의 접근법은 다음과 같습니다:

1. **윈도우 간 통신**: Electron 기본 IPC 우선 사용
2. **외부 프로세스**: ElectronCGI 또는 Named Pipes 활용
3. **오버레이 구성**: 투명 윈도우와 전역 단축키 조합
4. **성능 중시**: 프로세스 분리보다 같은 프로세스 공유 고려
5. **실시간 요구사항**: 100ms 이하 업데이트 주기로 최적화

이러한 방법들을 통해 메모리 리더, 게임 오버레이, 실시간 모니터링 등 다양한 애플리케이션을 효율적으로 구현할 수 있습니다.