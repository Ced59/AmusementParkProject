# Étape 7 — Histoire du parc, des parkItems et articles

Objectif : créer une histoire fiable, sourcée et lisible, en séparant les événements du parc, les événements des parkItems et les articles longs.

## Lire avant de commencer

- `park-data-integration-orchestrator.md`
- `park-graph-upsert-enums.md`
- `04-rich-descriptions-localization.md` pour le style public des résumés

## État de référence requis

Dans les deux modes, utiliser le registre consolidé et les réponses validées des étapes d’inventaire, de descriptions, d’images et d’horaires, sans nouvel export complet. Les timelines doivent pouvoir référencer les vrais IDs ou les `itemKey` existants ; demander seulement la section nécessaire lorsqu’un identifiant manque réellement.

## Découpage recommandé

Pour un parc riche :

1. Timeline du parc sans articles longs.
2. Articles majeurs du parc.
3. Timeline des parkItems majeurs encore dans le parc.
4. Timeline des parkItems fermés ou relocalisés.
5. Articles de parkItems seulement pour les cas importants.

Ne pas écrire toute l’histoire d’un grand parc et de toutes ses attractions en un seul JSON.

## Profondeur historique obligatoire

Pour un parc majeur, historiquement riche ou fermé, une timeline limitée à la fondation et à l’ouverture est insuffisante. La recherche doit couvrir, quand les sources existent :

- origine du projet, fondateurs et ouverture ;
- premières attractions ou zones structurantes ;
- extensions et nouveautés qui ont changé l’identité du parc ;
- changements d’exploitant, de propriétaire, de nom ou de thème ;
- attractions emblématiques définitivement fermées, remplacées, démolies ou relocalisées ;
- rénovations, interruptions et relances réellement structurantes ;
- annonces récentes encore pertinentes au moment de la recherche : nouveauté majeure, fermeture, remplacement, transformation, acquisition ou développement futur confirmé.

Croiser l’historique officiel avec la presse, les archives et les sources spécialisées fiables. Le site actuel du parc est rarement suffisant pour retrouver les anciennes attractions. La profondeur attendue dépend des faits documentables, mais le travail de recherche doit rester aussi rigoureux pour chaque parc complété.

À la fin de la recherche, comparer la timeline obtenue à l’inventaire des attractions définitivement fermées de l’étape 3. Toute fermeture emblématique sans jalon, source ou explication doit être corrigée ou inscrite comme lacune explicite.

Le cycle de vie du parc doit être explicable par la timeline quand les sources existent : `Announcement` pour `Planned`, `ConstructionStart` ou jalons pour `UnderConstruction`, `TemporaryClosure` pour `TemporarilyClosed`, `DefinitiveClosure` pour `ClosedDefinitively`, et un événement documentant l’annulation ou l’abandon pour `Cancelled` avec le type historique disponible le plus fidèle. Le statut du parc reste porté par `park.status`; un événement historique ne le remplace pas.

## Statut courant du parkItem vs événements historiques

Le même principe s’applique aux attractions, avec une séparation stricte entre l’état courant et les faits de timeline :

- `items[].attractionDetails.status` répond uniquement à la question « dans quel état cette attraction se trouve-t-elle maintenant dans ce parc ? » ;
- `history.events[]` répond à « quels faits durables lui sont arrivés, quand et dans quel contexte ? ».

Le statut courant doit rester dans le vocabulaire lifecycle contrôlé : `Operating`, `UnderConstruction`, `TemporarilyClosed`, `ClosedDefinitively`, `Removed`, `Planned`, `Unknown`.

Une transformation n’est jamais un statut. `Retracké`, `Délocalisé`, `Relocalisé`, `Rénové`, `Reconstruit`, `Renommé`, `Rethemé`, `Démonté`, `Stocké`, `Vendu`, `Transféré`, `Réinstallé`, `Remplacé`, `Démoli` et leurs équivalents dans d’autres langues doivent être exprimés par les types d’événements de parkItem disponibles.

Matrice de décision :

| Fait documenté | Statut courant typique | Timeline |
| --- | --- | --- |
| Retrack terminé et attraction rouverte | `Operating` | `Retrack`, et `Reopening` si pertinent |
| Retrack, rehab ou rénovation en cours avec fermeture | `TemporarilyClosed` | `Retrack`, `Rehab` ou `Refurbishment` |
| Changement de nom | état courant inchangé | `Rename` |
| Changement de thème ou d’histoire | état courant inchangé | `ThemeChange` ou `StoryChange` |
| Modification importante du parcours ou système | état courant selon exploitation | `LayoutChange` ou `RideSystemChange` |
| Déplacement dans le même parc puis réouverture | `Operating` | `RelocationDeparture`, `RelocationArrival`, éventuellement `Reinstallation` |
| Départ vers un autre parc | `Removed` dans le parc d’origine | `RelocationDeparture`, `Transfer` ou `Sale` |
| Démontage puis stockage | `Removed` | `Dismantling`, puis `Storage` |
| Démolition | `Removed` | `Demolition` |
| Fermeture définitive, installation encore présente | `ClosedDefinitively` | `DefinitiveClosure` |
| Remplacement par une autre attraction | `ClosedDefinitively` ou `Removed` selon la présence réelle | `Replacement`; la nouvelle attraction possède son propre statut |

Si l’étape 7 découvre dans l’état de référence une ancienne valeur historique stockée comme `attractionDetails.status`, ne pas la recopier et ne pas la supprimer silencieusement. La reprise doit :

1. rechercher l’état lifecycle actuel ;
2. corriger le statut dans un lot ciblé relevant de l’étape 3 ;
3. créer ou vérifier ici le ou les événements qui préservent le fait historique ;
4. conserver une précision de date prudente (`Year`, `Month`, `Day`) selon la source, sans inventer une date exacte.

En mode Codex autonome, cette reprise ciblée n’exige pas de revenir manuellement au début du workflow : la consigner comme correction de l’étape 3, appliquer le lot borné correspondant, puis continuer l’étape 7. En mode ChatGPT guidé, livrer clairement la correction ciblée avant de considérer l’historique comme complet.

## Événements de parc

Créer des événements pour les faits durables :

- fondation ;
- annonce ;
- construction ;
- ouverture ;
- ouverture de zone ;
- transformation d’ensemble du parc liée à plusieurs parkItems, sans dupliquer l’ouverture ou la fermeture propre à l’un d’eux ;
- changement d’exploitant ;
- acquisition ;
- extension ;
- fermeture temporaire marquante ;
- fermeture définitive ;
- démolition ;
- relance ou reconversion ;
- événement nommé durable ;
- incident important seulement s’il est sourcé et pertinent.

Ne pas créer d’événement historique pour une variation horaire générique.

## Événements de parkItem

Créer des événements pour :

- annonce ;
- construction ;
- tests ;
- ouverture ;
- soft opening si documentée ;
- fermeture temporaire ou définitive ;
- rénovation majeure ;
- retrack, rehab ou modification du parcours ;
- changement de thème, d’histoire ou de nom ;
- changement de trains, véhicules, système, modèle ou constructeur ;
- relocalisation ;
- démontage ;
- stockage ;
- vente ou transfert ;
- réinstallation ;
- remplacement ;
- démolition ;
- conservation patrimoniale.

Pour une attraction déplacée, la timeline du parkItem peut continuer hors du parc d’origine. Utiliser `contextParkId` quand l’événement se déroule dans un autre parc connu, ou un marqueur externe seulement si le modèle l’accepte et que le contexte est clair.

### Propriété des ouvertures et fermetures de parkItems

L’ouverture, la réouverture, la fermeture temporaire et la fermeture définitive d’un parkItem appartiennent exclusivement à la timeline de ce parkItem. Ne jamais dupliquer le même fait dans la timeline du parc, même lorsque le parkItem est emblématique ou que l’événement est majeur.

La timeline du parc peut documenter une transformation d’ensemble, une extension ou une période qui concerne plusieurs parkItems seulement si le fait possède une portée propre à l’échelle du parc. Cet événement de parc ne doit alors ni reprendre comme fait principal l’ouverture ou la fermeture d’un parkItem individuel, ni dupliquer ce même fait sous un autre titre. Des faits indépendants peuvent partager une date exacte lorsqu’elle est sourcée ; utiliser les rattachements vers les parkItems concernés pour conserver le contexte.

## Résolution des propriétaires d’événements history

Pour un événement d’histoire rattaché à un parkItem existant, ne jamais livrer seulement `itemKey` ou `parkItemKey`.

Format obligatoire recommandé :

```json
{
  "entityType": "ParkItem",
  "owner": "parkItem",
  "ownerId": "id-du-parkItem",
  "parkItemId": "id-du-parkItem",
  "itemId": "id-du-parkItem",
  "parkId": "id-du-parc",
  "contextParkId": "id-du-parc"
}
```

`itemKey` / `parkItemKey` est toléré uniquement en complément, pas comme seul mécanisme de résolution, sauf si le même JSON contient une section `items[]` minimale qui enregistre explicitement le parkItem.

Pour un événement de parc :

```json
{
  "entityType": "Park",
  "owner": "park",
  "ownerId": "id-du-parc",
  "parkId": "id-du-parc"
}
```

Un Preview avec `Impossible de résoudre le propriétaire de l’événement history` ou `Impossible de resoudre le proprietaire de l'evenement history` est bloquant.

## Articles

Créer un article uniquement si le sujet mérite un développement durable :

- histoire complète du parc ;
- ouverture majeure ;
- fermeture définitive ;
- démolition ;
- relocalisation complexe ;
- attraction emblématique ;
- nouveauté importante ;
- fermeture ou transformation marquante ;
- constructeur ou exploitant majeur ;
- transformation historique ;
- visite ou média original.
- captation photo ou vidéo originale ;
- page de patrimoine de loisirs ;
- actualité durable ;
- comparaison historique documentée.

Un article ne doit pas répéter la description ni devenir une fiche technique.

Une annonce d’attraction peut exposer son année, son récit, son implantation et sa portée durable sans recopier la fiche du constructeur. Vitesse, durée, capacité, nombre de sièges ou de véhicules, détail du tracé, rotations et accélérations restent dans les données structurées. Un chiffre ne rejoint le récit que s’il constitue lui-même le fait historique ou l’angle éditorial, jamais pour donner artificiellement une impression de précision.

Pour un parc majeur, un article est attendu pour chaque sujet qui mérite réellement un développement durable, notamment une annonce récente structurante, une transformation majeure ou la fermeture définitive d’une attraction emblématique. Cette obligation ne justifie aucun article de remplissage : les micro-changements restent de simples événements ou sont omis.

Mauvais sujets :

- micro-changement sans intérêt durable ;
- article qui répète la description ;
- texte sans source ;
- contenu promotionnel vide ;
- fiche technique transformée en article ;
- sujet artificiel créé pour remplir le site.

Structure recommandée :

1. Introduction courte.
2. Contexte du parc, de l’attraction ou de l’événement.
3. Développement chronologique ou thématique.
4. Informations vérifiées et sources.
5. Impact sur l’histoire, la visite ou la page du parc.
6. Conclusion naturelle.

### Profondeur et mise en forme

Pour un parc majeur, les repères suivants détectent un contenu trop mince :

- résumé de timeline : environ 25 à 55 mots visibles dans la langue de rédaction, généralement une à trois phrases ; il énonce le fait, son contexte immédiat et ce qu’il change durablement, sans recopier seulement le titre et la date ;
- sous-titre d’article : spécifique au sujet, assez précis pour annoncer l’angle, jamais une formule de série interchangeable ;
- résumé d’article : environ 45 à 90 mots, autonome et narratif ;
- article durable ciblé : au moins 250 mots visibles, résumé compris, avec normalement 3 intertitres et 3 paragraphes développés ;
- grand article de synthèse sur le parc ou une transformation structurante : environ 500 à 900 mots visibles, plusieurs périodes ou angles, et une alternance lisible de titres, paragraphes et images contextualisées quand elles existent.

Un sujet très circonscrit peut rester entre 150 et 250 mots si deux sections suffisent réellement. L’exception doit être motivée par le périmètre du sujet, jamais par le manque de rédaction. Ces bandes sont des alertes de relecture : elles n’autorisent ni répétition, ni fiche technique, ni paragraphes de remplissage.

Les huit langues reprennent le même nombre de blocs éditoriaux, les mêmes faits et une profondeur comparable. Les images ne remplacent pas un développement écrit ; inversement, ajouter des blocs vides ou des intertitres génériques ne rend pas l’article substantiel.

Le style doit être naturel, clair, agréable à lire, documenté, orienté lecteur, non promotionnel, non mécanique et non académique.

## Style obligatoire des articles historiques

Un article historique doit raconter le fait comme un contenu éditorial public, pas comme une justification de méthode.

Interdits dans les titres, sous-titres, résumés, paragraphes et légendes :

- “l’article n’a pas pour but…” ;
- “sans dramatisation” ;
- “ce n’est pas du sensationnalisme” ;
- “aucune image de scène non graphique et réutilisable n’a été retenue” ;
- “image contextuelle” comme excuse principale ;
- “repère documentaire prudent” ;
- “présence publique confirmée” ;
- “source faible” ;
- “selon la stratégie de prudence” ;
- toute phrase expliquant pourquoi le rédacteur a choisi d’écrire ou de ne pas écrire quelque chose.
- les sous-titres de série interchangeables comme « dates, contexte et portée » répétés d’un article à l’autre ;
- les résumés de fermeture qui se bornent à dire que l’attraction « reste documentée dans l’histoire du parc » sans raconter le fait, sa période ou sa transformation.
- les résumés d’ouverture ou d’annonce qui se bornent à dire que le lieu « ouvre et élargit l’offre du parc » ou qu’un projet est « annoncé comme développement futur », ainsi que leurs équivalents traduits.

Bon style :

- titre clair, humain, spécifique ;
- résumé qui raconte le fait et son intérêt historique ;
- paragraphes narratifs courts, factuels, sans effet dramatique ;
- les limites documentaires restent dans `metadata.notes` ou dans les sources, pas dans le texte public ;
- les événements sensibles sont factuels, sobres et précis, mais pas défensifs.

Pour un incident ou accident :

- créer un article si l’événement est sourcé, durable et utile à l’histoire du parc ou du parkItem ;
- tout incident ou accident trouvé sur un parkItem doit obligatoirement faire l’objet d’un article quand l’événement est sourcé et retenu dans la timeline ;
- associer une photo contextualisée si une image acceptable est trouvable ;
- utiliser le type `Accident` ou `Incident` selon les sources et les enums disponibles ;
- distinguer les faits établis, les suites opérationnelles et les zones non établies sans transformer l’article en note d’audit ;
- éviter les détails médicaux ou personnels non nécessaires ;
- si un détail personnel public est central pour comprendre une décision d’exploitation ou d’accessibilité, le mentionner sobrement et uniquement avec source solide.

Images d’incident ou accident :

- chercher d’abord une photo réelle de l’événement, du lieu ou de l’intervention, si elle existe, si elle est non graphique et si ses droits permettent l’import ;
- ne jamais utiliser d’image gore, humiliante, intrusive, sensationnaliste ou centrée sur une victime identifiable ;
- si aucune photo réelle réutilisable n’existe, utiliser une image de contexte de l’attraction ou du lieu, mais la légende doit rester naturelle ;
- ne pas écrire une légende défensive du type “aucune image de scène non graphique…” ;
- écrire plutôt une légende factuelle : “El Loco dans Adventuredome. La vue permet de situer la montagne russe concernée par l’incident de 2019.”

## Images créées ou utilisées dans un lot d’article

Le propriétaire d’une image et la référence de cette image depuis un article sont deux résolutions indépendantes. Un événement, un article, `mainImageKey`, `imageKey` ou `imageKeys` ne renseigne jamais le propriétaire de l’objet `images[]`.

Chaque jalon visible et chaque article doit recevoir une image contextualisée quand une image acceptable existe. Rechercher d’abord une image du fait, de l’attraction ou du lieu concerné à la bonne période ; une vue plus générale n’est acceptable que si elle situe honnêtement le sujet. Si aucune image n’est trouvable après recherche réelle, conserver l’exception dans l’audit au lieu d’utiliser une illustration trompeuse.

Les textes alternatifs et légendes suivent la charte de l’étape 4. Ils racontent naturellement ce que l’image montre et son lien avec le sujet ; ils ne mentionnent jamais l’import, la recherche, le choix par défaut, la faiblesse d’une source ou l’absence d’une autre photo.

Le processeur traite les sections dans cet ordre :

1. `items[]` et `references` enregistrent les clés de propriétaires ;
2. `images[]` résout les propriétaires et enregistre les clés d’images disponibles ;
3. `history.events` et les articles utilisent les IDs ou clés d’images enregistrés.

### Propriétaire de chaque image

Chaque image créée dans le lot doit suivre exactement les règles de l’étape 5 :

- `ownerType: "Park"` et `ownerKey: "park"` pour le parc cible ;
- `ownerType: "ParkItem"` et un `ownerKey` égal à la valeur exacte d’une entrée `items[].key` du même JSON ;
- `ownerType` de référence et `ownerKey` préfixé, avec la référence correspondante dans le même JSON ;
- jamais `ownerId` seul pour tenter de résoudre un parkItem, un exploitant, un fondateur ou un constructeur.

### Référence depuis l’article

Pour une image déjà présente dans l’état de référence, utiliser son ID :

- `mainImageId` ;
- `blocks[].imageId` ;
- `blocks[].imageIds`.

Pour une image distante créée dans le même JSON, définir une `images[].key` stable et unique après suppression des espaces de bord et sans tenir compte de la casse, puis recopier exactement cette valeur dans :

- `mainImageKey` ;
- `blocks[].imageKey` ;
- `blocks[].imageKeys`.

Ne jamais utiliser la clé d’une image créée dans un lot précédent. Le processeur ne précharge pas les clés de toutes les images existantes. Utiliser l’ID renvoyé par l’Apply ou l’import et conservé dans le registre consolidé ; exporter uniquement la section `Images` si cette réponse est inexploitable. Aucun export complet intermédiaire n’est requis.

Une image existante peut techniquement enregistrer une clé dans le lot courant si `images[]` contient à la fois son `imageId` et `key`, mais ChatGPT doit préférer l’ID direct dans l’article afin d’éviter une résolution indirecte inutile.

### Limite du Preview et contrôle obligatoire de ChatGPT

Pendant un Preview, une nouvelle image distante n’a pas encore d’ID importé. Sa `images[].key` n’est donc pas enregistrée pour l’article. De plus, les avertissements `La clé image '…' est introuvable` ne sont ajoutés par le processeur que pendant Apply.

Un Preview sans avertissement ne prouve donc pas que les clés d’images de l’article sont correctes. Avant livraison, ChatGPT doit effectuer ce contrôle statique :

1. lister toutes les définitions `images[].key` du JSON ;
2. supprimer leurs espaces de bord, les comparer sans tenir compte de la casse et vérifier qu’elles restent uniques ;
3. lister toutes les références `mainImageKey`, `imageKey` et `imageKeys` ;
4. vérifier caractère par caractère que chaque référence possède exactement une définition dans le même JSON ;
5. utiliser un ID exporté à la place pour toute image qui n’est pas créée dans ce lot.

Après Apply, tout avertissement `clé image introuvable` indique un livrable incorrect et impose une correction. Ne jamais présenter le Preview comme une validation de ces clés.

Exemple minimal d’une image de parkItem créée avec un article :

```json
{
  "items": [
    {
      "id": "id-exporte-du-parkItem",
      "key": "id-exporte-du-parkItem",
      "name": "Nom exact exporté du parkItem"
    }
  ],
  "images": [
    {
      "key": "article-context-image",
      "sourceUrl": "https://example.org/photo.jpg",
      "ownerType": "ParkItem",
      "ownerKey": "id-exporte-du-parkItem",
      "category": "ParkItem"
    }
  ],
  "history": {
    "events": [
      {
        "entityType": "Park",
        "owner": "park",
        "ownerId": "id-du-parc",
        "key": "article-history-event",
        "eventType": "ConstructionMilestone",
        "date": "2026",
        "article": {
          "mainImageKey": "article-context-image",
          "blocks": [
            {
              "type": "Image",
              "imageKey": "article-context-image"
            }
          ]
        }
      }
    ]
  }
}
```

## Style des événements et articles

Les titres, résumés et blocs d’articles visibles publiquement doivent se lire comme un récit utile pour un visiteur curieux, pas comme une note d’audit ou une justification interne.

Pour un événement, écrire ce qui s’est passé, ce que cela change dans l’histoire du parc ou de l’attraction, et pourquoi le fait est intéressant à retenir. La prudence documentaire doit rester dans les sources, la précision de date ou les notes de livraison, pas dans le texte public.

Interdits dans les résumés et articles publics :

- formules mécaniques du type “à cette date, le parc présente déjà…” ;
- “repère documentaire prudent” ;
- “présence publique confirmée” ;
- “sans être traité comme une date exacte” ;
- “élément documenté officiellement” utilisé comme angle principal ;
- mentions d’audit, d’upsert, de Preview, de source faible, de champ manquant ou de stratégie de prudence ;
- phrases qui expliquent la méthode de recherche plutôt que l’histoire racontée.

Quand une date est seulement une attestation documentaire, formuler naturellement :

- bon : “La Frigate est déjà mentionnée par le parc en 2010, ce qui confirme sa présence dans l’offre de cette période.” ;
- mauvais : “À cette date, le parc présente déjà Frigate dans ses pages d’attractions. C’est un repère documentaire prudent…”.

Si l’information est trop maigre pour produire un résumé humain et intéressant, ne pas créer d’événement visible, ou garder l’événement minimal avec une source et signaler la limite dans `metadata.notes`.

Pour un article historique, privilégier chronologie, fondateurs, exploitants successifs, attractions importantes, périodes de développement, transformation ou fermeture sourcée, traces actuelles et rôle patrimonial.

Pour une visite terrain ou une vidéo, préciser la date, distinguer faits et ressentis, mentionner les observations réelles et ne pas transformer une observation ponctuelle en règle générale.

## Sources

Chaque événement important doit avoir des sources. Les dates, exploitants, relocalisations et fermetures doivent être vérifiés.

Utiliser `accessedAt` avec la date de consultation.

Chaque `sources[].url` doit être vérifiée juste avant livraison :

- URL absolue `http` ou `https` ;
- page qui répond réellement après redirections ;
- pas de statut 404, 410, 5xx ou erreur réseau ;
- pas de soft-404, page vide, page d’accueil générique utilisée à la place d’un article disparu, ni page sans rapport avec l’affirmation sourcée ;
- pas d’URL inventée ou reconstruite à partir d’un titre ;
- si la page d’origine a disparu, utiliser une archive publique fiable ou une autre source valide ;
- si aucune source joignable n’existe, ne pas créer l’article et ne pas transformer la donnée en fait certain.

Une source non joignable n’est pas un warning acceptable : c’est une erreur de livrable à corriger avant de fournir le fichier JSON.

Sources possibles :

- site officiel du parc ;
- communiqué officiel ;
- presse locale ou nationale ;
- archives de presse ;
- documents historiques ;
- bases spécialisées fiables ;
- vidéos ou photos originales ;
- documents administratifs publics.

Les titres d’articles doivent être clairs, spécifiques et humains. Éviter titre générique, clickbait, promesse non tenue et sur-optimisation. Les liens internes doivent aider le lecteur vers parc, attraction, constructeur, opérateur, fondateur, vidéo, galerie ou article lié.

La méta description doit résumer le sujet et donner envie de lire, sans promesse non tenue ni répétition artificielle de mots-clés.

## JSON attendu

Section principale : `history.events`.

```json
{
  "documentType": "AmusementParkParkGraphUpsert",
  "schemaVersion": "2026-06-30",
  "mode": "merge",
  "metadata": {
    "source": "codex-history",
    "targetParkId": "id-du-parc",
    "targetParkName": "Nom du parc",
    "step": "07-park-history-lot-1",
    "notes": "Timeline du parc uniquement. Articles longs reportés au lot suivant."
  },
  "identity": {
    "parkId": "id-du-parc",
    "name": "Nom du parc"
  },
  "history": {
    "events": [
      {
        "owner": "park",
        "key": "park-opening-1992-04-12",
        "eventType": "Opening",
        "date": "1992-04-12",
        "isVisible": true,
        "isMajor": true,
        "titles": {
          "fr": "Ouverture du parc",
          "en": "The park opens"
        },
        "summaries": {
          "fr": "Le parc ouvre au public avec ses premières zones et attractions confirmées.",
          "en": "The park opens to visitors with its first confirmed areas and attractions."
        },
        "sources": [
          {
            "label": "Site officiel",
            "url": "https://example.com/history",
            "accessedAt": "2026-06-30"
          }
        ]
      }
    ]
  }
}
```

## Contrôles avant livraison

- Chaque événement a un propriétaire résolu.
- Chaque `eventType` est compatible avec `park` ou `parkItem` selon les valeurs de `park-graph-upsert-enums.md`.
- `entityType`, `datePrecision` et les types de blocs d’article utilisent les valeurs canoniques de `park-graph-upsert-enums.md`.
- Le statut courant de chaque attraction reste une valeur lifecycle contrôlée ; aucune transformation historique n’est laissée dans `attractionDetails.status`.
- Toute valeur legacy de statut qui décrivait un retrack, une relocalisation, une rénovation, un renommage, un changement de thème, un démontage, un stockage, un transfert, une réinstallation, un remplacement ou une démolition a été corrigée sans perdre le fait correspondant dans la timeline.
- La date respecte la précision disponible : année, mois ou jour.
- Toutes les URLs de `sources` ont été testées et répondent sans 404, 410, 5xx, soft-404 ou remplacement trompeur.
- Les URLs archivées pointent vers une capture consultable de la page utile, pas seulement vers une page d’archive vide.
- Les titres et résumés importants sont localisés dans les 8 langues quand le lot est complet.
- Les articles ont un vrai angle éditorial.
- Pour un parc majeur, les résumés de timeline sont contrôlés contre la bande indicative de 25 à 55 mots et racontent à la fois le fait et sa portée.
- Les articles durables atteignent normalement 250 mots visibles avec au moins 3 intertitres et 3 paragraphes ; les grandes synthèses atteignent normalement 500 à 900 mots. Toute exception courte est relue et justifiée par le sujet.
- Les huit versions d’un article conservent exactement les mêmes blocs éditoriaux utiles et une profondeur comparable ; aucune langue ne perd un paragraphe, une période ou une conclusion.
- Les annonces récentes à effet durable ont été recherchées et les sujets importants possèdent un article, pas seulement une ligne de timeline.
- Pour un parc majeur ou historique, la timeline couvre les grandes périodes et transformations documentables plutôt qu’un historique minimal.
- Les fermetures emblématiques de l’inventaire de l’étape 3 sont reliées à un jalon, un article quand le sujet le mérite, ou une lacune expliquée.
- Aucune ouverture, réouverture, fermeture temporaire ou fermeture définitive de parkItem n’est dupliquée dans la timeline du parc ; ces faits sont portés uniquement par la timeline du parkItem concerné.
- Les titres, résumés et blocs d’articles ne contiennent aucune formulation d’audit interne ou justification documentaire mécanique.
- Les images référencées existent déjà ou sont créées dans le même JSON.
- Chaque jalon visible et chaque article possède une image contextualisée quand une image acceptable est trouvable, sinon l’exception de recherche est documentée.
- Chaque image créée possède un propriétaire résolu selon l’étape 5, indépendamment de l’article.
- Chaque événement `ParkItem` contient `ownerId`, `parkItemId` et `itemId` explicites quand le parkItem existe déjà dans l’export.
- Chaque article qui référence une image existante utilise `mainImageId`, `blocks[].imageId` ou `blocks[].imageIds` depuis l’état de référence.
- `mainImageKey`, `imageKey` et `imageKeys` ne sont utilisés que pour des images créées dans le même JSON.
- Chaque clé d’image référencée correspond caractère par caractère à une unique `images[].key` du lot.
- Deux définitions `images[].key` ne deviennent jamais identiques après suppression des espaces de bord et comparaison sans tenir compte de la casse.
- Le Preview n’est pas considéré comme une validation des clés d’images utilisées par les articles.
- Les titres, sous-titres, résumés, paragraphes et légendes sont relus en affichage public mobile.
- Les annonces et récits d’attractions sont balayés par familles de vocabulaire mécanique et de spécifications chiffrées ; toute concentration qui explique le dispositif au lieu de raconter le sujet impose une réécriture.
- Les corps de résumés et sous-titres sont comparés entre événements et articles après retrait des noms propres ; aucun gabarit générique répété ne subsiste.
- Les huit versions d’un même résumé racontent les mêmes faits avec une précision comparable ; la seule présence des huit codes de langue ne valide pas des traductions raccourcies en phrases de secours.
- Aucune légende ne doit expliquer l’absence d’une autre image ; elle doit décrire l’image affichée et son lien avec le sujet.
- Les articles d’incidents ou accidents ne doivent contenir ni dramatisation, ni langage défensif, ni justification de méthode.
- Les incidents ou accidents retenus sur un parkItem ont un article associé et une photo contextualisée quand une image acceptable est trouvable.
- Les événements sensibles sont factuels, sourcés et sans dramatisation.

## Après Apply

Avant le lot historique suivant, contrôler la réponse Apply, intégrer ses résultats au registre consolidé et continuer sans export complet ; un export ciblé reste réservé à une réponse ambiguë ou à un identifiant indispensable. Une fois tous les lots de l’étape 7 terminés, effectuer l’unique export complet obligatoire avant de commencer l’étape 8.

À la fin de la réponse, ajouter `Pertinence de la prochaine étape` pour l’étape 8 — Audit final. L’audit final reste utile dès qu’un JSON a été appliqué, même si certains enrichissements ont été volontairement sautés. Si l’étape 8 est exceptionnellement jugée `probablement inutile`, expliquer pourquoi et rappeler qu’elle est normalement le point de contrôle final du parcours. En mode ChatGPT, attendre la validation utilisateur ; en mode Codex autonome, exécuter l’audit sans pause.