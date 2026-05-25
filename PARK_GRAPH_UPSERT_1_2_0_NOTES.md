# Park Graph Upsert 1.2.0

## Objectif

Ajout d'un flux d'import JSON général et partiel pour un parc complet ou tronqué, réservé à l'administration.

Le flux permet de prévisualiser puis d'appliquer un document JSON versionné contenant tout ou partie d'un graphe de parc : parc, références, zones, parkItems, détails d'attraction, conditions d'accès et rattachements d'images existantes.

## Endpoints

- `POST /admin/park-graph-upserts/preview`
- `POST /admin/park-graph-upserts/apply`

Les deux endpoints sont protégés par le rôle admin et par le filtre utilisateur activé/non bloqué.

## Sémantique de merge

- Champ absent : la donnée existante est conservée.
- Champ présent avec une valeur : la donnée est mise à jour.
- Champ présent à `null` : la donnée nullable est vidée quand le champ le permet.
- Tableaux localisés présents : fusion par `languageCode`, sans supprimer les langues absentes.
- Collections `zones` et `items` : upsert des éléments présents uniquement.
- `replaceCollections` est accepté dans le contrat mais reste non destructif dans cette version.

## Sécurité anti-erreur

- Pour modifier un parc existant, l'écran admin impose une recherche puis une sélection du parc afin de fournir `targetParkId`.
- La création sans parc cible nécessite `createIfMissing = true`.
- Le backend ne matche pas un parc existant par simple nom afin d'éviter les collisions.
- Chaque preview/apply est historisé dans la collection Mongo `parkGraphUpsertHistory`.
- Les actions sont aussi capturées par l'audit admin existant.

## Front admin

Ajout d'une entrée dédiée dans le panel admin : `Admin > Upsert JSON parc`.

Fonctions :

- recherche d'un parc existant ;
- sélection du parc cible ;
- éditeur JSON ;
- preview ;
- apply ;
- affichage des changements, avertissements et erreurs.
