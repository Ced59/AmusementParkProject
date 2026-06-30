# Étape 0 — Cadrage, pertinence et export

Objectif : décider si le parc doit être intégré, définir le niveau de profondeur et préparer un découpage qui ne saturera pas ChatGPT/Codex.

## Entrées obligatoires

- Nom du parc, pays et ville si connus.
- Export actuel du parc si le parc existe déjà dans l’administration.
- À défaut d’export, confirmation explicite que le parc doit être créé.
- Objectif du lot : création complète, enrichissement, correction ou audit.

## Décision de pertinence

Appliquer d’abord la règle de pertinence. Ne jamais formater, corriger, enrichir ou générer un JSON upsert avant d’avoir décidé si l’entité appartient au périmètre.

Le parc est pertinent s’il s’agit d’un parc d’attractions, parc à thème, parc aquatique, parc familial avec attractions fixes, parc animalier avec contenus nommables, ancien parc documenté ou lieu de loisirs stable contenant des éléments visiteurs nommables.

Sont aussi pertinents quand ils sont rattachés clairement à un parc ou à son histoire :

- attraction fixe isolée ;
- attraction déplacée reliant plusieurs parcs ;
- constructeur, exploitant, fondateur, propriétaire ou figure historique ;
- zone, restaurant, boutique, hôtel, parking, entrée, service, spectacle fixe, animal ou enclos nommé.

Si la pertinence est incertaine :

- ne pas générer de JSON complet ;
- lister les raisons du doute ;
- proposer au maximum un brouillon masqué avec `isVisible: false` et `adminReviewStatus: "ToReview"` si une conservation de trace est utile ;
- ne pas créer de longues descriptions.

Les attractions réellement itinérantes hors parc fixe ne sont pas des parcs à créer. Elles sont pertinentes seulement si elles documentent une relocalisation, une installation durable dans un parc, un constructeur ou un événement historique.

## Niveau de traitement

Classer le parc en niveau de profondeur :

- **Majeur** : traitement exhaustif attendu, lots multiples, descriptions longues, histoire, horaires, images et références.
- **Intermédiaire** : traitement complet sur les éléments fiables, mais pas d’invention de zones ou d’articles artificiels.
- **Local ou mineur** : fiche utile et spécifique, sans survente ni fausse importance.
- **Historique fermé** : dates, ancien emplacement, histoire, parkItems confirmés et relocalisations quand elles existent.

Un parc majeur ne doit jamais être traité comme une fiche minimale si les sources permettent mieux. Il doit être planifié pour recevoir descriptions longues, zones officielles, parkItems principaux et secondaires, restaurants, boutiques, services, hôtels, parkings, exploitants, fondateurs, constructeurs, images, horaires et histoire.

## Plan de lots recommandé

Pour un parc majeur, préparer ces lots :

1. Infos générales du parc.
2. Zones officielles.
3. Inventaire parkItems par zone ou famille.
4. Descriptions du parc et des zones.
5. Descriptions des parkItems par petits lots.
6. Images et logos.
7. Horaires et exceptions datées.
8. Histoire du parc.
9. Histoire des parkItems majeurs.
10. Audit final.

## Règle d’export

Après chaque Apply, demander un nouvel export complet du parc avant de continuer.

L’export actualisé est obligatoire parce qu’il contient :

- les IDs créés par l’application ;
- les clés et rattachements réellement acceptés ;
- les avertissements éventuels déjà corrigés ;
- les images importées et leurs IDs ;
- les données existantes à ne pas écraser.

## Sortie attendue

Produire une réponse courte avec :

- décision de pertinence ;
- niveau de traitement ;
- sources prioritaires à consulter ;
- découpage de lots ;
- prochaine étape à exécuter.

Ne pas produire de JSON upsert massif à l’étape 0.
