# Étape 7 — Tarifs actuels et historique annuel

Objectif : intégrer une grille tarifaire actuelle, vérifiée et directement utile à la préparation d’une visite, puis conserver les relevés annuels antérieurs réellement sourcés afin de mesurer l’évolution d’un même produit sans présenter une ancienne offre comme encore valable.

## Lire avant de commencer

- `park-data-integration-orchestrator.md`
- `codex-park-data-editor-api-workflow.md` lorsque Codex exécute le parcours
- `04-rich-descriptions-localization.md` pour la qualité des textes localisés

## État de référence requis

Utiliser le registre consolidé et le statut courant confirmé à l’étape 1. Ne pas lancer d’export complet. Un export ciblé `Pricing` est admis uniquement pour vérifier une grille existante, résoudre une réponse de mutation ambiguë ou préparer une correction sans perte ; il ne devient pas une routine de fin d’étape.

La propriété racine canonique est `pricing`. L’alias legacy `parkPricing` peut être lu par l’importeur, mais tout nouveau lot doit écrire `pricing`. La section d’export correspondante est `Pricing`.

## Applicabilité selon le statut

Seul un parc dont le statut courant est `Operating` peut recevoir et publier une grille actuelle.

Pour `Planned`, `UnderConstruction`, `TemporarilyClosed`, `ClosedDefinitively` et `Cancelled` :

- ne pas livrer de section `pricing` ;
- ne pas recycler une ancienne grille, un prix d’annonce ou un tarif historique comme prix actuel ;
- conserver un changement tarifaire durable dans `history.events` à l’étape 8 s’il possède une vraie valeur historique ;
- conclure explicitement « étape 7 non applicable au statut courant », puis évaluer l’étape 8.

Si le statut est `Operating` mais qu’aucun tarif actuel fiable n’est accessible après une vraie recherche, ne rien inventer. Consigner la lacune, les familles de sources vérifiées et passer à l’étape 8.

## Sources et fraîcheur

Privilégier, dans cet ordre :

1. la page tarifaire officielle du parc ;
2. la billetterie officielle ;
3. les conditions de vente officielles lorsqu’elles précisent les catégories ou périodes ;
4. une communication officielle datée.

Vérifier la page finale après redirection et s’assurer qu’elle décrit encore l’offre courante. Une page d’archive, un extrait de moteur de recherche, un revendeur non officiel ou une grille sans année identifiable ne suffit pas pour publier un tarif actuel.

Renseigner :

- `sourceUrl` avec la page officielle qui permet de contrôler la grille ;
- `purchaseUrl` avec le lien général d’achat officiel lorsqu’il existe ;
- `lastVerifiedAtUtc` avec l’instant UTC réel de la vérification, jamais une date supposée ;
- `notes` sous forme de textes localisés dans les huit langues, seulement pour une information publique utile qui n’a pas de champ plus précis. Ne jamais y placer de commentaire d’audit, de consigne Codex ou de justification de source.

## Périmètre de la grille

Rechercher séparément :

- `admissionOffers` : billets d’entrée par catégorie réellement proposée, par exemple adulte, enfant, jeune ou senior ;
- `annualPasses` : pass annuels ou abonnements comparables ;
- `parkingOffers` : voiture, moto, camping-car ou autre offre de stationnement officiellement tarifée.

Chaque offre comporte :

- un `code` stable, en minuscules, unique dans sa collection ;
- un `sortOrder` cohérent avec l’ordre de présentation attendu ;
- des `labels` pour un billet ou un parking, ou des `names` pour un pass annuel ;
- au moins un prix `onlinePrice` ou `gatePrice` ;
- `validFrom` et `validTo` seulement lorsque la période est vérifiable ;
- des `conditions` localisées lorsque des restrictions, âges, tailles de groupe, justificatifs ou règles de canal modifient réellement l’offre ;
- un `purchaseUrl` spécifique lorsque l’offre possède une destination officielle distincte.

Les libellés, noms et conditions destinés au public sont rédigés naturellement dans les huit langues `fr`, `en`, `es`, `de`, `it`, `nl`, `pt`, `pl`. Ne pas traduire un nom commercial officiel, mais localiser l’explication utile autour de ce nom. Ne pas transformer une condition tarifaire en paragraphe promotionnel.

`audienceCategory` est obligatoire pour chaque entrée de `admissionOffers`. Utiliser un code stable et descriptif tel que `adult`, `child`, `senior`, `youth`, `student`, `family` ou `group` selon la catégorie réellement publiée. Ne pas déduire une catégorie qui n’existe pas dans la source.

## Devise, montants et modes

`currencyCode` est obligatoire et utilise exactement trois lettres majuscules compatibles ISO 4217, par exemple `EUR`, `GBP` ou `USD`. Pour la grille actuelle, utiliser la devise dans laquelle le parc publie réellement ses tarifs, normalement la devise locale de son pays. Ne jamais convertir la source en euros ou dans la devise de l’utilisateur. Le code ISO de la devise source doit rester explicite même lorsque son symbole paraît familier ou ambigu, par exemple `$`.

Tous les montants sont des nombres décimaux sans symbole de devise et doivent être supérieurs ou égaux à zéro. Ne jamais convertir manuellement une devise ni arrondir un prix source.

Modes canoniques :

- `Fixed` : `amount` est obligatoire ; `minimumAmount` et `maximumAmount` sont omis ou `null` ;
- `Range` : `minimumAmount` et `maximumAmount` sont obligatoires, `minimumAmount <= maximumAmount`, et `amount` est omis ou `null` ;
- `Dynamic` : `minimumAmount` et `maximumAmount` sont facultatifs, mais lorsqu’ils sont tous les deux présents le minimum ne dépasse pas le maximum ; `amount` est omis ou `null`.

Un prix dynamique sans borne est acceptable seulement lorsque la source confirme réellement une tarification variable sans publier de montant exploitable. Ne pas remplacer un prix inconnu par `Dynamic`.

Un canal absent reste `null` ou est omis. Ne pas recopier automatiquement le prix en ligne au guichet, ni l’inverse.

## Historique annuel

Lorsque des tarifs antérieurs fiables sont disponibles, les conserver dans `historicalSnapshots`. Chaque instantané représente une année et contient :

- un `year` unique compris entre 1900 et 9999 ;
- son propre `currencyCode`, même si la devise a changé depuis ;
- sa `sourceUrl`, sa date `lastVerifiedAtUtc` et ses éventuelles `notes` localisées ;
- ses propres `admissionOffers`, `annualPasses` et `parkingOffers`, avec le même contrat que la grille actuelle.

Pour qu’un produit alimente une courbe cohérente, conserver exactement le même `code` d’une année à l’autre lorsque l’offre reste fonctionnellement comparable. Un changement de nom commercial ne justifie pas à lui seul un nouveau code. En revanche, ne fusionner sous un même code ni des catégories différentes, ni un pass dont les droits ont matériellement changé, ni deux produits seulement ressemblants.

Ne pas inventer un prix annuel à partir d’un souvenir, d’un extrait de moteur de recherche ou d’une conversion. Une archive officielle, une billetterie datée, des conditions de vente datées ou une capture archivée fiable sont nécessaires. La devise historique reste celle de la source de l’époque. Si la devise change entre deux instantanés, conserver les deux codes ISO : l’interface présentera les montants séparément sans calculer une évolution monétaire trompeuse.

Un instantané doit contenir au moins une offre tarifée. Ne pas créer un objet vide pour « réserver » une année. Conserver au maximum 25 instantanés annuels par parc et privilégier les cinq dernières années pour une courbe publique utile, sans supprimer un historique antérieur déjà fiable.

## Dates et saisonnalité

Les dates de validité utilisent `YYYY-MM-DD`. Si `validFrom` et `validTo` sont présents, `validFrom` doit précéder ou égaler `validTo`.

Créer plusieurs offres avec des codes distincts lorsque la source publie de vraies saisons ou périodes tarifaires différentes. Ne pas fabriquer un calendrier journalier à partir d’un simple « à partir de ». Les tarifs variables par date peuvent rester `Dynamic` avec leurs bornes publiées et des conditions explicites.

## JSON attendu

Section principale : `pricing`.

```json
{
  "documentType": "AmusementParkParkGraphUpsert",
  "schemaVersion": "2026-06-30",
  "mode": "merge",
  "metadata": {
    "source": "codex-pricing",
    "targetParkId": "id-du-parc",
    "targetParkName": "Nom du parc",
    "step": "07-pricing",
    "notes": "Grille vérifiée sur les pages officielles du parc."
  },
  "identity": {
    "parkId": "id-du-parc",
    "name": "Nom du parc"
  },
  "pricing": {
    "parkId": "id-du-parc",
    "currencyCode": "EUR",
    "sourceUrl": "https://example.com/tarifs",
    "purchaseUrl": "https://example.com/billetterie",
    "lastVerifiedAtUtc": "2026-08-09T10:00:00Z",
    "notes": [],
    "admissionOffers": [
      {
        "code": "adult-high-season",
        "audienceCategory": "adult",
        "labels": [
          { "languageCode": "fr", "value": "Adulte — haute saison" },
          { "languageCode": "en", "value": "Adult — high season" },
          { "languageCode": "es", "value": "Adulto — temporada alta" },
          { "languageCode": "de", "value": "Erwachsene — Hochsaison" },
          { "languageCode": "it", "value": "Adulto — alta stagione" },
          { "languageCode": "nl", "value": "Volwassene — hoogseizoen" },
          { "languageCode": "pt", "value": "Adulto — época alta" },
          { "languageCode": "pl", "value": "Dorosły — wysoki sezon" }
        ],
        "onlinePrice": {
          "mode": "Fixed",
          "amount": 39
        },
        "gatePrice": {
          "mode": "Fixed",
          "amount": 45
        },
        "validFrom": "2026-07-01",
        "validTo": "2026-08-31",
        "purchaseUrl": null,
        "conditions": [],
        "sortOrder": 10
      }
    ],
    "annualPasses": [],
    "parkingOffers": [],
    "historicalSnapshots": [
      {
        "year": 2025,
        "currencyCode": "EUR",
        "sourceUrl": "https://example.com/archives/tarifs-2025",
        "lastVerifiedAtUtc": "2026-08-10T10:00:00Z",
        "notes": [],
        "admissionOffers": [
          {
            "code": "adult-high-season",
            "audienceCategory": "adult",
            "labels": [
              { "languageCode": "fr", "value": "Adulte — haute saison" },
              { "languageCode": "en", "value": "Adult — high season" },
              { "languageCode": "es", "value": "Adulto — temporada alta" },
              { "languageCode": "de", "value": "Erwachsene — Hochsaison" },
              { "languageCode": "it", "value": "Adulto — alta stagione" },
              { "languageCode": "nl", "value": "Volwassene — hoogseizoen" },
              { "languageCode": "pt", "value": "Adulto — época alta" },
              { "languageCode": "pl", "value": "Dorosły — wysoki sezon" }
            ],
            "onlinePrice": { "mode": "Fixed", "amount": 36 },
            "gatePrice": { "mode": "Fixed", "amount": 42 },
            "conditions": [],
            "sortOrder": 10
          }
        ],
        "annualPasses": [],
        "parkingOffers": []
      }
    ]
  }
}
```

L’exemple inclut les huit langues requises. Un lot incomplet est rejeté en Preview avant toute écriture.

## Preview et Apply

Le bloc `pricing` passe par la même boucle bornée que les autres sections : état global disponible, Preview, lecture de chaque changement, absence d’erreur, puis Apply avec le reçu correspondant.

Sont bloquants :

- un parc cible non `Operating` ;
- un `pricing.parkId` différent du parc cible ;
- une devise absente ou invalide ;
- un code vide ou dupliqué dans la même collection ;
- une offre sans prix en ligne ni prix au guichet ;
- un montant négatif ;
- un `Fixed` sans `amount` ;
- un `Range` sans ses deux bornes ou avec une plage inversée ;
- un `Dynamic` dont les deux bornes sont inversées ;
- une période de validité inversée ;
- une structure de tableau, de date ou de prix refusée par le Preview.
- plus de 25 instantanés historiques, une année invalide ou dupliquée, une devise historique invalide ou un instantané sans offre tarifée ;

Une section `pricing` sans aucune offre n’efface pas une grille existante et ne constitue pas un lot utile. La commande courte de complétude n’autorise pas la suppression d’une grille existante ; toute suppression ou dépublication exige un périmètre explicite distinct.

Lors d’une fusion de parcs, la grille du parc source est rattachée au parc cible lorsque celui-ci n’en possède pas. Si les deux parcs possèdent déjà une grille, celle de la cible est conservée par défaut et le Preview émet un avertissement. Utiliser explicitement `sections.pricing: "source"` pour remplacer la grille cible par celle de la source. Après une fusion appliquée, aucune grille ne doit rester rattachée à l’identifiant source supprimé.

## Contrôles avant de terminer

- Le statut du parc est `Operating`.
- La source et la billetterie officielles ont été consultées au moment du lot.
- La devise et chaque montant correspondent exactement à la source.
- La grille actuelle utilise la devise réellement publiée par le parc et son code ISO reste visible ; aucune conversion n’a remplacé le montant source.
- Chaque instantané historique possède une année unique, sa propre devise et une source datée fiable.
- Les codes produit restent stables entre années uniquement pour des offres réellement comparables.
- Chaque offre possède un canal de prix au minimum.
- Les modes et leurs champs correspondent au contrat `Fixed` / `Range` / `Dynamic`.
- Les périodes sont cohérentes et n’extrapolent pas la source.
- Les catégories, libellés, noms et conditions sont spécifiques et localisés.
- Les codes sont stables et uniques dans leur collection.
- Les liens HTTP(S) pointent vers la bonne page officielle.
- `lastVerifiedAtUtc` reflète la vérification réelle.
- Le Preview ne produit aucune erreur et l’Apply est intégré au registre consolidé.

## Après Apply

Contrôler la réponse Apply et reporter dans le registre consolidé la devise, le nombre d’offres par collection, les périodes, les URLs et l’instant de vérification. Continuer sans export complet ; demander un export ciblé `Pricing` uniquement si la réponse est ambiguë ou si une vérification précise l’exige.

À la fin de la réponse, ajouter `Pertinence de la prochaine étape` pour l’étape 8 — Histoire du parc et des parkItems. En mode ChatGPT, attendre la validation utilisateur. En mode Codex autonome, continuer selon l’orchestrateur. L’unique export complet reste réservé au passage de l’étape 8 à l’audit final de l’étape 9.
