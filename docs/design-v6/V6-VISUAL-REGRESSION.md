# V6 — régression visuelle

## Ajustements du 16 octobre — primitives et pages pratiques

L'audit supplémentaire raccorde 108 déclarations de rayon dans 41 feuilles locales : 106 surfaces/champs/médias sur le token de surface et deux petits badges sur le token pilule. Les styles compilent en série, sans changement de dimensions ni de template. Un contrôle Node couvre l'ensemble des feuilles locales ; la revue du rendu complet et les états restent ouverts.

La prochaine recette hydratée capture aussi la vraie lightbox en 320 × 568, 390 × 640 et 1 366 × 480. Elle doit refuser un dialogue ou Fermer hors viewport, une photographie empiétant sur les crédits/vignettes et une image comprimée à moins de 80 px. Les captures et mesures seront conservées dans l'artefact ; ces trois nouveaux cas ne sont pas encore déclarés exécutés.

Les onglets segmentés utilisent des filets carrés, une bordure active sans ombre et un focus contrasté ; les rôles, comptes et touches restent ceux du composant existant. Les en-têtes repliables deviennent un bouton natif pleine largeur, sans réduire leur surface cliquable. Le dialogue partagé conserve sa composition et reçoit des commandes nommées et un focus limité au seul mode modal. Les nouveaux tests Angular et la revue des variantes restent à exécuter en CI.

Météo, horaires et tarifs réutilisent les mixins éditoriaux des sous-pages médias/cartes : suppression des gouttières imbriquées à toutes les largeurs, en-tête transparent à filets et titre plafonné à 57 px. Les styles de calendrier, graphiques et offres, leurs scrolls locaux et les marges basses mobiles sont conservés. Aucun template de données pratiques ni calcul n'est changé. Le contrôle de géométrie SSR couvre désormais aussi ces trois familles ; la comparaison réelle après ces modifications est **en attente de CI**, pas déclarée pixel-perfect.

La capture d'échec `004d01867/failed-hydrated-check.webp` montre la lightbox réelle en sombre à 1 366 px, avec photo publiée, crédits, contrôle Fermer et vignettes. Elle ne prouve pas la boucle clavier, qui échoue encore dans ce run. Le prochain test utilise Tab/Shift+Tab natifs et conserve un diagnostic dédié.

## Références

La capture Rhaegal 390 sombre de `a75861a30` révèle une photographie de fond estompée et un hero arrondi hérités. La correction P08 retire uniquement les décorations SVG/fonds, replace la vraie photo dans un cadre 16/10 distinct, met l'identité au-dessus et les actions sous le contenu, puis les liens précédent/suivant dans le flux. Notation, commentaires, taxonomy/zone, collections/watch, retours et médias restent liés aux mêmes données/événements. `sizes` et le preload existant partagent un unique gabarit de présentation ; aucune source photo ni priorité de chargement n'est changée. Le trail mobile revient à la ligne et conserve les noms complets avec des liens de 44 px, sans ellipsis ni scroll caché. Les 136 tests Node passent et le SCSS item compile (17 693 caractères avant le dernier ajustement de focus). Les mesures Angular aux 13 largeurs, deux thèmes et la revue après restent requises ; la standalone présente encore son ancien hero et n'est pas déclarée corrigée par ce lot.

Le prochain complément P20 retire `overflow-x: hidden` du `body` et mesure le maximum des largeurs du document et du corps : les débordements ne doivent pas être cachés par la page. Les captures statiques attendent la fin effective du changement de thème forcé ; elles ne prétendent plus mesurer un premier paint SSR. Celui-ci dispose d'une recette séparée sur les vrais scripts, dans six combinaisons système/préférence. Les 128 tests Node de présentation passent localement en série ; les captures Angular de ces changements restent à attester en CI.

La [campagne `a75861a30`](https://github.com/Ced59/AmusementParkProject/actions/runs/38769363185) recapture les ajustements mobiles. La capture Fleury 390 sombre a été inspectée : logo + titre sur une rangée, métadonnées/texte/notation en pleine largeur, huit actions visibles et libellés non tronqués. Les hôtes anonymes vides ne créent plus de faux espacements. L'accueil hydraté 1366 clair a également été inspecté face au PNG V6 fourni : split éditorial, échelle du titre, filets et panneau des quatre parcs dynamiques sont présents. Les logos réels remplacent la photo de démonstration faute de champ photo dans le modèle existant ; les données et contrôles réels ne sont pas supprimés pour imiter la maquette. Ces inspections ciblées ne constituent pas un verdict pixel-perfect global. Le premier échantillon 320 clair de l'entrée langue conserve une couleur de fond sombre pendant la transition de thème forcée par l'outillage ; les autres échantillons ont les couleurs attendues. Ce cas doit être distingué d'un vrai flash SSR par une recette de thème initial, sans modifier le moteur de thème ni masquer le constat.

La [campagne `6195d1a68`](https://github.com/Ced59/AmusementParkProject/actions/runs/38766079549) valide le nouvel en-tête sur les 13 largeurs et deux thèmes. Les captures Fleury 1366 clair / 390 sombre ont été inspectées : les actions sont regroupées et le logo n'est plus étalé en fond. Le mobile réserve encore inutilement toute une colonne au petit logo ; le prochain ajustement garde titre + logo sur une rangée, puis étend métadonnées, texte et notation à la largeur disponible. Les hôtes d'actions anonymes réellement vides ne doivent plus créer de faux espacements. Ces corrections restent à recapturer avant verdict final.

Revue de la [campagne `ae874422d`](https://github.com/Ced59/AmusementParkProject/actions/runs/38743956202) : 39 routes / 2 028 captures initiales, puis neuf captures hydratées anonymes. Le logo transparent de parc reste lisible sur son fond neutre. La fiche Fleury desktop révèle encore un en-tête trop haut et une colonne d'actions verticale héritée. Le correctif P06 en cours place ces actions sous l'identité, reprend l'échelle de titre 34–57 px (30–41 px au mobile), réserve un vrai cadre à la photographie et supprime le doublon de logo estompé en fond. Le logo réel reste dans son cadre ; l'image, ses sources, son chargement et son preload ne sont pas remplacés. Les actions supplémentaires du produit, absentes de la maquette, restent intégralement visibles et peuvent revenir à la ligne. Les nouvelles captures CI et leur revue restent requises ; aucun verdict pixel-perfect n'est acquis.

- Cible : HTML et captures du pack Playground V6.
- Avant : production V5.4.101 capturée le 14 octobre 2026 sans démarrer de serveur local.
- Après : captures de géométrie V6 produites sur fixture Chromium ; captures Angular réelles à valider dans l’environnement de revue final.

## Complément du 15 octobre : rendu initial Angular réel

`tools/design-v6/capture-public-ssr.mjs` invoque directement le moteur du bundle Angular compilé, sans ouvrir de serveur. Il n'autorise que des GET/HEAD anonymes vers l'API publique, retire cookies et autorisation, puis charge le HTML SSR et les assets du build dans Chromium. La CI de la branche V6 produit l'artefact `v6-public-ssr-initial`, avec 13 largeurs, deux thèmes, les huit accueils localisés et des routes publiques représentatives. Le rapport enregistre le SHA CI et le SHA source.

Ces captures prouvent uniquement le HTML initial et sa géométrie. Les scripts sont retirés des copies de capture : hydratation, `@defer`, interactions, focus et permissions ne sont donc pas validés par cet outil. Une seconde capture masque le panneau de consentement dans la copie pour inspecter le contenu ; elle est étiquetée comme telle et ne prétend pas démontrer le fonctionnement de son bouton. Le statut du moteur direct ne remplace pas les codes HTTP du serveur Express réel.

Un complément, exécuté uniquement en CI, conserve le vrai HTML et les bundles dans un navigateur isolé. Les assets viennent du build ; les lectures API anonymes utilisent les endpoints publics existants ; toutes les écritures réseau sont bloquées. Il vérifie l'hydratation de l'accueil, le clic « cookies nécessaires », la bascule de thème et une recherche réelle Fleury. Les verdicts de ce smoke sont enregistrés séparément sous `hydratedHome` ; ils ne valent pas validation des autres workflows. Les captures initiales incluent désormais Rhaegal, L'épée d'Odin, La Catapulte, La Crêperie et Hasenhorn Coaster visible, après vérification HTTP publique de leurs routes.

Le diagnostic local sur le build précédent, explicitement marqué `UNVERIFIED_LOCAL_BUILD`, a révélé que la règle mobile générique remplaçait les deux colonnes de la première carte hero par trois colonnes. Le titre était ainsi comprimé dans 1,7 rem. La règle à trois colonnes est désormais limitée aux autres cartes. Les 30 cas Chromium passent avec une assertion de largeur minimale du titre. La preuve du dernier build est attendue dans l'artefact CI, pas déduite de l'ancien build local.

L'[artefact de la CI 38694067574](https://github.com/Ced59/AmusementParkProject/actions/runs/38694067574), source `7637b029c`, contient 728 captures sur 14 routes (dont les huit langues de l'accueil). Les titres hero ont une largeur minimale de 218,42 px ; les 13 largeurs passent le contrôle d'overflow global. L'inspection a ensuite révélé cinq alias de thème non résolus sur `body`, corrigés dans `_tokens.scss`. La capture active désormais `prefers-reduced-motion: reduce` pour ne pas mesurer un mélange de couleurs au milieu de la transition de thème. Une nouvelle CI doit attester ces dernières corrections.

Le [run 38697638768](https://github.com/Ced59/AmusementParkProject/actions/runs/38697638768), source `e98d7f8fb`, réussit intégralement et produit [l'artefact SSR](https://github.com/Ced59/AmusementParkProject/actions/runs/38697638768/artifacts/11954423044) : 19 routes × 13 largeurs × 2 thèmes × 2 états de consentement, soit 988 captures initiales, plus les captures de l'accueil hydraté. Les assertions de largeur, de titre hero, d'alias de thème et de couleur active du footer passent. Ce volume ne remplace pas l'inspection humaine de chaque famille et état.

La revue a signalé le petit surtitre orange du choix de langue (environ 3,38:1 en clair). Il utilise désormais le token sémantique `--app-primary-strong`, protégé par un test ciblé. La prochaine campagne inclut aussi la route racine et relève les candidats de contraste sur fonds simples à 390/1366 px ; ce diagnostic ne constitue pas un verdict AA sur les images, fonds complexes ou tous les états interactifs.

La [CI ce3f7b800](https://github.com/Ced59/AmusementParkProject/actions/runs/38699592749) passe intégralement avec 20 routes / 1 040 captures initiales et le même smoke hydraté. Le diagnostic relève 163 occurrences candidates, et non 163 défauts confirmés : une partie concerne des descendants de `details` fermés, non peints, dont Chromium conserve les anciens styles/bounds. Le collecteur exclut maintenant explicitement ces descendants sans exclure leur `summary` visible. Les défauts visibles relevés portent sur la navigation active desktop, les kickers/chips primaires, le rose sémantique, les libellés discrets, la page 404, le contact et les liens de notation ; ils font l'objet de corrections et tests de contraste dédiés.

La campagne suivante ajoute les sous-pages Fleury (inventaire, zones, carte, images, vidéos, météo, horaires, prix, commentaires et histoire), l'histoire standalone visible, les classements/méthodologie, constructeurs, technique et versions : 36 routes prévues. Elle refuse un composant 404 inattendu et enregistre le titre initial de chaque route. Les blocs différés et cartes non hydratées restent explicitement hors validation interactive. L'inventaire complet est régénéré dans l'artefact CI ; les tailles SCSS utilisent des fins de ligne normalisées pour être reproductibles entre Windows et Linux.

Le [run 38701602727](https://github.com/Ced59/AmusementParkProject/actions/runs/38701602727) valide les tests et budgets, puis capture 24 routes / 1 248 images avant de refuser le titre manquant des tarifs Fleury. Diagnostic public : l'API tarifs renvoie réellement 404 et la production V5 renvoie déjà 404 avec le H3 « Aucun tarif disponible ». L'outil conserve l'exigence H1 pour les fiches remplies et n'accepte cet état vide que pour cette fixture, avec preuve API 404 et composant vide réel. Un échec futur dépose désormais le HTML diagnostique. La campagne ajoute aussi le parc Efteling et ses tarifs (API publique 200, 4 billets, 3 abonnements, 1 parking au 15 octobre), soit 38 routes prévues, sans donnée de démonstration ni publication.

Sur les 24 routes mesurées, les contrastes précédents ne réapparaissent pas. Il reste cinq occurrences candidates correspondant à deux composants : le badge « Aujourd'hui » du calendrier (2,82:1 en clair) et le compte sélectionné de galerie (2,42:1 en clair / 3,86:1 en sombre). Les premiers plans deviennent respectivement `--app-on-gold` et `--app-heading`, avec tests sur les deux thèmes et les fonds composités. Les alias de catégories sont aussi résolus sur `body` plutôt que `:root`, pour ne pas figer les accents clairs lors d'une bascule sombre. La prochaine CI doit attester ces changements.

La [CI 2675e3a8b](https://github.com/Ced59/AmusementParkProject/actions/runs/38703888781) réussit intégralement : [38 routes / 1 976 captures initiales](https://github.com/Ced59/AmusementParkProject/actions/runs/38703888781/artifacts/11957937389), trois captures hydratées et quatre contrôles d'accueil. Les cas tarifs vide/rempli sont distincts et les deux contrastes précédents ne réapparaissent pas. Les nouvelles pages révèlent 24 occurrences candidates sur les liens/libellés de tarifs, classements, méthodologie, constructeurs, récit historique et une légende photo : elles sont corrigées via les premiers plans sémantiques existants, sans changer données ou textes.

Le prochain smoke hydraté ajoute pagination page 2 réellement lue par API, tailles 20/10/5 avec retour page 1, recherche sans résultat, erreur 503 injectée uniquement dans le navigateur isolé, ouverture/fermeture et bounds de la modale anonyme, clic de résultat vers la vraie fiche parc et retour navigateur. Aucun formulaire d'authentification n'est soumis ; Google reste hors validation et ses requêtes externes sont bloquées. Les captures d'états simulés sont étiquetées comme recette, jamais comme données servies en production.

La campagne `5f5de42e3` produit 1 976 captures initiales et ne relève plus de candidat insuffisant dans son échantillon de contraste 390/1366 sur fonds simples. Quinze scénarios hydratés passent avant une erreur d'interception au retour arrière. La campagne `adcd267f1` passe les seize scénarios, y compris le retour, puis échoue pendant le nettoyage du navigateur : le document était détruit avant la fin des réponses interceptées. Le nettoyage bloque désormais les nouveaux accès réseau puis attend les réponses en cours, sans relâcher l'interdiction d'écriture. Ces campagnes ne sont pas déclarées intégralement vertes.

La comparaison des règles de la référence révèle encore un écart de fidélité : le titre à 1366 px était de 84,69 px au lieu de 72,40 px, la photo principale pouvait atteindre 240 px au lieu de 205 px, et les polices éditoriales conservaient les familles V5. Corrections : tokens Arial/system-ui de la maquette, police historique isolée au wordmark, titre/split/gap/marges et image de 205 px réalignés. Les gouttières du shell rejoignent 48/30/28 px. Le palier de reflow reste 960/680 selon le dépôt, au lieu de multiplier les 920/650 de la maquette ; les CTA se replient à 320 pour préserver leur texte. Les chiffres cités sont calculés depuis le CSS, pas présentés comme des captures inspectées. Les mesures navigateur et la revue des grandes enveloppes après correction restent requises.

L'inspection de la capture Angular `5f5de42e3` face au PNG V6 fourni confirme aussi le contour orange V5 de « parfait. ». Une règle globale de thème clair, plus spécifique, écrasait le style du composant : elle est retirée plutôt que contrebalancée par une nouvelle surcharge. La recette mesure désormais le contour de 1,4 px en encre sur desktop et le texte plein sans contour sur mobile, dans les deux thèmes. Les images du hero utilisent les logos réels disponibles dans le modèle existant ; elles ne sont pas déclarées équivalentes à une photographie de démonstration et aucun champ/endpoint n'est ajouté pour contourner cette différence de données.

## Baseline V5 enregistrée

Les fichiers sont sous `evidence/baseline-v5/`.

| Famille | Clair | Sombre | Mobile | Desktop |
|---|---|---|---|---|
| Accueil FR | oui | oui | 390 | 1366 |
| Accueil EN/DE/NL/IT/ES/PT/PL | oui | — | 390 | — |
| Accueil FR tablette | oui | oui | — | 768 |
| Catalogue parcs FR | — | oui | 390 | 1366 |
| Parc Le Fleury FR | oui | oui | 390 | 1366 |
| ParkItem L’épée d’Odin FR | — | oui | 390 | 1366 |
| Auth anonyme FR | — | oui | 390 | — |
| 404 FR | — | oui | 390 | 1366 |

Les captures pleine page mobiles de l’accueil contiennent des zones réservées par les blocs différés avant leur réalisation complète. Elles prouvent la géométrie initiale mais ne remplacent pas les captures segmentées des états chargés à P20.

## Preuves responsive V6 P20

Le dossier `evidence/responsive-v6/` contient 30 captures WebP et `responsive-report.json`. Chaque largeur contractuelle est mesurée en clair et sombre, avec deux cas supplémentaires de faible hauteur/paysage. Verdict automatisé : 30/30 sans violation de largeur, bounds, navigation fixe, cible de notation ou réservation média/carte.

| Échantillon inspecté | Verdict |
|---|---|
| 320 × 1200 sombre | PASS — reflow mono-colonne, contenu long contenu, consentement au-dessus de la bottom-nav |
| 390 × 568 clair | PASS — faible hauteur, actions de consentement accessibles, navigation réservée |
| 1366 × 1200 clair | PASS — composition dense, grilles et actions contenues |
| Matrice complète clair/sombre | PASS AUTOMATISÉ — 13 largeurs + 2 hauteurs contraintes, 0 violation |

La fixture compile les SCSS de production sans mocks métier ni faux routeur et ne nécessite aucun serveur. Ces images ne prétendent pas remplacer la revue des données réelles : home, Le Fleury, ParkItems, standalone visible, cartes, médias, compte et admin devront être approuvés sur la branche déployée pour revue avant le feu vert de fusion P22.

## Verdicts à renseigner après migration

Pour chaque famille : avant V5, cible V6, après Angular, léger, chargé, erreur, sans média, titre long, clair, sombre, mobile, tablette, desktop, zoom 200 %, focus clavier et mouvement réduit.

Tolérances : les alignements invariants dépassant 3 px et les blocs stables différant de plus de 5 % sont corrigés ou justifiés. Les données réelles, le wordmark protégé, le slogan et les contrôles produit absents de la maquette sont des écarts attendus.

## Écart contrôlé P03

Le pictogramme carré historique a été retiré du header et du footer, conformément à la clarification de marque. Le wordmark typographique original reste inchangé dans les deux emplacements. Le shell adopte la largeur de 1160 px, les filets, surfaces opaques et angles faibles de Playground ; les captures Angular finales restent dues à P20 dans l’environnement de recette.

La comparaison de la capture réelle `fd54cfcc3` à `V6_home_1366.png` montre que les liens étaient encore comprimés entre la marque et les utilitaires. Le shell est repris en deux niveaux : masthead de 85 px, puis navigation pleine largeur bordée de filets d'encre de 2 px, libellés 12 px et état actif orange à texte sombre contrasté. Le mobile conserve ses contrôles existants sur une ligne distincte et ses six accès de bas de page ; aucun faux menu ni bandeau de démonstration n'est importé. Le drapeau compact s'accompagne du code de langue, sans modifier les libellés accessibles ; la bascule de thème perd son disque et sa translation décorative. Les offsets d'ancres suivent cette hauteur sans modifier le header admin indépendant. Les captures CI vérifient la séparation des lignes et leur largeur réelle ; validation après build encore requise.

## Transposition P04

L’accueil suit les enveloppes de la référence 1366/390 : manifeste et statistiques à gauche, quatre emplacements API à droite, recherche pleine largeur, puis vedettes et nouveautés. Les photos de démonstration sont remplacées par le média réel disponible via le service image ; les valeurs, parcs et contenus de la maquette ne sont jamais repris comme fixtures de production. La capture après Angular demeure bloquante à P20.

La recherche est réalignée sur `.v6-search-controls` : barre de champs blanche à bordure d'encre, trois colonnes desktop (requête / catégorie / action), puis requête pleine largeur et catégorie/action côte à côte au mobile. Le conteneur encadré et le kicker en pastille sont retirés uniquement pour l'accueil ; les panneaux à filtres variables des autres pages conservent leur composition. Le bouton d'effacement reste visible, distinct et utilisable au clavier ; labels, autocomplétion, état compact après recherche, chargement et événements ne changent pas. Les positions réelles des champs seront contrôlées aux treize largeurs dans la prochaine CI.

## Transposition P07–P08

L’inventaire et la fiche ParkItem utilisent les mêmes surfaces plates, filets visibles et petits rayons que la fiche parc, tout en conservant la densité des spécifications, conditions d’accès et facettes. Les rail/cards n’emploient ni gradient, ni masque décoratif, ni ombre de carte. La navigation latérale garde uniquement sa translation fonctionnelle de centrage vertical. Les captures chargées, sans média, non-attraction, clair/sombre et 390/1366 restent dues à P20.

Le diagnostic CI `fd54cfcc3` signale le filtre actif « Tous » de la carte d'inventaire : 2,78:1 en clair et 4,23:1 en sombre. Une règle spécifique tardive rétablissait l'orange après la couleur d'encre de l'état actif ; cette surcharge est supprimée. Les tests protègent désormais la cascade et le contraste de l'encre sur la surface teintée des deux thèmes. La capture après correction reste à vérifier.

## Transposition P09–P10

La fiche autonome reprend la largeur éditoriale de 1160 px, un hero à filet orange et des panneaux plats, sans gradient, masque ni ombre. Les listes et détails de zones reprennent la même géométrie avec accents lime et une carte Leaflet inchangée. Les captures données réelles (visible/masqué, avec/sans zone, sans coordonnées) restent dues à P20.

## Transposition P11–P14

Cartes, plans, médias, histoire et panneaux pratiques emploient désormais des surfaces opaques, filets visibles, accents fonctionnels et rayons faibles. Galeries et cartes gardent leurs hauteurs réservées et défilements locaux ; les graphiques météo/prix et la timeline gardent leur information sans effets décoratifs. Les captures avec données réelles, erreur réseau, grande galerie, contenus historiques longs et calendriers restent dues à P20.

## Transposition P15

Classements, méthodologie, commentaires, collections, notifications et partage adoptent les panneaux éditoriaux plats de Playground, des filets d’accent par domaine et une largeur maîtrisée. Les gradients décoratifs et translations de survol ont été retirés sans altérer les étoiles, filtres, formulaires, actions ou états. Les captures anonyme/connecté, vide/chargé/erreur, clair/sombre et mobile/desktop restent dues à P20.

## Transposition P16

Park Fit, passeports publics/locaux, références métier et pages secondaires ont été ramenés au même vocabulaire éditorial : largeur 1160 px quand adaptée, surfaces opaques, filets visibles et rayons faibles. Les schémas techniques conservent leurs relations avec des traits unis ; les contenus variables et longs restent intacts. Les captures de parcours, formulaires, tableaux comparatifs et états d’erreur restent dues à P20.

## Transposition P17

La modale d’authentification, les écrans de récupération, le profil, le passeport et les voyages utilisent des panneaux opaques, de petits rayons et des filets d’accent sky/lime/orange. Les gradients et flous décoratifs ont été retirés, sans modifier les champs, validations, dialogues destructifs, barres d’actions persistantes ni états protégés. Les captures anonyme/connecté/session expirée, clair/sombre et mobile/desktop restent dues à P20.

## Transposition P18

L’administration reprend Playground dans une déclinaison dense : largeur 1480 px du shell conservée, champs et actions compacts, tables structurées, surfaces opaques, filets visibles et petits rayons. Les indicateurs chiffrés et barres restent lisibles avec une couleur unie ; les gradients, flous et élévations décoratives ont été retirés. Les cartes-tableaux se replient en couples libellé/valeur sous 760 px, tandis que les JSON editors, uploads, diagnostics et formulaires longs conservent leur densité et leur scroll local. Les captures avec compte admin et données de recette restent dues à P20/P21.

## Transposition P19

Les overlays et états rares suivent désormais les mêmes panneaux plats : consentement, dialogs, toast, tooltip, upload, quick-create, états de page et rich text. La page racine de choix de langue emploie une marque APF Playground distincte et non le pictogramme historique, avec une grille 2→1 colonnes. Les seuls gradients conservés ont un rôle contractuel ou fonctionnel documenté : lettrage `.fun` du wordmark, shimmer de skeleton neutralisé en mouvement réduit et rendu cartographique. Les captures focus, erreur, upload, consentement et texte long restent dues à P20.

## Complément P09 — identité autonome et photographie

La capture Hasenhorn 390 px du run `483818796` confirme l'ancien fond photographique estompé et une enveloppe trop haute. Le correctif replace kicker, titre, types/statut et trois liens réels dans un en-tête transparent ; la photographie publique existante rejoint le contenu, distincte du texte, dans le cadre de 340 px de la maquette standalone. La grille 1,45/0,8 et l'écart de 26 px reprennent sa composition ; les données techniques rejoignent la colonne latérale devant les mêmes tarifs, horaires, météo et carte conditionnelle. Au mobile, la grille se replie, les actions restent tactiles et les valeurs se réorganisent sans troncature.

Toutes les expressions de données, clés traduites, sources, permissions et appels réseau sont conservés. Aucun champ chiffré, avertissement de démonstration ou lien de faux routeur n'est copié ; Pendolino et ses médias masqués ne sont pas publiés. Les `sizes` décrivent seulement le cadre réel. Deux tests supplémentaires protègent composition et treize largeurs ; **159 tests ciblés** passent avec les contrôles existants de contraste/thème, et ce SCSS compile seul (4 268 caractères). Les captures Angular après correction sont encore requises : ce paragraphe ne constitue pas leur approbation.

## Complément P12 — enveloppes médias et filtres lisibles

Les captures Rhaegal sombre et Hasenhorn clair à 390 px du [run `a6bbfb21d`](https://github.com/Ced59/AmusementParkProject/actions/runs/38818065272) sont maintenant inspectées : photographie distincte, titre lisible, actions repliées et fil mobile visible. Les gardes de géométrie passent aux treize largeurs et deux thèmes pour ces familles. Cela ne solde ni les autres états/permissions ni la revue comparative exhaustive ; les cadres de sous-pages médias ci-dessous restent à contrôler sur leur nouveau build.

La capture de galerie Fleury à 390 px du run `483818796` présente encore un en-tête encadré/arrondi et des libellés d'année comprimés. Deux mixins dans `_mixins.scss`, réutilisés directement par galeries parc/item, liste vidéo et page carte, suppriment les marges intérieures doublées et définissent un en-tête transparent à filets, sans nouvelle couche CSS. Les titres de section de `_surfaces.scss` retrouvent la casse réelle, l'espacement nul et l'échelle V6 ; les H1 publics sont distingués des H2, sans agrandir les titres admin.

Les filtres années/tags gardent leur scroll local, leurs comptes et leurs actions, avec cellules d'au moins 11 rem, texte repliable et cibles de 44 px ; aucun masquage global d'overflow n'est réintroduit. La recette SSR relève les alignements du shell, les surfaces et largeurs utiles des labels. **164 contrôles ciblés passent** ; les six feuilles concernées compilent seules. La nouvelle campagne CI doit encore confirmer ces géométries et fournir les captures après correction. Aucun template ou appel de média/carte n'est modifié par cet incrément.

Le run `c4b765277` révèle une erreur d'import de ces quatre composants lors de la compilation Angular des tests : le nom court `mixins` était résolu seulement par le `loadPaths` du contrôle Sass ciblé. Les imports sont corrigés vers le chemin relatif existant `../../../../../styles/mixins`, sans modifier la configuration Angular ; les quatre feuilles recompilent sans `loadPaths`, et un test protège cette résolution. Les **165 contrôles ciblés** passent. L'échec CI est conservé ; la compilation Angular suivante doit encore attester le correctif.

