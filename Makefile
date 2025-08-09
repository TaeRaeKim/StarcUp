# StarcUp ������Ʈ Makefile
# ����: make [target]

.PHONY: help build build-core build-ui build-test run-ui test clean restore dev-setup release-core release korean-test

# �⺻ Ÿ��
help:
	@printf "StarcUp ������Ʈ ��ɾ�:"
	@printf "  build          - ��ü �ַ�� ����"
	@printf "  build-core     - StarcUp.Core ������Ʈ�� ����"
	@printf "  build-ui       - StarcUp.UI (Electron) ������Ʈ�� ����"
	@printf "  build-test     - StarcUp.Test ������Ʈ�� ����"
	@printf "  run-ui         - UI (Electron) �� ����"
	@printf "  test           - �׽�Ʈ ����"
	@printf "  clean          - ���� ��� ����"
	@printf "  restore        - NuGet ��Ű�� ����"
	@printf "  dev-setup      - ���� ȯ�� ����"
	@printf "  release-core   - StarcUp.Core ������ ���� (����ȭ ����)"
	@printf "  release        - ��ü ������ ���� (Core + UI)"
	@printf ""
	@printf "����: ������ ����ڰ� ���� �����մϴ�."

# ���� ��ɾ��
build:
	dotnet build StarcUp.sln
	cd StarcUp.UI && npm install && npm run build

build-core:
	dotnet build StarcUp.Core/StarcUp.Core.csproj

build-ui:
	cd StarcUp.UI && npm install && npm run build

build-test:
	dotnet build StarcUp.Test/StarcUp.Test.csproj

# ���� ��ɾ��
run-ui:
	cd StarcUp.UI && npm start

# �׽�Ʈ
test:
	dotnet test StarcUp.Test/StarcUp.Test.csproj

# ���� ��ɾ��
clean:
	dotnet clean StarcUp.sln

restore:
	dotnet restore StarcUp.sln

# ���� ȯ�� ����
dev-setup:
	@printf "���� ȯ�� ���� ��..."
	dotnet restore StarcUp.sln
	dotnet build StarcUp.sln
	@printf "���� ȯ�� ���� �Ϸ�!"

# ������ ����
release-core:
	@printf "StarcUp.Core ������ ���� ��..."
	dotnet build StarcUp.Core/StarcUp.Core.csproj -c Release --no-restore
	@printf "������ ���� �Ϸ�!"

release: release-core
	@printf "��ü ������ ���� ��..."
	cd StarcUp.UI && npm run release
	@printf "��ü ������ ���� �Ϸ�!"
