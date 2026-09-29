import { ParkPricing } from '@app/models/parks/park-pricing';
import { StandaloneAttractionPricingCardComponent } from './standalone-attraction-pricing-card.component';

describe('StandaloneAttractionPricingCardComponent', () => {
  it('builds localized summaries for admission and credit offers', () => {
    const component = new StandaloneAttractionPricingCardComponent();
    component.currentLanguage = 'fr';
    component.pricing = createPricing();

    expect(component.summaries).toHaveLength(2);
    expect(component.summaries[0].label).toBe('Un passage');
    expect(component.summaries[0].price).toContain('9');
    expect(component.summaries[1].label).toBe('Dix crédits');
    expect(component.summaries[1].price).toContain('15');
  });
});

function createPricing(): ParkPricing {
  return {
    parkId: 'standalone-1',
    currencyCode: 'EUR',
    notes: [],
    admissionOffers: [{
      code: 'single-ride',
      audienceCategory: 'all',
      labels: [
        { languageCode: 'fr', value: 'Un passage' },
        { languageCode: 'en', value: 'One ride' }
      ],
      gatePrice: { mode: 'Fixed', amount: 9 },
      conditions: [],
      sortOrder: 1
    }],
    annualPasses: [],
    parkingOffers: [],
    creditOffers: [{
      unitCode: 'ride',
      quantity: 10,
      labels: [
        { languageCode: 'fr', value: 'Dix crédits' },
        { languageCode: 'en', value: 'Ten credits' }
      ],
      prices: { onlinePrice: 15 },
      conditions: [],
      sortOrder: 2
    }]
  };
}
