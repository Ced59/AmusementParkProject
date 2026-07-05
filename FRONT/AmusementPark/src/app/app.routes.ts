import { Routes } from '@angular/router';

import { languageGuard } from '@core/guards/language.guard';
import { authGuard } from '@core/guards/auth.guard';
import { adminGuard } from '@core/guards/admin.guard';
import { HISTORY_ARTICLE_ROUTE_DATA_KEY, historyArticleResolver } from '@features/public/history/state/history-article.resolver';
import { HISTORY_TIMELINE_ROUTE_DATA_KEY, historyTimelineResolver } from '@features/public/history/state/history-timeline.resolver';

export const routes: Routes = [
  {
    path: '',
    redirectTo: 'en/home',
    pathMatch: 'full'
  },
  {
    path: ':lang',
    canActivate: [languageGuard],
    children: [
      {
        path: 'admin',
        loadComponent: () => import('@ui/layouts/admin-app-layout/admin-app-layout.component').then((m) => m.AdminAppLayoutComponent),
        canActivate: [authGuard, adminGuard],
        children: [
          {
            path: '',
            pathMatch: 'full',
            loadComponent: () => import('@features/admin/dashboard/pages/admin-dashboard/admin-dashboard.component').then((m) => m.AdminDashboardComponent)
          },
          {
            path: 'users',
            loadChildren: () =>
              import('@features/admin/users/pages/admin-users/admin-users.module')
                .then((m) => m.AdminUsersModule)
          },
          {
            path: 'parks',
            loadChildren: () =>
              import('@features/admin/parks/pages/admin-parks/admin-parks.module')
                .then((m) => m.AdminParksModule)
          },
          {
            path: 'items',
            loadChildren: () =>
              import('@features/admin/park-items/pages/admin-park-items-index/admin-park-items-index.module')
                .then((m) => m.AdminParkItemsIndexModule)
          },
          {
            path: 'field-mode/item/:itemId',
            loadComponent: () => import('@features/admin/field-mode/pages/admin-field-mode/admin-field-mode.component').then((m) => m.AdminFieldModeComponent)
          },
          {
            path: 'field-mode',
            loadComponent: () => import('@features/admin/field-mode/pages/admin-field-mode/admin-field-mode.component').then((m) => m.AdminFieldModeComponent)
          },
          {
            path: 'operators',
            loadChildren: () =>
              import('@features/admin/operators/pages/admin-operators/admin-operators.module')
                .then((m) => m.AdminOperatorsModule)
          },
          {
            path: 'founders',
            loadChildren: () =>
              import('@features/admin/founders/pages/admin-founders/admin-founders.module')
                .then((m) => m.AdminFoundersModule)
          },
          {
            path: 'manufacturers',
            loadChildren: () =>
              import('@features/admin/manufacturers/pages/admin-manufacturers/admin-manufacturers.module')
                .then((m) => m.AdminManufacturersModule)
          },
          {
            path: 'technical-pages',
            loadChildren: () =>
              import('@features/admin/technical-pages/pages/admin-technical-pages/admin-technical-pages.module')
                .then((m) => m.AdminTechnicalPagesModule)
          },
          {
            path: 'data',
            loadChildren: () =>
              import('@features/admin/data/pages/admin-data/admin-data.module')
                .then((m) => m.AdminDataModule)
          },
          {
            path: 'park-graph-upserts',
            loadComponent: () => import('@features/admin/park-graph-upserts/pages/admin-park-graph-upserts/admin-park-graph-upserts.component').then((m) => m.AdminParkGraphUpsertsComponent)
          },
          {
            path: 'bulk-park-graph-upserts',
            loadComponent: () => import('@features/admin/park-graph-upserts/pages/admin-bulk-park-graph-upserts/admin-bulk-park-graph-upserts.component').then((m) => m.AdminBulkParkGraphUpsertsComponent)
          },
          {
            path: 'history',
            loadComponent: () => import('@features/admin/history/pages/admin-history/admin-history.component').then((m) => m.AdminHistoryComponent)
          },
          {
            path: 'audit-logs',
            loadComponent: () => import('@features/admin/audit-logs/pages/admin-audit-logs/admin-audit-logs.component').then((m) => m.AdminAuditLogsComponent)
          },
          {
            path: 'seo-sitemaps',
            loadComponent: () => import('@features/admin/seo-sitemaps/pages/admin-seo-sitemaps/admin-seo-sitemaps.component').then((m) => m.AdminSeoSitemapsComponent)
          },
          {
            path: 'park-weather',
            loadComponent: () => import('@features/admin/park-weather/pages/admin-park-weather/admin-park-weather.component').then((m) => m.AdminParkWeatherComponent)
          },
          {
            path: 'contact-grievances',
            loadComponent: () => import('@features/admin/contact/pages/admin-contact-grievances/admin-contact-grievances.component').then((m) => m.AdminContactGrievancesComponent)
          },
          {
            path: 'social-share',
            loadComponent: () => import('@features/admin/social-share/pages/admin-social-share-stats/admin-social-share-stats.component').then((m) => m.AdminSocialShareStatsComponent)
          },
          {
            path: 'technical-stats',
            loadComponent: () => import('@features/admin/technical-stats/pages/admin-technical-stats/admin-technical-stats.component').then((m) => m.AdminTechnicalStatsComponent)
          },
          {
            path: 'images',
            loadChildren: () =>
              import('@features/admin/site/pages/admin-site/admin-site.module')
                .then((m) => m.AdminSiteModule)
          },
          {
            path: 'videos',
            loadComponent: () => import('@features/admin/videos/pages/admin-videos/admin-videos.component').then((m) => m.AdminVideosComponent)
          },
          {
            path: 'site',
            redirectTo: 'images',
            pathMatch: 'full'
          }
        ]
      },
      {
        path: '',
        loadComponent: () => import('@ui/layouts/account-layout/account-layout.component').then((m) => m.AccountLayoutComponent),
        children: [
          { path: 'profile', loadChildren: () => import('@features/profile/profile.module').then((m) => m.ProfileModule) },
          { path: 'confirm-account', loadComponent: () => import('@features/auth/pages/confirm-account-page/confirm-account-page.component').then((m) => m.ConfirmAccountPageComponent) },
          { path: 'forgot-password', loadComponent: () => import('@features/auth/pages/forgot-password-page/forgot-password-page.component').then((m) => m.ForgotPasswordPageComponent) },
          { path: 'reset-password', loadComponent: () => import('@features/auth/pages/reset-password-page/reset-password-page.component').then((m) => m.ResetPasswordPageComponent) }
        ]
      },
      {
        path: '',
        loadComponent: () => import('@ui/layouts/public-app-layout/public-app-layout.component').then((m) => m.PublicAppLayoutComponent),
        children: [
          { path: 'home', loadComponent: () => import('@features/public/home/pages/home.component').then((m) => m.HomeComponent) },
          { path: 'parks', loadComponent: () => import('./features/public/parks/pages/park-list-page.component').then((m) => m.ParkListPageComponent) },
          { path: 'sitemap', loadComponent: () => import('./features/public/sitemap/pages/public-sitemap-page.component').then((m) => m.PublicSitemapPageComponent) },
          { path: 'technical', loadComponent: () => import('./features/public/technical-pages/pages/technical-pages-page.component').then((m) => m.TechnicalPagesPageComponent) },
          { path: 'technical/:slug', loadComponent: () => import('./features/public/technical-pages/pages/technical-page-detail-page.component').then((m) => m.TechnicalPageDetailPageComponent) },
          { path: 'manufacturers', loadComponent: () => import('@features/public/manufacturers/pages/manufacturers-page.component').then((m) => m.ManufacturersPageComponent) },
          { path: 'rankings', loadComponent: () => import('@features/public/ratings/pages/rankings-page.component').then((m) => m.RankingsPageComponent) },
          { path: 'about', loadComponent: () => import('@features/public/about/pages/about.component').then((m) => m.AboutComponent) },
          { path: 'contact', loadComponent: () => import('@features/public/contact/pages/contact-page.component').then((m) => m.ContactPageComponent) },
          { path: 'versions', loadComponent: () => import('@features/public/version-history/pages/version-history-page.component').then((m) => m.VersionHistoryPageComponent) },
          { path: 'privacy', loadComponent: () => import('./features/public/legal/pages/privacy-policy-page.component').then((m) => m.PrivacyPolicyPageComponent) },
          { path: 'not-found', loadComponent: () => import('./features/public/not-found/pages/public-not-found-page.component').then((m) => m.PublicNotFoundPageComponent) },

          { path: 'park-operator/:id/:slug', loadComponent: () => import('./features/public/parks/pages/park-reference-detail-page.component').then((m) => m.ParkReferenceDetailPageComponent), data: { referenceKind: 'operator' } },
          { path: 'park-founder/:id/:slug', loadComponent: () => import('./features/public/parks/pages/park-reference-detail-page.component').then((m) => m.ParkReferenceDetailPageComponent), data: { referenceKind: 'founder' } },
          { path: 'park-manufacturer/:id/:slug', loadComponent: () => import('./features/public/parks/pages/park-reference-detail-page.component').then((m) => m.ParkReferenceDetailPageComponent), data: { referenceKind: 'manufacturer' } },
          { path: 'park/:id/:slug/images', loadComponent: () => import('./features/public/parks/pages/park-images-page.component').then((m) => m.ParkImagesPageComponent) },
          {
            path: 'park/:id/:slug/history/:eventId/:eventSlug',
            resolve: { [HISTORY_ARTICLE_ROUTE_DATA_KEY]: historyArticleResolver },
            loadComponent: () => import('./features/public/history/pages/history-article-page.component').then((m) => m.HistoryArticlePageComponent)
          },
          {
            path: 'park/:id/:slug/history',
            resolve: { [HISTORY_TIMELINE_ROUTE_DATA_KEY]: historyTimelineResolver },
            loadComponent: () => import('./features/public/history/pages/history-timeline-page.component').then((m) => m.HistoryTimelinePageComponent)
          },
          { path: 'park/:id/:slug/video/s/:videoId/:videoSlug', redirectTo: 'park/:id/:slug/videos/:videoId/:videoSlug', pathMatch: 'full' },
          { path: 'park/:id/:slug/video/:videoId/:videoSlug', redirectTo: 'park/:id/:slug/videos/:videoId/:videoSlug', pathMatch: 'full' },
          { path: 'park/:id/:slug/videos/:videoId/:videoSlug', loadComponent: () => import('./features/public/parks/pages/park-video-page.component').then((m) => m.ParkVideoPageComponent) },
          { path: 'park/:id/:slug/videos', loadComponent: () => import('./features/public/parks/pages/park-videos-page.component').then((m) => m.ParkVideosPageComponent) },
          { path: 'park/:id/:slug/map', loadComponent: () => import('./features/public/parks/pages/park-map-page.component').then((m) => m.ParkMapPageComponent) },
          { path: 'park/:id/:slug/zones', loadComponent: () => import('./features/public/parks/pages/park-zones-page.component').then((m) => m.ParkZonesPageComponent) },
          { path: 'park/:id/:slug/zone/:zoneId/:zoneSlug', loadComponent: () => import('./features/public/parks/pages/park-zone-page.component').then((m) => m.ParkZonePageComponent) },
          { path: 'park/:id/:slug/weather', loadComponent: () => import('./features/public/parks/pages/park-weather-page.component').then((m) => m.ParkWeatherPageComponent) },
          { path: 'park/:id/:slug/opening-hours', loadComponent: () => import('./features/public/parks/pages/park-opening-hours-page.component').then((m) => m.ParkOpeningHoursPageComponent) },
          { path: 'park/:id/:slug/items', loadComponent: () => import('./features/public/park-items/pages/park-items-page.component').then((m) => m.ParkItemsPageComponent) },
          { path: 'park/:id/:slug/item/:itemId/:itemSlug/images', loadComponent: () => import('./features/public/park-items/pages/park-item-images-page.component').then((m) => m.ParkItemImagesPageComponent) },
          {
            path: 'park/:id/:slug/item/:itemId/:itemSlug/history/:eventId/:eventSlug',
            resolve: { [HISTORY_ARTICLE_ROUTE_DATA_KEY]: historyArticleResolver },
            loadComponent: () => import('./features/public/history/pages/history-article-page.component').then((m) => m.HistoryArticlePageComponent)
          },
          {
            path: 'park/:id/:slug/item/:itemId/:itemSlug/history',
            resolve: { [HISTORY_TIMELINE_ROUTE_DATA_KEY]: historyTimelineResolver },
            loadComponent: () => import('./features/public/history/pages/history-timeline-page.component').then((m) => m.HistoryTimelinePageComponent)
          },
          { path: 'park/:id/:slug/item/:itemId/:itemSlug/video/s/:videoId/:videoSlug', redirectTo: 'park/:id/:slug/item/:itemId/:itemSlug/videos/:videoId/:videoSlug', pathMatch: 'full' },
          { path: 'park/:id/:slug/item/:itemId/:itemSlug/video/:videoId/:videoSlug', redirectTo: 'park/:id/:slug/item/:itemId/:itemSlug/videos/:videoId/:videoSlug', pathMatch: 'full' },
          { path: 'park/:id/:slug/item/:itemId/:itemSlug/videos/:videoId/:videoSlug', loadComponent: () => import('./features/public/park-items/pages/park-item-video-page.component').then((m) => m.ParkItemVideoPageComponent) },
          { path: 'park/:id/:slug/item/:itemId/:itemSlug/videos', loadComponent: () => import('./features/public/park-items/pages/park-item-videos-page.component').then((m) => m.ParkItemVideosPageComponent) },
          { path: 'park/:id/:slug/item/:itemId/:itemSlug', loadComponent: () => import('./features/public/park-items/pages/park-item-detail-page.component').then((m) => m.ParkItemDetailPageComponent) },
          { path: 'park/:id/:slug', loadComponent: () => import('./features/public/parks/pages/park-detail-page.component').then((m) => m.ParkDetailPageComponent) },

          { path: '', redirectTo: 'home', pathMatch: 'full' },
          { path: '**', loadComponent: () => import('./features/public/not-found/pages/public-not-found-page.component').then((m) => m.PublicNotFoundPageComponent) }
        ]
      }
    ]
  },
  { path: '**', redirectTo: 'en/not-found', pathMatch: 'full' }
];
