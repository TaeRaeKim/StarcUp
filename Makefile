# StarcUp 프로젝트 Makefile
# 사용법: make [target]

.PHONY: help build build-core build-ui run-ui run-forms test clean restore

# 기본 타겟
help:
	@echo "StarcUp 프로젝트 명령어:"
	@echo "  build        - 전체 솔루션 빌드"
	@echo "  build-core   - StarcUp.Core 프로젝트만 빌드"
	@echo "  build-ui     - StarcUp.UI 프로젝트만 빌드"
	@echo "  run-ui       - WinUI 3 앱 실행"
	@echo "  run-forms    - Windows Forms 앱 실행"
	@echo "  test         - 테스트 실행"
	@echo "  clean        - 빌드 출력 정리"
	@echo "  restore      - NuGet 패키지 복원"

# 빌드 명령어들
build:
	ssh Taerae@main "dotnet build StarcUp/StarcUp.sln"

build-core:
	ssh Taerae@main "dotnet build StarcUp/StarcUp.Core/StarcUp.Core.csproj"

build-ui:
	ssh Taerae@main "dotnet build StarcUp/StarcUp.UI/StarcUp.UI.csproj"

# 실행 명령어들
run-ui:
	ssh Taerae@main "dotnet run --project StarcUp/StarcUp.UI/StarcUp.UI.csproj"

run-forms:
	ssh Taerae@main "dotnet run --project StarcUp/StarcUp/StarcUp.csproj"

# 테스트
test:
	ssh Taerae@main "dotnet test StarcUp/StarcUp.Test/StarcUp.Test.csproj"

# 정리 명령어들
clean:
	ssh Taerae@main "dotnet clean StarcUp/StarcUp.sln"

restore:
	ssh Taerae@main "dotnet restore StarcUp/StarcUp.sln"

# 개발 환경 설정
dev-setup:
	@echo "개발 환경 설정 중..."
	ssh Taerae@main "dotnet restore StarcUp/StarcUp.sln"
	ssh Taerae@main "dotnet build StarcUp/StarcUp.sln"
	@echo "개발 환경 설정 완료!"