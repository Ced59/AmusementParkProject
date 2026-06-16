# AGENTS.md — AmusementParkProject

## Project overview

AmusementPark is a long-term amusement park portfolio project with a .NET backend and an Angular frontend.

The project must preserve its current architecture, SOLID principles, separation of concerns, and reusable components. Do not rewrite large areas unless explicitly requested.

## Repository layout

- `API/AmusementPark.Core`: domain/core layer.
- `API/AmusementPark.Application`: application/use-case layer.
- `API/AmusementPark.Infrastructure`: persistence, external services, concrete implementations.
- `API/AmusementPark.WebAPI`: HTTP API layer.
- `API/*Tests`: backend test projects.
- `FRONT/AmusementPark`: Angular frontend.
- `.github/workflows`: CI/CD workflows.
- `deploy`: deployment files.
- `docs`: project documentation, roadmaps, architecture, SEO, security and operations notes.

## General rules

- For implementation tasks, always start from a new branch based on `origin/master`, then commit, push, and open a pull request targeting `master`.
- Keep pull requests small, focused, and easy to review.
- Increment the release version in every PR unless the user explicitly asks for a major or intermediate version increment instead.
- Do not mix unrelated backend, frontend, SEO, security, UI, deployment, and refactoring changes in one PR.
- Respect the current architecture and naming conventions.
- Do not introduce shortcuts that bypass validation, authorization, domain rules, or application services.
- Do not remove files unless the deletion is clearly justified and listed in the PR summary.
- Do not silently change public contracts, route URLs, DTO shapes, database behavior, localization behavior, or SEO behavior.
- Prefer incremental, testable changes over broad rewrites.
- Add or update tests for every behavior change.
- Every new feature must include relevant unit tests.
- Always pay close attention to performance impact. The production VPS target is modest, so avoid unnecessary CPU work, memory pressure, network payload, bundle weight, synchronous blocking work, and repeated runtime computations.
- When unsure, inspect the existing pattern and follow it.

## Backend rules

- Respect clean architecture boundaries.
- Keep domain logic out of controllers.
- Keep infrastructure details out of Core and Application.
- Use application services, handlers, ports, and abstractions for orchestration.
- Do not inject infrastructure concerns directly into WebAPI controllers when an application abstraction exists.
- Do not weaken authentication, authorization, validation, rate limiting, security headers, error handling, or audit behavior.
- Preserve nullable reference type correctness.
- Avoid large service classes; split responsibilities when necessary.
- Prefer explicit contracts and small focused methods.
- Use explicit C# types. Do not use `var`.
- Always use braces `{ }`, even for one-line blocks.
- Add or update xUnit tests for behavior changes and edge cases.
- Keep test project structure aligned with the implementation project structure.

## Frontend rules

- Respect the existing Angular architecture.
- Do not inject concrete API services directly into facades when a port abstraction exists.
- Keep components focused on UI.
- Put orchestration in facades/services.
- Keep mapping logic out of templates.
- Reuse existing shared components where relevant.
- Maintain responsive behavior, especially for admin screens.
- Preserve route localization, SEO metadata, canonical URLs, hreflang, robots/noindex, Open Graph, and SSR behavior.
- Add or update tests for facades, mappers, guards, interceptors, ports, and edge cases.
- Do not introduce heavy dependencies without explicit justification.
- Do not move admin-only code into the public initial bundle.

## SEO and SSR rules

- Public pages must emit correct localized metadata.
- A French route must not emit English Open Graph locale metadata.
- Canonical URLs must be stable and match the current localized public route.
- Admin, account, auth, technical, and error routes must not become indexable accidentally.
- Do not declare hreflang alternates for pages that are not really served.
- JSON-LD must be based on reliable data only. Do not invent structured data fields.
- SSR must return useful initial HTML for important public pages.
- A missing public entity should not produce a false indexable 200 response.

## Security rules

- Mutating endpoints must not become public unless explicitly intended.
- Keep CORS restricted to approved origins.
- Keep production hosts, forwarded headers, proxy behavior, and security headers strict.
- Do not commit secrets, tokens, passwords, SMTP credentials, JWT secrets, provider keys, or production environment values.
- Do not weaken rate limiting on authentication, registration, password reset, refresh, or admin actions.
- Production errors must not expose internal exception details.
- Use trace/correlation identifiers when relevant.
- Admin-sensitive actions should be auditable when the existing architecture supports it.

## Backend commands

Run from the repository root unless noted otherwise.

```bash
dotnet restore AmusementPark.sln
dotnet build AmusementPark.sln --configuration Release --no-restore
dotnet test AmusementPark.sln --configuration Release --no-build
```

If the solution name or paths differ, inspect the repository and use the existing solution and test project structure.

## Frontend commands

Run from `FRONT/AmusementPark`.

```bash
npm ci
npm run test:ci
npm run architecture:facade-ports
npm run build -- --configuration production
```

If a script does not exist, inspect `package.json`, use the closest existing script, and mention the difference in the PR summary.

## Pull request expectations

Every PR must include:

- Clear title.
- Short summary of the change.
- Why the change was needed.
- Tests run.
- Risk areas.
- Files intentionally deleted, if any.
- Any known limitation or follow-up task.

Do not open a PR that contains broad formatting churn, unrelated renames, unrelated dependency updates, or unrelated refactors.

## Definition of done

A task is done only when:

- The requested behavior is implemented.
- The diff is small and focused.
- Architecture boundaries are preserved.
- Relevant tests are added or updated.
- Relevant backend and/or frontend commands pass, or failures are clearly explained.
- The PR summary is complete.
- Deleted files are explicitly listed.

## Review guidelines

When reviewing a PR, focus on:

- Architecture boundary violations.
- Missing tests or weak test coverage.
- Broken SSR or SEO behavior.
- Public/admin route exposure mistakes.
- Authentication, authorization, validation, and rate limiting regressions.
- i18n and locale mistakes.
- Silent breaking changes in API contracts.
- Unexpected file deletions.
- Overly large or unfocused diffs.
- Performance regressions on public pages.
- Admin responsive layout regressions.

## Codex task style

Prefer prompts structured as:

```text
Goal:
Describe the exact desired outcome.

Context:
List relevant files, bug reports, logs, screenshots, roadmap items, or previous decisions.

Constraints:
- Keep the PR small and focused.
- Respect the current architecture.
- Add or update tests.
- Do not refactor unrelated code.
- List deleted files in the PR summary.

Done when:
- The behavior is fixed.
- Relevant tests pass.
- The PR summary explains changes, tests, risks, and deleted files.
```

## Forbidden task shapes

Avoid prompts such as:

- "Refactor the whole project."
- "Clean everything."
- "Implement the entire roadmap."
- "Fix all warnings everywhere."
- "Rewrite the frontend architecture."
- "Modernize the backend." 

Instead, split work into small PRs, such as:

- Fix one SEO metadata bug.
- Add tests for one facade.
- Secure one endpoint group.
- Split one oversized class.
- Improve one admin responsive screen.
- Add one missing CI check.
