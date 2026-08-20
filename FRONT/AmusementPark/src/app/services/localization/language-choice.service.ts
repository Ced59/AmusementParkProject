import { DestroyRef, Injectable } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { EMPTY, Observable, Subject, catchError, concatMap, map } from 'rxjs';

import { UsersApiService } from '@data-access/users/users-api.service';
import { UserDto } from '@app/models/users/user_dto';
import { AuthService } from '@app/services/auth/auth.service';
import { CurrentUserService } from '@app/services/users/current-user.service';
import { LanguagePreferenceService } from './language-preference.service';

interface SavedLanguagePreference {
  readonly language: string;
  readonly user: UserDto;
}

@Injectable({
  providedIn: 'root'
})
export class LanguageChoiceService {
  private readonly authenticatedLanguageChoices = new Subject<string>();

  constructor(
    private readonly languagePreferenceService: LanguagePreferenceService,
    private readonly authService: AuthService,
    private readonly usersApiService: UsersApiService,
    private readonly currentUserService: CurrentUserService,
    private readonly destroyRef: DestroyRef
  ) {
    this.authenticatedLanguageChoices.pipe(
      concatMap((language: string): Observable<SavedLanguagePreference> => {
        return this.usersApiService.updateCurrentUserPreferredLanguage(language.toUpperCase()).pipe(
          map((user: UserDto): SavedLanguagePreference => ({ language, user })),
          catchError((error: unknown): Observable<never> => {
            console.error('Unable to save the preferred language for the current user.', error);
            return EMPTY;
          })
        );
      }),
      takeUntilDestroyed(this.destroyRef)
    ).subscribe((savedPreference: SavedLanguagePreference): void => {
      const currentUserId: string | null = this.authService.getUserIdFromToken();
      if (currentUserId === savedPreference.user.id
        && this.languagePreferenceService.getPreferredLanguage() === savedPreference.language) {
        this.currentUserService.setCurrentUser(savedPreference.user);
      }
    });
  }

  chooseLanguage(language: string): string | null {
    const normalizedLanguage: string | null = this.languagePreferenceService.setPreferredLanguage(language);
    if (normalizedLanguage === null) {
      return null;
    }

    const userId: string | null = this.authService.getUserIdFromToken();
    if (userId !== null) {
      this.authenticatedLanguageChoices.next(normalizedLanguage);
    }

    return normalizedLanguage;
  }
}
