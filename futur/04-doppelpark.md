# 04 — C03 / DoppelPark : des ressemblances expliquées

[Retour](README.md) · Troisième livraison, après C01 vérifié en production.

## 1. Promesse et limites

« Quel parc ressemble, de façon inattendue, à celui que tu aimes ? » La réponse compare la **composition documentée des attractions**, pas la qualité de visite, la thématisation ressentie, les prix, la sécurité ou les émotions. Le nom DoppelPark est un nom de travail à vérifier avant lancement ; il ne garantit pas une identité de marque disponible.

La première version doit être utile depuis une fiche parc : trois correspondances expliquées, leurs points communs, leurs différences et la confiance liée aux données. Un indice de 87/100 n'est ni une probabilité, ni « 87 % de la même expérience ». Ne pas appeler « jumeau » un résultat médiocre seulement pour remplir trois cartes.

V1 : comparaison par types et constructeurs, sélection d'un parc, modes « ressemblances » et « ressemblances lointaines », comparaison face-à-face, critères visibles, partage. Pas de profil psychologique, de personnalisation opaque, de filtre par données personnelles, d'embeddings, de scraping complémentaire à chaque requête ni de recommandation de sécurité/accessibilité.

## 2. Parcours utilisateur

1. Depuis une fiche publique : bloc discret « Découvre ses ressemblances inattendues » ; pas de calcul lourd au chargement de toutes les fiches.
2. Page DoppelPark : parc source, périmètre de données, date, couverture, deux modes, accès « Comment est calculé l'indice ? ».
3. Résultats : jusqu'à trois correspondances suffisantes, avec score, pays, distance lorsqu'elle est connue, deux raisons concrètes et une différence importante.
4. Comparaison détaillée : types représentés, parts relatives, constructeurs, taille d'inventaire documentée, données inconnues et fiches d'attractions illustratives.
5. Le visiteur bascule les deux parcs ou choisit un autre parc. L'indice brut reste symétrique ; le rang peut différer parce que les autres candidats ne sont pas les mêmes.
6. Il partage une carte de la paire ou ouvre le parc découvert. L'URL préserve paire, version du calcul et mode, sans révéler un historique de navigation.

État « données insuffisantes » valorisé : expliquer les champs manquants et proposer une autre découverte, sans résultat fabriqué. Afficher une seule bonne correspondance vaut mieux que trois mauvaises.

## 3. Cohorte et éligibilité

Par défaut, comparer des parcs publiables avec des attractions à l'activité connue, selon la politique du socle. Historique séparé et explicitement nommé, non mélangé au classement courant. Les parcs à dominante aquatique, animalière ou de loisirs généraux ne sont pas supposés comparables sans préciser le périmètre « attractions documentées » ; un filtre de type de parc doit être explicite.

Seuils initiaux proposés, à mesurer puis versionner : au moins 8 attractions éligibles, au moins 3 types précis distincts, couverture de type précis ≥ 80 %, couverture constructeur résolu ≥ 60 %. Une catégorie générique `Other` n'est pas un type précis. Ces seuils servent à éviter les faux jumeaux sur deux fiches renseignées, pas à exclure définitivement les petits parcs ; un profil petits inventaires peut être étudié plus tard, clairement séparé.

Ne pas utiliser comme score de confiance le seul pourcentage de complétude éditoriale d'un parc : celui-ci mesure aussi descriptions, images et autres champs sans rapport direct avec la comparaison. Calculer une couverture spécifique à chaque groupe de caractéristiques.

Le mode lointain applique une distance à vol d'oiseau configurable, proposition initiale 500 km, calculée sur des coordonnées connues. Sans coordonnées, le parc reste éligible au mode général mais pas à un résultat affirmant être lointain. La distance ne modifie pas l'indice de ressemblance ; elle filtre les candidats. Ne pas présenter cette distance comme un temps de trajet.

## 4. Empreinte V1 précisément définie

Deux groupes seulement entrent dans le score V1 :

- **Types précis d'attractions**, poids 0,60 ;
- **Constructeurs résolus**, poids 0,40.

Les modèles libres, matériaux, lancements, zones, restaurants, spectacles et services restent des informations explicatives lorsqu'elles sont fiables. Leur intégration dans le score sera une évolution versionnée, après audit de couverture et tests de pertinence. Cette restriction évite que des inventaires très inégalement renseignés créent des ressemblances artificielles.

Pour un groupe g et un parc p, former une distribution des occurrences connues par caractéristique k : `tf(p,g,k) = count(p,g,k) / knownCount(p,g)`. Les inconnus ne deviennent pas une caractéristique commune ; afficher leur proportion séparément. La normalisation évite que le seul nombre d'attractions domine le score. La taille totale documentée est tout de même visible dans les différences.

Dans la cohorte éligible figée de N parcs, calculer `idf(g,k) = min(3, 1 + ln((N+1)/(df(g,k)+1)))`. La borne 3 est un choix initial testable pour empêcher une donnée rare erronée de dominer. Utiliser `x(p,g,k) = tf(p,g,k) × idf(g,k)`.

Similarité du groupe : Jaccard pondéré `Jg(A,B) = sum_k min(xA,xB) / sum_k max(xA,xB)`. Si le dénominateur est nul, le groupe est indisponible, **pas identique**. Comme les deux groupes sont obligatoires dans le profil V1, un groupe indisponible rend la paire non publiable au lieu de redistribuer silencieusement ses poids.

Indice brut : `S(A,B) = 100 × (0,60 × Jtypes + 0,40 × Jconstructeurs)`. Conserver la précision interne ; arrondir uniquement pour l'affichage. Indice symétrique borné [0,100]. Réutiliser exactement les mêmes poids et IDF pour toutes les paires d'un dataset.

Confiance de couverture séparée : `C = 0,60 × min(typeCoverageA,typeCoverageB) + 0,40 × min(manufacturerCoverageA,manufacturerCoverageB)`. Exiger initialement C ≥ 0,70 et afficher les couvertures brutes. Ce n'est pas un intervalle statistique ni une probabilité de véracité. Une donnée renseignée peut rester erronée ; la preuve et la revue restent nécessaires.

Seuil de publication initial proposé : S ≥ 55, à confirmer sur une validation éditoriale avant lancement. Le seuil n'est jamais ajusté silencieusement pour un parc sans résultat. Sous ce seuil, afficher « aucune correspondance assez solide dans les données actuelles ».

## 5. Explicabilité sans faux récit

Pour chaque groupe, les contributions au numérateur identifient les caractéristiques communes. Les différences sont les plus grands écarts de distributions, complétées par la différence de taille connue et les limites de couverture.

Chaque phrase d'explication doit renvoyer à des comptes réels : par exemple « 4 types précis communs » avec la liste, ou « ce constructeur est représenté dans les deux inventaires » avec les attractions concernées. Ne pas affirmer une filiation, un plagiat, une inspiration ou une ambiance similaire depuis la seule similarité numérique.

Un référentiel de microtextes par cas est édité et traduit au développement. Le moteur assemble uniquement des labels et faits bornés, pas des descriptions génériques destinées à remplir les fiches. Aucun calcul à partir des longs textes du catalogue en V1.

L'interface peut proposer un filtre « autre exploitant » si l'OperatorId est connu des deux côtés. Il s'agit d'un filtre de découverte, pas d'une pénalité secrète dans le score. Ne pas exclure arbitrairement toutes les chaînes ni transformer un opérateur inconnu en opérateur différent.

## 6. Calcul, stockage et performances

Construire les empreintes une fois par dataset. Pour N modéré, calculer les paires hors requête, O(N²×F) au pire, en lots bornés ; mesurer avant de choisir un autre algorithme. Une liste inversée de caractéristiques peut réduire les candidats, mais doit être vérifiée contre un calcul exhaustif sur fixture pour ne pas rater les meilleurs résultats. Pas de base vectorielle.

Persister `ParkFingerprint` (parkId, datasetId, profileVersion, groupes, couverture, éligibilité), `ParkTwinResult` (paire ordonnée par IDs, score brut, contributions, confiance, versions, statut) et une projection top-K reconstruisible. L'unicité `(datasetId, profileVersion, minParkId, maxParkId)` empêche les doublons A/B et B/A. Ne pas conserver toutes les paires indéfiniment : garder les résultats utiles et la capacité de recalcul borné.

Le top-K est trié par score brut, puis couverture, puis ID stable ; le score arrondi ne sert pas à départager. Le mode lointain est un filtre sur les candidats correctement calculés, pas seulement sur trois résultats déjà retenus, sinon de bons jumeaux lointains seraient perdus.

Une modification de données construit une nouvelle version. Les anciennes comparaisons partagées restent datées, sauf retrait de source. L'annonce « nouveau jumeau » exige une variation significative définie (nouveau premier candidat avec gain minimum, par exemple 5 points, ou correction majeure), pas chaque variation d'arrondi. L'activation de notifications est hors V1.

## 7. API et front proposés

- `GET /api/discovery/doppelpark/parks/{id}` : éligibilité, couverture, versions, modes disponibles.
- `GET .../parks/{id}/matches?mode=...&cursor=...` : candidats filtrés avant sélection, score/confiance/raisons, page bornée.
- `GET /api/discovery/doppelpark/pairs/{pairId}` : comparaison stable et preuves paginées ; pas de dump d'empreinte interne arbitraire.
- `POST .../pairs/{pairId}/share-artifacts` : supports déterministes, bornés et datés.

Contrôler visibilité des deux parcs et sources, existence du dataset, mode valide, absence de division par zéro. Les médias masqués ne sont pas remplacés par une photo d'un autre parc.

Page mobile : deux en-têtes compacts puis une comparaison verticale ; graphiques accompagnés des valeurs et d'une table accessible. Le détail technique du score est dépliable. Pas de radar trompeur à axes hétérogènes non normalisés. SSR de la paire avec réserves et dates ; indexation réservée à une sélection de paires riches, pas tout le produit cartésien.

## 8. Supports de diffusion

Carte horizontale : deux parcs, localisation, indice « composition documentée », deux ressemblances et une limite lisible. Une carte 87/100 ne doit pas signifier « tu aimeras ce parc à 87 % ». Carrousel : question → ressemblances → différences → limites → lien.

Instagram/Reels/TikTok : transition A/B avec étiquettes et compteurs ; export sans marque imposée pour les canaux qui l'interdisent. Facebook : invitation à vérifier la comparaison sur le site. LinkedIn : présentation occasionnelle de la méthode explicable et d'un rapprochement industriel, sans surpromesse statistique. Aucun post automatique de toutes les paires ; sélection éditoriale et déduplication.

## 9. Lots et tests

| Lot | Livrable | Acceptation |
| --- | --- | --- |
| C03-A | Audit couverture, cohorte et profil V1 | seuils mesurés, absence d'inférence depuis les champs libres |
| C03-B | Empreintes, IDF, score et contributions | formules testées, reproductibilité et symétrie |
| C03-C | Calcul par lots, top-K et API | candidats lointains non perdus, bornes et invalidation |
| C03-D | Comparaison mobile/SSR et entrée sur fiche parc | score compréhensible, données insuffisantes traitées |
| C03-E | Diffusion, admin/observabilité et recette production | V1 complète, gate C03 avant C07 |

Tests : empreintes identiques donnent 100 dans les deux sens ; totalement disjointes donnent 0 ; groupe absent rend inéligible ; parc A×2 avec mêmes proportions conserve son score et affiche sa taille différente ; inconnus partagés n'augmentent pas le score ; IDF borné ; ordre des éléments sans effet ; noms identiques/IDs différents ; distance manquante ; statuts historiques ; pairId non public ; score 54,999 et seuil avant arrondi ; top-K déterministe ; modification/retrait source ; rendu huit langues.

Avant lancement, examiner manuellement un échantillon de paires hautes/moyennes/basses et des parcs de tailles/pays/types différents. Documenter les contre-exemples et la couverture, sans utiliser uniquement des exemples flatteurs. Métriques : clic vers le parc découvert, ouverture des preuves, partage, retour pour une nouvelle comparaison ; ne pas assimiler automatiquement un score élevé à un bon résultat produit.
