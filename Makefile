# StarcUp 프로젝트 Makefile
# 사용법: make [target]

.PHONY: help build build-core build-ui build-legacy build-test run-ui run-legacy test clean restore dev-setup

# 기본 타겟
help:
	@echo "StarcUp 프로젝트 명령어:"
	@echo "  build          - 전체 솔루션 빌드"
	@echo "  build-core     - StarcUp.Core 프로젝트만 빌드"
	@echo "  build-ui       - StarcUp.UI (Electron) 프로젝트만 빌드"
	@echo "  build-legacy   - StarcUp (Windows Forms) 프로젝트만 빌드"
	@echo "  build-test     - StarcUp.Test 프로젝트만 빌드"
	@echo "  run-ui         - UI (Electron) 앱 실행"
	@echo "  run-legacy     - Windows Forms 앱 실행 명령어 표시"
	@echo "  test           - 테스트 실행"
	@echo "  clean          - 빌드 출력 정리"
	@echo "  restore        - NuGet 패키지 복원"
	@echo "  dev-setup      - 개발 환경 설정"
	@echo ""
	@echo "참고: 실행은 사용자가 직접 수행합니다."

# 빌드 명령어들
build:
	dotnet build StarcUp.sln
	cd StracUp.UI && npm install && npm run build

build-core:
	dotnet build StarcUp.Core/StarcUp.Core.csproj

build-ui:
	cd StracUp.UI && npm install && npm run build

build-legacy:
	dotnet build StarcUp/StarcUp.csproj

build-test:
	dotnet build StarcUp.Test/StarcUp.Test.csproj

# 실행 명령어들
run-ui:
	cd StracUp.UI && npm start

run-legacy:
	dotnet run --project StarcUp/StarcUp.csproj

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
	@echo "개발 환경 설정 중..."
	dotnet restore StarcUp.sln
	dotnet build StarcUp.sln
	@echo "개발 환경 설정 완료!"