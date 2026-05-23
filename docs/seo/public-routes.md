# M19.1 — Inventaire des routes publiques indexables

## Règle générale

Une route est indexable uniquement si elle expose du contenu public utile à un moteur de recherche. Les routes d'administration, de compte, d'authentification, d'erreur et les routes techniques restent en `noindex` ou sont exclues de `robots.txt` quand c'est pertinent.

## Routes publiques indexables

| Route | Statut | Justification |
|---|---|---|
| `/:lang/home` | `index,follow` | Page d'accueil publique. |
| `/:lang/parks` | `index,follow` | Liste publique des parcs visibles. |
| `/:lang/about` | `index,follow` | Page publique de présentation du projet. |
| `/:lang/privacy` | `index,follow` | Page légale publique utile et accessible. |
| `/:lang/park/:id/:slug` | `index,follow` | Détail d'un parc public visible. |
| `/:lang/park/:id/:slug/items` | `index,follow` | Exploration publique des éléments d'un parc visible. |
| `/:lang/park/:id/:slug/item/:itemId/:itemSlug` | `index,follow` | Détail public d'un élément visible. |
| `/:lang/park-operator/:id/:slug` | `index,follow` | Référence publique d'exploitant. |
| `/:lang/park-founder/:id/:slug` | `index,follow` | Référence publique de fondateur. |
| `/:lang/park-manufacturer/:id/:slug` | `index,follow` | Référence publique de constructeur. |

Langues servies : `en`, `fr`, `es`, `de`, `it`, `pl`, `nl`, `pt`.

## Routes publiques non indexables

| Route | Statut | Justification |
|---|---|---|
| `/:lang/not-found` | `noindex,follow` | Page 404 publique. |
| route wildcard publique | `noindex,follow` | Affiche la vraie page 404 publique. |

## Routes privées / techniques non indexables

| Route | Statut | Justification |
|---|---|---|
| `/:lang/admin/**` | `noindex,nofollow` | Back-office privé. |
| `/:lang/profile` | `noindex,nofollow` | Compte utilisateur privé. |
| `/:lang/confirm-account` | `noindex,nofollow` | Parcours auth/email. |
| `/:lang/forgot-password` | `noindex,nofollow` | Parcours auth/email. |
| `/:lang/reset-password` | `noindex,nofollow` | Parcours auth/email. |
| `/api/**` | `Disallow` dans `robots.txt` | API technique consommée par le front. |
| `/robots.txt` | Non applicable | Fichier technique public. |
| `/sitemap.xml` | Non applicable | Fichier technique public. |

## Validation attendue

- Les pages publiques indexables reçoivent `title`, `description`, `canonical`, `robots=index,follow` et des alternates `hreflang`.
- Les pages privées reçoivent `robots=noindex,nofollow`.
- La page 404 publique reçoit `robots=noindex,follow`.
- `robots.txt` référence `/sitemap.xml` et exclut `/api/`, admin et compte/auth.
