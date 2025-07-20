# Core 측 프리셋 처리 시스템 설계

## 📋 개요

StarcUp.Core에서 UI로부터 받은 프리셋 초기화 및 업데이트 메시지를 처리하여 WorkerManager에 반영하는 시스템을 설계합니다.
기존 Named Pipe 통신 인프라를 활용하여 새로운 명령을 추가하고, 프리셋 데이터를 적절히 파싱하여 적용합니다.

## 🎯 목표

1. **프리셋 명령 추가**: 기존 NamedPipeProtocol에 프리셋 관련 명령 추가
2. **메시지 처리**: CommunicationService에서 프리셋 메시지 처리 로직 구현
3. **WorkerManager 연동**: 받은 프리셋 데이터를 WorkerPresetEnum으로 변환하여 적용
4. **에러 처리**: 프리셋 처리 실패 시 적절한 오류 응답 전송
5. **확장성**: 향후 다른 프리셋 타입 추가 시 쉽게 확장 가능한 구조

## 🏗️ 현재 Core 아키텍처 분석

### 기존 통신 구조
```
UI → NamedPipe → CommunicationService → 각종 Manager
                     ↓
               OnCommandRequestReceived
                     ↓
               switch (e.Command) 처리
```

### 기존 명령 처리 방식
```csharp
// CommunicationService.cs:158
switch (e.Command)
{
    case NamedPipeProtocol.Commands.StartGameDetect:
        _gameDetector.StartDetection();
        break;
    case NamedPipeProtocol.Commands.StopGameDetect:
        _gameDetector.StopDetection();
        break;
}
```

## 🔧 설계 방안

### 1. NamedPipeProtocol 확장

기존 `Commands` 클래스에 프리셋 관련 명령 추가:

```csharp
// NamedPipeProtocol.cs Commands 클래스에 추가
public static class Commands
{
    // 기존 명령들...
    public const string Ping = "ping";
    public const string StartGameDetect = "start-game-detect";
    public const string StopGameDetect = "stop-game-detect";
    
    // 새로운 프리셋 명령들
    public const string PresetInit = "preset-init";
    public const string PresetUpdate = "preset-update";
}
```

### 2. 프리셋 데이터 모델 정의

Core에서 사용할 프리셋 데이터 구조:

```csharp
// 새 파일: StarcUp.Core/Src/Business/Profile/Models/PresetModels.cs
namespace StarcUp.Business.Profile.Models
{
    public class PresetInitData
    {
        public string Type { get; set; } = "preset-init";
        public long Timestamp { get; set; }
        public PresetCollection Presets { get; set; } = new();
    }

    public class PresetUpdateData
    {
        public string Type { get; set; } = "preset-update";
        public long Timestamp { get; set; }
        public string PresetType { get; set; }
        public PresetItem Data { get; set; }
    }

    public class PresetCollection
    {
        public PresetItem Worker { get; set; }
        public PresetItem Population { get; set; }
        public PresetItem Unit { get; set; }
        public PresetItem Upgrade { get; set; }
        public PresetItem BuildOrder { get; set; }
    }

    public class PresetItem
    {
        public bool Enabled { get; set; }
        public int SettingsMask { get; set; }
    }
}
```

### 3. CommunicationService 확장

프리셋 명령 처리를 위한 메서드 추가:

```csharp
// CommunicationService.cs에 추가할 내용

private readonly IWorkerManager _workerManager; // 생성자에서 주입

private void OnCommandRequestReceived(object sender, CommandRequestEventArgs e)
{            
    try
    {
        switch (e.Command)
        {
            // 기존 명령들...
            case NamedPipeProtocol.Commands.StartGameDetect:
                _gameDetector.StartDetection();
                break;
                
            case NamedPipeProtocol.Commands.StopGameDetect:
                _gameDetector.StopDetection();
                break;
                
            // 새로운 프리셋 명령들
            case NamedPipeProtocol.Commands.PresetInit:
                HandlePresetInit(e);
                break;
                
            case NamedPipeProtocol.Commands.PresetUpdate:
                HandlePresetUpdate(e);
                break;
                
            default:
                Console.WriteLine($"⚠️ 알 수 없는 명령: {e.Command}");
                break;
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ 명령 처리 중 오류 발생: {e.Command} - {ex.Message}");
    }
}
```

### 4. 프리셋 처리 메서드 구현

```csharp
private void HandlePresetInit(CommandRequestEventArgs e)
{
    try
    {
        Console.WriteLine("🚀 프리셋 초기화 요청 수신");
        
        if (e.Payload == null)
        {
            SendErrorResponse(e.RequestId, "프리셋 초기화 데이터가 없습니다");
            return;
        }

        var initData = JsonSerializer.Deserialize<PresetInitData>(e.Payload.Value.GetRawText());
        
        // 일꾼 프리셋 처리
        if (initData.Presets?.Worker != null)
        {
            var workerPreset = (WorkerPresetEnum)initData.Presets.Worker.SettingsMask;
            _workerManager.WorkerPreset = workerPreset;
            
            Console.WriteLine($"✅ 일꾼 프리셋 초기화 완료: {workerPreset} (0x{(int)workerPreset:X2})");
        }
        
        // 향후 다른 프리셋들도 여기서 처리...
        
        SendSuccessResponse(e.RequestId, new { message = "프리셋 초기화 완료" });
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ 프리셋 초기화 실패: {ex.Message}");
        SendErrorResponse(e.RequestId, $"프리셋 초기화 실패: {ex.Message}");
    }
}

private void HandlePresetUpdate(CommandRequestEventArgs e)
{
    try
    {
        Console.WriteLine("🔄 프리셋 업데이트 요청 수신");
        
        if (e.Payload == null)
        {
            SendErrorResponse(e.RequestId, "프리셋 업데이트 데이터가 없습니다");
            return;
        }

        var updateData = JsonSerializer.Deserialize<PresetUpdateData>(e.Payload.Value.GetRawText());
        
        switch (updateData.PresetType?.ToLower())
        {
            case "worker":
                var workerPreset = (WorkerPresetEnum)updateData.Data.SettingsMask;
                _workerManager.WorkerPreset = workerPreset;
                Console.WriteLine($"✅ 일꾼 프리셋 업데이트 완료: {workerPreset} (0x{(int)workerPreset:X2})");
                break;
                
            case "population":
                // 향후 구현
                Console.WriteLine("⚠️ 인구수 프리셋은 아직 구현되지 않았습니다");
                break;
                
            case "unit":
                // 향후 구현
                Console.WriteLine("⚠️ 유닛 프리셋은 아직 구현되지 않았습니다");
                break;
                
            case "upgrade":
                // 향후 구현
                Console.WriteLine("⚠️ 업그레이드 프리셋은 아직 구현되지 않았습니다");
                break;
                
            case "buildorder":
                // 향후 구현
                Console.WriteLine("⚠️ 빌드오더 프리셋은 아직 구현되지 않았습니다");
                break;
                
            default:
                Console.WriteLine($"⚠️ 알 수 없는 프리셋 타입: {updateData.PresetType}");
                SendErrorResponse(e.RequestId, $"알 수 없는 프리셋 타입: {updateData.PresetType}");
                return;
        }
        
        SendSuccessResponse(e.RequestId, new { message = "프리셋 업데이트 완료", presetType = updateData.PresetType });
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ 프리셋 업데이트 실패: {ex.Message}");
        SendErrorResponse(e.RequestId, $"프리셋 업데이트 실패: {ex.Message}");
    }
}

// 응답 헬퍼 메서드들
private void SendSuccessResponse(string requestId, object data)
{
    var response = new NamedPipeProtocol.ResponseMessage(requestId, true, data);
    _pipeClient.SendResponse(response);
}

private void SendErrorResponse(string requestId, string errorMessage)
{
    var response = new NamedPipeProtocol.ResponseMessage(requestId, false, null, errorMessage);
    _pipeClient.SendResponse(response);
}
```

### 5. 의존성 주입 업데이트

ServiceRegistration.cs에서 WorkerManager 의존성 추가:

```csharp
// ServiceRegistration.cs의 CommunicationService 등록 부분 수정
public static void RegisterCommunicationServices(ServiceContainer container)
{
    // IWorkerManager 먼저 등록 (이미 되어 있을 것)
    // container.RegisterSingleton<IWorkerManager, WorkerManager>();
    
    // CommunicationService에 WorkerManager 의존성 추가
    container.RegisterSingleton<ICommunicationService>(c => 
        new CommunicationService(
            c.GetService<INamedPipeClient>(),
            c.GetService<IGameDetector>(),
            c.GetService<IInGameDetector>(),
            c.GetService<IWindowManager>(),
            c.GetService<IWorkerManager>() // 추가
        ));
}
```

### 6. WorkerPresetEnum 디버깅 유틸리티

```csharp
// WorkerManager.cs에 추가할 디버깅 메서드
public string GetWorkerPresetDescription()
{
    var flags = new List<string>();
    
    if (IsWorkerStateSet(WorkerPresetEnum.Default)) flags.Add("Default");
    if (IsWorkerStateSet(WorkerPresetEnum.IncludeProduction)) flags.Add("IncludeProduction");
    if (IsWorkerStateSet(WorkerPresetEnum.Idle)) flags.Add("Idle");
    if (IsWorkerStateSet(WorkerPresetEnum.DetectProduction)) flags.Add("DetectProduction");
    if (IsWorkerStateSet(WorkerPresetEnum.DetectDeath)) flags.Add("DetectDeath");
    if (IsWorkerStateSet(WorkerPresetEnum.CheckGas)) flags.Add("CheckGas");
    
    return flags.Count > 0 ? string.Join(" | ", flags) : "None";
}
```

## 🔄 데이터 흐름

### 프리셋 초기화 플로우
```
UI (App.tsx)
    ↓ sendPresetInit()
Named Pipe (preset-init command)
    ↓ 
CommunicationService.OnCommandRequestReceived()
    ↓ HandlePresetInit()
WorkerManager.WorkerPreset = (WorkerPresetEnum)mask
    ↓
성공 응답 전송
```

### 프리셋 업데이트 플로우
```
UI (WorkerDetailSettings)
    ↓ handleSave()
Named Pipe (preset-update command)
    ↓
CommunicationService.OnCommandRequestReceived()
    ↓ HandlePresetUpdate()
WorkerManager.WorkerPreset = (WorkerPresetEnum)mask
    ↓
성공 응답 전송
```

## 📝 구현 순서

### Phase 1: 기본 인프라
1. **NamedPipeProtocol 확장** - 새로운 명령 상수 추가
2. **프리셋 데이터 모델 생성** - PresetModels.cs 파일 생성
3. **CommunicationService 생성자 수정** - WorkerManager 의존성 추가

### Phase 2: 메시지 처리 로직
4. **HandlePresetInit 메서드 구현** - 초기화 요청 처리
5. **HandlePresetUpdate 메서드 구현** - 업데이트 요청 처리
6. **응답 헬퍼 메서드 구현** - 성공/실패 응답 전송

### Phase 3: 테스트 및 디버깅
7. **로깅 강화** - 프리셋 처리 과정 상세 로그
8. **에러 처리 개선** - 다양한 예외 상황 대응
9. **WorkerManager 디버깅 유틸리티 추가**

## 🧪 테스트 시나리오

### 1. 프리셋 초기화 테스트
- UI에서 Core 연결 시 초기화 메시지 전송
- WorkerManager에 올바른 프리셋 설정되는지 확인
- 잘못된 데이터 전송 시 에러 응답 확인

### 2. 프리셋 업데이트 테스트
- 일꾼 설정 변경 시 업데이트 메시지 전송
- WorkerManager 프리셋이 실시간으로 변경되는지 확인
- 다양한 비트마스크 조합 테스트

### 3. 에러 처리 테스트
- 잘못된 JSON 데이터 전송
- 존재하지 않는 프리셋 타입 전송
- 네트워크 연결 끊김 상황

## 🚀 향후 확장

### 다른 프리셋 타입 지원
- PopulationManager, UnitManager, UpgradeManager 등과 연동
- 각 Manager별 프리셋 enum 정의
- 통합 프리셋 관리 인터페이스 구현

### 프리셋 저장/로드 기능
- 프리셋을 파일로 저장
- 시작 시 저장된 프리셋 자동 로드
- 프리셋 프로필 관리

## 🔍 검토 포인트

- [ ] JSON 직렬화/역직렬화 성능
- [ ] WorkerPresetEnum 비트마스크 정확성
- [ ] 메모리 누수 방지 (IDisposable 패턴)
- [ ] 스레드 안전성 (멀티스레드 환경)
- [ ] 예외 처리 완전성