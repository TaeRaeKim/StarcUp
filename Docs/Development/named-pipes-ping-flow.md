# Named Pipes Ping 명령 플로우 문서

## 개요

이 문서는 StarcUp.UI에서 `ping` 명령을 전송했을 때, Named Pipes를 통해 StarcUp.Core와 통신하여 응답을 받는 전체 플로우를 상세히 설명합니다.

## 아키텍처 개요

```
┌─────────────────┐    Named Pipes    ┌─────────────────┐
│   StarcUp.UI    │ ◄────────────────► │  StarcUp.Core   │
│   (클라이언트)   │                    │    (서버)       │
└─────────────────┘                    └─────────────────┘
```

## 1. 초기 연결 설정

### 1.1 StarcUp.Core 서버 시작
```
StarcUp.Core.exe [pipeName]
```

**파일**: `StarcUp.Core/Program.cs`
```csharp
// Named Pipe 서비스 초기화 및 시작
_namedPipeService = _container.Resolve<INamedPipeService>();
await _namedPipeService.StartAsync(pipeName, _cancellationTokenSource.Token);
```

**파일**: `StarcUp.Core/Src/Infrastructure/Pipes/NamedPipeService.cs`
```csharp
// 서버 태스크 시작 - 클라이언트 연결 수락 대기
_serverTask = AcceptClientsAsync(_cancellationTokenSource.Token);
```

### 1.2 StarcUp.UI 클라이언트 연결
**파일**: `StarcUp.UI/electron/src/core-process-manager.ts`
```typescript
// Named Pipe 클라이언트 생성 및 연결
this.namedPipeClient = new NamedPipeClient({
  pipeName: this.pipeName,
  reconnectInterval: 3000,
  maxReconnectAttempts: 5,
  responseTimeout: 10000
})

await this.namedPipeClient.connect()
```

**파일**: `StarcUp.UI/electron/src/named-pipe-client.ts`
```typescript
// Windows Named Pipe 경로로 연결
const pipePath = `\\\\.\\pipe\\${this.pipeName}`
this.socket = createConnection(pipePath)
```

## 2. Ping 명령 전송 플로우

### 2.1 헬스 체크에서 Ping 명령 발생
**파일**: `StarcUp.UI/electron/src/core-process-manager.ts:startHealthCheck()`
```typescript
// 30초마다 헬스 체크 실행
this.healthCheckInterval = setInterval(async () => {
  if (Date.now() - this.lastHealthCheck > 30000) {
    const response = await this.sendCommand('ping')  // ← ping 명령 시작점
    
    if (response.success) {
      this.lastHealthCheck = Date.now()
      console.log('💓 StarcUp.Core 헬스 체크 성공')
    }
  }
}, 30000)
```

### 2.2 CoreProcessManager에서 명령 전송
**파일**: `StarcUp.UI/electron/src/core-process-manager.ts:sendCommand()`
```typescript
async sendCommand(command: string, args: string[] = []): Promise<CoreProcessResponse> {
  if (!this.isConnected || !this.namedPipeClient) {
    return { success: false, error: 'Named Pipe 클라이언트가 연결되지 않았습니다.' }
  }

  // Named Pipe 클라이언트로 명령 전달
  const response = await this.namedPipeClient.sendCommand(command, args)
  return {
    success: response.success,
    data: response.data,
    error: response.error
  }
}
```

### 2.3 NamedPipeClient에서 실제 전송
**파일**: `StarcUp.UI/electron/src/named-pipe-client.ts:sendCommand()`
```typescript
async sendCommand(command: string, args: string[] = []): Promise<NamedPipeResponse> {
  return new Promise<NamedPipeResponse>((resolve) => {
    const fullCommand = [command, ...args].join(' ')  // "ping"

    // 응답 콜백 등록
    this.responseQueue.push(resolve)

    // 타임아웃 설정 (10초)
    const timeoutId = setTimeout(() => {
      // 타임아웃 처리...
    }, this.responseTimeout)

    try {
      // 소켓을 통해 명령 전송
      this.socket!.write(fullCommand + '\n')  // "ping\n" 전송
      console.log(`📤 Named Pipe 명령 전송: ${fullCommand}`)
    } catch (error) {
      // 에러 처리...
    }
  })
}
```

## 3. StarcUp.Core에서 명령 수신 및 처리

### 3.1 NamedPipeService에서 명령 수신
**파일**: `StarcUp.Core/Src/Infrastructure/Pipes/NamedPipeService.cs`

#### AcceptClientsAsync에서 클라이언트 연결 수락
```csharp
// 클라이언트 연결 대기
await pipeServer.WaitForConnectionAsync(cancellationToken);

var clientId = Guid.NewGuid().ToString();
var client = new PipeClient(clientId, pipeServer, _commandHandler);

// 클라이언트 통신 시작
_ = Task.Run(async () => await client.StartAsync(cancellationToken), cancellationToken);
```

#### PipeClient에서 명령 리스닝
```csharp
private async Task ListenForCommandsAsync(CancellationToken cancellationToken)
{
  while (!cancellationToken.IsCancellationRequested && _pipeStream.IsConnected)
  {
    try
    {
      var command = await _reader.ReadLineAsync();  // "ping" 수신
      if (command == null) break;

      Console.WriteLine($"📥 클라이언트 {_clientId}로부터 명령 수신: {command}");

      // 명령 파싱
      var parts = command.Split(' ', StringSplitOptions.RemoveEmptyEntries);
      if (parts.Length > 0)
      {
        var commandName = parts[0];  // "ping"
        var arguments = parts.Length > 1 ? parts[1..] : Array.Empty<string>();
        var commandId = Guid.NewGuid().ToString();

        // 명령 처리 및 응답 전송
        await HandleCommandAsync(commandName, arguments, commandId);
      }
    }
    catch (Exception ex) {
      // 에러 처리...
    }
  }
}
```

### 3.2 CommandHandler에서 명령 처리
**파일**: `StarcUp.Core/Src/Infrastructure/Pipes/CommandHandler.cs:HandleCommandAsync()`
```csharp
public async Task<string> HandleCommandAsync(string command, string[] arguments, string commandId)
{
    Console.WriteLine($"🔧 명령 처리 중: {command} (ID: {commandId})");
    
    try
    {
        switch (command.ToLowerInvariant())
        {
            case "ping":
                return await HandlePingAsync();  // ← ping 명령 처리
                
            case "start-game-detect":
                return await HandleStartGameDetectAsync();
                
            // ... 기타 명령들
                
            default:
                return $"ERROR:알 수 없는 명령: {command}";
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ 명령 처리 중 오류: {ex.Message}");
        return $"ERROR:{ex.Message}";
    }
}
```

### 3.3 Ping 명령 구체적 처리
**파일**: `StarcUp.Core/Src/Infrastructure/Pipes/CommandHandler.cs:HandlePingAsync()`
```csharp
private async Task<string> HandlePingAsync()
{
    Console.WriteLine("🏓 Ping 요청 처리 중...");
    
    // 간단한 응답 반환 (실제로는 시스템 상태 체크 등을 할 수 있음)
    return "SUCCESS:pong";
}
```

## 4. 응답 전송 플로우

### 4.1 PipeClient에서 응답 전송
**파일**: `StarcUp.Core/Src/Infrastructure/Pipes/NamedPipeService.cs:PipeClient.HandleCommandAsync()`
```csharp
private async Task HandleCommandAsync(string command, string[] arguments, string commandId)
{
    try
    {
        // 명령 처리
        var result = await _commandHandler.HandleCommandAsync(command, arguments, commandId);
        // result = "SUCCESS:pong"

        // 응답 전송
        await SendResponseAsync(result);

        Console.WriteLine($"📤 클라이언트 {_clientId} 명령 처리 완료: {command} -> {result}");
    }
    catch (Exception ex) {
        // 에러 처리...
    }
}
```

### 4.2 실제 응답 데이터 전송
**파일**: `StarcUp.Core/Src/Infrastructure/Pipes/NamedPipeService.cs:PipeClient.SendResponseAsync()`
```csharp
public async Task SendResponseAsync(string response, CancellationToken cancellationToken = default)
{
    if (_disposed || !_pipeStream.IsConnected)
        return;

    try
    {
        await _writer.WriteLineAsync(response);  // "SUCCESS:pong\n" 전송
        Console.WriteLine($"📤 클라이언트 {_clientId}에게 응답 전송: {response}");
    }
    catch (Exception ex) {
        // 에러 처리...
    }
}
```

## 5. StarcUp.UI에서 응답 수신 및 처리

### 5.1 NamedPipeClient에서 데이터 수신
**파일**: `StarcUp.UI/electron/src/named-pipe-client.ts:setupDataListener()`
```typescript
private setupDataListener(): void {
  if (!this.socket) return

  this.socket.on('data', (chunk: Buffer) => {
    this.buffer += chunk.toString()
    
    // 줄 단위로 처리
    const lines = this.buffer.split('\n')
    this.buffer = lines.pop() || '' // 마지막 불완전한 줄은 버퍼에 보관

    for (const line of lines) {
      if (line.trim()) {
        this.handleResponse(line.trim())  // "SUCCESS:pong" 처리
      }
    }
  })
}
```

### 5.2 응답 파싱 및 Promise 해결
**파일**: `StarcUp.UI/electron/src/named-pipe-client.ts:handleResponse()`
```typescript
private handleResponse(response: string): void {
  console.log(`📥 Named Pipe 응답 수신: ${response}`)  // "SUCCESS:pong"

  try {
    // 응답 파싱 (SUCCESS:data 또는 ERROR:message 형식)
    const isSuccess = response.startsWith('SUCCESS:')
    const isError = response.startsWith('ERROR:')
    
    let result: NamedPipeResponse
    
    if (isSuccess) {
      const data = response.substring(8) // 'SUCCESS:' 제거 → "pong"
      result = { success: true, data }
    } else if (isError) {
      const error = response.substring(6) // 'ERROR:' 제거
      result = { success: false, error }
    } else {
      // 알 수 없는 응답 형식 - 원시 데이터로 처리
      result = { success: true, data: response }
    }

    // 대기 중인 첫 번째 명령의 응답 해결
    if (this.responseQueue.length > 0) {
      const resolver = this.responseQueue.shift()
      resolver!(result)  // Promise 해결
    }
  } catch (error) {
    // 에러 처리...
  }
}
```

### 5.3 헬스 체크에서 결과 확인
**파일**: `StarcUp.UI/electron/src/core-process-manager.ts:startHealthCheck()`
```typescript
const response = await this.sendCommand('ping')  // Promise가 해결됨
// response = { success: true, data: "pong" }

if (response.success) {
  this.lastHealthCheck = Date.now()
  console.log('💓 StarcUp.Core 헬스 체크 성공')
} else {
  console.warn('⚠️ StarcUp.Core 헬스 체크 실패')
  if (this.autoReconnect) {
    this.attemptReconnect()
  }
}
```

## 6. 전체 플로우 시퀀스 다이어그램

```
StarcUp.UI                    Named Pipe                    StarcUp.Core
    │                             │                             │
    │ ─────── sendCommand("ping") ─────────────────────────────► │
    │                             │                             │
    │                             │ ◄─── "ping\n" 전송 ────────── │
    │                             │                             │
    │                             │ ────► ListenForCommandsAsync │
    │                             │                             │
    │                             │                CommandHandler│
    │                             │                HandlePingAsync│
    │                             │                             │
    │                             │ ◄─── "SUCCESS:pong\n" ────── │
    │                             │                             │
    │ ◄─── handleResponse ─────────────────────────────────────── │
    │                             │                             │
    │ Promise 해결                 │                             │
    │ { success: true, data: "pong" }                           │
    │                             │                             │
    │ 헬스 체크 성공 로그           │                             │
    │                             │                             │
```

## 7. 에러 처리 및 재연결 메커니즘

### 7.1 타임아웃 처리
- **클라이언트**: 10초 타임아웃 (설정 가능)
- **타임아웃 시**: `{ success: false, error: 'Command timeout' }` 반환

### 7.2 연결 끊김 처리
- **자동 감지**: Named Pipe 연결 상태 모니터링
- **자동 재연결**: 최대 5회 시도 (3초 간격)
- **프로세스 재시작**: 필요시 StarcUp.Core 프로세스 재시작

### 7.3 로깅 및 모니터링
- **상세 로그**: 모든 단계에서 console.log 출력
- **에러 추적**: try-catch 블록을 통한 에러 캐치
- **상태 추적**: 연결 상태 및 헬스 체크 상태 모니터링

## 8. 성능 고려사항

### 8.1 비동기 처리
- **Promise 기반**: 모든 I/O 작업은 비동기
- **논블로킹**: UI 스레드 블로킹 방지

### 8.2 리소스 관리
- **적절한 정리**: Dispose 패턴으로 리소스 해제
- **메모리 관리**: 버퍼 및 큐 적절한 정리

### 8.3 확장성
- **다중 클라이언트**: 서버는 여러 클라이언트 동시 지원
- **명령 확장**: CommandHandler에 새 명령 쉽게 추가 가능

## 9. 디버깅 팁

### 9.1 로그 확인 포인트
1. **StarcUp.UI**: Electron 개발자 도구 콘솔
2. **StarcUp.Core**: 콘솔 애플리케이션 출력

### 9.2 일반적인 문제
- **파이프 이름 불일치**: 클라이언트와 서버 파이프 이름 확인
- **권한 문제**: Windows Named Pipe 권한 확인
- **프로세스 종료**: StarcUp.Core 프로세스 상태 확인
- **방화벽**: Windows 방화벽이 Named Pipe를 차단하지 않는지 확인

이 문서는 ping 명령의 전체 라이프사이클을 보여주며, 동일한 패턴이 다른 모든 명령에도 적용됩니다.