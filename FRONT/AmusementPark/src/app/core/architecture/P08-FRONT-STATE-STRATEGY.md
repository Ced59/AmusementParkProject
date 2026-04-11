# P08 — Front state strategy for Angular 21

## Goal

P08 formalizes a reusable state strategy so feature migrations stop relying on scattered
`subscribe()` calls, imperative `ChangeDetectorRef.markForCheck()` usage and ad hoc booleans.

This phase does **not** require every screen to be rewritten at once. It introduces the central,
clean-architecture-compatible primitives and applies them on a representative set of screens.
The remaining screens can then migrate feature by feature before P09.

## Official doctrine

### Preferred primitives

- `signal()` for local writable state
- `computed()` for derived state
- `AsyncPipe` when a view still consumes an observable stream directly
- `takeUntilDestroyed()` when an imperative subscription is still needed

### What should become rare

- manual `ChangeDetectorRef.markForCheck()` for routine screen refreshes
- duplicated `loading`, `error`, `empty` booleans in each component
- list filtering state split across unrelated properties without a dedicated façade

## Official layering

### Shared reusable primitives

Reusable screen-state contracts and generic helpers live in:

```text
src/app/shared/models/contracts/
src/app/shared/state/
```

They remain framework-light and feature-agnostic.

### Feature-specific orchestration

Feature state façades live in the corresponding feature folder, for example:

```text
src/app/features/admin/users/state/
src/app/features/admin/operators/state/
```

A façade can depend on `data-access` services and shared contracts, but components should avoid
re-implementing the orchestration logic once a façade exists.

## Screen-state convention

Each feature screen should converge toward a single source of truth based on `ScreenState<TData>`:

- `loading`
- `ready`
- `empty`
- `error`

The façade owns the state transitions.
The component consumes signals and keeps only view concerns.

## Migration rule from P08 onward

For any new or refactored screen:

1. move loading/error/empty orchestration into a feature façade;
2. expose writable state through signals and derived state through `computed()`;
3. keep `ChangeDetectorRef` only for genuine interop edge cases;
4. avoid recreating local screen-state helpers inside components.

## Representative applications delivered in P08

The first migrated samples are:

- admin users list
- admin operators list
- admin manufacturers list
- admin parks list

These samples define the pattern to reuse for the remaining screens before the next large phase.
