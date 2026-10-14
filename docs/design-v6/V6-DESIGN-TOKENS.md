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

Le texte courant vise au moins 4,5:1 ; le texte large et les séparateurs essentiels au moins 3:1. Le texte posé sur le lime reste foncé. Le focus ne dépend jamais uniquement de la couleur.

## Géométrie et densité

- Largeur éditoriale courante : `1160px`.
- Largeur dense/admin : l’alias large existant reste disponible jusqu’à `1480px`.
- Rayons : 2–8 px en règle générale ; 12–16 px uniquement pour des surfaces explicitement adoucies ; pilule réservée aux petits contrôles et statuts.
- Ombres : une séparation légère seulement lorsqu’un filet ou un changement de surface ne suffit pas.
- Pas de gradients décoratifs, glassmorphism, texture de bruit ni halo systématique.
- Cibles tactiles : au moins 44 px lorsque le contrôle est isolé ; tableaux admin compacts avec focus et libellé conservés.

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

