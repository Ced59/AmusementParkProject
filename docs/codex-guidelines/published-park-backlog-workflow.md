# Workflow du backlog des parcs publiés

Ce workflow complète les étapes 0 à 9 pour un cas précis : la reprise progressive d’un parc déjà visible qui figure dans le backlog versionné des parcs publiés. Il ne modifie pas les autorisations d’une commande de complétude ordinaire.

## Périmètre et sélection

- Le parc doit être présent dans `docs/roadmaps/published-parks-completeness-backlog-2026-08-13.md` et encore avoir `isVisible: true` au début du traitement.
- Une demande telle que `Reprends le prochain parc du jackpot` ou `Reprends le prochain parc du backlog` sélectionne la première ligne selon l’ordre versionné : score croissant, puis nom croissant pour départager un même score.
- Une demande qui nomme un parc sélectionne cet identifiant exact dans le backlog, sans substituer un homonyme.
- La visibilité publique existante est préservée pendant toute la reprise. Aucune suppression, aucun masquage et aucun nettoyage d’entité legacy inconnue ne sont implicites.

## Garnissage et rafraîchissement du backlog

Le backlog est alimenté uniquement par le client officiel `PARK_DATA_EDITOR`, en appels séquentiels :

1. parcourir toutes les pages de `SearchParks`, avec au plus 50 fiches par page et en traitant une page avant de demander la suivante ;
2. retenir uniquement les parcs dont `isVisible` vaut `true` ;
3. recalculer individuellement le score de chaque parc retenu avec `Completeness` ;
4. ajouter toute fiche dont le score est inférieur ou égal à 95, car la condition de sortie du workflow est strictement supérieure à 95 ;
5. dédupliquer par identifiant technique et trier par score croissant, puis par nom croissant ;
6. enregistrer le score, le niveau, le pays, le statut, l’audience, les points gagnés/applicables et l’identifiant, ainsi que la date et l’heure du relevé ;
7. mettre à jour les compteurs globaux et les compteurs par niveau dans le même commit documentaire.

### Critère d’entrée exact

Une fiche entre dans le backlog si, et seulement si, les deux conditions suivantes sont vraies au moment du relevé :

- il s’agit d’un parc retourné par `SearchParks` avec `isVisible: true` ;
- son appel individuel `Completeness` réussit et renvoie un score inférieur ou égal à 95.

Le statut du parc (`Operating`, fermeture temporaire ou définitive, projet, etc.), son audience et son statut de revue ne changent pas ce critère. Chaque identifiant est évalué séparément, y compris lorsque plusieurs fiches portent le même nom. Les parkItems, attractions autonomes et fiches masquées n’entrent pas dans ce fichier.

Une fiche visible dont le score est strictement supérieur à 95 n’entre pas. Si la recherche, la pagination ou le calcul individuel échoue, le relevé n’est pas déclaré complet : l’identifiant concerné est ajouté à une section `Anomalies à vérifier` et aucune omission silencieuse n’est autorisée.

Un rafraîchissement ajoute les nouveaux parcs éligibles et actualise les valeurs des lignes existantes. Il ne retire jamais silencieusement une ligne devenue introuvable ou masquée : cette anomalie reste signalée jusqu’à vérification. La seule sortie normale du backlog est la procédure de validation ci-dessous.

## Reprise complète d’une ligne

Chaque parc suit intégralement l’orchestrateur, les étapes 0 à 9 et le workflow API Codex. Le score numérique ne remplace ni la recherche de sources ni l’audit éditorial. L’inventaire actuel et historique, les descriptions dans les huit langues, les images et le logo, les horaires, les tarifs applicables, les jalons, les articles, les références et les lacunes doivent être examinés.

La condition de réussite avant publication est cumulative :

- export complet frais immédiatement avant l’étape 9 ;
- audit final sans bloqueur ;
- contrôle du corpus public sans duplication ou texte mécanique non justifié ;
- score individuel projeté avec `Completeness -ProjectForPublication` strictement supérieur à 95, donc au minimum 96.
- liste `publicationBlockers` vide ; un score brut élevé ne neutralise jamais un bloqueur éditorial, et `public-text.forbidden-editorial-language` impose une réécriture avant publication.

La projection simule uniquement l’état final déjà audité : validation du parc, publication des médias intégrés et publication des articles intégrés. Elle ne modifie aucune donnée, ne change aucune visibilité et ne contourne ni un bloqueur d’audit ni le statut `NotRelevant`. Si une de ces conditions manque, le parc reste dans le backlog avec son score courant actualisé et une note de blocage factuelle ; le score projeté peut être mentionné séparément mais ne remplace jamais la valeur courante du tableau.

## Publication des données et de Facebook

La commande de reprise d’un parc visible du backlog autorise explicitement la phase de publication suivante une fois toutes les conditions précédentes remplies :

1. publier de façon ciblée les nouveaux contenus et médias validés, sans basculer globalement des éléments non audités ;
2. conserver le parc visible et contrôler de nouveau ses pages publiques, son logo, ses contenus et l’idempotence des lots ;
3. recalculer le score courant, sans projection, et exiger qu’il reste strictement supérieur à 95 ; sinon conserver la ligne et ne pas lancer Facebook ;
4. résoudre le brouillon Facebook de la page canonique du parc avec `ResolveFacebookPublication` ;
5. si `hasPublishedParkAnnouncement` vaut `true`, conserver la publication existante et ne rien republier ;
6. si aucun historique d’annonce n’existe, appeler `PublishFacebook` sans `Message` et sans `ImageId` afin d’utiliser le texte bilingue par défaut et les règles Open Graph courantes ; pour ce cas exact, le serveur utilise la clé idempotente du parc et ne publie pas deux fois ;
7. si l’annonce automatique existe avec le statut `Failed`, relever son identifiant depuis la réponse de publication ou de résolution, puis appeler `RetryFacebookPublication` avec l’identifiant exact du parc et de cette publication ; le serveur commence obligatoirement par rapprocher la tentative avec les publications Facebook de la Page dans sa fenêtre temporelle : s’il retrouve le message exact, il rattache l’identifiant Facebook et passe l’enregistrement à `Published` sans republier ; il ne relance le même enregistrement que si Facebook répond correctement et confirme qu’aucun post correspondant n’existe ; une recherche impossible ou ambiguë interdit la relance ;
8. après la reprise, résoudre de nouveau le brouillon et exiger `hasPublishedParkAnnouncement: true` et `parkAnnouncementStatus: Published` ; si la reprise échoue ou si un autre statut est présent, ne pas rappeler `PublishFacebook`, ne pas créer de doublon manuel, conserver la ligne du backlog et rapporter le blocage.

Cette autorisation Facebook est limitée au parc visible traité depuis ce backlog. Elle ne s’étend ni à ses parkItems, ni aux articles, ni aux commandes de complétude ordinaires.

## Retrait et livraison cumulative

La ligne peut être supprimée du fichier versionné seulement lorsque les données ont été publiées avec succès, que le score courant post-publication est strictement supérieur à 95 et que l’annonce Facebook est soit déjà publiée, soit publiée avec succès pendant le traitement.

Après validation :

- supprimer uniquement la ligne du parc ;
- recalculer les compteurs et conserver l’ordre du tableau ;
- créer un commit documentaire dédié au parc sur une branche cumulative de maintenance du backlog ;
- ne pas ouvrir de PR, ne pas incrémenter la version et ne pas déclencher de déploiement pour chaque ligne ;
- regrouper normalement dix retraits, ou le lot explicitement demandé par l’utilisateur, dans une seule PR documentaire ;
- incrémenter la version une seule fois pour cette PR groupée, conformément aux règles de livraison générales.

Le premier lot qui introduit ce workflow peut regrouper sa documentation, le support technique anti-doublon et le premier retrait validé dans une seule version. Les lots suivants restent cumulatifs.
