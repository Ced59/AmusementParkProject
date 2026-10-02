import { TestBed } from '@angular/core/testing';
import { firstValueFrom, Observable, of, throwError } from 'rxjs';
import { TranslationService } from '@app/services/translation.service';
import { completeTranslationsGuard } from './complete-translations.guard';

describe('completeTranslationsGuard', () => {
  const translations = { getCurrentLang: vi.fn().mockReturnValue('en'), loadCompleteTranslations: vi.fn() };
  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [{ provide: TranslationService, useValue: translations }] });
    translations.loadCompleteTranslations.mockReturnValue(of({}));
  });
  function activate(url: string): Promise<unknown> {
    const result = TestBed.runInInjectionContext(() => completeTranslationsGuard({} as never, { url } as never));
    return firstValueFrom(result as Observable<unknown>);
  }
  it('prepares the target route language before activating admin children', async () => {
    await expect(activate('/de/admin/parks?edit=1')).resolves.toBe(true);
    expect(translations.loadCompleteTranslations).toHaveBeenCalledWith('de');
  });
  it('does not activate a child when both translation delivery paths fail', async () => {
    translations.loadCompleteTranslations.mockReturnValue(throwError(() => new Error('Unavailable')));
    await expect(activate('/fr/admin')).rejects.toThrow('Unavailable');
  });
});
