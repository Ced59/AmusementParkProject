# Étape 0 — Cadrage, pertinence et export

Objectif : décider si le parc doit être intégré, définir le niveau de profondeur et préparer un découpage qui ne saturera pas ChatGPT/Codex.

## Entrées obligatoires

- Nom du parc, pays et ville si connus.
- Export actuel du parc si le parc existe déjà dans l’administration.
- À défaut d’export, confirmation explicite que le parc doit être créé.
- Objectif du lot : création complète, enrichissement, correction ou audit.

En mode Codex autonome, Codex obtient lui-même l’export et les données de complétude par l’API `PARK_DATA_EDITOR`. Il ne demande pas à l’utilisateur de les extraire depuis l’administration.

Une demande `Complète le parc <nom>` signifie toujours `création ou enrichissement complet avec audit de l’existant`. Elle ne doit pas être réduite à l’ajout de quelques champs manifestement absents.

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
- **Projet annoncé** : identité, annonce officielle, site prévu, période cible et histoire du projet ; pas d’inventaire ni d’horaires inventés.
- **En construction** : éléments du projet officiellement confirmés et jalons de chantier ; ne pas présenter les concepts comme déjà visitables.
- **Fermé temporairement** : données existantes conservées, raison et reprise seulement si sourcées ; aucun statut « ouvert » public.
- **Projet annulé** : annonce, annulation et sources historiques ; pas de coordonnées, exploitant ou offre visiteurs supposés.

La sortie de cadrage doit retenir exactement un `ParkStatus` canonique parmi `Planned`, `UnderConstruction`, `Operating`, `TemporarilyClosed`, `ClosedDefinitively` et `Cancelled`. En cas d’incertitude entre deux états, conserver la fiche masquée et demander une validation plutôt que choisir le statut le plus optimiste.

Un parc majeur ne doit jamais être traité comme une fiche minimale si les sources permettent mieux. Il doit être planifié pour recevoir descriptions longues, zones officielles, parkItems principaux et secondaires, restaurants, boutiques, services, hôtels, parkings, exploitants, fondateurs, constructeurs, images, horaires et histoire.

Le niveau choisi adapte le volume, pas la rigueur. Même pour un parc local, un projet ou un parc fermé, vérifier systématiquement identité, statut, inventaire applicable, anciennes attractions, descriptions, logo, images, histoire, annonces durables et sources. Ne jamais produire artificiellement le même volume qu’un parc majeur lorsque les sources ne le justifient pas.

## État initial et objectifs de couverture

Avant le plan de lots, établir un état initial chiffré à partir de l’export :

- parkItems totaux et attractions par statut ;
- attractions actuelles, annoncées, en construction et définitivement fermées ;
- entités disposant des 8 descriptions publiques ;
- entités disposant d’au moins une image ;
- présence d’un logo officiel courant et d’une image principale du parc ;
- nombre de jalons historiques, d’articles, de sources et d’images associées ;
- données signalées par le contrôle de complétude.

Préparer ensuite les recherches dans quatre ensembles distincts : offre actuelle, projets confirmés, inventaire historique fermé, histoire et actualité récente durable. Pour un parc majeur ou historiquement riche, prévoir des sources officielles, de presse, d’archives et spécialisées : le seul site actuel du parc ne suffit généralement pas à retrouver les éléments disparus.

L’étape 0 doit ouvrir un registre des lacunes. Une donnée ou une image ne peut y être notée `introuvable` qu’après vérification réelle des familles de sources pertinentes. Ce registre nourrit l’audit final ; il n’autorise ni image générique, ni texte inventé.

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

En mode ChatGPT guidé, demander un export actualisé après chaque Apply avant de continuer, puisque l’utilisateur opère les mutations hors de la conversation.

En mode Codex autonome, effectuer l’export complet initial, puis tenir un état de travail consolidé avec les réponses Preview/Apply et d’import d’image. Ne jamais lancer un export complet après chaque Apply ou import, entre deux lots, ni entre les étapes 1 à 7. Hors export initial, le seul export complet planifié et obligatoire a lieu immédiatement avant l’audit de l’étape 8.

Avant l’étape 8, un export limité à certaines sections est exceptionnellement autorisé seulement pour résoudre une incohérence précise, une réponse de mutation manquante ou un ID indispensable absent des résultats. Il ne doit pas devenir une vérification systématique.

Le registre local Codex conserve notamment :

- les IDs créés retournés par l’application ;
- les clés et rattachements réellement acceptés ;
- les avertissements corrigés ou explicitement acceptés ;
- les images importées, leurs IDs et leur statut courant attendu ;
- les données existantes à ne pas écraser ;
- les compteurs de couverture et les lacunes à confirmer par l’export complet préalable à l’étape 8.

## Sortie attendue

Produire une réponse courte avec :

- décision de pertinence ;
- niveau de traitement ;
- sources prioritaires à consulter ;
- état initial chiffré et objectifs de couverture ;
- registre initial des lacunes et recherches nécessaires ;
- découpage de sous-lots dans les étapes officielles ;
- prochaine étape officielle à exécuter ;
- pertinence de la prochaine étape ;
- si la prochaine étape officielle est probablement inutile, prochaine étape officielle jugée utile ou à décider, trouvée de proche en proche.

Ne pas produire de JSON upsert massif à l’étape 0.

À la fin de l’étape 0, la prochaine étape officielle est toujours l’étape 1 pour un vrai parc. Si le parc est pertinent, dire si l’étape 1 est `utile`, `probablement inutile` ou `à décider`, avec la raison. Si elle est jugée `probablement inutile`, appliquer la règle de proche en proche de l’orchestrateur jusqu’à la prochaine étape officielle `utile` ou `à décider`. En mode ChatGPT, attendre la validation utilisateur ; en mode Codex autonome, consigner la décision et continuer selon l’orchestrateur. Ne pas inventer une étape préparatoire avant l’étape 1.

Exception : si l’étape 0 conclut que le bon modèle est `StandaloneAttraction`, suspendre le parcours parc 1 à 8 et basculer vers `standalone-attraction-data-integration.md`.
