# ADR-0001: Modular monolith over microservices

- **Status:** Proposed
- **Date:** 2026-05-16
- **Deciders:** aha (solo maintainer)

## Context

Orleans Monster Arena is a one-person showcase repository. The
domain has three bounded contexts — `Catalog`, `Battles`,
`Players` — and an Orleans silo for live battle state. Two
deployment shapes are on the table:

1. One process hosting all modules + the silo (modular monolith).
2. Each module as its own service plus a separate silo cluster
   (microservices).

The project must stay cheap to operate (Azure Container Apps
scale-to-zero, target idle cost <10 EUR/month), demoable from a
single `docker compose up`, and small enough for one person to
keep in their head. At the same time, reviewers should still see
clean module boundaries.

## Decision

Ship as a **modular monolith**: one binary, one deployable, with
strict in-process module boundaries enforced through per-module
`<Module>.Contracts` interfaces ([ADR-0005]). Orleans runs
co-hosted in the same process (see [ADR-0002]).

## Consequences

- **Positive:**
  - Single artifact to build, ship, and observe — fits solo
    maintenance and the idle-cost budget.
  - Module boundaries stay visible in code (one project per
    module, no shared `DbContext`) so the design reads as
    "ready to extract" if it ever needs to scale out.
  - Local dev = `dotnet run`. No service mesh, no per-service
    auth, no inter-service contract versioning.
- **Negative:**
  - Cannot independently scale a single module — fine for a
    showcase, would matter in production.
  - A bug in one module can crash the whole process. Mitigated
    by tests and module-level health checks.
- **Neutral / follow-ups:**
  - Inter-module communication rules are locked in [ADR-0005].
  - If a real workload ever demands it, modules can be lifted
    out by moving their `<Module>.Contracts` interface
    implementation behind an HTTP/gRPC adapter.

## Alternatives considered

- **Microservices from day one** — rejected: operational
  overhead and cost dwarf the benefit for a solo showcase.
- **Single project, no module split** — rejected: loses the
  "I understand module boundaries" signal reviewers look for.

## References

- [ADR-0002]: Orleans co-hosted in the same process
- [ADR-0005]: Inter-module communication via service interfaces

[ADR-0002]: 0002-orleans-cohosted.md
[ADR-0005]: 0005-inter-module-services.md
