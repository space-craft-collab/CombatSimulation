# Security Policy

## Scope

Orleans Monster Arena is a public **showcase**
project. It is not run as a production service
and holds no real user data.

## Supported versions

Only the tip of `main` is supported. There are
no maintained release branches.

## Reporting a vulnerability

Please report privately — do **not** open a
public issue.

Use GitHub
[private vulnerability reporting](https://github.com/space-craft-collab/CombatSimulation/security/advisories/new).

Expect an acknowledgement within 7 days. As a
solo hobby project, fixes are best-effort.

## Dependencies

- NuGet versions are centrally pinned in
  `Directory.Packages.props`
- Dependabot opens weekly update PRs
- `TreatWarningsAsErrors` promotes NuGet audit
  warnings (`NU1902`/`NU1903`) to build failures
