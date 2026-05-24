# Architecture Decision Records

Lightweight [MADR](https://adr.github.io/madr/)-style ADRs. One
file per decision, numbered sequentially. Use
[`0000-template.md`](0000-template.md) as the starting point for
new entries.

## Index

| #    | Title                                                                | Status   |
|------|----------------------------------------------------------------------|----------|
| 0001 | [Modular monolith over microservices](0001-modular-monolith.md)      | Accepted |
| 0002 | [Orleans silo co-hosted with the web host](0002-orleans-cohosted.md) | Accepted |
| 0003 | [No transactional outbox for hot→cold handoff](0003-no-outbox.md)    | Accepted |
| 0004 | [SvelteKit + Svelte 5 for the frontend](0004-sveltekit-frontend.md)  | Accepted |
| 0005 | [Inter-module communication via service interfaces](0005-inter-module-services.md) | Accepted |
| 0006 | [NLog for structured logging](0006-nlog-logging.md)                  | Accepted |
| 0007 | [Turn-based interaction model](0007-turn-based.md)                   | Accepted |

## Conventions

- Filenames: `NNNN-short-kebab-title.md`, four-digit prefix.
- Status values: `Proposed`, `Accepted`,
  `Superseded by ADR-XXXX`, `Deprecated`.
- Never edit an `Accepted` ADR's decision text after the fact.
  If a decision changes, write a new ADR and mark the old one
  `Superseded`.
- Cross-link related ADRs by relative path.
