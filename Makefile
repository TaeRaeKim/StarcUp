# StarcUp 프로젝트 Makefile
# 사용법: make [target]

.PHONY: help build build-core build-ui build-test run-ui test clean restore dev-setup release-core release korean-test

# 기본 타겟
help:
	@printf "StarcUp 프로젝트 명령어:"
	@printf "  build          - 전체 솔루션 빌드"
	@printf "  build-core     - StarcUp.Core 프로젝트만 빌드"
	@printf "  build-ui       - StarcUp.UI (Electron) 프로젝트만 빌드"
	@printf "  build-test     - StarcUp.Test 프로젝트만 빌드"
	@printf "  run-ui         - UI (Electron) 앱 실행"
	@printf "  test           - 테스트 실행"
	@printf "  clean          - 빌드 출력 정리"
	@printf "  restore        - NuGet 패키지 복원"
	@printf "  dev-setup      - 개발 환경 설정"
	@printf "  release-core   - StarcUp.Core 릴리스 빌드 (최적화 포함)"
	@printf "  release        - 전체 릴리스 빌드 (Core + UI)"
	@printf ""
	@printf "참고: 실행은 사용자가 직접 수행합니다."

# 빌드 명령어들
build:
	dotnet build StarcUp.sln
	cd StarcUp.UI && npm install && npm run build

build-core:
	dotnet build StarcUp.Core/StarcUp.Core.csproj

build-ui:
	cd StarcUp.UI && npm install && npm run build

build-test:
	dotnet build StarcUp.Test/StarcUp.Test.csproj

# 실행 명령어들
run-ui:
	cd StarcUp.UI && npm start

# 테스트
test:
	dotnet test StarcUp.Test/StarcUp.Test.csproj

# 정리 명령어들
clean:
	dotnet clean StarcUp.sln

restore:
	dotnet restore StarcUp.sln

# 개발 환경 설정
dev-setup:
	@printf "개발 환경 설정 중..."
	dotnet restore StarcUp.sln
	dotnet build StarcUp.sln
	@printf "개발 환경 설정 완료!"

# 릴리스 빌드
release-core:
	@printf "StarcUp.Core 릴리스 빌드 중..."
	dotnet build StarcUp.Core/StarcUp.Core.csproj -c Release --no-restore
	@printf "릴리스 빌드 완료!"

release: release-core
	@printf "전체 릴리스 빌드 중..."
	cd StarcUp.UI && npm run release
	@printf "전체 릴리스 빌드 완료!"
