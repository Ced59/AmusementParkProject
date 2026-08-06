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

- For implementation tasks, always start from a new branch based on `origin/master`, then implement, verify, commit, push, open a pull request targeting `master`, monitor its checks and reviews, merge it, and monitor the resulting deployment when the repository workflow deploys `master`. Do not stop at PR creation unless the user explicitly asks to stop earlier or a genuine blocker requires user input.
- Name branches with an intent prefix such as `feat/`, `fix/`, `chore/`, `docs/`, `test/`, or `perf/` depending on the change. Do not use a generic `codex/` prefix.
- Keep pull requests small, focused, and easy to review.
- Open pull requests as ready for review by default. Use a draft only when the user explicitly requests one or when the work is knowingly incomplete and cannot yet be reviewed safely.
- Increment the release version in every PR unless the user explicitly asks for a major or intermediate version increment instead. Always base that increment on the current `origin/master` release version, not on the local `master` branch or the current working branch.
- When incrementing a release version, update `FRONT/AmusementPark/release-version.json`: set the new version and add or update the matching history entry with the release date and short non-technical localized labels for every supported language.
- For admin-only changes, keep release-version labels generic and avoid describing sensitive or precise back-office capabilities. Prefer wording such as "admin ergonomics improvements".
- Do not mix unrelated backend, frontend, SEO, security, UI, deployment, and refactoring changes in one PR.
- Respect the current architecture and naming conventions.
- Do not introduce shortcuts that bypass validation, authorization, domain rules, or application services.
- Do not remove files unless the deletion is clearly justified and listed in the PR summary.
- When extracting shared frontend components or services, use the shared implementation directly instead of keeping no-op wrappers around the old local abstraction.
- Do not silently change public contracts, route URLs, DTO shapes, database behavior, localization behavior, or SEO behavior.
- Prefer incremental, testable changes over broad rewrites.
- Add or update tests for every behavior change.
- Every new feature must include relevant unit tests.
- All application emails must use a rich branded HTML template consistent with the site visual identity, escape dynamic content safely, and keep a readable plain-text alternative for SMTP delivery.
- Always pay close attention to performance impact. The production VPS target is modest, so avoid unnecessary CPU work, memory pressure, network payload, bundle weight, synchronous blocking work, and repeated runtime computations.
- Do not test by starting a local frontend or backend server on the user's PC unless the user explicitly asks for it.
- Do not spend time running heavy local test/build suites when the CI pipeline will run them after the PR is opened. Prefer lightweight local checks when useful, then monitor the CI runs and fix any failures from those runs.
- If `.codex-remote-attachments/` exists in the local repository, remove it before committing or pushing.
- When unsure, inspect the existing pattern and follow it.

## Park data completion requests

- Treat a request such as `Complète le parc <nom>`, `Intègre le parc <nom>` or an equivalent formulation as a data-integration operation, not as a request to change application code or to work directly in the administration UI.
- Before acting, read `docs/codex-guidelines/README.md`, `docs/codex-guidelines/park-data-integration-orchestrator.md`, every applicable file from steps 0 through 8, and `docs/codex-guidelines/codex-park-data-editor-api-workflow.md` when Codex performs the work.
- Codex must use the dedicated `PARK_DATA_EDITOR` API workflow from end to end. It must not fall back to direct administration, a browser admin session, direct database access, or improvised endpoints. Missing or revoked credentials are a blocker to report, not permission to bypass this workflow.
- Use the already provisioned `admin@amusement-parks.fun` account only through its locally encrypted credential and the scoped token flow documented there. Never register that address again, expose its password or token, or use any broader account privilege through another channel.
- The short completion request authorizes autonomous execution of every applicable step, including research, bounded Preview/Apply lots, refreshed exports, private image imports and the final audit. It does not authorize publication of the park, its new content or its media, deletion, hiding previously public content, or cleanup of an unknown legacy entity.
- Keep a new or currently hidden park in its review state until the user explicitly authorizes publication. Preserve the visibility of an already public park while enriching it unless a separate correction has been explicitly requested.
- Apply the same high editorial standard used for a major reference park to every newly completed park. The rigor is constant even when the amount of available content varies: current and historical inventory, permanently closed attractions, recent durable announcements, useful articles, official logo, representative park imagery, contextual images, eight-language public copy, verified sources and documented gaps must all be audited.
- A park is not complete merely because the API accepts its JSON or a numeric score crosses a threshold. Report quantitative coverage for attractions, descriptions, images, history, articles and logo, and leave only gaps that remain after a genuine source search.
- Public text, including image descriptions, alternative text and captions, must be natural and editorial. Never expose technical, mechanical, audit-oriented or tool-oriented wording to visitors.
- Descriptions must explain the identity, setting and experience of the entity itself. Never tell visitors how to organize their day, reserve an item for a profile, take a break from queues or fit content into an itinerary, and never reuse category-wide filler paragraphs across distinct entities.
- Step 8 must audit the complete public-text corpus after stripping headings and entity names so that repeated paragraph bodies, translated boilerplate and generic article subtitles cannot be hidden by otherwise unique HTML. A score of 100 never replaces this corpus review.
- Audit global founders, operators and manufacturers used by the park, but do not rewrite a shared reference casually: any change that would affect other parks requires an explicitly cross-park, fact-checked scope.
- A data-only completion run does not require a repository branch, release bump or pull request. Use the repository delivery workflow only when the user also requests documentation or code changes, as in any other implementation task.

## Backend rules

- Respect clean architecture boundaries.
- Keep pure business logic and pure domain-calculated data in `API/AmusementPark.Core` entities, value objects, or domain services. Application, WebAPI, Infrastructure and frontend layers may collect facts, orchestrate use cases, persist data, map DTOs, or display results, but must not own business rules that belong to the domain.
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
- When creating a deep public page, update the visible public breadcrumb/navigation trail and the BreadcrumbList JSON-LD with contextual, localized labels and clickable parent links. Do not leave generic labels when entity names are available.
- Public-facing copy must use a consistent informal tone in every supported language. In French, use tutoiement instead of vouvoiement for public, auth, account, SEO and sharing texts.
- Localized copy, SEO metadata, JSON-LD labels, release notes, and admin-visible labels must use proper accents, diacritics, punctuation, and language-specific characters. Do not leave ASCII transliterations such as `Oeff`, `fuer`, `piu`, `mas`, `publica`, `parkow`, or French text without accents unless the word is intentionally technical, a route segment, an identifier, or a product name.
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
- PR descriptions must be written in French.
- Short summary of the change.
- Why the change was needed.
- Tests run.
- Risk areas.
- Files intentionally deleted, if any.
- Any known limitation or follow-up task.

Before merging every PR:

- Wait for all required CI checks to complete and fix relevant failures.
- Inspect top-level comments, reviews, and unresolved review threads.
- Address every relevant and actionable comment, rerun the appropriate checks, and update the PR before merging.
- Do not implement irrelevant, outdated, duplicate, or behavior-regressing feedback blindly; document why it is not applied when a response is useful.
- Merge only when the PR is ready, required checks are green, and no relevant actionable review feedback remains.

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
- The ready-for-review PR has been checked for relevant comments, merged into `master`, and its deployment has completed successfully when deployment is part of the repository workflow.

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
