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
  exposes its cross-module surface through interfaces — e.g.
  `ICatalogQueryService` (read by `Battles`) and
  `IPlayerStatsService` (written by `Battles`).
  `IBattleSnapshotWriter` is *not* cross-module: the grain calls it
  inside `Battles` (see [ADR-0003]).
- **Contracts live in per-module `<Module>.Contracts` projects**,
  not a global `Shared.Contracts` assembly. A consuming module
  references only the producer's `.Contracts` project — never the
  producer's implementation. `Battles` references
  `Catalog.Contracts` and `Players.Contracts`; it does not see
  `Catalog`'s or `Players`' internals.
- **Grain interfaces** (`IArenaGrain`, `ILiveBattleGrain`,
  `IMonsterInstanceGrain`) do **not** live in `Battles.Contracts`.
  A grain interface must inherit an Orleans marker
  (`IGrainWithGuidKey`) and its DTOs need `[GenerateSerializer]`,
  both of which pull in `Microsoft.Orleans.Sdk`. They live in a
  separate `Battles.Grains.Abstractions` project that carries that
  Orleans reference, consumed only by `Battles.*` and the host.
  This keeps the plain `*.Contracts` projects Orleans-free, so a
  module consuming a Battles service contract never sees an Orleans
  type ([ADR-0002]).
- **Other modules consume those interfaces via DI**, registered
  in the Bootstrapper.
- **No in-process bus, no MediatR.** Inside a single module,
  plain method calls and (where useful) plain C# events suffice.
- `<Module>.Contracts` projects stay dependency-light (they may
  reference `Shared.Kernel` for shared primitives, nothing more).
- **Interfaces only where they carry real value.** The contract
  and grain interfaces above earn their keep: they decouple
  modules and keep the "ready to extract" property. This is *not*
  a licence for `IFooRepository`-style interfaces whose only
  purpose is making a class mockable in a unit test. Inside a
  module, default to concrete classes; when a test genuinely needs
  a seam, inject a **function delegate** (`Func<...>` / `Action<...>`)
  rather than minting a single-method interface for it. The
  cross-module surface is the boundary where an interface is
  justified; an internal test seam is not.

The dependency direction is therefore: a module depends only on
the `<Module>.Contracts` projects of the modules it consumes; no
module depends on another module's implementation assembly. The
sole Orleans-bearing exception is `Battles.Grains.Abstractions`,
which is internal to the `Battles` module plus the host — no other
module references it. See the diagram in
[`docs/diagrams/project-dependencies.html`].

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
- [ADR-0002]: Orleans co-hosted — why grain interfaces stay out of
  the plain `*.Contracts` projects.
- [ADR-0003]: No outbox — `IBattleSnapshotWriter` is a
  Battles-internal abstraction (the grain calls it within the
  module); the canonical *cross-module* calls are
  `ICatalogQueryService` and `IPlayerStatsService`.
- [ADR-0008]: Shared layer split (Kernel + Infrastructure)
- [Ardalis RiverBooks]: reference modular-monolith sample
- [`docs/diagrams/project-dependencies.html`]: interactive project
  graph + battle data flow.

[ADR-0001]: 0001-modular-monolith.md
[ADR-0002]: 0002-orleans-cohosted.md
[ADR-0003]: 0003-no-outbox.md
[ADR-0008]: 0008-shared-layer-split.md
[Ardalis RiverBooks]: https://github.com/ardalis/RiverBooks
[`docs/diagrams/project-dependencies.html`]: ../diagrams/project-dependencies.html
