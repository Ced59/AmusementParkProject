# V6 — design tokens Playground Edition

## Principes

Audit complémentaire du 17 octobre : les références CSS de 1 895 fichiers de production (SCSS, templates et styles inline TypeScript) contenaient 57 noms absents de toute déclaration, dont 38 utilisés sans fallback. Les consommateurs sont raccordés aux tokens **existants**, dans 75 fichiers de présentation ; aucun nouveau moteur de thème ni alias global de compatibilité n'est ajouté. Après correction : zéro nom inconnu obligatoire, seulement les deux références externes optionnelles avec repli `--p-primary-color` et `--app-public-header-height`. Le test vérifie aussi les bindings Angular et `setProperty`. C'est une couverture **lexicale**, pas une preuve de résolution dans chaque scope CSS ; les rendus CI restent obligatoires.

| Anciennes références | Tokens existants utilisés | Rôle préservé |
|---|---|---|
| `text-color`, `text-primary` / `text-color-secondary`, `app-muted` | `app-text` / `app-text-muted` | Encre / texte secondaire |
| `surface-ground` / `surface-card`, `surface-100`, anciens alias panel/admin | `app-bg` / `app-surface` | Papier / panneau |
| `surface-50`, `app-surface-alt` / `surface-0`, `app-surface-raised` | `app-surface-2` / `field` | Surface secondaire / champ |
| `surface-border`, `color-border` | `app-border-stronger` | Filet essentiel |
| `primary-color` | `app-primary-strong` pour le texte, `app-primary` pour l'aplat | Encre distincte du signal décoratif |
| `app-info`, `app-accent`, anciens `color-sky*` | `app-accent-cyan` pour le texte, `c-sky` pour l'aplat | Bleu sémantique / bleu décoratif |
| Anciens `green*`, `yellow*`, `pink*`, `color-lime*`, `color-gold*` | Encres `app-accent-lime/gold/pink`, aplats `c-lime/gold/rose` selon la propriété | État et type de contrôle conservés |
| `app-error`, anciens `red*`, `color-danger*` | `app-danger` | Erreur lisible dans les deux thèmes |
| `app-primary-contrast`, `app-on-primary`, ancien `app-background` utilisé en premier plan | `app-on-accent` | Texte sur aplat |
| `shadow-lg`, `shadow-popover` | `app-shadow-2` | Élévation existante |

Les boutons de compatibilité de la directive partagée distinguent maintenant leur aplat (`--button-bg`) de leur encre outline/text/link (`--button-ink`, propriété locale). Succès utilise le lime décoratif et son encre sémantique, warning/danger/help les premiers plans or/rose/violet déjà présents. Les signatures, événements, labels, états disabled et dimensions ne changent pas. Tests numériques : chaque sévérité pleine et ses variantes sur quatre surfaces, dans les deux thèmes, à 4,5:1 minimum. L'avertissement d'adresse e-mail non confirmée rejoint `--app-primary-strong`, son erreur `--app-danger`, après la mesure CI à 3,38:1. **157 tests Node ciblés passent ; nouvelles captures requises.** La marque protégée n'est pas modifiée par ce raccordement.

La revue de cascade dense admin trouve ensuite une surcharge `color:#fff` sur les boutons primaires, insuffisante sur l'orange des deux thèmes. Seul ce premier plan rejoint `--app-on-accent` ; les autres règles de densité/sévérité restent inchangées. Le test conserve le blanc défaillant et exige l'encre AA ainsi que sa liaison réelle à ce selector. **36 tests tokens/admin passent** après cette correction source ; aucun rendu admin opérationnel n'est inventé.

Le petit libellé actif de la toolbar publique admin utilise également `--app-primary-strong` sur son fond teinté à 12 %, désormais composé depuis `--c-orange` plutôt qu'un ancien RGBA fixe. Les tests calculent le contraste dans les deux thèmes. Les règles du host ciblent son sélecteur réel, adapté à son encapsulation None déjà existante ; ni le moteur de thème ni le mode d'édition ne changent. La recette conditionnelle admin-shell et ses nouveaux rendus sont décrits dans le rapport visuel ; **162 tests Node ciblés passent**, sans certification d'administration réelle.

Le run suivant `f975d9bc4 / 38903181087` valide le kicker bleu mobile puis mesure le kicker or du profil desktop à 4,25:1. `--app-light-accent-gold` devient `#794500`, avec son alias sémantique relié. Un garde numérique supplémentaire vérifie les cinq tons de petits kickers sur cinq fonds de cartes teintés existants ; il révèle aussi une combinaison lime/orange insuffisante, corrigée par `--app-light-accent-lime: #495a00` et son alias. Les 17 tests tokens passent, dont les deux fonds réellement mesurés et les 25 compositions ; les nouveaux rendus restent exigés. Palette canonique, wordmark, fonds et sombre inchangés.

La CI `7115bb901 / 38900388260` mesure deux kickers du profil clair à 4,25:1 sur leur fond réellement composé. L'alias de texte `--app-light-accent-sky` passe de `#086b7c` à `#075f6e` et `--app-accent-cyan` le réutilise. Le bleu canonique de la maquette, les fonds, le sombre et la marque ne changent pas. Un test conserve la composition mesurée comme cas de régression sous 4,5:1 avec l'ancienne couleur et au-dessus avec la nouvelle ; cela ne remplace pas le nouveau rendu navigateur exigé.

Le shell compte reçoit deux noms de régions/groupes localisés, jusque-là figés en anglais. Deux nouvelles clés sont ajoutées dans chaque langue (7 322 clés au total), sans modifier une valeur existante ni les slogans. Le groupe de repères reçoit la sémantique `group`, sans événement ou logique supplémentaire. Le décor vide `home-hero__backdrop`, déjà `display:none` dans les deux thèmes, et ses deux règles mortes sont retirés : aucun contenu, image ou contrôle n'est supprimé. Rendu et mesure P04.6 du prochain build restent requis, plafond JS inchangé.

Quatre messages de secours auth sont ensuite localisés dans les huit langues (7 326 clés), avec textes français et messages serveur préservés. Cette correction de présentation, documentée dans la recette et le gate, ne touche ni aux tokens visuels ni aux mécanismes d'authentification. Elle exige ses nouveaux tests et rendus CI avant acceptation.

La revue hydratée du popup révèle une surcharge locale de catégorie encore en brun V5, prioritaire sur son texte générique. Elle est raccordée à `--app-primary-strong` dans le SCSS existant ; un test protège cette surcharge. L'accent fixe cyan de la saisie historique admin rejoint également `--c-sky`. Les valeurs historiques encore présentes comme fallbacks de `var()` ne sont pas prises pour des couleurs calculées : les alias réellement résolus et les états doivent être contrôlés, sans toucher aux couleurs protégées du wordmark.

Audit des styles locaux du 16 octobre : 106 rayons fixes de panneaux/champs/médias, répartis dans 41 feuilles public/compte/admin, sont raccordés à `--app-radius-md`. Les deux badges explicitement en pilule utilisent `--app-radius-pill` ; avatars et pourcentages circulaires restent distincts. Aucun token concurrent, wrapper, dimension, condition ou handler n'est ajouté par ce raccordement. Les 41 feuilles compilent séparément sans chemin Sass ajouté ; un test parcourt tous les styles locaux pour empêcher le retour de gros rayons numériques. Ce contrôle de cohérence n'est pas une preuve de recette visuelle complète.

Les alias de rayon `xs` à `2xl` sont ensuite plafonnés ensemble à `0.125rem` (2 px) : la première adaptation gardait encore des rayons de 6–16 px sur certaines surfaces, non conformes à la géométrie quasi carrée de référence. Les noms et familles existants sont conservés. `--app-surface-muted`, déjà utilisé dans six déclarations mais absent du moteur, est relié à `--app-surface-2` sur `body`, au niveau réel des thèmes ; les captures SSR vérifient désormais aussi sa résolution. Les petits liens d'offre et la légende Max utilisent le premier plan contrasté, sans recolorer les séries météo. **190 tests Node passent**, dont les contrastes sur surfaces principales/secondaires dans les deux thèmes ; le rendu après ces corrections exige une nouvelle CI.

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
| `--radius-md` | `1rem` | `0.125rem` |
| `--radius-2xl` | `2.5rem` | `0.125rem` |
| `--shadow-card` | halo de 24 px | filet d’élévation de 1 px |

Le texte courant vise au moins 4,5:1 ; le texte large et les séparateurs essentiels au moins 3:1. Le texte posé sur le lime et sur les accents secondaires clairs du thème sombre reste foncé. Les variantes rose, or et violet atteignent au moins 4,5:1 sur leur surface teintée à 14 %. Le focus ne dépend jamais uniquement de la couleur.

Les contrôles pleins utilisent des premiers plans adaptés à chaque ton et état. En clair, `--on-accent: #101512` est réservé aux aplats orange/teal/lime ; le survol orange/teal utilise `--app-on-accent-hover: #ffffff`. Les boutons or/rose/violet utilisent un texte blanc et assombrissent leur aplat au survol. En sombre, ces textes restent `#151a17` et les aplats s'éclaircissent au survol. Le test de contraste contrôle les deux états de chaque ton à 4,5:1 minimum.

Les alias `--app-field`, `--app-map`, `--app-line`, `--app-line-weak` et `--app-on-accent` sont résolus sur `body`, où vivent leurs valeurs de thème. Leur ancienne déclaration sur `:root` ne pouvait pas lire les variables définies sur le descendant `body` ; le test Chromium a reproduit cinq valeurs calculées vides avant correction puis passe dans les 30 cas après correction. Les petits libellés de recherche utilisent les premiers plans sémantiques contrastés (`--app-primary-strong`, `--app-accent-cyan`, `--app-accent-lime`) plutôt que les couleurs décoratives.

Le diagnostic sur les 20 routes du commit `ce3f7b800` a relevé des petits textes encore décoratifs (kickers, navigation desktop, liens de notation), des chips primaires à 4,15:1 et du texte discret à 4,11:1 en clair. Corrections en recette : `--app-primary-strong` clair réutilise `--app-navigation-active-text` (`#9a2c15`), le rose sémantique devient `#9c1643` et `--text-faint` devient `#5e6761`. Les aplats orange/rose et leurs couleurs de bouton ne sont pas remplacés par ces premiers plans. Les tests calculent aussi le contraste sur les vrais fonds teintés/composités, pas uniquement sur le papier général ; la prochaine CI doit confirmer les mesures navigateur.

Les six alias `--type-*` rejoignent également le bloc `body` existant de `_tokens.scss` : leur valeur suit alors les accents du thème courant, au lieu d'hériter de couleurs déjà résolues sur `:root`. Les surcharges sémantiques claires de catégories sont conservées. Le badge de jour courant utilise le premier plan or de chaque thème et les petits compteurs de galerie l'encre du thème sur leur double fond teinté.

`--app-logo-surface: #f7f6f0` est un fond neutre réservé aux logos d'entités transparents (hero accueil, cartes, résultats et cadre de logo parc), identique dans les deux thèmes. La capture sombre de Six Flags Over Georgia révèle que son lettrage noir disparaît sur le fond cartographique sombre ; l'image originale n'est ni inversée, ni recolorée, ni remplacée. Les photos, cartes, placeholders sans logo et le wordmark protégé gardent leurs traitements propres. Ce token s'intègre au SCSS existant, sans moteur de thème parallèle.

## Géométrie et densité

Le header ParkItem utilise un cadre photo 16/10 distinct, une grille desktop 1,55 / 0,85 avec 22 px de séparation, puis une seule colonne à 960 px. Titres 34–57 px, 30–41 px au mobile. Les contrôles réels restent en flux, comme les liens précédent/suivant ; aucun texte n'est posé sur une photographie estompée. Le gabarit image est partagé entre vue et preload. Les valeurs de `sizes` décrivent le slot de viewport et non les données du lieu ; un éventuel gutter natif de navigateur peut réduire le rendu sans modifier le contrat d'image. Le trail mobile se replie sur plusieurs lignes au lieu de tronquer le nom courant.

- Largeur éditoriale courante : `1160px`.
- Largeur dense/admin : l’alias large existant reste disponible jusqu’à `1480px`.
- Rayons de surface : 2 px via les alias conservés `xs` à `2xl`, ou 0 explicite sur les filets éditoriaux ; aucun panneau 12–16 px. Les petits contrôles peuvent garder leur géométrie spécifique ; pilule et cercle restent réservés aux contrôles/statuts et avatars, pas aux surfaces de page.
- Ombres : une séparation légère seulement lorsqu’un filet ou un changement de surface ne suffit pas.
- Pas de gradients décoratifs, glassmorphism, texture de bruit ni halo systématique.
- Cibles tactiles : au moins 44 px lorsque le contrôle est isolé ; tableaux admin compacts avec focus et libellé conservés.

Les gradients restants sont bornés et vérifiables : rendu historique du `.fun` dans le wordmark protégé, shimmer du skeleton avec arrêt sous `prefers-reduced-motion` et grille de fond cartographique. Le relief brillant des marqueurs, repéré lors de la recette hydratée, a été retiré dans `725ced59c` au profit des aplats thématiques. Aucun gradient décoratif ne définit une surface de page, carte, formulaire, navigation ou panneau.

## Typographie

- Les titres/display utilisent `Arial, Helvetica, sans-serif` et le texte courant `system-ui, -apple-system, "Segoe UI", Arial, sans-serif`, comme le HTML V6 fourni. Aucun téléchargement ni nouvelle police n'est ajouté.
- `Bebas Neue` et ses fallbacks historiques restent isolés dans `.app-brand-wordmark`, y compris pour le point, avec le `.fun` en Satisfy existant. Ils ne définissent plus la typographie des titres éditoriaux.
- Les titres utilisent des hauteurs de ligne lisibles et aucun `letter-spacing` négatif.
- Les mots longs allemands, néerlandais et polonais utilisent `min-width: 0`, `overflow-wrap: anywhere` aux bons niveaux, sans tronquer l’information essentielle.
- Les descriptions restent de vrais paragraphes ; la casse forcée est limitée aux kickers courts.

## Responsive

Les points structurels du dépôt restent `480`, `680`, `960` et `1180px`. Les mesures de recette sont 320, 360, 390, 414, 480, 680, 768, 960, 1024, 1180, 1366, 1440 et 1920 px, plus faible hauteur, paysage et zoom 200 %.

Les gouttières du shell public sont de 48 px au total sur desktop, 30 px sous 680 et 28 px sous 480, avec la largeur maximale de 1160 px. Les points de rupture restent ceux du dépôt ; les valeurs viennent de la maquette. L'accueil reprend son split 1,08/0,92, gap 18–38 px, titre 39–74 px (38–53 px mobile, 35 px sous 350), interligne 1,065/1,09 et média principal de 205 px. Ces alignements nécessitent encore les mesures CI et la revue visuelle ; les données réelles et textes de production restent intacts.

## Interactions

- Hover : changement de filet, surface ou soulignement ; pas de translation décorative coûteuse.
- Focus : anneau extérieur contrasté, non masqué par overflow.
- Active : état lisible par forme et contraste, pas seulement par couleur.
- Disabled : lisible, non ambigu, curseur et attribut natif conservés.
- Loading : réservation de la taille et `aria-busy` existant conservé.
- `prefers-reduced-motion: reduce` neutralise les mouvements non essentiels.

## Marque protégée

La structure et les styles de `.app-brand-wordmark` sont conservés pour le nom `AMUSEMENT-PARKS.fun` dans la navbar et le footer. Le pictogramme autonome n’est pas sanctuarisé. Le footer conserve les clés `footer.brand.sloganData` et `footer.brand.sloganFun` ; en français : « La rigueur des données » et « pour toujours plus de fun ».

L'audit de dépendances confirme que la police display est identique à celle de `origin/master`, mais que l'ancien `color: var(--app-heading)` aurait hérité de la nouvelle encre V6. La couleur du lettrage et du point est donc isolée dans le scope de marque : blanc `#ffffff` en sombre et `#160a00` en clair, exactement les valeurs de production de la base. Le traitement `fun`, ses gradients, filtres, police Satisfy et proportions restent inchangés. Un test protège cette indépendance par rapport à la palette éditoriale.

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

