# V6 — performance, SEO et SSR

## Dernier contrôle borné validé — `791dc2c46`

La [CI `38889015354`](https://github.com/Ced59/AmusementParkProject/actions/runs/38889015354) réussit intégralement, Docker compris : **2 769 tests Angular / 583 fichiers**, **224 tests Node qualité**, **26 GET / 224 assertions HTTP**, **40 routes / 2 080 captures SSR**, **38 détails Fleury**, **307 contrôles hydratés** et **732 captures hydratées additionnelles**, sans exception navigateur ni erreur d'interception. Déploiement ignoré sur PR. [Artefact `12041197882`](https://github.com/Ced59/AmusementParkProject/actions/runs/38889015354/artifacts/12041197882).

Les six paires 390/1 366 px conservent les mêmes populations API master/V6 : **105 → 107 requêtes**, **84 → 86 scripts**, **+3 280 octets JS**, **−851 octets décodés au total** (1 700 561 → 1 699 710). Fonts 3, images 12, CSS 1, document 1, XHR 2 et autres 2 restent inchangés en nombre. Le verdict est **PASS_WITH_OWNER_EXCEPTION**, avec seulement **22 octets** de marge sous le plafond inchangé de +3 302 ; aucune extension de l'exception.

Médianes LCP proxy : **244 → 212 ms** mobile, **364 → 216 ms** desktop. CLS V6 médian nul ; maximum desktop **0,00365**, mobile nul. `domComplete` médian **317,2 → 389,1 ms** mobile et **391,7 → 389,2 ms** desktop : le ralentissement mobile reste visible, sans conclusion générale de gain de performance. Ce sont des proxys de laboratoire, pas une mesure INP ou CWV terrain. Les six variantes du premier thème passent, avec 25–29 frames post-FCP et zéro mauvais thème. Les captures compte sur fixtures isolées, ajoutées ensuite, ne sont pas encore attestées.

## Contrôle antérieur — `754cce98c`

La [CI `38863174263`](https://github.com/Ced59/AmusementParkProject/actions/runs/38863174263) réussit intégralement, Docker compris : 2 728 tests Angular, 200 tests Node qualité, 224 assertions HTTP, 40 routes / 2 080 captures, 38 détails Fleury, 142 contrôles hydratés et 52 captures pratiques. Les six paires restent **105 → 107 requêtes**, avec **+2 854 octets JS** et **+5 500 octets décodés au total** pour cette population. Le manifeste conserve son plafond de +3 302 : il n'est ni augmenté ni réinterprété. Le retrait des variables CSS orphelines explique une partie de la baisse ; aucun nouveau nombre de requêtes API, images ou fonts n'est accepté.

Médianes LCP proxy : **180 → 140 ms** mobile, **224 → 144 ms** desktop ; CLS V6 médian nul, maximum mobile **0,00327**. `domComplete` médian **214,5 → 555,5 ms** mobile et **230,8 → 529,6 ms** desktop. Les populations restent identiques dans chaque paire, pas entre campagnes. Il s'agit toujours d'un laboratoire, pas de CWV terrain ou d'une mesure INP de production. Les corrections et recettes suivantes doivent repasser ces mêmes contrôles.

## Exception propriétaire acceptée — mesure `579060704`

Le propriétaire a explicitement accepté le 16 octobre : **« Oui, accepter cette exception ciblée »** pour les deux requêtes JavaScript du confinement clavier CDK. La portée est enregistrée dans `V6-PERFORMANCE-EXCEPTION.json`, liée à la base exacte `7f6a9088…` et au run `38857089021`. Le contrôle exige exactement deux scripts supplémentaires, au plus **3 302 octets JS décodés supplémentaires**, et aucun changement du nombre de requêtes d'un autre type (dont API, images, fonts). Une nouvelle base, un troisième script, un dépassement d'octets ou une requête d'un autre type échoue. Le verdict est `PASS_WITH_OWNER_EXCEPTION`, jamais un PASS sans réserve ; les budgets Angular et les autres gates restent inchangés.

Cette [CI 38857089021](https://github.com/Ced59/AmusementParkProject/actions/runs/38857089021) confirme 2 728 tests Angular, 224 contrôles HTTP, 40 routes / 2 080 captures, 38 détails Fleury et **124 contrôles hydratés**, sans exception ni erreur d'interception. Les six paires mesurent 105 → 107 requêtes et **+7 944 octets décodés** : JS +3 302, images +12 431 (même nombre), CSS −3 599, HTML −4 324, XHR +134 (même nombre), fonts identiques. Les populations API sont identiques dans chaque paire, mais différentes du run précédent ; aucun nombre ou nom ne devient une donnée du produit.

Médianes LCP proxy : mobile **180 → 156 ms**, desktop **264 → 156 ms** ; CLS V6 nul dans les six échantillons. `domComplete` reste plus tardif : médianes **229 → 547 ms** en mobile et **263 → 553 ms** en desktop. Ces mesures de laboratoire ne prouvent ni INP ni CWV terrain. Le run s'est arrêté sur l'ancien refus inconditionnel des deux requêtes, avant les six scénarios de thème ; le contrôle borné et la nouvelle recette pratique doivent encore repasser en CI.

## Diagnostic attesté des deux scripts — `47c850246`

La [CI 38833796275](https://github.com/Ced59/AmusementParkProject/actions/runs/38833796275) retrouve cinq fixtures API valides et passe les 224 contrôles HTTP SSR. Son graphe compilé contient 379 modules master / 381 V6. Les ressources initiales du run `0a75fa1e4` chargent bien le dialogue `chunk-O2443OX5.js` (3 497 octets), son import focus `chunk-27DQZD33.js` (7 645), puis son import `chunk-WT6YM62S.js` (2 942), absents de la chaîne du dialogue master `chunk-TJ7DNCNS.js` (2 846). Ces noms et imports sont recoupés avec `browser-module-evidence.json` du nouvel artefact : les deux requêtes supplémentaires proviennent de la chaîne CDK du confinement clavier, déjà installé dans le projet. Le delta net de tous les scripts reste +3 270 octets, et non la somme brute de ces chunks. Aucune nouvelle requête API n'est mesurée.

À cet instant, l'exception était seulement demandée. Son acceptation ultérieure est consignée ci-dessus ; le coût des images réelles et les autres budgets ne sont pas exemptés. Ce run antérieur s'arrête sur un chevauchement de lightbox à 320 × 568, corrigé et validé par `579060704`.

## Incident réseau du runner — `408c71639`

La CI `38832267608` passe **2 728 tests Angular / 581 fichiers** et le backend. Les appels du SSR isolé vers `https://amusement-parks.fun` échouent ensuite avec `UND_ERR_CONNECT_TIMEOUT` (connexion 10 s), avant toute campagne navigateur. Le catalogue Bing renvoie alors 503 avec `missing-canonical`, conformément au garde SEO, et le contrôle exigeant 200 refuse ce rendu. La fixture de référence ne contient aucune réponse API : elle ne doit pas être utilisée comme vraie comparaison V5/V6.

Une unique lecture anonyme de `/api/public-stats/home` depuis le poste répond 200 en 981 ms après le run ; cela ne prouve pas la connectivité du runner et ne requalifie pas son échec. Aucun délai, politique SSR, service ou infrastructure n'est modifié. Le diagnostic compilé est déplacé avant la recette HTTP, afin de rester disponible même quand cette connexion échoue. Une nouvelle CI est nécessaire ; le dépassement de deux scripts du run précédent reste également ouvert.

## Mesure bloquante du 16 octobre — `0a75fa1e4`

La [CI 38829033532](https://github.com/Ced59/AmusementParkProject/actions/runs/38829033532) passe le backend, la sécurité, les **2 726 tests Angular**, 40 routes / 2 080 captures, les 38 détails Fleury et 123 contrôles hydratés. L'isolation entre recette et snapshots est réparée. Les nouveaux en-têtes pratiques passent les contrôles de géométrie. La campagne s'arrête ensuite sur P04.6 : les six comparaisons identiques par population, à 390/1 366 px, mesurent **105 → 107 requêtes** et **+60 364 octets décodés**. Ce dépassement n'est ni ignoré ni accepté par défaut.

Le détail est stable sur les trois répétitions : scripts **84 → 86** et **+3 270 octets** ; images **12 → 12**, **+65 465 octets** pour cette population réelle ; fonts **3 → 3**, CSS **−3 719**, HTML **−4 786**, XHR **+134 octets**. Les quatre noms de hero sont identiques entre les deux versions ; il s'agit ici de Beech Bend, Six Flags Fiesta Texas, Babylon Park London et Arcolandia Pacifico, pas de nouvelles valeurs figées du produit. La différence de coût d'image dépend donc de la population API et ne doit pas être masquée par la campagne précédente plus légère.

Les LCP proxy sont 228–260 → 192–220 ms en mobile et 296–332 → 196–204 ms en desktop. Le CLS est nul sauf un échantillon V6 mobile à 0,00202 ; ce ne sont pas des CWV terrain. L'origine des deux modules supplémentaires reste à attester par le graphe compilé, sans l'attribuer prématurément au focus. Le prochain artefact conserve seulement noms, tailles, imports statiques et marqueurs focus/dialogue, pas le code, les données API ou des credentials. Le seuil et les budgets restent inchangés.

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
- Vidéos et chargements différés existants conservés ; coût initial des quatre nouveaux visuels hero comparé à V5 dans l'échantillon décrit ci-dessous.
- `@defer`, lazy routes, `loading="lazy"`, `srcset`, `sizes`, cache, `TransferState`, hydratation et préchargements SEO restent intacts.
- Aucun serveur local ne sera démarré sans autorisation explicite.

## Contrôle accueil P04

Le panneau des quatre parcs hero réutilise `app-image-display` et les identifiants issus de l’API. Seul le premier média visible reçoit `loading="eager"` et `fetchpriority="high"` ; les trois suivants restent lazy, avec `srcset`, `sizes`, dimensions réservées et placeholder. Le bloc des derniers contenus conserve `@defer (on viewport; hydrate on viewport; prefetch on idle)`. Aucun endpoint ou dépendance UI n'est ajouté. Ces quatre images n'existaient pas dans le template hero V5 : leur impact doit être mesuré, pas déduit du seul attribut lazy. La campagne comparative `a75861a30` démontre la non-augmentation des requêtes initiales pour les deux largeurs et populations anonymes testées ; ce PASS ciblé ne couvre pas toutes les populations possibles ni les performances terrain.

### Mesure comparative inspectée — CI `a75861a30`

Source : [`home-network-comparison.json`, artefact 11992235285](https://github.com/Ced59/AmusementParkProject/actions/runs/38769363185/artifacts/11992235285), contre `master@7f6a9088cb20e1fa6e450b2e8f4bacb9e2019a22`. Un passage par largeur/version, cinq secondes après hydratation, cache navigateur froid et mêmes populations API SSR/client. Les réponses publiques rejouées sont exclusivement des fixtures CI, jamais des données du produit.

| Mesure V5 → V6 | 390 px | 1366 px |
|---|---:|---:|
| Requêtes émises | 105 → 105 | 105 → 105 |
| Corps décodés totaux | 1 700 250 → 1 687 164 o | 1 700 250 → 1 687 164 o |
| Images : requêtes / corps décodés | 12 / 16 320 → 12 / 20 077 o | 12 / 16 320 → 12 / 20 077 o |
| LCP proxy de laboratoire | 188 → 152 ms | 240 → 144 ms |
| CLS observé | 0 → 0 | 0 → 0,0001014 |
| `domComplete` | 228 → 422 ms | 226,6 → 445 ms |

Le bilan est de **0 requête supplémentaire et −13 086 octets décodés**, malgré +3 757 octets d'images ; CSS et scripts compensent cette différence. Les trois images hero non prioritaires sont encore non chargées dans cet état initial, conformément à leur présentation hors du premier emplacement. Polices chargées dans les quatre passes. Le clic natif d'unités fournit des événements ≥16 ms de 24 ms avant / 16 et 16 ms après au mobile ; aucun événement ≥16 ms au desktop. L'absence d'échantillon ne signifie pas zéro latence, et ce geste unique n'est pas une mesure INP.

Le `domComplete` est plus tardif en V6 dans ce passage : ce résultat est conservé, pas masqué par le LCP plus court. Une répétition est nécessaire pour distinguer variance de laboratoire et régression stable. Ces mesures ne simulent ni connexion mobile ralentie, ni compression réseau, ni coût réel de l'API et du CDN, ni navigation longue ; elles ne soldent donc pas P21 dans son ensemble.

Le complément de recette prépare **trois paires par largeur**, avec ordre V5/V6 alterné et origines isolées distinctes à chaque passe. Il conserve chaque mesure brute et publie min/médiane/max ; un seul échantillon comportant une requête supplémentaire reste bloquant même si la médiane est nulle. Des mesures manquantes, dupliquées ou sans LCP observé ne sont pas acceptées. Son exécution CI reste à attester ; les chiffres du tableau précédent ne sont pas présentés comme des médianes répétées.

Six autres navigations isolées préparent le contrôle du thème initial : système clair/sombre × préférence absente/claire/sombre. L'observateur ne modifie pas les classes de thème ; il prépare uniquement la clé locale existante avant le vrai script `data-app-theme-init`, puis observe les couleurs après le premier paint et le résultat après hydratation. Les frames antérieures au premier paint ne sont pas présentées comme un flash visible. Absence de paint/frame, échec de stockage de la fixture ou couleur incorrecte font échouer le contrôle. Le moteur de thème et ses contrats restent inchangés. Résultat CI encore requis.

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

## Résultats locaux P21

| Contrôle | Résultat |
|---|---|
| Build SSR production | PASS — initial 824 269 o, sous le warning Angular 950 kB et l’erreur 1 MB |
| Budget initial outillé | PASS — 824 269 / 1 000 000 o |
| Plus gros chunk lazy | PASS — 214 874 / 300 000 o |
| JavaScript total | PASS — 6 694 120 / 7 200 000 o |
| CSS total | PASS — 210 814 / 250 000 o |
| Architecture facades/ports | PASS — 160 facades |
| Une classe par fichier | PASS — 0 violation C# ou TypeScript |
| Analytics typés | PASS — 1 914 fichiers TypeScript |
| Qualité accessibilité/i18n | PASS — 113/113 tests, 8 langues, 7 316 clés |
| Test Angular ciblé fiche parc | PASS — 1/1 |
| Tests outils SSR avec type stripping | PASS — 15/15 |
| Tests cœur des budgets/performance | PASS — 6/6 |
| Smoke SEO production V5 | PASS — 5/5 routes, HTML SSR Bing sans JS exécutable |
| Baseline HTTP V5 | PASS — 30/30 échantillons HTTP attendus, aucun budget dépassé |
| Suite Angular avec couverture | CI REQUISE — compilation réussie, arrêt préventif à 1,56 Gio libres avant exécution |
| Outils Node 24 avec serveurs éphémères | CI REQUISE — poste local en Node 22 et démarrage de service local non autorisé |
| Smoke SSR du bundle V6 | CI/REVUE REQUISE — aucun serveur applicatif local démarré |

Le rapport brut HTTP est versionné dans `evidence/performance-baseline-v5.json`. Ses p95 sont 46,58 ms (home), 77,63 ms (parcs), 36,83 ms (classements), 37,21 ms (Park Fit), 24,70 ms (health) et 31,53 ms (capabilities). Les pages home, parcs et classements répondent depuis le cache SSR ; Park Fit conserve son mode CSR fallback existant.

Le budget JavaScript total possède 505 880 octets de marge et le budget CSS 39 186 octets. Ces marges sont suffisantes pour la branche mais restent des points d’attention : aucune nouvelle dépendance UI n’a été ajoutée. Le build signale le diagnostic Angular `NG8102` déjà hors périmètre visuel et les dépendances CommonJS existantes Leaflet, Quill, QRCode, dijkstrajs et pngjs ; aucune n’a été introduite par la refonte.

La commande complète `test:ci` a construit les 14,95 MB de bundles de test puis Vitest a commencé. La RAM libre est descendue de 5,45 à 1,56 Gio, encore en croissance ; la suite lancée par Codex a été interrompue proprement pour respecter la marge absolue de 1,5 Gio. La CI Node 24, isolée, reste donc la preuve obligatoire de couverture avant toute fusion.

## SEO/SSR à préserver

Dernière campagne entièrement verte inspectée : [CI `a75861a30`](https://github.com/Ced59/AmusementParkProject/actions/runs/38769363185), 2 712 tests, 40 routes / 2 080 captures, 26 GET / 224 assertions HTTP et 54 assertions hydratées sans exception. Initial **827 368 o**, CSS **213 915 o** ; aucun budget n'a été relevé. Le complément P07 suivant reste en CI.

Le complément de mesure de l'accueil compile séparément le frontend du SHA exact de base de la PR sur le runner Linux, avec le même lockfile et les dépendances déjà installées, puis le frontend V6 est comparé à ce build. Aucun serveur local ou de production n'est démarré par cette mesure. Les cinq réponses API publiques initiales sont enregistrées anonymement, sans cookies/autorisation ; le renouvellement client des parcs aléatoires dispose de sa seconde vraie réponse. Ces deux populations sont rejouées à l'identique aux deux builds, uniquement dans l'outillage CI, jamais dans l'application livrée.

Deux largeurs (390/1366) sont observées cinq secondes après hydratation, cache navigateur désactivé : requêtes émises, corps décodés par type, LCP/CLS de laboratoire et Event Timing d'un clic natif sur les unités. Ce ne sont ni des octets réseau compressés, ni des Core Web Vitals terrain, ni l'INP d'une visite complète. Les mutations et origines tierces restent bloquées. Toute augmentation des requêtes initiales fait échouer P04.6 : elle exige correction ou exception précise du propriétaire après mesure, pas un PASS silencieux. L'exécution et les résultats inspectés sont consignés dans le tableau ci-dessus.

La [CI e98d7f8fb](https://github.com/Ced59/AmusementParkProject/actions/runs/38697638768) réussit build SSR, tests frontend avec couverture, budgets, qualité, sécurité, backend et images Docker. Le moteur SSR réel rend 19 routes publiques et les 38 détails visibles du Fleury. Le navigateur isolé hydrate l'accueil sans exception et exécute consentement, thème et recherche. Ces preuves ne mesurent pas les Core Web Vitals terrain et ne remplacent pas les codes HTTP, le cache et les en-têtes du serveur de recette déployé.

| Mesure CI `e98d7f8fb` | Octets | Budget | Verdict |
|---|---:|---:|---|
| Initial | 824 846 | 1 000 000 | PASS |
| Plus gros lazy chunk | 214 874 | 300 000 | PASS |
| JavaScript total | 6 695 088 | 7 200 000 | PASS |
| CSS total | 211 393 | 250 000 | PASS |

Les 578 fichiers de tests Angular passent. Le CSS possède encore 38 607 octets de marge ; aucun budget n'a été augmenté pour faire passer la refonte.

Codes 200/301/404, canonical, hreflang réels, locale Open Graph, JSON-LD fiable, robots/noindex, sitemap, HTML SSR initial et absence de discordance d’hydratation. Les routes admin, compte, auth, techniques et erreurs ne deviennent pas indexables.

## Recette HTTP du bundle — CI uniquement

### Comparaison répétée `3b560d471` — laboratoire isolé

Le [run 38820443559](https://github.com/Ced59/AmusementParkProject/actions/runs/38820443559) valide le frontend, les 2 718 tests, 90 contrôles hydratés, 40 routes / 2 080 captures, 38 détails Fleury et les contrats de source. Trois paires par largeur alternent V5/V6 avec les mêmes populations API. Les mesures suivantes sont des proxys de laboratoire, pas des Core Web Vitals terrain ou un INP :

| Médiane, trois paires | 390 px V5 → V6 | 1 366 px V5 → V6 |
|---|---:|---:|
| Requêtes initiales | 106 → 105 | 106 → 105 |
| Octets décodés | 1 723 552 → 1 697 871 | 1 723 552 → 1 765 939 |
| LCP proxy | 180 → 164 ms | 288 → 152 ms |
| CLS proxy | 0 → 0 | 0 → 0 |
| `domComplete` | 242,6 → 531,8 ms | 240,4 → 691,9 ms |

Les trois répétitions gardent les mêmes deltas de requêtes/octets. Plages LCP : mobile 168–204 → 136–164 ms ; desktop 240–300 → 148–152 ms. `domComplete` est plus tardif et le coût décodé desktop augmente de 42 387 o : ces résultats ne sont pas masqués derrière le LCP favorable. Le détail isole le surcoût aux images (18 147 → 99 141 o desktop), tandis que CSS (218 015 → 213 854 o), scripts (936 317 → 928 089 o) et fontes (64 260 → 42 532 o, quatre → trois requêtes) diminuent. Le budget initial du build est de 827 307 o et le plus gros lazy de 214 874 o, budgets inchangés.

La revue de source identifie un `sizes` trop large pour le nouveau logo hero : le CSS borne l'image à `min(72%, 18rem)` mais le HTML annonçait `36vw` desktop. Le correctif décrit le vrai plafond `18rem` et les petits écrans, sans changer pixels CSS, image, priorité, `srcWidth` de secours ou candidats retina. Un test protège ce raccordement ; une nouvelle comparaison doit démontrer le résultat, aucune baisse future n'est présumée. Les tailles et coûts de la lightbox CDK nouvellement raccordée doivent aussi être revalidés.

Les six combinaisons système clair/sombre × préférence auto/clair/sombre passent : 29–31 frames post-FCP observées par cas, zéro frame de mauvais thème. Le FCP varie de 136 à 168 ms. C'est une preuve bornée de l'initialisation réelle dans ce navigateur, pas une garantie universelle sur tous appareils.

L'outil `tools/design-v6/verify-public-http.mjs` refuse de fonctionner hors runner Linux CI. Avant le démarrage de Chromium, il démarre le serveur Express compilé sur un port éphémère du runner, puis l'arrête systématiquement. Cette exécution séquentielle fait échouer rapidement une recette HTTP incorrecte, avant de produire les 1 976 captures. Aucun serveur n'est démarré sur le PC du propriétaire ; aucune configuration produit ou infrastructure n'est modifiée. Les appels sont des GET anonymes, séquentiels ; l'API publique fournit les données réelles sans compte, cookie ni écriture.

La campagne vérifie huit accueils, catalogue, parc, ParkItem, standalone, tarifs remplis et vides, 404 connue/inconnue, canonical, hreflang, locale Open Graph, JSON-LD, CSP, version et cache chaud Bing/Google. Quatre routes compte/admin/auth/Park Fit conservent leur shell CSR et `noindex` ; elles ne sont pas déclarées fonctionnellement recettées. La redirection 301 vidéo utilise un identifiant synthétique uniquement pour tester la normalisation d'URL, sans prétendre vérifier un média réel. `http-ssr-report.json` et le journal borné du serveur sont joints à l'artefact CI. Ce contrôle doit encore passer ; il n'atteste ni le déploiement, ni les métriques terrain, ni la bascule transactionnelle.

La campagne `2986bba56` passe tests Angular, fixture responsive, build/budgets et les seize contrôles hydratés, nettoyage compris. Le nouveau test HTTP refuse à tort l'accueil sans JSON-LD : la production V5.4.101 renvoie déjà zéro bloc sur `/fr/home` (200, SSR cache HIT), et `SeoService.applyHomeSeo` n'en déclare pas ; Le Fleury en sert deux. Le contrat de recette exige donc zéro bloc sur les accueils, des blocs JSON valides sur les pages entités/catalogue, sans modifier le SEO de production. Tout futur échec conserve aussi le dernier HTML HTTP anonyme pour diagnostic. La suite HTTP complète reste à attester.

La [campagne `fd54cfcc3`](https://github.com/Ced59/AmusementParkProject/actions/runs/38719508087) valide encore tests/build/budgets, 1 976 captures et 16 contrôles hydratés. Le serveur HTTP de test reçoit toutefois son origine loopback au lieu du host public et tente des appels HTTPS vers son port HTTP ; le catalogue répond alors 503 `missing-canonical`. Les journaux établissent `ERR_SSL_WRONG_VERSION_NUMBER` sur `https://127.0.0.1:<port>/api/...`. La recette transmet désormais explicitement `X-Forwarded-Host`, comme le proxy existant, et configure l'origine interne sans suffixe `/api`. Aucune logique de serveur, SEO, transport Angular ou production n'est changée ; une nouvelle campagne doit attester les réponses attendues.

La [campagne `2482da82c`](https://github.com/Ced59/AmusementParkProject/actions/runs/38721331679) valide les huit accueils, catalogue, parc, ParkItem, standalone et tarifs remplis/vides en HTTP, puis rencontre une autre hypothèse incorrecte du test sur la 404 générique. Le GET anonyme Bing de `/fr/not-found` en production V5.4.101 confirme HTTP 404, `CSR_FALLBACK`, shell `lang=en`, scripts client, aucun H1, aucune description/locale Open Graph ni alternate. Le contrôle distingue désormais exactement les deux routes d'erreur génériques de la 404 métier « tarifs vides » : maintien du statut, de `noindex`, du shell client et absence de données structurées/hreflang, sans inventer un SSR ou changer le produit. Cache chaud, routes privées et redirection restent à attester après cette correction de recette.

La [CI `88ad17e45`](https://github.com/Ced59/AmusementParkProject/actions/runs/38723392833) réussit intégralement : **2 710 tests Angular / 578 fichiers**, 30 cas de fixture responsive, **38 routes / 1 976 captures initiales**, 38 détails Fleury et 16 contrôles hydratés sans exception ni erreur d'interception. La recette Express compilée passe **24 GET séquentiels / 202 assertions**, dont cache chaud Google/Bing, routes CSR privées et 301. Budgets : initial **827 527 o**, plus gros lazy **214 874 o**, JavaScript total **6 694 150 o**, CSS **214 074 o**. Le diagnostic de contraste ciblé ne relève aucun candidat aux deux largeurs échantillonnées ; ce n'est pas une preuve AA exhaustive. Les corrections de lisibilité des logos et les nouveaux scénarios de navigation ajoutés ensuite nécessitent encore leur propre CI.

La [CI `ae874422d`](https://github.com/Ced59/AmusementParkProject/actions/runs/38743956202) atteste ensuite tous les jobs, **25 GET / 213 assertions HTTP**, **39 routes / 2 028 captures**, 38 détails Fleury et 28 contrôles hydratés sans exception. Budgets inchangés : initial **827 368 o**, lazy maximal **214 874 o**, JavaScript total **6 694 173 o**, CSS **213 915 o**. Le complément de recette catalogue/page 2 n'ajoute aucun code au bundle applicatif. Il ne solde pas les mesures réseau initial des médias hero ni les Core Web Vitals comparables, toujours ouvertes.

