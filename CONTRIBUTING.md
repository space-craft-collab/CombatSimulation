# Contributing

This is a solo **showcase** project built to
demonstrate Microsoft Orleans and modular-monolith
patterns. It is not looking for feature
contributions, but issues and discussion are
very welcome.

## Good things to open an issue about

- A bug in the code or docs
- An architecture decision you think is wrong —
  see [`docs/adr/`](docs/adr/) and argue with it
- A question about why something is built this way

## If you do want to send a PR

Keep it small and focused. Larger changes are
likely to be declined simply because they cut
across the roadmap.

## Build and test

```bash
dotnet restore
dotnet build --configuration Release
dotnet test --configuration Release
```

The SDK version is pinned in `global.json`.

## Ground rules for code

Everything is written down in
[`CLAUDE.md`](CLAUDE.md) — it is the coding
standard, not just an AI prompt. Highlights:

- .NET 10, `LangVersion=latest`, nullable enabled
- File-scoped namespaces, Allman braces, 4 spaces
- XML doc comments on all public types and members
- English code and comments
- One type per file
- xUnit v3, `Method_Scenario_ExpectedResult` names
- No test-only interfaces — inject a `Func<>` or
  `Action<>` instead (ADR-0010)
- Separate read and write repositories (CQRS-style)

Warnings are errors. `dotnet build` must be clean
before you push.

## Architecture changes

Anything that changes structure needs an ADR in
[`docs/adr/`](docs/adr/), using
[`0000-template.md`](docs/adr/0000-template.md).
ADRs start as `Proposed` and are edited in place;
once `Accepted`, changes require a superseding ADR.
