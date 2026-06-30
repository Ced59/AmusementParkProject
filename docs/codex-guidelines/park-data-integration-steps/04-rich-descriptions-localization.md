# Étape 4 — Descriptions longues localisées

Objectif : produire les descriptions publiques longues, naturelles et utiles dans les 8 langues, sans saturer le contexte.

## Lire avant de commencer

- `park-data-integration-orchestrator.md`

## Export requis

Utiliser l’export actualisé après l’inventaire des parkItems. Les descriptions doivent viser les IDs, noms, zones et statuts réellement présents.

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
5. Restaurants, boutiques, services, parkings et hôtels par familles.

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

Interdit dans les descriptions :

- restrictions d’accès ;
- tailles ;
- tarifs ;
- horaires ;
- dates d’ouverture ;
- coordonnées GPS ;
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

Éviter aussi les introductions répétitives : “Situé dans…”, “Cette attraction propose…”, “Idéal pour…”, “Cette zone permet…”.

## Règles par type de description

- Parc majeur : introduction immersive, identité du parc, grandes familles d’expériences, ambiance générale, intérêt pour différents profils de visiteurs, conclusion naturelle.
- Parc local : texte spécifique et utile sans inventer une importance excessive.
- Zone : espace vécu, ambiance, décor, rôle dans la visite, points d’intérêt, logique de circulation si utile.
- Attraction : expérience ressentie, observations visibles, rythme, ambiance, place dans le parc, sensations fiables.
- Restaurant, boutique, service : type de lieu, ambiance, utilité réelle, positionnement, particularité visible ou nommable.
- Fondateur, exploitant, constructeur : biographie factuelle, réutilisable, prudente, non centrée artificiellement sur le parc courant.

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
- Aucun texte ne réemploie mécaniquement la même structure d’un item à l’autre.
- Le HTML reste simple : `<p>`, `<h2>`, `<h3>`, `<ul>`, `<li>`, `<strong>` si utile.
- Aucune information structurée ne pollue la narration.
- Le texte donne envie de lire sans survente.
- Le texte ne ressemble pas à une fiche interne.

## Après Apply

Demander l’export actualisé avant le lot de descriptions suivant ou avant les images.
