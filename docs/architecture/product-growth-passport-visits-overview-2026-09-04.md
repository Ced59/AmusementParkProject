# Passeport — consultation de l'historique privé des visites

Date : 2026-09-04

Roadmap : `docs/roadmaps/product-growth/02-visit-passport-and-ride-log-roadmap.md`

## Résultat

Le profil distingue maintenant deux intentions :

- « Voir mon passeport » ouvre la route privée et localisée `/:lang/profile/passport` ;
- « Ajouter une visite » conserve le parcours de création rapide existant.

La page du passeport affiche les visites du propriétaire, permet d'ouvrir leur éditeur et conserve le formulaire de création rapide. Elle couvre les états de chargement, liste vide, erreur initiale, erreur de page suivante et pagination par curseur.

## Flux et frontières

```text
Page Profil
  -> intention UI "ouvrir le passeport"
  -> route lazy /:lang/profile/passport + authGuard
  -> PassportVisitsOverviewStateFacade
  -> PassportVisitsOverviewApiPort
  -> GET /me/passport/visits?limit=20&cursor=...
  -> ListUserVisitsQueryHandler
  -> IUserVisitRepository.ListOwnedAsync (filtre propriétaire + curseur)
  -> IParkRepository.GetByIdsAsync (un seul lot de noms)
  -> projection HTTP légère
  -> mapper de vue pur et date localisée fidèle à sa précision
```

Le composant gère uniquement les intentions de présentation et de navigation. La façade possède l'état asynchrone, le curseur, la déduplication et le changement de langue. Le service HTTP possède l'URL privée et désactive `TransferState`. Le handler applicatif orchestre les deux ports sans déplacer de logique métier dans le contrôleur ou l'infrastructure.

## Confidentialité et projection

- l'endpoint reste authentifié et limité au propriétaire ;
- la route de compte demeure non indexable ;
- la liste ne renvoie ni le commentaire d'appréciation ni le texte de la note privée ;
- un booléen `HasPrivateNote` suffit à afficher l'indicateur de confidentialité ;
- le détail complet reste disponible uniquement lors de l'ouverture de la visite possédée ;
- aucun contenu privé n'entre dans le cache de transfert SSR.

## Performance

Une page réalise au plus :

1. une lecture Mongo paginée des visites du propriétaire ;
2. une lecture groupée des parcs distincts de la page.

Il n'existe donc pas de requête parc par carte. Une visite historique est conservée même si son parc ne peut plus être résolu ; l'identifiant devient alors le libellé de repli. Les pages suivantes sont ajoutées sans dupliquer un identifiant déjà affiché et une erreur de pagination ne détruit jamais la liste existante.

## Contrat responsive

La page et chaque cellule de grille ont `min-width: 0`, le conteneur est borné par `max-width: 100%`, les textes longs utilisent `overflow-wrap: anywhere` et l'hôte coupe tout débordement horizontal résiduel. La grille passe de deux à une colonne sous 760 px. Sous 520 px, l'en-tête, les métadonnées et toutes les actions utilisent la largeur disponible sans largeur minimale rigide.

Les tests de composant figent les bornes de largeur et les deux ruptures mobiles. La validation de déploiement doit contrôler au minimum 320, 360 et 390 px.

## Preuves automatisées

- handler : filtre propriétaire, curseur et hydratation groupée du nom du parc ;
- mapper HTTP : nom exposé, texte privé omis et indicateur conservé dans la liste ;
- data access : curseur encodé et transfert HTTP privé désactivé ;
- façade : premier chargement, pagination dédupliquée, erreur incrémentale non destructive et changement de langue sans nouvel appel API ;
- route : chargement paresseux et `authGuard` ;
- profil : deux actions distinctes, dont la navigation vers le passeport localisé ;
- présentation : page vide et règles de largeur mobile.
