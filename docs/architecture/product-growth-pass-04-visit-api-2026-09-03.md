# PASS-04 — API propriétaire de création et lecture des visites

Date : 2026-09-03

Roadmap : `docs/roadmaps/product-growth/02-visit-passport-and-ride-log-roadmap.md`

## Résultat

Le premier contrat HTTP du Passeport expose trois opérations privées et additives :

```text
POST /api/me/passport/visits
GET  /api/me/passport/visits
GET  /api/me/passport/visits/{visitId}
```

Le préfixe `/api` est fourni par le routage d'hébergement existant ; le contrôleur déclare `me/passport/visits`. Les trois opérations exigent un compte authentifié, activé et non bloqué, et répondent avec `Cache-Control: no-store`.

La réponse `201` fournit une URI `Location` relative vers le détail. Sa construction réutilise le préfixe public validé (`X-Forwarded-Prefix` ou `PathBase`) afin de conserver `/api` lorsque Nginx le retire avant de transférer la requête à ASP.NET.

Le corps public ne contient jamais `userId`. Le contrôleur le lit dans le claim authentifié, puis les handlers Application le transmettent au repository propriétaire. Une visite appartenant à un autre compte et une visite absente produisent le même `404 visit.not-found`.

## Création idempotente

`POST` exige l'en-tête `Idempotency-Key`, limité à 128 caractères affichables. La création suit ce flux :

```text
claim propriétaire
  -> validation date partielle + fuseau IANA
  -> vérification de l'existence du parc
  -> création de l'agrégat Visit privé
  -> insert Mongo avec hash(clé) + hash(payload normalisé)
```

La collection possède l'index partiel unique suivant :

```text
{ userId: 1, creationOperationKeyHash: 1 } UNIQUE
```

- même propriétaire + même clé + même payload : la visite initiale est rejouée avec `201` et `Idempotency-Replayed: true` ;
- même propriétaire + même clé + payload différent : `409 visit.idempotency-key-conflict` ;
- la clé brute n'est jamais persistée ;
- la durée de rejouabilité est la durée de vie de la visite. Le futur workflow de suppression devra conserver un tombstone d'idempotence pendant sa période de rétention avant purge.

Le hash du payload couvre le parc, la date complète ou partielle, son caractère approximatif, le fuseau, la convention de jour de service, le titre et la note privée. Il exclut l'identifiant généré et les timestamps.

## Liste cursorisée

La liste accepte `limit` (1 à 100), `parkId`, `year`, `status` et `cursor`. Le curseur opaque encode uniquement la dernière clé de tri : date métier, `updatedAtUtc` et `visitId`. Il ne contient ni utilisateur ni donnée d'un autre compte.

Le tri stable est :

```text
dateSortKey DESC, updatedAt DESC, _id ASC
```

`dateSortKey` vaut `YYYYMMDD`, avec `00` pour une partie inconnue. Ainsi une date exacte reste avant le mois correspondant, puis le mois avant l'année seule, sans inventer une précision métier. Un index propriétaire dédié couvre ce tri.

Au démarrage, un backfill Mongo idempotent calcule cette projection pour les éventuelles visites écrites par PASS-03 avant la création de l'index. Le calcul source reste dans `VisitDate` (Core) ; le pipeline Mongo en reproduit la formule uniquement pour migrer les documents historiques.

Le filtre `hasAssessment` restera absent jusqu'à PASS-09, car exposer un filtre sur une donnée qui n'existe pas encore créerait un faux contrat.

## Frontières d'architecture

- Core valide `Visit`, `VisitDate`, confidentialité et états ;
- Application orchestre propriétaire, parc, horloge, validation de fuseau et résultats ;
- Infrastructure possède `TimeZoneInfo`, empreintes, BSON, index et requêtes Mongo ;
- WebAPI possède les DTO, enums HTTP, curseur opaque, authentification et Problem Details ;
- aucune règle métier n'est calculée dans le contrôleur.

## Preuves

Les tests couvrent création, replay, conflit de clé, date et fuseau invalides, parc absent, requêtes propriétaire, `404` sans fuite, filtres, curseur exclusif, ordre chronologique, index unique, DTO sans `userId`, routes authentifiées, `no-store`, header HTTP et enregistrement DI.

## Limites de la tranche

La modification, les transitions de statut, la suppression, les occurrences de ride et l'interface Angular ne font pas partie de PASS-04. PASS-05 ajoutera la création rapide responsive depuis une fiche parc et le profil en consommant exclusivement ce contrat.
