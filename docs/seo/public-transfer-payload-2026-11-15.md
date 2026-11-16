# Réduction des données de démarrage des pages publiques

## Problème mesuré

Sur la fiche Adventuredome en néerlandais, le HTML 6.0.39 représente 275 411 octets
avant compression, dont 87 011 octets dans `ng-state`. La réponse des destinations
recommandées représente à elle seule 60 815 octets de ce JSON : quatre fiches
complètes avec descriptions en huit langues et détails d'attractions sont transférées
pour des cartes qui affichent seulement un résumé de 140 caractères.

Les fiches Phantasialand, Europa-Park, Disneyland et Parc Astérix présentent le même
problème. Leur HTML initial est déjà complet et leurs canoniques sont correctes.
Cette optimisation ne constitue pas un diagnostic de la couverture d'indexation.

## Périmètre et équivalence fonctionnelle

| Appel / usage public audité | Décision |
|---|---|
| Catalogue des parcs et recherche serveur | Vue de cartes avec les résumés courts dans toutes les langues, mêmes pages, filtres et métadonnées de pagination. Les marqueurs de la carte restent complets. |
| Cartes de l'accueil : sélection aléatoire, parcs mis en avant, derniers parcs et articles | Résumés limités au format réellement affiché de chaque carte : 140 ou 142 caractères, avec son ellipse d'origine. Titres, liens, images, compteurs et mentions sponsorisées sont conservés. |
| Annuaire des constructeurs | Biographies limitées aux aperçus de 180 caractères, avec la même sélection de traduction, y compris les biographies vides ou composées d'images. |
| Noms de constructeurs des listes de parkItems | Projection identifiant/nom sur toutes les pages du chargement ; les filtres et les libellés sont identiques. |
| Destinations recommandées, depuis un parc ou une attraction autonome | Vue `cardView=true` : uniquement les champs des cartes et les résumés déjà formatés, dans toutes les langues d'origine. Rang, liens, coordonnées, distances, durées, statuts, logos et photos sont conservés. |
| Disponibilité des vidéos sur une fiche de parc ou de parkItem | `includeItems=false` : mêmes filtres, autorisations et pagination ; les objets vidéo inutilisés ne sont plus sérialisés. |
| Compteur de l'onglet des vidéos de parkItems | Même projection de pagination, puis chargement normal de la galerie quand elle est sélectionnée. |
| Existence de l'historique d'un parkItem | Réponse booléenne utilisant exactement la même requête de chronologie publique ; aucun événement ni objet parent transmis pour le lien. |
| Vérification de la présence d'un logo dans le repli de la fiche parc | Pagination seule ; les images du parc et des parkItems restent complètes car elles déterminent l'image sociale. |
| Aperçu d'image de l'onglet parkItems | Conservé : le premier objet est réellement affiché, il ne s'agit pas d'un simple compteur. |
| Descriptions principales, textes de parkItems recherchés localement, coordonnées, météo, calendriers et galeries consultables | Conservés : ces données alimentent l'affichage, les interactions, la prochaine ouverture ou les changements de langue. |

Les réponses API historiques restent les réponses par défaut. Les lecteurs des vues
compactes acceptent aussi les anciennes réponses pendant un déploiement progressif
ou un retour arrière, sans deuxième requête. Les options de projection
font partie de la clé des caches HTTP et Angular ; le cache serveur varie déjà sur tous
les paramètres de requête. Aucun transfert Angular n'est désactivé, aucun appel réseau
supplémentaire n'est nécessaire à l'hydratation, au changement de langue ou d'unités.

Le formatage des résumés suit les règles existantes du navigateur : suppression des
balises, traitement de `&nbsp;`, espaces ECMAScript, longueur UTF-16 et coupure naturelle
à 140 caractères. Le navigateur utilise ensuite ces résumés sans les reformater.
Les textes principaux et leurs traductions intégrales restent dans les fiches de détail.

Les optimisations de données conservent le contenu éditorial, les règles de visibilité,
les canonical, les hreflang, le JSON-LD et la politique de rendu Google.

## Filtre des destinations à proximité

Une évolution fonctionnelle demandée séparément accompagne ces optimisations : les
sections de proximité des parcs et des attractions isolées affichent les parcs seuls
par défaut. Un switch « Inclure les attractions isolées », traduit dans les huit
langues, permet de retrouver les quatre destinations les plus proches, toutes
catégories confondues. La distance du lieu le plus proche et les états vides suivent
le filtre actif.

L'unique appel initial ajoute `groupByKind=true` : il charge au maximum quatre parcs
et quatre attractions isolées en vue compacte, ordonnés par distance. Cela garantit
quatre parcs disponibles même si des attractions sont plus proches. Le switch filtre
ce petit ensemble en mémoire ; il ne refait aucun appel API. Les requêtes de recherche
géographique restent bornées et identiques en nombre. Sans cette option, le contrat
historique conserve sa limite globale et son mélange de destinations.

## Vérifications

- Tests du formatage, des champs des cartes et de la conservation des huit langues.
- Comparaison des cartes complètes et compactes dans les huit langues et les deux
  systèmes de mesure, y compris un texte contenant des chevrons littéraux.
- Tests de pagination exacte sans exécution du mapper d'objets inutilisés.
- Tests du transport HTTP et des façades existantes : liens, galeries, navigation,
  requêtes obsolètes, erreurs et disponibilité.
- Tests du filtre par défaut et des allers-retours du switch sans nouvelle requête,
  depuis les deux types de fiche, y compris un ensemble sans parc.
- Contrôle responsive du switch avec un libellé long, dans les deux thèmes.
- Captures de référence en production avant déploiement : les huit langues
  d'Adventuredome, SkyJump Las Vegas et quatre grandes fiches de parc.
- Après déploiement : vérifier la version, mesurer le HTML/JSON et comparer les
  textes, liens, métadonnées et données structurées avec ces références, en tenant
  compte du nouveau filtre demandé dans la section de proximité ; vérifier
  l'hydratation dans Chromium mobile et bureau ainsi que les réponses historiques.
