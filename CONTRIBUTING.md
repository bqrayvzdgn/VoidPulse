# Contributing to VoidPulse

Thank you for your interest in contributing to VoidPulse. This document provides guidelines and instructions for contributing.

## Development Setup

### Prerequisites

- .NET 10 SDK
- Docker and Docker Compose
- Git
- A code editor with C# support (Visual Studio, VS Code with C# Dev Kit, or Rider)

### Getting Started

1. Fork the repository and clone your fork:

```bash
git clone https://github.com/your-username/VoidPulse.git
cd VoidPulse
```

2. Start infrastructure services:

```bash
docker compose up -d db redis
```

3. Restore dependencies:

```bash
dotnet restore src/backend/
```

4. Copy the environment file:

```bash
cp .env.example .env
```

5. Run database migrations:

```bash
dotnet ef database update \
  --project src/backend/VoidPulse.Infrastructure \
  --startup-project src/backend/VoidPulse.Api
```

6. Run the API:

```bash
dotnet run --project src/backend/VoidPulse.Api
```

7. Verify everything works:

```bash
curl http://localhost:8080/api/v1/health
```

## Branch Naming

Use the following prefixes for branches:

| Prefix | Purpose | Example |
|--------|---------|---------|
| `feature/` | New features | `feature/pcap-parser` |
| `fix/` | Bug fixes | `fix/login-token-expiry` |
| `refactor/` | Code refactoring | `refactor/dashboard-queries` |
| `docs/` | Documentation updates | `docs/api-reference` |

Always branch from `develop`:

```bash
git checkout develop
git pull origin develop
git checkout -b feature/your-feature-name
```

## Commit Messages

This project follows [Conventional Commits](https://www.conventionalcommits.org/). Every commit message must follow this format:

```
<type>(<scope>): <description>

[optional body]

[optional footer]
```

### Types

| Type | Description |
|------|-------------|
| `feat` | A new feature |
| `fix` | A bug fix |
| `refactor` | Code change that neither fixes a bug nor adds a feature |
| `docs` | Documentation only changes |
| `test` | Adding or updating tests |
| `chore` | Maintenance tasks (deps, CI, build) |
| `perf` | Performance improvement |
| `style` | Code style changes (formatting, semicolons, etc.) |

### Scopes

Use the layer or component name as the scope:

- `api` -- Controller and middleware changes
- `domain` -- Entity and interface changes
- `app` -- Application service and DTO changes
- `infra` -- Repository, EF Core, Redis, JWT changes
- `test` -- Test changes
- `devops` -- Docker, K8s, CI/CD changes

### Examples

```
feat(api): add PCAP upload endpoint with file size validation

fix(infra): correct refresh token expiration check in JwtService

refactor(app): extract dashboard aggregation into repository queries

test(app): add unit tests for TrafficService ingestion logic

docs: update API reference with export endpoint details

chore(devops): upgrade PostgreSQL image from 16.1 to 16.2
```

## Pull Request Process

1. **Create a feature branch** from `develop` using the naming convention above.

2. **Make your changes** following the code style guidelines below.

3. **Write tests** for all new functionality. Ensure existing tests still pass.

4. **Run the full test suite** before submitting:

```bash
dotnet test src/backend/ --verbosity normal
```

5. **Push your branch** and open a pull request against `develop`:

```bash
git push origin feature/your-feature-name
```

6. **Fill out the PR template** with:
   - A clear description of the change
   - Related issue numbers (e.g., `Closes #42`)
   - Any breaking changes
   - Testing steps for reviewers

7. **Address review feedback** promptly. Push additional commits to your branch (do not force-push during review).

8. **Squash and merge** will be used when merging to `develop`.

### PR Review Checklist

Reviewers will check for:

- [ ] Code follows the project style guidelines
- [ ] New code has appropriate test coverage
- [ ] All tests pass
- [ ] No security vulnerabilities introduced
- [ ] API changes are backward-compatible (or breaking changes are documented)
- [ ] Database migrations are correct and reversible
- [ ] No secrets or credentials in the code

## Code Style

### C# Conventions

- Use **file-scoped namespaces** (`namespace Foo;` instead of `namespace Foo { }`)
- Use **records** for DTOs and value objects
- Use **primary constructors** where appropriate
- Follow [Microsoft's C# coding conventions](https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions)
- Use `var` when the type is obvious from the right side of the assignment
- Prefer `async/await` over `.Result` or `.Wait()`
- Use `CancellationToken` in async methods where applicable

### Naming

| Element | Convention | Example |
|---------|-----------|---------|
| Classes, Records | PascalCase | `TrafficFlowResponse` |
| Interfaces | IPascalCase | `ITrafficService` |
| Methods | PascalCase | `GetByIdAsync` |
| Properties | PascalCase | `SourceIp` |
| Private fields | _camelCase | `_trafficService` |
| Local variables | camelCase | `tenantId` |
| Constants | PascalCase | `MaxPageSize` |

### Architecture Rules

- **Domain layer** has no dependencies on other layers
- **Application layer** depends only on Domain
- **Infrastructure layer** depends on Domain and Application
- **API layer** depends on Application (and registers Infrastructure via DI)
- Controllers should be thin -- delegate to services
- Services contain business logic
- Repositories handle data access

### File Organization

- One class/record/interface per file
- DTOs go in `Application/DTOs/{Feature}/`
- Validators go in `Application/Validators/`
- Entity configurations go in `Infrastructure/Data/Configurations/`

## Testing Requirements

- **Minimum coverage target**: 80% for the Application layer
- **Unit tests**: Required for all services and validators
- **Integration tests**: Required for all controller endpoints
- Use **xUnit** as the test framework
- Use **Moq** for mocking dependencies
- Use **FluentAssertions** for readable assertions
- Use **Bogus** for test data generation

### Test File Naming

- Unit tests: `{ClassName}Tests.cs` in `Tests/Unit/{Layer}/`
- Integration tests: `{ControllerName}Tests.cs` in `Tests/Integration/Controllers/`

### Example Test Structure

```csharp
public class AuthServiceTests
{
    private readonly Mock<IUserRepository> _userRepo;
    private readonly AuthService _sut;

    public AuthServiceTests()
    {
        _userRepo = new Mock<IUserRepository>();
        _sut = new AuthService(_userRepo.Object, /* ... */);
    }

    [Fact]
    public async Task LoginAsync_WithValidCredentials_ReturnsAuthResponse()
    {
        // Arrange
        // ...

        // Act
        var result = await _sut.LoginAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.AccessToken.Should().NotBeEmpty();
    }
}
```

## Reporting Issues

When filing an issue, include:

- A clear title and description
- Steps to reproduce (for bugs)
- Expected vs. actual behavior
- Environment details (.NET version, OS, Docker version)
- Relevant logs or error messages

## Questions?

If you have questions about contributing, open a GitHub Discussion or reach out to the maintainers.
