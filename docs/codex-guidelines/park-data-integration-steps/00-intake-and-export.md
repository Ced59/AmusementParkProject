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

Une attraction fixe isolée pertinente doit être orientée vers le flux `StandaloneAttraction`, pas vers une fiche parc artificielle. Ne pas créer ou enrichir un parc contenant un seul parkItem si le lieu est uniquement une attraction durable isolée. Si une ancienne fiche parc mono-attraction existe déjà, conserver ses IDs dans la décision d’étape 0 et prévoir une migration vers `standaloneAttraction`, via l’interface admin ou via un JSON `standaloneAttractionGraph`.

## Niveau de traitement

Classer le parc en niveau de profondeur :

- **Majeur** : traitement exhaustif attendu, lots multiples, descriptions longues, histoire, horaires, images et références.
- **Intermédiaire** : traitement complet sur les éléments fiables, mais pas d’invention de zones ou d’articles artificiels.
- **Local ou mineur** : fiche utile et spécifique, sans survente ni fausse importance.
- **Historique fermé** : dates, ancien emplacement, histoire, parkItems confirmés et relocalisations quand elles existent.

Un parc majeur ne doit jamais être traité comme une fiche minimale si les sources permettent mieux. Il doit être planifié pour recevoir descriptions longues, zones officielles, parkItems principaux et secondaires, restaurants, boutiques, services, hôtels, parkings, exploitants, fondateurs, constructeurs, images, horaires et histoire.

## Plan de sous-lots recommandé

Pour un parc majeur, préparer des sous-lots à l’intérieur des étapes officielles. Cette liste aide à éviter la saturation, mais ne remplace jamais le parcours 0 à 8 de l’orchestrateur.

Exemples de sous-lots possibles :

- étape 1 : fiche parc et références fondateurs/exploitants nécessaires ;
- étape 2 : zones officielles, éventuellement par groupe si le parc est très grand ;
- étape 3 : inventaire parkItems par zone ou famille ;
- étape 4 : descriptions du parc, des zones, puis des parkItems par petits lots ;
- étape 5 : images, logos et enrichissement de références ;
- étape 6 : horaires et exceptions datées ;
- étape 7 : histoire du parc, puis histoire des parkItems majeurs ;
- étape 8 : audit final.

Ne pas proposer une étape nouvelle. Si un sujet semble manquer, le rattacher à l’étape officielle où il appartient et expliquer ce rattachement.

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
- découpage de sous-lots dans les étapes officielles ;
- prochaine étape officielle à exécuter ;
- pertinence de la prochaine étape ;
- si la prochaine étape officielle est probablement inutile, prochaine étape officielle jugée utile ou à décider, trouvée de proche en proche.

Ne pas produire de JSON upsert massif à l’étape 0.

À la fin de l’étape 0, la prochaine étape officielle est toujours l’étape 1 pour un vrai parc. Si le parc est pertinent, dire si l’étape 1 est `utile`, `probablement inutile` ou `à décider`, avec la raison. Si elle est jugée `probablement inutile`, appliquer la règle de proche en proche de l’orchestrateur jusqu’à la prochaine étape officielle `utile` ou `à décider`, sans exécuter ni sauter d’étape sans validation utilisateur. Ne pas inventer une étape préparatoire avant l’étape 1.

Exception : si l’étape 0 conclut que le bon modèle est `StandaloneAttraction`, suspendre le parcours parc 1 à 8 et basculer vers `standalone-attraction-data-integration.md`.
