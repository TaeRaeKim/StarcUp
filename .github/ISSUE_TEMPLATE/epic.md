---
name: "Epic 📊"
about: "큰 규모의 기능이나 프로젝트 목표를 위한 Epic 템플릿"
title: "[Epic] "
labels: "epic"
assignees: ""
body:
  - type: markdown
    attributes:
      value: |
        # Epic 📊
        Epic은 큰 규모의 기능이나 프로젝트 목표를 나타냅니다. 여러 개의 Task로 구성되며, Story와 연관될 수 있습니다.
  - type: textarea
    id: epic-overview
    attributes:
      label: "🎯 Epic 개요 (필수)"
      description: "이 Epic의 목적과 전체적인 비전을 설명해주세요"
      placeholder: "Epic의 목적과 전체적인 비전을 설명해주세요"
    validations:
      required: true
  - type: textarea
    id: business-value
    attributes:
      label: "🎯 비즈니스 가치 (필수)"
      description: "이 Epic이 제공하는 비즈니스 가치 또는 사용자 가치를 설명해주세요"
      placeholder: "이 Epic이 제공하는 비즈니스 가치 또는 사용자 가치를 설명해주세요"
    validations:
      required: true
  - type: textarea
    id: detailed-description
    attributes:
      label: "🔍 상세 설명 (필수)"
      description: "Epic의 배경, 목표, 전체적인 접근 방법을 설명해주세요"
      placeholder: |
        ### 배경
        - 왜 이 Epic이 필요한지
        
        ### 목표
        - 이 Epic을 통해 달성하고자 하는 목표
        
        ### 범위
        - 이 Epic에 포함되는 것과 포함되지 않는 것
    validations:
      required: true
  - type: input
    id: start-date
    attributes:
      label: "📅 시작 예정일"
      description: "Epic 시작 예정일을 입력하세요"
      placeholder: "YYYY-MM-DD"
  - type: input
    id: end-date
    attributes:
      label: "📅 완료 예정일"
      description: "Epic 완료 예정일을 입력하세요"
      placeholder: "YYYY-MM-DD"
  - type: checkboxes
    id: definition-of-done
    attributes:
      label: "✅ 완료 조건 (Definition of Done)"
      description: "이 Epic이 완료되었다고 판단할 수 있는 조건들"
      options:
        - label: "모든 하위 Task 완료"
        - label: "전체 기능 통합 테스트 완료"
        - label: "사용자 인수 테스트 완료"
        - label: "문서 업데이트 완료"
        - label: "성능 및 안정성 검증 완료"
  - type: textarea
    id: technical-considerations
    attributes:
      label: "💭 기술적 고려사항"
      description: "Epic 전반에 걸친 고려사항, 제약사항, 위험요소"
      placeholder: |
        ### 기술적 고려사항
        - 아키텍처 설계 고려사항
        - 성능 요구사항
        - 보안 고려사항
        - 호환성 고려사항
        
        ### 위험 요소
        - 이 Epic 진행 중 발생할 수 있는 위험 요소들
  - type: textarea
    id: dependencies
    attributes:
      label: "🔗 의존성 및 관련 항목"
      description: "다른 Epic이나 이슈와의 관계를 입력하세요"
      placeholder: |
        - 선행 Epic: #00
        - 후속 Epic: #00
        - 관련 이슈: #00
---

# Epic 📊

## 🎯 Epic 개요
<!-- 이 Epic의 목적과 전체적인 비전을 설명해주세요 -->

## 🎯 비즈니스 가치
<!-- 이 Epic이 제공하는 비즈니스 가치 또는 사용자 가치 -->

## 📋 포함될 Story 목록
<!-- 이 Epic을 구성할 Story들의 목록 -->
- [ ] #00 - [Story 제목]
- [ ] #00 - [Story 제목]
- [ ] #00 - [Story 제목]

## 📅 예상 일정
- 시작 예정일: YYYY-MM-DD
- 완료 예정일: YYYY-MM-DD
- 우선순위: High / Medium / Low

## 🔍 상세 설명
<!-- Epic의 배경, 목표, 전체적인 접근 방법 -->

### 배경
<!-- 왜 이 Epic이 필요한지 -->

### 목표
<!-- 이 Epic을 통해 달성하고자 하는 목표 -->

### 범위
<!-- 이 Epic에 포함되는 것과 포함되지 않는 것 -->

## ✅ 완료 조건 (Definition of Done)
<!-- 이 Epic이 완료되었다고 판단할 수 있는 조건들 -->
- [ ] 모든 하위 Story 완료
- [ ] 전체 기능 통합 테스트 완료
- [ ] 사용자 인수 테스트 완료
- [ ] 문서 업데이트 완료

## 💭 추가 고려사항
<!-- Epic 전반에 걸친 고려사항, 제약사항, 위험요소 -->

### 기술적 고려사항
- [ ] 아키텍처 설계 필요
- [ ] 성능 요구사항 고려
- [ ] 보안 고려사항
- [ ] 호환성 고려사항

### 위험 요소
<!-- 이 Epic 진행 중 발생할 수 있는 위험 요소들 -->

## 📚 참고 자료
<!-- Epic 관련 참고 자료, 외부 링크 -->

## 🔗 관련 항목
<!-- 다른 Epic이나 이슈와의 관계 -->
- 선행 Epic: #00
- 후속 Epic: #00
- 관련 이슈: #00