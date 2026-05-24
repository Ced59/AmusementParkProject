# ui/primitives

Primitives visuelles transverses de la refonte MVP.

## Règles

- Aucune primitive ne dépend d'une route, d'une facade, d'un service métier ou d'un DTO.
- Les directives `appUiButton` et `appUiSurface` préservent les balises sémantiques (`a`, `button`, `section`, `article`, etc.).
- Les composants `app-ui-chip`, `app-ui-kicker`, `app-ui-section-header` et `app-ui-stat-card` ne portent que de la structure UI répétée.
- Les variantes disponibles sont : `primary`, `ghost`, `lime`, `sky`, `rose`, `gold`, `purple`, `soft`.

## Usage rapide

```html
<a appUiButton="primary">Action</a>
<section appUiSurface="panel">...</section>
<app-ui-kicker iconClass="pi pi-star">Label</app-ui-kicker>
<app-ui-chip tone="sky">Badge</app-ui-chip>
<app-ui-section-header titleKey="home.featured.title"></app-ui-section-header>
<app-ui-stat-card labelKey="parks.title" [value]="42"></app-ui-stat-card>
```
