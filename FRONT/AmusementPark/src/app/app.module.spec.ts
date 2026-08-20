import { defer, of } from 'rxjs';

import { AuthService } from '@app/services/auth/auth.service';
import { TranslationService } from '@app/services/translation.service';
import { AuthenticatedUserLanguageService } from '@app/services/users/authenticated-user-language.service';
import { initializeApp } from './app.module';

describe('initializeApp', () => {
  it('restores account preferences after language and session initialization', async () => {
    const calls: string[] = [];
    const translationService = {
      initializeLanguage: vi.fn().mockImplementation(async (): Promise<void> => {
        calls.push('language');
      }),
    } as unknown as TranslationService;
    const authService = {
      initializeSession: vi.fn().mockImplementation(async (): Promise<void> => {
        calls.push('session');
      }),
    } as unknown as AuthService;
    const authenticatedUserLanguageService = {
      hydratePreferencesFromCurrentUser: vi.fn().mockReturnValue(defer(() => {
        calls.push('preferences');
        return of(null);
      })),
    } as unknown as AuthenticatedUserLanguageService;

    await initializeApp(
      translationService,
      authService,
      authenticatedUserLanguageService
    )();

    expect(calls).toEqual(['language', 'session', 'preferences']);
  });
});
