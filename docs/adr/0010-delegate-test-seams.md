# ADR-0010: Function-delegate test seams instead of test-only interfaces

- **Status:** Proposed
- **Date:** 2026-07-31
- **Deciders:** aha (solo maintainer)

## Context

[ADR-0005] already states the rule in passing: interfaces are
justified at the cross-module boundary (`<Module>.Contracts`,
grain interfaces), not as unit-test seams. This ADR records the
seam pattern itself, because it shapes every feature handler.

The classic shape being rejected: a handler needs one value from
persistence, so an `IPlayerQueryRepository` interface is minted
with a single method, implemented once, and mocked in exactly
one unit test. The interface exists solely so the test compiles
— abstraction without value, one more file per seam, and a
dependency surface (the whole repository) far wider than what
the handler actually uses.

The maintainer's production experience confirms the
alternative: when the only reason for an abstraction is
testability, a **function delegate** is the lighter and more
honest seam.

## Decision

**When a class needs a seam only so a unit test can substitute
a dependency, inject a function delegate (`Func<...>` /
`Action<...>`), not an interface.**

Example — a handler that needs a player's level, not a
repository:

```csharp
public sealed class StartBattleHandler(
    Func<Guid, Task<int>> getPlayerLevel)
{
    public async Task<StartBattleResult> HandleAsync(
        StartBattleRequest request)
    {
        var level = await getPlayerLevel(request.PlayerId);
        // ...
    }
}
```

Composition wires the delegate to the concrete read repository
([read/write split][ADR-0009]) in the feature's registration:

```csharp
services.AddScoped<StartBattleHandler>(sp =>
    new StartBattleHandler(
        sp.GetRequiredService<PlayerReadRepository>()
          .GetLevelAsync));
```

The unit test substitutes a lambda — no mocking library needed:

```csharp
var handler = new StartBattleHandler(_ => Task.FromResult(5));
```

Conventions:

- **Resolve delegates via factory lambdas** at the consumer's
  registration (as above). Do not register bare `Func<...>`
  types in the container — two seams with the same signature
  would collide.
- If the same seam recurs across several consumers, promote it
  to a **named delegate type**
  (`public delegate Task<int> GetPlayerLevel(Guid playerId);`)
  for readability and unambiguous DI registration.
- The rule is about *test-only* seams. Interfaces with real
  architectural value stay: `<Module>.Contracts`, grain
  interfaces ([ADR-0005]), and genuinely polymorphic strategies.
- Default remains **concrete classes with no seam at all**;
  introduce the delegate only when a test genuinely needs one.

## Consequences

- **Positive:**
  - The dependency surface is exact and honest: the handler
    declares the one function it uses, not a repository with
    ten methods.
  - Tests need no mocking framework — a lambda is the stub.
  - One less file and registration per seam; no
    `IFoo`/`Foo` pairs that exist for ceremony.
- **Negative:**
  - A bare `Func<Guid, Task<int>>` carries less meaning than a
    named interface method; parameter names in the primary
    constructor (and named delegate types where shared) carry
    the intent instead.
  - A class accumulating many delegates is a smell — at that
    point the cluster probably *is* a real abstraction and
    should become an interface deliberately.
  - Unusual pattern for newcomers used to interface-everywhere
    codebases; this ADR is the explanation to point at.
- **Neutral / follow-ups:**
  - `NSubstitute` stays pinned for the rare contract-interface
    mock; feature-level unit tests should not need it.
  - Integration tests are unaffected — they hit real
    infrastructure via Testcontainers, no seam involved.

## Alternatives considered

- **Single-method `IFooRepository` interfaces** — rejected: the
  interface's only consumer is the test; pure ceremony.
- **Mocking concrete classes (virtual members / proxies)** —
  rejected: forces `virtual` into production code and couples
  tests to class internals.
- **No unit seam; integration-test everything** — rejected:
  Testcontainers covers the integration level, but round-trip
  tests for every branch of handler logic are too slow as the
  only feedback loop.

## References

- [ADR-0005]: Inter-module communication — where interfaces
  *are* wanted, and the origin of this rule.
- [ADR-0009]: Per-module structure — read/write repositories
  the delegates bind to.
- `CLAUDE.md` — "Dependencies & Abstractions" section states
  the same rule for day-to-day coding.

[ADR-0005]: 0005-inter-module-services.md
[ADR-0009]: 0009-module-internal-structure.md
