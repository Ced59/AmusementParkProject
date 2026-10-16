# V6 — régression visuelle

## Second contraste profil — `f975d9bc4`

Le [run `38903181087`](https://github.com/Ced59/AmusementParkProject/actions/runs/38903181087) passe 2 777 tests Angular et atteint le profil clair 1 366 px. La capture `v6_standard_fr_account-profile_1366_light.webp` est inspectée : en-tête, nom long et panneaux restent dans le document ; le kicker bleu mobile précédemment refusé ne bloque plus. Le kicker « Passeport de visites » de 11,52 px mesure **4,25:1** et arrête la recette. Aucune exception ni fixture manquante. L'alias or et le cas numérique de sa composition sont corrigés ; un garde de 25 compositions complète aussi le lime. **Ce run reste FAIL ; nouveau rendu requis.**

## Défaut profil détecté — `7115bb901`

L'[artefact `12046276843`](https://github.com/Ced59/AmusementParkProject/actions/runs/38900388260/artifacts/12046276843) conserve le FAIL original : `v6_standard_fr_account-profile_390_light.webp` et son rapport. La capture inspectée montre nom long, navigation et actions repliés dans le viewport ; le document ne déborde pas. Deux kickers de 11,52 px (« Radar personnel » et « Voyages à préparer ») sont mesurés à **4,25:1**, sous le seuil de 4,5:1. Aucun échec de fixture ni exception navigateur. La couleur de texte est corrigée via l'alias existant et un test protège ce fond exact ; **nouvelle CI/capture requise**, sans transformer rétroactivement ce FAIL en PASS.

## Profil compilé — premier PASS `fc742b010`

Les **104 captures compte master/V6**, identités standard/incomplètes, passent les 13 largeurs et deux thèmes dans [l'artefact `12043431107`](https://github.com/Ced59/AmusementParkProject/actions/runs/38892315894/artifacts/12043431107). Inspection de `v6_standard_account-profile_390_dark.webp` : nom long sur plusieurs lignes, avatar sans média, email et navigation mobile dans le viewport. Inspection de `v6_incomplete_account-profile_1366_light.webp` : fallback email visible, panneau plat, sections/actions présentes. Ce sont des données synthétiques réservées à la recette CI, jamais un compte ou texte de production. Éditeur détaillé, avertissement suppression, listes compte et session expirée attendent leur nouvelle campagne. Approbation propriétaire et couverture exhaustive restent ouvertes.

## Dernier rendu attesté — `791dc2c46`

La [campagne complète `38889015354`, artefact `12041197882`](https://github.com/Ced59/AmusementParkProject/actions/runs/38889015354/artifacts/12041197882) passe 2 080 captures SSR et **732 captures hydratées additionnelles** : 30 popups carte, 208 détails et 208 panneaux de données standalone (huit langues × 13 largeurs × deux thèmes), puis les cas vidéos, récit, sources, calendrier, météo, historique standalone et récupération de mot de passe. Les trois lightboxes 320 × 568, 390 × 640 et 1 366 × 480 gardent une image de 208/303/117 px et Fermer au premier plan. Les 30 popups gardent titre, actions, Fermer et les deux commandes de zoom accessibles ; le défaut de recouvrement cité dans les campagnes antérieures est soldé dans ces cas précis.

L'inspection de `fr_home_hydrated_1366.webp` confirme les gouttières, le filet de séparation, les CTA réels et le wordmark original dans cet échantillon sombre. Les parcs et chiffres sont ceux de la population API du run, pas des valeurs intégrées au produit. `pl_standalone-data_390_dark.webp` montre des données techniques, météo et adresse polonaises lisibles, avec retour à la ligne et navigation mobile. Ces deux inspections ne valent pas approbation exhaustive/pixel-perfect. Les tuiles externes et lecteurs fournisseurs bloqués par l'isolation ne sont pas déclarés testés.

Les variantes compte compilées standard/incomplètes ajoutées après ce run restent **à exécuter**. Revue propriétaire, zoom réel 200 %, SR et autres familles/états restent ouverts.

La revue source du tableau standalone admin découvre encore des fonds cyan/orange codés avec les anciennes valeurs. Ils sont raccordés aux tokens sémantiques existants, avec panneaux/champs opaques, focus contrasté et texte d'erreur `--app-primary-strong` plutôt que l'orange décoratif clair. Cette feuille SCSS compile seule (8 768 octets avant le dernier raccordement de deux couleurs) ; rendu admin et test Angular du nouveau bouton clavier restent à attester, pas déclarés visuellement approuvés.

## Popup normal — synchronisation des bounds à attester

La campagne `e6a531fe6` refuse encore la géométrie à 320 px après les attentes normales. Son [artefact `12039486997`](https://github.com/Ced59/AmusementParkProject/actions/runs/38884665674/artifacts/12039486997) montre cette fois le cadre entièrement dans la carte (y 433 → 626,9 dans y 382,9 → 734,9), mais le titre recouvert par les contrôles de zoom. La synchronisation seule n'est donc pas présentée comme la cause résolue.

Le correctif réutilise une factory pure sous `ui/maps/leaflet` pour réserver les 80 px des deux boutons natifs et de leur marge, via **`autoPanPaddingTopLeft: [5, 80]`**. C'est une option de placement présentationnel explicitement documentée, appliquée aux deux adaptateurs ; activation de pan, zoom, drag, fermeture, coordonnées, ancres, liens et événements restent inchangés. Le contenu conserve un scroll local avec maximum 224 px pour tenir dans la carte mobile. Aucun décalage forcé du popup, masquage des contrôles ou changement de `z-index` n'est utilisé. Tests Angular de factory/liaison et nouvelles captures requis ; aucun succès annoncé sur la seule compilation.

Le [run `527bbabf3`, artefact `12038434133`](https://github.com/Ced59/AmusementParkProject/actions/runs/38883443563/artifacts/12038434133) valide à nouveau les trois lightboxes (photos 208/303/117 px, Fermer au premier plan), puis constate le dépassement de popup à 320 px. Le diagnostic DOM conserve 194 px de hauteur et un `bottom: -7px` natif ; aucun champ privé n'est utilisé. La relecture identifie que le retour du nombre de marqueurs ne prouve pas la fin des cycles de bounds existants (0 puis 120 ms) ni de l'auto-pan du popup. La recette normale attend désormais trois observations stables du proxy **et** du pane, sans classe native pan/zoom, après reset puis ouverture. Aucun délai produit, option Leaflet, interaction ou test de destruction rapide n'est changé. Le défaut reste ouvert jusqu'au nouveau rendu, sans attribution définitive prématurée.

## Mesure transitoire de lightbox — `348589406`

La [CI `38882167904`, artefact `12037963149`](https://github.com/Ced59/AmusementParkProject/actions/runs/38882167904/artifacts/12037963149) s'arrête avant la carte : la première mesure 320 × 568 conserve une hauteur de dialogue de 1 024 px, alors que la capture peinte juste après montre déjà le dialogue borné et la photo d'environ 200 px. Cet écart mesure/paint après l'émulation du viewport interdit d'attribuer le défaut au CSS sans vérification. La recette attend désormais deux mesures identiques et valides du vrai dialogue, avec les mêmes bounds, minimum d'image et hit-test Fermer ; en cas de timeout elle conserve la dernière mesure et échoue. Aucun style de produit ni assertion de géométrie n'est affaibli pour ce cas.

## Popup après correction — preuve `df03114c9`

La campagne suivante `a9cbcbe93` recadre effectivement le document, mais révèle un second défaut distinct à 320 px : le wrapper du popup dépasse de 6,9 px le haut de la carte (y 376 contre 382,9), alors que ses textes et liens restent visibles. L'assertion de bounds n'est pas assouplie. Le prochain diagnostic conserve les dimensions DOM/CSS publiques de la bulle et du conteneur pour identifier l'écart, sans lire ni modifier les champs privés Leaflet. Les vidéos/histoires restent non exécutées dans cette campagne.

La [CI `38880212474`, artefact `12037800624`](https://github.com/Ced59/AmusementParkProject/actions/runs/38880212474/artifacts/12037800624) valide les 13 largeurs du popup sombre à hauteur 1 200 px, mais refuse ensuite la capture 390 × 640. La géométrie distingue le défaut : le popup tient dans la carte (y −44,6 → 149,3 dans y −50,7 → 301,3), mais **la carte entière a défilé au-dessus du viewport** après le reflow. L'inspection de la capture d'échec confirme ce décalage du document, pas une nouvelle troncature interne du popup.

Le recadrage de recette utilise désormais la position réelle de la carte et la place prise par la navigation mobile, puis défile uniquement le document après le reflow. Il ne rouvre pas le popup, ne déplace pas la carte Leaflet et conserve les assertions de titre, Fermer et actions au premier plan. Trois cas purs couvrent le décalage, le cadrage déjà correct et la faible hauteur. **La nouvelle capture reste requise ; la campagne précédente n'est pas déclarée verte.**

## Popup Leaflet — défaut résiduel observé, correction à recapturer

La capture hydratée `fr_map-marker-popup_390_dark.webp` du [run `cd1b7dd28`, artefact `12032732139`](https://github.com/Ced59/AmusementParkProject/actions/runs/38869945426/artifacts/12032732139) confirme les nouveaux marqueurs plats. Elle révèle cependant une bulle encore blanche/arrondie V5 et le titre hors des bounds après redimensionnement. Les styles réutilisent désormais les tokens de champ/texte/boutons, des coins 2 px, une largeur bornée et un défilement vertical local pour le contenu très long. Les actions gardent leurs liens et des cibles de 44 px ; Fermer reste un contrôle natif Leaflet d'au moins 32 px au réglage de base.

La mise à jour de taille existante recalcule seulement les popups ouvertes, sans rouvrir celles fermées ni changer les options de zoom/drag/autoPan ou les données. Le focus et les listeners du même lien sont conservés après reconstruction du contenu. Le contrôle suivant refuse titre/surface coupés et actions/Fermer recouverts, y compris aux hauteurs réduites. **Correctif et test Angular à attester, aucune disparition du défaut annoncée avant capture.**

## Fidélité des deux CTA d'accueil

La relecture du vrai CSS de la maquette confirme `12px / 850`, capitales, approche positive `0.015em`, bordure 2 px, padding 10/14 px et mobile 11 px. Les seuls CTA du hero sont réalignés sur ces valeurs dans le SCSS existant, avec `0.75rem / 0.6875rem` pour respecter une taille racine agrandie ; les routes réelles **parcs** et **à propos**, traductions et événements ne sont pas remplacés par ceux du prototype. La hauteur minimale reste **44 px** au lieu des 40 px de la maquette, exception de présentation motivée par la cible tactile/accessibilité, à montrer lors de la revue finale. **Capture du prochain build et mesure P04.6 bornée encore requises.**

## Marqueurs Leaflet réels — styles V5 résiduels identifiés

La capture hydratée du run `2335fc043` révèle les pins et clusters brillants encore dans le SCSS global `_maps.scss`, non visibles dans les réservations SSR de carte. Le lot suivant remplace leurs dégradés, grandes ombres, rondelles et mouvement de survol par des têtes quasi carrées, aplat de champ, filet/couleur de catégorie et contour au focus/survol. Le halo d'un pixel est uniquement un contour de lisibilité sur les tuiles. Les 36 couleurs de la factory UI deviennent des références aux tokens existants, sans palette parallèle. Le pictogramme de marque séparé des marqueurs parc/site est remplacé par leur glyph 🎢 déjà défini ; aucun fichier asset n'est supprimé, aucun nouveau téléchargement n'est ajouté. Les wordmarks navbar/footer ne sont pas touchés.

Dimensions, ancrages/popup, catégories, glyphs des lieux, labels et seuils de clusters sont protégés par les nouveaux tests. Les 22 contrôles Node cartes/tokens et la compilation SCSS globale ciblée passent (9 713 caractères avant retrait du style logo désormais inutilisé). **Rendu hydraté et 38 nouveaux tests Vitest à attester en CI.**

## Calendrier à 320 px — lisibilité après le premier correctif

La capture `fr_opening-hours-selected_320_dark_scroll-end.webp` du run `f5ddfd8a4` a ensuite été inspectée : les plages sont regroupées sur deux lignes à la séparation, le badge complet tient sur deux lignes et la dernière colonne est atteignable avec une scrollbar visible. La compression précédente est corrigée dans cet échantillon ; ce n'est pas une validation des huit langues ou du zoom. Le run reste globalement en échec sur la navigation document du sandbox, indépendante de cette capture.

La [CI `754cce98c`, artefact `12029833735`](https://github.com/Ced59/AmusementParkProject/actions/runs/38863174263/artifacts/12029833735) passe 26 géométries internes du calendrier (31 vrais jours et un badge Aujourd'hui chacune), ainsi que 26 captures météo historique. L'inspection du calendrier **320 px sombre** confirme la disparition du chevauchement, mais montre encore des heures morcelées et le badge sur quatre lignes : la lisibilité n'est pas validée par le seul containment.

Le complément conserve des cases d'au moins 44 px et un tableau de largeur minimale 26 rem, dans une région nommée focalisable avec défilement horizontal **local**, scrollbar contrastée et aucune dissimulation du document. La recette exige l'accès effectif à la dernière colonne et capture séparément la fin du scroll. Les horaires mobiles passent de 9,9 à 11,2 px ; dates, données et sélection restent inchangées. **Nouvelle capture et inspection requises.** Le SCSS ciblé compile (12 424 caractères), les 40 tests pratiques/cartes/médias/histoire passent et les 21 templates comparés ne perdent aucun contrat (1 889 → 1 931).

## Calendrier mobile — défaut découvert après la CI verte `da81bf4ab`

La [campagne `38859824025`, artefact `12028973687`](https://github.com/Ced59/AmusementParkProject/actions/runs/38859824025/artifacts/12028973687) passe les contrôles fonctionnels et la largeur du document. L'inspection de `fr_opening-hours-selected_390_dark.webp` révèle pourtant le badge Aujourd'hui débordant dans la case voisine. Le contrôle global de largeur ne suffisait donc pas. Le correctif borne le track de grille à `minmax(0, 1fr)`, laisse revenir le texte complet à la ligne et supprime sa réduction mobile à 7–8 px ainsi que les rayons fixes 6/7 px. Les jours, horaires, états et handlers restent inchangés. La recette suivante vérifie les rectangles et scroll internes de chaque case aux 13 largeurs, deux thèmes, et exige que le vrai mois comporte effectivement le badge testé. **Correctif source testé ; rendu navigateur à attester, défaut non déclaré soldé.**

La météo historique réelle à 390 px sombre a aussi été inspectée : matrice en défilement local, attribution et partage lisibles. Ce constat ciblé ne valide pas les huit langues, le zoom ou tous les états pratiques.

Les captures statiques Fleury vidéos, histoire et carte à 390 px sombre de ce même artefact ont été inspectées : en-têtes, filtres, sources et navigation restent dans le document, sans chevauchement visible dans l'échantillon. La carte initiale est une réservation SSR, pas une preuve de tuiles/gestes Leaflet ; la vidéo initiale ne prouve pas la lecture du fournisseur. Les prochains scénarios hydratés ciblent justement ces états et le récit complet.

Les cinq déclarations orphelines `--hero-stat-color` sont retirées : aucune lecture de cette variable n'existait dans `src/`. Les chiffres de l'accueil conservent leur premier plan et leurs sources ; cette suppression de CSS mort n'élargit pas l'exception de poids JS acceptée.

## Lightbox corrigée — validation `579060704`

Les trois captures réelles passent sans chevauchement ni bouton recouvert : **320 × 568**, **390 × 640** et **1 366 × 480**. `elementFromPoint` confirme Fermer au premier plan dans les trois cas. Les hauteurs de l'image sont respectivement **208, 303 et 117 px** ; les crédits/vignettes restent dans leur zone accessible. La capture 320 × 568 sombre est inspectée : la navigation ne couvre plus le dialogue, le titre revient à la ligne et la photographie ne chevauche plus sa légende. [Artefact 12028425423](https://github.com/Ced59/AmusementParkProject/actions/runs/38857089021/artifacts/12028425423). Les contrôles clavier/fermeture/restauration et 124 contrôles hydratés passent. Il s'agit d'une validation ciblée, pas d'une approbation visuelle exhaustive P22.

## Défaut réel de lightbox — CI `47c850246`

La [capture 320 × 568 sombre, artefact 12018054610](https://github.com/Ced59/AmusementParkProject/actions/runs/38833796275/artifacts/12018054610) montre une image de 329 px qui dépasse de 46 px sur le footer. La navigation fixe recouvre aussi le dialogue, enfermé dans le stacking context du contenu. Le correctif réserve une hauteur de dialogue liée au viewport dynamique, donne à l'image la place restante dans la grille et borne les crédits/vignettes à une zone de défilement locale. Le contenu principal rejoint sa couche modale existante uniquement lorsqu'une lightbox est présente (`:has`), sans changer d'état Angular ni de comportement de navigation. Le prochain contrôle vérifie aussi que le bouton Fermer est réellement au premier plan avec `elementFromPoint`. Les trois paliers et la fermeture clavier doivent repasser : **correctif non encore validé en navigateur**.

L'inventaire corrigé découvre également les deux blocs CSS inline : rayon du panneau de navigation admin raccordé au token existant, pilule de position conservée et indicateur d'upload de l'éditeur respectant `prefers-reduced-motion`. Aucun champ ou flux d'édition n'est modifié.

Les **29 contrôles Node ciblés** (inventaire/contrats, primitives, médias) passent le 16 octobre en série. Ils ne remplacent pas la nouvelle mesure du navigateur isolé. Les styles de la lightbox et du layout ont aussi été compilés sans erreur lors du contrôle ciblé précédent ; aucun build Angular local n'est lancé.

## Ajustements du 16 octobre — primitives et pages pratiques

La revue des captures `0a75fa1e4` confirme les en-têtes et gouttières de météo Fleury à 390 px et tarifs Efteling à 1 366 px. Elle détecte deux petits textes encore en accent décoratif : légende Max et liens d'offre. Leur premier plan devient sémantique contrasté ; les courbes gardent leurs couleurs. Les rayons de tokens encore à 6–16 px sont également ramenés à 2 px. Ces corrections doivent être revues sur le prochain rendu ; les badges de données brutes, prix, dates, champs et contenus API ne sont ni réécrits ni figés.

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

