# V6 — gate de release 6.0.0

## Décision actuelle

**NO-GO MERGE — PR #917 ouverte, recette complète non terminée.** Les PASS ciblés ci-dessous ne constituent pas une acceptation de tous les critères de chaque gate. Aucun merge, tag ni déploiement n’est autorisé avant les checks du dernier commit, la recette visuelle/fonctionnelle Angular réelle et le feu vert explicite du propriétaire.

| Contrôle | État |
|---|---|
| Branche unique issue du dernier `origin/master` | PASS |
| SHA et version de départ consignés | PASS |
| Inventaire route/template/SCSS/interaction généré | SOURCE RÉCONCILIÉE — 148 routes / 293 composants / 268 SCSS + 2 styles inline, CSV master et V6 versionnés ; source distincte des scénarios exécutés |
| Baseline visuelle V5 initiale | PASS |
| P00 inventaires | Base exacte et V6 réconciliées : 2 057 → 2 056 cibles, delta présentationnel expliqué ; états et baseline comportementale complète toujours non démontrés |
| P01 | PASS CIBLÉ — tokens et thèmes contrôlés ; contrastes de tous les états encore à recetter |
| P02 | PASS CIBLÉ — suite a11y complète à P21 |
| P03 | PASS CIBLÉ — 17 tests statiques, suite Angular complète à P21 |
| P04 | PASS CIBLÉ — 24 tests statiques, suite Angular complète à P21 |
| P05 | PASS CIBLÉ — 28 tests statiques, navigation Angular à P21 |
| P06 | PASS CIBLÉ — 32 tests statiques, données multi-parcs à P20/P21 |
| P07 | PASS CIBLÉ — inventaire, filtres, carte et pagination protégés |
| P08 | PASS CIBLÉ — fiche complète et actions protégées, 35 tests statiques cumulés |
| P09 | PASS CIBLÉ — autonomie, données pratiques et visibilité protégées |
| P10 | PASS CIBLÉ — zones, carte et liens filtrés protégés, 38 tests statiques cumulés |
| P11 | PASS CIBLÉ — Leaflet et plans officiels protégés, 42 tests cumulés |
| P12 | PASS CIBLÉ — médias et lecteurs protégés, 46 tests cumulés |
| P13 | PASS CIBLÉ — histoire et routes profondes protégées, 51 tests cumulés |
| P14 | PASS CIBLÉ — données pratiques et live protégés, 56 tests cumulés |
| P15 | PASS CIBLÉ — classements, commentaires, collections, watch et partage protégés, 61 tests cumulés |
| P16 | PASS CIBLÉ — toutes les familles publiques secondaires protégées, 67 tests cumulés |
| P17 | PASS CIBLÉ — auth, profil, passeport et trips protégés, 74 tests cumulés |
| P18 | PASS CIBLÉ — 58 terminaux et workflows admin protégés, 81 tests cumulés |
| P19 | PASS CIBLÉ — états et composants transversaux protégés, 88 tests cumulés |
| P20 | PASS CIBLÉ — 30 cas Chromium, 13 largeurs, deux thèmes, faible hauteur, 113 tests qualité/a11y |
| Revue visuelle Angular réelle P22 | PARTIELLE — captures du bundle CI réel inspectées ; revue exhaustive et validation propriétaire toujours requises |
| P21 local | PASS CIBLÉ — build SSR, budgets, architecture, a11y/i18n, SEO et tests ciblés verts |
| P21 CI Node 24 | Dernier PASS complet `754cce98c` : 2 728 tests Angular / 581 fichiers, 200 tests Node qualité, 224 assertions HTTP, 40 routes / 2 080 captures et 142 contrôles hydratés ; `f5ddfd8a4` passe ensuite zoom/catégories mais le sandbox ne sert pas encore les navigations document publiques |
| Exception P04.6 | PASS_WITH_OWNER_EXCEPTION sur six paires `754cce98c` : deux scripts CDK, +2 854 octets JS, aucun autre nombre de requêtes modifié ; plafond +3 302 inchangé |
| Recette pratique additionnelle | PASS CIBLÉ `754cce98c` — jour/mois réel, historique météo différé/cache, retour exact et 52 captures ; chevauchement corrigé mais textes comprimés à 320 px, nouveau tableau localement défilable à attester |
| Version `6.0.0` et historique 8 langues | PASS — commit de contenu `545345dc3a81c099a328e29a510447339b3f9342` |
| Diff de portée | Exceptions explicitement autorisées le 15 octobre : référence PKCS 10.0.13 et nettoyage Leaflet à la destruction |
| PR unique | [#917](https://github.com/Ced59/AmusementParkProject/pull/917) ouverte ; recette complète encore requise |
| CI intégralement verte | NON sur `f5ddfd8a4` : URL Aeroclub atteinte mais document traité comme asset par l'outil ; renderer SSR V6 borné et marqueurs plats à revalider |
| Revue visuelle propriétaire | NON DEMANDÉE |
| En-têtes ParkItem et standalone | PASS GÉOMÉTRIE CIBLÉ `a6bbfb21d`, captures Rhaegal et Hasenhorn 390 px inspectées ; variantes complètes et approbation globale toujours ouvertes |
| Recette isolée admin | EXCEPTION ACCEPTÉE le 15 octobre — environnement inexistant ; tests CI/présentation avant sortie, corrections admin après déploiement acceptées par le propriétaire |
| Réseau initial des nouveaux médias hero | Exception ciblée acceptée et contrôlée ; aucun appel API supplémentaire, populations API distinctes des premières campagnes, pas d'exception générale de poids ou de requêtes |
| Premier thème réel | PASS CIBLÉ `da81bf4ab` — six variantes système/préférence, 25–28 frames post-FCP par cas, zéro mauvais thème |
| Navigation rapide pendant un zoom Leaflet | PASS CIBLÉ `483818796` — les deux adaptateurs corrigés, 2 718 tests passent et retour fiche → inventaire sans exception ; reste du parcours interrompu par une attente d'historique distincte |
| Instruction du propriétaire sur la fusion | Poursuite jusqu'au merge explicitement demandée le 15 octobre ; exécution conditionnée à la recette et aux checks, aucun défaut connu accepté silencieusement |
| Déploiement transactionnel et postchecks | NON EXÉCUTÉS |
| Libellés admin ciblés | 13 noms traduits ajoutés sans changement de handler ; 7 318 clés i18n, 179 tests Node et scan local à 13 constats hérités, nouvelle CI requise |
| Primitives transversales P20 | Onglets plats, panneau natif, dialogue modal avec focus ; 182 tests Node, 7 320 clés et scan à six constats hérités ; cinq nouveaux tests Angular à exécuter en CI |
| Upload/tiroir P19/P20 | Boutons natifs, rayons raccordés et même blocage pendant upload ; scan local à quatre constats hérités ; deux nouveaux tests Angular à valider |

## Informations de référence

- SHA : `7f6a9088cb20e1fa6e450b2e8f4bacb9e2019a22`
- Version de départ : `5.4.101`
- Branche : `feat/ui-v6-playground-redesign`
- Version cible : `6.0.0`
- Date de cadrage : 14 octobre 2026
- `origin/master` rafraîchi avant release : `7f6a9088cb20e1fa6e450b2e8f4bacb9e2019a22` (aucune divergence entrante)
- SHA de contenu 6.0.0 audité : `545345dc3a81c099a328e29a510447339b3f9342`

## Audit de portée P22

- Audit du SHA `da81bf4ab` : 352 fichiers modifiés, dont 275 sous `FRONT/`, 74 sous `docs/`, deux sous `API/` et un workflow sous `.github/`.
- 165 feuilles SCSS, 16 templates HTML, 45 tests/outils MJS, 30 fichiers TypeScript, huit inventaires CSV et 22 JSON ; les preuves graphiques et documents complètent ce décompte. Cet audit daté doit être recalculé au gel final, pas extrapolé après les corrections suivantes.
- Aucun fichier supprimé ; aucun changement sous `deploy/`. Les exceptions backend sont la référence PKCS autorisée et la stabilisation du test à horloge fixe ci-dessous ; le workflow reçoit uniquement la recette CI conditionnelle à la branche V6.
- `app.routes.ts`, `package-lock.json`, endpoints, DTO, guards, facades, ports, services, persistance et infrastructure inchangés.
- Les TypeScript de production réutilisent le composant image et CDK déjà installés, réalignent `sizes`, déplacent le style du tooltip vers le SCSS partagé, rendent natifs les contrôles d'accessibilité et corrigent le nettoyage Leaflet explicitement autorisé. Aucun contrat d'entrée/sortie n'est modifié. Le clavier des en-têtes triables passe par leur même clic et le tri existant, avec tests dédiés.
- Zéro ajout de `!important`, gradient décoratif, `backdrop-filter`, letter-spacing négatif, `overflow-x: hidden` ou dépendance UI. Les URLs du nouvel outil de recette sont réservées aux lectures publiques et à son origine de navigateur isolé ; elles ne sont pas ajoutées au produit.
- Dernier inventaire généré et réconcilié avec la base exacte : 148 routes, 293 composants, 268 SCSS et deux styles inline. La régénération finale après gel visuel reste requise ; elle n'est pas déclarée achevée pendant les corrections de recette.

## Décision avant fusion

Le 15 octobre, le propriétaire autorise les corrections nécessaires aux blocages et demande de poursuivre jusqu'au push, à la PR et au merge. Cette instruction est enregistrée sans transformer les gates ouverts en PASS. La revue visuelle propriétaire demeure non réalisée et les critères de recette restent applicables. Le garde mémoire local, hors dépôt et hors livraison, reçoit une récupération sur preuve native de fin pour les seuls appels d'entrée CUA finis et strictement bornés ; 31 tests ciblés passent, seuils et classification des opérations inchangés. Aucun processus actif, onglet ou automatisation GSC n'est fermé ou désactivé par cette réparation.

### Exception de nettoyage Leaflet autorisée le 15 octobre

Le propriétaire répond « Oui, corriger uniquement ce nettoyage » au diagnostic de fin de zoom après destruction de la carte. Le correctif est borné à `LeafletMapComponent.ngOnDestroy()` : libération immédiate de la référence du composant, retrait immédiat au repos ou retrait après l'événement natif `zoomend` et son dispatch. Il ne touche pas aux données, marqueurs, interactions, dépendances, champs privés Leaflet, facades ou API. Deux tests Angular et la recette rapide ayant découvert l'exception doivent passer avant de lever ce blocage. L'autorisation ne vaut pas feu vert de fusion ni permission d'élargir une correction fonctionnelle.

La PR unique est ouverte. La fusion reste interdite jusqu’à ce que la CI Node 24 du dernier commit soit entièrement verte, que les scénarios Angular réels et les comparaisons visuelles contractuelles soient exécutés puis revus, et que le propriétaire écrive explicitement son feu vert. Les captures de fixtures ne permettent pas de remplacer cette recette. La procédure de rollback demeure celle de `docs/deploy/transactional-app-deployment.md` et porte sur une génération complète.

### Ajustement explicite du propriétaire — recette admin

Le 15 octobre, en réponse à la demande d'URL et d'accès de recette, le propriétaire indique ne pas disposer d'un environnement isolé et demande de réaliser les écrans admin, quitte à les corriger un par un après déploiement. Cette instruction plus récente lève le blocage lié à l'absence d'environnement pour la recette réelle admin. Les 58 écrans restent dans le périmètre d'implémentation et de tests CI ; leur validation opérationnelle complète n'est pas déclarée acquise. Aucune mutation de production n'est autorisée pour simuler une recette. Cette acceptation de risque ne vaut ni feu vert final de fusion, ni acceptation d'un défaut bloquant connu de sécurité, permission, métier ou disponibilité. Les autres gates restent applicables.

## Stabilisation d'un test au 16 octobre

L'autorisation générale de corriger les blocages couvre le test `GetWatchPilotMetricsQueryHandlerTests.HandleAsync_NormalizesAllMetricsToTheRequestedUtcCalendarDays`. Son horloge système rendait les dates fixes de septembre invalides après le changement de journée UTC. Le correctif injecte uniquement le `FixedWatchPilotTimeProvider` déjà présent ; aucune assertion, règle métier, borne de rétention, API ni implémentation de production n'est modifiée. La validation est confiée à la CI.

## Exception sécurité autorisée le 15 octobre

Le scan détecte `System.Security.Cryptography.Pkcs 10.0.0`, transitive déjà présente sur la base `origin/master`, affectée par [CVE-2026-84008, High 7,5](https://github.com/advisories/GHSA-846f-f764-76mx) et [CVE-2026-70162, Moderate 5,9](https://github.com/advisories/GHSA-2fmv-rv2c-59x7), publiées le 13 octobre. Le propriétaire a explicitement autorisé le correctif minimal dans la même PR : référence directe `10.0.13` dans le projet Infrastructure, commit `235470597`. Aucun endpoint, code métier ou configuration d'infrastructure n'est modifié par cette exception ; le scan est conservé sans contournement.

La [CI du commit 4bde5e74d](https://github.com/Ced59/AmusementParkProject/actions/runs/38689894921) réussit intégralement, y compris le scan de sécurité et les builds Docker. Le déploiement est normalement ignoré pour la PR. Les corrections visuelles et l'outillage de capture ajoutés ensuite imposent une nouvelle CI.

La [CI du commit e98d7f8fb](https://github.com/Ced59/AmusementParkProject/actions/runs/38697638768) réussit également intégralement : 19 routes publiques capturées, 38 lieux Fleury rendus en SSR et quatre interactions d'accueil hydraté vérifiées. Les parcours compte/admin, les états rares, la comparaison visuelle exhaustive et le feu vert propriétaire ne sont pas soldés : **la décision demeure NO-GO**.

La [CI ce3f7b800](https://github.com/Ced59/AmusementParkProject/actions/runs/38699592749) réussit intégralement avec la route racine ajoutée (20 routes). Son diagnostic révèle des contrastes visibles à corriger, malgré les tests précédemment verts : les gates P01/P02/P03/P20 restent ouverts. Les nouvelles corrections et l'élargissement à 36 routes doivent passer la prochaine CI avant tout nouveau verdict.

La [CI 3e7266e0e](https://github.com/Ced59/AmusementParkProject/actions/runs/38701602727) valide tests/build/budgets/sécurité mais échoue dans la capture : le cas tarifs Fleury est réellement vide (API et page V5 404), non une fiche remplie avec H1. L'outillage exige désormais la preuve de cet état vide et ajoute Efteling rempli. Deux derniers contrastes calendrier/galerie sont corrigés ; les 38 routes de la nouvelle campagne et les gates ouverts doivent encore être validés.

La [CI 2675e3a8b](https://github.com/Ced59/AmusementParkProject/actions/runs/38703888781) réussit intégralement avec les 38 routes. Le diagnostic identifie des libellés encore insuffisamment contrastés sur les pages ajoutées ; corrections, couleur protégée du wordmark, périmètre fermé de l'inventaire et nouveaux scénarios hydratés restent à attester sur le prochain commit. Aucune recette complète n'est déduite de la seule couleur verte de la CI.

Le workflow existant reçoit uniquement deux étapes de recette conditionnées à la PR de cette branche : capture initiale SSR publique et dépôt de l'artefact. Les conditions de livraison, images et déploiement ne sont pas modifiées. La PR reste en brouillon tant que la recette complète n'est pas démontrée.

La [CI 5f5de42e3](https://github.com/Ced59/AmusementParkProject/actions/runs/38707367998) valide tests, build et sécurité et produit les 1 976 captures des 38 routes ainsi que les 38 détails Fleury. Les nouveaux scénarios hydratés atteignent le retour navigateur, puis l'outil échoue en répondant à des requêtes annulées par Chromium. Le correctif de recette n'accepte cette erreur de protocole que si `Network.loadingFailed` atteste l'annulation du même identifiant réseau ; les autres erreurs restent bloquantes. Une nouvelle CI est requise.

La [CI adcd267f1](https://github.com/Ced59/AmusementParkProject/actions/runs/38709110336) passe les seize contrôles hydratés et les captures, mais échoue au nettoyage du navigateur. Le prochain correctif draine les lectures avant fermeture et bloque tout nouvel accès réseau pendant le nettoyage. Les nouveaux tests HTTP Express restent exclusivement sur Linux CI. Le réalignement typographique/géométrique à la maquette exige également une nouvelle CI et revue visuelle ; la décision reste NO-GO.

Le verrou mémoire local a été réparé avec l'autorisation explicite du propriétaire le 15 octobre : récupération uniquement sur preuve native de fin de l'opération exacte, identité conservée, seuils inchangés, aucun processus actif fermé. Les 26 tests ciblés du garde et les 30 tests de présentation ciblés passent. Les changements V6 préparés sont publiés dans `c74663c46` et `042e86389`. La [CI 042e86389](https://github.com/Ced59/AmusementParkProject/actions/runs/38715838997) détecte ensuite les attributs `sizes` restés sur les anciennes gouttières (à 320 px : 304 déclarés / 292 rendus pour le hero, 272 / 260 pour la photo). Leurs formules sont réalignées sur 48/30/28 px, sans modifier chargement, sources ou résolution. La recette CI reste requise.

## Rollback

En cas d’échec après feu vert et fusion, appliquer la procédure transactionnelle existante de `docs/deploy/transactional-app-deployment.md` vers la génération complète antérieure. Aucun rollback CSS sélectif, aucune publication partielle et aucun mélange permanent V5/V6.

