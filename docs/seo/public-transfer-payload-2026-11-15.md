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
| Destinations recommandées, depuis un parc ou une attraction autonome | Vue `cardView=true` : uniquement les champs des cartes et les résumés déjà formatés, dans toutes les langues d'origine. Rang, liens, coordonnées, distances, durées, statuts, logos et photos sont conservés. |
| Disponibilité des vidéos sur une fiche de parc ou de parkItem | `includeItems=false` : mêmes filtres, autorisations et pagination ; les objets vidéo inutilisés ne sont plus sérialisés. |
| Compteur de l'onglet des vidéos de parkItems | Même projection de pagination, puis chargement normal de la galerie quand elle est sélectionnée. |
| Existence de l'historique d'un parkItem | Réponse booléenne utilisant exactement la même requête de chronologie publique ; aucun événement ni objet parent transmis pour le lien. |
| Vérification de la présence d'un logo dans le repli de la fiche parc | Pagination seule ; les images du parc et des parkItems restent complètes car elles déterminent l'image sociale. |
| Aperçu d'image de l'onglet parkItems | Conservé : le premier objet est réellement affiché, il ne s'agit pas d'un simple compteur. |
| Descriptions principales, coordonnées, météo, calendriers et galeries consultables | Conservés : ces données alimentent l'affichage, les interactions, la prochaine ouverture ou les changements de langue. |

Les réponses API historiques restent les réponses par défaut. Les options de projection
font partie de la clé des caches HTTP et Angular ; le cache serveur varie déjà sur tous
les paramètres de requête. Aucun transfert Angular n'est désactivé, aucun appel réseau
supplémentaire n'est nécessaire à l'hydratation, au changement de langue ou d'unités.

Le formatage des résumés suit les règles existantes du navigateur : suppression des
balises, traitement de `&nbsp;`, espaces ECMAScript, longueur UTF-16 et coupure naturelle
à 140 caractères. Le navigateur utilise ensuite ces résumés sans les reformater.
Les textes principaux et leurs traductions intégrales restent dans les fiches de détail.

Aucun changement de contenu éditorial, de visibilité, de canonical, de hreflang, de
JSON-LD, de CSS ou de politique de rendu Google n'est introduit.

## Vérifications

- Tests du formatage, des champs des cartes et de la conservation des huit langues.
- Comparaison des cartes complètes et compactes dans les huit langues et les deux
  systèmes de mesure, y compris un texte contenant des chevrons littéraux.
- Tests de pagination exacte sans exécution du mapper d'objets inutilisés.
- Tests du transport HTTP et des façades existantes : liens, galeries, navigation,
  requêtes obsolètes, erreurs et disponibilité.
- Captures de référence en production avant déploiement : les huit langues
  d'Adventuredome, SkyJump Las Vegas et quatre grandes fiches de parc.
- Après déploiement : vérifier la version, mesurer le HTML/JSON et comparer les
  textes, liens, métadonnées et données structurées avec ces références ; vérifier
  l'hydratation dans Chromium mobile et bureau ainsi que les réponses historiques.
