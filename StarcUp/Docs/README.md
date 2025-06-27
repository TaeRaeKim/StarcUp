# StarcUp 문서

StarcUp 프로젝트의 문서 모음입니다.

## 📁 폴더 구조

### 🏗️ `Architecture/`
프로젝트 아키텍처 관련 문서
- UML 다이어그램 (클래스, 시퀀스 등)
- 시스템 설계 문서
- 컴포넌트 구조도

### 🛠️ `Development/`
개발 관련 가이드라인 및 규칙
- [commit-guidelines.md](Development/commit-guidelines.md) - Git 커밋 메시지 작성 가이드라인
- 코딩 스타일 가이드
- 개발 환경 설정
- 테스트 가이드

### 📋 `PullRequests/`
Pull Request 관련 문서
- `archive/` - 과거 PR 문서들의 아카이브
- PR 템플릿
- 리뷰 가이드라인

## 📚 문서 작성 규칙

1. **한국어 우선**: 내부 문서는 한국어로 작성
2. **Markdown 사용**: 모든 문서는 `.md` 형식으로 작성
3. **명확한 제목**: 문서 목적이 명확하게 드러나는 제목 사용
4. **적절한 분류**: 위 폴더 구조에 맞게 문서 분류

## 🔄 문서 업데이트

- 주요 아키텍처 변경 시 `Architecture/` 폴더 문서 업데이트
- 새로운 개발 규칙 추가 시 `Development/` 폴더에 문서 추가
- PR 과정에서 생성된 문서는 `PullRequests/archive/`에 정리