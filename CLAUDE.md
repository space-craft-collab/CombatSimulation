# CombatSimulation

.NET combat simulation project.

## Language & Comments

- All code in **English**
- All comments in **English**
- XML doc comments (`///`) required for:
  - All `public` classes
  - All `public` methods
  - All `public` properties (non-trivial)
- Internal/private members: comment only when intent is non-obvious

## .NET Standards

Target the **most modern** .NET standards available.

- **TFM:** latest stable `.NET` (e.g. `net9.0`)
- **C# language version:** `latest`
- **Nullable reference types:** `enable`
- **Implicit usings:** `enable`
- **File-scoped namespaces** (no nested braces)
- **`var`** when type is obvious from RHS
- **Primary constructors** where they simplify code
- **Collection expressions** (`[1, 2, 3]`)
- **Pattern matching** over chained `if/else`
- **Records** for immutable data
- **`required`** keyword over constructor boilerplate
- **`async`/`await`** end-to-end (no `.Result` / `.Wait()`)

## Naming

- `PascalCase`: types, methods, properties, constants
- `camelCase`: locals, parameters
- `_camelCase`: private fields
- `IPascalCase`: interfaces
- No Hungarian notation

## Style

- Allman braces
- 4-space indent
- One type per file
- Usings outside namespace, sorted, `System.*` first
- Expression-bodied members for one-liners
- Prefer `readonly` and immutability

## Error Handling

- Throw specific exceptions (`ArgumentNullException`, etc.)
- Use `ArgumentNullException.ThrowIfNull(x)` guard helpers
- No empty `catch` blocks
- No catching `Exception` without rethrow or logging

## XML Doc Example

```csharp
/// <summary>
/// Resolves a single combat round between two units.
/// </summary>
/// <param name="attacker">The attacking unit.</param>
/// <param name="defender">The defending unit.</param>
/// <returns>The combat result for this round.</returns>
public CombatResult Resolve(Unit attacker, Unit defender)
```

## Dependencies & Abstractions

- **Interfaces only where they carry real value.** Cross-module
  contracts in `<Module>.Contracts` and Orleans grain interfaces
  are wanted (see ADR-0005).
- **No test-only interfaces.** Do not create `IFooRepository`-style
  interfaces whose sole purpose is making a class mockable in a
  unit test. For such test seams, inject a **function delegate**
  (`Func<...>` / `Action<...>`) instead of an interface.
- Default to concrete classes; introduce a seam only when a test
  genuinely needs one, and prefer the delegate form for it.
- **Separate read and write repositories.** Reads and writes are
  distinct responsibilities — do not merge them into one combined
  repository per aggregate (CQRS-style split).

## Module layout

Each module is **one project** (`Microsoft.NET.Sdk`) — see ADR-0009.
Internal folders:

- `Domain/` — entities, value objects, aggregates, invariants.
- `Features/` — vertical slices; **one subfolder per use case**,
  holding its service, request/response types, validation, and its
  Minimal API endpoint.
- `Infrastructure/` — `DbContext`, EF configs, the read and write
  repositories, external clients.
- `<Module>Module.cs` at the root — the DI/endpoint registration
  entry the host calls.

ASP.NET Core comes via `<FrameworkReference Include="Microsoft.AspNetCore.App" />`,
not a NuGet package. The `Domain` → `Features` → `Infrastructure`
direction is a convention enforced by an architecture test
(NetArchTest), since folders are not compiler-checked.

## Tests

- **xUnit v3** (the v3 line, not v2)
- Arrange / Act / Assert layout
- Test names: `Method_Scenario_ExpectedResult`

## Output Formatting

See `mobile.md` — keep responses mobile-friendly.
