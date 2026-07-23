import { RenderMode, ServerRoute } from '@angular/ssr';

/**
 * SSR runtime routes.
 *
 * M20 intentionally disables prerendering: dynamic public pages are rendered by
 * the Node SSR container at request time, while private/admin pages keep a CSR
 * shell to avoid server-rendering user-specific or authenticated content.
 */
export const serverRoutes: ServerRoute[] = [
  // Public localized entry points.
  { path: '', renderMode: RenderMode.Server },
  { path: ':lang', renderMode: RenderMode.Server },
  { path: ':lang/home', renderMode: RenderMode.Server },
  { path: ':lang/parks', renderMode: RenderMode.Server },
  { path: ':lang/sitemap', renderMode: RenderMode.Server },
  { path: ':lang/technical', renderMode: RenderMode.Server },
  { path: ':lang/technical/:slug', renderMode: RenderMode.Server },
  { path: ':lang/manufacturers', renderMode: RenderMode.Server },
  { path: ':lang/rankings', renderMode: RenderMode.Server },
  { path: ':lang/about', renderMode: RenderMode.Server },
  { path: ':lang/contact', renderMode: RenderMode.Server },
  { path: ':lang/versions', renderMode: RenderMode.Server },
  { path: ':lang/privacy', renderMode: RenderMode.Server },
  { path: ':lang/not-found', renderMode: RenderMode.Server },

  // Public SEO routes.
  { path: ':lang/park-operator/:id/:slug', renderMode: RenderMode.Server },
  { path: ':lang/park-founder/:id/:slug', renderMode: RenderMode.Server },
  { path: ':lang/park-manufacturer/:id/:slug', renderMode: RenderMode.Server },
  { path: ':lang/attraction/:id/:slug', renderMode: RenderMode.Server },
  { path: ':lang/park/:id/:slug/images', renderMode: RenderMode.Server },
  { path: ':lang/park/:id/:slug/history/page/:page', renderMode: RenderMode.Server },
  { path: ':lang/park/:id/:slug/history/:eventId/:eventSlug', renderMode: RenderMode.Server },
  { path: ':lang/park/:id/:slug/history', renderMode: RenderMode.Server },
  { path: ':lang/park/:id/:slug/video/s/:videoId/:videoSlug', renderMode: RenderMode.Server },
  { path: ':lang/park/:id/:slug/video/:videoId/:videoSlug', renderMode: RenderMode.Server },
  { path: ':lang/park/:id/:slug/videos/:videoId/:videoSlug', renderMode: RenderMode.Server },
  { path: ':lang/park/:id/:slug/videos', renderMode: RenderMode.Server },
  { path: ':lang/park/:id/:slug/map', renderMode: RenderMode.Server },
  { path: ':lang/park/:id/:slug/zones', renderMode: RenderMode.Server },
  { path: ':lang/park/:id/:slug/zone/:zoneId/:zoneSlug', renderMode: RenderMode.Server },
  { path: ':lang/park/:id/:slug/weather', renderMode: RenderMode.Server },
  { path: ':lang/park/:id/:slug/opening-hours', renderMode: RenderMode.Server },
  { path: ':lang/park/:id/:slug/items', renderMode: RenderMode.Server },
  { path: ':lang/park/:id/:slug/item/:itemId/:itemSlug/images', renderMode: RenderMode.Server },
  { path: ':lang/park/:id/:slug/item/:itemId/:itemSlug/history/page/:page', renderMode: RenderMode.Server },
  { path: ':lang/park/:id/:slug/item/:itemId/:itemSlug/history/:eventId/:eventSlug', renderMode: RenderMode.Server },
  { path: ':lang/park/:id/:slug/item/:itemId/:itemSlug/history', renderMode: RenderMode.Server },
  { path: ':lang/park/:id/:slug/item/:itemId/:itemSlug/video/s/:videoId/:videoSlug', renderMode: RenderMode.Server },
  { path: ':lang/park/:id/:slug/item/:itemId/:itemSlug/video/:videoId/:videoSlug', renderMode: RenderMode.Server },
  { path: ':lang/park/:id/:slug/item/:itemId/:itemSlug/videos/:videoId/:videoSlug', renderMode: RenderMode.Server },
  { path: ':lang/park/:id/:slug/item/:itemId/:itemSlug/videos', renderMode: RenderMode.Server },
  { path: ':lang/park/:id/:slug/item/:itemId/:itemSlug', renderMode: RenderMode.Server },
  { path: ':lang/park/:id/:slug', renderMode: RenderMode.Server },

  // Authenticated or user-specific routes: CSR shell + noindex handled by SEO service.
  { path: ':lang/profile', renderMode: RenderMode.Client },
  { path: ':lang/confirm-account', renderMode: RenderMode.Client },
  { path: ':lang/forgot-password', renderMode: RenderMode.Client },
  { path: ':lang/reset-password', renderMode: RenderMode.Client },
  { path: ':lang/admin', renderMode: RenderMode.Client },
  { path: ':lang/admin/**', renderMode: RenderMode.Client },

  // Unknown public URLs render the public 404 route and are marked 404 by server.ts.
  { path: '**', renderMode: RenderMode.Server }
];
