# Rapport de livraison — crawl, SEO, cache, statistiques et architecture

Date : 23 juillet 2026  
Périmètre : PR #400 à #429, production `amusement-parks.fun`, GitHub Actions et VPS KVM2.

## Résultat global

Le chantier demandé a été livré par lots focalisés. La version est passée au niveau
mineur 3.5 puis a progressé jusqu’à 3.5.21. Les changements couvrent la politique
robots, les statistiques techniques, le crawl SSR, le cache continu, le déploiement
sans coupure, la chaîne CI/CD, les dépendances, la CSP et les premières extractions
Clean Architecture prioritaires côté frontend et backend.

La production autorise désormais les robots de recherche, d’assistants et d’aperçu
qui ne servent pas à l’entraînement, tout en gardant les robots d’entraînement
explicitement exclus. Ahrefs est autorisé et reçoit du HTML SSR exploitable. Les
agents sont distingués au maximum dans les statistiques au lieu d’être agrégés
indistinctement dans « Other bots ».

## Livraisons par domaine

### Robots, agents et crawl

| PR | Livraison |
|---:|---|
| #400 | Autorisation des agents IA hors entraînement et distinction statistique des familles connues. |
| #401 | Extraction de la politique robots hors du cœur applicatif du serveur SSR. |
| #402 | Reconnaissance du vrai token `GoogleAgent-Mariner`. |
| #405 | Conservation du JavaScript interactif pour Mariner malgré son traitement robot/SSR. |
| #409 | Couverture CSR-only de toutes les routes admin imbriquées. |
| #415 | Bascule de déploiement avec candidats sains avant remplacement des services canoniques. |
| #416–#417 | Warmup complet continu, borné, supervisé par systemd et repris après redémarrage/déploiement. |

Applebot a été conservé : c’est le crawler d’Apple pour ses fonctions de recherche
et services, pas un robot générique d’entraînement. Les familles Apple, Google,
Bing, Ahrefs, OpenAI Search, assistants, Claude, Perplexity et autres agents connus
sont identifiées par leurs vrais User-Agents. Les variantes d’entraînement restent
traitées séparément.

Le crawl froid Ahrefs réalisé pendant l’audit initial a validé 14 familles d’URL en
200 avec HTML SSR, canonical/hreflang/robots cohérents et 404 réel pour une entité
absente. Le sitemap observé contenait 73 560 URL uniques dans 440 fichiers. Ce
volume explique qu’un crawl séquentiel avec délai de deux secondes puisse dépasser
40 heures sans impliquer une boucle d’URL.

### Statistiques techniques

| PR | Livraison |
|---:|---|
| #403 | Vue quotidienne, graphiques par jour et par agent, plage fondée sur les buckets réellement présents, ergonomie mobile. |
| #404 | Recherche de parcs réparée uniquement sur la page « Contenus du parc ». |
| #406 | Tendances 503 corrigées, formatteurs partagés et lecture des buckets rendue scalable. |
| #408 | Rétention restaurable de 100 jours via variable GitHub et runtime. |
| #414 | Tests de cohérence du cache mémoire des buckets retenus. |

Les 15 anciens buckets ont été supprimés et les statistiques ont été remises à zéro
après l’évolution des catégories d’agents, afin d’éviter de comparer deux modèles
de classification incompatibles. La vue s’adapte au nombre réel de buckets
disponibles, jusqu’à la rétention configurée.

### Cache SSR et VPS

- VPS contrôlé via le profil SSH root par adresse IP, sans consigner l’adresse ni
  la clé dans le dépôt.
- RAM, disque, absence de swap, conteneurs et inodes vérifiés ; la marge observée
  était compatible avec le profil de cache retenu.
- Cache mesuré à plusieurs milliers de fichiers et plusieurs Gio, sous son plafond
  configuré.
- Plafond disque, cache mémoire et concurrence de rendu conservés à des niveaux
  adaptés aux ressources mesurées.
- Warmup complet, borné et séquentiel, avec pause entre requêtes, cycles périodiques
  et reprise sur erreur.
- Service systemd activé et actif, artefacts de warmup retenus sept jours.
- Les pages déjà chaudes répondaient typiquement en quelques dizaines de
  millisecondes depuis le PC ; aucun 502 n’a été observé pendant les boucles de
  validation du nouveau déploiement.

Variables GitHub alignées, sans publier leurs valeurs de production :

- `SSR_TECHNICAL_STATS_RETENTION_DAYS`
- `SSR_WARMUP_CONTINUOUS_ENABLED`
- `SSR_WARMUP_CONTINUOUS_INTERVAL_SECONDS`
- `SSR_WARMUP_CONTINUOUS_RETRY_SECONDS`
- `SSR_WARMUP_PROFILE`
- `SSR_WARMUP_MAX_URLS`
- `SSR_WARMUP_CONCURRENCY`
- `SSR_WARMUP_SLEEP_SECONDS`
- `SSR_WARMUP_BOT_VALIDATION_SAMPLE_SIZE`
- `CSP_REPORT_ONLY`

### Sécurité et chaîne CI/CD

| PR | Livraison |
|---:|---|
| #411 | Mise à jour Angular/SSR et dépendances frontend critiques. |
| #412 | Actions GitHub mises à niveau vers les versions compatibles Node 24. |
| #413 | Dépendances backend vulnérables mises à jour. |
| #419 | Gate bloquant sur vulnérabilités critiques/hautes, rapports toujours publiés et images conditionnées au contrôle. |
| #423 | CSP passée de Report-Only à enforcement après contrôle des rapports et du navigateur. |
| #425 | Distinction fiable entre vulnérabilité npm et indisponibilité du registre/scanner. |

Les scans finaux du chantier ont donné zéro vulnérabilité .NET connue et zéro
vulnérabilité npm haute/critique ; trois avis npm modérés transitifs restent
rapportés. La CSP appliquée conserve temporairement `unsafe-inline`, mais bloque
dès maintenant les violations structurelles. Les lecteurs officiellement supportés
— YouTube, YouTube No-Cookie, Dailymotion et Vimeo — sont explicitement autorisés
dans `frame-src`. Google, Matomo et Clarity restent bornés aux origines nécessaires.

### SEO et routes

| PR | Livraison |
|---:|---|
| #418 | Extraction de `SeoRoutePolicyService`, avec correction du cas des propriétés héritées. |
| #424 | Racines localisées `/fr`, `/en`, etc. redirigées vers leur accueil avant le layout compte. |

La correction #424 supprime l’incohérence où `/fr` exposait les métadonnées de
l’accueil tout en rendant « Ton passeport parcs ». Les huit racines localisées
produisent désormais le canonical `/{lang}/home` et le contenu public attendu.

La qualification du volume sitemap conduit à conserver les familles actuelles à ce
stade : les échantillons parc, élément, image, historique et référence sont des
pages publiques distinctes, localisées et canonalisées. Une réduction aveugle
supprimerait des pages utiles. La décision future doit être fondée sur les
impressions/clics Search Console et les crawls Ahrefs postérieurs à cette livraison,
famille par famille. Les images et vues dérivées restent la première famille à
réévaluer si elles restent sans impression sur une fenêtre représentative.

### Clean Architecture frontend

| PR | Livraison |
|---:|---|
| #420 | État de sélection de l’arbre de navigation extrait de la façade publique. |
| #421 | Pages techniques publiques migrées vers des ports d’injection dédiés. |
| #426 | Primitives d’onglets extraites du monolithe `primitives.ts`, réexports compatibles conservés. |
| #427–#428 | Opérations distantes de l’upsert de graphe admin placées derrière une façade puis des ports feature-scoped. |

Le contrôle `architecture:facade-ports` valide 73 façades après #428. Le commentaire
P1 reçu à la fin de #427 a été pris en compte immédiatement : #428 retire toute
injection concrète de la nouvelle façade.

### Clean Architecture backend

| PR | Livraison |
|---:|---|
| #422 | Lecture/parsing des entrées d’invalidation SSR extraite du resolver de plus de 1 200 lignes. |
| #429 | Politique de tri Mongo des contenus extraite de `ParkItemRepository`, avec tests sur le BSON rendu. |

Ces extractions conservent les contrats HTTP et Mongo. Elles réduisent les
responsabilités des deux points chauds sans migration de données ni changement de
DTO.

## Commentaires de revue traités

- Token réel `GoogleAgent-Mariner` et conservation de son interactivité.
- Cache des buckets techniques et coût des formatteurs de dates.
- Libellé 503 d’un agent sélectionné.
- Validation stricte des durées du warmup et chargement de `.env`.
- Supervision/reprise du warmup et verrou hérité.
- Clés de routes héritées dans la politique SEO.
- Couverture des composants après migration vers des ports.
- Lecteurs vidéo manquants dans `frame-src` avant enforcement CSP.
- Ports feature-scoped manquants dans la façade admin #427, corrigés par #428.

Chaque PR a été contrôlée avec le script thread-aware de revue avant fusion. Le cas
#427 est documenté explicitement : le commentaire a été publié dans la fenêtre
entre le contrôle final et la fusion ; le suivi correctif a été créé et livré
immédiatement.

## Tests et validations

- Tests Karma ciblés pour routes, CSP, vidéos, statistiques, ports, façade admin et
  primitives UI.
- Contrôle frontend `architecture:facade-ports`.
- Tests xUnit ciblés pour cache de buckets, invalidation SSR et tri Mongo.
- Builds frontend SSR et backend Release en CI.
- Audits npm et NuGet à chaque pipeline.
- Construction des images Docker avant chaque fusion.
- Vérifications HTTP depuis le PC et depuis le VPS : health, API, robots, sitemap,
  pages publiques, canonical, version et en-têtes CSP.
- Contrôles navigateur réels : page d’accueil, console sans violation CSP et
  version déployée.
- Contrôle Docker/systemd, mémoire, disque et cache depuis le VPS.

## État final et dette explicitement conservée

Le périmètre prioritaire du rapport initial est livré. Les éléments suivants ne sont
pas des régressions ni des blocages de crawl, mais restent des améliorations
incrémentales possibles :

- supprimer `unsafe-inline` via nonces/hashes dans une PR dédiée ;
- poursuivre la décomposition du service SEO par générateurs JSON-LD et familles
  de pages ;
- extraire d’autres groupes de primitives et d’autres politiques de filtres Mongo ;
- poursuivre la réduction des deux autres gros écrans admin ;
- réévaluer les familles de sitemap avec des données Search Console/Ahrefs
  postérieures au nouveau crawl, sans noindex massif spéculatif.

Ces suites doivent rester des PR focalisées et testées, conformément à
`AGENTS.md`.
