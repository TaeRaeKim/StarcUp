# StarcUp UI/UX 디자인 시스템

## 🎨 Color Palette

### Primary Colors
```
Brand Primary:    #0099FF  (스타크래프트 블루)
Brand Secondary:  #FF6B35  (액센트 오렌지)
Brand Dark:       #1A1A2E  (다크 네이비)
```

### Status Colors
```
Success:          #00D084  (초록 - 완료/정상)
Warning:          #FFB800  (노랑 - 진행중/주의)
Error:            #FF3B30  (빨강 - 에러/위험)
Info:             #007AFF  (파랑 - 정보)
```

### Neutral Colors
```
Gray 900:         #1A1A1A  (최고 대비 텍스트)
Gray 800:         #2D2D2D  (헤더/강조)
Gray 700:         #4A4A4A  (본문 텍스트)
Gray 600:         #6B6B6B  (보조 텍스트)
Gray 500:         #9B9B9B  (플레이스홀더)
Gray 400:         #C7C7C7  (구분선)
Gray 300:         #E3E3E3  (배경 구분)
Gray 200:         #F0F0F0  (카드 배경)
Gray 100:         #F8F8F8  (페이지 배경)
Gray 50:          #FAFAFA  (최밝은 배경)
```

### Game Overlay Colors
```
Overlay Background:   rgba(0, 0, 0, 0.85)  (반투명 검정)
Overlay Border:       rgba(255, 255, 255, 0.2)  (반투명 흰색)
Text Primary:         #FFFFFF  (흰색 - 최고 가독성)
Text Secondary:       #E0E0E0  (밝은 회색)
```

## 📝 Typography

### Font Family
```
Primary:     "Segoe UI", "Malgun Gothic", sans-serif
Monospace:   "Consolas", "Monaco", monospace
Icon:        "Segoe UI Symbol", sans-serif
```

### Font Sizes & Weights
```
Display Large:    28px / Bold (700)     - 메인 타이틀
Display Medium:   24px / Bold (700)     - 섹션 제목
Display Small:    20px / SemiBold (600) - 카드 제목

Heading H1:       18px / SemiBold (600) - 주요 헤딩
Heading H2:       16px / SemiBold (600) - 보조 헤딩
Heading H3:       14px / SemiBold (600) - 소제목

Body Large:       16px / Regular (400)  - 본문 텍스트
Body Medium:      14px / Regular (400)  - 일반 텍스트
Body Small:       12px / Regular (400)  - 보조 텍스트

Caption:          11px / Regular (400)  - 캡션
Label:            10px / Medium (500)   - 라벨
```

### Line Heights
```
Display:          1.2  (타이트한 간격)
Heading:          1.3  (제목용)
Body:             1.5  (읽기 좋은 간격)
Caption:          1.4  (컴팩트)
```

## 🔲 Spacing System

### Spacing Scale (8px 기준)
```
XXS:    4px   (미세 간격)
XS:     8px   (최소 간격)
S:      12px  (작은 간격)
M:      16px  (기본 간격)
L:      20px  (넓은 간격)
XL:     24px  (더 넓은 간격)
XXL:    32px  (최대 간격)
XXXL:   40px  (특별한 경우)
```

### Component Spacing
```
Card Padding:         16px
Button Padding:       8px 16px
Input Padding:        12px 16px
Icon Margin:          8px
List Item Padding:    12px 16px
```

## 🎯 Component Library

### Buttons

#### Primary Button
```
Background:       #0099FF
Text:             #FFFFFF
Border:           none
Border Radius:    6px
Padding:          12px 20px
Font:             14px / SemiBold (600)

Hover:            #0077CC
Active:           #005599
Disabled:         #C7C7C7 (background), #9B9B9B (text)
```

#### Secondary Button
```
Background:       transparent
Text:             #0099FF
Border:           1px solid #0099FF
Border Radius:    6px
Padding:          12px 20px
Font:             14px / SemiBold (600)

Hover:            #F0F8FF (background)
Active:           #E3F2FD (background)
```

#### Icon Button
```
Size:             32px × 32px
Background:       transparent
Border Radius:    4px
Icon Size:        16px

Hover:            rgba(0, 153, 255, 0.1)
Active:           rgba(0, 153, 255, 0.2)
```

### Form Elements

#### Input Field
```
Background:       #FFFFFF
Border:           1px solid #C7C7C7
Border Radius:    4px
Padding:          12px 16px
Font:             14px / Regular (400)
Min Height:       44px

Focus:            border #0099FF, box-shadow 0 0 0 3px rgba(0, 153, 255, 0.1)
Error:            border #FF3B30
Success:          border #00D084
```

#### Checkbox
```
Size:             18px × 18px
Border:           2px solid #C7C7C7
Border Radius:    3px
Background:       #FFFFFF

Checked:          background #0099FF, border #0099FF
Checkmark:        #FFFFFF, 12px
```

#### Dropdown
```
Background:       #FFFFFF
Border:           1px solid #C7C7C7
Border Radius:    4px
Padding:          12px 16px
Min Height:       44px
Arrow:            8px, #6B6B6B

Open:             border #0099FF
```

### Cards & Containers

#### Overlay Card
```
Background:       rgba(0, 0, 0, 0.85)
Border:           1px solid rgba(255, 255, 255, 0.2)
Border Radius:    8px
Padding:          16px
Backdrop Filter:  blur(10px)
Box Shadow:       0 4px 20px rgba(0, 0, 0, 0.3)
```

#### Settings Panel
```
Background:       #FFFFFF
Border:           1px solid #E3E3E3
Border Radius:    8px
Padding:          20px
Box Shadow:       0 2px 10px rgba(0, 0, 0, 0.1)
```

#### Information Card
```
Background:       #F8F8F8
Border:           1px solid #E3E3E3
Border Radius:    6px
Padding:          16px
```

## 🎮 Game Overlay Components

### Worker Status Display
```
Container:
  Background:     rgba(0, 0, 0, 0.8)
  Border Radius:  6px
  Padding:        8px 12px
  Min Width:      80px

Worker Icon:
  Size:           20px × 20px
  Color:          #FFB800 (normal), #FF3B30 (death flash)

Count Text:
  Font:           16px / Bold (700)
  Color:          #FFFFFF
  Margin Left:    6px

Idle Count:
  Font:           14px / Regular (400)
  Color:          #FF6B35
  Format:         "(2)"
```

### Build Order Guide
```
Container:
  Background:     rgba(0, 0, 0, 0.85)
  Border Radius:  8px
  Padding:        12px 16px
  Max Width:      300px

Current Phase (Line 1):
  Building Icon:  24px × 24px
  Icon States:    
    - Incomplete: #6B6B6B (회색)
    - Progress:   #FFB800 (노랑)
    - Complete:   #00D084 (초록)
  
  Count Badge:
    Background:   rgba(255, 255, 255, 0.9)
    Text:         12px / Bold (700), #1A1A1A
    Padding:      2px 6px
    Border Radius: 10px
    Position:     bottom-right of icon

Next Phase (Line 2):
  Building Icon:  20px × 20px
  Icon Color:     #6B6B6B (회색, 50% opacity)
  Margin Top:     8px
```

### Unit Count Display
```
Category Container:
  Background:     rgba(0, 0, 0, 0.8)
  Border Radius:  6px
  Padding:        8px 12px
  Margin Bottom:  4px

Unit Icon:
  Size:           18px × 18px
  Margin Right:   6px

Count Text:
  Font:           14px / SemiBold (600)
  Color:          #FFFFFF

Category Title:
  Font:           12px / Regular (400)
  Color:          #C7C7C7
  Margin Bottom:  4px
```

### Upgrade Progress
```
Container:
  Background:     rgba(0, 0, 0, 0.8)
  Border Radius:  6px
  Padding:        8px 12px

Upgrade Icon:
  Size:           20px × 20px

Progress Display:
  Time Format:    "2:30" / 14px Bold #FFFFFF
  Progress Bar:   
    Width:        60px
    Height:       4px
    Background:   rgba(255, 255, 255, 0.3)
    Fill:         #0099FF
    Border Radius: 2px

Percentage:
  Font:           12px / Regular (400)
  Color:          #E0E0E0
```

### Population Warning
```
Popup Container:
  Background:     #FF3B30
  Border Radius:  8px
  Padding:        12px 16px
  Box Shadow:     0 4px 20px rgba(255, 59, 48, 0.4)
  Animation:      fadeInUp 0.3s ease

Warning Icon:
  Size:           20px × 20px
  Color:          #FFFFFF

Warning Text:
  Font:           14px / SemiBold (600)
  Color:          #FFFFFF
  Margin Left:    8px
```

## 🔍 Icons & Symbols

### Icon Sizes
```
Small:      16px × 16px  (버튼 내부, 인라인)
Medium:     20px × 20px  (기본 오버레이)
Large:      24px × 24px  (빌드 오더, 강조)
XLarge:     32px × 32px  (설정 패널)
```

### Icon Colors
```
Active:         #FFFFFF  (활성 상태)
Inactive:       #6B6B6B  (비활성)
Success:        #00D084  (완료/성공)
Warning:        #FFB800  (주의/진행)
Error:          #FF3B30  (오류/위험)
```

### Building Icons (24px)
```
Protoss:
  Gateway:      🏢  (#FFB800 → #00D084)
  Cybernetics:  🔬  (#FFB800 → #00D084)
  Forge:        ⚒️   (#FFB800 → #00D084)
  Pylon:        ⚡  (#FFB800 → #00D084)

Terran:
  Barracks:     🏭  (#FFB800 → #00D084)
  Factory:      🔧  (#FFB800 → #00D084)
  Supply:       📦  (#FFB800 → #00D084)

Zerg:
  Hatchery:     🥚  (#FFB800 → #00D084)
  Spawning:     🕳️   (#FFB800 → #00D084)
```

## 🖼️ Layout Patterns

### Overlay Layout Grid
```
Game Screen: 1920 × 1080 (기준)

Safe Zones:
  Top Left:     20px margin from edges
  Top Right:    20px margin from edges
  Bottom Left:  20px margin from edges
  Bottom Right: 20px margin from edges

Avoid Areas:
  Center:       400px × 300px (주요 게임 화면)
  Minimap:      Bottom Right 200px × 200px
```

### Settings Window Layout
```
Window Size:      800px × 600px (기본)
Sidebar Width:    200px
Content Width:    580px
Header Height:    60px
Footer Height:    80px

Grid:             20px baseline grid
Margins:          20px all sides
```

## 🎭 Animation & Transitions

### Transition Timing
```
Fast:       0.15s  (버튼 호버, 포커스)
Normal:     0.3s   (모달, 드롭다운)
Slow:       0.5s   (페이지 전환)
```

### Easing Functions
```
Default:        ease-out
Entrance:       cubic-bezier(0.0, 0.0, 0.2, 1)
Exit:           cubic-bezier(0.4, 0.0, 1, 1)
```

### Animation Examples
```
Fade In:        opacity 0 → 1, 0.3s ease-out
Slide Up:       transform translateY(20px) → 0, 0.3s ease-out
Scale In:       transform scale(0.95) → 1, 0.15s ease-out
```

## 📱 Responsive Breakpoints

### Screen Sizes
```
Small:      1366 × 768   (최소 지원)
Medium:     1600 × 900   (일반적)
Large:      1920 × 1080  (기준)
XLarge:     2560 × 1440  (고해상도)
```

### Scaling Rules
```
1366px:     0.8x scale
1600px:     0.9x scale  
1920px:     1.0x scale (기준)
2560px:     1.2x scale
```

## ✅ Accessibility

### Color Contrast
```
Text on Background:     4.5:1 (AA 기준)
Large Text:             3:1 (AA 기준)
UI Components:          3:1 (AA 기준)
```

### Focus States
```
Keyboard Focus:         2px solid #0099FF outline
Offset:                 2px
Border Radius:          4px
```

### Text Requirements
```
Minimum Size:           12px
Reading Distance:       60cm (게임 환경 고려)
Line Height:            1.4 이상
```