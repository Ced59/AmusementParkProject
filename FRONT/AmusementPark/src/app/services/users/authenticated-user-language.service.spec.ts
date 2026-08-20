import type { MockedObject } from 'vitest';
import { Router } from '@angular/router';
import { firstValueFrom, of } from 'rxjs';

import { AuthApiService } from '@data-access/auth/auth-api.service';
import { UserDto } from '@app/models/users/user_dto';
import { AuthService } from '@app/services/auth/auth.service';
import { LanguagePreferenceService } from '@app/services/localization/language-preference.service';
import { MeasurementPreferenceService } from '@app/services/measurements/measurement-preference.service';
import { TranslationService } from '@app/services/translation.service';
import { CurrentUserService } from './current-user.service';
import { AuthenticatedUserLanguageService } from './authenticated-user-language.service';

const user: UserDto = {
  id: 'user-1',
  email: 'user@example.com',
  firstName: 'Ada',
  lastName: 'Lovelace',
  isActivated: true,
  isBlocked: false,
  roles: ['USER'],
  preferredLanguage: 'FR',
  preferredMeasurementSystem: 'Metric',
  avatarUrl: '',
  createdAt: '2026-08-20T00:00:00Z',
  updatedAt: '2026-08-20T00:00:00Z',
};

describe('AuthenticatedUserLanguageService', () => {
  it('hydrates account preferences after automatic session restoration without navigating', async () => {
    const dependencies = createDependencies('/');
    const service = createService(dependencies);

    const result: UserDto | null = await firstValueFrom(service.hydratePreferencesFromCurrentUser());

    expect(result).toBe(user);
    expect(dependencies.currentUserService.setCurrentUser).toHaveBeenCalledWith(user);
    expect(dependencies.measurementPreferenceService.syncFromUser).toHaveBeenCalledWith(user);
    expect(dependencies.languagePreferenceService.setPreferredLanguage).toHaveBeenCalledWith('FR');
    expect(dependencies.translationService.useLang).not.toHaveBeenCalled();
    expect(dependencies.router.navigateByUrl).not.toHaveBeenCalled();
  });

  it('keeps an explicit route language ahead of the account preference', async () => {
    const dependencies = createDependencies('/de/parks');
    const service = createService(dependencies);

    const result = await firstValueFrom(service.syncPreferredLanguageFromCurrentUser());

    expect(result.language).toBe('de');
    expect(result.navigated).toBe(false);
    expect(dependencies.translationService.useLang).toHaveBeenCalledWith('de');
    expect(dependencies.router.navigateByUrl).not.toHaveBeenCalled();
  });

  it('uses the account preference when the current url is neutral', async () => {
    const dependencies = createDependencies('/');
    const service = createService(dependencies);

    const result = await firstValueFrom(service.syncPreferredLanguageFromCurrentUser());

    expect(result.language).toBe('fr');
    expect(result.navigated).toBe(true);
    expect(dependencies.translationService.useLang).toHaveBeenCalledWith('fr');
    expect(dependencies.router.navigateByUrl).toHaveBeenCalledWith('/fr/home');
  });
});

interface Dependencies {
  authApiService: MockedObject<AuthApiService>;
  authService: MockedObject<AuthService>;
  currentUserService: MockedObject<CurrentUserService>;
  measurementPreferenceService: MockedObject<MeasurementPreferenceService>;
  languagePreferenceService: MockedObject<LanguagePreferenceService>;
  router: MockedObject<Router>;
  translationService: MockedObject<TranslationService>;
}

function createDependencies(url: string): Dependencies {
  const authApiService = {
    getCurrentUserById: vi.fn().mockReturnValue(of(user)),
  } as unknown as MockedObject<AuthApiService>;
  const authService = {
    getUserIdFromToken: vi.fn().mockReturnValue('user-1'),
  } as unknown as MockedObject<AuthService>;
  const currentUserService = {
    setCurrentUser: vi.fn(),
  } as unknown as MockedObject<CurrentUserService>;
  const measurementPreferenceService = {
    syncFromUser: vi.fn(),
  } as unknown as MockedObject<MeasurementPreferenceService>;
  const languagePreferenceService = {
    setPreferredLanguage: vi.fn().mockReturnValue('fr'),
  } as unknown as MockedObject<LanguagePreferenceService>;
  const router = {
    url,
    navigateByUrl: vi.fn().mockResolvedValue(true),
  } as unknown as MockedObject<Router>;
  const translationService = {
    getCurrentLang: vi.fn().mockReturnValue('en'),
    useLang: vi.fn().mockReturnValue(of(null)),
  } as unknown as MockedObject<TranslationService>;

  return {
    authApiService,
    authService,
    currentUserService,
    measurementPreferenceService,
    languagePreferenceService,
    router,
    translationService,
  };
}

function createService(dependencies: Dependencies): AuthenticatedUserLanguageService {
  return new AuthenticatedUserLanguageService(
    dependencies.authApiService,
    dependencies.authService,
    dependencies.currentUserService,
    dependencies.measurementPreferenceService,
    dependencies.languagePreferenceService,
    dependencies.router,
    dependencies.translationService
  );
}
