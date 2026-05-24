# ADR-0005: Inter-module communication via service interfaces

- **Status:** Accepted
- **Date:** 2026-05-16
- **Deciders:** aha (solo maintainer)

## Context

The repo is a modular monolith ([ADR-0001]) with three modules
(`Catalog`, `Battles`, `Players`). They need to talk to each
other:

- `Battles` reads monster stats from `Catalog`.
- `Battles` writes results to `Players` when a battle finishes.

Three common shapes for in-process module communication:

1. **Direct project references** between modules.
2. **In-process bus / Mediator** (`MediatR`, `Mediator.NET`,
   custom dispatcher).
3. **Plain service interfaces in a shared contracts assembly.**

Common best-practice for modular monoliths is option 2, but for
a solo project on this scale it adds layers (handlers, request
types, behaviour pipelines) without removing a real pain.

## Decision

- **No direct project references between modules.** Each module
  exposes its public surface through an **interface in
  `Shared.Contracts`** — e.g. `ICatalogQueryService`,
  `IPlayerStatsService`, `IBattleSnapshotWriter`.
- **Other modules consume those interfaces via DI**, registered
  in the Bootstrapper.
- **No in-process bus, no MediatR.** Inside a single module,
  plain method calls and (where useful) plain C# events suffice.

The dependency direction is therefore: every module depends on
`Shared.Contracts`; no module depends on another module's
assemblies.

## Consequences

- **Positive:**
  - Module boundaries are visible in the `.csproj` graph — a
    reviewer can see the dependency direction at a glance.
  - The contract assembly is the only thing that needs to
    change if a module is ever extracted to its own service
    (swap the in-process implementation for an HTTP client).
  - Stack traces stay short — no Mediator pipeline frames.
- **Negative:**
  - No automatic fan-out: when `Battles` finishes a battle it
    must call each interested service explicitly. With three
    modules this is fine; with thirty it would not be.
  - No built-in behaviour pipeline (validation, logging,
    retries). Cross-cutting concerns live in `Shared.Kernel`
    helpers or DI decorators, not in middleware.
- **Neutral / follow-ups:**
  - If a future module needs fan-out, introduce a narrow
    domain-event abstraction inside that module only — do not
    promote it to a global bus.

## Alternatives considered

- **MediatR / in-process bus** — rejected: adds indirection
  that this scale doesn't need; user has explicitly preferred
  simplicity here.
- **Direct project references** — rejected: would couple
  module internals and lose the "ready to extract" property.
- **Source generators for the contract layer** — rejected:
  cost > benefit at three modules.

## References

- [ADR-0001]: Modular monolith
- [ADR-0003]: No outbox — referenced because
  `IBattleSnapshotWriter` is the canonical example of a
  cross-module call.

[ADR-0001]: 0001-modular-monolith.md
[ADR-0003]: 0003-no-outbox.md
