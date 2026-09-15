# Pagination explorable de l’annuaire des parcs

Le composant de l’annuaire chargeait systématiquement la première page, y compris pour une arrivée sur `/fr/parks?page=2`. Le contrôle de pagination ne produisait que des boutons. Les liens des fiches étaient déjà présents dans le HTML initial, mais les pages suivantes ne disposaient pas d’un chemin HTML fonctionnel.

Cette correction concerne exclusivement les annuaires `/<lang>/parks`. Elle améliore la découverte par les liens HTML et ne modifie ni les sitemaps XML, ni leur soumission, ni leur traitement dans Search Console. Elle ne constitue pas une explication ou une résolution d’un sitemap XML en attente.

## Contrat de navigation

- La liste standard garde ses neuf fiches par page, ses parcs publics au statut `Operating` et son ordre actuel. L’API pagine déjà en base avec un ordre déterministe ; aucun contrat backend ne change.
- Chaque page standard est accessible directement en SSR. Les liens numérotés, précédent, suivant, premier et dernier utilisent de vrais `href`, sans dépendre d’un clic JavaScript du robot. Les autres utilisateurs du paginator conservent leur comportement actuel.
- La page 1 a pour canonical `/<lang>/parks`. Les pages suivantes ont leur propre canonical `?page=N`, reprise dans Open Graph. Les métadonnées ne deviennent indexables qu’après réception des données correspondant à la page et à la langue demandées.
- La racine conserve ses alternates linguistiques. Les pages suivantes n’en déclarent pas sans vérification de correspondance entre les pages des différentes langues. Le fil visible et son `BreadcrumbList` identifient la page et son annuaire parent.
- Recherches, filtres et tailles alternatives restent utilisables. Un changement retire l’ancien numéro de page de l’URL sans annuler la sélection. Ces états ne récupèrent pas les métadonnées indexables de la liste standard. Les paramètres externes, dont le suivi de campagne, restent fonctionnels mais non indexables.
- Numéro invalide ou page inexistante : 404 et noindex. Échec de données : 503 et noindex. Une première page réellement vide reste non indexable. La grammaire partagée entre Angular et le serveur ne suffit jamais à accorder l’indexabilité ; les erreurs et les fallbacks CSR restent exclus.

## Coût et cache

La navigation entre pages ne recharge pas la carte. Une arrivée SSR conserve la lecture habituelle des points de carte et une seule page de neuf fiches. Aucun préchargement de toutes les pages, nouveau réchauffement massif ou chargement intégral de l’annuaire n’est ajouté.

Le cache SSR inclut déjà la query dans sa clé ; les pages 1 et 2 ne partagent donc pas la même entrée. L’invalidation existante du pathname `/<lang>/parks` couvre toutes les pages. Les limites de taille, de capacité, d’authentification et de préparation SSR sont conservées. Cela ne dispense pas de mesurer les temps et les poids en production : une augmentation de la découverte peut augmenter les premières lectures de pages.

## Régressions couvertes

Les tests ciblent l’arrivée directe en page 2, les vrais liens après optimisation sans JavaScript, la navigation sans relecture de carte, la réutilisation du composant, les tailles et filtres, les changements de langue, `NavigationEnd`, les réponses asynchrones obsolètes, les pages invalides, les erreurs API, les canonicals et la couche finale d’en-têtes/balises SSR. Les garde-fous de mise en page permettent au fil localisé de revenir à la ligne et réutilisent la pagination mobile existante.

Les contrôles locaux restent légers : syntaxe TypeScript, analyse des templates, assertions des helpers réels, contrôle des ports de façades et `git diff --check`. Les compilations et tests Angular complets sont exécutés par la CI. Après déploiement, contrôler quelques pages et leurs en-têtes réels, dont une dernière page, une erreur et un test d’inspection Google, sans lancer de crawl concurrent.

Référence primaire : [pagination et chargement progressif — Google Search Central](https://developers.google.com/search/docs/specialty/ecommerce/pagination-and-incremental-page-loading). Google recommande des liens séquentiels explorables et une URL canonique propre à chaque page, en excluant les variantes filtrées de l’indexation.
