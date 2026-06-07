# Correctif sitemap — URLs publiques non tronquées

## Diagnostic

La génération des sitemaps réutilisait encore une ancienne limite de volume dynamique via `MaxDynamicUrlsPerType`. Cette limite pouvait tronquer les pages publiques référencées, notamment les park items, mais aussi les parcs et les références.

## Correction

- suppression de la propagation de `MaxDynamicUrlsPerType` dans les commandes et requêtes SEO ;
- suppression du champ de contexte sitemap associé ;
- suppression du réglage `Seo:MaxDynamicUrlsPerType` dans la configuration applicative ;
- génération des sections sitemap à partir de l'ensemble des candidats publics visibles ;
- suppression des `.Take(...)` sur les références sitemap ;
- conservation du découpage protocolaire par sitemap à 50 000 URLs maximum par fichier ;
- découpage par langue et par chunks si une section dépasse la limite protocolaire.

## Règle cible

Un sitemap ne doit pas appliquer de limite métier arbitraire. Les seules exclusions doivent venir des règles éditoriales et de visibilité : entité visible, publique, parent visible, et non marquée `NotRelevant`.
