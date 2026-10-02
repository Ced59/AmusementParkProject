import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import { PublicFirstTranslateLoader } from './public-first-translate.loader';

describe('PublicFirstTranslateLoader', () => {
  let http: HttpTestingController;
  let loader: PublicFirstTranslateLoader;
  const publicDictionary = { home: { title: 'Accueil' }, admin: { parks: { types: { zoo: 'Zoo' } } } };
  const adminDictionary = { admin: { parks: { types: { zoo: 'Zoo' } }, dashboard: { title: 'Administration' } } };

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    http = TestBed.inject(HttpTestingController);
    loader = new PublicFirstTranslateLoader(TestBed.inject(HttpClient));
  });
  afterEach(() => http.verify());

  it('loads only the public payload and reuses it for anonymous visitors', async () => {
    const initial = firstValueFrom(loader.getTranslation('fr'));
    http.expectOne('./assets/i18n/public/fr.json').flush(publicDictionary);
    await expect(initial).resolves.toEqual(publicDictionary);
    await expect(firstValueFrom(loader.getTranslation('fr'))).resolves.toEqual(publicDictionary);
    expect(loader.requiresCompleteTranslations).toBe(false);
  });

  it('adds admin labels without requesting the public dictionary again', async () => {
    const initial = firstValueFrom(loader.getTranslation('fr'));
    http.expectOne('./assets/i18n/public/fr.json').flush(publicDictionary);
    await initial;
    const complete = firstValueFrom(loader.getCompleteTranslation('fr'));
    http.expectOne('./assets/i18n/admin/fr.json').flush(adminDictionary);
    await expect(complete).resolves.toEqual({ ...publicDictionary, ...adminDictionary });
    await expect(firstValueFrom(loader.getTranslation('fr'))).resolves.toEqual({ ...publicDictionary, ...adminDictionary });
  });

  it('loads complete dictionaries for subsequent language changes', async () => {
    const first = firstValueFrom(loader.getCompleteTranslation('fr'));
    http.expectOne('./assets/i18n/public/fr.json').flush(publicDictionary);
    http.expectOne('./assets/i18n/admin/fr.json').flush(adminDictionary);
    await first;
    const next = firstValueFrom(loader.getTranslation('de'));
    http.expectOne('./assets/i18n/public/de.json').flush({ home: 'Start' });
    http.expectOne('./assets/i18n/admin/de.json').flush({ admin: 'Verwaltung' });
    await expect(next).resolves.toEqual({ home: 'Start', admin: 'Verwaltung' });
  });

  it('falls back to the existing complete file if a public payload is unavailable', async () => {
    const result = firstValueFrom(loader.getTranslation('fr'));
    http.expectOne('./assets/i18n/public/fr.json').flush(null, { status: 404, statusText: 'Not Found' });
    http.expectOne('./assets/i18n/fr.json').flush({ ...publicDictionary, ...adminDictionary });
    await expect(result).resolves.toEqual({ ...publicDictionary, ...adminDictionary });
  });

  it('falls back to the complete file when the admin fragment fails', async () => {
    const result = firstValueFrom(loader.getCompleteTranslation('fr'));
    http.expectOne('./assets/i18n/public/fr.json').flush(publicDictionary);
    http.expectOne('./assets/i18n/admin/fr.json').flush(null, { status: 500, statusText: 'Error' });
    http.expectOne('./assets/i18n/fr.json').flush({ ...publicDictionary, ...adminDictionary });
    await expect(result).resolves.toEqual({ ...publicDictionary, ...adminDictionary });
  });

  it('can retry a failed complete load without caching the failure', async () => {
    const result = firstValueFrom(loader.getCompleteTranslation('fr'));
    const rejection = expect(result).rejects.toBeTruthy();
    http.expectOne('./assets/i18n/public/fr.json').flush(publicDictionary);
    http.expectOne('./assets/i18n/admin/fr.json').flush(null, { status: 500, statusText: 'Error' });
    http.expectOne('./assets/i18n/fr.json').flush(null, { status: 500, statusText: 'Error' });
    await rejection;
    const retry = firstValueFrom(loader.getCompleteTranslation('fr'));
    http.expectOne('./assets/i18n/admin/fr.json').flush(adminDictionary);
    await expect(retry).resolves.toEqual({ ...publicDictionary, ...adminDictionary });
  });
});
