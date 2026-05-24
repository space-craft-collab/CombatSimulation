# Architecture Decision Records

Lightweight [MADR](https://adr.github.io/madr/)-style ADRs. One
file per decision, numbered sequentially. Use
[`0000-template.md`](0000-template.md) as the starting point for
new entries.

## Index

| #    | Title                                                                | Status   |
|------|----------------------------------------------------------------------|----------|
| 0001 | [Modular monolith over microservices](0001-modular-monolith.md)      | Proposed |
| 0002 | [Orleans silo co-hosted with the web host](0002-orleans-cohosted.md) | Proposed |
| 0003 | [No transactional outbox for hot→cold handoff](0003-no-outbox.md)    | Proposed |
| 0004 | [SvelteKit + Svelte 5 for the frontend](0004-sveltekit-frontend.md)  | Proposed |
| 0005 | [Inter-module communication via service interfaces](0005-inter-module-services.md) | Proposed |
| 0006 | [NLog for structured logging](0006-nlog-logging.md)                  | Proposed |
| 0007 | [Turn-based interaction model](0007-turn-based.md)                   | Proposed |
| 0008 | [Split the Shared layer: Kernel + Infrastructure](0008-shared-layer-split.md) | Proposed |

## Conventions

- Filenames: `NNNN-short-kebab-title.md`, four-digit prefix.
- Status values: `Proposed`, `Accepted`,
  `Superseded by ADR-XXXX`, `Deprecated`.
- The project is pre-code: ADRs stay `Proposed` and are edited
  in place as the design settles. The supersede workflow below
  only kicks in once an ADR is marked `Accepted`.
- Never edit an `Accepted` ADR's decision text after the fact.
  If a decision changes, write a new ADR and mark the old one
  `Superseded`.
- Cross-link related ADRs by relative path.
