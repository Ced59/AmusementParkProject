import { LocalizedItem } from '@app/models/shared/localized-item';

import { LocalizedTextPipe } from './localized-text.pipe';

describe('LocalizedTextPipe', () => {
  let pipe: LocalizedTextPipe;

  beforeEach(() => {
    pipe = new LocalizedTextPipe();
  });

  it('returns the localized text for the current language', () => {
    const items: LocalizedItem<string>[] = [
      { languageCode: 'en', value: 'Hello' },
      { languageCode: 'fr', value: 'Bonjour' }
    ];

    expect(pipe.transform(items, 'fr')).toBe('Bonjour');
  });

  it('falls back to configured fallback when no usable translation exists', () => {
    expect(pipe.transform([], 'fr', 'N/A')).toBe('N/A');
  });
});
