# V6 — couverture d’implémentation

## Point de contrôle du 17 octobre — standard V6/master

Les quatre rapports intégraux standard/incomplet FR du run `db82d71eb / 38973483962` sont désormais lus : **2 672 contrôles / 2 366 captures standard**, **1 702 / 1 508 incomplet**, pour **chacun** de master et V6, zéro fixture/exception. La session expirée échoue ensuite sur l'incohérence héros client / exclusions featured SSR de la fixture, corrigée dans `10f3de113` par le rejeu exact du jeu SSR cohérent. Les huit langues et variantes suivantes ne sont pas déduites de ces PASS.

Le nouveau stress admin de `0b7570fb6` révèle un document de 520 px à viewport 320 px / root 32 px et un accès Admin masqué ; correction de rendu `a732fb2b1`, inventaire réconcilié sur ce SHA exact, mêmes 148 routes / 293 composants / 2 057 interactions. **175 tests Node ciblés PASS**, puis **55 tests compte PASS** après ajout du garde de population SSR. CI `1f603739c` : deux nouveaux tests Angular du bouton flottant PASS, 2 778 PASS et une assertion CSS avatar périmée actualisée ; nouveau build et nouvelles captures requis. La nouvelle campagne `10f3de113` est en cours, pas déclarée intégralement PASS.

Le diagnostic `a31f12c25` révèle une mauvaise attente du test, pas une toolbar manquante du produit : le compte n'utilise pas le shell public. La recette distingue maintenant profil et confidentialité publique avant de contrôler la toolbar, sans changer les layouts/routage du site. **172 tests Node ciblés PASS** après correction et CTA ; captures de stress encore à exécuter, anciens FAIL conservés.

Le dernier complément est uniquement de recette : shell admin normal/police doublée réellement mesurée, dès le diagnostic à 320/390/1 366 px ; matrice intégrale et huit langues conservées. Les 170 tests Node ciblés passent, mais aucun nouveau rendu n'est déclaré acquis. L'inventaire de source UI pointe maintenant exactement sur `2d93b5e68abfa552b388bd83a133d82c6661e588` ; outils/docs ultérieurs ne changent pas ses composants/routes/styles.

Trois chemins SSR Parc Fit et sept états client sans recherche sont ajoutés après `b56328197` : 43 chemins SSR préparés, sans prétendre qu'ils sont déjà capturés. Champs/participants/préférences/validation invalides et états sans résultat sont distincts du calcul serveur ou d'une comparaison remplie. Les 37 encres de cette famille rejoignent les tokens existants ; inventaires source à nouveau calculés, comptes/contrats de routes/composants inchangés. **166 tests Node ciblés PASS**, recette CI de ce complément ouverte.

Le diagnostic préalable suivant `db82d71eb / 38973483962` passe les quatre cas FR standard/incomplet master/V6 ; la campagne complète tourne. Le scénario conditionnel `admin-shell` est préparé séparément pour les huit langues, sans aucune ouverture ou mutation admin. Il mesure les liens, les sept accès mobiles et la toolbar réelle ; il ne transforme pas l'exception propriétaire admin en recette opérationnelle réalisée. **162 tests Node ciblés PASS**, preuve navigateur du nouveau scénario en attente.

Le run `4331fa087 / 38968888897` termine les scénarios isolés standard FR V6 **et** master (670 contrôles / 364 captures chacun). Le cas V6 incomplet termine 426 contrôles / 232 captures puis refuse un contraste e-mail, corrigé en source. Tous les compléments compte/auth/brouillons/histoire listés ci-dessous ont désormais une preuve standard FR de scénario ; leurs états complets, permissions serveur et workflows persistés ne sont pas déduits de ce sous-ensemble. La matrice historique ci-dessous décrit les anciens checkpoints, non le dernier état de recette. **Campagne intégrale et gel final encore ouverts.**

L'audit lexical couvre 1 895 fichiers SCSS/templates/TypeScript, sans référence obligatoire inconnue après migration vers les tokens existants. Les deux seules références externes optionnelles ont un fallback explicite. Les 75 fichiers de présentation raccordés et le warning e-mail sont protégés par 157 tests Node ciblés ; aucun nouveau composant métier, contrat ou route. Ce contrôle lexical ne prouve ni le scope calculé de chaque token ni le rendu admin opérationnel.

Les CSV V6 sont régénérés depuis `db82d71eb7a7ca31466545fc2c20f3551cfdd4a4`, base master immuable conservée. Comparaison indépendante : **148 contrats de routes identiques** (type, composant, template/styles, guards, resolvers, fichier de routage), **293 fichiers de composants identiques**, 268 SCSS + deux blocs inline, zéro template manquant. Les cibles reviennent à **2 057 → 2 057** : au delta historique −1 s'ajoute le bouton clavier natif standalone déjà documenté et testé. L'égalité des comptes n'est pas utilisée comme preuve d'égalité fonctionnelle ; les contrats et scénarios restent distincts. `origin/master` est à nouveau lu le 17 octobre, toujours `7f6a9088…`. Nouveau recalcul requis si la source change après ce checkpoint.

Les tailles de styles sont recalculées après la correction de couleur de texte du bouton primaire dense admin ; ce delta purement SCSS est explicitement indiqué dans le README généré. Les ensembles de routes/composants et les comptes ne changent pas. La baseline master reste intacte.

## Point de contrôle du 16 octobre — source et recette distinctes

`master` distant reste `7f6a9088cb20e1fa6e450b2e8f4bacb9e2019a22` après nouvelle lecture. Les CSV ci-dessous restent le snapshot de source `579060704`, pas un inventaire final du chantier. Le nouveau bouton clavier standalone ajoute une cible de présentation : son delta et les tailles SCSS devront être recalculés avec le générateur existant au gel final, sans modifier la base immuable.

La comparaison courante porte désormais sur **23 templates**, **2 197 → 2 244 contrats**, **zéro contrat manquant après les exceptions présentationnelles exactes documentées**. Les deux noms de région/groupe compte passent de littéraux anglais à des bindings traduits ; cette transformation ne permet ni handler absent ni autre remplacement. Les huit dictionnaires conservent toutes leurs 7 316 valeurs de base ; six clés de nommage clavier/a11y puis quatre messages de secours auth ont été ajoutés (7 326 au total). Le décor d'accueil supprimé était vide, caché et sans contrat fonctionnel. L'exception de libellés auth touche explicitement trois facades, sans changement de port, signal, validation, appel ou payload ; elle n'est pas dissimulée derrière une affirmation de diff SCSS pur.

Les 26 routes de la famille compte ne sont pas déclarées toutes recettées :

| Famille de recette compte | Routes | Preuve navigateur actuelle |
|---|---:|---|
| Profil et mot de passe oublié | 2 | PASS ciblé `fc742b010` et campagne publique ; identité/annulation, état vide de récupération, sans soumission |
| Collections, liste voyages, passeport et profils de groupe Park Fit | 4 | Master standard FR passe dans `7115bb901` ; V6 arrêtée au contraste du profil avant ces compléments, aucun PASS comparatif |
| Détails et outils de voyage | 7 | Master standard FR passe les sept vues dans `5ccfde1ed` ; variantes V6 et lecture seule encore non attestées |
| Éditeur visite, statistiques, partage et invitation passeport | 8 | Master standard FR passe les sept statistiques/partage dans `5ccfde1ed`, puis s'arrête au GET de rapports manquant dans la fixture éditeur ; réponse exacte corrigée, rendus V6 et workflows persistés encore ouverts |
| Notifications | 1 | Master standard FR passe dans `906bff28b` puis `5ccfde1ed` ; variantes V6 et vides encore non attestées |
| Confirmation et réinitialisation de compte | 2 | Sept nouveaux tests Angular auth PASS dans `f975d9bc4` ; recette UI isolée sans soumission préparée, rendu CI encore requis |
| Brouillons locaux anonymes | 2 | Recette IndexedDB CI vide/remplie/verrouillée/manquante préparée ; rendu CI et import réel ouverts |

Ce décompte conserve toutes les routes de la matrice et ne transforme pas la présence de tests en preuve de tous leurs scénarios, permissions ou états. La release reste NO-GO.

Le diagnostic `6e8a908f6` atteint 445 contrôles / 240 captures V6 standard FR mais reste FAIL (fixture de journal avec chevauchement non conforme au comportement existant, contrastes et cadrage). Le nouveau contrôle impose une cible réellement visible après reflow, et les 72 tests Node ciblés passent après corrections. Les captures antérieures sans bounds de cible ne sont pas utilisées pour déclarer TESTED tous les gabarits mobiles ; nouvelle campagne comparative obligatoire.

Les 36 tests Node compte/fixtures et 17 tokens passent (53 ciblés). Le diagnostic préalable ajouté dans `eea4e8b10` réutilise les scénarios FR standard/incomplet à 390/1 366 px, V6/master et deux thèmes ; **ce sous-ensemble ne remplace pas la campagne complète**. Le [run `38957806574`](https://github.com/Ced59/AmusementParkProject/actions/runs/38957806574) atteste profil/identité V6 aux quatre combinaisons largeur/thème, puis refuse le slogan footer clair à 3,38:1. Les suites suivantes ne sont pas exécutées ; les 1 561 contrôles / 1 378 captures de `5ccfde1ed` restent une preuve master partielle, pas une comparaison V6 réussie. Après correction sémantique du slogan, 65 tests ciblés compte/tokens/shell/footer passent ; nouveau rendu requis.

Audit complémentaire du 16 octobre : le filtre historique `*.component.ts` omettait **20 primitives `shared/ui` nommées simplement `dialog.ts`, `table.ts`, etc.** Le générateur corrigé lit les vrais décorateurs TypeScript et l'AST Angular des templates externes/inline, sans tronquer les expressions contenant `>`. Les CSV de la base immuable et de la V6 sont recalculés : **293 composants de production, 268 feuilles SCSS et deux styles inline** dans les deux versions, sans template manquant. Onze hôtes de tests sont explicitement exclus ; les événements et conditions héritées sont conservés. Ces nombres de source ne prouvent pas l'exécution des scénarios.

La base exacte `7f6a9088…` est relue depuis une archive Git limitée à `src/app` et `src/styles`, sans checkout, dépendances ni serveur supplémentaire. Une comparaison indépendante des CSV confirme les mêmes 148 routes (type, composant, fichier de routage, guards et resolvers) et les mêmes 293 fichiers de composants, dont 22 templates inline. Le générateur refuse désormais une déclaration de route non résolue au lieu de l'omettre silencieusement. `V6_SOURCE_ROOT`, `V6_SOURCE_REVISION` et `V6_INVENTORY_DIR` permettent de reproduire le calcul avec le même outil.

Les cibles recensées passent de **2 057 à 2 056** : upload **+1** (bouton natif pleine surface), panneau **−1** (ancien bouton interne regroupé dans l'en-tête natif) et lightbox **−1** (suppression du conteneur qui ne faisait que stopper la propagation, fond natif distinct). Ce delta de cibles n'est pas une perte d'action ; les handlers utiles et sorties restent vérifiés par la comparaison des contrats et les tests de composants.

## Référence immuable

- Branche : `feat/ui-v6-playground-redesign`
- Base `origin/master` : `7f6a9088cb20e1fa6e450b2e8f4bacb9e2019a22`
- Date de la base : `2026-10-10T02:07:19+02:00`
- Version de départ : `5.4.101`
- Cible unique : `6.0.0`
- Référence graphique : `APF-Playground-V6-Accueil-Reel-Standalone.html` du pack V6 daté du 14 octobre 2026.
- Exceptions graphiques : le wordmark réel `AMUSEMENT-PARKS.fun` dans la navbar et le footer, puis les deux segments traduits du slogan du footer.

## Inventaire P00 automatisé

Le script `FRONT/AmusementPark/tools/design-v6/generate-p00-inventory.mjs` analyse `app.routes.ts`, les enfants `loadChildren`, les métadonnées des composants, leurs templates et leurs styles. Il doit être rejoué après chaque intégration de `origin/master` et avant P22.

| Élément | Base master | V6 `a732fb2b1` |
|---|---:|---:|
| Routes terminales, redirections et écrans développés | 148 | 148 |
| Composants Angular de production | 293 | 293 |
| Contrôles, champs et cibles événementielles | 2 057 | 2 057 |
| Feuilles SCSS globales et locales | 268 | 268 |
| Blocs de styles inline | 2 | 2 |
| Templates de composants manquants | 0 | 0 |
| Écrans d’administration | 58 | 58 |
| Écrans compte/profil | 26 | 26 |
| Écrans publics généraux | 29 | 29 |
| Écrans parc | 19 | 19 |
| Écrans ParkItem | 10 | 10 |
| Écrans standalone | 3 | 3 |

Les sources exhaustives et versionnables sont :

- `generated/V6-ROUTE-INVENTORY.csv` : route, composant, template, SCSS, guards, resolvers et métriques d’état ;
- `generated/V6-COMPONENT-INVENTORY.csv` : tous les composants `ui`, `shared`, `features/public`, `features/profile`, `features/auth` et `features/admin` ;
- `generated/V6-INTERACTION-INVENTORY.csv` : boutons, liens, formulaires, champs, handlers, routes, états disabled et conditions de visibilité.
- `generated/V6-STYLE-INVENTORY.csv` : taille, variables, media/container queries, gradients, ombres, rayons et usages de `!important` de chaque feuille SCSS.

Les mêmes quatre CSV sous `generated/baseline-master/` décrivent exclusivement la base immuable. Leurs README consignent leur SHA. Les anciens nombres 273 / 1 869 correspondaient au filtre incomplet, pas à un retrait de composants. Les styles inline portent une référence `fichier.ts#style-N`, sans fabriquer de fichier SCSS.

Deux lignes `/:lang` peuvent apparaître dans la matrice : elles appartiennent aux deux branches de layout ordonnées de l’application. Elles ne représentent pas deux URLs publiques distinctes.

## Gates

**Lecture corrigée au 15 octobre 2026 : les PASS ci-dessous désignent les contrôles ciblés cités, pas l'acceptation des gates contractuels complets.** Les contrats statiques, les tests unitaires et la fixture responsive ne prouvent pas la fidélité pixel-perfect, les interactions Angular rendues, les huit langues et toutes les variantes authentifiées/admin. Ces recettes restent ouvertes et bloquent la fusion. L'implémentation n'est pas déclarée terminée.

| Gate | État | Preuve attendue |
|---|---|---|
| P00 — baseline et inventaires | CIBLÉ — RECETTE OUVERTE | 4 CSV générés, 24 captures V5 ; tous les états de baseline non exercés |
| P01 — tokens et thèmes | CIBLÉ — RECETTE OUVERTE | mapping et tests ciblés ; contraste exhaustif/focus à valider |
| P02 — primitives | CIBLÉ — RECETTE OUVERTE | 7 tests statiques verts, 160 facades conformes ; parcours clavier/SR à valider |
| P03 — shells et navigation | CIBLÉ — RECETTE OUVERTE | wordmark/slogan protégés, contrats de navigation inchangés ; rôles et menus complets à valider |
| P04 — accueil | CIBLÉ — RECETTE OUVERTE | structure réelle, hydratation/thème/consentement/recherche et pagination 5/10/20 vérifiés ; six paires réseau PASS_WITH_OWNER_EXCEPTION bornée, fidélité exhaustive ouverte |
| P05 — catalogue, exploration, recherche | CIBLÉ — RECETTE OUVERTE | filtres, carte, URL et pagination protégés statiquement |
| P06 — détail de parc | CIBLÉ — RECETTE OUVERTE | sections/actions protégées ; capture SSR Fleury disponible, autres profils à valider |
| P07 — inventaire des ParkItems | CIBLÉ — RECETTE OUVERTE | filtres/carte/pagination protégés ; liens réels des 38 lieux Fleury découverts |
| P08 — fiches ParkItem | CIBLÉ — RECETTE OUVERTE | 38 détails Fleury rendus en SSR ; interactions et états à valider |
| P09 — attractions autonomes | CIBLÉ — RECETTE OUVERTE | Hasenhorn visible capturé ; état masqué et interactions à valider |
| P10 — zones | CIBLÉ — RECETTE OUVERTE | liste, détail, carte, facettes et liens ParkItem protégés statiquement |
| P11 — cartes et plans | CIBLÉ — RECETTE OUVERTE | Leaflet/années/documents protégés ; gestes réels et plans à valider |
| P12 — médias | CIBLÉ — RECETTE OUVERTE | galeries/lightbox/crédits/lazy/vidéos protégés ; volumes et erreurs à valider |
| P13 — histoire | CIBLÉ — RECETTE OUVERTE | timelines/articles/snapshots/comparaison/lignées protégés statiquement |
| P14 — données pratiques | CIBLÉ — RECETTE OUVERTE | horaires/météo/prix/live protégés ; valeurs et interactions réelles à valider |
| P15 — notes, classements et actions sociales | CIBLÉ — RECETTE OUVERTE | contrats protégés ; actions authentifiées non exercées en recette |
| P16 — autres parcours publics | CIBLÉ — RECETTE OUVERTE | contrats protégés ; pages secondaires partiellement capturées |
| P17 — auth/compte | CIBLÉ — RECETTE OUVERTE | tests unitaires/contrats ; comptes et sessions de recette nécessaires |
| P18 — administration | CIBLÉ — EXCEPTION DE RECETTE ACCEPTÉE | 58 terminaux/guards/workflows inventoriés ; propriétaire accepte les corrections admin après déploiement faute d'environnement isolé, sans prétendre à une recette réelle complète |
| P19 — états rares | CIBLÉ — RECETTE OUVERTE | contrats protégés ; tous les overlays/erreurs/états longs à valider |
| P20 — responsive/a11y | PASS CIBLÉ | 30 cas Chromium, 13 largeurs, 2 thèmes, faible hauteur, 113 contrôles qualité/a11y ; revue Angular réelle à P22 |
| P21 — CI/performance/SEO | PASS CI CIBLÉ | CI da81bf4ab intégralement verte, 224 assertions HTTP, 138 contrôles hydratés ; exception réseau acceptée, CWV terrain et déploiement non attestés ; corrections suivantes à revalider |
| P22 — version et release atomique | NO-GO | PR #917 en brouillon ; recette complète, feu vert, merge et déploiement en attente |

Un gate reste bloquant tant que toutes ses preuves ne sont pas enregistrées. La version et la PR ont déjà été préparées sur la branche, mais ne constituent pas une validation P22 ni une autorisation de sortie.

Le PAL est ajouté comme troisième profil de parc à la prochaine campagne, aux côtés du Fleury et d'Efteling. Son URL et son état public sont vérifiés par recherche API anonyme, puis HTTP 200/canonical/H1 sur V5.4.101. Cet identifiant est une fixture de recette uniquement, pas une donnée figée du produit. La nouvelle couverture prévue est de 39 routes / 2 028 captures ; elle n'est pas déclarée exécutée avant résultat CI.

## Couverture P20 — responsive et accessibilité

- La fixture Chromium statique couvre 320, 360, 390, 414, 480, 680, 768, 960, 1024, 1180, 1366, 1440 et 1920 px dans les deux thèmes, plus 390 × 568 et 1024 × 600.
- Les 30 cas enregistrés dans `evidence/responsive-v6/responsive-report.json` ne présentent aucun overflow global, élément hors viewport, recouvrement de navigation fixe, cible de notation sous-dimensionnée ni réservation instable d’image ou de carte.
- Les largeurs 320–480 vérifient le reflow étroit ; elles ne remplacent pas le zoom navigateur 200 % ni l'agrandissement de police, encore à tester. `overflow-x: clip` n’est jamais utilisé comme verdict, car les bounds de chaque contrôle sont mesurés séparément.
- Les 30 captures WebP clair/sombre ont été produites sans serveur applicatif. Elles documentent la géométrie des composants, les libellés longs, le consentement, la navigation mobile et les panneaux admin/compte.
- `quality:accessibility-i18n` passe : 8 langues et 7 316 clés, contrastes actifs/footer, thème initial SSR, réduction de mouvement, navigation, focus/ARIA et 113 tests Node.
- Le contrôle a révélé puis corrigé les attributs `sizes` de la fiche parc, restés dimensionnés pour l’ancienne largeur éditoriale : les images hero et principale correspondent désormais au conteneur V6 de 1160 px.

La recette visuelle sur l’application Angular branchée aux données réelles, les rôles authentifié/admin et les gestes Leaflet reste à exécuter et documenter avant P22. Elle relève de la validation du chantier ; le feu vert humain ne remplace pas cette preuve. Le contrat interdit de démarrer un serveur applicatif sur ce PC sans autorisation et permet les vérifications en CI isolée.

## Préparation P22 — release atomique

- `origin/master` a été rafraîchi juste avant la release et reste au SHA de départ ; aucun correctif fonctionnel concurrent n’est absent de la branche.
- Les régénérations CI reproduisent les 148 routes et 273 composants de P00, sans route, template ou SCSS manquant ; la régénération versionnée après gel visuel reste à faire.
- `release-version.json` passe directement de 5.4.101 à 6.0.0 avec une entrée datée du 14 octobre 2026 et huit libellés localisés.
- L'audit de portée actualisé est dans `V6-RELEASE-GATE.md` : aucune suppression, route ou contrat métier modifié ; seule exception backend, la dépendance PKCS autorisée. Le workflow reçoit uniquement deux étapes de recette CI V6, sans changer le déploiement.
- Le SHA de contenu versionné et audité est `545345dc3a81c099a328e29a510447339b3f9342`.

La PR unique est désormais ouverte en brouillon : [#917](https://github.com/Ced59/AmusementParkProject/pull/917). La CI du commit `e98d7f8fb` passe intégralement, y compris sécurité et Docker. Le correctif PKCS 10.0.13 autorisé est dans `235470597`. Le 15 octobre, la référence GitHub de `master` reste `7f6a9088cb20e1fa6e450b2e8f4bacb9e2019a22`. La recette complète, le feu vert, le merge, le déploiement et les postchecks restent ouverts.

## Registre des cas sensibles P00

| Domaine | Référence conservée | Preuve de comportement attendue |
|---|---|---|
| Leaflet | composants `ui/maps` et cartes publiques | bounds, zoom, drag, tap, marqueurs, popup, filtre, navigation |
| Plans officiels | routes parc maps et sélecteur d’année | image/source/année, plans multiples, absence de plan |
| Médias | `app-image-display`, galerie, carrousel, vidéos | 0/1/8/9/100+, lazy, alt, crédits, erreur réseau, lightbox |
| Historique | timeline, article, snapshot, comparaison | pagination, années, sources, deep link, SEO SSR |
| Météo/horaire/prix/live | composants pratiques du parc/standalone | fuseau, fraîcheur, indisponible, mois, canaux et sources |
| Notes/commentaires | ratings et commentaires publics | anonyme/connecté, seuils, langue, pagination, erreurs |
| Collections/watch/passeport | routes publiques et profil | login, permissions, confirmation, état vide/plein |
| Park Fit/trips | parcours conditionnels | validations, retour, comparaison, brouillon local |
| Édition/import admin | formulaires, workbench, field mode | dirty, preview/apply, upload, long job, erreur et annulation |
| Auth/compte | modales et routes profil | login, Google, inscription, reset, expiration, export/suppression |

Les 58 terminaux admin et les 26 terminaux compte sont explicitement présents dans la matrice de routes ; ils ne peuvent pas être exclus par une absence de maquette.

## Couverture P03 — shells et navigation

- Le header et le footer utilisent toujours la structure complète de `.app-brand-wordmark` ; seul le pictogramme autonome, non sanctuarisé, a été retiré de ces deux compositions.
- Les clés `footer.brand.sloganData` et `footer.brand.sloganFun` restent les seules sources du slogan traduit.
- Les accès Accueil, Parcs, Technique, Classements, Constructeurs, À propos et Admin conditionnel restent présents, comme les contrôles profil/login, unités, langue et thème.
- La navigation mobile conserve ses six accès publics et l’accès Admin conditionnel, avec cible tactile, état actif et `safe-area-inset-bottom`.
- L’arbre dynamique et le trail conservent leurs routes, `queryParams`, niveaux et libellés issus de la facade ; seuls leurs styles ont changé.
- Les shells public, compte et admin conservent leurs outlets et leur composition fonctionnelle. Le public adopte la largeur éditoriale de 1160 px et l’admin garde la largeur dense de 1480 px.
- Preuve ciblée : 17 tests Node réussis, dont contraste du footer et des états actifs, absence de glassmorphism, marque protégée et exhaustivité statique des accès.

## Couverture P04 — accueil réel

- Le hero conserve ses deux CTA, les trois statistiques issues de `homeStats()` et les quatre emplacements issus de `heroFeaturedParks()` ; aucune valeur de la maquette n’est codée en dur.
- Le premier média réel disponible est le seul marqué `eager`/priorité haute ; les trois autres restent lazy. Le composant image, ses `srcset`, tailles, placeholder et texte alternatif existants sont réutilisés.
- La recherche conserve la saisie, le clavier, le bouton explicite, les catégories, l’effacement, l’autocomplétion, les suggestions, les liens par type, les états et la pagination 5/10/20.
- Les vedettes, derniers parcs, derniers articles et le partage sont tous conservés. Le bloc des derniers contenus garde `@defer`, l’hydratation au viewport et le prefetch idle.
- La mise en page transpose la maquette V6 en grille éditoriale, avec quatre cartes dynamiques et un repli sans animation décorative à 960/680/350 px.
- Preuve ciblée cumulée : 24 tests Node réussis. Les tests Angular existants restent la preuve exécutable des handlers et facades et seront lancés avec la suite P21.

## Couverture P05 — catalogue des parcs

- Le terme, la région, le statut, la classification d’audience et la portée de découverte restent connectés aux mêmes inputs/outputs et à la même facade.
- La carte reste une vraie carte Leaflet synchronisée : bounds, marqueurs, sélection, filtres régionaux et événement `markerClick` sont inchangés.
- Les cartes ouvrent toujours les routes de détail ; les résultats mixtes gardent leurs liens typés. Aucun clic n’est transformé en popup.
- La pagination serveur conserve ses liens `pageHref`, son trail, les tailles 6/9/18/36, le retour navigateur et les métadonnées SEO existantes.
- Les tests Angular existants couvrent directement page 2, précédent/suivant, changements de filtre/langue, tailles personnalisées, historique et erreurs HTTP. Ils ne sont pas réécrits par la refonte.
- Preuve ciblée cumulée : 28 tests Node réussis, surfaces catalogue sans gradient ni backdrop et contrats de carte/filtres/pagination présents.

## Couverture P06 — détail de parc

- Le hero conserve image LCP, logo, badges, titre, note, commentaires, collections, watch, visite passeport et tous les liens conditionnels (inventaire, zones, médias, histoire, carte, site officiel).
- Le cycle de vie, le live, les statistiques, la photo secondaire lazy, la description riche, le résumé de contenu, les données pratiques et références sont intacts.
- La colonne pratique conserve géographie, tarification, horaires et météo ; le partage et les parcs proches restent après le contenu principal.
- Les absences de logo, photo, description, géolocalisation ou données pratiques gardent leurs conditions et placeholders existants.
- Une seule image hero reste eager/prioritaire ; les médias secondaires restent lazy et les tailles responsive existantes sont inchangées.
- Preuve ciblée cumulée : 32 tests Node réussis et test de composant existant conservé pour le lien galerie, son nom accessible et ses tailles d’image.

## Couverture P07 — inventaire des ParkItems

- Les filtres texte, catégorie, type, zone et éléments fermés conservent leurs inputs, outputs, compteurs et conditions d’affichage.
- La carte de zone reste une vraie surface Leaflet, avec sélection des marqueurs et état explicite des éléments sans coordonnées.
- Chaque carte conserve sa navigation vers une page ParkItem complète ; aucune ouverture en popup ni modification de route n’est introduite.
- La pagination serveur conserve les tailles 6/12/24/48, le total, les liens et les actions précédent/suivant.
- Les badges, images de repli, états vide/chargement/erreur et le partage restent présents, avec une composition éditoriale plate Playground.

## Couverture P08 — fiches ParkItem

- Les liens précédent/suivant, retours parc/zone et facettes catégorie/type/sous-type conservent les mêmes routes et paramètres.
- Le hero, la description riche, les spécifications par type, l’accès, la géolocalisation conditionnelle, les médias, vidéos, historiques et contenus liés restent intacts.
- Les notes, commentaires, collections, watch, passeport, état live et partage gardent leurs conditions d’authentification et leurs événements.
- La carte n’est rendue que lorsque les coordonnées existent ; aucune géolocalisation ni relation parente n’est inventée.
- Preuve ciblée cumulée P07/P08 : 35 tests Node réussis. Les cas Le Fleury/Rhaegal/L’épée d’Odin/La Catapulte, sans média et non-attraction restent dus à la recette de données P20/P21.

## Couverture P09 — attractions autonomes

- La route autonome conserve son identité propre : aucun parent parc ou ParkItem artificiel n’a été introduit.
- Description riche, caractéristiques, constructeur, état, conditions, image LCP, prix, horaires, météo, localisation, carte conditionnelle, historique et site officiel restent présents.
- Les données tarifaires et liens d’achat proviennent toujours des modèles/API existants ; aucune donnée Pendolino ou valeur de maquette n’est intégrée.
- Preuve ciblée : contrats statiques et surfaces plates inclus dans les 38 tests Node cumulés. Le contrôle d’une attraction publique visible et de Pendolino masqué reste dû à P20/P21.

## Couverture P10 — zones

- La liste conserve quantités réelles, descriptions localisées, highlights par type, lien de détail, retour au parc, état vide et partage.
- Le détail conserve description, compteurs, lien filtré `?zone=`, carte Leaflet avec bounds, cartes ParkItem, état sans marqueur et partage.
- Les parcs avec et sans zone restent distingués par les conditions et états existants ; aucune zone fictive n’est créée.
- Les surfaces adoptent les filets, petits rayons et accents Playground sans modifier les modèles ni la facade.

## Couverture P11 — cartes interactives et plans officiels

- Les modes carte interactive/plans officiels, tabs, filtre fermé, filtres zone/catégorie et états sans géolocalisation restent câblés aux mêmes inputs/outputs.
- Leaflet conserve `fitBounds`, zoom maximal, marqueurs de navigation, sélection catalogue et événement `markerClick`.
- Les plans conservent année, document original, aperçu, langue, taille, nom de fichier, provenance et liens externes.
- Les conteneurs sont plats, réservent toujours leur hauteur et n’ajoutent aucun marqueur ou placeholder présenté comme une donnée réelle.

## Couverture P12 — médias

- Le carrousel conserve filtres année/tag, photo active eager, miniatures lazy, alt, métadonnées, crédits, sources, carte, clavier et lightbox modale.
- Les galeries parc et ParkItem gardent leurs tabs, lots API, `loadMore`, états vide/chargement/erreur et liens de retour.
- Les listes vidéo conservent tabs, type, tag, créateur, détail, miniatures, métadonnées, tags et chargement incrémental.
- Le lecteur conserve embed/externe, précédent/suivant, créateur, fournisseur, tags, erreurs et partage ; aucune vidéo de démonstration n’est ajoutée.

## Couverture P13 — histoire

- Les routes profondes, resolvers, pagination, dates/précision, sujets, événements, images, sources et articles restent inchangés.
- Les snapshots gardent date complète et couverture ; le comparateur garde `fromYear`/`toYear`, ouverts, fermés, modifiés, incertains et stables.
- Les lignées gardent relations, cycles/troncatures, preuves, auteurs/éditeurs et sources externes.
- Les contenus rédigés ne sont ni tronqués ni remplacés ; la refonte se limite aux surfaces plates et à la géométrie.

## Couverture P14 — horaires, météo, prix, proximité et live

- Le calendrier conserve navigation mois, jour sélectionné, horaires, dernière admission, événements, fuseau, couverture et source officielle.
- La météo conserve séries min/max, probabilité et somme de précipitations, vent, graphe SVG, états indisponibles et comparaison historique différée.
- Les prix conservent entrées, crédits, abonnements, parking, web/guichet, validité, conditions, achats, historique, devise, source et fraîcheur.
- La proximité conserve distance/provenance. Le live conserve heure locale, refresh, offline, erreurs, files multiples, filtres, prévision, historique, fraîcheur, méthodologie et attributions.

## Couverture P15 — notes, classements, collections, watch et partage

- Les classements conservent catégories, filtres d’attractions, recherche, chargement incrémental, arbre, liste et routes de méthodologie versionnée.
- La méthodologie conserve seuils de preuve, score bayésien, composition du score parc, règles de publication, historique des versions et canal de signalement.
- Les notes restent éditables par demi-étoile selon les mêmes permissions ; les commentaires gardent rich text localisé, langues, rôles, édition, suppression, images gérées et états.
- Les collections gardent filtres, routes typées, suppression et états. Watch, alertes live et notifications gardent abonnements, pause, seuils, filtres, sources, pagination et signalement.
- Le partage conserve Web Share, copie, réseaux, QR avec ses états et signalement de modération. Les surfaces deviennent plates, à petits rayons et accents par filet.
- Preuve ciblée cumulée : 61 tests Node réussis ; aucune facade, route, API, permission ou règle de classement n’est modifiée.

## Couverture P16 — autres parcours publics

- Park Fit conserve le questionnaire conditionnel (date, localisation, 1–8 participants, âge, taille, accompagnement et préférences), les profils enregistrés, résultats sourcés et comparaison 2–4 parcs.
- Les passeports partagés conservent visites, récapitulatifs annuels, profil, classements, catégories de comparaison, limites, CTA et signalement. Les brouillons locaux gardent recherche, trajets, export, suppression et import comparé/consenti.
- Le catalogue constructeur et les fiches exploitant/fondateur/constructeur gardent recherche, rich text, faits, sites officiels, parcs et attractions liés.
- À propos, contact, confidentialité/cookies, versions, sitemap paginé, pages techniques riches, invitation trip et 404 gardent toutes leurs routes et actions.
- Les pages secondaires utilisent désormais largeur éditoriale, panneaux plats, petits rayons et filets d’accent, sans gradient décoratif ni nouvelle dépendance.
- Preuve ciblée cumulée : 67 tests Node réussis ; les 148 routes inventoriées restent présentes et les tests SEO/SSR complets restent réservés à P21.

## Couverture P17 — authentification, compte et profil

- La modale conserve Google, inscription et connexion ; les formulaires gardent soumission, autocomplétion, validation, confirmation, renvoi et récupération de mot de passe.
- Les routes `confirm-account`, `forgot-password` et `reset-password` restent dans le layout compte. Les 21 routes profil restent lazy et protégées par `authGuard`.
- Le profil conserve identité, langue, unités, avatar, déconnexion et accès aux collections, notifications, voyages et passeport. La suppression exige toujours email, mot de passe et confirmation explicite.
- Park Fit profil, passeport, statistiques, partage, visites, transitions, suppression et évaluations restent câblés aux mêmes facades et permissions.
- Les voyages conservent création, dates, jours, participants, wishlist, préférences, cohérence, activité, transition passeport, impression et export JSON.
- Preuve ciblée cumulée : 74 tests Node réussis ; aucune route, facade, port, endpoint ou validation auth/compte n’est modifié.

## Couverture P18 — administration intégrale

- Les 58 terminaux admin inventoriés conservent `languageGuard`, `authGuard`, `adminGuard` et `completeTranslationsGuard`, leurs composants lazy, templates et routes exactes.
- Dashboard, utilisateurs, parcs, zones, ParkItems, standalone, exploitants, fondateurs, constructeurs, pages techniques, images et vidéos restent couverts avec leurs listes et éditeurs.
- Field mode, graph upsert simple/bulk et tiroir contextuel gardent fichiers JSON, aperçu obligatoire, capacité d’application, erreurs, sélection et confirmations.
- Import Captain Coaster, diagnostics/workbench historiques, live operations/mappings, audit, SEO, météo, contact, partage, quality, passeport, Park Fit, classements, modération, événements, watch, trip et statistiques techniques restent présents.
- Les formulaires longs, huit panneaux de l’éditeur parc, horaires JSON, upload multiple, catégorisation en lot, chargement incrémental, tables et états responsive gardent leurs interactions.
- Le skin admin conserve sa densité propre et passe à des surfaces opaques, filets, petits rayons et indicateurs unis. Les 41 gradients/flous décoratifs admin ont été retirés sans changer les graphiques de données ni leurs valeurs.
- Preuve ciblée cumulée : 81 tests Node réussis ; toutes les feuilles admin sont sans gradient ni `backdrop-filter`, et aucun fichier métier/API/guard n’est modifié.

## Couverture P19 — états et composants transversaux

- La sélection initiale conserve huit langues, route localisée et persistance du choix. Son ancien pictogramme autonome a été remplacé par une marque Playground originale, conformément à l’exception de marque limitée à la navbar et au footer.
- Le consentement conserve acceptation optionnelle, refus aux cookies nécessaires, lien confidentialité, initialisation compatible SSR/hydratation et safe-area mobile.
- Dialogues, toasts et tooltips conservent rôles, régions live, fermeture, masque, clavier, focus et `aria-describedby`; le style statique du tooltip rejoint le système SCSS au lieu de rester injecté en ligne.
- Les états loading/empty/error restent annoncés ; le skeleton est neutralisé sous `prefers-reduced-motion`. Uploads, drag/drop, blocage pendant envoi et contrôle de saut de page sont protégés.
- L’éditeur Quill localisé conserve huit panneaux possibles, sanitization HTML, images gérées, alt/layout, readonly et scroll local de toolbar.
- Les dernières surfaces oubliées (résumé de contenu parc, cartes de zones et quick-create admin) adoptent panneaux opaques, filets et petits rayons.
- Après P19, les seuls gradients encore présents sont ceux du wordmark sanctuarisé, du skeleton fonctionnel et des fonds/marqueurs cartographiques. Aucun `backdrop-filter` ne reste dans l’UI.
- Preuve ciblée cumulée : 88 tests Node réussis, sans changement de service, facade, sanitization, consentement ni routage.

## Invariants fonctionnels

Dernière campagne complète verte : [CI `791dc2c46`](https://github.com/Ced59/AmusementParkProject/actions/runs/38889015354), Docker compris, 40 routes / 2 080 captures, 38 détails Fleury, 26 GET / 224 assertions HTTP, **307 contrôles hydratés et 732 captures additionnelles**, sans exception navigateur ni erreur d'interception. Marqueurs, 30 popups avec zoom non recouvert, CTA, vidéo, récit, calendrier et standalone huit langues passent les cas cités. L'exception P04.6 reste bornée à deux scripts et +3 302 octets JS (observé +3 280). Les snapshots/comparaisons/lignées non publiés dans la fixture Fleury, le nouveau complément compte et les scénarios de toutes les familles/revue exhaustive restent ouverts ; aucun PASS global déduit de ce run.

- Aucun changement d’API, DTO, port, guard, resolver, route, slug, query parameter ou règle de visibilité. Exception de présentation documentée : quatre messages de secours localisés dans trois facades auth, français et contrats fonctionnels conservés.
- Aucun changement d’authentification, rôle, pagination serveur, classement, note, collection, watch, passeport, Park Fit, trip ou workflow admin.
- Aucun remplacement de Leaflet, Quill, SSR, hydratation, `TransferState`, `@defer`, lazy loading, cache, SEO, canonical, hreflang ou JSON-LD.
- Aucun nombre, parc, ParkItem, prix, horaire, météo ou événement de la maquette n’est codé en dur.
- Les pages ParkItem restent des pages complètes ; les attractions standalone restent autonomes.
- Les écrans admin gardent leur densité et leurs opérations, sans entrer dans le bundle public initial.

