# AmusementPark - StandaloneAttraction Data Integration

Version : **2026-07-16**

Ce guide remplace le parcours parc 1 à 8 quand l’entité pertinente est une attraction fixe isolée : alpine coaster hors parc, luge sur rail durable, grande roue permanente, attraction mécanique exploitée seule, ou installation similaire.

## Quand Utiliser Ce Flux

Utiliser `StandaloneAttraction` si toutes ces conditions sont vraies :

- l’attraction est fixe, durable et exploitée comme lieu visitable ;
- elle n’appartient pas à un parc d’attractions structuré avec zones et inventaire de parkItems ;
- les activités voisines relèvent d’un domaine touristique plus large et ne doivent pas être importées comme parc artificiel ;
- une page publique autonome est plus juste qu’une fiche parc contenant un seul item.

Ne pas utiliser ce flux pour une attraction foraine itinérante, un événement temporaire, un parcours saisonnier démonté, ou une attraction clairement située dans un vrai parc existant.

## Migration Legacy

Si une attraction isolée existe déjà comme parc mono-attraction :

- conserver les IDs legacy dans la décision d’étape 0 ;
- créer ou mettre à jour une fiche `standaloneAttraction` ;
- renseigner `legacyParkId` et `legacyParkItemId` ;
- utiliser l’interface admin `Attractions isolées` ou un JSON `standaloneAttractionGraph` avec bloc `migration` ;
- masquer ou retirer le parc legacy et son item seulement après migration contrôlée.

Exemple Bardonecchia :

| Entité legacy | ID |
| --- | --- |
| Parc legacy | `b2ddc5c4-bfa5-430b-bcbb-5ba8c6a183cb` |
| Attraction legacy | `bb146495-2321-454b-9f02-f2f71c6becf6` |

## Contrat JSON

Utiliser `documentType: "standaloneAttractionGraph"`.

Structure minimale :

```json
{
  "documentType": "standaloneAttractionGraph",
  "schemaVersion": "2026-07-16",
  "mode": "merge",
  "identity": {
    "standaloneAttractionId": "id-if-known",
    "legacyParkId": "legacy-park-id-if-any",
    "legacyParkItemId": "legacy-item-id-if-any"
  },
  "standaloneAttraction": {
    "name": "Bardonecchia Alpine Coaster",
    "countryCode": "IT",
    "type": "RollerCoaster",
    "subtype": "Alpine coaster",
    "isVisible": false,
    "adminReviewStatus": "ToReview"
  }
}
```

Migration contrôlée :

```json
{
  "documentType": "standaloneAttractionGraph",
  "schemaVersion": "2026-07-16",
  "mode": "merge",
  "migration": {
    "legacyParkId": "b2ddc5c4-bfa5-430b-bcbb-5ba8c6a183cb",
    "legacyParkItemId": "bb146495-2321-454b-9f02-f2f71c6becf6",
    "targetStandaloneAttractionId": null,
    "retireLegacyPark": true,
    "retireLegacyParkItem": true
  },
  "standaloneAttraction": {
    "name": "Bardonecchia Alpine Coaster",
    "countryCode": "IT",
    "type": "RollerCoaster",
    "isVisible": false,
    "adminReviewStatus": "ToReview"
  }
}
```

Images :

```json
{
  "images": [
    {
      "ownerType": "StandaloneAttraction",
      "ownerKey": "standaloneAttraction",
      "category": "StandaloneAttraction",
      "sourceUrl": "https://example.org/photo.jpg",
      "isPublished": false,
      "altTexts": [
        { "languageCode": "fr", "value": "Bardonecchia Alpine Coaster dans les bois de Campo Smith" },
        { "languageCode": "en", "value": "Bardonecchia Alpine Coaster in the Campo Smith woods" }
      ]
    }
  ]
}
```

`ownerKey` accepte `standaloneAttraction`, `standalone-attraction`, `attraction` ou `standalone-attraction:<id-or-key>` pour cette fiche.

## Données À Renseigner

Priorité :

- `name`, `countryCode`, `type`, `subtype` ;
- adresse structurée : `street`, `city`, `postalCode`, coordonnées ;
- `operatorId` si l’exploitant existe ou est créé dans `references.operators` ;
- `websiteUrl` officiel ;
- descriptions localisées dans les 8 langues quand les sources sont assez solides ;
- `attractionDetails` : constructeur, modèle, statut, dates, longueur, vitesse, durée, capacité, conditions d’accès ;
- `attractionLocations` si l’entrée, la sortie ou les points d’accès sont fiables.

Ne pas rattacher artificiellement :

- zones ;
- restaurants ou hôtels du domaine touristique ;
- bike park, adventure park, remontées mécaniques ou autres activités voisines ;
- horaires du parc legacy.

## Horaires

Le modèle d’horaires actuel est encore attaché aux parcs. Pour une attraction isolée, ne pas stocker les horaires sur l’ancien parc legacy pour contourner cette limite.

Tant que le modèle d’horaires autonome n’est pas disponible :

- conserver les horaires dans les notes de livraison et sources ;
- ne pas publier une route d’horaires autonome ;
- ne pas dupliquer un calendrier de domaine touristique comme s’il appartenait à l’attraction seule.

## Sortie Attendue

Chaque livraison doit indiquer :

- pourquoi l’attraction est autonome ;
- ce qui est migré depuis le parc legacy ;
- ce qui est exclu du domaine touristique plus large ;
- les contradictions de sources non tranchées ;
- le fichier JSON ou l’action admin à appliquer ;
- la prochaine vérification après Preview.
