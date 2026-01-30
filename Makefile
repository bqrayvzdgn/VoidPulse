.PHONY: dev dev-down build test restore clean migrate logs

# Start all services in development mode
dev:
	docker compose up -d

# Stop all services
dev-down:
	docker compose down

# Build all Docker images
build:
	docker compose build

# Run tests
test:
	dotnet test src/backend/

# Restore NuGet packages
restore:
	dotnet restore src/backend/

# Clean build artifacts
clean:
	dotnet clean src/backend/

# Run database migrations (placeholder)
migrate:
	@echo "TODO: Add migration command, e.g.:"
	@echo "  dotnet ef database update --project src/backend/VoidPulse.Infrastructure --startup-project src/backend/VoidPulse.Api"

# Tail logs from all services
logs:
	docker compose logs -f
