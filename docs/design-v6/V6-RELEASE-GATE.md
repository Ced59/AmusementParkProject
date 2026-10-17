# V6 — gate de release 6.0.0

## Décision actuelle

Le diagnostic CI est séparé en deux phases **toutes deux obligatoires** : shell admin, publication immédiate `v6-admin-preflight`, puis compte standard/incomplet, puis campagne intégrale inchangée. Sans phase explicite, le CLI conserve tous les cas ; valeurs inconnues ou phase sans garde Linux CI refusées. Leur réunion est testée égale au diagnostic original. Le rapport nomme la phase et conserve `fullCampaignRequired:true`. Le probe mesure désormais aussi le bouton de saut et refuse son recouvrement de la toolbar/menu ou sa sortie du viewport ; aucun défaut produit n'est accepté par cette séparation. Nouveaux rendus requis.

La publication d'artefact du diagnostic est placée **avant** la longue campagne intégrale, sous la même condition PR/branche V6 et `always()`, pour permettre sa revue sans attendre plusieurs milliers d'autres captures. Le diagnostic et la campagne restent tous deux obligatoires ; aucun `continue-on-error`, nouvelle permission, secret, serveur, condition de production ou seuil n'est ajouté/modifié. L'artefact intégral existant reste conservé. Changement d'outillage CI seulement, test d'ordre/portée ajouté ; nouvelle CI à obtenir.

Le [run `1f603739c / 38984534106`](https://github.com/Ced59/AmusementParkProject/actions/runs/38984534106) valide **2 778 tests Angular**, dont les deux nouveaux tests de hauteur/nettoyage du bouton flottant ; un ancien test de style avatar échoue encore car il exige `7.5rem`. Assertion actualisée pour conserver carré/rond/cover et vérifier 120 px, max-width 100 %, hauteur auto. Pas de build/capture acquis sur ce run, nouveau passage requis.

Cause exacte du précédent échec master/expiré : le jeu client contient un tirage aléatoire de quatre héros différent du SSR, mais sa réponse featured conserve les exclusions du SSR (TransferState sur l'accueil normal). La redirection compte est CSR sans ce TransferState et demande légitimement les exclusions du tirage client ; la fixture exacte refuse. La recette de session expirée rejoue désormais **le jeu SSR cohérent**, avec ses quatre IDs et sa clé featured correspondante, pour master et V6. Aucun endpoint générique, réécriture de query ou forward réseau ajouté, comparaison P04.6 inchangée. Ancien artefact : standard/incomplet FR master/V6 passent chacun respectivement **2 672/1 702 contrôles et 2 366/1 508 captures** ; aucune variante suivante n'est acquise.

Le [run `0b7570fb6 / 38981470665`](https://github.com/Ced59/AmusementParkProject/actions/runs/38981470665) confirme enfin la police racine **32 px** et révèle un défaut réel à 320 px sombre : document **520 px**, accès Admin mobile recouvert par le bouton de saut. Les six captures normales passent, zéro erreur fixture/exception ; l'image de refus a été inspectée. Correctif présenté : grille/footer sans minimum rigide, wordmark ajusté uniquement par son conteneur (fichier protégé inchangé), navigation qui se redistribue et hauteur réellement mesurée dans la passe RAF existante, avatar 120 px conforme à son contrat image et profil refluant. Aucun masquage horizontal, nouveau listener, observer ou dépendance. **175 tests Node ciblés PASS** ; deux tests Angular de mesure/nettoyage ajoutés, **non exécutés localement**. Nouveau rendu et mesure du surcoût JS obligatoires, plafond P04.6 +3 302 octets inchangé. **NO-GO**.

L'ancienne campagne complète `db82d71eb / 38973483962` s'est terminée en FAIL sur le scénario master/session expirée après les captures publiques ; pas de PASS intégral. L'artefact `12080924759` est conservé (environ 509 Mo), cause précise à lire dans son rapport sans attribuer cet échec à la V6 ni à l'expiration de fixture par supposition.

La fixture d'auth UI est bornée à la fenêtre maximale de six heures du job CI, au lieu d'expirer au milieu des nombreuses captures après une heure. Seul son jeton synthétique non signé change ; jeux de données master/V6 identiques, issuer/audience `.invalid`, refus du refresh expiré et tous les contrôles restent conservés. Aucune durée ni règle d'auth réelle n'est modifiée, aucun workflow étendu et aucune preuve serveur revendiquée. Nouvelle CI toujours requise.

Dernier run terminé [`acbbf19f6 / 38979892609`](https://github.com/Ced59/AmusementParkProject/actions/runs/38979892609) : **six captures compte admin normales PASS**, 320/390/1 366 px clair/sombre, root mesurée 16 px, zéro fixture/exception. Le protocole refuse ensuite la police doublée car sa mesure immédiate ne confirme pas l'override ; aucun rendu agrandi/public-toolbar n'est acquis. Attente bornée à 30 frames de recalcul ajoutée, **égalité exacte avec la taille doublée conservée**, CSS/valeurs réellement mesurées enregistrées en cas de refus et restauration maintenue. **173 tests Node ciblés PASS**, nouveau run obligatoire ; aucun changement produit pour ce correctif de mesure.

Dernier run terminé [`a31f12c25 / 38978469321`](https://github.com/Ced59/AmusementParkProject/actions/runs/38978469321) : **FAIL du scénario admin**, zéro capture, zéro fixture/exception. La recette exigeait à tort la toolbar publique dans le layout compte : `app.routes.ts` compose `AccountLayout` pour le profil, sans toolbar ; seul `PublicAppLayout` la monte. Le test est corrigé pour mesurer les liens sur le profil, puis les liens/toolbar sur la page publique confidentialité, dans les deux tailles de police. Aucune toolbar n'est ajoutée au compte et aucun routage/permission ne change. **172 tests Node ciblés PASS** après correction ; nouveau rendu obligatoire, stress non encore exécuté. Le correctif de CTA Parc Fit `9492817ac` est enregistré sur la branche mais attend sa CI.

Le run terminé [`2d93b5e68 / 38977656405`](https://github.com/Ced59/AmusementParkProject/actions/runs/38977656405) refuse uniquement les contrastes du CTA Parc Fit après **736 contrôles / 392 captures V6 standard FR**, zéro fixture/exception. Les sept nouveaux états de formulaire passent, aucune recherche/géolocalisation demandée. Le libellé est mesuré à **1,49:1 sombre / 1,66:1 clair** : `.park-fit-submit span` écrasait l'encre du bouton. Le selector vise désormais seulement la description directe, sans changement de label/action/fond. **171 tests Node ciblés PASS** après correction et inventaires recalculés. Nouveau rendu requis ; le stress admin/les autres variantes ne sont pas déduits de ce refus.

Le prochain diagnostic ouvre le shell admin synthétique à 320/390/1 366 px, normal puis police racine doublée et mesurée, V6/master et deux thèmes, avant de conserver standard/incomplet FR. Toute géométrie/absence/recouvrement échoue toujours ; restauration et image avant nettoyage sont testées. **170 tests Node ciblés PASS**, source UI inchangée depuis `2d93b5e68` ; ce stress n'est ni un PASS de rendu anticipé ni le zoom natif 200 %. **NO-GO maintenu.**

Dernier run terminé [`b56328197 / 38975602735`](https://github.com/Ced59/AmusementParkProject/actions/runs/38975602735) : **FAIL de recette master**, pas un PASS global. V6 standard FR passe 670 contrôles / 364 captures, zéro erreur fixture/exception. Master standard atteint 603 contrôles / 328 captures puis reçoit `Invalid InterceptionId.` pour la police locale `syne-latin-ext.woff2` au passage vers l'histoire. Artefact `12079610272` conservé ; aucune exception Angular. La recette suivante attend les polices réellement chargées et le drainage borné des réponses locales avant remplacement de document/capture, sans élargir les erreurs ignorées. Les identités/propriétaires/proofs natifs de tout prochain refus sont enregistrés. **168 tests Node ciblés PASS**, 23 templates sans contrat manquant. Nouveau run intégral obligatoire ; le cas admin-shell n'a pas encore été exécuté dans ce run refusé.

Complément après `b56328197` : trois routes SSR Parc Fit (43 chemins préparés), sept états de formulaire/absence de résultat sans recherche, 37 encres raccordées dans cinq feuilles SCSS, contrats/données/algorithmes inchangés. **166 tests Node ciblés PASS**, syntaxe des scripts et inventaires contrôlés. Nouveaux rendus CI requis ; résultats calculés, comparaison remplie et workflows serveur non déclarés testés par ce complément. La décision reste **NO-GO**.

Le diagnostic préalable du [run `db82d71eb / 38973483962`](https://github.com/Ced59/AmusementParkProject/actions/runs/38973483962) est **PASS** : standard/incomplet FR, master/V6, 390/1 366 px et deux thèmes. Le run poursuit la campagne complète ; aucun PASS intégral n'est encore déduit. Les compléments locaux suivants (encre primaire admin, barre et nouveau cas admin-shell) exigent leur propre CI sur le prochain HEAD.

La recette admin-shell est ajoutée sans environnement privé : entrée de rôle **synthétique CI uniquement**, liens conditionnels header/mobile et toolbar lazy réels, 13 largeurs FR et mobile/desktop dans les sept autres langues, master/V6 et deux thèmes. Aucun écran admin ni endpoint admin n'est ouvert par ce scénario, aucune mutation métier ; il ne certifie ni autorisation serveur ni workflow admin. **162 tests Node ciblés PASS** et syntaxe de capture contrôlée. Le garde vérifie présence des deux liens, sept accès mobiles, bounds/taille/hit-test et absence de recouvrement toolbar/navigation ; nouveaux rendus obligatoires.

Dernier run [`38968888897`](https://github.com/Ced59/AmusementParkProject/actions/runs/38968888897), `4331fa087` : **FAIL du diagnostic incomplet**, après réussite des scénarios standard FR V6 et master, chacun à **670 contrôles / 364 captures**. Les deux anciens contrastes comparaison/lignée et l'alignement des champs d'année passent côté V6. La fixture V6 incomplète termine 426 contrôles / 232 captures sans erreur fixture/interception ni exception, mais refuse cinq captures pour un seul avertissement e-mail à **3,38:1**. Le SCSS corrigé réutilise l'encre AA ; l'audit lexical raccorde aussi les anciens noms de variables absents aux tokens existants, y compris les styles admin. **157 tests Node ciblés passent** après correction. Les 2 777 tests Angular / 583 fichiers, build/budgets/backend/sécurité du run passent ; nouvelle CI, master incomplet et campagne intégrale toujours obligatoires. Le PASS des scénarios master ne certifie pas ses anciens contrastes, conservés comme baseline.

Un diagnostic préliminaire Linux CI vérifie maintenant les scénarios isolés standard/incomplet FR à 390/1 366 px, deux thèmes, V6 puis master. Il réutilise le navigateur de recette et le nettoyage existants, sans serveur ni réseau privé. Son rapport porte `preflightOnly: true` et `fullCampaignRequired: true` : **il ne valide aucune gate globale**. La campagne complète reste obligatoire après réussite ; un échec conserve ses captures dans le même artefact et bloque les étapes suivantes. Thèmes, huit langues, paliers complets, état session expirée, SSR public et métriques réseau ne sont pas remplacés par ce sous-ensemble.

Les 53 tests Node ciblés compte/tokens passent localement, sérialisés ; syntaxe du script de capture contrôlée. Aucun navigateur, serveur ni build local n'est lancé. Les conditions de production/master ne changent pas ; seul le diagnostic PR V6 et la conservation de son artefact sont ajoutés.

Le diagnostic peut désormais **recenser plusieurs contrastes défaillants d'une même fixture avant de la refuser**. Ce mode exige explicitement le flag préflight, garde chaque mesure/capture dans `layouts` et vérifie obligatoirement toutes les issues avant `passed=true` : aucun défaut n'est accepté. Géométrie, absence de texte mesurable, requête inconnue et exceptions restent des arrêts immédiats. La campagne complète conserve son arrêt au premier défaut et les seuils 4,5:1 / 3:1 inchangés. Les 69 tests Node ciblés compte/tokens/shell/footer passent, dont le refus final d'une fixture ayant un seul contraste différé et l'impossibilité de différer un débordement.

**NO-GO MERGE — PR #917 ouverte, recette complète non terminée.** Les PASS ciblés ci-dessous ne constituent pas une acceptation de tous les critères de chaque gate. Aucun merge, tag ni déploiement n’est autorisé avant les checks du dernier commit, la recette visuelle/fonctionnelle Angular réelle et le feu vert explicite du propriétaire.

| Contrôle | État |
|---|---|
| Branche unique issue du dernier `origin/master` | PASS |
| SHA et version de départ consignés | PASS |
| Inventaire route/template/SCSS/interaction généré | SOURCE RÉCONCILIÉE `db82d71eb` — 148 contrats de routes et 293 fichiers de composants identiques, 268 SCSS + 2 styles inline, CSV master et V6 versionnés ; source distincte des scénarios exécutés |
| Baseline visuelle V5 initiale | PASS |
| P00 inventaires | Base exacte et V6 réconciliées : 2 057 → 2 057 cibles après bouton natif standalone, deltas présentationnels expliqués ; états et baseline comportementale complète toujours non démontrés |
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
| P21 CI Node 24 | PASS complet `fc742b010` — 2 769 tests Angular / 583 fichiers, 224 tests qualité, 224 assertions HTTP, 307 contrôles publics hydratés, 132 contrôles/104 captures profil master/V6 et Docker ; compléments listes/session expirée à exécuter |
| Exception P04.6 | PASS ciblé `7115bb901`, six paires, +3 110 octets JS sous plafond +3 302, aucune défaillance ; économie du seul pictogramme prouvée séparément, aucun élargissement de l'exception positive |
| Dernière CI terminée `0b7570fb6` | Six captures normales passent ; FAIL réel à police 32 px / viewport 320 px (document 520 px et Admin recouvert), zéro fixture/exception ; correctif responsive préparé, nouveau rendu et budget JS requis |
| Recette pratique additionnelle | PASS CIBLÉ `791dc2c46` — 732 captures hydratées additionnelles, calendrier localement défilable, météo, 30 popups, vidéos, récit et standalone huit langues ; scénarios absents et revue exhaustive toujours ouverts |
| Version `6.0.0` et historique 8 langues | PASS — commit de contenu `545345dc3a81c099a328e29a510447339b3f9342` |
| Diff de portée | Exceptions autorisées : PKCS 10.0.13, nettoyage Leaflet, tests backend stabilisés (deadline image PASS `136a9b8e2`) ; correction présentationnelle de quatre messages de secours dans trois facades auth, sans modification des mécanismes/ports/validations/payloads, sept nouveaux tests auth PASS `f975d9bc4` |
| PR unique | [#917](https://github.com/Ced59/AmusementParkProject/pull/917) ouverte ; recette complète encore requise |
| CI intégralement verte | OUI sur `fc742b010`, [run 38892315894](https://github.com/Ced59/AmusementParkProject/actions/runs/38892315894), Docker compris, déploiement ignoré ; ne préjuge pas des commits suivants ni des gates ouverts |
| Revue visuelle propriétaire | NON DEMANDÉE |
| En-têtes ParkItem et standalone | PASS GÉOMÉTRIE CIBLÉ `a6bbfb21d`, captures Rhaegal et Hasenhorn 390 px inspectées ; variantes complètes et approbation globale toujours ouvertes |
| Recette isolée admin | EXCEPTION ACCEPTÉE le 15 octobre — environnement inexistant ; tests CI/présentation avant sortie, corrections admin après déploiement acceptées par le propriétaire |
| Réseau initial des nouveaux médias hero | Exception ciblée acceptée et contrôlée ; aucun appel API supplémentaire, populations API distinctes des premières campagnes, pas d'exception générale de poids ou de requêtes |
| Premier thème réel | PASS CIBLÉ `791dc2c46` — six variantes système/préférence, 25–29 frames post-FCP par cas, zéro mauvais thème |
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

Audit borné du 17 octobre au commit `e31fd061d` : **448 fichiers**, zéro suppression, zéro route/guard/resolver ou lockfile modifié. Hors frontend/docs V6 : le workflow de recette et les quatre fichiers backend de sécurité/tests déjà autorisés, aucun code métier ni fichier `deploy/`. Le nouveau helper admin-shell est un outil de recette hors bundle, pas un service de production. Cette liste doit encore être rafraîchie au gel final.

- Audit du SHA `da81bf4ab` : 352 fichiers modifiés, dont 275 sous `FRONT/`, 74 sous `docs/`, deux sous `API/` et un workflow sous `.github/`.
- 165 feuilles SCSS, 16 templates HTML, 45 tests/outils MJS, 30 fichiers TypeScript, huit inventaires CSV et 22 JSON ; les preuves graphiques et documents complètent ce décompte. Cet audit daté doit être recalculé au gel final, pas extrapolé après les corrections suivantes.
- Aucun fichier supprimé ; aucun changement sous `deploy/`. Les exceptions backend sont la référence PKCS autorisée et la stabilisation du test à horloge fixe ci-dessous ; le workflow reçoit uniquement la recette CI conditionnelle à la branche V6.
- À ce SHA d'audit, `app.routes.ts`, `package-lock.json`, endpoints, DTO, guards, facades, ports, services, persistance et infrastructure sont inchangés. L'exception ultérieure des quatre messages de secours dans trois facades auth est explicitement décrite dans le tableau de portée et la recette.
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

## Reprise autorisée le 16 octobre

Le run `348589406` s'arrête avant les métriques carte, sur une mesure de lightbox transitoire après changement de viewport ; capture peinte et mesure divergent. Le contrôle attend maintenant deux géométries réelles identiques, toutes assertions conservées, avec dernière mesure en cas d'échec. Les 38 tests Node ciblés médias/histoire/cartes passent localement ; aucune campagne navigateur complète n'est déduite de ce résultat. Le défaut de popup à 320 px reste ouvert tant que sa preuve diagnostique n'est pas obtenue.

La CI `df03114c9` passe 2 767 tests Angular / 582 fichiers, 213 tests qualité Node et 224 contrôles HTTP. La recette s'arrête à 139 contrôles sur le cadrage du document à 390 × 640 ; aucun échec applicatif/interception n'est enregistré. Le popup tient dans les bounds Leaflet mais la carte entière dépasse en haut du viewport. Le recadrage de recette doit repasser avant d'atteindre les scénarios vidéo/histoire/standalone ; aucun résultat ultérieur n'est acquis.

Le propriétaire autorise la libération du verrou de l'appel navigateur exactement confirmé terminé, et des suivants dans le même cas après vérification. La récupération vérifie l'identité native du propriétaire, la fin native de l'appel exact et la fin de son tour sous le verrou partagé. Aucun processus, seuil, hook ou service n'est modifié. Cette autorisation n'autorise pas la suppression d'un verrou actif ou sans preuve de fin.

Les corrections sauvegardées de recette vidéo et du popup Leaflet passent 35 contrôles Node ciblés et la compilation Sass du popup (2 501 octets CSS compressés). Le popup réutilise sa mise en page native après redimensionnement, rebranche les mêmes liens et conserve leur focus ; test Angular et captures réelles à valider en CI. Aucun PASS global ni feu vert de fusion n'est déduit de ces seuls contrôles locaux.

## Rollback

En cas d’échec après feu vert et fusion, appliquer la procédure transactionnelle existante de `docs/deploy/transactional-app-deployment.md` vers la génération complète antérieure. Aucun rollback CSS sélectif, aucune publication partielle et aucun mélange permanent V5/V6.

