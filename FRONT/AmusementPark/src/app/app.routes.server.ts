import { RenderMode, ServerRoute } from '@angular/ssr';

const supportedLangs: string[] = ['en', 'fr'];

function getLangParams(): Promise<Record<string, string>[]> {
  return Promise.resolve(supportedLangs.map((lang) => ({ lang })));
}

export const serverRoutes: ServerRoute[] = [
  // Racine / redirections
  { path: '', renderMode: RenderMode.Server },
  { path: ':lang', renderMode: RenderMode.Server },

  // Pages publiques stables => prerender
  {
    path: ':lang/home',
    renderMode: RenderMode.Prerender,
    getPrerenderParams: getLangParams
  },
  {
    path: ':lang/parks',
    renderMode: RenderMode.Prerender,
    getPrerenderParams: getLangParams
  },
  {
    path: ':lang/about',
    renderMode: RenderMode.Prerender,
    getPrerenderParams: getLangParams
  },

  // Pages utilisateur / admin => SSR
  { path: ':lang/profile', renderMode: RenderMode.Server },
  { path: ':lang/confirm-account', renderMode: RenderMode.Server },
  { path: ':lang/forgot-password', renderMode: RenderMode.Server },
  { path: ':lang/reset-password', renderMode: RenderMode.Server },

  { path: ':lang/admin/users', renderMode: RenderMode.Server },
  { path: ':lang/admin/parks', renderMode: RenderMode.Server },
  { path: ':lang/admin/items', renderMode: RenderMode.Server },
  { path: ':lang/admin/operators', renderMode: RenderMode.Server },
  { path: ':lang/admin/founders', renderMode: RenderMode.Server },
  { path: ':lang/admin/founders/new', renderMode: RenderMode.Server },
  { path: ':lang/admin/founders/edit/:id', renderMode: RenderMode.Server },
  { path: ':lang/admin/manufacturers', renderMode: RenderMode.Server },
  { path: ':lang/admin/data', renderMode: RenderMode.Server },
  { path: ':lang/admin/images', renderMode: RenderMode.Server },
  { path: ':lang/admin/site', renderMode: RenderMode.Server },

  // Pages de parc dynamiques => SSR
  { path: ':lang/park-operator/:id/:slug', renderMode: RenderMode.Server },
  { path: ':lang/park-founder/:id/:slug', renderMode: RenderMode.Server },
  { path: ':lang/park-manufacturer/:id/:slug', renderMode: RenderMode.Server },
  { path: ':lang/park/:id/:slug', renderMode: RenderMode.Server },
  { path: ':lang/park/:id/:slug/items', renderMode: RenderMode.Server },
  { path: ':lang/park/:id/:slug/item/:itemId/:itemSlug', renderMode: RenderMode.Server },

  // Fallback
  { path: '**', renderMode: RenderMode.Server }
];
