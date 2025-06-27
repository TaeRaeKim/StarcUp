# 스택 메서드 리팩토링 및 명확화

## 📋 개요

StarcUp 프로젝트의 스택 관련 메서드들의 이름과 실제 동작이 일치하지 않는 문제를 해결하고, 각 메서드의 용도를 명확히 구분했습니다.

## 🚨 문제점

### 기존 코드의 혼란

기존 `GetStackStart()`와 `GetStackTop()` 메서드의 이름과 실제 동작이 반대였습니다:

```csharp
// 기존 (잘못된 명명)
GetStackStart() // TEB + 0x08을 읽음 → 실제로는 스택 상단 (높은 주소)
GetStackTop()   // TEB + 0x10을 읽음 → 실제로는 스택 하단 (낮은 주소)
```

### Windows TEB 구조체의 정확한 의미

```
TEB + 0x08: NT_TIB.StackBase  - 스택의 상단 (높은 주소, 최대 확장 가능 주소)
TEB + 0x10: NT_TIB.StackLimit - 스택의 하단 (낮은 주소, 최소 사용 가능 주소)
```

## ✅ 해결 방안

### 1. 메서드 이름 수정

| 기존 메서드 | 새 메서드 | TEB 오프셋 | 설명 |
|------------|-----------|-----------|------|
| `GetStackStart()` | `GetStackTop()` | TEB + 0x08 | 스택 상단 (높은 주소) |
| `GetStackTop()` | `GetStackBottom()` | TEB + 0x10 | 스택 하단 (낮은 주소) |

### 2. 새로운 메서드 추가

```csharp
/// <summary>
/// 치트엔진 방식으로 스레드 스택 주소를 계산합니다 (kernel32 기반 검색)
/// GameTime 등 특정 메모리 해킹 용도로 사용됩니다.
/// </summary>
GetThreadStackAddress(int threadIndex)
```

## 🏗️ 스택 메모리 구조

```
높은 주소 ┌─────────────┐ ← TEB+0x08 (StackBase)  = GetStackTop()
          │             │
          │    스택     │   ↓ 스택 증가 방향
          │             │
낮은 주소  └─────────────┘ ← TEB+0x10 (StackLimit) = GetStackBottom()
```

## 🎯 메서드별 용도

### 1. GetStackTop() - 스택 상단 주소
- **오프셋**: TEB + 0x08 (NT_TIB.StackBase)
- **용도**: 디버깅, 스택 정보 표시
- **사용처**: ControlForm의 메모리 정보 표시

### 2. GetStackBottom() - 스택 하단 주소  
- **오프셋**: TEB + 0x10 (NT_TIB.StackLimit)
- **용도**: 디버깅, 스택 범위 확인
- **사용처**: 현재 직접 사용되지 않음

### 3. GetThreadStackAddress() - 치트엔진 방식 스택 주소
- **방식**: kernel32.dll 기반 역방향 검색
- **용도**: 메모리 해킹, GameTime 읽기
- **사용처**: ReadGameTime() 메서드

## 🔧 구현 세부사항

### GetStackTop() 구현
```csharp
public nint GetStackTop(int threadIndex = 0)
{
    var tebList = GetTebAddresses();
    nint tebAddress = tebList[threadIndex].TebAddress;
    nint stackTopPtr = tebAddress + 0x08; // NT_TIB.StackBase
    nint stackTop = ReadPointer(stackTopPtr);
    return stackTop;
}
```

### GetStackBottom() 구현
```csharp
public nint GetStackBottom(int threadIndex = 0)
{
    var tebList = GetTebAddresses();
    nint tebAddress = tebList[threadIndex].TebAddress;
    nint stackBottomPtr = tebAddress + 0x10; // NT_TIB.StackLimit
    nint stackBottom = ReadPointer(stackBottomPtr);
    return stackBottom;
}
```

### GetThreadStackAddress() 구현
```csharp
public nint GetThreadStackAddress(int threadIndex = 0)
{
    // 1. kernel32.dll 모듈 정보 가져오기
    var kernel32Module = GetKernel32Module();
    
    // 2. StackTop에서 4096바이트 역방향 검색
    nint stackTop = GetStackTop(threadIndex);
    nint stackSearchStart = stackTop - 4096;
    
    // 3. kernel32 범위에 있는 주소 찾기
    // ... (상세 구현은 코드 참조)
}
```

## 📊 변경사항 요약

### 파일 변경
- `IMemoryService.cs`: 인터페이스 메서드 이름 변경 및 문서 추가
- `MemoryService.cs`: 구현체 메서드 이름 변경 및 문서 추가  
- `ControlForm.cs`: 호출 코드 수정
- `stack-methods-refactoring.md`: 이 문서 추가

### 호환성
- **기존 코드**: ControlForm의 디버깅 표시 기능은 정상 동작
- **새 기능**: GameTime 읽기는 정확한 치트엔진 방식 사용

## 🧪 테스트 방법

1. **ControlForm 테스트**: 메모리 정보 표시에서 StackTop 주소 확인
2. **GameTime 테스트**: 인게임에서 GameTime이 정상적으로 읽히는지 확인
3. **로그 확인**: 각 메서드의 로그에서 주소값이 올바른지 확인

## 🔍 참고 자료

- [Windows NT_TIB Structure](https://docs.microsoft.com/en-us/windows/win32/api/winnt/ns-winnt-nt_tib)
- [Thread Environment Block (TEB)](https://en.wikipedia.org/wiki/Win32_Thread_Information_Block)
- [Stack Layout in Windows](https://docs.microsoft.com/en-us/windows/win32/debug/stack-walking)

---

**작성일**: 2025-06-27  
**작성자**: Claude Code Assistant  
**관련 이슈**: GameTime 읽기 실패 문제 해결