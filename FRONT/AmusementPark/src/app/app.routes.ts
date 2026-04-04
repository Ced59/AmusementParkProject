import { Routes } from '@angular/router';

import { languageGuard } from './guards/language.guard';
import { authGuard } from './guards/auth.guard';
import { adminGuard } from './guards/admin.guard';

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
      { path: 'home', loadComponent: () => import('./components/home/home.component').then((m) => m.HomeComponent) },
      { path: 'parks', loadComponent: () => import('./components/park-list/park-list.component').then((m) => m.ParkListComponent) },
      { path: 'about', loadComponent: () => import('./components/about/about.component').then((m) => m.AboutComponent) },

      { path: 'profile', loadChildren: () => import('./components/login-register/profile/profile.module').then((m) => m.ProfileModule) },

      { path: 'confirm-account', loadComponent: () => import('./components/login-register/confirm-account-page/confirm-account-page.component').then((m) => m.ConfirmAccountPageComponent) },
      { path: 'forgot-password', loadComponent: () => import('./components/login-register/forgot-password-page/forgot-password-page.component').then((m) => m.ForgotPasswordPageComponent) },
      { path: 'reset-password', loadComponent: () => import('./components/login-register/reset-password-page/reset-password-page.component').then((m) => m.ResetPasswordPageComponent) },

      { path: 'park/:id/:slug/items', loadComponent: () => import('./components/public/park-items-page/park-items-page.component').then((m) => m.ParkItemsPageComponent) },
      { path: 'park/:id/:slug/item/:itemId/:itemSlug', loadComponent: () => import('./components/public/park-item-detail/park-item-detail.component').then((m) => m.ParkItemDetailComponent) },
      { path: 'park/:id/:slug', loadComponent: () => import('./components/park-detail/park-detail.component').then((m) => m.ParkDetailComponent) },

      {
        path: 'admin',
        loadComponent: () => import('./components/admin/admin-dashboard/admin-dashboard.component').then((m) => m.AdminDashboardComponent),
        canActivate: [authGuard, adminGuard],
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
            path: 'site',
            loadChildren: () =>
              import('./components/admin/site/admin-site/admin-site.module')
                .then((m) => m.AdminSiteModule)
          },
          { path: '', redirectTo: 'users', pathMatch: 'full' }
        ]
      },

      { path: '', redirectTo: 'home', pathMatch: 'full' }
    ]
  },
  { path: '**', redirectTo: 'en/home', pathMatch: 'full' }
];
