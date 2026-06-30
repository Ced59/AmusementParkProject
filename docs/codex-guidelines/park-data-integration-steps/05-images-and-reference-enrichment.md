# Étape 5 — Images et enrichissement des références

Objectif : ajouter les images fiables et enrichir les fondateurs, exploitants ou constructeurs sans créer de doublons ni d’images non importables.

## Lire avant de commencer

- `park-data-integration-orchestrator.md`
- `04-rich-descriptions-localization.md` pour les biographies et descriptions de références

## Export requis

Utiliser l’export actualisé après les items et descriptions concernés. Les `ownerKey` doivent correspondre aux clés déjà présentes ou aux références créées dans le même JSON.

## Ce que l’import permet techniquement

Le flux d’import d’images remote accepte plus qu’une URL de fichier “directe” classique. D’après le code actuel, une image peut être importée si toutes ces conditions sont vraies :

- `sourceUrl` est une URL absolue `http` ou `https` ;
- l’URL ne contient pas d’identifiants dans `UserInfo` ;
- l’hôte n’est pas `localhost` ;
- le DNS ou l’adresse IP résout vers une adresse publique ;
- les adresses loopback, privées, link-local, multicast, réservées ou équivalentes sont rejetées ;
- les redirections HTTP(S) sont acceptées jusqu’à 5 redirections ;
- chaque URL de redirection doit rester publique et HTTP(S) ;
- la réponse finale est un succès HTTP ;
- `Content-Length`, s’il est fourni, est strictement positif et inférieur ou égal à 10 Mo ;
- le flux téléchargé ne dépasse pas 10 Mo ;
- le contenu téléchargé n’est pas vide ;
- ImageSharp détecte un vrai format d’image dans les octets téléchargés ;
- l’extension de l’URL n’est pas obligatoire : le format détecté peut corriger ou ajouter l’extension ;
- le `Content-Type` peut être imprécis, par exemple `application/octet-stream`, si les octets sont bien une image ;
- les serveurs CDN, URLs signées, URLs avec paramètres ou URLs de transformation peuvent passer si elles respectent les conditions ci-dessus ;
- l’import envoie des headers proches d’un navigateur, mais une protection anti-hotlinking peut quand même bloquer le téléchargement ;
- les logos ne reçoivent jamais de watermark, même si `withWatermark` est demandé.

Les propriétaires importables par JSON upsert sont :

- `Park` ;
- `ParkItem` ;
- `ParkOperator` ;
- `AttractionManufacturer` ;
- `ParkFounder`.

## Images à privilégier éditorialement

Une image externe doit être :

- une URL stable quand c’est possible ;
- téléchargeable ;
- fidèle au parc ou à l’item ;
- créditable ;
- sans watermark non autorisé, sauf logo officiel ;
- issue d’une source fiable ou librement exploitable selon le contexte du projet.

Refuser ou éviter :

- page HTML qui ne renvoie pas directement une image au téléchargement ;
- preview qui ne renvoie pas les octets de l’image finale ;
- miniature trop petite quand une image de meilleure qualité est disponible ;
- image générique ;
- image dont l’élément représenté est douteux.
- image avec watermark, sauf logo officiel.

Ne pas utiliser de lien CDN interne du site comme source externe pour réimporter une image déjà stockée. Utiliser l’ID d’image existant dans ce cas.

## Propriétaires d’images

Utiliser :

- `ownerKey: "park"` pour le parc ;
- `ownerType: "ParkItem"` avec `ownerKey` d’item pour un parkItem ;
- `ownerKey: "manufacturer:key"` pour un constructeur ;
- `ownerKey: "operator:key"` pour un exploitant ;
- `ownerKey: "founder:key"` pour un fondateur.

Si le propriétaire ne peut pas être résolu, ne pas inclure l’image.

Ne jamais utiliser un UUID, un ID interne ou une valeur devinée comme `ownerKey` si l’export ne prouve pas que cette valeur est acceptée. Pour un parkItem, `ownerKey` doit correspondre à la clé ou à l’identifiant réellement attendu par l’import selon l’export et le modèle du JSON. En cas de doute, ne pas inclure l’image et signaler le blocage.

## Métadonnées image

Chaque image doit avoir, si possible :

- `key` ;
- `sourceUrl` ;
- `ownerKey` ou `ownerType` + `ownerKey` ;
- `category` ;
- `isPublished` ;
- `withWatermark` ;
- `setAsCurrent` si elle doit devenir logo ou image principale ;
- `description` interne courte ;
- `altTexts`, `captions`, `credits` dans les 8 langues quand l’image est publique.

Si une image est techniquement importable mais éditorialement fragile, ne pas l’ajouter : préférer une absence d’image à une image trompeuse, instable ou mal créditée.

## Références à enrichir

Enrichir seulement les références utiles :

- constructeurs réellement liés à des items ;
- exploitants du parc ;
- fondateurs documentés ;
- propriétaires si le modèle ou le contexte les prend en charge.

Les biographies doivent être génériques et réutilisables. Ne pas écrire une bio de constructeur centrée uniquement sur le parc en cours.

Pour les constructeurs majeurs, une bonne biographie peut couvrir l’origine, la période d’activité, les spécialités, les modèles marquants, l’influence dans l’industrie et des exemples connus. Pour une source limitée, rester prudent et plus court plutôt que remplir.

Ne pas modifier une biographie déjà validée explicitement, notamment Vekoma, sauf demande directe.

Ne pas transformer ces enrichissements en étape autonome. Cette étape 5 est le bloc prévu pour compléter les références déjà nécessaires au parc, aux parkItems ou aux images. Si une référence minimale manque parce qu’elle aurait dû être créée à l’étape 1 ou 3, la résoudre ici seulement si elle est indispensable au livrable d’images ou de biographies, et signaler l’écart dans `metadata.notes`.

## JSON attendu

Sections possibles :

- `images`
- `references.founders`
- `references.operators`
- `references.manufacturers`

```json
{
  "documentType": "AmusementParkParkGraphUpsert",
  "schemaVersion": "2026-06-30",
  "mode": "merge",
  "metadata": {
    "source": "codex-images-references",
    "targetParkId": "id-du-parc",
    "targetParkName": "Nom du parc",
    "step": "05-images",
    "notes": "Images Wikimedia Commons directes avec crédits localisés."
  },
  "identity": {
    "parkId": "id-du-parc",
    "name": "Nom du parc"
  },
  "images": [
    {
      "key": "park-main-image",
      "sourceUrl": "https://upload.wikimedia.org/example/image.jpg",
      "ownerKey": "park",
      "category": "Park",
      "isPublished": true,
      "withWatermark": false,
      "setAsCurrent": true,
      "description": "Vue du parc - source et licence.",
      "altTexts": [
        { "languageCode": "fr", "value": "Vue du parc." }
      ],
      "credits": [
        { "languageCode": "fr", "value": "Photo : auteur, source, licence." }
      ]
    }
  ]
}
```

## Contrôles avant livraison

- Toutes les URLs images sont techniquement importables selon les règles ci-dessus.
- Tous les propriétaires sont résolus.
- Les crédits sont lisibles pour un visiteur.
- Les logos ne sont pas confondus avec des photos.
- Les images historiques ne prétendent pas montrer une date ou un état qu’elles ne montrent pas.
- Les biographies ne créent pas de doublons de références.

## Après Apply

Demander l’export actualisé pour récupérer les IDs d’images avant de les référencer dans l’histoire.

À la fin de la réponse, ajouter `Pertinence de la prochaine étape` pour l’étape 6 — Horaires et événements nommés. Si aucun calendrier fiable n’existe ou si le parc est fermé sans horaires utiles, indiquer `probablement inutile` ou `à décider` avec la raison. Si l’étape 6 est `probablement inutile`, appliquer la règle de proche en proche de l’orchestrateur jusqu’à la prochaine étape officielle `utile` ou `à décider`, puis attendre la décision utilisateur.
