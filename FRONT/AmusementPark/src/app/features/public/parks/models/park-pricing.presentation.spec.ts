import {
  buildParkPricingHistorySeries,
  formatParkPrice,
  hasSingleHistoryCurrency,
  parkPriceChartAmount,
  ParkPriceFormattingLabels,
  resolvePricingLocalizedText,
} from './park-pricing.presentation';
import { ParkPricing } from '@app/models/parks/park-pricing';

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

  it('builds a five-year series from stable product codes and keeps the current localized label', () => {
    const pricing: ParkPricing = createPricingHistory(['EUR', 'EUR', 'EUR', 'EUR']);

    const series = buildParkPricingHistorySeries(pricing, 'fr', 2026, 5);

    expect(series).toHaveLength(1);
    const [historySeries] = series;
    expect(historySeries.label).toBe('Adulte actuel');
    expect(historySeries.points.map(point => point.year)).toEqual([2022, 2023, 2024, 2025, 2026]);
    expect(historySeries.points.map(point => point.onlinePrice?.amount)).toEqual([35, 37, 39, 42, 49]);
  });

  it('keeps currency changes explicit instead of treating them as one comparable series', () => {
    const pricing: ParkPricing = createPricingHistory(['HRK', 'HRK', 'EUR', 'EUR']);
    const [series] = buildParkPricingHistorySeries(pricing, 'en', 2026, 5);

    expect(hasSingleHistoryCurrency(series)).toBe(false);
    expect(series.points.map(point => point.currencyCode)).toEqual(['HRK', 'HRK', 'EUR', 'EUR', 'EUR']);
  });

  it('uses fixed amounts and advertised minimums as chart values', () => {
    expect(parkPriceChartAmount({ mode: 'Fixed', amount: 49 })).toBe(49);
    expect(parkPriceChartAmount({ mode: 'Range', minimumAmount: 39, maximumAmount: 59 })).toBe(39);
    expect(parkPriceChartAmount({ mode: 'Dynamic', minimumAmount: 35 })).toBe(35);
    expect(parkPriceChartAmount({ mode: 'Dynamic', maximumAmount: 60 })).toBeNull();
  });
});

function createPricingHistory(currencies: string[]): ParkPricing {
  const years: number[] = [2022, 2023, 2024, 2025];
  const amounts: number[] = [35, 37, 39, 42];
  return {
    parkId: 'park-1',
    currencyCode: 'EUR',
    notes: [],
    admissionOffers: [{
      code: 'adult', audienceCategory: 'adult',
      labels: [{ languageCode: 'fr', value: 'Adulte actuel' }, { languageCode: 'en', value: 'Current adult' }],
      onlinePrice: { mode: 'Fixed', amount: 49 }, gatePrice: null,
      conditions: [], sortOrder: 1,
    }],
    annualPasses: [],
    parkingOffers: [],
    historicalSnapshots: years.map((year: number, index: number) => ({
      year,
      currencyCode: currencies[index] ?? 'EUR',
      notes: [],
      admissionOffers: [{
        code: 'adult', audienceCategory: 'adult',
        labels: [{ languageCode: 'fr', value: `Adulte ${year}` }, { languageCode: 'en', value: `Adult ${year}` }],
        onlinePrice: { mode: 'Fixed', amount: amounts[index] ?? 0 }, gatePrice: null,
        conditions: [], sortOrder: 1,
      }],
      annualPasses: [],
      parkingOffers: [],
    })),
  };
}
