# Étape 7 — Histoire du parc, des parkItems et articles

Objectif : créer une histoire fiable, sourcée et lisible, en séparant les événements du parc, les événements des parkItems et les articles longs.

## Lire avant de commencer

- `park-data-integration-orchestrator.md`
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

Pour un article historique, privilégier chronologie, fondateurs, exploitants successifs, attractions importantes, périodes de développement, transformation ou fermeture sourcée, traces actuelles et rôle patrimonial.

Pour une visite terrain ou une vidéo, préciser la date, distinguer faits et ressentis, mentionner les observations réelles et ne pas transformer une observation ponctuelle en règle générale.

## Sources

Chaque événement important doit avoir des sources. Les dates, exploitants, relocalisations et fermetures doivent être vérifiés.

Utiliser `accessedAt` avec la date de consultation.

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
- Chaque `eventType` est compatible avec `park` ou `parkItem`.
- La date respecte la précision disponible : année, mois ou jour.
- Les titres et résumés importants sont localisés dans les 8 langues quand le lot est complet.
- Les articles ont un vrai angle éditorial.
- Les images référencées existent déjà ou sont créées dans le même JSON.
- Les événements sensibles sont factuels, sourcés et sans dramatisation.

## Après Apply

Demander l’export actualisé avant de créer le lot historique suivant ou avant l’audit final.

À la fin de la réponse, ajouter `Pertinence de la prochaine étape` pour l’étape 8 — Audit final. L’audit final reste utile dès qu’un JSON a été appliqué, même si certains enrichissements ont été volontairement sautés. Si l’étape 8 est exceptionnellement jugée `probablement inutile`, expliquer pourquoi et rappeler qu’elle est normalement le point de contrôle final du parcours. Attendre la validation utilisateur avant de continuer.
