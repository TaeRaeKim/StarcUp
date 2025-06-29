# Git Commit Guidelines

## 개요
StarcUp 프로젝트의 일관된 커밋 메시지 작성을 위한 가이드라인입니다.

## 커밋 메시지 구조
```
[Prefix] 간결한 제목 (50자 이내)

선택사항: 상세 설명
- 변경 이유
- 구현 방식
- 주의사항 등
```

## Prefix 종류

### 🚀 기능 개발
- **`[Feat]`** - 새로운 기능 추가
  ```
  [Feat] Add interactive unit test UI to ControlForm
  [Feat] Implement real-time unit search functionality
  ```

### 🐛 버그 수정
- **`[Fix]`** - 버그 수정
  ```
  [Fix] Correct Unit class property names in display
  [Fix] Resolve TEB address calculation error
  ```

### ⚡ 성능 개선
- **`[Perf]`** - 성능 최적화
  ```
  [Perf] Cache InGame address calculation in InGameDetector
  [Perf] Optimize memory reading with buffer pooling
  ```

### 🔧 리팩토링
- **`[Refactor]`** - 코드 구조 개선 (기능 변경 없음)
  ```
  [Refactor] Extract unit filtering logic to separate service
  [Refactor] Simplify error handling in MemoryService
  ```

### 💄 스타일 개선
- **`[Style]`** - 코드 포맷팅, 네이밍 등
  ```
  [Style] Apply consistent naming convention to variables
  [Style] Format code according to project standards
  ```

### 📚 문서화
- **`[Docs]`** - 문서 작성/수정
  ```
  [Docs] Add unit testing guide to README
  [Docs] Update API documentation for UnitService
  ```

### 🧪 테스트
- **`[Test]`** - 테스트 코드 추가/수정
  ```
  [Test] Add unit tests for InGameDetector
  [Test] Improve coverage for MemoryService methods
  ```

### 🔧 기타 작업
- **`[Chore]`** - 빌드, 설정, 의존성 등
  ```
  [Chore] Update project dependencies
  [Chore] Configure build settings for release
  ```

### 🗑️ 제거/변경
- **`[Remove]`** - 코드/파일 삭제
  ```
  [Remove] Delete unused legacy memory reader
  [Remove] Clean up obsolete test files
  ```

- **`[Rename]`** - 파일명/클래스명 변경
  ```
  [Rename] Change UnitDetector to UnitService
  [Rename] Move utility classes to Common namespace
  ```

## 작성 가이드라인

### ✅ 좋은 예시
```
[Feat] Add InGame-only unit test UI with player selection

- NumericUpDown for player index (0-7)
- ComboBox with popular unit types
- Real-time enabling based on InGame status
- Display unit details including position and HP
```

### ❌ 피해야 할 예시
```
update code           // prefix 없음, 모호함
[Fix] fix bug         // 어떤 버그인지 불명확
[Feat] 한글 제목      // 영어 사용 권장
```

### 제목 작성 규칙
1. **50자 이내**로 간결하게
2. **영어 사용** 권장
3. **동사 시작** (Add, Fix, Update, Remove 등)
4. **현재형** 사용
5. **마침표 사용 금지**

### 본문 작성 (선택사항)
- 제목과 한 줄 띄우기
- **왜** 변경했는지 설명
- **어떻게** 구현했는지 설명
- 관련 이슈 번호 참조 (`#123`)

## 커밋 빈도
- **작은 단위**로 자주 커밋
- **하나의 변경사항**당 하나의 커밋
- **완성된 기능** 단위로 커밋

## 예시 시나리오

### 새 기능 개발 시퀀스
```
[Feat] Add basic unit test UI structure
[Feat] Implement player index selection
[Feat] Add unit type dropdown with popular units
[Feat] Connect UI with UnitService for data retrieval
[Test] Add unit tests for new UI components
[Docs] Update README with unit test feature guide
```

### 버그 수정 시퀀스
```
[Fix] Resolve TEB address calculation returning wrong values
[Test] Add test cases for TEB address edge cases
[Docs] Document TEB address calculation process
```

### 성능 개선 시퀀스
```
[Perf] Cache InGame address to avoid repeated calculations
[Test] Add performance benchmarks for InGame detection
[Docs] Document performance improvements in CHANGELOG
```

## 도구 및 자동화
- **IDE Plugin**: GitMoji, Conventional Commits 확장 사용 권장
- **Pre-commit Hook**: 커밋 메시지 형식 검증
- **Template**: `.gitmessage` 파일로 템플릿 설정

---

## 참고 자료
- [Conventional Commits](https://www.conventionalcommits.org/)
- [Angular Commit Guidelines](https://github.com/angular/angular/blob/main/CONTRIBUTING.md#commit)
- [How to Write a Git Commit Message](https://chris.beams.io/posts/git-commit/)