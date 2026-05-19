import { Injectable } from '@angular/core';
import { Router } from '@angular/router';
import { Observable, catchError, from, map, of, switchMap, tap } from 'rxjs';

import { AuthApiService } from '@data-access/auth/auth-api.service';
import { UserDto } from '@app/models/users/user_dto';
import { AuthService } from '@app/services/auth/auth.service';
import { TranslationService } from '@app/services/translation.service';
import { CurrentUserService } from '@app/services/users/current-user.service';
import { isSupportedLanguage, resolveSupportedLanguage } from '@shared/utils/routing/localized-route.helpers';

export interface AuthenticatedUserLanguageSyncResult {
  user: UserDto | null;
  language: string | null;
  navigated: boolean;
}

@Injectable({
  providedIn: 'root'
})
export class AuthenticatedUserLanguageService {
  constructor(
    private readonly authApiService: AuthApiService,
    private readonly authService: AuthService,
    private readonly currentUserService: CurrentUserService,
    private readonly router: Router,
    private readonly translationService: TranslationService
  ) {
  }

  syncPreferredLanguageFromCurrentUser(targetUrl: string | null = null): Observable<AuthenticatedUserLanguageSyncResult> {
    const userId: string | null = this.authService.getUserIdFromToken();
    if (!userId) {
      return of({ user: null, language: null, navigated: false });
    }

    return this.authApiService.getCurrentUserById(userId).pipe(
      tap((user: UserDto): void => this.currentUserService.setCurrentUser(user)),
      switchMap((user: UserDto): Observable<AuthenticatedUserLanguageSyncResult> => {
        const preferredLanguage: string = this.resolvePreferredLanguage(user.preferredLanguage);
        const localizedTargetUrl: string = this.buildLocalizedUrl(targetUrl ?? this.router.url, preferredLanguage);
        const shouldNavigate: boolean = localizedTargetUrl !== this.router.url;

        return this.translationService.useLang(preferredLanguage).pipe(
          switchMap((): Observable<boolean> => shouldNavigate ? from(this.router.navigateByUrl(localizedTargetUrl)) : of(false)),
          map((navigated: boolean): AuthenticatedUserLanguageSyncResult => ({
            user,
            language: preferredLanguage,
            navigated
          }))
        );
      }),
      catchError((error: unknown): Observable<AuthenticatedUserLanguageSyncResult> => {
        console.error('Unable to synchronize authenticated user preferred language.', error);
        return of({ user: null, language: null, navigated: false });
      })
    );
  }

  toPreferredLanguageUrl(url: string | null | undefined, language: string | null | undefined): string {
    return this.buildLocalizedUrl(url ?? this.router.url, this.resolvePreferredLanguage(language));
  }

  private resolvePreferredLanguage(preferredLanguage: string | null | undefined): string {
    const normalizedLanguage: string = this.normalizeLanguage(preferredLanguage);
    return resolveSupportedLanguage(normalizedLanguage, this.translationService.getCurrentLang() || 'en');
  }

  private normalizeLanguage(language: string | null | undefined): string {
    const normalizedLanguage: string = (language ?? '').trim().toLowerCase();

    if (!normalizedLanguage) {
      return '';
    }

    const shortLanguage: string = normalizedLanguage.split(/[-_]/)[0] ?? normalizedLanguage;
    return shortLanguage;
  }

  private buildLocalizedUrl(url: string, language: string): string {
    const fallbackLanguage: string = resolveSupportedLanguage(language, this.translationService.getCurrentLang() || 'en');
    const safeUrl: string = url && url.startsWith('/') ? url : `/${fallbackLanguage}/home`;
    const hashSeparatorIndex: number = safeUrl.indexOf('#');
    const urlWithoutFragment: string = hashSeparatorIndex >= 0 ? safeUrl.slice(0, hashSeparatorIndex) : safeUrl;
    const fragment: string = hashSeparatorIndex >= 0 ? safeUrl.slice(hashSeparatorIndex) : '';
    const querySeparatorIndex: number = urlWithoutFragment.indexOf('?');
    const path: string = querySeparatorIndex >= 0 ? urlWithoutFragment.slice(0, querySeparatorIndex) : urlWithoutFragment;
    const query: string = querySeparatorIndex >= 0 ? urlWithoutFragment.slice(querySeparatorIndex) : '';
    const segments: string[] = path.split('/').filter((segment: string) => segment.length > 0);

    if (segments.length > 0 && isSupportedLanguage(segments[0])) {
      segments[0] = fallbackLanguage;
    } else {
      segments.unshift(fallbackLanguage);
    }

    if (segments.length === 1) {
      segments.push('home');
    }

    return `/${segments.join('/')}${query}${fragment}`;
  }
}
