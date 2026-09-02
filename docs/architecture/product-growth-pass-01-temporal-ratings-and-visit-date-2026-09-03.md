# PASS-01 — Préférence actuelle, observations temporelles et date de visite

Date : 2026-09-03

Statut : accepté

Roadmap : `docs/roadmaps/product-growth/02-visit-passport-and-ride-log-roadmap.md`

## Contexte

La note globale existante et les futures notes du Passeport répondent à des questions différentes. `UserRating` exprime la préférence actuelle d'une personne pour un parc ou un élément. Une note associée à une visite ou à une occurrence de ride décrit une expérience privée située dans le temps.

Les confondre permettrait à plusieurs rides d'une même personne de produire plusieurs votes communautaires. Convertir une ancienne visite partielle en date exacte créerait par ailleurs une information fausse et rendrait les statistiques personnelles trompeuses.

Cette décision complète les ADR FOUNDATION. Elle fixe les invariants applicables avant toute création de collection Mongo ou de contrat HTTP PASS.

## Décision 1 — deux familles de notes physiquement séparées

### Préférence communautaire actuelle

`UserRating` reste l'unique source d'un vote utilisateur pour une cible :

- unicité par `(UserId, TargetType, TargetId)` ;
- modification explicite par l'utilisateur ;
- contribution au maximum une fois à `RatingAggregate` et aux snapshots de classement ;
- existence possible sans visite ;
- visibilité éventuelle dans le classement personnel selon les réglages existants.

### Observation personnelle temporelle

Les observations du Passeport seront stockées exclusivement dans leur agrégat propriétaire :

```text
Visit.parkAssessment
RideOccurrence.assessment
```

Chaque assessment actif contiendra `valueHalfSteps`, un commentaire privé facultatif, une révision et ses timestamps. Son historique de correction sera append-only et séparé de l'état actif.

Invariants :

- zéro ou une note de parc active par visite ;
- zéro ou une note active par occurrence ;
- confidentialité par défaut et accès réservé au propriétaire ;
- aucune écriture automatique dans `UserRating`, `RatingAggregate` ou un snapshot communautaire ;
- aucune création de vote communautaire lors d'un ajout, d'une duplication, d'une correction ou d'une suppression de ride ;
- une éventuelle copie vers la préférence globale exigera une action distincte, explicite et confirmée.

Le Core réutilise `RatingValue`. Les documents temporels stockeront l'entier exact `valueHalfSteps` ; `0` ne représentera jamais l'absence de note.

## Décision 2 — une visite est une session, pas une clé calendaire

Une `Visit` appartient à un utilisateur et à un seul parc. Elle représente une session déclarée rattachée, lorsqu'un jour est connu, au jour de service local choisi.

- deux parcs le même jour donnent deux visites ;
- deux jours consécutifs dans le même parc donnent deux visites ;
- plusieurs visites du même parc le même jour restent autorisées ;
- aucun index unique `(UserId, ParkId, Date)` ne sera créé ;
- une visite traversant minuit peut rester rattachée à son jour de service initial.

Les identifiants restent des chaînes opaques aux frontières et des wrappers typés dans le Core. Aucune migration globale vers `Guid` n'est autorisée.

## Décision 3 — préserver la précision réellement connue

`VisitDate` contient exactement :

```text
year
month?          absent avec la précision Year
day?            présent uniquement avec la précision Day
precision       Year | Month | Day
isApproximate
```

Règles :

- `Year` interdit mois et jour ;
- `Month` exige un mois valide et interdit le jour ;
- `Day` exige une date grégorienne valide, années bissextiles comprises ;
- les années de 1 à 9999 sont acceptées, y compris avant 1900 ;
- le caractère approximatif ne change pas la précision ;
- aucune date partielle n'est transformée en premier du mois, premier janvier ou minuit UTC comme source de vérité ;
- des bornes calculées peuvent servir aux filtres, mais ne sont jamais persistées comme date déclarée.

Le tri de liste sera stable et explicite : année, composants connus selon la vue, puis `CreatedAtUtc` et identifiant. Les graphiques regrouperont les dates partielles par période au lieu de les placer sur un jour inventé.

## Décision 4 — fuseau et jour de service portés par la visite

Le fuseau IANA n'appartient pas à `VisitDate`. Il sera stocké sur `Visit`, car il décrit le contexte du parc et peut être corrigé sans modifier la précision de la date.

`LocalServiceDayConvention` distingue :

- `VisitStartLocalDate` : le jour local du début de la session ;
- `UserSelectedServiceDate` : le jour de service choisi explicitement, notamment après minuit.

Une heure d'occurrence exigera une précision `Day` et un fuseau IANA valide. L'heure locale restera la donnée déclarée ; les heures inexistantes lors du passage à l'heure d'été seront rejetées et les heures ambiguës exigeront un choix d'offset avant toute conversion UTC.

Le value object calendaire accepte toute date valide. L'agrégat `Visit`, qui connaît son statut et son contexte local, interdira une visite accomplie dans le futur. Une date future planifiée relève de `TripPlan`, pas du journal de visites.

## Décision 5 — frontières d'architecture et de persistance

- Core : invariants de `VisitDate`, états de visite, ownership structurel, note exacte et transitions ;
- Application : autorisation du propriétaire, orchestration, horloge, validation IANA via un port, idempotence et mapping ;
- Infrastructure : documents Mongo, indexes, concurrence optimiste et audit ;
- WebAPI : authentification, contrats additifs, `If-Match` et traduction des erreurs ;
- Angular : saisie et affichage localisés, sans déduire le format d'un identifiant ni recalculer les règles métier.

Les assessments actifs seront embarqués dans leur parent pour garantir une écriture atomique sur MongoDB autonome. Les occurrences resteront dans une collection séparée afin de borner la taille du document `Visit`.

Toutes les mutations porteront une version optimiste. Les créations rapides et groupées utiliseront un identifiant d'opération idempotent : même clé et même payload rejouent le résultat ; même clé et payload différent produisent un conflit.

## Conséquences

### Bénéfices

- dix rides d'une personne ne valent jamais dix voix dans le classement ;
- les analyses temporelles restent personnelles et reproductibles ;
- une visite ancienne conserve honnêtement son niveau d'incertitude ;
- les futures interfaces Web et mobile peuvent partager les mêmes contrats ;
- la persistance peut évoluer sans déplacer les règles métier hors du Core.

### Coûts assumés

- deux libellés et deux parcours de modification doivent rester clairement distingués ;
- les requêtes statistiques doivent choisir explicitement leur source ;
- les dates partielles demandent des filtres et graphiques spécifiques ;
- la gestion des heures nécessite le fuseau et les cas DST.

## Décisions rejetées

- agréger automatiquement les notes de rides dans la note communautaire ;
- déduire une visite depuis `UserRating.CreatedAtUtc` ;
- créer une visite unique par parc et par jour ;
- convertir une date partielle en `DateTime` synthétique ;
- stocker le fuseau dans `VisitDate` ;
- créer dès la V1 une collection séparée pour chaque assessment un-à-un ;
- utiliser le dernier écrivain gagnant sans conflit visible.

## Preuves exigées avant PASS-02

- tests purs des précisions `Year`, `Month` et `Day` ;
- tests d'années bissextiles et de jours impossibles ;
- rejet des combinaisons précision/composants incohérentes ;
- conservation explicite de l'approximation ;
- bornes calculées sans mutation de la précision ;
- vérification que la convention de jour de service est un choix métier explicite ;
- aucune collection, aucun endpoint et aucune modification de `UserRating` dans cette tranche.

## Retour arrière

PASS-01 ne crée ni collection, ni index, ni donnée utilisateur, ni contrat public. Son retour arrière consiste à retirer les nouveaux types purs et cet ADR. Les notes et classements 4.3.1 restent inchangés.
