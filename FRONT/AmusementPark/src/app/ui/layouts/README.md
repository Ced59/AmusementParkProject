# ui/layouts

Layouts applicatifs cibles de la refonte MVP.

## Layouts actifs depuis M02

- `public-app-layout` : navigation publique desktop, bottom nav mobile, footer et zone de contenu public.
- `admin-app-layout` : shell admin dense, séparé du layout public et sans bottom nav publique.
- `account-layout` : shell dédié aux pages auth, reset password et profile.

Ces layouts sont branchés par `app.routes.ts`. Le composant racine ne porte plus le header/sidebar global : il ne contient plus que le `router-outlet` applicatif et le toast global.
