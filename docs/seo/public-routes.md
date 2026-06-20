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
| `/:lang/park/:id/:slug/images` | `index,follow` | Galerie publique du parc quand des images publiees existent. |
| `/:lang/park/:id/:slug/videos` | `index,follow` | Galerie video publique du parc quand des videos publiees existent. |
| `/:lang/park/:id/:slug/videos/:videoId/:videoSlug` | `index,follow` | Detail public d'une video publiee de parc. |
| `/:lang/park/:id/:slug/zones` | `index,follow` | Vue publique des zones visibles qui contiennent des elements publics. |
| `/:lang/park/:id/:slug/zone/:zoneId/:zoneSlug` | `index,follow` | Detail public d'une zone visible qui contient des elements publics. |
| `/:lang/park/:id/:slug/weather` | `index,follow` | Meteo publique du parc quand des previsions sont disponibles. |
| `/:lang/park/:id/:slug/items` | `index,follow` | Exploration publique des elements visibles du parc, sans filtres de query string. |
| `/:lang/park/:id/:slug/item/:itemId/:itemSlug` | `index,follow` | Detail public d'un element visible. |
| `/:lang/park/:id/:slug/item/:itemId/:itemSlug/images` | `index,follow` | Galerie publique d'un element visible quand des images publiees existent. |
| `/:lang/park/:id/:slug/item/:itemId/:itemSlug/videos` | `index,follow` | Galerie video publique d'un element visible quand des videos publiees existent. |
| `/:lang/park/:id/:slug/item/:itemId/:itemSlug/videos/:videoId/:videoSlug` | `index,follow` | Detail public d'une video publiee d'element visible. |
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
| Listes d'elements | La page `items` d'un parc est incluse seulement si le parc public contient au moins un park item public. |
| Zones | La page `zones` et les details de zones sont inclus seulement pour les zones visibles contenant au moins un park item public. |
| Images | Les galeries sont incluses seulement si des images publiees existent pour le parc ou le park item public. |
| Videos | Les galeries et details video sont inclus seulement si des videos publiees existent, avec filtrage par langue quand une video declare des langues. |
| References | Les exploitants et constructeurs `NotRelevant` sont exclus ; les fondateurs publics avec `id` et nom sont inclus. |

## Routes publiques non indexables

| Route | Statut | Justification |
|---|---|---|
| `/:lang/park/:id/:slug/items?*` | `noindex,follow` | Combinaisons de filtres items non validees comme pages SEO autonomes. |
| `/:lang/park/:id/:slug/zones?*` | `noindex,follow` | Combinaisons de filtres zones non validees comme pages SEO autonomes. |
| `/:lang/park/:id/:slug/zone/:zoneId/:zoneSlug?*` | `noindex,follow` | Variante filtree d'une zone publique. |
| `/:lang/park/:id/:slug/images?*` | `noindex,follow` | Combinaisons de filtres images non validees comme pages SEO autonomes. |
| `/:lang/park/:id/:slug/videos?*` | `noindex,follow` | Combinaisons de filtres videos non validees comme pages SEO autonomes. |
| `/:lang/park/:id/:slug/weather?*` | `noindex,follow` | Variante filtree ou parametree de la meteo publique. |
| `/:lang/park/:id/:slug/item/:itemId/:itemSlug/images?*` | `noindex,follow` | Combinaisons de filtres images non validees comme pages SEO autonomes. |
| `/:lang/park/:id/:slug/item/:itemId/:itemSlug/videos?*` | `noindex,follow` | Combinaisons de filtres videos non validees comme pages SEO autonomes. |
| `/:lang/park/:id/:slug/map` | `noindex,follow` | Carte interactive dediee, utile aux visiteurs mais faible valeur SEO brute. |
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
| `/api/**` | `Disallow` dans `robots.txt` | API technique consommee par le front. |
| `/robots.txt` | Non applicable | Fichier technique public. |
| `/sitemap.xml` | Non applicable | Sitemap index public, proxifie vers l'API. |
| `/sitemaps/*.xml` | Non applicable | Sections techniques du sitemap, proxifiees vers l'API. |

## Validation attendue

- Les pages publiques indexables recoivent `title`, `description`, `canonical`, `robots=index,follow` et des alternates `hreflang`.
- Les pages publiques indexables dynamiques doivent etre rendues en SSR sur cache miss pour eviter que les robots ne voient uniquement le shell `<app-root>`.
- Les pages privees recoivent `robots=noindex,nofollow`.
- La page 404 publique recoit `robots=noindex,follow`.
- `robots.txt` reference `/sitemap.xml` et exclut `/api/`, admin et compte/auth.
