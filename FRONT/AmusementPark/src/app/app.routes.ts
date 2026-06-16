import { Routes } from '@angular/router';

import { languageGuard } from '@core/guards/language.guard';
import { authGuard } from '@core/guards/auth.guard';
import { adminGuard } from '@core/guards/admin.guard';

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
            path: 'localized-content',
            loadComponent: () => import('@features/admin/localized-content/pages/admin-localized-content/admin-localized-content.component').then((m) => m.AdminLocalizedContentComponent)
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
            path: 'images',
            loadChildren: () =>
              import('@features/admin/site/pages/admin-site/admin-site.module')
                .then((m) => m.AdminSiteModule)
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
          { path: 'about', loadComponent: () => import('@features/public/about/pages/about.component').then((m) => m.AboutComponent) },
          { path: 'privacy', loadComponent: () => import('./features/public/legal/pages/privacy-policy-page.component').then((m) => m.PrivacyPolicyPageComponent) },
          { path: 'not-found', loadComponent: () => import('./features/public/not-found/pages/public-not-found-page.component').then((m) => m.PublicNotFoundPageComponent) },

          { path: 'park-operator/:id/:slug', loadComponent: () => import('./features/public/parks/pages/park-reference-detail-page.component').then((m) => m.ParkReferenceDetailPageComponent), data: { referenceKind: 'operator' } },
          { path: 'park-founder/:id/:slug', loadComponent: () => import('./features/public/parks/pages/park-reference-detail-page.component').then((m) => m.ParkReferenceDetailPageComponent), data: { referenceKind: 'founder' } },
          { path: 'park-manufacturer/:id/:slug', loadComponent: () => import('./features/public/parks/pages/park-reference-detail-page.component').then((m) => m.ParkReferenceDetailPageComponent), data: { referenceKind: 'manufacturer' } },
          { path: 'park/:id/:slug/images', loadComponent: () => import('./features/public/parks/pages/park-images-page.component').then((m) => m.ParkImagesPageComponent) },
          { path: 'park/:id/:slug/map', loadComponent: () => import('./features/public/parks/pages/park-map-page.component').then((m) => m.ParkMapPageComponent) },
          { path: 'park/:id/:slug/zones', loadComponent: () => import('./features/public/parks/pages/park-zones-page.component').then((m) => m.ParkZonesPageComponent) },
          { path: 'park/:id/:slug/zone/:zoneId/:zoneSlug', loadComponent: () => import('./features/public/parks/pages/park-zone-page.component').then((m) => m.ParkZonePageComponent) },
          { path: 'park/:id/:slug/items', loadComponent: () => import('./features/public/park-items/pages/park-items-page.component').then((m) => m.ParkItemsPageComponent) },
          { path: 'park/:id/:slug/item/:itemId/:itemSlug/images', loadComponent: () => import('./features/public/park-items/pages/park-item-images-page.component').then((m) => m.ParkItemImagesPageComponent) },
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
