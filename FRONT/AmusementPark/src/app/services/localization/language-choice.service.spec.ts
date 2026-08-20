import type { MockedObject } from 'vitest';
import { DestroyRef } from '@angular/core';
import { of, Subject } from 'rxjs';

import { UsersApiService } from '@data-access/users/users-api.service';
import { UserDto } from '@app/models/users/user_dto';
import { AuthService } from '@app/services/auth/auth.service';
import { CurrentUserService } from '@app/services/users/current-user.service';
import { LanguageChoiceService } from './language-choice.service';
import { LanguagePreferenceService } from './language-preference.service';

describe('LanguageChoiceService', () => {
  const user: UserDto = {
    id: 'user-1',
    email: 'user@example.com',
    firstName: 'Ada',
    lastName: 'Lovelace',
    isActivated: true,
    isBlocked: false,
    roles: ['USER'],
    preferredLanguage: 'FR',
    avatarUrl: '',
    createdAt: '2026-08-20T00:00:00Z',
    updatedAt: '2026-08-20T00:00:00Z',
  };

  it('stores an anonymous visitor choice without calling the account API', () => {
    const dependencies = createDependencies();
    dependencies.authService.getUserIdFromToken.mockReturnValue(null);
    dependencies.languagePreferenceService.setPreferredLanguage.mockReturnValue('fr');
    const service = createService(dependencies);

    expect(service.chooseLanguage('FR')).toBe('fr');
    expect(dependencies.usersApiService.updateCurrentUserPreferredLanguage).not.toHaveBeenCalled();
  });

  it('also saves an authenticated user choice through the dedicated endpoint', () => {
    const dependencies = createDependencies();
    dependencies.authService.getUserIdFromToken.mockReturnValue('user-1');
    dependencies.languagePreferenceService.setPreferredLanguage.mockReturnValue('fr');
    dependencies.languagePreferenceService.getPreferredLanguage.mockReturnValue('fr');
    dependencies.usersApiService.updateCurrentUserPreferredLanguage.mockReturnValue(of(user));
    const service = createService(dependencies);

    expect(service.chooseLanguage('fr')).toBe('fr');
    expect(dependencies.usersApiService.updateCurrentUserPreferredLanguage).toHaveBeenCalledWith('FR');
    expect(dependencies.currentUserService.setCurrentUser).toHaveBeenCalledWith(user);
  });

  it('serializes rapid authenticated choices and only exposes the latest response', () => {
    const dependencies = createDependencies();
    const firstResponse = new Subject<UserDto>();
    const germanUser: UserDto = { ...user, preferredLanguage: 'DE' };
    let currentLanguage: string | null = null;
    dependencies.authService.getUserIdFromToken.mockReturnValue('user-1');
    dependencies.languagePreferenceService.setPreferredLanguage.mockImplementation((language: string | null | undefined): string | null => {
      currentLanguage = language?.toLowerCase() ?? null;
      return currentLanguage;
    });
    dependencies.languagePreferenceService.getPreferredLanguage.mockImplementation((): string | null => currentLanguage);
    dependencies.usersApiService.updateCurrentUserPreferredLanguage
      .mockReturnValueOnce(firstResponse)
      .mockReturnValueOnce(of(germanUser));
    const service = createService(dependencies);

    service.chooseLanguage('fr');
    service.chooseLanguage('de');

    expect(dependencies.usersApiService.updateCurrentUserPreferredLanguage).toHaveBeenCalledTimes(1);
    expect(dependencies.usersApiService.updateCurrentUserPreferredLanguage).toHaveBeenLastCalledWith('FR');

    firstResponse.next(user);
    firstResponse.complete();

    expect(dependencies.usersApiService.updateCurrentUserPreferredLanguage).toHaveBeenCalledTimes(2);
    expect(dependencies.usersApiService.updateCurrentUserPreferredLanguage).toHaveBeenLastCalledWith('DE');
    expect(dependencies.currentUserService.setCurrentUser).toHaveBeenCalledTimes(1);
    expect(dependencies.currentUserService.setCurrentUser).toHaveBeenCalledWith(germanUser);
  });

  it('ignores unsupported choices', () => {
    const dependencies = createDependencies();
    dependencies.languagePreferenceService.setPreferredLanguage.mockReturnValue(null);
    const service = createService(dependencies);

    expect(service.chooseLanguage('ja')).toBeNull();
    expect(dependencies.authService.getUserIdFromToken).not.toHaveBeenCalled();
  });
});

interface Dependencies {
  languagePreferenceService: MockedObject<LanguagePreferenceService>;
  authService: MockedObject<AuthService>;
  usersApiService: MockedObject<UsersApiService>;
  currentUserService: MockedObject<CurrentUserService>;
}

function createDependencies(): Dependencies {
  return {
    languagePreferenceService: {
      getPreferredLanguage: vi.fn(),
      setPreferredLanguage: vi.fn(),
    } as unknown as MockedObject<LanguagePreferenceService>,
    authService: {
      getUserIdFromToken: vi.fn(),
    } as unknown as MockedObject<AuthService>,
    usersApiService: {
      updateCurrentUserPreferredLanguage: vi.fn(),
    } as unknown as MockedObject<UsersApiService>,
    currentUserService: {
      setCurrentUser: vi.fn(),
    } as unknown as MockedObject<CurrentUserService>,
  };
}

function createService(dependencies: Dependencies): LanguageChoiceService {
  const destroyRef: DestroyRef = {
    onDestroy: vi.fn().mockReturnValue((): void => undefined),
  } as unknown as DestroyRef;
  return new LanguageChoiceService(
    dependencies.languagePreferenceService,
    dependencies.authService,
    dependencies.usersApiService,
    dependencies.currentUserService,
    destroyRef
  );
}
