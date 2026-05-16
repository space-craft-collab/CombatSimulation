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

## Tests

- xUnit preferred
- Arrange / Act / Assert layout
- Test names: `Method_Scenario_ExpectedResult`

## Output Formatting

See `mobile.md` — keep responses mobile-friendly.
