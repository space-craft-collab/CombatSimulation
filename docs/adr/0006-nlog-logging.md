# ADR-0006: NLog for structured logging

- **Status:** Accepted
- **Date:** 2026-05-16
- **Deciders:** aha (solo maintainer)

## Context

The backend needs structured logging that flows through
`Microsoft.Extensions.Logging` and lights up correctly in
Orleans (grain identity in scope, deactivation traces, cluster
membership events). The two realistic options are:

1. **Serilog** — most popular in .NET community,
   `LoggerConfiguration` fluent API.
2. **NLog** — older, config-file driven, still actively
   maintained, first-class `ILogger` integration.

The maintainer has stronger NLog experience and prefers a
config-file-driven setup so log targets / levels can be tuned in
deployed environments without a redeploy.

## Decision

Use **NLog** as the logging provider, wired through the standard
`Microsoft.Extensions.Logging` abstraction. Configuration lives
in `nlog.config` next to the Bootstrapper. Structured properties
are passed via the standard `ILogger` message-template syntax
(no NLog-specific call sites in domain code).

OpenTelemetry traces and metrics are added on top in Phase 1
(separate from the logging-provider choice — they can coexist
with any `ILogger` backend).

## Consequences

- **Positive:**
  - `nlog.config` is editable in deployment without a rebuild.
  - Domain code stays vendor-neutral (`ILogger<T>` only) — the
    provider could be swapped later with zero code changes.
  - Matches the maintainer's existing muscle memory →
    faster diagnosis of weird issues.
- **Negative:**
  - Smaller share-of-voice than Serilog in 2026 .NET blog
    posts. Mostly a documentation-discoverability nit.
  - Some third-party sinks ship Serilog-first; NLog equivalents
    sometimes lag.
- **Neutral / follow-ups:**
  - Trace/metric export to OTLP is set up in Phase 1; NLog only
    handles logs.
  - The `nlog.config` location and rolling-file vs console
    target choice is fixed in Phase 1.

## Alternatives considered

- **Serilog** — rejected: no decisive feature advantage for this
  project, and the maintainer prefers NLog's config-file model.
- **`Microsoft.Extensions.Logging` console only** — rejected:
  fine for dev, weak for production (no rolling files,
  structured sinks).

## References

- NLog documentation: NLog + Microsoft.Extensions.Logging
- [ADR-0001]: Modular monolith — explains why we only pick one
  logging stack for the whole process.

[ADR-0001]: 0001-modular-monolith.md
