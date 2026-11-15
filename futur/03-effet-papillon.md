# 03 — C02 / L'effet papillon : retirer sans effacer

[Retour](README.md) · Premier concept à réaliser.

## 1. Promesse et formulation honnête

Le visiteur sélectionne un constructeur et découvre ce qui disparaît **de la vue simulée du catalogue**. Il peut comparer avant/après, comprendre les parcs concernés et partager un résultat frappant mais exact.

Le titre narratif « Et si ce constructeur n'avait jamais existé ? » doit être accompagné immédiatement de la règle : **simulation fondée sur les attractions associées dans le catalogue ; aucune prédiction d'une histoire alternative réelle**. Il est impossible de déduire les machines de remplacement, les choix économiques, les faillites de parcs ou les conséquences industrielles indirectes. Aucune de ces conséquences ne sera inventée.

La fonctionnalité ne supprime, ne masque et ne modifie jamais une donnée source. Un parc sans attraction restante dans la simulation n'est ni fermé ni inexistant ; restaurants, hôtels, spectacles, personnel et services ne disparaissent pas par contagion.

## 2. Périmètre V1

Sélection d'un ou deux constructeurs résolus, périmètre monde/pays/parc, deux modes explicitement distincts :

- `current-known` : uniquement les attractions publiables dont l'état en activité est établi par la normalisation validée ; statut inconnu affiché comme exclu du périmètre, jamais assimilé à actif ;
- `catalogue-associations` : toutes les associations publiables du catalogue, y compris historiques, avec leur statut ; aucun libellé laissant croire qu'elles étaient toutes présentes simultanément.

Le mode initial est `current-known` lorsqu'il dispose d'assez de données fiables ; sinon proposer explicitement le mode catalogue sans substitution silencieuse. Le seuil de disponibilité est documenté au cadrage. Toujours permettre de comprendre ce qui n'entre pas dans le calcul.

La V1 inclut une page dédiée, une entrée depuis la fiche constructeur, synthèse chiffrée, liste de parcs, carte facultative, détail avant/après, URL stable du scénario et supports sociaux. Les comparaisons à plus de deux constructeurs, l'animation vidéo longue, les simulations historiques datées et les conséquences économiques sont hors V1.

## 3. Parcours utilisateur complet

1. Accès depuis constructeur, navigation découverte ou publication sociale ; scénario déjà prérempli si partagé.
2. Choix du périmètre et du mode, puis recherche de constructeur avec nombre d'associations disponibles et indication de couverture.
3. Bouton « Lancer la simulation » ; résultat avec trois nombres : attractions recensées dans ce périmètre, associations retirées avec certitude, attractions dont le constructeur est inconnu.
4. Comparaison avant/après conservant les éléments non concernés. Liste des parcs avec nombres et explication, tri par nombre retiré ou proportion, jamais classement de qualité.
5. Ouverture d'un parc : attractions concernées, preuve constructeur, liens fiches, statuts et date du dataset. Carte et liste restent synchronisées.
6. Ajout éventuel d'un second constructeur et recalcul depuis le même dataset ; comparaison clairement étiquetée.
7. Partage du scénario, pas d'un état visuel impossible à reproduire. Un ami ouvre les mêmes paramètres et le même résultat daté, ou une indication de retrait/expiration.
8. Proposition d'un autre scénario éditorial sélectionné, sans multiplication de messages promotionnels.

Sur mobile : liste d'abord, carte à ouvrir ; avant/après via sélecteur et tableau lisible, pas une carte qui exige un drag précis. Mode reduced-motion disponible ; aucun flash ou disparition de contenu essentiel.

## 4. Calcul et dénominateurs

Soit `U` l'ensemble des IDs uniques des attractions publiques éligibles au mode/périmètre. Soit `K` celles dont le constructeur est résolu, `Q = U \ K`, et `S` les IDs de constructeurs sélectionnés. Le retrait certain est `R = { a ∈ K : manufacturer(a) ∈ S }`. Les éléments restants dans la vue sont `U \ R`. Les inconnus `Q` y restent mais sont signalés « association inconnue » ; ne pas affirmer qu'ils sont indépendants de S.

Afficher `|R|`, `|U|`, `|Q|` et couverture `|K| / |U|`. La proportion primaire est `|R| / |U|`, nommée « attractions recensées liées aux constructeurs retirés ». Une proportion sur constructeurs connus `|R| / |K|` est possible en détail, avec un libellé différent. Jamais de division par zéro, jamais de pourcentage sans dénominateur.

Exemple de test purement synthétique : 8 attractions éligibles, dont 3 M1, 2 M2, 1 M3 et 2 inconnues. Retirer M1 donne 3 retraits certains sur 8, soit 37,5 %, couverture constructeur 75 %, 5 restantes dont 2 incertaines. Retirer M1+M2 donne 5/8, pas une addition de pourcentages arrondis. Les calculs utilisent les entiers puis arrondissent à l'affichage localisé.

Le modèle actuel expose un constructeur unique par attraction ; ne pas le convertir en liste multiconstructeurs sans besoin et migration séparément justifiée. L'algorithme par ensembles reste robuste à l'ajout futur de rôles multiples, mais la V1 n'invente pas ces rôles. Dédupliquer par ID d'attraction, pas par nom ; si des doublons de catalogue sont détectés, les signaler dans la couverture plutôt que modifier automatiquement les données.

Pour chaque parc, calculer les mêmes ensembles sur les enfants éligibles. Le nombre de parcs concernés est le cardinal des ParkId de R. Le nombre de pays concernés utilise uniquement les CountryCode connus ; rapporter les pays inconnus séparément. Les parcs sans coordonnées restent dans les totaux et la liste, avec un compteur « non représentés sur la carte ».

## 5. Architecture et persistance

Domaine : service pur de simulation recevant une projection immuable et des options validées, retournant agrégats et contributions. Application : lecture du dataset, normalisation de la demande, cache, orchestration des supports. Infrastructure : index des associations et persistance reconstruisible ; aucune commande d'update du catalogue.

Paramètres canoniques : mode, périmètre, IDs constructeurs dédupliqués/triés, version dataset et algorithme. Clé de cache calculée sur ces paramètres ; changer l'ordre de M1/M2 ne crée pas un autre scénario. Les options inconnues ou excessives sont rejetées, pas ignorées silencieusement.

Un `ButterflyScenario` public stable peut contenir ID, paramètres canoniques, datasetId, algorithmVersion, dates, synthèse et statut. Persister un scénario personnel uniquement lorsque le visiteur demande de sauvegarder/partager ; un GET de consultation ne doit pas remplir la base de millions de variantes. Les scénarios éditoriaux ont une conservation distincte des variantes utilisateur.

Les résultats détaillés sont paginés. Les filtres d'affichage (tri, page) ne changent pas le total du scénario ; l'UI doit distinguer total du périmètre et lignes affichées. Ne pas télécharger tout le catalogue dans Angular.

## 6. Contrats HTTP proposés

| Opération | Entrée / sortie |
| --- | --- |
| `GET /api/discovery/butterfly/manufacturers` | recherche bornée, périmètre/mode ; candidats résolus et couverture |
| `GET /api/discovery/butterfly/preview` | paramètres bornés ; synthèse sans stockage automatique ni média lourd |
| `POST /api/discovery/butterfly/scenarios` | demande idempotente de scénario partageable ; serveur recalcule/relit, ID stable |
| `GET /api/discovery/butterfly/scenarios/{id}` | paramètres, dataset, nombres, couverture, réserves, pagination |
| `GET .../{id}/parks` | liste paginée triée avec contributions et compteurs |
| `GET .../{id}/parks/{parkId}` | preuves uniquement pour un parc du scénario, contrôle de visibilité |
| `POST .../{id}/share-artifacts` | format/locale/template autorisés ; génération sous quota |

Limiter nombre de constructeurs, longueur des recherches, taille des pages et cadence des previews. Une prévisualisation coûteuse peut devenir un job asynchrone retournant un statut, mais pas un calcul illimité dans un GET. Une panne de rendu ne rend pas le scénario indisponible.

États UI/HTTP : dataset en préparation, aucun constructeur éligible, aucun effet dans ce périmètre, résultat partiel documenté, scénario retiré, paramètres invalides, rate limit. Une sélection sans effet est un résultat valable, pas une erreur serveur.

## 7. Rendu social propre au concept

Visuel 1200×630 : identité constructeur en texte ou image autorisée, chiffre certain, périmètre, mode et date, mention de simulation, URL courte du site. Aucun « X parcs disparaîtraient » : préférer « X parcs du catalogue sont concernés ». Le visuel doit rester exact sans lecture de la légende.

Carrousel : question → avant → retraits documentés → incertitudes/couverture → invitation à tester. Vidéo verticale courte : compteur et transition déterministe avant/après, sans vraie destruction d'un parc ni photo laissant croire à une fermeture. Le jeu de données et le message de réserve sont figés avec l'artefact.

Facebook : partage du scénario depuis le site et post de Page sélectionné par la politique autorisée. Instagram/TikTok : formats natifs et exports sans dépendance à une API ; ne pas supposer que la légende peut contenir un lien cliquable. LinkedIn : angle diversité des fournisseurs, avec avertissement qu'il s'agit d'associations du catalogue, pas d'une mesure de dépendance financière. Les autres réseaux réutilisent les capacités du document 06.

Aucune image de carte issue d'un téléchargement massif de tuiles ; utiliser un rendu schématique propre avec les coordonnées disponibles ou une visualisation sans fond géographique. Si les droits médias sont insuffisants, une carte typographique est un rendu complet acceptable.

## 8. Lots et critères de sortie

| Lot | Contenu | Acceptation |
| --- | --- | --- |
| C02-A | Cadrage, couverture, normalisation statuts et constructeur | rapport réel, seuils justifiés, inconnus conservés |
| C02-B | Projection versionnée et calcul pur | fixture 8 attractions exacte, aucune mutation source |
| C02-C | API, scénarios, cache et pagination | déterminisme, limites, visibilité, tests d'accès |
| C02-D | Écrans avant/après, preuves, carte/listes, huit langues | mobile, clavier, SSR, absence de fausse fermeture |
| C02-E | Artefacts, partage natif/FB et publication draft existante | images/exports lisibles, mode et couverture présents |
| C02-F | Admin minimale, métriques, review et production | C02 VERIFIED selon document 07, puis arrêt |

## 9. Recette indispensable

Tester sélections inversées, constructeur répété, inconnu, renommé, supprimé/masqué ; parent masqué ; attraction visible sous parent non découvrable ; historique vs actuel ; zéro attraction, zéro constructeur connu, une seule attraction ; nombres arrondis ; doublon de nom ; coordonnées manquantes ; tri/pagination ; image retirée ; résultat partagé ancien ; cohérence des huit langues.

Contrôler qu'aucun endpoint n'appelle un repository de mutation de Park/ParkItem. Comparer les compteurs de synthèse à la somme des contributions, hors inconnus explicitement exclus. Vérifier sur un exemple public choisi à partir du vrai dataset que chaque attraction retirée possède la preuve attendue ; aucun chiffre du brainstorming n'est une fixture de production.

Métriques : vue→simulation, comparaison avec second constructeur, consultation des preuves/fiches, partage initié et arrivée sur scénario partagé, retour pour une nouvelle simulation. Faire une revue produit après assez de sessions, sans confondre intérêt ponctuel et rétention durable.
