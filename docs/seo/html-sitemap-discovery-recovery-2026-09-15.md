# Découverte des pages dans le plan HTML — 15 septembre 2026

## Défaut identifié

Le commit `0cafc2541` du 6 juillet 2026 a limité le rendu serveur du plan HTML aux rubriques racines pour réduire son poids. Le chargement des liens détaillés reposait ensuite sur JavaScript. Or les robots recevaient une version sans JavaScript, et `/api/` reste interdit à l’exploration dans `robots.txt`.

Le plan HTML est également en `noindex,follow`. Google peut ignorer le rendu JavaScript d’une page portant déjà `noindex`. Rétablir les scripts ne suffit donc pas à garantir la découverte des liens ajoutés seulement après hydratation. La liste principale des parcs ne compense pas ce manque : sa pagination repose sur des boutons, que Google ne clique pas.

Références : [traitement JavaScript par Google](https://developers.google.com/search/docs/crawling-indexing/javascript/javascript-seo-basics), [pagination et liens explorables](https://developers.google.com/search/docs/specialty/ecommerce/pagination-and-incremental-page-loading).

Cette rupture de découverte est distincte de la récupération du fichier XML. Elle ne prouve pas à elle seule la cause de toute la baisse d’impressions, ni celle du statut « impossible de récupérer le sitemap » dans Search Console.

La découverte, le rendu et l’indexation contribuent également à l’éligibilité aux réponses génératives de Google : une page doit être indexée, autorisée à présenter un extrait et le site doit être inclus dans ces fonctionnalités dans Search Console. Google ne demande aucune balise spéciale pour l’IA ; ces corrections rétablissent des conditions techniques utiles sans garantir une citation, conformément à son [guide officiel d’optimisation pour la recherche générative](https://developers.google.com/search/docs/fundamentals/ai-optimization-guide).

## Correction

Le plan HTML utilise la même navigation rendue côté serveur pour les visiteurs et les robots :

- `/fr/sitemap` contient les liens vers les rubriques ;
- `?node=parks` expose les parcs ;
- `?node=parks/park:<identifiant>` expose les rubriques du parc ;
- `?node=parks/park:<identifiant>/park-items:<identifiant>` expose ses attractions ;
- les rubriques d’histoire donnent accès aux articles ;
- « Toutes les pages » complète cet arbre à partir des sections publiées du sitemap, notamment pour les attractions autonomes, en conservant toutes les familles de pages présentes dans ces snapshots.

Chaque niveau et chaque page de résultats possède un véritable lien `href`. Cent entrées au maximum sont affichées par page, avec des liens précédent/suivant. L’API existante charge seulement les collections de la branche choisie et de ses ancêtres ; le navigateur ne demande plus le snapshot complet en arrière-plan. Le fil d’Ariane utilise les libellés des nœuds effectivement retournés, sans reprendre un nom arbitraire dans l’URL.

## Limites et protections

- La politique `noindex,follow` du plan HTML reste en place. Le chemin public existant est conservé : la navigation utilise ses paramètres `node` et `page`.
- Le plan HTML n’émettait pas de `BreadcrumbList`. Les réponses à paramètres suppriment déjà tout JSON-LD par la normalisation des pages `noindex` ; cette politique est conservée, sans ajouter un schéma qui disparaîtrait du HTML serveur.
- Seuls `node` et `page` sont acceptés. Les paramètres répétés ou malformés, les hiérarchies inconnues et les pages hors limites renvoient 404. Une panne de données renvoie 503.
- Le chemin est limité à six niveaux. Chaque nœud doit appartenir aux enfants réellement retournés par le niveau précédent avant qu’une requête soit adressée à sa branche.
- Les clés du cache SSR distinguent les paramètres `node` et `page`. Les réponses en erreur ne remplacent pas la racine et ne sont pas enregistrées comme des pages réussies.
- Les liens générés omettent `page=1` et les paramètres vides. Aucune permission supplémentaire de parcours de `/api/` n’est nécessaire.
- Le cache des réponses API et les protections existantes de taille du cache HTML restent actifs. Aucun rendu récursif de l’ensemble du site n’est introduit.

## Vérification

Les tests couvrent les véritables `href`, le chargement direct d’une branche, les liens vers les attractions et articles, la pagination, les changements de paramètres sur un composant réutilisé, l’annulation des requêtes obsolètes et les erreurs 404/503. Les tests du complément par sections vérifient la conservation des familles de pages fournies par les snapshots publics.

Après déploiement, vérifier une racine, une rubrique de parcs, un parc, une liste d’attractions, un historique et une section complémentaire avec et sans identification Googlebot. Vérifier que les liens détaillés sont présents dans la réponse HTML initiale, que les pages suivantes sont accessibles par `href` et que les anciennes réponses 404/503 ne sont pas masquées.

La validation séparée des sitemaps XML dans Search Console reste nécessaire. Une réponse HTTP 200 à un test public ou à l’outil d’inspection ne signifie pas que le service Sitemaps de Google a accepté le fichier.
