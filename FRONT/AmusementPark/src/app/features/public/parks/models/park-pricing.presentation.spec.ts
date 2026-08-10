import {
  formatParkPrice,
  ParkPriceFormattingLabels,
  resolvePricingLocalizedText,
} from './park-pricing.presentation';

describe('park pricing presentation', () => {
  const labels: ParkPriceFormattingLabels = {
    from: 'à partir de',
    upTo: 'jusqu’à',
    dynamic: 'tarif dynamique',
  };
  const euro: Intl.NumberFormat = new Intl.NumberFormat('fr', {
    style: 'currency',
    currency: 'EUR',
    maximumFractionDigits: 2,
  });

  it('formats a fixed price with Intl.NumberFormat', () => {
    expect(formatParkPrice({ mode: 'Fixed', amount: 49 }, 'EUR', 'fr', labels))
      .toBe(euro.format(49));
  });

  it('formats a price range', () => {
    expect(formatParkPrice({
      mode: 'Range',
      minimumAmount: 39,
      maximumAmount: 59,
    }, 'EUR', 'fr', labels)).toBe(`${euro.format(39)} – ${euro.format(59)}`);
  });

  it.each([
    [{ mode: 'Dynamic', minimumAmount: 39 }, `à partir de ${euro.format(39)}`],
    [{ mode: 'Dynamic', maximumAmount: 59 }, `jusqu’à ${euro.format(59)}`],
    [{ mode: 'Dynamic' }, 'tarif dynamique'],
  ] as const)('formats a dynamic price', (price, expected) => {
    expect(formatParkPrice(price, 'EUR', 'fr', labels)).toBe(expected);
  });

  it('resolves the requested localized label before English and the first value', () => {
    const values = [
      { languageCode: 'en', value: 'Adult' },
      { languageCode: 'fr', value: 'Adulte' },
      { languageCode: 'de', value: 'Erwachsene' },
    ];

    expect(resolvePricingLocalizedText(values, 'fr-FR')).toBe('Adulte');
    expect(resolvePricingLocalizedText(values, 'es')).toBe('Adult');
    expect(resolvePricingLocalizedText(values.slice(2), 'es', 'Fallback')).toBe('Erwachsene');
    expect(resolvePricingLocalizedText([], 'fr', 'Fallback')).toBe('Fallback');
  });
});
