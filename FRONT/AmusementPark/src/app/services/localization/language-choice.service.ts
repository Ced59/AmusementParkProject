import { DestroyRef, Injectable } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

import { UsersApiService } from '@data-access/users/users-api.service';
import { UserDto } from '@app/models/users/user_dto';
import { AuthService } from '@app/services/auth/auth.service';
import { CurrentUserService } from '@app/services/users/current-user.service';
import { LanguagePreferenceService } from './language-preference.service';

@Injectable({
  providedIn: 'root'
})
export class LanguageChoiceService {
  constructor(
    private readonly languagePreferenceService: LanguagePreferenceService,
    private readonly authService: AuthService,
    private readonly usersApiService: UsersApiService,
    private readonly currentUserService: CurrentUserService,
    private readonly destroyRef: DestroyRef
  ) {
  }

  chooseLanguage(language: string): string | null {
    const normalizedLanguage: string | null = this.languagePreferenceService.setPreferredLanguage(language);
    if (normalizedLanguage === null) {
      return null;
    }

    const userId: string | null = this.authService.getUserIdFromToken();
    if (userId !== null) {
      this.usersApiService.updateCurrentUserPreferredLanguage(normalizedLanguage.toUpperCase())
        .pipe(takeUntilDestroyed(this.destroyRef))
        .subscribe({
          next: (user: UserDto): void => this.currentUserService.setCurrentUser(user),
          error: (error: unknown): void => console.error('Unable to save the preferred language for the current user.', error)
        });
    }

    return normalizedLanguage;
  }
}
