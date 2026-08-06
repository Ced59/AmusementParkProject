# Étape 4 — Descriptions longues localisées

Objectif : produire les descriptions publiques longues, naturelles et utiles dans les 8 langues, sans saturer le contexte.

## Lire avant de commencer

- `park-data-integration-orchestrator.md`
- `park-graph-upsert-enums.md` si le lot contient aussi des champs structurés

## État de référence requis

En mode ChatGPT guidé, utiliser l’export actualisé après l’inventaire des parkItems. En mode Codex autonome, utiliser le registre local consolidé, sans nouvel export complet. Les descriptions doivent viser les IDs, noms, zones et statuts réellement présents.

## Langues attendues

Les 8 langues publiques sont :

- `fr`
- `en`
- `de`
- `nl`
- `it`
- `es`
- `pl`
- `pt`

Reprendre les codes de langue présents dans l’export si l’état existant diffère.

## Découpage anti-saturation

Ordre recommandé :

1. Description du parc seul.
2. Descriptions des zones.
3. ParkItems majeurs par lots de 5 à 8.
4. ParkItems secondaires par lots de 8 à 12.
5. Attractions définitivement fermées et autres items historiques par lots dédiés.
6. Restaurants, boutiques, services, parkings et hôtels par familles.

Ne pas rédiger toutes les descriptions d’un grand parc en une seule réponse.

## Niveau de longueur

- Parc majeur : description riche, structurée en plusieurs paragraphes courts.
- Parc local : description plus courte, mais spécifique et utile.
- Zone : ambiance, rôle dans la visite, repères concrets.
- Attraction : expérience observée, rythme, ambiance, place dans le parc.
- Restaurant, boutique, service : utilité réelle et identité visible, sans phrase vide.
- Référence : biographie réutilisable et non centrée uniquement sur le parc du lot.

## Règles rédactionnelles à préserver

Ne pas modifier la charte pour rendre les textes techniques. Les descriptions restent publiques, naturelles et user friendly.

Une description doit aider un visiteur réel à comprendre ce qu’il va voir, l’ambiance du lieu, le type d’expérience proposée, l’identité propre de l’entité et pourquoi elle mérite d’être remarquée.

Elle décrit l’entité, pas l’emploi du temps du lecteur. Le texte peut expliquer un rythme calme, familial ou intense lorsqu’il s’agit d’une caractéristique réelle de l’expérience, mais il ne doit jamais prescrire à quel moment venir, à quel profil réserver l’activité ou comment l’insérer entre deux files.

Le temps verbal et les promesses doivent suivre `park.status` : futur et formulations de projet pour `Planned`/`UnderConstruction`, fermeture temporaire explicite pour `TemporarilyClosed`, passé pour `ClosedDefinitively`, et projet non réalisé pour `Cancelled`. Ne jamais inviter à visiter, annoncer une disponibilité actuelle ou transformer une promesse de projet en équipement existant hors de `Operating`.

Avant de valider une description, se demander : est-ce qu’un visiteur du parc pourrait lire ce texte sur son téléphone et le trouver utile, naturel et agréable ? Si la réponse est non, réécrire.

Priorités en cas de doute :

1. Exactitude factuelle.
2. Utilité visiteur.
3. Clarté mobile.
4. Ton naturel.
5. SEO discret.
6. Structure HTML propre.
7. Cohérence multilingue.

Le style attendu est naturel, éditorial, spécifique au lieu, agréable à lire, orienté visiteur, non mécanique, non cloné d’un parc à l’autre et sans remplissage.

Cette charte s’applique à tout texte public produit dans les étapes suivantes, notamment titres, résumés, articles, textes alternatifs et légendes d’images. Aucun de ces textes ne doit ressembler à un champ rempli automatiquement, à une fiche technique, à une note de contrôle ou à une explication du processus d’intégration.

Interdit dans les descriptions :

- restrictions d’accès ;
- tailles ;
- âge conseillé sous forme réglementaire ;
- tarifs ;
- horaires ;
- dates d’ouverture ;
- détails techniques bruts ;
- coordonnées GPS ;
- notes d’administration ;
- notes de complétude ;
- jargon admin ;
- explication d’upsert, de SEO ou de base de données.

Formulations interdites :

- “ce que ça apporte à la journée” ;
- “ce que ça apporte au groupe” ;
- “comment l’intégrer dans la journée” ;
- “quand cela devient utile” ;
- “pour une fiche visiteur” ;
- “à référencer” ;
- “contenu public” ;
- “élément de parc” ;
- “dans la base” ;
- “upsert” ;
- “SEO” dans un texte public.
- “garde cette attraction pour…” ou “choisis-la si…” ;
- “place-la entre…” ou “intègre-la dans la journée” ;
- “une pause entre les files, véhicules ou grandes attractions” ;
- “une expérience utile pour varier le parcours” ;
- “une place claire dans la visite ou le programme de la journée” ;
- les équivalents traduits de ces consignes ou paragraphes de secours dans les sept autres langues.

Éviter aussi les introductions répétitives : “Situé dans…”, “Cette attraction propose…”, “Idéal pour…”, “Cette zone permet…”.

## Contrôle anti-gabarit sur tout le corpus

La présence de huit textes et de noms différents ne suffit pas. Avant Apply puis à l’étape 8 :

1. retirer temporairement les `<h2>`, le nom de l’entité et les balises de mise en forme pour comparer les corps de texte ;
2. repérer les paragraphes identiques, quasi identiques et les mêmes enchaînements de phrases dans chaque langue ;
3. rechercher les familles de formulations d’itinéraire, de classement interne et de remplissage, pas seulement une liste exacte de mots interdits ;
4. relire manuellement chaque groupe détecté et réécrire avec des faits propres à l’entité ;
5. refaire ce contrôle sur le corpus complet de l’export préalable à l’étape 8, car un titre différent peut masquer un corps de paragraphe cloné.

Un même fait peut naturellement employer un vocabulaire proche, mais deux entités distinctes ne doivent pas partager un paragraphe passe-partout. Les traductions allemande et néerlandaise, comme toutes les autres, ne peuvent pas servir de versions de secours plus génériques que le français ou l’anglais.

## Règles par type de description

- Parc majeur : introduction immersive, identité du parc, grandes familles d’expériences, ambiance générale, intérêt pour différents profils de visiteurs, conclusion naturelle.
- Parc local : texte spécifique et utile sans inventer une importance excessive.
- Parc ou item en général : éviter les superlatifs non sourcés, ne pas masquer les incertitudes et ne jamais promettre une expérience non vérifiée.
- Zone : espace vécu, ambiance, décor, rôle dans la visite, points d’intérêt, logique de circulation si utile.
- Attraction : expérience ressentie, observations visibles, rythme, ambiance, place dans le parc, sensations fiables.
- Restaurant, boutique, service : type de lieu, ambiance, utilité réelle, positionnement, particularité visible ou nommable.
- Fondateur, exploitant, constructeur : biographie factuelle, réutilisable, prudente, non centrée artificiellement sur le parc courant.
- Attraction définitivement fermée : raconter naturellement l’expérience qu’elle proposait et sa place dans le parc au passé, sans la présenter comme encore visitable ni réduire le texte à ses dates et caractéristiques.

Les descriptions ou biographies de références peuvent être traitées ici seulement si le lot est explicitement consacré à du texte localisé. Sinon, les compléter à l’étape 5. Dans tous les cas, ne pas considérer une référence importante comme complète si elle n’a pas de biographie ou description fiable dans les 8 langues, sauf absence de sources documentée.

## Exactitude et anti-duplication

- Ne jamais présenter comme certaine une date, un constructeur, une zone, une attraction, une ouverture, une fermeture ou une photo non vérifiée.
- Si une information est incertaine, la documenter dans `metadata.notes` ou l’omettre.
- Ne jamais copier-coller une description d’un parc à l’autre.
- Même pour deux attractions de même type, varier l’angle, le vocabulaire, le rythme, les détails spécifiques et le contexte.
- Le SEO doit rester discret et naturel.

## JSON attendu

Sections possibles :

- `park.descriptions`
- `zones[].descriptions`
- `items[].descriptions`
- `references.*.biography` ou `references.operators[].description` si le lot cible des références

```json
{
  "documentType": "AmusementParkParkGraphUpsert",
  "schemaVersion": "2026-06-30",
  "mode": "merge",
  "metadata": {
    "source": "codex-rich-descriptions",
    "targetParkId": "id-du-parc",
    "targetParkName": "Nom du parc",
    "step": "04-descriptions-items-lot-1",
    "notes": "Lot limité à 6 attractions majeures."
  },
  "identity": {
    "parkId": "id-du-parc",
    "name": "Nom du parc"
  },
  "items": [
    {
      "id": "id-item-exporte",
      "key": "item-key",
      "name": "Nom de l’item",
      "descriptions": [
        { "languageCode": "fr", "value": "<p>Description française naturelle.</p>" },
        { "languageCode": "en", "value": "<p>Natural English description.</p>" }
      ]
    }
  ]
}
```

## Contrôles avant livraison

- Les 8 langues sont présentes pour chaque entité du lot, sauf décision explicitement documentée.
- Les traductions sont naturelles, pas mot à mot.
- Le français public utilise un ton direct et informel quand le contexte s’y prête.
- Les textes localisés utilisent les accents, diacritiques, ponctuations et caractères propres à chaque langue.
- Aucun texte ne réemploie mécaniquement la même structure d’un item à l’autre.
- Aucun corps de paragraphe n’est dupliqué entre entités après retrait du titre et du nom, sauf citation ou fait commun explicitement justifié.
- Aucune description ne donne de conseil d’itinéraire ou ne classe l’entité selon son utilité dans la journée.
- Le HTML reste simple : `<p>`, `<h2>`, `<h3>`, `<ul>`, `<li>`, `<strong>` si utile, sans structure lourde ou décorative.
- Aucune information structurée ne pollue la narration.
- Le texte donne envie de lire sans survente.
- Le texte ne ressemble pas à une fiche interne.
- Toute attraction publiable, y compris annoncée, en construction ou définitivement fermée, possède ses 8 descriptions naturelles, sauf lacune de source précisément documentée.

## Après Apply

Avant le lot de descriptions suivant ou avant les images, demander l’export actualisé en mode ChatGPT. En mode Codex autonome, intégrer les résultats Apply au registre local et continuer sans nouvel export.

À la fin de la réponse, ajouter `Pertinence de la prochaine étape` pour l’étape 5 — Images et références. Si aucune image fiable, créditable ou techniquement importable n’a été trouvée après une recherche réelle, indiquer `probablement inutile` ou `à décider` avec la raison. Si l’étape 5 est `probablement inutile`, appliquer la règle de proche en proche de l’orchestrateur jusqu’à la prochaine étape officielle `utile` ou `à décider`. En mode ChatGPT, attendre la décision utilisateur ; en mode Codex autonome, consigner la non-applicabilité et continuer selon l’orchestrateur.
