# V6 — design tokens Playground Edition

## Principes

La V6 est une interface éditoriale documentée et ludique : papier, encre, filets visibles, photographie prioritaire, grands repères numériques et géométrie essentiellement droite. Les accents orange, lime et bleu signalent une action ou une information ; ils ne remplacent pas la hiérarchie sémantique.

Les variables V6 alimentent les alias `--app-*` et `--ap-*` existants. Aucun second système de thème n’est créé et la logique `body.light-mode` / `body.dark-mode`, l’initialisation SSR et le stockage de préférence restent inchangés.

## Palette contractuelle

| Rôle | Clair | Sombre | Alias existants |
|---|---|---|---|
| Papier | `#f7f6f0` | `#151a17` | `--bg`, `--app-bg`, `--ap-bg-main` |
| Encre | `#19201d` | `#f5f5ec` | `--text`, `--heading`, `--app-text` |
| Texte secondaire | `#57605a` | `#bbc5bc` | `--text-muted`, `--app-text-muted` |
| Filet principal | `#aeb7ae` | `#67716a` | `--border-2`, `--app-border-strong` |
| Filet secondaire | `#d8ddd5` | `#38433b` | `--border`, `--app-border` |
| Surface | `#eeefe8` | `#252c26` | `--bg-card`, `--app-surface` |
| Champ | `#ffffff` | `#1c241e` | surface des contrôles |
| Orange | `#e54b28` | `#ff7551` | `--c-orange`, `--app-primary` |
| Lime | `#d5ed60` | `#d1ec70` | `--c-lime`, succès/accent |
| Bleu/teal | `#0c8ca3` | `#49c9df` | `--c-sky`, lien/technique |
| Rose secondaire | `#bd315d` | `#ff8fae` | `--c-rose`, danger/variation éditoriale |
| Or secondaire | `#9a6500` | `#f5c451` | `--c-gold`, avertissement/variation éditoriale |
| Violet secondaire | `#7250a8` | `#c4a7ff` | `--c-purple`, variation éditoriale |
| Carte | `#e0e8de` | `#243c34` | fond du shell cartographique |

## Correspondance V5 → V6

| Token existant | V5 clair / sombre | V6 clair / sombre |
|---|---|---|
| `--bg` | `#fff9f0` / `#0f0b06` | `#f7f6f0` / `#151a17` |
| `--bg-card` | `#ffffff` / `#1a140f` | `#eeefe8` / `#252c26` |
| `--text` | `#2a1a08` / `#f6eed9` | `#19201d` / `#f5f5ec` |
| `--text-muted` | `#5f411e` / `#b09d7a` | `#57605a` / `#bbc5bc` |
| `--border` | brun translucide | `#d8ddd5` / `#38433b` |
| `--c-orange` | `#ff5a1f` | `#e54b28` / `#ff7551` |
| `--c-lime` | `#c4ff27` | `#d5ed60` / `#d1ec70` |
| `--c-sky` | `#38c5f5` | `#0c8ca3` / `#49c9df` |
| `--content-max-width` | `1340px` | `1160px` |
| `--radius-md` | `1rem` | `0.375rem` |
| `--radius-2xl` | `2.5rem` | `1rem` |
| `--shadow-card` | halo de 24 px | filet d’élévation de 1 px |

Le texte courant vise au moins 4,5:1 ; le texte large et les séparateurs essentiels au moins 3:1. Le texte posé sur le lime et sur les accents secondaires clairs du thème sombre reste foncé. Les variantes rose, or et violet atteignent au moins 4,5:1 sur leur surface teintée à 14 %. Le focus ne dépend jamais uniquement de la couleur.

Les contrôles pleins utilisent des premiers plans adaptés à chaque ton et état. En clair, `--on-accent: #101512` est réservé aux aplats orange/teal/lime ; le survol orange/teal utilise `--app-on-accent-hover: #ffffff`. Les boutons or/rose/violet utilisent un texte blanc et assombrissent leur aplat au survol. En sombre, ces textes restent `#151a17` et les aplats s'éclaircissent au survol. Le test de contraste contrôle les deux états de chaque ton à 4,5:1 minimum.

Les alias `--app-field`, `--app-map`, `--app-line`, `--app-line-weak` et `--app-on-accent` sont résolus sur `body`, où vivent leurs valeurs de thème. Leur ancienne déclaration sur `:root` ne pouvait pas lire les variables définies sur le descendant `body` ; le test Chromium a reproduit cinq valeurs calculées vides avant correction puis passe dans les 30 cas après correction. Les petits libellés de recherche utilisent les premiers plans sémantiques contrastés (`--app-primary-strong`, `--app-accent-cyan`, `--app-accent-lime`) plutôt que les couleurs décoratives.

Le diagnostic sur les 20 routes du commit `ce3f7b800` a relevé des petits textes encore décoratifs (kickers, navigation desktop, liens de notation), des chips primaires à 4,15:1 et du texte discret à 4,11:1 en clair. Corrections en recette : `--app-primary-strong` clair réutilise `--app-navigation-active-text` (`#9a2c15`), le rose sémantique devient `#9c1643` et `--text-faint` devient `#5e6761`. Les aplats orange/rose et leurs couleurs de bouton ne sont pas remplacés par ces premiers plans. Les tests calculent aussi le contraste sur les vrais fonds teintés/composités, pas uniquement sur le papier général ; la prochaine CI doit confirmer les mesures navigateur.

Les six alias `--type-*` rejoignent également le bloc `body` existant de `_tokens.scss` : leur valeur suit alors les accents du thème courant, au lieu d'hériter de couleurs déjà résolues sur `:root`. Les surcharges sémantiques claires de catégories sont conservées. Le badge de jour courant utilise le premier plan or de chaque thème et les petits compteurs de galerie l'encre du thème sur leur double fond teinté.

## Géométrie et densité

- Largeur éditoriale courante : `1160px`.
- Largeur dense/admin : l’alias large existant reste disponible jusqu’à `1480px`.
- Rayons : 2–8 px en règle générale ; 12–16 px uniquement pour des surfaces explicitement adoucies ; pilule réservée aux petits contrôles et statuts.
- Ombres : une séparation légère seulement lorsqu’un filet ou un changement de surface ne suffit pas.
- Pas de gradients décoratifs, glassmorphism, texture de bruit ni halo systématique.
- Cibles tactiles : au moins 44 px lorsque le contrôle est isolé ; tableaux admin compacts avec focus et libellé conservés.

Les gradients restants sont bornés et vérifiables : rendu historique du `.fun` dans le wordmark protégé, shimmer du skeleton avec arrêt sous `prefers-reduced-motion`, grille de fond et relief des marqueurs cartographiques. Ils ne définissent aucune surface de page, carte, formulaire, navigation ou panneau.

## Typographie

- `Plus Jakarta Sans` reste la police de lecture locale principale.
- `Bebas Neue` peut servir de repère éditorial chiffré ou de display, jamais pour un long texte ni un libellé critique.
- Les titres utilisent des hauteurs de ligne lisibles et aucun `letter-spacing` négatif.
- Les mots longs allemands, néerlandais et polonais utilisent `min-width: 0`, `overflow-wrap: anywhere` aux bons niveaux, sans tronquer l’information essentielle.
- Les descriptions restent de vrais paragraphes ; la casse forcée est limitée aux kickers courts.

## Responsive

Les points structurels du dépôt restent `480`, `680`, `960` et `1180px`. Les mesures de recette sont 320, 360, 390, 414, 480, 680, 768, 960, 1024, 1180, 1366, 1440 et 1920 px, plus faible hauteur, paysage et zoom 200 %.

## Interactions

- Hover : changement de filet, surface ou soulignement ; pas de translation décorative coûteuse.
- Focus : anneau extérieur contrasté, non masqué par overflow.
- Active : état lisible par forme et contraste, pas seulement par couleur.
- Disabled : lisible, non ambigu, curseur et attribut natif conservés.
- Loading : réservation de la taille et `aria-busy` existant conservé.
- `prefers-reduced-motion: reduce` neutralise les mouvements non essentiels.

## Marque protégée

La structure et les styles de `.app-brand-wordmark` sont conservés pour le nom `AMUSEMENT-PARKS.fun` dans la navbar et le footer. Le pictogramme autonome n’est pas sanctuarisé. Le footer conserve les clés `footer.brand.sloganData` et `footer.brand.sloganFun` ; en français : « La rigueur des données » et « pour toujours plus de fun ».

## Preuves P01

- Le moteur de thème, ses sélecteurs et le script de premier rendu ne sont pas modifiés.
- Les surfaces de page des deux thèmes sont plates : aucun gradient ni bruit SVG dans `base` ou les thèmes.
- Le test `playground-v6-tokens.test.mjs` contrôle les valeurs, la largeur, les rayons et les contrastes AA du texte courant/secondaire.
- Le test de texture a été converti en garde anti-texture V6.
- Contrôle ciblé exécuté : 4 tests réussis, 0 échec.

## Preuves P02

- Boutons et champs partagés : géométrie carrée, focus commun, cibles d’au moins 40–44 px selon la densité.
- Cards et surfaces : aplats, filets, accents supérieurs ou latéraux, sans translation décorative au hover.
- États : loading, empty, error et skeleton conservent leurs sélecteurs et leur contenu.
- Primitives denses : tables, pagination, tabs, dialogs, toasts et éditeur riche restent couvertes dans `_ui-primitives.scss`.
- Shells cartographiques : vraie carte inchangée, réserve de hauteur conservée, fond `--app-map` pour les placeholders.
- Tests ciblés P01/P02 : 7 réussis, 0 échec.
- Vérification architecture : 160 facades conformes aux ports.
- Le contrôle a11y complet requiert `@angular/compiler`, absent tant que `npm ci` n’a pas été exécuté ; il est bloquant à P21/CI et non ignoré.

## Application P03 aux shells

- Header : filet orange supérieur, surface opaque, navigation active soulignée, contrôles carrés et grille à deux rangées sous 680 px pour ne pas tronquer le wordmark.
- Navigation mobile : barre pleine largeur, filet supérieur, réserve de `safe-area`, aucune carte flottante ni flou d’arrière-plan.
- Trail et arbre de parc : panneaux plats, niveaux et états courants lisibles sans modifier les commandes de route ni les paramètres.
- Footer : composition éditoriale marque + trois colonnes, repli 3 puis 1 colonne, slogan intact et contraste AA contrôlé.
- Compte/admin : surfaces plates et denses ; les outlets, guards et comportements existants ne sont pas déplacés.
- Le test `playground-v6-shell.test.mjs` protège la composition de marque, les contrôles de navigation, les dimensions et les invariants responsive.

