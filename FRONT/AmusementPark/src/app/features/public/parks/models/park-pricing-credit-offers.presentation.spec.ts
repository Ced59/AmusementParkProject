import { ParkPricing } from '@app/models/parks/park-pricing';
import { buildParkPricingHistorySeries } from './park-pricing.presentation';

describe('park pricing credit offers presentation', () => {
  it('builds a history series from unit code and quantity', () => {
    const pricing: ParkPricing = {
      parkId: 'park-1', currencyCode: 'RSD', notes: [], admissionOffers: [], annualPasses: [], parkingOffers: [],
      creditOffers: [{ unitCode: 'token', quantity: 10, labels: [{ languageCode: 'en', value: '10 tokens' }], prices: { gatePrice: 2500 }, conditions: [], sortOrder: 1 }],
      historicalSnapshots: [{ year: 2025, currencyCode: 'RSD', notes: [], admissionOffers: [], annualPasses: [], parkingOffers: [], creditOffers: [{ unitCode: 'token', quantity: 10, labels: [{ languageCode: 'en', value: '10 tokens' }], prices: { gatePrice: 2200 }, conditions: [], sortOrder: 1 }] }]
    };

    const [series] = buildParkPricingHistorySeries(pricing, 'en', 2026, 5);

    expect(series.kind).toBe('credit');
    expect(series.code).toBe('token:10');
    expect(series.points.map(point => point.gatePrice?.amount)).toEqual([2200, 2500]);
  });
});
