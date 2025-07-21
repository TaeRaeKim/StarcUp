# 📊 오버레이 이펙트 구현 가이드

## 🎯 1. HTML 구조 설정

### 기본 오버레이 HTML
```html
<div class="unit-counter" id="unitCounter">
    유닛 수: <span class="counter-number" id="counterNumber">0</span>
</div>

<!-- 이펙트 테스트 버튼들 -->
<div class="effect-controls">
    <button class="effect-btn effect-spawn-btn" onclick="testSpawnEffect()">
        🔵 생산 이펙트 테스트
    </button>
    <button class="effect-btn effect-death-btn" onclick="testDeathEffect()">
        🔴 사망 이펙트 테스트
    </button>
</div>
```

## 🎨 2. CSS 스타일링

### 기본 오버레이 스타일
```css
.unit-counter {
    position: absolute;
    top: 20px;
    left: 20px;
    background: rgba(0, 0, 0, 0.9);
    padding: 15px 25px;
    border-radius: 30px;
    border: 3px solid #2196F3;  /* 파란색 기본 테두리 */
    font-size: 20px;
    font-weight: bold;
    z-index: 1000;
    transition: all 0.2s ease;
    box-shadow: 0 0 20px rgba(33, 150, 243, 0.3);
    min-width: 150px;
    text-align: center;
}
```

### 생산 이펙트 클래스 (파란색)
```css
.unit-counter.spawn-effect {
    animation: spawnCounterEffect 0.8s ease-out;
    border-color: #2196F3;
    color: #2196F3;
    box-shadow: 
        0 0 30px rgba(33, 150, 243, 0.8),
        inset 0 0 20px rgba(33, 150, 243, 0.2);
}

@keyframes spawnCounterEffect {
    0% {
        filter: brightness(1);
    }
    20% {
        filter: brightness(1.5);
        border-color: #64B5F6;  /* 밝은 파란색 */
        box-shadow: 
            0 0 40px rgba(33, 150, 243, 1),
            inset 0 0 25px rgba(33, 150, 243, 0.4);
    }
    40% {
        filter: brightness(1.3);
    }
    60% {
        filter: brightness(1.1);
    }
    100% {
        filter: brightness(1);
    }
}
```

### 사망 이펙트 클래스 (빨간색)
```css
.unit-counter.death-effect {
    animation: deathCounterEffect 0.6s ease-out;
    border-color: #f44336;
    color: #f44336;
    box-shadow: 
        0 0 25px rgba(244, 67, 54, 0.8),
        inset 0 0 15px rgba(244, 67, 54, 0.3);
}

@keyframes deathCounterEffect {
    0% {
        filter: brightness(1);
    }
    10% {
        filter: brightness(1.8) saturate(1.5);
        border-color: #FF5722;  /* 진한 빨간색 */
        background: rgba(244, 67, 54, 0.2);
    }
    20% {
        filter: brightness(1.6);
    }
    30% {
        filter: brightness(1.4);
    }
    40% {
        filter: brightness(1.2);
    }
    50% {
        filter: brightness(1.1);
    }
    60% {
        filter: brightness(1.05);
    }
    100% {
        filter: brightness(1);
        background: rgba(0, 0, 0, 0.9);
    }
}
```

### 테스트 버튼 스타일
```css
.effect-controls {
    display: flex;
    gap: 10px;
    justify-content: center;
    margin-bottom: 15px;
    flex-wrap: wrap;
}

.effect-btn {
    padding: 8px 16px;
    border: none;
    border-radius: 20px;
    font-size: 14px;
    font-weight: bold;
    cursor: pointer;
    transition: all 0.3s ease;
}

.effect-spawn-btn {
    background: linear-gradient(45deg, #2196F3, #1976D2);
    color: white;
    box-shadow: 0 2px 10px rgba(33, 150, 243, 0.3);
}

.effect-death-btn {
    background: linear-gradient(45deg, #f44336, #d32f2f);
    color: white;
    box-shadow: 0 2px 10px rgba(244, 67, 54, 0.3);
}
```

## ⚙️ 3. JavaScript 함수 구현

### 기본 이펙트 트리거 함수들
```javascript
function triggerSpawnEffect() {
    const unitCounter = document.getElementById('unitCounter');
    
    // 기존 이펙트 클래스 제거
    unitCounter.classList.remove('spawn-effect', 'death-effect');
    
    // 약간의 딜레이 후 새 이펙트 적용 (CSS 리플로우를 위해)
    setTimeout(() => {
        unitCounter.classList.add('spawn-effect');
        
        // 이펙트 완료 후 클래스 제거
        setTimeout(() => {
            unitCounter.classList.remove('spawn-effect');
        }, 800); // 애니메이션 시간과 일치
    }, 10);
}

function triggerDeathEffect() {
    const unitCounter = document.getElementById('unitCounter');
    
    // 기존 이펙트 클래스 제거
    unitCounter.classList.remove('spawn-effect', 'death-effect');
    
    // 약간의 딜레이 후 새 이펙트 적용
    setTimeout(() => {
        unitCounter.classList.add('death-effect');
        
        // 이펙트 완료 후 클래스 제거
        setTimeout(() => {
            unitCounter.classList.remove('death-effect');
        }, 600); // 애니메이션 시간과 일치
    }, 10);
}
```

### 테스트 전용 함수들 (숫자 변화 없음)
```javascript
function testSpawnEffect() {
    // 숫자 변화 없이 이펙트만 테스트
    triggerSpawnEffect();
}

function testDeathEffect() {
    // 숫자 변화 없이 이펙트만 테스트
    triggerDeathEffect();
}
```

### 실제 게임 로직과 연동된 함수들
```javascript
function spawnUnit() {
    // ... 유닛 생성 로직 ...
    
    unitCount++;
    updateCounter(true);        // 숫자 업데이트 + 숫자 애니메이션
    triggerSpawnEffect();       // 오버레이 이펙트
}

function killUnit() {
    // ... 유닛 제거 로직 ...
    
    unitCount--;
    updateCounter(false);       // 숫자 업데이트 + 숫자 애니메이션
    triggerDeathEffect();       // 오버레이 이펙트
}

function updateCounter(isIncrease = null) {
    const counterNumber = document.getElementById('counterNumber');
    counterNumber.textContent = unitCount;
    
    // 숫자 변화 애니메이션
    if (isIncrease === true) {
        counterNumber.className = 'counter-number increase';
    } else if (isIncrease === false) {
        counterNumber.className = 'counter-number decrease';
    }
    
    // 애니메이션 클래스 제거
    setTimeout(() => {
        counterNumber.className = 'counter-number';
    }, 500);
}
```

## 🔧 4. 핵심 구현 포인트

### A. 이펙트 중복 방지
```javascript
// ❌ 잘못된 방법: 이펙트가 중복될 수 있음
function triggerSpawnEffect() {
    unitCounter.classList.add('spawn-effect');
}

// ✅ 올바른 방법: 기존 이펙트 제거 후 새 이펙트 적용
function triggerSpawnEffect() {
    unitCounter.classList.remove('spawn-effect', 'death-effect');
    setTimeout(() => {
        unitCounter.classList.add('spawn-effect');
    }, 10);
}
```

### B. 타이밍 관리
```javascript
// 애니메이션 시간과 setTimeout 시간을 일치시켜야 함
@keyframes spawnCounterEffect {
    /* 0.8s 애니메이션 */
}

setTimeout(() => {
    unitCounter.classList.remove('spawn-effect');
}, 800); // CSS 애니메이션 시간과 일치
```

### C. CSS 리플로우 고려
```javascript
// 10ms 딜레이로 브라우저가 DOM 변경사항을 처리할 시간 제공
setTimeout(() => {
    unitCounter.classList.add('spawn-effect');
}, 10);
```

## 🎯 5. 사용 예시

### HTML에서 호출
```html
<button onclick="testSpawnEffect()">생산 이펙트 테스트</button>
<button onclick="testDeathEffect()">사망 이펙트 테스트</button>
```

### JavaScript에서 호출
```javascript
// 유닛 생산 시
spawnUnit(); // 숫자 변화 + 이펙트

// 이펙트만 테스트할 때
testSpawnEffect(); // 이펙트만

// 연속 이펙트
for (let i = 0; i < 3; i++) {
    setTimeout(() => testSpawnEffect(), i * 1000);
}
```

## 🎨 6. 커스터마이징 가이드

### 색상 변경
```css
/* 생산 이펙트를 초록색으로 변경하고 싶다면 */
.unit-counter.spawn-effect {
    border-color: #4CAF50;
    color: #4CAF50;
    box-shadow: 
        0 0 30px rgba(76, 175, 80, 0.8),
        inset 0 0 20px rgba(76, 175, 80, 0.2);
}
```

### 애니메이션 시간 조절
```css
/* 더 빠른 이펙트를 원한다면 */
.unit-counter.spawn-effect {
    animation: spawnCounterEffect 0.4s ease-out; /* 0.8s → 0.4s */
}
```

### 추가 이펙트 구현
```css
/* 크리티컬 이벤트용 골드 이펙트 */
.unit-counter.critical-effect {
    animation: criticalCounterEffect 1.2s ease-out;
    border-color: #FFD700;
    color: #FFD700;
    box-shadow: 
        0 0 50px rgba(255, 215, 0, 1),
        inset 0 0 30px rgba(255, 215, 0, 0.3);
}
```

이 가이드를 따라하면 게임에 맞는 오버레이 이펙트 시스템을 구현할 수 있습니다!