# Audit SEO, crawl, cache, CI/CD et Clean Architecture

Date : 23 juillet 2026  
Périmètre : production `amusement-parks.fun`, frontend Angular/SSR, backend .NET, cache SSR, statistiques techniques, GitHub Actions et commentaires de revue récents.

## Synthèse exécutive

Le site n'est plus bloqué pour Ahrefs. Un crawl froid, effectué avec l'User-Agent Ahrefs et un délai de 2,1 secondes entre les requêtes, a retourné un HTML SSR exploitable et un statut 200 pour chacune des 14 familles d'URL testées. Les routes publiques échantillonnées exposent des titres, canonical, hreflang et robots cohérents. Une entité publique inexistante retourne bien 404.

Le volume est néanmoins très important : le sitemap contient 73 560 URL uniques réparties dans 440 fichiers. À deux secondes par URL, un passage séquentiel complet demande au minimum environ 40 h 52 min. Le rapport Ahrefs montrant environ 33 000 URL en erreur est donc compatible avec un ancien blocage, une fenêtre de crawl incomplète, une saturation SSR ou une indisponibilité de déploiement ; il ne prouve pas une boucle d'URL, car aucun doublon n'a été détecté dans les sitemaps actuels.

Les priorités sont : supprimer les coupures au redéploiement, mesurer le cache réel sur le VPS, traiter les dépendances vulnérables liées au SSR, puis réduire progressivement les principaux points de complexité front et infrastructure. Les frontières principales de la Clean Architecture backend sont correctes ; le frontend est lazy-loadé, mais l'application reste hétérogène dans l'usage des ports/facades et comporte plusieurs classes surdimensionnées.

## Méthode et limites

- Inspection du code, des dépendances de projets, des routes Angular, des politiques robots/SSR, du cache et du workflow de production.
- Lecture thread-aware des commentaires de revue des PR récentes, puis correction des remarques pertinentes dans les PR #405 et #406.
- Validation en production des fichiers robots/sitemaps, des en-têtes, des statuts, des métadonnées et d'un échantillon stratifié des familles d'URL.
- Inspection des variables GitHub Actions et des alertes de dépendances, sans exposer de secret.
- L'accès SSH `root@amusement-parks.fun` n'est pas utilisable depuis cet environnement : aucune configuration de profil SSH n'y est présente et l'authentification par clé est refusée. L'occupation réelle du disque, la mémoire, le CPU, les tailles p50/p95 des pages en cache et les statistiques Docker n'ont donc pas pu être mesurés. Les recommandations de capacité restent conditionnelles à ces mesures.

## Crawl, robots et indexabilité

### État vérifié

- `robots.txt` retourne 200 et autorise explicitement `AhrefsBot` et `AhrefsSiteAudit`, avec `Crawl-delay: 2` et les exclusions des routes API, admin, compte et authentification.
- La règle générique autorise les autres robots sur les pages publiques tout en conservant les mêmes exclusions sensibles.
- Le sitemap déclaré est `https://amusement-parks.fun/sitemap.xml`.
- Les robots de recherche, d'assistants et d'aperçu sont distingués dans les statistiques. Les robots d'entraînement restent exclus selon la politique décidée.
- `GoogleAgent-Mariner` est comptabilisé comme robot, bénéficie du SSR froid et conserve désormais le JavaScript interactif. Le commentaire de revue P1 de la PR #402 était pertinent et a été corrigé par la PR #405.
- Applebot est le robot d'Apple utilisé notamment pour la recherche et les services Apple ; il ne doit pas être confondu avec un robot d'entraînement. Les familles Apple concernées sont distinguées par User-Agent dans la politique actuelle.

### Sitemaps

Le sitemap index référence 440 fichiers contenant 73 560 URL, toutes uniques dans le relevé effectué :

| Famille | URL |
|---|---:|
| Éléments de parc | 40 624 |
| Images d'éléments | 10 976 |
| Historique | 10 032 |
| Références | 3 632 |
| Zones | 3 032 |
| Parcs | 2 336 |
| Listes d'éléments | 1 072 |
| Images de parc | 792 |
| Articles historiques | 632 |
| Horaires | 216 |
| Vidéos | 120 |
| Pages statiques | 72 |
| Pages techniques | 16 |
| Attractions autonomes | 8 |

### Test de crawl froid

Une URL froide de chaque famille a été demandée en premier par Ahrefs. Les 14 réponses étaient en 200, marquées `MISS` puis `SSR_RENDERED`, avec un HTML SEO exploitable. Les temps observés vont d'environ 0,8 s à 5 s ; la page d'élément de parc est la plus lente de l'échantillon, proche de 5 s.

Le risque résiduel est la concurrence : la production autorise un seul rendu SSR à la fois et une file de 16 entrées. Un robot lançant des rafales peut donc provoquer des 503 lorsque le cache est froid. Le délai Ahrefs aide, mais ne contrôle pas tous les robots et Google n'applique pas la directive non standard `Crawl-delay`.

### Métadonnées et statuts

- Les pages de parc et d'élément testées exposent le bon `lang`, un canonical stable, neuf alternates hreflang (langues servies et `x-default`), un titre localisé et `index,follow`.
- Une entité publique inexistante retourne 404 avec un document SSR, ce qui évite le faux 200 indexable.
- Les routes d'administration sont CSR-only et protégées par les guards d'authentification et d'administration. Elles sont également exclues du crawl.
- Le domaine `www` redirige en 301 vers le domaine canonique sans `www`.

### Actions SEO recommandées

1. Comparer dans Ahrefs le nouveau crawl à une date postérieure aux PR robots/cache ; l'ancien rapport ne reflète plus la politique actuelle.
2. Suivre séparément 2xx, 404, 429, 502 et 503 par famille de robot et par jour sur toute la rétention disponible.
3. Examiner la valeur d'indexation des 73 560 URL, surtout les images et vues dérivées. Une URL techniquement crawlable n'est pas nécessairement une page utile à indexer.
4. Conserver un test automatisé de crawl froid pour chaque famille critique et chaque User-Agent autorisé.

## Cache SSR et capacité VPS

### Configuration déclarée observée

- Cache mémoire : 2 000 entrées.
- Cache disque : plafond de 16 Gio.
- Durée de cache : 30 jours ; stale : 1 jour.
- Rendu SSR : concurrence 1, file 16.
- Warmup de déploiement : profil SEO important, 2 500 URL maximum, concurrence 1.
- Statistiques : agrégation disque par lots ; la rétention de 100 jours est enregistrée au runtime, mais aucune variable GitHub `SSR_TECHNICAL_STATS_RETENTION_DAYS=100` ne garantit actuellement cette valeur en cas de perte du volume ou des réglages persistés.

Le warmup couvre au plus 3,4 % du sitemap et le cache mémoire 2,7 %. Le disque est donc la couche décisive. Seize Gio suffisent pour les 73 560 pages uniquement si leur taille moyenne sur disque reste inférieure à environ 233 Kio, marge opérationnelle exclue. Cette hypothèse doit être vérifiée sur le VPS.

### Plan de mesure avant réglage

1. Rétablir un profil SSH utilisable, puis relever RAM, swap, CPU, espace et inodes, tailles du volume SSR, nombre de fichiers, taille p50/p95/p99 et consommation Docker.
2. Mesurer par jour et par famille : hit mémoire, hit disque, stale, miss, rendu, file pleine, 503, temps p50/p95/p99.
3. Fixer le plafond disque en gardant une marge système et Docker explicite ; ne pas consommer tout le disque pour le SSR.
4. N'augmenter la concurrence de rendu à 2 qu'après un test de charge démontrant une marge CPU/RAM suffisante. Sur un petit VPS, 1 peut rester le meilleur réglage.
5. Remplacer le warmup limité de déploiement par un réchauffage continu et borné : pages nouvelles/modifiées, pages les plus demandées, puis cache expirant, avec reprise et débit adaptatif.
6. Déclarer `SSR_TECHNICAL_STATS_RETENTION_DAYS=100` dans les variables GitHub si 100 jours doit être le défaut restaurable. La vue doit continuer à utiliser le nombre réel de buckets disponibles.

## Disponibilité du déploiement

Une courte série de 502 a été observée pendant le redéploiement, puis les mêmes URL sont revenues en 200. Le script utilise `docker compose up -d --remove-orphans` avec des noms de conteneurs fixes et ne bascule pas entre deux stacks front saines. Le healthcheck est exécuté après la mise à jour, ce qui détecte un échec mais n'évite pas la fenêtre où le proxy vise le conteneur remplacé.

Priorité haute : mettre en place un déploiement sans coupure ou à coupure fortement réduite (blue/green ou double instance avec bascule du proxy après readiness), puis tester automatiquement une boucle de requêtes publiques pendant le déploiement. Le warmup doit rester hors chemin critique, comme aujourd'hui.

## GitHub Actions et sécurité de la chaîne

### Constats

- Plusieurs actions JavaScript utilisent des versions annoncées comme dépréciées pour Node 20 et sont forcées vers Node 24 par GitHub. Elles doivent être mises à niveau vers les versions officiellement compatibles.
- Le job de sécurité des dépendances produit un rapport mais ne bloque pas la CI.
- Le dépôt signalait 65 alertes sur la branche par défaut : 24 hautes, 35 modérées et 6 faibles.
- `npm audit` relevait 38 vulnérabilités : 1 critique, 19 hautes, 15 modérées et 3 faibles. Les avis incluent Angular/SSR et `tar`; leur exploitabilité exacte doit être qualifiée, mais les sujets SSR, XSS, TransferCache et cache poisoning sont prioritaires.
- Côté .NET, les paquets signalés incluent notamment `Microsoft.OpenApi` et `Snappier` en sévérité haute, puis `MailKit`, `MimeKit` et `SharpCompress` en modérée. Core et Application ne portent pas directement ces dépendances vulnérables.
- La CSP est actuellement en mode Report-Only et contient encore des tolérances `unsafe-inline`. Il ne faut pas l'activer en bloc sans exploiter les rapports et corriger les violations légitimes.

### Actions recommandées

1. PR dédiée aux versions des actions GitHub.
2. PR frontend de mise à jour Angular et dépendances, avec tests SSR/cache et vérification des bundles.
3. PR backend de mise à jour des dépendances vulnérables, avec tests d'intégration ciblés.
4. Après réduction du stock, rendre la CI bloquante sur toute nouvelle vulnérabilité critique/haute.
5. Exploiter les rapports CSP, retirer progressivement les sources permissives, puis passer en enforcement dans une PR distincte.

## Audit Clean Architecture backend

### Points solides

- Dépendances de projets conformes : Core n'a aucune dépendance projet ; Application dépend de Core ; Infrastructure dépend de Core et Application ; WebAPI dépend d'Application et Infrastructure pour la composition.
- Aucun import d'Infrastructure, MongoDB ou Entity Framework n'a été détecté dans la logique Core/Application, hors règle d'architecture qui manipule ces noms comme critères de test.
- Les contrôleurs ne référencent pas directement l'infrastructure ; les usages WebAPI d'Infrastructure sont concentrés dans la composition, l'initialisation et les diagnostics.
- La stratégie d'autorisation emploie une politique authentifiée par défaut ; les accès publics sont explicites et les contrôleurs admin sont protégés.

### Dette prioritaire

- `SsrPageCacheInvalidationRequestResolver` dépasse 1 200 lignes et cumule parsing, résolution et politique d'invalidation.
- Plusieurs repositories Mongo dépassent 700 à 980 lignes et mélangent requêtes, projections, assemblage et maintenance de cache.
- `SitemapSectionProviders`, `HttpTechnicalStatsProvider`, `AutomaticHistoryEventFactory` et certains mappers HTTP dépassent 600 lignes.
- L'intégration Captain Coaster est répartie en fichiers partiels, mais conserve une responsabilité globale large.

Plan : ajouter d'abord des tests de caractérisation, extraire des collaborateurs internes focalisés (requêtes, projections, mapping, politiques), puis ajouter des tests d'architecture automatisés. Ne pas changer simultanément les contrats HTTP ou Mongo.

## Audit Clean Architecture frontend

### Points solides

- Les routes admin, compte et publiques sont chargées paresseusement ; l'admin n'est pas volontairement placé dans le bundle public initial.
- Les routes admin sont CSR-only côté SSR.
- Le contrôle automatisé facade/ports passe sur les facades couvertes.
- Les fonctionnalités récentes emploient majoritairement ports, tokens d'injection, facades et composants partagés.

### Dette prioritaire

- `seo.service.ts` dépasse 3 500 lignes : métadonnées, canonical, hreflang, JSON-LD et règles par type de page forment un service central trop large et très risqué.
- `public-park-navigation-tree.facade.ts` dépasse 1 400 lignes.
- Les écrans admin de graph upsert, batch photo et bulk graph dépassent 1 000 lignes ; plusieurs autres composants/facades font 500 à 850 lignes.
- `shared/models/primitives.ts` dépasse 1 100 lignes, ce qui augmente le couplage et complique l'évolution ciblée.
- Des composants injectent encore directement des services API concrets (pages techniques publiques, attraction autonome, profil/auth, météo/horaires, et plusieurs écrans admin). L'architecture frontend est donc cohérente par endroits, mais pas uniformément appliquée.

Plan :

1. Décomposer `seo.service.ts` derrière son API publique actuelle, par responsabilités et types de pages, avec tests de non-régression SSR/SEO.
2. Scinder la facade de navigation en chargement, construction d'arbre et état de sélection.
3. Extraire les orchestrations des gros composants admin vers des facades/ports par écran, sans mutualisation prématurée.
4. Migrer les injections API directes verticalement, fonctionnalité par fonctionnalité, en commençant par le public SSR.
5. Scinder `primitives.ts` par domaine en conservant temporairement des réexports compatibles, puis supprimer les réexports devenus inutiles.
6. Étendre le contrôle d'architecture pour interdire progressivement les nouvelles injections concrètes dans les composants et facades.

## Commentaires de PR récents

Les commentaires pertinents ont été pris en compte :

- PR #402 : `GoogleAgent-Mariner` ne devait pas perdre son JavaScript interactif. Corrigé et livré par #405.
- PR #403 : la lecture de tous les buckets à chaque requête admin était synchrone et non scalable jusqu'à 365 fichiers. Corrigé par un cache mémoire maintenu/invalide dans #406.
- PR #403 : le formatteur de date était recréé jusqu'à 1 095 fois. Corrigé par des formatteurs partagés dans #406.
- PR #403 : le libellé 503 d'un agent sélectionné était présenté à tort comme uniquement cache-only. Corrigé dans #406.
- Le commentaire plus ancien sur le token Mariner sans tiret avait déjà été corrigé par #402.

Les commentaires plus anciens portant sur d'autres fonctionnalités ou des choix de libellés n'ont pas été mélangés à ces corrections ciblées.

## Plan d'action proposé

| Priorité | Lot autonome | Résultat attendu |
|---|---|---|
| P0 | Déploiement sans coupure | Aucun 502 pendant le remplacement du front/API |
| P0 | Dépendances SSR/frontend critiques | Réduction immédiate du risque Angular, tar, XSS/cache poisoning |
| P1 | Mesures VPS et cache | Dimensionnement fondé sur RAM/disque/tailles/hit-rate réels |
| P1 | Warmup continu piloté | Davantage de pages chaudes sans pic au déploiement |
| P1 | Alignement rétention GitHub à 100 jours | Valeur restaurable et cohérente avec le runtime |
| P1 | Dépendances backend hautes/modérées | Suppression des alertes exploitables sans changement fonctionnel |
| P1 | Décomposition du service SEO | Risque SEO central réduit, tests plus ciblés |
| P2 | Facade navigation et composants admin | Responsabilités plus petites et testables |
| P2 | Migration progressive vers les ports | Architecture frontend homogène sans big bang |
| P2 | Repositories et invalidation SSR | Infrastructure plus maintenable, contrats inchangés |
| P2 | CSP progressive | Passage maîtrisé de Report-Only à enforcement |
| P3 | Qualification de la valeur des 73 560 URL | Crawl budget concentré sur les pages utiles |

Chaque ligne doit faire l'objet d'une PR focalisée avec ses propres mesures et tests. Aucun de ces refactorings n'est lancé par cet audit.

## Mise à jour après livraison

Les actions prioritaires ont été exécutées par les PR #408 à #429. Le détail des
changements, mesures VPS, variables GitHub, commentaires de revue, tests et dettes
résiduelles est consigné dans
`docs/architecture/audit-delivery-report-2026-07-23.md`.

## Correctifs déjà livrés pendant la vérification

- #404 : recherche du filtre parc rétablie uniquement sur la page admin « Contenus du parc ».
- #405 : navigation interactive de GoogleAgent-Mariner conservée tout en gardant SSR et statistiques robot.
- #406 : affichage 503 corrigé et lecture des statistiques techniques rendue scalable.

Les trois PR sont fusionnées, leurs pipelines de production sont terminés avec succès.
