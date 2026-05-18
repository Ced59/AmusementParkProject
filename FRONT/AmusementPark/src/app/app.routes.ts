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
            loadComponent: () => import('./components/admin/admin-dashboard/admin-dashboard.component').then((m) => m.AdminDashboardComponent),
            children: [
              {
                path: 'users',
                loadChildren: () =>
                  import('./components/admin/users/admin-users/admin-users.module')
                    .then((m) => m.AdminUsersModule)
              },
              {
                path: 'parks',
                loadChildren: () =>
                  import('./components/admin/parks/admin-parks/admin-parks.module')
                    .then((m) => m.AdminParksModule)
              },
              {
                path: 'items',
                loadChildren: () =>
                  import('./components/admin/park-items/admin-park-items-index/admin-park-items-index.module')
                    .then((m) => m.AdminParkItemsIndexModule)
              },
              {
                path: 'operators',
                loadChildren: () =>
                  import('./components/admin/operators/admin-operators/admin-operators.module')
                    .then((m) => m.AdminOperatorsModule)
              },
              {
                path: 'manufacturers',
                loadChildren: () =>
                  import('./components/admin/manufacturers/admin-manufacturers/admin-manufacturers.module')
                    .then((m) => m.AdminManufacturersModule)
              },
              {
                path: 'data',
                loadChildren: () =>
                  import('./components/admin/data/admin-data/admin-data.module')
                    .then((m) => m.AdminDataModule)
              },
              {
                path: 'images',
                loadChildren: () =>
                  import('./components/admin/site/admin-site/admin-site.module')
                    .then((m) => m.AdminSiteModule)
              },
              {
                path: 'site',
                redirectTo: 'images',
                pathMatch: 'full'
              },
              { path: '', redirectTo: 'users', pathMatch: 'full' }
            ]
          }
        ]
      },
      {
        path: '',
        loadComponent: () => import('@ui/layouts/account-layout/account-layout.component').then((m) => m.AccountLayoutComponent),
        children: [
          { path: 'profile', loadChildren: () => import('./components/login-register/profile/profile.module').then((m) => m.ProfileModule) },
          { path: 'confirm-account', loadComponent: () => import('./components/login-register/confirm-account-page/confirm-account-page.component').then((m) => m.ConfirmAccountPageComponent) },
          { path: 'forgot-password', loadComponent: () => import('./components/login-register/forgot-password-page/forgot-password-page.component').then((m) => m.ForgotPasswordPageComponent) },
          { path: 'reset-password', loadComponent: () => import('./components/login-register/reset-password-page/reset-password-page.component').then((m) => m.ResetPasswordPageComponent) }
        ]
      },
      {
        path: '',
        loadComponent: () => import('@ui/layouts/public-app-layout/public-app-layout.component').then((m) => m.PublicAppLayoutComponent),
        children: [
          { path: 'home', loadComponent: () => import('./components/home/home.component').then((m) => m.HomeComponent) },
          { path: 'parks', loadComponent: () => import('./features/public/parks/pages/park-list-page.component').then((m) => m.ParkListPageComponent) },
          { path: 'about', loadComponent: () => import('./components/about/about.component').then((m) => m.AboutComponent) },

          { path: 'park-operator/:id/:slug', loadComponent: () => import('./features/public/parks/pages/park-reference-detail-page.component').then((m) => m.ParkReferenceDetailPageComponent), data: { referenceKind: 'operator' } },
          { path: 'park-founder/:id/:slug', loadComponent: () => import('./features/public/parks/pages/park-reference-detail-page.component').then((m) => m.ParkReferenceDetailPageComponent), data: { referenceKind: 'founder' } },
          { path: 'park/:id/:slug/items', loadComponent: () => import('./features/public/park-items/pages/park-items-page.component').then((m) => m.ParkItemsPageComponent) },
          { path: 'park/:id/:slug/item/:itemId/:itemSlug', loadComponent: () => import('./features/public/park-items/pages/park-item-detail-page.component').then((m) => m.ParkItemDetailPageComponent) },
          { path: 'park/:id/:slug', loadComponent: () => import('./features/public/parks/pages/park-detail-page.component').then((m) => m.ParkDetailPageComponent) },

          { path: '', redirectTo: 'home', pathMatch: 'full' }
        ]
      }
    ]
  },
  { path: '**', redirectTo: 'en/home', pathMatch: 'full' }
];
