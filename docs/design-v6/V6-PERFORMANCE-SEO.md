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
- Vidéos et chargements différés existants conservés ; coût initial des quatre nouveaux visuels hero encore à comparer à V5.
- `@defer`, lazy routes, `loading="lazy"`, `srcset`, `sizes`, cache, `TransferState`, hydratation et préchargements SEO restent intacts.
- Aucun serveur local ne sera démarré sans autorisation explicite.

## Contrôle accueil P04

Le panneau des quatre parcs hero réutilise `app-image-display` et les identifiants issus de l’API. Seul le premier média visible reçoit `loading="eager"` et `fetchpriority="high"` ; les trois suivants restent lazy, avec `srcset`, `sizes`, dimensions réservées et placeholder. Le bloc des derniers contenus conserve `@defer (on viewport; hydrate on viewport; prefetch on idle)`. Aucun endpoint ou dépendance UI n'est ajouté. En revanche, ces quatre images n'existaient pas dans le template hero V5 : les téléchargements d'assets peuvent augmenter, même avec `loading="lazy"` pour les trois dernières. L'exigence P04.6 de non-augmentation des requêtes initiales **n'est pas démontrée** et reste bloquante ; elle doit être mesurée à données/viewport identiques puis corrigée ou faire l'objet d'une décision explicite, sans inventer de résultat.

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

Dernière campagne entièrement verte : [CI `6195d1a68`](https://github.com/Ced59/AmusementParkProject/actions/runs/38766079549), 2 712 tests, 40 routes / 2 080 captures, 26 GET / 224 assertions HTTP et 54 assertions hydratées sans exception. Initial **827 368 o**, CSS **213 915 o** ; aucun budget n'a été relevé.

Le complément de mesure de l'accueil compile séparément le frontend du SHA exact de base de la PR sur le runner Linux, avec le même lockfile et les dépendances déjà installées, puis le frontend V6 est comparé à ce build. Aucun serveur local ou de production n'est démarré par cette mesure. Les cinq réponses API publiques initiales sont enregistrées anonymement, sans cookies/autorisation ; le renouvellement client des parcs aléatoires dispose de sa seconde vraie réponse. Ces deux populations sont rejouées à l'identique aux deux builds, uniquement dans l'outillage CI, jamais dans l'application livrée.

Deux largeurs (390/1366) sont observées cinq secondes après hydratation, cache navigateur désactivé : requêtes émises, corps décodés par type, LCP/CLS de laboratoire et Event Timing d'un clic natif sur les unités. Ce ne sont ni des octets réseau compressés, ni des Core Web Vitals terrain, ni l'INP d'une visite complète. Les mutations et origines tierces restent bloquées. Toute augmentation des requêtes initiales fait échouer P04.6 : elle exige correction ou exception précise du propriétaire après mesure, pas un PASS silencieux. L'exécution de ce complément reste à attester.

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

L'outil `tools/design-v6/verify-public-http.mjs` refuse de fonctionner hors runner Linux CI. Avant le démarrage de Chromium, il démarre le serveur Express compilé sur un port éphémère du runner, puis l'arrête systématiquement. Cette exécution séquentielle fait échouer rapidement une recette HTTP incorrecte, avant de produire les 1 976 captures. Aucun serveur n'est démarré sur le PC du propriétaire ; aucune configuration produit ou infrastructure n'est modifiée. Les appels sont des GET anonymes, séquentiels ; l'API publique fournit les données réelles sans compte, cookie ni écriture.

La campagne vérifie huit accueils, catalogue, parc, ParkItem, standalone, tarifs remplis et vides, 404 connue/inconnue, canonical, hreflang, locale Open Graph, JSON-LD, CSP, version et cache chaud Bing/Google. Quatre routes compte/admin/auth/Park Fit conservent leur shell CSR et `noindex` ; elles ne sont pas déclarées fonctionnellement recettées. La redirection 301 vidéo utilise un identifiant synthétique uniquement pour tester la normalisation d'URL, sans prétendre vérifier un média réel. `http-ssr-report.json` et le journal borné du serveur sont joints à l'artefact CI. Ce contrôle doit encore passer ; il n'atteste ni le déploiement, ni les métriques terrain, ni la bascule transactionnelle.

La campagne `2986bba56` passe tests Angular, fixture responsive, build/budgets et les seize contrôles hydratés, nettoyage compris. Le nouveau test HTTP refuse à tort l'accueil sans JSON-LD : la production V5.4.101 renvoie déjà zéro bloc sur `/fr/home` (200, SSR cache HIT), et `SeoService.applyHomeSeo` n'en déclare pas ; Le Fleury en sert deux. Le contrat de recette exige donc zéro bloc sur les accueils, des blocs JSON valides sur les pages entités/catalogue, sans modifier le SEO de production. Tout futur échec conserve aussi le dernier HTML HTTP anonyme pour diagnostic. La suite HTTP complète reste à attester.

La [campagne `fd54cfcc3`](https://github.com/Ced59/AmusementParkProject/actions/runs/38719508087) valide encore tests/build/budgets, 1 976 captures et 16 contrôles hydratés. Le serveur HTTP de test reçoit toutefois son origine loopback au lieu du host public et tente des appels HTTPS vers son port HTTP ; le catalogue répond alors 503 `missing-canonical`. Les journaux établissent `ERR_SSL_WRONG_VERSION_NUMBER` sur `https://127.0.0.1:<port>/api/...`. La recette transmet désormais explicitement `X-Forwarded-Host`, comme le proxy existant, et configure l'origine interne sans suffixe `/api`. Aucune logique de serveur, SEO, transport Angular ou production n'est changée ; une nouvelle campagne doit attester les réponses attendues.

La [campagne `2482da82c`](https://github.com/Ced59/AmusementParkProject/actions/runs/38721331679) valide les huit accueils, catalogue, parc, ParkItem, standalone et tarifs remplis/vides en HTTP, puis rencontre une autre hypothèse incorrecte du test sur la 404 générique. Le GET anonyme Bing de `/fr/not-found` en production V5.4.101 confirme HTTP 404, `CSR_FALLBACK`, shell `lang=en`, scripts client, aucun H1, aucune description/locale Open Graph ni alternate. Le contrôle distingue désormais exactement les deux routes d'erreur génériques de la 404 métier « tarifs vides » : maintien du statut, de `noindex`, du shell client et absence de données structurées/hreflang, sans inventer un SSR ou changer le produit. Cache chaud, routes privées et redirection restent à attester après cette correction de recette.

La [CI `88ad17e45`](https://github.com/Ced59/AmusementParkProject/actions/runs/38723392833) réussit intégralement : **2 710 tests Angular / 578 fichiers**, 30 cas de fixture responsive, **38 routes / 1 976 captures initiales**, 38 détails Fleury et 16 contrôles hydratés sans exception ni erreur d'interception. La recette Express compilée passe **24 GET séquentiels / 202 assertions**, dont cache chaud Google/Bing, routes CSR privées et 301. Budgets : initial **827 527 o**, plus gros lazy **214 874 o**, JavaScript total **6 694 150 o**, CSS **214 074 o**. Le diagnostic de contraste ciblé ne relève aucun candidat aux deux largeurs échantillonnées ; ce n'est pas une preuve AA exhaustive. Les corrections de lisibilité des logos et les nouveaux scénarios de navigation ajoutés ensuite nécessitent encore leur propre CI.

La [CI `ae874422d`](https://github.com/Ced59/AmusementParkProject/actions/runs/38743956202) atteste ensuite tous les jobs, **25 GET / 213 assertions HTTP**, **39 routes / 2 028 captures**, 38 détails Fleury et 28 contrôles hydratés sans exception. Budgets inchangés : initial **827 368 o**, lazy maximal **214 874 o**, JavaScript total **6 694 173 o**, CSS **213 915 o**. Le complément de recette catalogue/page 2 n'ajoute aucun code au bundle applicatif. Il ne solde pas les mesures réseau initial des médias hero ni les Core Web Vitals comparables, toujours ouvertes.

