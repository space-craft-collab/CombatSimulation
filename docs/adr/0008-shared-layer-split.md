# ADR-0008: Split the Shared layer into Kernel and Infrastructure

- **Status:** Proposed
- **Date:** 2026-05-24
- **Deciders:** aha (solo maintainer)

## Context

The modular monolith ([ADR-0001]) needs shared assemblies that
every module can build on without coupling modules to each other.
Inter-module *contracts* are handled per-module ([ADR-0005]), so
this ADR concerns only the genuinely cross-cutting shared code.

The risk with a single `Shared.Kernel` is that it becomes a
god-package: if it holds both dependency-light domain primitives
**and** concrete infrastructure (EF Core, Orleans, Azure SDKs),
then every module's Domain project transitively drags heavy infra
dependencies in, and the layering rots.

## Decision

Split the shared layer into **two** assemblies with a strict
dependency direction:

- **`Shared.Kernel`** — dependency-light primitives: `Result<T>`,
  domain base types (`Entity`, `ValueObject`, `AggregateRoot`),
  guard helpers, abstractions. **No EF Core / Orleans / Azure
  references.** Referenced by module Domain and Application layers.
- **`Shared.Infrastructure`** — concrete cross-cutting infra:
  base `DbContext` conventions, common EF mappings, NLog +
  OpenTelemetry registration extensions, Azure Table Storage /
  Orleans wiring helpers. References EF Core, Orleans, and Azure
  SDKs. Referenced only by module **Infrastructure** projects and
  the Bootstrapper.

Inter-module service interfaces and DTOs live in per-module
`<Module>.Contracts` projects — see [ADR-0005], not here.

The resulting direction is:

```
Domain          → Shared.Kernel
Application     → Shared.Kernel, <consumed module>.Contracts
Infrastructure  → Shared.Kernel, Shared.Infrastructure
```

## Consequences

- **Positive:**
  - `Shared.Kernel` stays free of infra packages, so Domain
    projects compile against a small, stable surface.
  - Cross-cutting infra wiring (logging, tracing, persistence
    conventions) lives in one place instead of being copy-pasted
    across module Infrastructure projects.
  - The dependency graph makes the intended layering visible in
    the `.csproj` references — reviewers can see it at a glance.
- **Negative:**
  - One more project to maintain than the original two-assembly
    plan.
  - Requires discipline to keep infra packages out of
    `Shared.Kernel`; an accidental EF reference there would defeat
    the purpose.
- **Neutral / follow-ups:**
  - If `Shared.Infrastructure` itself grows into a grab-bag,
    revisit and split by concern (e.g. `*.Persistence`,
    `*.Observability`).

## Alternatives considered

- **Single `Shared.Kernel` holding everything** — rejected:
  becomes the god-package the plan explicitly warned against;
  pollutes Domain projects with infra dependencies.
- **No shared infra; duplicate wiring per module** — rejected:
  repeated NLog/OTEL/EF setup across modules drifts out of sync.
- **Push infra wiring into the Bootstrapper only** — rejected:
  the Bootstrapper would need to know each module's internals;
  module Infrastructure projects still need shared base types.

## References

- [ADR-0001]: Modular monolith
- [ADR-0005]: Per-module Contracts projects — inter-module
  contracts live there, not in the Shared layer.
- [ADR-0006]: NLog for structured logging — wiring lives in
  `Shared.Infrastructure`.

[ADR-0001]: 0001-modular-monolith.md
[ADR-0005]: 0005-inter-module-services.md
[ADR-0006]: 0006-nlog-logging.md
