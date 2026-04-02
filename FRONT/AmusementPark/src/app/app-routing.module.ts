import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { HomeComponent } from './components/home/home.component';
import { AboutComponent } from './components/about/about.component';
import { languageGuard } from './guards/language.guard';
import { ParkDetailComponent } from './components/park-detail/park-detail.component';
import { ParkListComponent } from './components/park-list/park-list.component';
import { ParkExplorerComponent } from './components/park-explorer/park-explorer.component';
import { AdminDashboardComponent } from './components/admin/admin-dashboard/admin-dashboard.component';
import { authGuard } from './guards/auth.guard';
import { adminGuard } from './guards/admin.guard';

const routes: Routes = [
  {
    path: ':lang',
    canActivate: [languageGuard],
    children: [
      { path: 'home', component: HomeComponent },
      { path: 'parks', component: ParkListComponent },
      { path: 'about', component: AboutComponent },
      { path: 'profile', loadChildren: () => import('./components/login-register/profile/profile.module').then(m => m.ProfileModule) },
      { path: 'park/:id/:slug/explore', component: ParkExplorerComponent },
      { path: 'park/:id/:slug', component: ParkDetailComponent },
      {
        path: 'admin',
        component: AdminDashboardComponent,
        canActivate: [authGuard, adminGuard],
        children: [
          {
            path: 'users',
            loadChildren: () =>
              import('./components/admin/users/admin-users/admin-users.module')
                .then(m => m.AdminUsersModule)
          },
          {
            path: 'parks',
            loadChildren: () =>
              import('./components/admin/parks/admin-parks/admin-parks.module')
                .then(m => m.AdminParksModule)
          },
          {
            path: 'items',
            loadChildren: () =>
              import('./components/admin/park-items/admin-park-items-index/admin-park-items-index.module')
                .then(m => m.AdminParkItemsIndexModule)
          },
          {
            path: 'operators',
            loadChildren: () =>
              import('./components/admin/operators/admin-operators/admin-operators.module')
                .then(m => m.AdminOperatorsModule)
          },
          {
            path: 'manufacturers',
            loadChildren: () =>
              import('./components/admin/manufacturers/admin-manufacturers/admin-manufacturers.module')
                .then(m => m.AdminManufacturersModule)
          },
          {
            path: 'site',
            loadChildren: () =>
              import('./components/admin/site/admin-site/admin-site.module')
                .then(m => m.AdminSiteModule)
          },
          { path: '', redirectTo: 'users', pathMatch: 'full' }
        ]
      },
      { path: '', redirectTo: 'home', pathMatch: 'full' }
    ]
  },
  { path: '**', redirectTo: 'en/home', pathMatch: 'full' }
];

@NgModule({
  imports: [RouterModule.forRoot(routes)],
  exports: [RouterModule]
})
export class AppRoutingModule {}
