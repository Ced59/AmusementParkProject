import { ParkPricing } from '@app/models/parks/park-pricing';
import { TranslateService } from '@ngx-translate/core';
import { StandaloneAttractionPricingCardComponent } from './standalone-attraction-pricing-card.component';

describe('StandaloneAttractionPricingCardComponent', () => {
  it('builds localized summaries for every supported offer type', () => {
    const component = createComponent();
    component.currentLanguage = 'fr';
    component.pricing = createPricing();

    expect(component.summaries).toHaveLength(4);
    expect(component.summaries[0].label).toBe('Un passage');
    expect(component.summaries[0].price).toContain('9');
    expect(component.summaries[1].label).toBe('Pass saison');
    expect(component.summaries[1].price).toContain('99');
    expect(component.summaries[2].label).toBe('Parking journée');
    expect(component.summaries[2].price).toContain('6');
    expect(component.summaries[3].label).toBe('Dix crédits');
    expect(component.summaries[3].price).toContain('15');
  });

  it('keeps unbounded dynamic prices and offer purchase links visible', () => {
    const component = createComponent();
    component.currentLanguage = 'fr';
    const pricing: ParkPricing = createPricing();
    pricing.purchaseUrl = null;
    pricing.admissionOffers = [{
      ...pricing.admissionOffers[0],
      gatePrice: { mode: 'Dynamic' },
      purchaseUrl: 'https://example.test/buy-ticket'
    }];
    pricing.annualPasses = [];
    pricing.parkingOffers = [];
    pricing.creditOffers = [];
    component.pricing = pricing;

    expect(component.summaries).toEqual([
      expect.objectContaining({
        price: 'tarif dynamique',
        purchaseUrl: 'https://example.test/buy-ticket'
      })
    ]);
  });

  it('does not truncate later offer families when admission has four offers', () => {
    const component = createComponent();
    const pricing: ParkPricing = createPricing();
    pricing.admissionOffers = [0, 1, 2, 3].map((index: number) => ({
      ...pricing.admissionOffers[0],
      id: `admission-${index}`,
      code: `admission-${index}`
    }));
    component.pricing = pricing;

    expect(component.summaries).toHaveLength(7);
    expect(component.summaries.map((summary) => summary.key)).toEqual(expect.arrayContaining([
      'annual-pass:season-pass',
      'parking:day-parking',
      'credit:ride:10'
    ]));
  });
});

function createComponent(): StandaloneAttractionPricingCardComponent {
  const translateService: TranslateService = {
    instant: (key: string): string => ({
      'parkPricing.price.from': 'à partir de',
      'parkPricing.price.upTo': 'jusqu’à',
      'parkPricing.price.dynamic': 'tarif dynamique'
    })[key] ?? key
  } as unknown as TranslateService;
  return new StandaloneAttractionPricingCardComponent(translateService);
}

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
    annualPasses: [{
      code: 'season-pass',
      names: [
        { languageCode: 'fr', value: 'Pass saison' },
        { languageCode: 'en', value: 'Season pass' }
      ],
      onlinePrice: { mode: 'Fixed', amount: 99 },
      conditions: [],
      sortOrder: 2
    }],
    parkingOffers: [{
      code: 'day-parking',
      labels: [
        { languageCode: 'fr', value: 'Parking journée' },
        { languageCode: 'en', value: 'Day parking' }
      ],
      gatePrice: { mode: 'Fixed', amount: 6 },
      conditions: [],
      sortOrder: 3
    }],
    creditOffers: [{
      unitCode: 'ride',
      quantity: 10,
      labels: [
        { languageCode: 'fr', value: 'Dix crédits' },
        { languageCode: 'en', value: 'Ten credits' }
      ],
      prices: { onlinePrice: 15 },
      conditions: [],
      sortOrder: 4
    }]
  };
}
