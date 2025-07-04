---
name: "Task ⚡"
about: "Story를 구성하는 구체적인 작업 템플릿"
title: "[Task] "
labels: "task"
assignees: ""
body:
  - type: markdown
    attributes:
      value: |
        # Task ⚡
        Task를 생성하기 전에 반드시 소속될 Story가 존재해야 합니다.
  - type: input
    id: story-reference
    attributes:
      label: "📖 소속 Story (필수)"
      description: "이 Task가 속할 Story의 이슈 번호를 입력하세요"
      placeholder: "예: #15"
    validations:
      required: true
  - type: textarea
    id: task-overview
    attributes:
      label: "📋 Task 개요 (필수)"
      description: "이 Task의 목적과 구체적인 작업 내용을 설명해주세요"
      placeholder: "Task의 목적과 구체적인 작업 내용을 설명해주세요"
    validations:
      required: true
  - type: checkboxes
    id: task-type
    attributes:
      label: "🎯 Task 유형 (필수)"
      description: "해당하는 항목을 선택하세요"
      options:
        - label: "🚀 새로운 기능 구현 (Feature)"
        - label: "🐛 버그 수정 (Bug Fix)"
        - label: "⚡ 성능 개선 (Performance)"
        - label: "🔧 리팩토링 (Refactor)"
        - label: "📚 문서 작업 (Documentation)"
        - label: "🧪 테스트 (Test)"
        - label: "🔧 기타 (Chore)"
  - type: dropdown
    id: difficulty
    attributes:
      label: "🔧 난이도"
      description: "이 Task의 예상 난이도를 선택하세요"
      options:
        - Easy
        - Medium
        - Hard
    validations:
      required: true
  - type: input
    id: estimated-time
    attributes:
      label: "⏱️ 예상 작업 시간"
      description: "이 Task에 소요될 것으로 예상되는 시간을 입력하세요"
      placeholder: "예: 2-3 시간"
  - type: textarea
    id: detailed-description
    attributes:
      label: "🔍 상세 설명 (필수)"
      description: "구체적인 작업 내용을 설명해주세요"
      placeholder: |
        ### 현재 상황
        - 현재 문제점이나 개선이 필요한 부분
        
        ### 목표
        - 이 Task를 통해 달성하고자 하는 목표
        
        ### 해결 방안
        - 어떻게 접근할 것인지 구체적인 계획
    validations:
      required: true
  - type: checkboxes
    id: definition-of-done
    attributes:
      label: "✅ 완료 조건 (Definition of Done)"
      description: "이 Task가 완료되었다고 판단할 수 있는 구체적인 조건들"
      options:
        - label: "기능 구현 완료"
        - label: "단위 테스트 작성 및 통과"
        - label: "빌드 성공 (`make build` 또는 `dotnet build`)"
        - label: "문서 업데이트 (필요한 경우)"
  - type: textarea
    id: technical-considerations
    attributes:
      label: "💭 기술적 고려사항"
      description: "Task 수행 시 특별히 주의해야 할 점이나 추가 정보"
      placeholder: |
        ### 변경 영향 범위
        - 변경 대상 파일:
        - 영향 받는 컴포넌트:
        - 테스트 파일:
        
        ### 테스트 전략
        - 단위 테스트 작성 필요: Yes/No
        - 통합 테스트 필요: Yes/No
        - 수동 테스트 시나리오 필요: Yes/No
  - type: textarea
    id: dependencies
    attributes:
      label: "🔗 의존성 및 관련 항목"
      description: "다른 Task나 이슈와의 관계를 입력하세요"
      placeholder: |
        - 선행 Task: #00
        - 후속 Task: #00
        - 의존성: #00
---

# Task ⚡

## 📋 Task 개요
<!-- 이 Task의 목적과 구체적인 작업 내용을 설명해주세요 -->

## 🎯 Task 유형
<!-- 해당하는 항목에 'x'를 표시해주세요 -->
- [ ] 🚀 새로운 기능 구현 (Feature)
- [ ] 🐛 버그 수정 (Bug Fix)
- [ ] ⚡ 성능 개선 (Performance)
- [ ] 🔧 리팩토링 (Refactor)
- [ ] 📚 문서 작업 (Documentation)
- [ ] 🧪 테스트 (Test)
- [ ] 🔧 기타 (Chore)

## 📖 소속 Story
<!-- 이 Task가 속한 Story -->
- 소속 Story: #00 - [Story 제목]

## 📅 예상 작업 시간
- 예상 시간: ___ 시간
- 우선순위: High / Medium / Low
- 난이도: Easy / Medium / Hard

## 🔍 상세 설명
<!-- 구체적인 작업 내용을 설명해주세요 -->

### 현재 상황
<!-- 현재 문제점이나 개선이 필요한 부분 -->

### 목표
<!-- 이 Task를 통해 달성하고자 하는 목표 -->

### 해결 방안
<!-- 어떻게 접근할 것인지 구체적인 계획 -->

## ✅ 완료 조건 (Definition of Done)
<!-- 이 Task가 완료되었다고 판단할 수 있는 구체적인 조건들 -->
- [ ] 기능 구현 완료
- [ ] 단위 테스트 작성 및 통과
- [ ] 빌드 성공 (`make build` 또는 `dotnet build`)
- [ ] 코드 리뷰 완료
- [ ] 문서 업데이트 (필요한 경우)

## 💭 추가 고려사항
<!-- Task 수행 시 특별히 주의해야 할 점이나 추가 정보 -->

### 기술적 고려사항
- [ ] 메모리 안전성 고려
- [ ] 스레드 안전성 고려
- [ ] 성능 영향도 평가
- [ ] 호환성 고려

### 변경 영향 범위
<!-- 이 Task로 인해 영향을 받을 수 있는 파일이나 컴포넌트 -->
- 변경 대상 파일: 
- 영향 받는 컴포넌트:
- 테스트 파일:

### 테스트 전략
- [ ] 단위 테스트 작성 필요
- [ ] 통합 테스트 필요
- [ ] 수동 테스트 시나리오 정의
- [ ] 성능 테스트 필요

## 📚 참고 자료
<!-- Task 관련 참고 자료, 외부 링크 -->

## 🔗 관련 항목
<!-- 다른 Task나 이슈와의 관계 -->
- 선행 Task: #00
- 후속 Task: #00
- 의존성: #00