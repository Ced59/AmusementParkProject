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

## État après le lot Core Historique

Le lot Historique extrait les quatre modèles publics qui partageaient
`HistoryModels.cs` et le record interne de précision calendaire imbriqué dans
`AutomaticHistoryEventFactory.cs`. Les namespaces, propriétés, valeurs par défaut et
usages restent identiques ; aucun contrat HTTP, format MongoDB ou comportement
métier ne change.

Après ce lot et les réductions déjà intégrées par les jalons fonctionnels
intermédiaires, l'inventaire contient :

- 281 fichiers C# multi-classes et 33 fichiers TypeScript multi-classes, soit 314 au total ;
- 453 fichiers présentant encore au moins une incompatibilité de nom ;
- 101 fichiers contenant encore au moins une classe C# `partial` écrite à la main ;
- 462 fichiers non conformes distincts : 381 en C# et 81 en TypeScript ;
- 13 fichiers non conformes dans `AmusementPark.Core`, contre 15 avant ce lot.

## État après le lot Core Qualité des données de parc

Le lot Qualité des données de parc sépare les modèles de score, les contextes de
calcul, le constructeur interne, les règles de complétude et les signaux de
publication. Les namespaces, propriétés, valeurs par défaut et règles de calcul
restent identiques ; aucun contrat HTTP, format MongoDB ou résultat de score ne
change.

Après ce lot, l'inventaire contient :

- 279 fichiers C# multi-classes et 33 fichiers TypeScript multi-classes, soit 312 au total ;
- 451 fichiers présentant encore au moins une incompatibilité de nom ;
- 101 fichiers contenant encore au moins une classe C# `partial` écrite à la main ;
- 460 fichiers non conformes distincts : 379 en C# et 81 en TypeScript ;
- 11 fichiers non conformes dans `AmusementPark.Core`, contre 13 avant ce lot.

## État après le lot Core Horaires d'ouverture

Le lot Horaires d'ouverture sépare la planification régulière, les exceptions par
date, les plages horaires, la couverture calendaire et leurs résumés de lecture.
Les namespaces, propriétés, valeurs par défaut et relations entre modèles restent
identiques ; aucun horaire, contrat HTTP ou format MongoDB ne change.

Après ce lot, l'inventaire contient :

- 276 fichiers C# multi-classes et 33 fichiers TypeScript multi-classes, soit 309 au total ;
- 448 fichiers présentant encore au moins une incompatibilité de nom ;
- 101 fichiers contenant encore au moins une classe C# `partial` écrite à la main ;
- 457 fichiers non conformes distincts : 376 en C# et 81 en TypeScript ;
- 8 fichiers non conformes dans `AmusementPark.Core`, contre 11 avant ce lot.

## État après le lot Core Tarification

Le lot Tarification sépare les offres d'entrée, pass annuels, parkings, crédits,
instantanés historiques, valeurs monétaires et résultats de normalisation. Les
namespaces, propriétés, valeurs par défaut et règles de validation restent
identiques ; aucun montant, contrat HTTP ou format MongoDB ne change.

Après ce lot, l'inventaire contient :

- 274 fichiers C# multi-classes et 33 fichiers TypeScript multi-classes, soit 307 au total ;
- 446 fichiers présentant encore au moins une incompatibilité de nom ;
- 101 fichiers contenant encore au moins une classe C# `partial` écrite à la main ;
- 455 fichiers non conformes distincts : 374 en C# et 81 en TypeScript ;
- 6 fichiers non conformes dans `AmusementPark.Core`, contre 8 avant ce lot.

## État après le lot Core Pages techniques

Le lot Pages techniques sépare la page, ses alias, ses blocs de contenu, listes,
tableaux, cellules, métriques et liens. Les namespaces, propriétés, valeurs par
défaut et relations entre modèles restent identiques ; aucun contenu, contrat
HTTP, format MongoDB ou comportement SEO ne change.

Après ce lot, l'inventaire contient :

- 273 fichiers C# multi-classes et 33 fichiers TypeScript multi-classes, soit 306 au total ;
- 445 fichiers présentant encore au moins une incompatibilité de nom ;
- 101 fichiers contenant encore au moins une classe C# `partial` écrite à la main ;
- 454 fichiers non conformes distincts : 373 en C# et 81 en TypeScript ;
- 5 fichiers non conformes dans `AmusementPark.Core`, contre 6 avant ce lot.

## État après le lot Core Statistiques du passeport

Le lot Statistiques du passeport sépare les observations de visites et de tours,
les distributions de notes, les bilans par élément, parc et année, les tendances
et leurs calculateurs. Les namespaces, signatures, formules, seuils et messages
de validation restent identiques ; aucun résultat statistique, contrat HTTP ou
format MongoDB ne change.

Après ce lot, l'inventaire contient :

- 270 fichiers C# multi-classes et 33 fichiers TypeScript multi-classes, soit 303 au total ;
- 442 fichiers présentant encore au moins une incompatibilité de nom ;
- 101 fichiers contenant encore au moins une classe C# `partial` écrite à la main ;
- 451 fichiers non conformes distincts : 370 en C# et 81 en TypeScript ;
- 2 fichiers non conformes dans `AmusementPark.Core`, contre 5 avant ce lot.

## État après le lot Core Ordre des tours

Le lot Ordre des tours sépare la position calculée, la garde de concurrence et le
plan produit lors d'un déplacement dans la timeline. Les identifiants, positions,
règles d'ancrage, normalisation et limites restent identiques ; aucun ordre de
tour, contrat HTTP ou format MongoDB ne change.

Après ce lot, l'inventaire contient :

- 269 fichiers C# multi-classes et 33 fichiers TypeScript multi-classes, soit 302 au total ;
- 441 fichiers présentant encore au moins une incompatibilité de nom ;
- 101 fichiers contenant encore au moins une classe C# `partial` écrite à la main ;
- 450 fichiers non conformes distincts : 369 en C# et 81 en TypeScript ;
- 1 fichier non conforme dans `AmusementPark.Core`, contre 2 avant ce lot.

## État après le lot Core Météo

Le lot Météo sépare l'instantané quotidien, l'exécution d'actualisation et son
détail par parc. Les propriétés, statuts, compteurs et types de données restent
identiques ; aucune prévision, observation, orchestration ou représentation
MongoDB ne change.

Après ce lot, l'inventaire contient :

- 268 fichiers C# multi-classes et 33 fichiers TypeScript multi-classes, soit 301 au total ;
- 440 fichiers présentant encore au moins une incompatibilité de nom ;
- 101 fichiers contenant encore au moins une classe C# `partial` écrite à la main ;
- 449 fichiers non conformes distincts : 368 en C# et 81 en TypeScript ;
- aucun fichier non conforme dans `AmusementPark.Core`, contre 15 au début de la résorption Core.

## État après le lot Application Contrats statistiques du passeport

Ce lot sépare les contrats de résultat des statistiques d'attraction, de parc et
d'année, ainsi que les projections sources utilisées par les cas d'usage. Les
espaces de noms, signatures, propriétés, ordres de paramètres, valeurs par défaut
et formats JSON restent identiques. Aucun calcul métier, contrat HTTP ni document
MongoDB ne change.

Après ce lot, l'inventaire contient :

- 265 fichiers C# multi-classes et 33 fichiers TypeScript multi-classes, soit 298 au total ;
- 437 fichiers présentant encore au moins une incompatibilité de nom ;
- 101 fichiers contenant encore au moins une classe C# `partial` écrite à la main ;
- 446 fichiers non conformes distincts : 365 en C# et 81 en TypeScript ;
- 154 fichiers non conformes dans `AmusementPark.Application`, contre 157 avant ce lot ;
- aucun fichier non conforme dans `AmusementPark.Core`.

## État après le lot Application Modèles de persistance du passeport

Ce lot sépare les modèles qui portent la pagination des visites et des tours,
l'ajout idempotent, la réservation des clés de création, le réordonnancement
versionné et les suggestions de note globale. Les espaces de noms, signatures,
constantes, statuts et valeurs restent identiques. Aucun calcul métier, contrat
HTTP, index ni document MongoDB ne change.

Après ce lot, l'inventaire contient :

- 262 fichiers C# multi-classes et 33 fichiers TypeScript multi-classes, soit 295 au total ;
- 433 fichiers présentant encore au moins une incompatibilité de nom ;
- 101 fichiers contenant encore au moins une classe C# `partial` écrite à la main ;
- 442 fichiers non conformes distincts : 361 en C# et 81 en TypeScript ;
- 150 fichiers non conformes dans `AmusementPark.Application`, contre 157 au début de la résorption Application ;
- 8 fichiers non conformes dans le périmètre Application du passeport.

## État après le lot Application Preuves d'audit du passeport

Ce lot sépare les instantanés minimisés d'une visite, d'une note de parc et d'un
tour, leurs deux fabriques d'événements d'audit, ainsi que la portée analysée d'une
requête de tour. Les champs comparés, corrélations, versions et garanties
d'exclusion des textes privés restent identiques. Aucun événement, calcul métier,
contrat HTTP ni document MongoDB ne change.

Après ce lot, l'inventaire contient :

- 260 fichiers C# multi-classes et 33 fichiers TypeScript multi-classes, soit 293 au total ;
- 431 fichiers présentant encore au moins une incompatibilité de nom ;
- 101 fichiers contenant encore au moins une classe C# `partial` écrite à la main ;
- 440 fichiers non conformes distincts : 359 en C# et 81 en TypeScript ;
- 148 fichiers non conformes dans `AmusementPark.Application`, contre 157 au début de la résorption Application ;
- 6 fichiers non conformes dans le périmètre Application du passeport.

## État après le lot Application Parcours de visite du passeport

Ce lot sépare les gestionnaires de création et de consultation des visites, de
modification des informations, de passage entre brouillon, terminé et archivé,
ainsi que d'ajout ou suppression de la note du parc. Les dépendances, validations,
versions attendues, baux de mutation et résultats restent identiques. Aucun
comportement utilisateur, contrat HTTP ni document MongoDB ne change.

Après ce lot, l'inventaire contient :

- 257 fichiers C# multi-classes et 33 fichiers TypeScript multi-classes, soit 290 au total ;
- 428 fichiers présentant encore au moins une incompatibilité de nom ;
- 101 fichiers contenant encore au moins une classe C# `partial` écrite à la main ;
- 437 fichiers non conformes distincts : 356 en C# et 81 en TypeScript ;
- 145 fichiers non conformes dans `AmusementPark.Application`, contre 157 au début de la résorption Application ;
- 3 fichiers non conformes dans le périmètre Application du passeport.

## État après le lot Application Parcours des tours du passeport

Ce lot sépare les gestionnaires de consultation, modification, suppression et
réordonnancement des tours, ainsi que d'ajout ou suppression de leur note privée.
Les dépendances, validations, versions attendues, clés d'idempotence, baux de
mutation et résultats restent identiques. Aucun comportement utilisateur, contrat
HTTP ni document MongoDB ne change.

Après ce lot, l'inventaire contient :

- 254 fichiers C# multi-classes et 33 fichiers TypeScript multi-classes, soit 287 au total ;
- 425 fichiers présentant encore au moins une incompatibilité de nom ;
- 101 fichiers contenant encore au moins une classe C# `partial` écrite à la main ;
- 434 fichiers non conformes distincts : 353 en C# et 81 en TypeScript ;
- 142 fichiers non conformes dans `AmusementPark.Application`, contre 157 au début de la résorption Application ;
- aucun fichier non conforme dans le périmètre Application du passeport.

## État après le lot Application Socle et traitements en arrière-plan

Ce lot sépare les états, charges, demandes, baux, résultats, diagnostics et
contrats d'exécution des traitements différés. Il isole également la demande
d'invalidation du cache SSR de son port. Les signatures publiques, valeurs par
défaut, règles de sérialisation et comportements des gestionnaires restent
identiques. Aucun comportement utilisateur, contrat HTTP ni document MongoDB ne
change.

Après ce lot, l'inventaire contient :

- 252 fichiers C# multi-classes et 33 fichiers TypeScript multi-classes, soit 285 au total ;
- 422 fichiers présentant encore au moins une incompatibilité de nom ;
- 101 fichiers contenant encore au moins une classe C# `partial` écrite à la main ;
- 431 fichiers non conformes distincts : 350 en C# et 81 en TypeScript ;
- 139 fichiers non conformes dans `AmusementPark.Application`, contre 157 au début de la résorption Application ;
- aucun fichier non conforme dans le périmètre Application des traitements en arrière-plan et des ports partagés.

## État après le lot Application Commentaires

Ce lot sépare les commandes, requêtes, résultats et gestionnaires de création,
modification et suppression des commentaires. Il isole également les opérations
d'images et les services internes de validation, normalisation, autorisation et
projection. Les dépendances, validations, règles d'autorisation, réservations
d'images et réponses restent identiques. Aucun comportement utilisateur, contrat
HTTP ni document MongoDB ne change.

Après ce lot, l'inventaire contient :

- 244 fichiers C# multi-classes et 33 fichiers TypeScript multi-classes, soit 277 au total ;
- 414 fichiers présentant encore au moins une incompatibilité de nom ;
- 101 fichiers contenant encore au moins une classe C# `partial` écrite à la main ;
- 423 fichiers non conformes distincts : 342 en C# et 81 en TypeScript ;
- 131 fichiers non conformes dans `AmusementPark.Application`, contre 157 au début de la résorption Application ;
- aucun fichier non conforme dans le périmètre Application des commentaires.

## État après le lot Application Histoire

Ce lot sépare les commandes, requêtes, résultats et gestionnaires des frises
historiques de parc, d'attraction rattachée et d'attraction autonome. Il isole
également les articles, la pagination et la règle interne de visibilité
publique. Les filtres, règles de visibilité, enrichissements d'images et réponses
restent identiques. Aucun comportement utilisateur, contrat HTTP ni document
MongoDB ne change.

Après ce lot, l'inventaire contient :

- 239 fichiers C# multi-classes et 33 fichiers TypeScript multi-classes, soit 272 au total ;
- 407 fichiers présentant encore au moins une incompatibilité de nom ;
- 101 fichiers contenant encore au moins une classe C# `partial` écrite à la main ;
- 416 fichiers non conformes distincts : 335 en C# et 81 en TypeScript ;
- 124 fichiers non conformes dans `AmusementPark.Application`, contre 157 au début de la résorption Application ;
- aucun fichier non conforme dans le périmètre Application de l'histoire.

## État après le lot Application Météo des parcs

Ce lot sépare les commandes, requêtes, résultats et gestionnaires de prévisions,
comparaisons historiques et campagnes de rafraîchissement météo. Il isole
également le résultat d'un fournisseur de son port de stratégie. Les règles de
rafraîchissement, attributions, filtres, comparaisons et réponses restent
identiques. Aucun comportement utilisateur, contrat HTTP ni document MongoDB ne
change.

Après ce lot, l'inventaire contient :

- 234 fichiers C# multi-classes et 33 fichiers TypeScript multi-classes, soit 267 au total ;
- 401 fichiers présentant encore au moins une incompatibilité de nom ;
- 101 fichiers contenant encore au moins une classe C# `partial` écrite à la main ;
- 410 fichiers non conformes distincts : 329 en C# et 81 en TypeScript ;
- 118 fichiers non conformes dans `AmusementPark.Application`, contre 157 au début de la résorption Application ;
- aucun fichier non conforme dans le périmètre Application de la météo des parcs.

## État après le lot Application Horaires d'ouverture

Ce lot sépare les commandes, requêtes, résultats et gestionnaires des horaires
de parc. Il isole également les alertes de couverture et leurs candidates des
ports et processeurs qui les utilisent. Les règles saisonnières, exceptions de
dates, plages horaires, calendriers visiteurs et notifications restent
identiques. Aucun comportement utilisateur, contrat HTTP ni document MongoDB ne
change.

Après ce lot, l'inventaire contient :

- 230 fichiers C# multi-classes et 33 fichiers TypeScript multi-classes, soit 263 au total ;
- 394 fichiers présentant encore au moins une incompatibilité de nom ;
- 101 fichiers contenant encore au moins une classe C# `partial` écrite à la main ;
- 403 fichiers non conformes distincts : 322 en C# et 81 en TypeScript ;
- 111 fichiers non conformes dans `AmusementPark.Application`, contre 157 au début de la résorption Application ;
- aucun fichier non conforme dans le périmètre Application des horaires d'ouverture.

## État après le lot Application Pages techniques

Ce lot sépare les commandes, requêtes, résultats et gestionnaires qui permettent
d'afficher et d'administrer les pages d'aide, d'information et les contenus
institutionnels. Il isole également le résultat de persistance de son port. Le
normaliseur conserve une expression régulière compilée et partagée, mais n'a
plus besoin d'une classe `partial` générée. Les contenus, slugs, validations,
traductions, règles de visibilité et rafraîchissements SEO restent identiques.
Aucun contrat HTTP ni document MongoDB ne change.

Après ce lot, l'inventaire contient :

- 226 fichiers C# multi-classes et 33 fichiers TypeScript multi-classes, soit 259 au total ;
- 389 fichiers présentant encore au moins une incompatibilité de nom ;
- 100 fichiers contenant encore au moins une classe C# `partial` écrite à la main ;
- 397 fichiers non conformes distincts : 316 en C# et 81 en TypeScript ;
- 105 fichiers non conformes dans `AmusementPark.Application`, contre 157 au début de la résorption Application ;
- aucun fichier non conforme dans le périmètre Application des pages techniques.

## État après le lot Application Publication sociale

Ce lot sépare les commandes, contrats, requêtes et gestionnaires qui préparent,
publient, réconcilient et synchronisent les liens et annonces sur les plateformes
sociales. Il isole aussi les résultats de reprise et les modèles internes de
résolution d'une cible publique. Les contrôles d'éligibilité, messages, images,
reprises après échec, aperçus et règles anti-duplication restent identiques.
Aucun appel externe, contrat HTTP ni document MongoDB ne change.

Après ce lot, l'inventaire contient :

- 220 fichiers C# multi-classes et 33 fichiers TypeScript multi-classes, soit 253 au total ;
- 383 fichiers présentant encore au moins une incompatibilité de nom ;
- 100 fichiers contenant encore au moins une classe C# `partial` écrite à la main ;
- 391 fichiers non conformes distincts : 310 en C# et 81 en TypeScript ;
- 99 fichiers non conformes dans `AmusementPark.Application`, contre 157 au début de la résorption Application ;
- aucun fichier non conforme dans le périmètre Application de la publication sociale.

## État après le lot Application Modèles SEO

Ce lot sépare les commandes, requêtes, résultats et modèles qui décrivent les
URL publiques, les instantanés de contenu, les réglages du sitemap et les
résultats transmis aux moteurs de recherche. Les identifiants, URL, règles
d'indexation, formats de sérialisation et réponses restent identiques. Aucun
comportement utilisateur, contrat HTTP ni document MongoDB ne change.

Après ce lot, l'inventaire contient :

- 215 fichiers C# multi-classes et 33 fichiers TypeScript multi-classes, soit 248 au total ;
- 378 fichiers présentant encore au moins une incompatibilité de nom ;
- 100 fichiers contenant encore au moins une classe C# `partial` écrite à la main ;
- 386 fichiers non conformes distincts : 305 en C# et 81 en TypeScript ;
- 94 fichiers non conformes dans `AmusementPark.Application`, contre 157 au début de la résorption Application ;
- aucun fichier non conforme dans le périmètre Application des modèles SEO concernés par ce lot.

## État après le lot Application Gestionnaires SEO

Ce lot sépare les six cas d'usage qui permettent de générer les sitemaps,
d'administrer les réglages IndexNow, de consulter l'état et l'historique des
générations, puis de servir les documents publics. Les validations, valeurs par
défaut, règles de pagination, générations de secours, découpages de sections et
réponses restent identiques. Aucun écran, contrat HTTP, appel externe ni
document MongoDB ne change.

Après ce lot, l'inventaire contient :

- 214 fichiers C# multi-classes et 33 fichiers TypeScript multi-classes, soit 247 au total ;
- 377 fichiers présentant encore au moins une incompatibilité de nom ;
- 100 fichiers contenant encore au moins une classe C# `partial` écrite à la main ;
- 385 fichiers non conformes distincts : 304 en C# et 81 en TypeScript ;
- 93 fichiers non conformes dans `AmusementPark.Application`, contre 157 au début de la résorption Application ;
- aucun fichier multi-classe dans le périmètre des gestionnaires de sitemap SEO.

## État après le lot Application Référencement des contenus historiques

Ce lot sépare les fournisseurs de sitemap des frises et des articles historiques,
le résolveur de leurs candidats publics, les données résolues et l’évaluation de
leur indexabilité. Les critères de visibilité, de langue, de rattachement à un
parc, de date, de slug et de dernière modification restent identiques. Aucun
écran, contrat HTTP, appel externe ni document MongoDB ne change.

Après ce lot, l’inventaire contient :

- 212 fichiers C# multi-classes et 33 fichiers TypeScript multi-classes, soit 245 au total ;
- 375 fichiers présentant encore au moins une incompatibilité de nom ;
- 100 fichiers contenant encore au moins une classe C# `partial` écrite à la main ;
- 383 fichiers non conformes distincts : 302 en C# et 81 en TypeScript ;
- 91 fichiers non conformes dans `AmusementPark.Application`, contre 157 au début de la résorption Application ;
- aucun fichier multi-classe dans le périmètre du référencement des contenus historiques.

## État après le lot Application Référencement des galeries d’images

Ce lot sépare les deux fournisseurs qui publient dans les sitemaps les galeries
d’images des parcs et celles de leurs attractions. Les contrôles de visibilité,
le nombre minimal d’images, les langues, les URL et les dates de dernière
modification restent identiques. Aucun écran, média, contrat HTTP ni document
MongoDB ne change.

Après ce lot, l’inventaire contient :

- 211 fichiers C# multi-classes et 33 fichiers TypeScript multi-classes, soit 244 au total ;
- 374 fichiers présentant encore au moins une incompatibilité de nom ;
- 100 fichiers contenant encore au moins une classe C# `partial` écrite à la main ;
- 382 fichiers non conformes distincts : 301 en C# et 81 en TypeScript ;
- 90 fichiers non conformes dans `AmusementPark.Application`, contre 157 au début de la résorption Application ;
- aucun fichier multi-classe dans le périmètre du référencement des galeries d’images.

## État après le lot Application Référencement des vidéos

Ce lot sépare les fournisseurs de sitemap des vidéos de parcs et d’attractions,
ainsi que leur utilitaire commun de chargement et de regroupement. Les contrôles
de visibilité, de rattachement, de langue, les URL, la pagination et les dates de
dernière modification restent identiques. Aucun écran, média, contrat HTTP ni
document MongoDB ne change.

Après ce lot, l’inventaire contient :

- 210 fichiers C# multi-classes et 33 fichiers TypeScript multi-classes, soit 243 au total ;
- 373 fichiers présentant encore au moins une incompatibilité de nom ;
- 100 fichiers contenant encore au moins une classe C# `partial` écrite à la main ;
- 381 fichiers non conformes distincts : 300 en C# et 81 en TypeScript ;
- 89 fichiers non conformes dans `AmusementPark.Application`, contre 157 au début de la résorption Application ;
- aucun fichier multi-classe dans le périmètre du référencement des vidéos.

## État après le lot Application Notification des mises à jour SEO

Ce lot sépare le service qui signale les changements de contenu public et
l’ordonnanceur neutre utilisé lorsqu’aucun rafraîchissement de sitemap n’est
nécessaire. La résolution des URL, l’invalidation, IndexNow, la tolérance aux
erreurs et les délais restent identiques. Aucun écran, contrat HTTP, appel
externe supplémentaire ni document MongoDB ne change.

Après ce lot, l’inventaire contient :

- 209 fichiers C# multi-classes et 33 fichiers TypeScript multi-classes, soit 242 au total ;
- 372 fichiers présentant encore au moins une incompatibilité de nom ;
- 100 fichiers contenant encore au moins une classe C# `partial` écrite à la main ;
- 380 fichiers non conformes distincts : 299 en C# et 81 en TypeScript ;
- 88 fichiers non conformes dans `AmusementPark.Application`, contre 157 au début de la résorption Application ;
- aucun fichier multi-classe dans le périmètre de la notification des mises à jour SEO.

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
