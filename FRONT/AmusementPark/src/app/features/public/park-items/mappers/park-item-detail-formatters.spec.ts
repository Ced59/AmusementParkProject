import { formatAge, resolveOptionalLocalizedText } from './park-item-detail-formatters';

describe('park item detail formatters', () => {
  it('formats ages in every supported language', () => {
    const expectedAges: Record<string, readonly [string, string]> = {
      de: ['1 Jahr', '3 Jahre'],
      en: ['1 year', '3 years'],
      es: ['1 año', '3 años'],
      fr: ['1 an', '3 ans'],
      it: ['1 anno', '3 anni'],
      nl: ['1 jaar', '3 jaar'],
      pl: ['1 rok', '3 lata'],
      pt: ['1 ano', '3 anos']
    };

    for (const [languageCode, ages] of Object.entries(expectedAges)) {
      expect(formatAge(1, languageCode)).toBe(ages[0]);
      expect(formatAge(3, languageCode)).toBe(ages[1]);
    }

    expect(formatAge(7, 'pl')).toBe('7 lat');
    expect(formatAge(3, 'de-DE')).toBe('3 Jahre');
  });

  it('does not leak another language when an access condition lacks the requested translation', () => {
    const labels = [
      { languageCode: 'en', value: 'Not recommended during pregnancy' },
      { languageCode: 'fr', value: 'Déconseillé pendant la grossesse' }
    ];

    expect(resolveOptionalLocalizedText(labels, 'de')).toBeNull();
    expect(resolveOptionalLocalizedText(labels, 'fr-FR')).toBe('Déconseillé pendant la grossesse');
  });
});
