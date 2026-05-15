import { ChangeDetectionStrategy, Component, DestroyRef, OnInit, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { NavigationEnd, Router, RouterLink, RouterLinkActive } from '@angular/router';
import { filter } from 'rxjs/operators';

import { TranslateModule } from '@ngx-translate/core';

import { AuthService } from '@app/services/auth/auth.service';
import { SharedService } from '@app/services/shared/shared.service';
import { TranslationService } from '@app/services/translation.service';

@Component({
  selector: 'app-public-mobile-bottom-nav',
  templateUrl: './public-mobile-bottom-nav.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, RouterLinkActive, TranslateModule]
})
export class PublicMobileBottomNavComponent implements OnInit {
  protected readonly currentLang = signal<string>('en');
  protected readonly isLoggedIn = signal<boolean>(false);
  protected readonly isAdmin = signal<boolean>(false);

  constructor(
    private readonly authService: AuthService,
    private readonly sharedService: SharedService,
    private readonly translationService: TranslationService,
    private readonly router: Router,
    private readonly destroyRef: DestroyRef
  ) {
  }

  ngOnInit(): void {
    this.currentLang.set(this.getLanguageFromUrl());
    this.checkAuthStatus();

    this.translationService.languageChanged
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((lang: string): void => {
        this.currentLang.set(lang);
      });

    this.router.events.pipe(
      filter((event: unknown): event is NavigationEnd => event instanceof NavigationEnd),
      takeUntilDestroyed(this.destroyRef)
    ).subscribe((): void => {
      this.currentLang.set(this.getLanguageFromUrl());
    });

    this.sharedService.getLoginStatusListener()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((): void => {
        this.checkAuthStatus();
      });
  }

  private checkAuthStatus(): void {
    const isLoggedIn: boolean = this.authService.isLoggedIn();

    this.isLoggedIn.set(isLoggedIn);
    this.isAdmin.set(isLoggedIn && this.authService.hasRole('ADMIN'));
  }

  private getLanguageFromUrl(): string {
    return this.router.url.split('/')[1] || this.translationService.getCurrentLang() || 'en';
  }
}
