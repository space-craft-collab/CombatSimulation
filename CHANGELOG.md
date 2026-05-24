# Changelog

All notable changes to this project will be documented
in this file.

The format is based on
[Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to
[Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Phase 0 — Foundations & ADRs (in progress)

#### Added
- Initial repo scaffolding: MIT `LICENSE`,
  `.gitignore` (.NET + Node + IDE), `README.md` stub
- `CLAUDE.md` — coding standards (.NET, English code
  + XML doc comments on public API)
- `mobile.md` — mobile-friendly output style guide
- Architecture plan (modular monolith + Orleans
  co-hosted, hot/cold path split)
- `.editorconfig` — enforces CLAUDE.md style
  (Allman, 4-space indent, file-scoped namespaces,
  naming rules, `var` when apparent, expression bodies)
- `Directory.Build.props` — solution-wide MSBuild
  defaults: `net10.0`, `LangVersion=latest`,
  `Nullable=enable`, `ImplicitUsings=enable`,
  `TreatWarningsAsErrors`, latest analyzers,
  `GenerateDocumentationFile` for public XML docs
  (relaxed for `*.Tests` projects)
- `Directory.Packages.props` — Central Package
  Management pinning Orleans 9, EF Core 10,
  ASP.NET Core 10, NLog, OpenTelemetry, xUnit +
  Testcontainers
- `docs/adr/` — ADR directory with MADR template,
  index `README.md`, and ADRs 0001–0007
  (modular monolith, Orleans co-hosted, no outbox,
  SvelteKit frontend, inter-module service
  interfaces, NLog logging, turn-based model)

#### Decided
- Stack: .NET 10, Orleans 9, SvelteKit + Svelte 5,
  Azure SQL Basic + Azure Table Storage, NLog,
  ASP.NET Core Minimal API + SignalR
- Interaction model: **turn-based** (fits actor model)
- Inter-module comms: **plain service interfaces in
  `Shared.Contracts`** — no in-process bus / Mediator
- OpenAPI client: **Kiota** (NSwag rejected)
- Logging: **NLog** (not Serilog)

#### Pending
- Event-Storming notes (paper, photo in README)
- SvelteKit adapter choice — deferred to start of
  Phase 4 (1-day spike, see ADR-0004)
- Branch protection on `main`
