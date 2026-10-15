# V6 — régression visuelle

## Références

- Cible : HTML et captures du pack Playground V6.
- Avant : production V5.4.101 capturée le 14 octobre 2026 sans démarrer de serveur local.
- Après : captures de géométrie V6 produites sur fixture Chromium ; captures Angular réelles à valider dans l’environnement de revue final.

## Complément du 15 octobre : rendu initial Angular réel

`tools/design-v6/capture-public-ssr.mjs` invoque directement le moteur du bundle Angular compilé, sans ouvrir de serveur. Il n'autorise que des GET/HEAD anonymes vers l'API publique, retire cookies et autorisation, puis charge le HTML SSR et les assets du build dans Chromium. La CI de la branche V6 produit l'artefact `v6-public-ssr-initial`, avec 13 largeurs, deux thèmes, les huit accueils localisés et des routes publiques représentatives. Le rapport enregistre le SHA CI et le SHA source.

Ces captures prouvent uniquement le HTML initial et sa géométrie. Les scripts sont retirés des copies de capture : hydratation, `@defer`, interactions, focus et permissions ne sont donc pas validés par cet outil. Une seconde capture masque le panneau de consentement dans la copie pour inspecter le contenu ; elle est étiquetée comme telle et ne prétend pas démontrer le fonctionnement de son bouton. Le statut du moteur direct ne remplace pas les codes HTTP du serveur Express réel.

Le diagnostic local sur le build précédent, explicitement marqué `UNVERIFIED_LOCAL_BUILD`, a révélé que la règle mobile générique remplaçait les deux colonnes de la première carte hero par trois colonnes. Le titre était ainsi comprimé dans 1,7 rem. La règle à trois colonnes est désormais limitée aux autres cartes. Les 30 cas Chromium passent avec une assertion de largeur minimale du titre. La preuve du dernier build est attendue dans l'artefact CI, pas déduite de l'ancien build local.

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

## Transposition P04

L’accueil suit les enveloppes de la référence 1366/390 : manifeste et statistiques à gauche, quatre emplacements API à droite, recherche pleine largeur, puis vedettes et nouveautés. Les photos de démonstration sont remplacées par le média réel disponible via le service image ; les valeurs, parcs et contenus de la maquette ne sont jamais repris comme fixtures de production. La capture après Angular demeure bloquante à P20.

## Transposition P07–P08

L’inventaire et la fiche ParkItem utilisent les mêmes surfaces plates, filets visibles et petits rayons que la fiche parc, tout en conservant la densité des spécifications, conditions d’accès et facettes. Les rail/cards n’emploient ni gradient, ni masque décoratif, ni ombre de carte. La navigation latérale garde uniquement sa translation fonctionnelle de centrage vertical. Les captures chargées, sans média, non-attraction, clair/sombre et 390/1366 restent dues à P20.

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

