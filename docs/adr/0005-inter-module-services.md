# ADR-0005: Inter-module communication via service interfaces

- **Status:** Proposed
- **Date:** 2026-05-24
- **Deciders:** aha (solo maintainer)

## Context

The repo is a modular monolith ([ADR-0001]) with three modules
(`Catalog`, `Battles`, `Players`). They need to talk to each
other:

- `Battles` reads monster stats from `Catalog`.
- `Battles` writes results to `Players` when a battle finishes.

Two questions: *how* do modules call each other, and *where* do
the contracts live?

On the *how*, three common shapes:

1. **Direct project references** between modules.
2. **In-process bus / Mediator** (`MediatR`, `Mediator.NET`,
   custom dispatcher).
3. **Plain service interfaces** consumed via DI.

Common best-practice for modular monoliths is option 2, but for
a solo project on this scale it adds layers (handlers, request
types, behaviour pipelines) without removing a real pain.

On the *where*, a single global `Shared.Contracts` assembly
couples every module to every other module's public surface:
`Battles` only needs to read monster stats from `Catalog`, but a
global assembly also exposes it to `Players`' contracts, and any
change to one module's contract recompiles all of them. It also
makes the dependency graph lie — the `.csproj` references no
longer show *which* module talks to *which*. The
[Ardalis RiverBooks] sample solves this by giving each module its
own `<Module>.Contracts` project.

## Decision

- **No direct project references between modules.** Each module
  exposes its public surface through interfaces — e.g.
  `ICatalogQueryService`, `IPlayerStatsService`,
  `IBattleSnapshotWriter`.
- **Contracts live in per-module `<Module>.Contracts` projects**,
  not a global `Shared.Contracts` assembly. A consuming module
  references only the producer's `.Contracts` project — never the
  producer's implementation. `Battles` references
  `Catalog.Contracts` and `Players.Contracts`; it does not see
  `Catalog`'s or `Players`' internals.
- **Grain interfaces** (`IArenaGrain`, `ILiveBattleGrain`,
  `IMonsterInstanceGrain`) are part of the `Battles` module's
  public surface and live in `Battles.Contracts`.
- **Other modules consume those interfaces via DI**, registered
  in the Bootstrapper.
- **No in-process bus, no MediatR.** Inside a single module,
  plain method calls and (where useful) plain C# events suffice.
- `<Module>.Contracts` projects stay dependency-light (they may
  reference `Shared.Kernel` for shared primitives, nothing more).

The dependency direction is therefore: a module depends only on
the `<Module>.Contracts` projects of the modules it consumes; no
module depends on another module's implementation assembly.

## Consequences

- **Positive:**
  - The `.csproj` graph shows exactly which module depends on
    which — `Battles → Catalog.Contracts` is explicit.
  - The contract projects are the only thing that needs to
    change if a module is ever extracted to its own service
    (swap the in-process implementation for an HTTP client).
  - Changing one module's contract recompiles only its consumers,
    not the whole solution.
  - Stack traces stay short — no Mediator pipeline frames.
- **Negative:**
  - No automatic fan-out: when `Battles` finishes a battle it
    must call each interested service explicitly. With three
    modules this is fine; with thirty it would not be.
  - No built-in behaviour pipeline (validation, logging,
    retries). Cross-cutting concerns live in
    `Shared.Infrastructure` helpers or DI decorators, not in
    middleware.
  - More projects (one extra per module) than a single shared
    assembly.
- **Neutral / follow-ups:**
  - Shared cross-cutting DTOs (if any genuinely belong to no
    single module) have no obvious home — push such types into
    `Shared.Kernel`, or duplicate the small DTO rather than
    creating a god-contracts project.
  - If a future module needs fan-out, introduce a narrow
    domain-event abstraction inside that module only — do not
    promote it to a global bus.

## Alternatives considered

- **MediatR / in-process bus** — rejected: adds indirection
  that this scale doesn't need; user has explicitly preferred
  simplicity here.
- **Single global `Shared.Contracts`** — rejected: couples every
  module to every other module's surface and hides the real
  dependency direction.
- **Direct project references** — rejected: would couple
  module internals and lose the "ready to extract" property.
- **Source generators for the contract layer** — rejected:
  cost > benefit at three modules.

## References

- [ADR-0001]: Modular monolith
- [ADR-0003]: No outbox — referenced because
  `IBattleSnapshotWriter` is the canonical example of a
  cross-module call.
- [ADR-0008]: Shared layer split (Kernel + Infrastructure)
- [Ardalis RiverBooks]: reference modular-monolith sample

[ADR-0001]: 0001-modular-monolith.md
[ADR-0003]: 0003-no-outbox.md
[ADR-0008]: 0008-shared-layer-split.md
[Ardalis RiverBooks]: https://github.com/ardalis/RiverBooks
