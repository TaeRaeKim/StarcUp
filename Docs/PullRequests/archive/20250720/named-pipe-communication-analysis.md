# Named Pipe 통신 분석 보고서

## 📋 개요

StarcUp 프로젝트에서 UI(Electron)와 Core(.NET) 간의 모든 Named Pipe 통신을 조사한 결과를 정리한 문서입니다.

**작성일**: 2025-07-20  
**분석 범위**: StarcUp.UI ↔ StarcUp.Core 간 모든 Named Pipe 통신

---

## 🔍 현재 통신 구조 문제점

### ⚠️ **두 가지 서로 다른 통신 규약이 혼재**

#### 1️⃣ **의도된 새로운 규약** (CoreCommunicationService 레벨)
```typescript
// UI에서 사용하려던 규약
{ type: 'preset:init', payload: { ... } }
```

#### 2️⃣ **실제 구현된 기존 규약** (NamedPipeService 레벨)  
```json
// 실제 전송되는 형태
{ 
  "type": "Request", 
  "command": "preset-init", 
  "args": ["{JSON_STRING}"], 
  "id": "...", 
  "timestamp": ... 
}
```

---

## 📊 Named Pipe 통신 전체 현황

### 🖥️ **UI → Core 명령어 (Request)**

| 명령어 | UI 호출 위치 | Core 처리 위치 | 데이터 구조 | 상태 |
|--------|-------------|---------------|-------------|------|
| `start-game-detect` | CoreCommunicationService:102 | CommunicationService:166 | `args: []` | ✅ 정상 |
| `stop-game-detect` | CoreCommunicationService:107 | CommunicationService:170 | `args: []` | ✅ 정상 |
| `get-game-status` | CoreCommunicationService:112 | - | `args: []` | ❌ 핸들러 없음 |
| `get-unit-counts` | CoreCommunicationService:121 | - | `args: [playerId?]` | ❌ 핸들러 없음 |
| `get-player-info` | CoreCommunicationService:127 | - | `args: []` | ❌ 핸들러 없음 |
| `preset-init` | CoreCommunicationService:135 | CommunicationService:175 | `args: [JSON_STRING]` | ⚠️ args 추출 필요 |
| `preset-update` | CoreCommunicationService:143 | CommunicationService:179 | `args: [JSON_STRING]` | ⚠️ args 추출 필요 |

### 📤 **Core → UI 이벤트 (Event)**

| 이벤트 | Core 발생 위치 | 데이터 구조 | 상태 |
|--------|---------------|-------------|------|
| `game-detected` | CommunicationService | `{ eventType, gameInfo }` | ✅ 정상 |
| `game-ended` | CommunicationService | `{ eventType, gameInfo }` | ✅ 정상 |
| `game-status-changed` | CommunicationService | `{ eventType, status }` | ✅ 정상 |

---

## 🔧 각 통신의 상세 분석

### 1. **게임 감지 명령어들** ✅

**패턴**: 단순 명령어, args 없음
```json
// 요청
{ "type": "Request", "command": "start-game-detect", "args": [] }

// 응답  
{ "type": "Response", "success": true, "data": {...} }
```

**처리 흐름**:
1. UI: `CoreCommunicationService.startGameDetection()`
2. NamedPipe: `sendCommand('start-game-detect')`
3. Core: `CommunicationService.OnCommandRequestReceived()` → `_gameDetector.StartDetection()`

### 2. **프리셋 명령어들** ⚠️

**패턴**: JSON 데이터를 args 배열에 문자열로 전송

#### **preset-init**
```json
// UI에서 전송
{
  "type": "Request",
  "command": "preset-init", 
  "args": ["{\"type\":\"preset-init\",\"timestamp\":...,\"presets\":{\"worker\":{\"enabled\":true,\"settingsMask\":61}}}"]
}
```

**문제점**: 
- Core에서 `args[0]`을 다시 JSON 파싱해야 함
- `HandlePresetInit()`에서 args 추출 로직 필요 (✅ 수정 완료)

#### **preset-update**  
```json
// UI에서 전송
{
  "type": "Request",
  "command": "preset-update",
  "args": ["{\"type\":\"preset-update\",\"timestamp\":...,\"presetType\":\"worker\",\"data\":{...}}"]
}
```

**문제점**:
- 동일한 args 추출 문제 (✅ 기존에 해결됨)

### 3. **미구현 명령어들** ❌

다음 명령어들은 UI에서 전송 코드는 있지만 Core에서 처리하지 않음:
- `get-game-status`
- `get-unit-counts` 
- `get-player-info`

---

## 🗂️ 코드 위치 정리

### **UI 측 (StarcUp.UI)**

#### CoreCommunicationService.ts
```typescript
// 라인 102-143: 모든 Named Pipe 명령어 등록
this.commandRegistry.register({
  name: 'game:detect:start',
  handler: async () => await this.namedPipeService.sendCommand('start-game-detect')
})
```

#### NamedPipeService.ts  
```typescript
// 라인 200+: 실제 Named Pipe 통신 구현
async sendCommand(command: string, args: string[] = []): Promise<ICoreResponse>
```

### **Core 측 (StarcUp.Core)**

#### NamedPipeClient.cs
```csharp
// 라인 316: 들어오는 요청 처리
private async Task HandleIncomingRequest(JsonElement root)
// CommandRequestReceived 이벤트 발생
```

#### CommunicationService.cs
```csharp  
// 라인 160: 명령어 라우팅
private void OnCommandRequestReceived(object sender, CommandRequestEventArgs e)

// 라인 441: 프리셋 초기화 처리
private void HandlePresetInit(CommandRequestEventArgs e)

// 라인 526: 프리셋 업데이트 처리  
private void HandlePresetUpdate(CommandRequestEventArgs e)
```

#### NamedPipeProtocol.cs
```csharp
// 라인 120-128: 지원 명령어 상수 정의
public static class Commands
{
    public const string StartGameDetect = "start-game-detect";
    public const string PresetInit = "preset-init";
    // ...
}
```

---

## 💡 개선 방안

### 1. **통신 규약 통일**

**Option A**: 기존 args 방식으로 통일
```json
{ "command": "preset-init", "args": ["data"] }
```

**Option B**: 새로운 payload 방식으로 통일  
```json
{ "type": "preset:init", "payload": {...} }
```

### 2. **args 추출 로직 중앙화**

```csharp
// 공통 유틸리티 메서드
private string ExtractJsonFromArgs(object payload)
{
    // args 배열에서 JSON 문자열 추출하는 공통 로직
}
```

### 3. **미구현 명령어 처리**

- `get-game-status` 등 누락된 Core 핸들러 구현
- 또는 UI에서 불필요한 명령어 제거

### 4. **타입 안전성 강화**

```typescript
// TypeScript 인터페이스로 통신 규약 명확화
interface NamedPipeRequest {
  type: 'Request';
  command: string;
  args: string[];
  id: string;
  timestamp: number;
}
```

---

## 📈 우선순위별 개선 계획

### 🔥 **High Priority**
1. ✅ preset-init args 추출 문제 해결 (완료)
2. 통신 규약 통일 방향 결정
3. 공통 args 추출 유틸리티 구현

### 🔶 **Medium Priority**  
1. 미구현 명령어 처리 결정
2. 타입 안전성 강화
3. 에러 처리 표준화

### 🔵 **Low Priority**
1. 성능 최적화
2. 로깅 표준화
3. 문서화 완선

---

## 📝 결론

현재 UI-Core 간 Named Pipe 통신은 **두 가지 서로 다른 규약이 혼재**하여 일관성이 부족합니다. 특히 프리셋 관련 명령어는 JSON 데이터를 args 배열에 문자열로 래핑하여 전송하므로, Core에서 추가적인 파싱 로직이 필요합니다.

**권장사항**: 
1. 통신 규약을 하나로 통일
2. 공통 유틸리티로 중복 코드 제거  
3. TypeScript 인터페이스로 타입 안전성 확보
