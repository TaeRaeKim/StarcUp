# Electron 오버레이 아키텍처 문서

## 개요
이 문서는 StarcUp.Electron 프로젝트에서 구현된 Windows 오버레이 시스템의 아키텍처와 구현 방식을 설명합니다.

## 시스템 구조

### 1. 전체 아키텍처
```
┌─────────────────────────────────────────────────────────────┐
│                    Electron Application                     │
├─────────────────────────────────────────────────────────────┤
│  Main Process (main.ts)                                     │
│  ├─ BrowserWindow (메인 창)                                  │
│  ├─ BrowserWindow (오버레이 창)                               │
│  └─ IPC 핸들러 (오버레이 제어)                                │
├─────────────────────────────────────────────────────────────┤
│  Preload Script (preload.ts)                               │
│  └─ electronAPI 인터페이스 (IPC 브리지)                       │
├─────────────────────────────────────────────────────────────┤
│  Renderer Process (React)                                  │
│  ├─ App.tsx (메인 UI)                                       │
│  ├─ 오버레이 상태 관리 (useState)                             │
│  └─ UI 컨트롤 (버튼, 스위치)                                 │
└─────────────────────────────────────────────────────────────┘
```

### 2. 핵심 컴포넌트

#### 2.1 Main Process (electron/main.ts)
```typescript
// 주요 변수
let win: BrowserWindow | null           // 메인 애플리케이션 창
let overlayWin: BrowserWindow | null    // 오버레이 창

// 주요 함수
createWindow()        // 메인 창 생성
createOverlayWindow() // 오버레이 창 생성
setupIPC()           // IPC 핸들러 설정
```

**오버레이 창 설정:**
- `transparent: true` - 투명 배경
- `alwaysOnTop: true` - 항상 최상위
- `skipTaskbar: true` - 작업표시줄에 표시 안함
- `focusable: false` - 포커스 불가
- `setIgnoreMouseEvents(true)` - 클릭 통과

#### 2.2 Preload Script (electron/preload.ts)
```typescript
// IPC 브리지 역할
contextBridge.exposeInMainWorld('electronAPI', {
  // 창 제어
  minimizeWindow: () => ipcRenderer.send('minimize-window'),
  maximizeWindow: () => ipcRenderer.send('maximize-window'),
  closeWindow: () => ipcRenderer.send('close-window'),
  
  // 오버레이 제어
  toggleOverlay: () => ipcRenderer.send('toggle-overlay'),
  showOverlay: () => ipcRenderer.send('show-overlay'),
  hideOverlay: () => ipcRenderer.send('hide-overlay'),
})
```

#### 2.3 Renderer Process (src/App.tsx)
```typescript
// 상태 관리
const [isActive, setIsActive] = useState(false)

// 오버레이 제어 함수
const toggleOverlay = () => {
  const newState = !isActive
  setIsActive(newState)
  
  // Electron 오버레이 창 제어
  if (newState) {
    window.electronAPI?.showOverlay()
  } else {
    window.electronAPI?.hideOverlay()
  }
}
```

## IPC 통신 플로우

### 1. 오버레이 활성화 플로우
```
[UI 버튼 클릭] 
    ↓
[toggleOverlay() 함수 호출]
    ↓
[React 상태 업데이트: setIsActive(true)]
    ↓
[window.electronAPI.showOverlay() 호출]
    ↓
[preload.ts에서 IPC 메시지 전송: 'show-overlay']
    ↓
[main.ts IPC 핸들러에서 overlayWin.show() 실행]
    ↓
[오버레이 창 표시]
```

### 2. 오버레이 비활성화 플로우
```
[UI 버튼 클릭] 
    ↓
[toggleOverlay() 함수 호출]
    ↓
[React 상태 업데이트: setIsActive(false)]
    ↓
[window.electronAPI.hideOverlay() 호출]
    ↓
[preload.ts에서 IPC 메시지 전송: 'hide-overlay']
    ↓
[main.ts IPC 핸들러에서 overlayWin.hide() 실행]
    ↓
[오버레이 창 숨김]
```

## 오버레이 창 특성

### 1. 시각적 특성
- **투명 배경**: CSS `background: transparent`
- **반투명 컨텐츠**: `rgba(0, 0, 0, 0.8)` 배경
- **네온 효과**: 초록색 테두리 + 글로우 효과
- **중앙 배치**: 화면 중앙에 위치

### 2. 동작 특성
- **클릭 통과**: 마우스 이벤트가 하위 창으로 전달
- **항상 최상위**: 다른 모든 창 위에 표시
- **포커스 불가**: 키보드 포커스를 받지 않음
- **작업표시줄 숨김**: 작업표시줄에 표시되지 않음

### 3. 컨텐츠 구조
```html
<div class="overlay-content">
  Hello World!
</div>
```

## 단축키 지원

### 글로벌 단축키
- **F1**: 오버레이 토글
- **F12**: 개발자 도구 토글
- **Ctrl+Shift+I**: 개발자 도구 토글

## 상태 관리

### 1. React 상태
```typescript
const [isActive, setIsActive] = useState(false)
```
- UI 상태를 관리
- 버튼 및 스위치 상태 제어
- 시각적 피드백 제공

### 2. Electron 창 상태
```typescript
overlayWin.isVisible()  // 오버레이 창 표시 여부
overlayWin.show()       // 오버레이 창 표시
overlayWin.hide()       // 오버레이 창 숨김
```

## 보안 고려사항

### 1. Context Isolation
- `contextIsolation: true` 설정으로 보안 격리
- preload 스크립트를 통한 안전한 API 노출

### 2. Node Integration
- `nodeIntegration: false` 설정으로 렌더러 프로세스에서 Node.js 직접 접근 제한

### 3. 제한된 API 노출
- 필요한 기능만 `electronAPI`를 통해 선택적 노출

## 확장 가능성

### 1. 오버레이 컨텐츠 확장
- HTML 템플릿 수정으로 다양한 컨텐츠 표시 가능
- React 컴포넌트 기반 오버레이 구현 가능

### 2. 다중 오버레이
- 여러 오버레이 창 동시 관리 가능
- 각기 다른 목적의 오버레이 생성 가능

### 3. 게임 통합
- 게임 상태에 따른 동적 오버레이 컨텐츠 변경
- 실시간 게임 데이터 표시

## 문제 해결

### 1. 일반적인 문제
- **오버레이가 표시되지 않는 경우**: `overlayWin.isVisible()` 상태 확인
- **클릭이 통과되지 않는 경우**: `setIgnoreMouseEvents(true)` 설정 확인
- **투명도 문제**: `transparent: true` 및 CSS 설정 확인

### 2. 개발 도구 에러
- `Autofill.enable` 에러는 harmless하며 무시 가능
- 개발자 도구 자동 열기 비활성화로 해결 가능

## 성능 최적화

### 1. 메모리 관리
- 오버레이 창 생성/제거 시 적절한 리소스 정리
- 불필요한 렌더링 최소화

### 2. CPU 사용량
- 투명 창 사용 시 GPU 가속 활용
- 애니메이션 최적화

## 버전 정보
- **Electron**: 30.x
- **React**: 18.x
- **TypeScript**: 5.x
- **Node.js**: 22.x

---

이 문서는 StarcUp.Electron 프로젝트의 오버레이 시스템 구현을 위한 기술 문서입니다.