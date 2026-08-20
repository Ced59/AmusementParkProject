# M19.1 - Inventaire des routes publiques indexables

## Regle generale

Une route est indexable uniquement si elle expose du contenu public utile a un moteur de recherche. Les routes d'administration, de compte, d'authentification, d'erreur et les routes techniques restent en `noindex` ou sont exclues de `robots.txt` quand c'est pertinent.

## Routes publiques indexables

| Route | Statut | Justification |
|---|---|---|
| `/:lang/home` | `index,follow` | Page d'accueil publique. |
| `/:lang/parks` | `index,follow` | Liste publique des parcs visibles. |
| `/:lang/rankings` | `index,follow` | Classements publics alimentes par les notes visiteurs. |
| `/:lang/about` | `index,follow` | Page publique de presentation du projet. |
| `/:lang/contact` | `index,follow` | Page publique de contact. |
| `/:lang/versions` | `index,follow` | Historique public des versions. |
| `/:lang/privacy` | `index,follow` | Page legale publique utile et accessible. |
| `/:lang/park/:id/:slug` | `index,follow` | Detail d'un parc public visible. |
| `/:lang/park/:id/:slug/images` | `index,follow` | Galerie publique du parc à partir de trois images publiées. |
| `/:lang/park/:id/:slug/videos` | `index,follow` | Galerie vidéo publique du parc à partir de deux vidéos publiées. |
| `/:lang/park/:id/:slug/videos/:videoId/:videoSlug` | `index,follow` | Detail public d'une video publiee de parc. |
| `/:lang/park/:id/:slug/map` | `index,follow` | Carte publique quand au moins deux repères d'éléments publics sont disponibles. |
| `/:lang/park/:id/:slug/zones` | `index,follow` | Vue publique à partir de deux zones visibles contenant des éléments publics. |
| `/:lang/park/:id/:slug/zone/:zoneId/:zoneSlug` | `index,follow` | Detail public d'une zone visible qui contient des elements publics. |
| `/:lang/park/:id/:slug/weather` | `index,follow` | Meteo publique du parc quand des previsions sont disponibles. |
| `/:lang/park/:id/:slug/opening-hours` | `index,follow` | Horaires publics quand des jours d'ouverture sont disponibles. |
| `/:lang/park/:id/:slug/pricing` | `index,follow` | Tarifs publics quand au moins une offre est disponible. |
| `/:lang/park/:id/:slug/comments` | `index,follow` | Discussion publique quand au moins un commentaire est visible. |
| `/:lang/park/:id/:slug/items` | `index,follow` | Exploration publique à partir de deux éléments visibles, sans filtres de query string. |
| `/:lang/park/:id/:slug/item/:itemId/:itemSlug` | `index,follow` | Detail public d'un element visible. |
| `/:lang/park/:id/:slug/item/:itemId/:itemSlug/images` | `index,follow` | Galerie publique d'un élément visible à partir de trois images publiées. |
| `/:lang/park/:id/:slug/item/:itemId/:itemSlug/videos` | `index,follow` | Galerie vidéo publique d'un élément visible à partir de deux vidéos publiées. |
| `/:lang/park/:id/:slug/item/:itemId/:itemSlug/videos/:videoId/:videoSlug` | `index,follow` | Detail public d'une video publiee d'element visible. |
| `/:lang/park/:id/:slug/item/:itemId/:itemSlug/comments` | `index,follow` | Discussion publique quand au moins un commentaire est visible. |
| Routes publiques `history` | `index,follow` | Chronologie publique d'un parc, d'un element ou d'une attraction autonome quand au moins deux evenements sont visibles. |
| `/:lang/park-operator/:id/:slug` | `index,follow` | Reference publique d'exploitant. |
| `/:lang/park-founder/:id/:slug` | `index,follow` | Reference publique de fondateur. |
| `/:lang/park-manufacturer/:id/:slug` | `index,follow` | Reference publique de constructeur. |

Langues servies : `en`, `fr`, `es`, `de`, `it`, `pl`, `nl`, `pt`.

## Regles d'inclusion sitemap

| Type d'URL | Regle |
|---|---|
| Pages statiques | Une URL par langue supportee pour `home`, `parks`, `rankings`, `about`, `contact`, `versions` et `privacy`. |
| Parcs | Le parc doit avoir un `id`, un nom, `IsVisible = true` et un statut admin different de `NotRelevant`. |
| Park items | L'element doit avoir un `id`, un `parkId`, un nom, `IsVisible = true`, un statut admin different de `NotRelevant`, et son parc parent doit respecter les regles publiques des parcs. |
| Listes d'éléments | La page `items` d'un parc est incluse seulement si le parc public contient au moins deux park items publics. |
| Cartes | La page `map` est incluse seulement si au moins deux park items publics possèdent un repère exploitable. |
| Zones | La page `zones` exige au moins deux zones publiques ; chaque détail de zone exige au moins un park item public. |
| Images | Les galeries sont incluses à partir de trois images publiées pour le parc ou le park item public. |
| Vidéos | Les galeries sont incluses à partir de deux vidéos publiées ; les détails restent filtrés par langue quand une vidéo déclare des langues. |
| Historiques | Une chronologie est incluse seulement si sa vue canonique expose au moins deux evenements publics. |
| References | Les exploitants et constructeurs `NotRelevant` sont exclus ; les fondateurs publics avec `id` et nom sont inclus. |

## Routes publiques non indexables

| Route | Statut | Justification |
|---|---|---|
| `/:lang/park/:id/:slug/items?*` | `noindex,follow` | Combinaisons de filtres items non validees comme pages SEO autonomes. |
| `/:lang/park/:id/:slug/zones?*` | `noindex,follow` | Combinaisons de filtres zones non validees comme pages SEO autonomes. |
| `/:lang/park/:id/:slug/zone/:zoneId/:zoneSlug?*` | `noindex,follow` | Variante filtree d'une zone publique. |
| `/:lang/park/:id/:slug/map?*` | `noindex,follow` | Variante filtrée ou paramétrée de la carte publique. |
| `/:lang/park/:id/:slug/images?*` | `noindex,follow` | Combinaisons de filtres images non validees comme pages SEO autonomes. |
| `/:lang/park/:id/:slug/videos?*` | `noindex,follow` | Combinaisons de filtres videos non validees comme pages SEO autonomes. |
| `/:lang/park/:id/:slug/weather?*` | `noindex,follow` | Variante filtree ou parametree de la meteo publique. |
| `/:lang/park/:id/:slug/opening-hours?*` | `noindex,follow` | Variante paramétrée des horaires publics. |
| `/:lang/park/:id/:slug/pricing?*` | `noindex,follow` | Variante paramétrée des tarifs publics. |
| Routes publiques `comments` vides ou avec une query string | `noindex,follow` | Discussion sans contenu ou variante paramétrée sans valeur SEO autonome. |
| `/:lang/park/:id/:slug/item/:itemId/:itemSlug/images?*` | `noindex,follow` | Combinaisons de filtres images non validees comme pages SEO autonomes. |
| `/:lang/park/:id/:slug/item/:itemId/:itemSlug/videos?*` | `noindex,follow` | Combinaisons de filtres videos non validees comme pages SEO autonomes. |
| Routes publiques `history` avec moins de deux evenements ou une query string | `noindex,follow` | Chronologie trop legere ou variante parametree sans valeur SEO autonome. |
| Collections sous leur seuil de valeur | `noindex,follow` | Moins de trois images, ou moins de deux vidéos, éléments, zones ou repères de carte. |
| Météo, horaires ou tarifs sans donnée publique | `noindex,follow` | Sous-page utile à la navigation mais sans contenu autonome exploitable. |
| `/:lang/not-found` | `noindex,follow` | Page 404 publique. |
| route wildcard publique | `noindex,follow` | Affiche la vraie page 404 publique. |

## Routes privees / techniques non indexables

| Route | Statut | Justification |
|---|---|---|
| `/:lang/admin/**` | `noindex,nofollow` | Back-office prive. |
| `/:lang/profile` | `noindex,nofollow` | Compte utilisateur prive. |
| `/:lang/confirm-account` | `noindex,nofollow` | Parcours auth/email. |
| `/:lang/forgot-password` | `noindex,nofollow` | Parcours auth/email. |
| `/:lang/reset-password` | `noindex,nofollow` | Parcours auth/email. |
| `/api/images/**` | `Allow` dans `robots.txt` | Images publiques utilisées par les pages indexables. |
| `/api/**` | `Disallow` dans `robots.txt` | API technique consommée par le front, hors images publiques. |
| `/robots.txt` | Non applicable | Fichier technique public. |
| `/sitemap.xml` | Non applicable | Sitemap index public, proxifie vers l'API. |
| `/sitemaps/*.xml` | Non applicable | Sections techniques du sitemap, proxifiees vers l'API. |

## Validation attendue

- Les pages publiques indexables recoivent `title`, `description`, `canonical`, `robots=index,follow` et des alternates `hreflang`.
- Les pages publiques indexables dynamiques doivent etre rendues en SSR sur cache miss pour eviter que les robots ne voient uniquement le shell `<app-root>`.
- Les pages privees recoivent `robots=noindex,nofollow`.
- La page 404 publique recoit `robots=noindex,follow`.
- `robots.txt` reference `/sitemap.xml`, autorise `/api/images/` et exclut le reste de `/api/`, admin et compte/auth.
