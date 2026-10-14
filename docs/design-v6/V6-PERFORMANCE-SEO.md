# V6 — performance, SEO et SSR

## Baseline

- Base : `origin/master@7f6a9088cb20e1fa6e450b2e8f4bacb9e2019a22`, version 5.4.101.
- Accueil public observé : 6 scripts, 1 feuille de style, 18 images présentes dans le DOM initial, dont 7 explicitement lazy.
- Aucun avertissement ni erreur console pendant la capture ciblée.
- Les métriques réseau détaillées seront produites par les outils du dépôt et la CI ; l’API de navigateur isolée utilisée pour les captures n’expose pas les entrées Performance Timing.
- La dernière mesure PageSpeed versionnée avant la branche donne, pour l’accueil FR, 58 mobile (FCP 5,0 s, LCP 12,2 s, TBT 210 ms, CLS 0) et 94 desktop (FCP 0,6 s, LCP 1,1 s, TBT 80 ms, CLS 0,085). Elle sert de repère historique, pas de donnée terrain CrUX.

## Budgets et invariants

- Budget initial Angular confirmé dans `angular.json` : avertissement 950 kB, erreur 1 MB.
- Budget de style composant confirmé : avertissement 100 kB, erreur 150 kB.
- Budgets outillés : initial 1 000 000 octets, plus gros lazy chunk 300 000, JavaScript total 7 200 000, CSS total 250 000.
- Aucune dépendance UI, police ou icône ajoutée.
- Aucun chargement d’image ou vidéo hors viewport supplémentaire.
- `@defer`, lazy routes, `loading="lazy"`, `srcset`, `sizes`, cache, `TransferState`, hydratation et préchargements SEO restent intacts.
- Aucun serveur local ne sera démarré sans autorisation explicite.

## Contrôle accueil P04

Le panneau des quatre parcs hero réutilise `app-image-display` et les identifiants issus de l’API. Seul le premier média visible reçoit `loading="eager"` et `fetchpriority="high"` ; les trois suivants restent lazy, avec `srcset`, `sizes`, dimensions réservées et placeholder. Le bloc des derniers contenus conserve `@defer (on viewport; hydrate on viewport; prefetch on idle)`. Aucun endpoint, requête initiale ou dépendance n’est ajouté.

## Contrôles P21

- `npm run build -- --configuration production`
- `npm run test:ci`
- `npm run i18n:check`
- `npm run quality:accessibility-i18n`
- `npm run architecture:facade-ports`
- `npm run architecture:one-class-per-file`
- `npm run architecture:product-analytics`
- `npm run performance:baseline`
- `npm run performance:bundle`
- `npm run test:performance-tools`
- `npm run seo:ssr-smoke`

Les suites sont exécutées en série, avec contrôle de RAM avant et pendant. La CI fournit la preuve finale si une suite locale enfreint la marge de sécurité.

## SEO/SSR à préserver

Codes 200/301/404, canonical, hreflang réels, locale Open Graph, JSON-LD fiable, robots/noindex, sitemap, HTML SSR initial et absence de discordance d’hydratation. Les routes admin, compte, auth, techniques et erreurs ne deviennent pas indexables.

