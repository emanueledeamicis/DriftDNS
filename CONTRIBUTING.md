# Contributing to DriftDNS

Thank you for your interest in contributing! Here's everything you need to get started.

---

## Ways to contribute

- Report bugs via [GitHub Issues](https://github.com/emanueledeamicis/DriftDNS/issues)
- Suggest new features or improvements
- Submit pull requests (bug fixes, new providers, UI improvements)
- Improve documentation

---

## Getting started

### Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download)
- Docker (optional, for local container testing)

### Running locally

```bash
git clone https://github.com/emanueledeamicis/DriftDNS.git
cd DriftDNS
dotnet run --project src/DriftDNS.App
```

Open **http://localhost:5000** (or the port shown in the terminal).

### Running tests

```bash
dotnet test
```

---

## Submitting a pull request

1. Fork the repository and create a branch from `main`
2. Make your changes following the conventions below
3. Add or update tests as needed
4. Open a pull request — fill in the PR template and describe what you changed and why

---

## Code conventions

- **Language**: English for all code, comments, and UI text
- **Style**: follow the existing patterns in each project layer
- **Nullable**: reference types are enabled everywhere — don't introduce nullable warnings
- **No REST API**: Blazor pages interact with `DbContext` directly, not through API controllers

## Project structure

| Project | Purpose |
|---|---|
| `DriftDNS.Core` | Domain models and interfaces — no external dependencies |
| `DriftDNS.Infrastructure` | EF Core, migrations, sync service, IP resolver |
| `DriftDNS.Providers.*` | One project per DNS provider, implements `IDnsProvider` |
| `DriftDNS.App` | Blazor Server UI, background worker, DI registration |
| `DriftDNS.Tests` | NUnit + FluentAssertions + Moq |

## Adding a new DNS provider

1. Create `src/DriftDNS.Providers.<Name>/<Name>DnsProvider.cs`
2. Implement `IDnsProvider` from `DriftDNS.Core`
3. Register in `DriftDNS.App/Program.cs`
4. Add the project reference to `DriftDNS.App.csproj` and `Dockerfile`
5. Add tests in `DriftDNS.Tests`

---

## Commit messages

Keep commit messages short and in the imperative mood:

```
Add Cloudflare provider
Fix sync not triggering on endpoint creation
Update log retention default to 24h
```
