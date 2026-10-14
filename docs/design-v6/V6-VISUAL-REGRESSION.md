# V6 — régression visuelle

## Références

- Cible : HTML et captures du pack Playground V6.
- Avant : production V5.4.101 capturée le 14 octobre 2026 sans démarrer de serveur local.
- Après : captures Angular à produire dans un environnement isolé après chaque famille de pages.

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

## Verdicts à renseigner après migration

Pour chaque famille : avant V5, cible V6, après Angular, léger, chargé, erreur, sans média, titre long, clair, sombre, mobile, tablette, desktop, zoom 200 %, focus clavier et mouvement réduit.

Tolérances : les alignements invariants dépassant 3 px et les blocs stables différant de plus de 5 % sont corrigés ou justifiés. Les données réelles, le wordmark protégé, le slogan et les contrôles produit absents de la maquette sont des écarts attendus.

## Écart contrôlé P03

Le pictogramme carré historique a été retiré du header et du footer, conformément à la clarification de marque. Le wordmark typographique original reste inchangé dans les deux emplacements. Le shell adopte la largeur de 1160 px, les filets, surfaces opaques et angles faibles de Playground ; les captures Angular finales restent dues à P20 dans l’environnement de recette.

