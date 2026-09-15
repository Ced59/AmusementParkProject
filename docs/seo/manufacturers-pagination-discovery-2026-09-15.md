# Pagination explorable de l’annuaire des fabricants

## Défaut et périmètre

L’annuaire public `/fr/manufacturers` chargeait 24 fiches, puis changeait de page uniquement dans l’état du composant. Ses boutons de pagination ne fournissaient aucun `href` et un accès direct à `?page=2` rechargeait la première page. Le plan HTML donnait déjà un autre chemin vers ces fiches ; il ne rendait pas la pagination de l’annuaire elle-même adressable.

La correction concerne uniquement `/<lang>/manufacturers?page=N`. Elle réutilise le composant de pagination et la grammaire bornée introduite pour les parcs : un entier de 1 à 999999, sans doublon ni autre paramètre pour une page indexable. Le helper `public-parks-location.ts` et son test deviennent `public-directory-location.ts` et son test ; les appelants utilisent directement cette implémentation commune, sans ancien wrapper. Le comportement de l’annuaire des parcs est conservé.

## Comportement attendu

- Une navigation standard charge exactement la page demandée, à raison de 24 fiches au maximum. Les liens précédent, suivant et numérotés sont présents dans le HTML initial et restent lisibles après l’optimisation destinée aux robots sans JavaScript.
- La racine et `page=1` ont pour canonical la racine de l’annuaire. Une page ultérieure validée dispose de sa propre canonical, de la même URL Open Graph, d’un titre paginé et d’un fil d’Ariane visible et JSON-LD. Les traductions de la racine sont conservées ; aucune alternative `hreflang` n’est supposée pour une page ultérieure.
- Les métadonnées restent `noindex,follow` jusqu’à validation de la réponse correspondant à l’URL. Les en-têtes SSR ne remplacent pas cette décision par une indexabilité déduite de la seule syntaxe.
- Une page mal formée ou hors plage retourne 404. Une erreur API ou une pagination incohérente retourne 503. Les corps d’erreur et les réponses de secours CSR restent exclus. Une dernière page de moins de 24 fiches est valide ; des données non routables ne constituent pas une page utile.
- La recherche et les tailles personnalisées restent interactives. Elles retirent l’ancienne query de pagination et ne génèrent pas de nouvelles pages indexables. Une query de suivi conserve, après validation des données, sa canonical propre, avec `noindex` et sans alternatives ni JSON-LD.
- Le retour arrière restaure la page et la taille standard. Le marqueur d’une suppression de query est éphémère (`NavigationExtras.info`), jamais enregistré dans l’historique. Les réponses et recherches retardées d’une navigation abandonnée sont ignorées.

## Coût et validation

Le port paginé existant est conservé. Aucune récupération du catalogue complet, prélecture de page, nouvelle requête de carte ou nouvelle dépendance n’est ajoutée. L’API utilise déjà un tri stable par nom puis identifiant, avec comptage et enrichissements limités aux identifiants de la page. Le coût de traitement et de HTML ajouté reste proportionnel à ces 24 fiches ; le coût existant du comptage et du `skip` MongoDB dépend toujours des données et du numéro de page. Un changement de langue réutilise les données multilingues déjà chargées.

La clé de cache SSR conserve la query, tandis que l’invalidation existante par pathname couvre toutes les pages de `/manufacturers`. Aucun réglage serveur ou cache n’est modifié.

Les tests couvrent la navigation Angular réelle, les événements de langue avant/après navigation, le retour arrière, les recherches différées, la dernière page à une fiche, les données non routables, les erreurs et les métadonnées après `NavigationEnd`. Les tests de transport vérifient les directives après préparation du HTML pour les robots. Le fil de navigation utilise un retour à la ligne et un débordement des mots maîtrisés ; les règles de grille mobile, tablette et bureau existantes sont conservées. La validation locale légère porte sur la syntaxe TypeScript, les templates Angular, les fonctions pures et l’architecture des ports ; les suites Angular et la construction complète sont exécutées en CI, sans serveur local.

## Limite

Cette modification corrige un défaut de découverte HTML. Elle ne modifie ni les sitemaps XML ni leur publication, et ne constitue pas une résolution ou une explication du statut de récupération observé dans Search Console. Elle ne garantit pas l’indexation, le classement ou du trafic supplémentaire.
