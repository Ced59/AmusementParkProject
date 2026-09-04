# Audit « une classe par fichier » — 4 septembre 2026

## Objectif

Le projet applique désormais la règle suivante au code C# et TypeScript écrit à la main : chaque classe possède son fichier dédié et aucun fichier ne déclare plusieurs classes. Les records C# de type référence sont considérés comme des classes ; les `record struct` restent des types valeur. Les sources générées et le code tiers sont exclus du contrôle.

Le dépôt contient une dette historique trop large pour être déplacée dans une seule PR sans augmenter fortement le risque de régression. Un inventaire versionné rend cette dette explicite et bloque dès maintenant toute nouvelle colocation ou aggravation d'un fichier existant.

## État initial vérifié

Le contrôle syntaxique recense :

- 328 fichiers C# multi-classes ;
- 33 fichiers TypeScript multi-classes ;
- 361 fichiers multi-classes au total ;
- 504 fichiers dont le nom ne correspond pas à toutes les classes déclarées, ce qui inclut les fichiers multi-classes ;
- 101 fichiers contenant au moins une classe C# `partial` écrite à la main ;
- 513 fichiers non conformes distincts au total : 432 en C# et 81 en TypeScript.

Répartition C# initiale :

| Projet | Multi-classes | Non conformes au total |
|---|---:|---:|
| `AmusementPark.Application` | 136 | 193 |
| `AmusementPark.Infrastructure` | 62 | 107 |
| `AmusementPark.WebAPI` | 53 | 54 |
| `AmusementPark.Application.Tests` | 32 | 33 |
| `AmusementPark.Core` | 20 | 20 |
| `AmusementPark.Infrastructure.Tests` | 13 | 13 |
| `AmusementPark.WebAPI.Tests` | 7 | 7 |
| `AmusementPark.Core.Tests` | 5 | 5 |

Parmi les 33 fichiers TypeScript multi-classes, 6 appartiennent au code de production et 27 aux tests. Le contrôle de nom porte au total sur 68 fichiers `.spec.ts` et 13 autres fichiers TypeScript non conformes.

## État après le lot Core Notations

Le premier lot déplace les 19 classes et records de référence qui étaient regroupés dans cinq fichiers du domaine Notations. Il ne modifie ni leur namespace, ni leur visibilité, ni leur comportement.

Après ce lot, l'inventaire contient :

- 323 fichiers C# multi-classes et 33 fichiers TypeScript multi-classes, soit 356 au total ;
- 499 fichiers présentant encore au moins une incompatibilité de nom ;
- 101 fichiers contenant encore au moins une classe C# `partial` écrite à la main ;
- 508 fichiers non conformes distincts : 427 en C# et 81 en TypeScript ;
- 15 fichiers non conformes dans `AmusementPark.Core`, contre 20 initialement.

## Fonctionnement du garde-fou

Le script `tools/architecture/check-one-class-per-file.mjs` :

1. parcourt les sources C# de `API` et toutes les extensions TypeScript prises en charge dans `FRONT/AmusementPark`, y compris `server.ts` et les fichiers de déclaration écrits à la main ;
2. ignore les sorties de compilation, dépendances, sources générées et répertoires tiers ;
3. retire commentaires et littéraux avant d'identifier les classes et records de référence C# ;
4. utilise l'arbre syntaxique TypeScript pour identifier les classes, y compris les classes imbriquées ou anonymes ;
5. valide que le nom du fichier correspond au nom de la classe, en tenant compte des séparateurs Angular `-` et `.` ;
6. normalise les identifiants C# verbatim comme `@event` et leurs échappements Unicode avant la comparaison ;
7. refuse toute nouvelle déclaration `partial` écrite à la main ;
8. refuse tout nouveau fichier multi-classe, toute classe ajoutée dans un fichier inventorié et toute augmentation du nombre de classes ;
9. compare l'inventaire proposé au SHA de base réel fourni par le workflow pour `main` ou `master`, afin qu'une PR ou un push ne puisse pas l'agrandir ;
10. exige la réduction du fichier de référence dans la même PR lorsqu'une dette est corrigée.

Le fichier `one-class-per-file-baseline.json` est un inventaire temporaire, pas une liste d'exceptions permanentes. Il ne doit jamais être agrandi.

## Ordre de résorption

Les corrections restent de simples déplacements sans changement de contrats ni de comportement et sont livrées par lots cohérents :

1. domaine Core ;
2. contrats et cas d'usage Application, en commençant par Passeport et Notations ;
3. documents et repositories Infrastructure ;
4. contrats, mappers et services WebAPI ;
5. classes Angular de production ;
6. fixtures et helpers de tests, projet par projet ;
7. suppression du dernier inventaire vide et durcissement du contrôle sans dette tolérée.

Chaque lot doit conserver les namespaces, exports, injections et visibilités existants, exécuter les tests ciblés de sa couche, puis laisser la CI complète vérifier l'intégration.
