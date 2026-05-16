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
- `.editorconfig` + `Directory.Build.props`
- `Directory.Packages.props` (central package mgmt)
- ADRs 001–007 in `docs/adr/`
- Event-Storming notes
- SvelteKit adapter choice
