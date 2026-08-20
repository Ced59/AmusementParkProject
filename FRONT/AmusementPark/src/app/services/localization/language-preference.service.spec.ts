import { LanguagePreferenceService } from './language-preference.service';
import { LANGUAGE_PREFERENCE_STORAGE_KEY } from '@shared/models/localization';

describe('LanguagePreferenceService', () => {
  it('restores a supported local preference', () => {
    const localStorage = createStorage('FR');
    const document = createDocument(localStorage);
    const service = new LanguagePreferenceService('browser' as unknown as object, document);

    expect(service.getPreferredLanguage()).toBe('fr');
    expect(localStorage.getItem).toHaveBeenCalledWith(LANGUAGE_PREFERENCE_STORAGE_KEY);
  });

  it('falls back to the technical cookie when local storage has no preference', () => {
    const localStorage = createStorage(null);
    const document = createDocument(localStorage, 'theme=dark; amusementpark.language=pt');
    const service = new LanguagePreferenceService('browser' as unknown as object, document);

    expect(service.getPreferredLanguage()).toBe('pt');
  });

  it('persists a normalized preference in local storage and the root cookie', () => {
    const localStorage = createStorage(null);
    const document = createDocument(localStorage);
    const service = new LanguagePreferenceService('browser' as unknown as object, document);

    expect(service.setPreferredLanguage(' de-DE ')).toBe('de');
    expect(service.preferredLanguage()).toBe('de');
    expect(localStorage.setItem).toHaveBeenCalledWith(LANGUAGE_PREFERENCE_STORAGE_KEY, 'de');
    expect(document.cookie).toContain('amusementpark.language=de');
    expect(document.cookie).toContain('SameSite=Lax');
    expect(document.cookie).toContain('Secure');
  });

  it('rejects unsupported preferences without writing browser state', () => {
    const localStorage = createStorage(null);
    const document = createDocument(localStorage);
    const service = new LanguagePreferenceService('browser' as unknown as object, document);

    expect(service.setPreferredLanguage('ja')).toBeNull();
    expect(localStorage.setItem).not.toHaveBeenCalled();
    expect(document.cookie).toBe('');
  });

  it('does not access browser storage during server rendering', () => {
    const localStorage = createStorage('fr');
    const document = createDocument(localStorage);
    const service = new LanguagePreferenceService('server' as unknown as object, document);

    expect(service.getPreferredLanguage()).toBeNull();
    expect(localStorage.getItem).not.toHaveBeenCalled();
  });
});

function createStorage(value: string | null): Storage & {
  getItem: ReturnType<typeof vi.fn>;
  setItem: ReturnType<typeof vi.fn>;
} {
  return {
    clear: vi.fn(),
    getItem: vi.fn().mockReturnValue(value),
    key: vi.fn().mockReturnValue(null),
    length: 0,
    removeItem: vi.fn(),
    setItem: vi.fn(),
  } as unknown as Storage & {
    getItem: ReturnType<typeof vi.fn>;
    setItem: ReturnType<typeof vi.fn>;
  };
}

function createDocument(localStorage: Storage, cookie: string = ''): Document {
  return {
    cookie,
    defaultView: {
      localStorage,
    },
    location: {
      protocol: 'https:',
    },
  } as unknown as Document;
}
