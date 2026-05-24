# ADR-0002: Orleans silo co-hosted with the web host

- **Status:** Accepted
- **Date:** 2026-05-16
- **Deciders:** aha (solo maintainer)

## Context

Microsoft Orleans is the centrepiece of this showcase. Two
hosting shapes are common:

1. **Co-hosted** — `UseOrleans()` on the same
   `WebApplicationBuilder` as the ASP.NET host. Web requests and
   grain calls share the process.
2. **Separate silo service** — silo runs in its own deployable,
   the API tier talks to it through `IClusterClient` over the
   network.

The project ships as a modular monolith ([ADR-0001]) on Azure
Container Apps with scale-to-zero. The hot-path workload (live
battles) is the same code that serves the REST/SignalR
endpoints, so most grain calls would otherwise hop through
SignalR → API → silo → back.

## Decision

**Co-host the Orleans silo with the ASP.NET Core host** in a
single process. Web endpoints obtain the local `IClusterClient`
through DI and call grains in-process. Cluster membership still
uses Azure Table Storage so the deployment scales horizontally
to multiple silos in Phase 5.

## Consequences

- **Positive:**
  - One Dockerfile, one container, one deployment unit — keeps
    the modular-monolith promise intact.
  - Grain calls from API endpoints have no network hop.
  - Phase 5 multi-silo work only adds replicas; no new service
    boundary to define.
- **Negative:**
  - Web request CPU competes with grain CPU. Acceptable for a
    showcase workload, would be revisited under real load.
  - Silo restart = API restart. Mitigated by running ≥2
    replicas once Phase 5 lands.
- **Neutral / follow-ups:**
  - Cluster membership provider is Azure Table Storage (see
    Phase 3 / 5 work).
  - The `Battles` module is the only module that holds the
    `IClusterClient`; other modules never see Orleans types
    directly ([ADR-0005]).

## Alternatives considered

- **Separate silo service** — rejected: doubles deployment
  surface and idle cost without a workload that justifies it.
- **In-cluster client only** — rejected: still needs the silo
  somewhere; we would lose the local-call shortcut.

## References

- Orleans docs: "Hosting an Orleans application"
- [ADR-0001]: Modular monolith

[ADR-0001]: 0001-modular-monolith.md
[ADR-0005]: 0005-inter-module-services.md
