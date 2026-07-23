import { isPlatformBrowser, NgComponentOutlet } from '@angular/common';
import { ChangeDetectionStrategy, Component, DestroyRef, Inject, OnInit, PLATFORM_ID, Type, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { RouterOutlet } from '@angular/router';

import { AuthService } from '@app/services/auth/auth.service';
import { SharedService } from '@app/services/shared/shared.service';
import { PublicParkNavigationTreeFacade } from '@features/public/navigation/state/public-park-navigation-tree.facade';
import { PublicParkNavigationTreeState } from '@features/public/navigation/state/public-park-navigation-tree.state';
import { PublicFooterComponent } from '@ui/layouts/public-footer/public-footer.component';
import { PublicHeaderComponent } from '@ui/layouts/public-header/public-header.component';
import { PublicMobileBottomNavComponent } from '@ui/layouts/public-mobile-bottom-nav/public-mobile-bottom-nav.component';
import { PublicParkNavigationTrailComponent } from '@ui/layouts/public-park-navigation-trail/public-park-navigation-trail.component';

@Component({
  selector: 'app-public-app-layout',
  templateUrl: './public-app-layout.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [PublicParkNavigationTreeFacade, PublicParkNavigationTreeState],
  imports: [
    NgComponentOutlet,
    PublicFooterComponent,
    PublicHeaderComponent,
    PublicMobileBottomNavComponent,
    PublicParkNavigationTrailComponent,
    RouterOutlet
  ]
})
export class PublicAppLayoutComponent implements OnInit {
  protected readonly adminToolbarComponent = signal<Type<unknown> | null>(null);

  private adminToolbarLoadPromise: Promise<Type<unknown>> | null = null;

  constructor(
    private readonly publicParkNavigationTreeFacade: PublicParkNavigationTreeFacade,
    private readonly authService: AuthService,
    private readonly sharedService: SharedService,
    private readonly destroyRef: DestroyRef,
    @Inject(PLATFORM_ID) private readonly platformId: object
  ) {
  }

  ngOnInit(): void {
    this.publicParkNavigationTreeFacade.initialize();
    this.refreshAdminToolbarVisibility();

    this.sharedService.getLoginStatusListener()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((): void => {
        this.refreshAdminToolbarVisibility();
      });
  }

  private refreshAdminToolbarVisibility(): void {
    if (!this.isAuthenticatedAdminBrowser()) {
      this.adminToolbarComponent.set(null);
      return;
    }

    this.ensureAdminToolbarLoaded();
  }

  private ensureAdminToolbarLoaded(): void {
    if (this.adminToolbarComponent()) {
      return;
    }

    if (!this.adminToolbarLoadPromise) {
      this.adminToolbarLoadPromise = this.loadAdminToolbarComponent();
    }

    void this.adminToolbarLoadPromise
      .then((componentType: Type<unknown>): void => {
        if (this.isAuthenticatedAdminBrowser()) {
          this.adminToolbarComponent.set(componentType);
        }
      })
      .catch((error: unknown): void => {
        console.error('Failed to load the admin public view toolbar', error);
      })
      .finally((): void => {
        this.adminToolbarLoadPromise = null;
      });
  }

  private loadAdminToolbarComponent(): Promise<Type<unknown>> {
    return import('@features/admin/contextual-editing/ui/admin-public-view-toolbar/admin-public-view-toolbar.component')
      .then((module) => module.AdminPublicViewToolbarComponent);
  }

  private isAuthenticatedAdminBrowser(): boolean {
    return isPlatformBrowser(this.platformId)
      && this.authService.isLoggedIn()
      && this.authService.hasRole('ADMIN');
  }
}
