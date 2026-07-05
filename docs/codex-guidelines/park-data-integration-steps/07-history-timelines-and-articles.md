# Étape 7 — Histoire du parc, des parkItems et articles

Objectif : créer une histoire fiable, sourcée et lisible, en séparant les événements du parc, les événements des parkItems et les articles longs.

## Lire avant de commencer

- `park-data-integration-orchestrator.md`
- `park-graph-upsert-enums.md`
- `04-rich-descriptions-localization.md` pour le style public des résumés

## Export requis

Utiliser l’export actualisé après les étapes d’inventaire, de descriptions, d’images et d’horaires. Les timelines doivent pouvoir référencer les vrais IDs ou les `itemKey` existants.

## Découpage recommandé

Pour un parc riche :

1. Timeline du parc sans articles longs.
2. Articles majeurs du parc.
3. Timeline des parkItems majeurs encore dans le parc.
4. Timeline des parkItems fermés ou relocalisés.
5. Articles de parkItems seulement pour les cas importants.

Ne pas écrire toute l’histoire d’un grand parc et de toutes ses attractions en un seul JSON.

## Événements de parc

Créer des événements pour les faits durables :

- fondation ;
- annonce ;
- construction ;
- ouverture ;
- ouverture de zone ;
- attraction majeure ;
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
- changement de thème ou nom ;
- changement de trains, système ou constructeur ;
- relocalisation ;
- stockage ;
- réinstallation ;
- démolition ;
- conservation patrimoniale.

Pour une attraction déplacée, la timeline du parkItem peut continuer hors du parc d’origine. Utiliser `contextParkId` quand l’événement se déroule dans un autre parc connu, ou un marqueur externe seulement si le modèle l’accepte et que le contexte est clair.

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
- La date respecte la précision disponible : année, mois ou jour.
- Toutes les URLs de `sources` ont été testées et répondent sans 404, 410, 5xx, soft-404 ou remplacement trompeur.
- Les URLs archivées pointent vers une capture consultable de la page utile, pas seulement vers une page d’archive vide.
- Les titres et résumés importants sont localisés dans les 8 langues quand le lot est complet.
- Les articles ont un vrai angle éditorial.
- Les titres, résumés et blocs d’articles ne contiennent aucune formulation d’audit interne ou justification documentaire mécanique.
- Les images référencées existent déjà ou sont créées dans le même JSON.
- Chaque événement `ParkItem` contient `ownerId`, `parkItemId` et `itemId` explicites quand le parkItem existe déjà dans l’export.
- Chaque article qui référence une image existante utilise `mainImageId`, `blocks[].imageId` ou `blocks[].imageIds` depuis l’export actualisé.
- `mainImageKey`, `imageKey` et `imageKeys` ne sont utilisés que pour des images créées dans le même JSON ou dont la clé est démontrée résolue.
- Les titres, sous-titres, résumés, paragraphes et légendes sont relus en affichage public mobile.
- Aucune légende ne doit expliquer l’absence d’une autre image ; elle doit décrire l’image affichée et son lien avec le sujet.
- Les articles d’incidents ou accidents ne doivent contenir ni dramatisation, ni langage défensif, ni justification de méthode.
- Les incidents ou accidents retenus sur un parkItem ont un article associé et une photo contextualisée quand une image acceptable est trouvable.
- Les événements sensibles sont factuels, sourcés et sans dramatisation.

## Après Apply

Demander l’export actualisé avant de créer le lot historique suivant ou avant l’audit final.

À la fin de la réponse, ajouter `Pertinence de la prochaine étape` pour l’étape 8 — Audit final. L’audit final reste utile dès qu’un JSON a été appliqué, même si certains enrichissements ont été volontairement sautés. Si l’étape 8 est exceptionnellement jugée `probablement inutile`, expliquer pourquoi et rappeler qu’elle est normalement le point de contrôle final du parcours. Attendre la validation utilisateur avant de continuer.
