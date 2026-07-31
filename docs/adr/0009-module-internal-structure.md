# ADR-0009: Per-module internal structure — vertical slices in one project

- **Status:** Proposed
- **Date:** 2026-06-21
- **Deciders:** aha (solo maintainer)

## Context

[ADR-0001] ships one binary with one project per module
(`Catalog`, `Battles`, `Players`). [ADR-0005] fixes the
*cross-module* contracts (per-module `<Module>.Contracts`) and
[ADR-0008] fixes the *shared* layer (`Shared.Kernel` +
`Shared.Infrastructure`). What neither pins down is how each
module is organised **internally**.

Two shapes are on the table:

1. **Separate per-layer projects** per module —
   `<Module>.Domain`, `<Module>.Application`,
   `<Module>.Infrastructure`. The layering is compiler-enforced,
   but three modules × three layers is nine projects before
   contracts, grains, shared, host and tests — a project
   explosion for a solo showcase, and it contradicts [ADR-0001]'s
   "one project per module".
2. **One project per module** with the layers as folders, and a
   vertical-slice `Features/` folder for use cases.

Earlier drafts (the ROADMAP graph, the [ADR-0008] dependency
table) had drifted toward option 1. This ADR settles on option 2
and the conventions around it.

## Decision

Each module is a **single `Microsoft.NET.Sdk` class library**.
The internal layering is expressed as folders, not projects:

- **`Domain/`** — entities, value objects, aggregates, domain
  events and domain services: the invariants and business rules
  that hold regardless of any single use case.
- **`Features/`** — vertical slices, **one subfolder per use
  case**. A feature folder holds its service/handler, its
  module-internal request/response types, its validation, and its
  Minimal API endpoint (`MapXEndpoint`). Cross-module DTOs do
  *not* live here — they stay in `<Module>.Contracts` ([ADR-0005]).
- **`Infrastructure/`** — the persistence and external edges:
  `DbContext`, EF entity configurations, the **separate read and
  write repositories** (read/write split), and external clients.

Additional conventions:

- **`<Module>Module.cs`** at the project root is the module's only
  composition entry point: a DI registration extension
  (`AddCatalogModule(this IServiceCollection)`) plus an endpoint
  registration extension (`MapCatalogEndpoints(this
  IEndpointRouteBuilder)`) that the `AppHost` calls. The
  host never reaches into a module's internals.
- **`Battles` only** additionally has a **`Grains/`** folder for
  grain *implementations*; their interfaces stay in
  `Battles.Grains.Abstractions` ([ADR-0005]).
- The module gets ASP.NET Core types (Minimal API,
  `IEndpointRouteBuilder`, `Results`, …) through the shared
  framework, via `<FrameworkReference
  Include="Microsoft.AspNetCore.App" />` — **not** a NuGet
  package. The host (`Microsoft.NET.Sdk.Web`) references the
  shared framework implicitly and must not add it again.
- The intra-module layering is a **convention enforced by an
  architecture test** (NetArchTest), not by csproj edges:
  `Domain` must not depend on `Features` or `Infrastructure`;
  `Features` and `Infrastructure` may depend on `Domain`. The
  test lives in the test project and runs in CI (Phase 1).

At the project level the module still references only what
[ADR-0005]/[ADR-0008] allow: `Shared.Kernel`,
`Shared.Infrastructure`, the `<Module>.Contracts` of each
consumed module, and — for `Battles` — `Battles.Grains.Abstractions`.
Those cross-project edges remain compiler-enforced.

## Consequences

- **Positive:**
  - One project per module instead of three — far fewer csproj
    files to maintain, matching [ADR-0001].
  - Endpoints sit next to the logic they expose: a feature is one
    folder you can read, add, or delete as a unit (true vertical
    slice, high cohesion).
  - No `Application`-layer / MediatR ceremony; plain services,
    consistent with [ADR-0005].
  - `FrameworkReference` keeps the module package-light — no
    `Microsoft.AspNetCore.*` NuGet sprawl.
- **Negative:**
  - Folders are not compiler-enforced: a `Domain` file *could*
    `using` `Infrastructure`. The architecture test is the guard
    and must be kept current — if it is deleted or skipped, the
    layering can rot silently.
  - The module now carries an ASP.NET Core framework reference, so
    it is no longer transport-agnostic. Acceptable in a co-hosted
    monolith; if a module were ever extracted, its endpoints would
    move to the new host.
- **Neutral / follow-ups:**
  - If a module ever outgrows a single project, the three folders
    map 1:1 onto extractable projects later — the decision is
    reversible.
  - `docs/diagrams/project-dependencies.html` has been redrawn
    at module granularity to match this ADR.

## Alternatives considered

- **Separate per-layer projects per module** — rejected: nine+
  projects for a solo showcase; ceremony over value; contradicts
  [ADR-0001]'s "one project per module".
- **Classic `Application` layer name** — rejected: implies a
  separate orchestration layer; `Features` is the honest name for
  vertical slices and matches the "plain services" stance.
- **Endpoints in the host** — rejected: splits a feature across
  projects and breaks slice cohesion; the whole point is keeping a
  use case in one place.
- **`Microsoft.AspNetCore.*` NuGet packages in the module** —
  rejected: the shared-framework `FrameworkReference` is the
  supported way to consume ASP.NET Core types from a class
  library.

## References

- [ADR-0001]: Modular monolith — one project per module.
- [ADR-0005]: Per-module Contracts + `Battles.Grains.Abstractions`
  (grain interfaces live there, not in a module's `Features`).
- [ADR-0008]: Shared layer split — the project-level references a
  module is allowed to make.
- [Use ASP.NET Core APIs in a class library]: the `FrameworkReference`
  approach.
- [NetArchTest]: the architecture-test library that enforces the
  intra-module layering.

[ADR-0001]: 0001-modular-monolith.md
[ADR-0005]: 0005-inter-module-services.md
[ADR-0008]: 0008-shared-layer-split.md
[Use ASP.NET Core APIs in a class library]: https://learn.microsoft.com/en-us/aspnet/core/fundamentals/target-aspnetcore
[NetArchTest]: https://github.com/BenMorris/NetArchTest
