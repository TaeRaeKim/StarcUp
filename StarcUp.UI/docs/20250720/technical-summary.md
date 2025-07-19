# 기술 요약: StarcUp 오버레이 위치 동기화

**구현 일자**: 2025년 1월 19일  
**최종 상태**: Phase 3 완료 (Debounced Throttling + 성능 최적화)

## 🎯 최종 구현 결과

### 핵심 성과
- ✅ **실시간 위치 동기화**: 스타크래프트 윈도우 ↔ React 오버레이 완벽 동기화
- ✅ **Debounced Throttling**: 성능 최적화 + 마지막 이벤트 누락 방지
- ✅ **성능 모니터링**: 실시간 FPS, 업데이트 횟수, 연결 상태 추적
- ✅ **양방향 빌드 성공**: StarcUp.Core + StarcUp.UI 모두 안정적 빌드

### 기술 스택
- **Backend**: .NET 8.0 (StarcUp.Core) - Windows API, Named Pipe
- **Frontend**: React 18 + TypeScript + Electron 30 (StarcUp.UI)
- **통신**: Named Pipe (Core ↔ UI) + IPC (Electron Main ↔ Renderer)

## 📁 주요 변경 파일

### StarcUp.Core
```
📂 StarcUp.Core/Src/
├── 📂 Infrastructure/Windows/
│   ├── 📄 WindowPositionData.cs (확장됨)
│   └── 📄 WindowInfoExtensions.cs (활용됨)
└── 📂 Business/Communication/
    └── 📄 CommunicationService.cs (이벤트 전송 최적화)
```

### StarcUp.UI
```
📂 StarcUp.UI/
├── 📂 electron/src/services/
│   ├── 📂 types.ts (CenterPositionData 추가)
│   ├── 📂 core/CoreCommunicationService.ts (이벤트 처리)
│   └── 📂 overlay/OverlayAutoManager.ts (Debounced Throttling)
├── 📂 electron/preload.ts (IPC 확장)
└── 📂 src/overlay/
    ├── 📄 OverlayApp.tsx (React 컴포넌트 + 성능 모니터링)
    ├── 📄 main.tsx (오버레이 엔트리포인트)
    └── 📄 overlay.css (오버레이 전용 스타일)
```

## 🔧 핵심 기술 구현

### 1. Debounced Throttling 패턴
```typescript
// 즉시 처리 + 지연 처리 하이브리드 방식
if (this.shouldSyncPosition()) {
  this.syncOverlayPosition(position)      // 즉시 처리
  this.pendingPosition = null
  this.clearDebounceTimer()
} else {
  this.setupDebounceTimer()               // 50ms 후 처리
}
```

**효과**: 
- 60fps 성능 유지 (16ms throttling)
- 마지막 이벤트 100% 처리 보장
- 최대 50ms 지연으로 정확한 최종 위치

### 2. 클라이언트 영역 기반 동기화
```csharp
// StarcUp.Core - 정확한 게임 화면 영역 계산
var windowPosition = new WindowPositionData {
    ClientX = clientPoint.X,
    ClientY = clientPoint.Y,
    ClientWidth = clientRect.Right - clientRect.Left,
    ClientHeight = clientRect.Bottom - clientRect.Top,
    // ... 윈도우 상태 정보
};
```

**효과**: 타이틀바, 테두리 제외한 순수 게임 화면에만 오버레이 배치

### 3. 실시간 성능 모니터링
```typescript
// React 컴포넌트 - 개발자 디버그 패널
<div>Updates: {updateCount}</div>
<div>FPS: {frameRate > 0 ? `~${frameRate}` : 'N/A'}</div>
<div>Throttling: 16ms (60fps target)</div>
<div>Debounce Delay: 50ms (last event guarantee)</div>
<div>Last Event: {lastEventType || 'N/A'}</div>
```

**효과**: 실시간 성능 추적 및 문제 진단 가능

## 📊 성능 지표

| 구분 | 설정값 | 목적 |
|------|--------|------|
| **UI Throttling** | 16ms | 60fps 목표 성능 |
| **Debounce Delay** | 50ms | 마지막 이벤트 보장 |
| **Core Throttling** | 50ms | Named Pipe 최적화 |
| **위치 변경 임계값** | 5px | 미세 움직임 필터링 |

## 🌊 데이터 플로우

```
1. 🖼️  스타크래프트 윈도우 이동
2. 🔍  WindowManager.WindowPositionChanged 이벤트 발생
3. 📊  WindowInfo → WindowPositionData 변환
4. 🚇  Named Pipe로 StarcUp.UI 전송
5. ⚡  OverlayAutoManager에서 Debounced Throttling 적용
6. 🖥️  Electron 오버레이 윈도우 위치/크기 설정
7. 📡  IPC로 React 컴포넌트에 중앙 좌표 전달
8. 🎯  "Hello World" 텍스트가 게임 화면 정확한 중앙에 배치
```

## 🛠️ 개발자 도구

### 디버그 정보 (개발 환경 전용)
- **연결 상태**: 실시간 Core ↔ UI 연결 상태
- **위치 정보**: 중앙 좌표, 게임 영역 bounds
- **성능 메트릭**: 업데이트 횟수, 예상 FPS
- **타임스탬프**: 마지막 업데이트 시간

### 콘솔 로깅
```
✅ 즉시 위치 동기화 실행
⏳ Throttling으로 인해 debounce 타이머 설정  
⏰ Debounce 타이머로 마지막 위치 동기화 실행
🎯 오버레이 중앙 위치 업데이트: {x: 640, y: 360}
```

## 🔄 Phase별 진행 상황

### ✅ Phase 1: StarcUp.Core 윈도우 위치 동기화
- WindowPositionData 확장 (클라이언트 영역 포함)
- CommunicationService 이벤트 전송 구현
- 50ms throttling + 5px 임계값 최적화

### ✅ Phase 2: StarcUp.UI 오버레이 위치 동기화
- **2-1**: React 오버레이 컴포넌트 중앙 배치 구현
- **2-2**: preload.ts IPC 통신 확장 
- **2-3**: 오버레이 컴포넌트 디버그 정보 표시
- **2-4**: 성능 최적화 및 테스트

### ✅ Phase 3: 마지막 이벤트 누락 방지 개선
- Debounced Throttling 패턴 구현
- 성능 모니터링 도구 추가
- shouldSyncPositionForced 메서드로 강제 동기화

### ❌ Phase 4: 초기 연결 시 윈도우 위치 이벤트 전송 (원복됨)
- 사용자 요청에 따라 모든 변경사항 원복
- 성능 최적화 기능은 유지

## 🚀 사용 방법

### 실행 순서
1. 스타크래프트 실행
2. StarcUp.Core 실행: `dotnet run`
3. StarcUp.UI 실행: `npm run dev`

### 확인 방법
1. 게임 윈도우 이동 → "Hello World" 중앙 배치 확인
2. F12 개발자 도구 → 디버그 정보 확인
3. 연속 드래그 → 최종 위치 정확성 확인

## 🔮 향후 개선 방향

### 단기 개선사항
- [ ] 다중 모니터 환경 지원
- [ ] 사용자 설정 가능한 성능 파라미터
- [ ] 오버레이 투명도/크기 조절

### 장기 확장성
- [ ] 게임 정보 실시간 표시 오버레이
- [ ] 인터랙티브 UI 요소 추가
- [ ] 다른 게임 지원 확장

## 📋 검증 완료 항목

- ✅ StarcUp.Core 빌드 성공 (경고 0개, 오류 0개)
- ✅ StarcUp.UI 빌드 성공 (TypeScript 컴파일 + Electron 패키징)
- ✅ 실시간 위치 동기화 동작
- ✅ Debounced Throttling 성능 최적화
- ✅ 성능 모니터링 도구 작동
- ✅ 개발자 디버그 정보 표시
- ✅ 메모리 누수 방지 (타이머 정리, 이벤트 해제)

---

**결론**: 안정적이고 성능 최적화된 실시간 오버레이 위치 동기화 시스템 구축 완료 🎉