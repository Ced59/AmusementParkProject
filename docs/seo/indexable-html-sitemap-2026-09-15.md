# Indexabilité du plan HTML — 15 septembre 2026

Le plan HTML `/fr/sitemap` et ses autres versions linguistiques étaient volontairement en `noindex,follow`, même après l’ajout de liens de navigation directement présents dans le HTML. À la demande explicite du propriétaire, les pages utiles et valides de ce plan peuvent désormais être indexées.

## Périmètre

- La racine et chaque branche deviennent `index,follow` seulement après résolution de leur hiérarchie, validation de la pagination et chargement d’au moins une entrée. La limite de 100 lignes reste inchangée.
- Chaque page a sa propre adresse canonique et son `og:url`, avec seulement les paramètres `node` et `page` normalisés. Les paramètres vides et la page 1 sont omis. Le service canonique général et les règles des autres pages filtrées restent inchangés.
- Les titres, descriptions et données `BreadcrumbList` reprennent les rubriques réellement chargées et le numéro de page. La racine annonce ses huit langues et `x-default`. Les branches n’annoncent pas d’alternatives linguistiques : les sections de snapshot et leurs nombres de pages ne prouvent pas une équivalence entre langues.
- Pendant le chargement, pour une branche vide, une requête invalide ou une erreur, les métadonnées restent `noindex,follow`, sans canonique périmée, alternates ni données structurées de la page précédente. Les réponses 404 et 503 existantes sont conservées.
- Les métadonnées validées sont réappliquées après `NavigationEnd` et après une réponse asynchrone. Cela couvre notamment les données SSR immédiatement disponibles, que les valeurs par défaut globales pouvaient sinon écraser. Un changement de langue ou de page invalide le contexte précédent.
- La couche de livraison SSR utilisait également une exclusion générale pour les URL avec paramètres. Une exception réservée au plan HTML et à sa syntaxe autorisée laisse désormais intactes les balises issues d’Angular ; elle ne force jamais `index`. Le même parseur est partagé avec le composant. Les réponses 404/503 et les réponses de secours CSR restent exclues, dans l’en-tête `X-Robots-Tag` comme dans le HTML.

## Vérification

Les tests couvrent le chargement synchrone avec application globale des valeurs par défaut à `NavigationEnd`, le chargement asynchrone, les réponses obsolètes, la langue émise avant navigation, les branches paginées, les paramètres dupliqués ou inconnus, les erreurs, les branches vides, les liens canoniques et la localisation dans les huit langues. Le test d’encodage compare directement l’adresse canonique à la sérialisation Angular utilisée par les liens.

Les tests de livraison exécutent également la préparation HTML Google/Bing et la politique finale des robots : conservation des métadonnées validées, des liens et de `BreadcrumbList`, maintien du `noindex` émis par Angular, exclusion HTTP/HTML des erreurs et des réponses de secours. L’utilitaire de paramètres et ses tests sont déplacés de la fonctionnalité sitemap vers `shared/utils/routing`, pour éviter deux validations divergentes.

Après déploiement, vérifier séquentiellement la racine, une branche et sa page 2 : HTTP 200, contenu initial utile, `robots` et `googlebot` à `index,follow`, canonique et `og:url` identiques à l’adresse normalisée, et liens présents sans exécution JavaScript. Vérifier également une branche inconnue (404, `noindex`) et l’absence d’alternates inventés sur les branches.

## Limites

Cette décision concerne exclusivement le plan HTML. Elle ne modifie ni la génération, ni le contenu, ni la livraison des fichiers XML et ne résout pas à elle seule leur statut dans Search Console. Une page devenue techniquement indexable n’est pas nécessairement indexée ou mieux classée. Les diagnostics et mesures privés des moteurs sont conservés hors du dépôt.
