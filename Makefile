.PHONY: help restore build test up down logs smoke demo ci docker-build migrate preflight format

BASE_URL ?= http://localhost:5000

help:
	@echo "FieldOps make targets:"
	@echo "  make restore      - restore NuGet packages"
	@echo "  make build        - build solution (Release)"
	@echo "  make test         - run unit + integration tests"
	@echo "  make format       - apply dotnet format"
	@echo "  make preflight    - format verify + ci (pre-push gate)"
	@echo "  make up           - docker compose pull + up, wait for API"
	@echo "  make down         - stop compose stack"
	@echo "  make logs         - tail API logs"
	@echo "  make smoke        - API smoke test (stack must be up)"
	@echo "  make demo         - up + smoke + interview talking points"
	@echo "  make migrate      - apply EF Core migrations"
	@echo "  make ci           - local CI: restore + build + test"
	@echo "  make docker-build - build local API image"

restore:
	dotnet restore FieldOps.sln

build:
	dotnet build FieldOps.sln -c Release

test:
	dotnet test FieldOps.sln -c Release --verbosity minimal

format:
	dotnet format FieldOps.sln

preflight:
	chmod +x scripts/*.sh
	./scripts/preflight.sh

ci: restore build test

up:
	chmod +x scripts/*.sh
	./scripts/dev-up.sh

down:
	docker compose down

logs:
	docker compose logs -f api

smoke:
	chmod +x scripts/*.sh
	./scripts/smoke-test.sh $(BASE_URL)

demo:
	chmod +x scripts/*.sh
	./scripts/interview-demo.sh

migrate:
	chmod +x scripts/*.sh
	./scripts/migrate.sh

docker-build:
	docker build -t mirzasaikatahmmed/fieldops-api:latest .
