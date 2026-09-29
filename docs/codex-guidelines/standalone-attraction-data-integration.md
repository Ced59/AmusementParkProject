# AmusementPark - StandaloneAttraction Data Integration

Version : **2026-09-30**

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
- pour Codex, utiliser exclusivement le workflow `PARK_DATA_EDITOR` et un JSON `standaloneAttractionGraph` avec bloc `migration` ; l’interface admin reste un outil humain et ne constitue jamais une solution de repli ;
- résoudre et vérifier les identifiants legacy par les recherches et exports bornés du workflow avant de lancer la migration ;
- ne masquer ou classer `NotRelevant` le parc legacy et son item qu'au cours de la migration contrôlée afin d’éviter deux fiches publiques concurrentes ;
- lorsque l’utilisateur a explicitement autorisé le nettoyage, supprimer ensuite les images et autres dépendances legacy, puis le parkItem, puis le parc artificiel parent avec `PreviewDeletion` et `ApplyDeletion`, après publication et contrôle anonyme de la nouvelle fiche ; ne considérer la migration terminée qu’après preuve de leur absence ;
- refuser la clôture du traitement tant que le legacy existe encore lorsqu'une suppression a été demandée, ou tant qu'une dépendance (image, zone, horaires, tarif, historique, carte officielle ou commentaire) bloque sa suppression ;
- contrôler la page publique autonome sur mobile et dans les huit langues avant toute annonce sociale : type et statut localisés, sous-type lisible, pays développé et noms des références résolus, sans enum brut ni GUID visible ; préserver les marques, noms de modèles et désignations techniques établies comme `Alpine coaster` au lieu de les surtraduire.

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
  "schemaVersion": "2026-09-30",
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
  "schemaVersion": "2026-09-30",
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

Dans le workflow Codex, tout document passe d’abord par `Preview`, puis par `Apply` avec le reçu exact uniquement lorsque `canApply: true`. L’état global `park-data-editor/operations/status` doit être disponible avant chaque opération intensive, conformément au workflow API dédié.

Un JSON exporté est conçu pour être réimporté sans effet de migration destructif : lorsque des IDs legacy sont présents, `retireLegacyPark` et `retireLegacyParkItem` valent `false`. Ne les passer à `true` que pour une migration initiale explicitement contrôlée.

Historique :

```json
{
  "history": {
    "events": [
      {
        "key": "bardonecchia-opening-2006",
        "entityType": "StandaloneAttraction",
        "ownerId": "standalone-attraction-id",
        "date": "2006",
        "eventType": "Opening",
        "isMajor": true,
        "isVisible": true,
        "titles": {
          "fr": "Ouverture de Bardonecchia Alpine Coaster",
          "en": "Bardonecchia Alpine Coaster opens"
        },
        "article": {
          "slug": "ouverture-bardonecchia-alpine-coaster",
          "isPublished": true,
          "blocks": [
            {
              "type": "Paragraph",
              "sortOrder": 1,
              "texts": {
                "fr": "Récit éditorial sourcé de l’ouverture.",
                "en": "A sourced editorial account of the opening."
              }
            }
          ],
          "sources": [
            {
              "label": "Source officielle",
              "url": "https://example.org/history"
            }
          ]
        }
      }
    ]
  }
}
```

L’export doit conserver l’article complet, notamment ses `blocks` et ses `sources`. Une réapplication en mode merge ne doit ni vider ces contenus ni rattacher l’événement à un parc artificiel.

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
- `attractionLocations` si l’entrée, la sortie ou les points d’accès sont fiables ;
- pour une attraction `Operating`, horaires propres à l’installation et tarifs actuels vérifiés ;
- coordonnées exactes permettant son inclusion dans le batch météo automatique.

Ne pas rattacher artificiellement :

- zones ;
- restaurants ou hôtels du domaine touristique ;
- bike park, adventure park, remontées mécaniques ou autres activités voisines ;
- horaires, tarifs ou météo du parc legacy.

## Informations Visiteurs Actuelles

Les sections `openingHours` et `pricing` sont prises en charge nativement par `standaloneAttractionGraph`. Elles utilisent les mêmes structures, exigences de source et contrôles de validité que pour un parc, mais ciblent l’attraction autonome.

Règles obligatoires :

- seul le statut `Operating` autorise des horaires, des tarifs ou une météo actuels ;
- dans chaque bloc, utiliser `standaloneAttractionId` et jamais `parkId` ;
- les horaires doivent décrire l’attraction elle-même, pas ceux d’une station, d’un domaine touristique ou d’une remontée voisine, sauf preuve qu’ils sont strictement identiques et officiellement applicables ;
- les tarifs doivent être actuels, sourcés, datés et conserver la devise officielle ; une grille générique du domaine ne doit pas être attribuée à l’attraction sans preuve ;
- une absence de donnée fiable laisse la carte publique correspondante masquée ; ne jamais inventer une plage horaire, un montant ou une saison ;
- une attraction qui quitte `Operating` ne doit plus exposer ses anciennes informations visiteurs, même si des données historiques restent stockées pour audit.

Reprendre les structures complètes et les contrôles des étapes 6 et 7 en remplaçant uniquement la propriété de rattachement `parkId` par `standaloneAttractionId`. Une section vide n’est pas une livraison utile et ne doit pas être envoyée pour simuler une couverture.

Le batch météo global inclut automatiquement toute attraction autonome visible, `Operating` et dotée de coordonnées valides. Les prévisions sont stockées dans la collection autonome et affichées sur sa page publique ; elles ne sont ni importées dans le JSON ni rattachées au parc legacy. Après publication d’une nouvelle attraction, vérifier le premier passage du batch et l’affichage anonyme. Une réponse météo encore vide avant ce passage est normale et doit masquer proprement la carte.

## Découverte Publique

Une attraction autonome visible et validée est cherchable hors de tout parc parent.

Elle doit apparaître :

- dans la recherche globale ;
- dans le filtre public `standaloneAttractions` affiché comme `Attractions isolées seules` ;
- dans le filtre public `attractionsWithStandalone` affiché comme `Attractions + isolées`, qui mélange les parkItems d’attraction et les attractions autonomes.

Ne pas créer un parc parent artificiel pour rendre l’attraction découvrable. Si l’attraction ne sort pas dans la recherche après application, contrôler la projection de recherche plutôt que de réintroduire un lien parc factice.

## Sortie Attendue

Chaque livraison doit indiquer :

- pourquoi l’attraction est autonome ;
- ce qui est migré depuis le parc legacy ;
- ce qui est exclu du domaine touristique plus large ;
- les contradictions de sources non tranchées ;
- le fichier JSON à prévisualiser puis appliquer par `PARK_DATA_EDITOR` ;
- la couverture des horaires, tarifs et du batch météo, avec les lacunes factuelles restantes ;
- la prochaine vérification après Preview puis le contrôle public anonyme.
