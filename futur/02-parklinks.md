# 02 — C01 / PARKLINKS : les connexions prouvées

[Retour](README.md) · Ordre de réalisation : deuxième, après C02 vérifié en production.

## 1. Expérience attendue

Deux parcs, une consigne compréhensible, et une question : comment passer de l'un à l'autre à travers les liens réels du catalogue ? Le joueur construit une chaîne de preuves ; il ne répond pas à une question à choix multiple arbitraire.

Un **pont** standard relie deux parcs via une attraction de chacun et leur constructeur commun : `parc A → attraction a → constructeur M → attraction b → parc B`. L'écran compte **un pont**, pas quatre arêtes techniques. La preuve est consultable en ouvrant les deux fiches d'attractions et le constructeur. Les libellés doivent distinguer « associé dans les données publiées » de toute affirmation historique supplémentaire.

La V1 comprend le défi quotidien, un tutoriel court, des indices, la reprise d'une tentative sur le même navigateur, un résultat partageable sans solution, les preuves et une archive limitée de défis clos. Pas de classement mondial, de lot à gagner, de chat, d'achat ou de duel temps réel.

### Parcours de référence

1. Le visiteur arrive sur une carte sociale « Deux parcs. À toi de trouver les liens. » et ouvre l'URL localisée du défi.
2. Il voit départ, arrivée, règles exactes, date, disponibilité, nombre maximal de ponts et bouton « Commencer ». Aucun compte imposé.
3. Le tutoriel montre un exemple **synthétique distinct du défi**, puis disparaît sans imposer une inscription.
4. Il choisit une attraction et un constructeur/une destination admissibles depuis le parc courant, avec recherche paginée. Le site montre la preuve avant de confirmer le pont.
5. Il peut revenir au pont précédent, consulter une fiche et retrouver sa tentative. Un retour en arrière n'efface pas les indices déjà consommés.
6. En arrivant au parc cible, le serveur valide l'ensemble de la chaîne. Le résultat présente les ponts, les indices et l'écart à l'optimum du défi.
7. « Défier un ami » partage départ/arrivée et résultat sans les intermédiaires. « Comprendre les liens » ouvre les preuves. « Revenir demain » annonce la prochaine disponibilité, sans notification inscrite d'office.

## 2. Règles de jeu précises

La V1 utilise uniquement les ponts par constructeur résolu. Les liens opérateur/fondateur sont une extension du **même concept**, après définition de leur sémantique temporelle ; aucune égalité de pays, mot du nom, couleur, catégorie « attraction » ou sous-type libre ne crée automatiquement un lien.

Un pont n'est admissible que si ses deux parcs et attractions sont publics et présents dans le dataset du défi, les deux attractions appartiennent aux bons parcs, leurs constructeurs résolus ont le même ID et toutes les preuves restent publiables. Une chaîne ne répète ni parc ni constructeur. Maximum initial : six ponts ; réglage versionné et non modifiable au milieu d'un défi.

Le score principal est un **tuple**, pas une formule opaque : réussite, nombre de ponts, nombre d'indices. Affichage de l'optimum en ponts seulement après réussite ou abandon. Le temps n'est qu'une information facultative, jamais un classement défavorisant les lecteurs lents ou l'accessibilité. Aucun score envoyé par le navigateur n'est accepté comme autorité.

Une tentative abandonnée peut consulter la solution mais est marquée `Revealed`, pas `Solved`. Une nouvelle tentative du même défi reste clairement une répétition. L'anonymat ne permet pas de garantir « une personne, une tentative » : ne pas annoncer d'anti-triche absolu.

### Piège à éviter : tous les grands parcs sont à un pont

Beaucoup de parcs peuvent partager un constructeur populaire. Un BFS brut sur ce graphe peut produire presque uniquement des défis triviaux. Le générateur doit mesurer la distribution des distances, le nombre de solutions et la diversité des ponts AVANT le lancement.

Chercher d'abord des paires dont le plus court chemin admissible comporte deux à quatre ponts. Si elles sont trop rares, employer un **sous-graphe de défi explicitement annoncé** (liste de constructeurs autorisés, cohortes ou un constructeur interdit), puis recalculer la solvabilité. Ne jamais ajouter silencieusement une contrainte après le début, ni prétendre qu'un chemin est mondialement optimal lorsque l'optimum dépend de règles de jeu restrictives.

Une relation universelle comme « même pays » n'est pas un repli acceptable. Si aucun défi intéressant et prouvé n'est disponible, afficher un défi d'entraînement clairement identifié ou une pause éditoriale, plutôt qu'inventer un lien.

## 3. Génération et algorithme

Construire un index biparti par constructeur avec des listes dédupliquées de parcs et preuves d'attractions. Éviter de matérialiser toutes les paires d'un constructeur très présent : énumérer les voisins à partir de l'index, avec bornes de candidats.

La recherche tient compte des contraintes de non-répétition. Son état comprend le parc courant, les constructeurs déjà utilisés et les parcs déjà visités ; un BFS ne marquant que `visited[parkId]` serait incorrect sous ces contraintes. Profondeur et nombre d'états sont plafonnés. Rechercher une solution exacte pour les défis publiés ; un timeout de calcul signifie « candidat rejeté », pas « optimum trouvé ».

Chaque candidat est validé par le même moteur de règles que les soumissions. Calculer l'optimum sous les règles exactes, quelques solutions alternatives et un indicateur de difficulté. Écarter absence de solution, source incertaine, répétition trop récente des mêmes parcs et quasi-doublon d'un défi récent. La diversité géographique est un objectif éditorial secondaire, pas une preuve de qualité.

Le défi journalier est choisi dans un pool validé au moyen d'une graine déterministe incluant date Europe/Paris, version de règles et dataset. Persister la sélection : ne pas recalculer un autre défi après un redéploiement ou un enrichissement de la base. Une contrainte unique sur `editionKey` garantit le même défi mondial, quelle que soit la langue ou le fuseau du visiteur.

Fenêtre : de 00:00 Europe/Paris à 00:00 du lendemain, convertie en instants UTC ; utiliser le fuseau, pas un ajout de 24 heures lors d'un changement d'heure. Préparer un petit stock borné d'éditions ; pas d'année entière de rendus vidéos d'avance.

## 4. Persistance proposée

| Objet | Champs importants | Index/contraintes |
| --- | --- | --- |
| `ParklinksChallenge` | editionKey, opensAtUtc, closesAtUtc, datasetId, rulesVersion, départ/arrivée, constraints, statut, optimum interne, solution interne | `editionKey` unique ; statut + fenêtre |
| `ParklinksAttempt` | ID opaque, challengeId, subjectId nullable ou jeton anonyme haché, version, état, ponts validés, hintCount, started/completedAt | propriétaire + défi ; expiration TTL pour anonymes |
| `ParklinksHintReceipt` ou état embarqué | attemptId, clé d'indice, ordre, contenu autorisé | unicité/rejeu idempotent ; éviter une collection séparée si l'agrégat suffit |
| `ShareArtifact` existant/étendu | tentative validée ou défi, spoilerMode, dataset, locale | clé déterministe de rendition |

Les solutions et optima internes ne doivent apparaître ni dans le DTO public initial, ni dans TransferState, JSON-LD, source HTML, nom d'image ou attribut DOM. Les DTO internes et publics sont des types distincts. L'archive peut révéler une solution une fois l'édition close, conformément à son contrat explicite.

Anonyme : jeton aléatoire non devinable, stocké selon la politique de session du site ; aucun ID de compte ou secret dans l'URL de partage. Un attaquant peut créer plusieurs sessions : appliquer quotas et limitations, sans fingerprinting. Pour un compte connecté, le stockage serveur de progression reste une préférence, pas une condition pour jouer.

## 5. API proposée

Le préfixe et les conventions finales sont à aligner sur le dépôt. Les chemins suivants décrivent les contrats, pas des routes existantes.

| Opération | Contrat principal |
| --- | --- |
| `GET /api/discovery/parklinks/today` | édition publique, règles, dates, état ; aucun spoiler |
| `GET /api/discovery/parklinks/challenges/{id}` | même contrat pour une édition stable, lien localisé |
| `POST .../{id}/attempts` | crée/reprend sous quota une tentative ; réponse état initial et version |
| `GET /api/discovery/parklinks/attempts/{id}` | réservé au propriétaire anonyme/authentifié |
| `GET .../attempts/{id}/options?cursor=...` | voisins admissibles depuis l'état autoritaire, taille plafonnée |
| `POST .../attempts/{id}/steps` | IDs du pont + expectedVersion + clé idempotence ; serveur revalide |
| `DELETE .../attempts/{id}/steps/last` | annule un pont, version attendue, conserve les indices |
| `POST .../attempts/{id}/hints` | consomme un indice déterministe et atomique, rejouable sans double pénalité |
| `POST .../attempts/{id}/finish` | validation complète, résultat canonique ; aucun score client |
| `POST .../attempts/{id}/reveal` | abandon/révélation explicite, état non gagnant |
| `POST .../attempts/{id}/share-artifacts` | seulement tentative autorisée, support sans spoiler par défaut |

Erreurs métier stables : challenge indisponible/retiré, tentative inconnue, accès interdit, version obsolète (409), pont invalide, limite atteinte, rate limit (429). Une preuve retirée invalide le défi et ses tentatives comparatives ; afficher une explication, conserver les traces internes et ne pas convertir les abandons en échecs du joueur.

## 6. Front et contenu

Route lazy `/{lang}/decouverte/parklinks/...` illustrative, via le système de localisation existant. Une facade orchestre un port API, pas un service HTTP concret injecté directement. Composants séparés : briefing, step builder, evidence drawer, hint panel, attempt result, share actions.

Sur mobile, une chaîne verticale remplace le graphe horizontal. Un tableau/liste accessible permet tout le parcours sans drag-and-drop et sans carte. Noms longs enveloppés, focus restauré après ajout/retrait, erreur attachée au pont concerné. La carte est facultative et lazy. Les images ne sont pas nécessaires pour résoudre un défi.

L'état `Loading` ne remplace pas toute la page lors d'une validation. Empêcher les doubles clics mais ne pas se fier à ce seul contrôle. Gérer réseau perdu puis reprise par idempotence/version. Une édition close affiche clairement entraînement/archives, pas un nouveau score quotidien.

## 7. Diffusion intégrée à la V1

Artefacts distincts : invitation quotidienne sans solution ; résultat personnel sans intermédiaires ; explication d'un ancien défi avec spoiler explicite après clôture. Une variante vidéo peut révéler progressivement les liens **uniquement pour une édition close ou une démonstration**.

Facebook : aperçu départ/arrivée, numérotation du défi, CTA vers la page ; publication Page seulement selon politique autorisée. Instagram : première carte question, cartes suivantes sans solution avant clôture, dernière CTA ; après clôture, carrousel explicatif. TikTok/Reels/Shorts : export vertical de 12–20 secondes avec textes lisibles, sans audio sous droit imposé. LinkedIn : sélection occasionnelle expliquant un lien industriel, pas le score privé d'un utilisateur. Copier le lien, partager natif et télécharger l'image restent toujours disponibles.

Ne pas créer automatiquement un post de marque à chaque réussite visiteur. Le partage individuel est une action volontaire. Voir document 06 pour les restrictions de chaque réseau et la chaîne de publication.

## 8. Lots de réalisation

| Lot | Travail livrable | Validation de sortie |
| --- | --- | --- |
| C01-A | Audit de densité, règles de pont, graphe/proofs et fixtures | règles déterministes, trivialité quantifiée, aucune donnée masquée |
| C01-B | Générateur borné, snapshots, dates et sélection quotidienne | éditions reproductibles et solvables, concurrence de jobs testée |
| C01-C | Tentatives, indices, validation, reprise et contrats HTTP | tests sécurité/IDOR/version/idempotence |
| C01-D | Parcours mobile, preuves, tutoriel et résultat | scénario sans compte réalisable, SSR et accessibilité |
| C01-E | Supports sociaux, archive contrôlée, métriques et admin | zéro spoiler, droits des médias, dry-run multicanal |
| C01-F | Review, déploiement, activation et recette | gate document 07 complet avant C03 |

## 9. Tests d'acceptation obligatoires

- Graphe synthétique : A et B reliés par M1, B et C par M2, A et C sans pont direct ; optimum A→C = deux ponts. Une attraction d'un mauvais parc ou d'un autre constructeur est refusée.
- Cas où deux chemins vers un même parc ont des constructeurs déjà utilisés différents : le moteur conserve l'état utile et ne perd pas une solution.
- Constructeur hub, graphe dense, données déconnectées, timeout, cycle, doublon de nom et ID absent : comportement borné et explicite.
- Deux workers produisent une seule édition ; relance après crash donne les mêmes départ/arrivée/règles. Test des deux changements d'heure.
- Score client falsifié, pont inventé, indice concurrent, accès à la tentative d'autrui, token absent et preuve cachée : aucun succès indu ni fuite.
- Inspecter HTML, DTO, TransferState, OG et asset names : aucun intermédiaire ou solution du défi actif.
- Même résultat dans toutes les langues ; annulation de partage n'est pas un échec de jeu ; aucun post réel en CI.

Indicateurs : passage vue→début, début→résolution/révélation, nombre de ponts/indices, consultation de preuves, action de partage, visiteurs revenant à J+1/J+7 et arrivée d'un ami. Aucun objectif de rétention chiffré n'est présenté comme déjà obtenu.
