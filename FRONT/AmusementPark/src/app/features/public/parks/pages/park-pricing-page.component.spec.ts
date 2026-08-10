import type { MockedObject } from 'vitest';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { EventEmitter } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { ActivatedRoute, convertToParamMap } from '@angular/router';
import { TranslateService } from '@ngx-translate/core';
import { of, throwError } from 'rxjs';

import { Park } from '@app/models/parks/park';
import { ParkDetailSummary } from '@app/models/parks/park-detail-summary';
import { ParkPricing } from '@app/models/parks/park-pricing';
import { TranslationService } from '@app/services/translation.service';
import { SeoService } from '@core/seo/seo.service';
import { SsrHttpStatusService } from '@core/ssr/ssr-http-status.service';
import { ParksApiService } from '@data-access/parks/parks-api.service';
import {
  COMMON_TEST_IMPORTS,
  provideCommonTestDependencies,
} from '@app/testing/common-test-providers';
import { ParkPricingPageComponent } from './park-pricing-page.component';

class FakeTranslationService {
  public readonly languageChanged: EventEmitter<string> = new EventEmitter<string>();

  getCurrentLang(): string {
    return 'fr';
  }
}

describe('ParkPricingPageComponent', () => {
  let parksApiService: MockedObject<ParksApiService>;
  let seoService: MockedObject<SeoService>;
  let ssrHttpStatusService: MockedObject<SsrHttpStatusService>;

  beforeEach(async () => {
    parksApiService = {
      getParkDetailSummary: vi.fn().mockName('ParksApiService.getParkDetailSummary'),
      getParkPricing: vi.fn().mockName('ParksApiService.getParkPricing'),
    } as unknown as MockedObject<ParksApiService>;
    seoService = {
      applyParkPricingSeo: vi.fn().mockName('SeoService.applyParkPricingSeo'),
      applyParkUnavailableFeatureSeo: vi.fn().mockName('SeoService.applyParkUnavailableFeatureSeo'),
    } as unknown as MockedObject<SeoService>;
    ssrHttpStatusService = {
      setNotFound: vi.fn().mockName('SsrHttpStatusService.setNotFound'),
      setStatus: vi.fn().mockName('SsrHttpStatusService.setStatus'),
    } as unknown as MockedObject<SsrHttpStatusService>;

    parksApiService.getParkDetailSummary.mockReturnValue(of(createSummary()));
    parksApiService.getParkPricing.mockReturnValue(of(createPricing()));

    await TestBed.configureTestingModule({
      imports: [...COMMON_TEST_IMPORTS, ParkPricingPageComponent],
      providers: [
        ...provideCommonTestDependencies(),
        {
          provide: ActivatedRoute,
          useValue: {
            snapshot: { paramMap: convertToParamMap({ id: 'park-1', lang: 'fr' }) },
            parent: null,
            paramMap: of(convertToParamMap({ id: 'park-1', lang: 'fr' })),
          },
        },
        { provide: ParksApiService, useValue: parksApiService },
        { provide: SeoService, useValue: seoService },
        { provide: SsrHttpStatusService, useValue: ssrHttpStatusService },
        { provide: TranslationService, useClass: FakeTranslationService },
      ],
    }).compileComponents();

    const translateService: TranslateService = TestBed.inject(TranslateService);
    translateService.setTranslation('fr', {
      parkPricing: {
        page: {
          kicker: 'Préparer ta visite', title: 'Tarifs de {{name}}', titleShort: 'Tarifs',
          lead: 'Compare les tarifs de {{name}}.', backToPark: 'Retour à {{name}}',
          breadcrumbLabel: 'Fil', loadingTitle: 'Chargement', loadingMessage: 'Chargement',
          emptyTitle: 'Aucun tarif disponible', emptyMessage: 'Aucun tarif actuel.',
          errorTitle: 'Erreur', errorMessage: 'Erreur de chargement',
          unavailableTitle: 'Tarifs indisponibles pour {{name}}',
          unavailableMessage: 'Uniquement pour les parcs ouverts.',
        },
        disclaimer: { title: 'Tarifs indicatifs', message: 'Vérifie le prix final de {{name}}.' },
        sections: {
          admission: { title: 'Billets', subtitle: 'Par catégorie' },
          passes: { title: 'Pass', subtitle: 'Toute l’année' },
          parking: { title: 'Parking', subtitle: 'Stationnement' },
        },
        channels: { online: 'En ligne', gate: 'Au guichet' },
        modes: { Fixed: 'Prix fixe', Range: 'Plage', Dynamic: 'Dynamique' },
        price: { from: 'à partir de', upTo: 'jusqu’à', dynamic: 'tarif dynamique' },
        audiences: { adult: 'Adulte' },
        validity: { range: 'Du {{from}} au {{to}}', from: 'Depuis {{date}}', until: 'Jusqu’au {{date}}' },
        fields: { conditions: 'Conditions', currency: 'Devise', lastVerified: 'Vérifié', source: 'Source', purchase: 'Achat', notes: 'Notes' },
        actions: { buy: 'Acheter', buyOffer: 'Voir', openSource: 'Source' },
        meta: { title: 'Informations' },
      },
    });
    translateService.use('fr');
  });

  it('loads and renders the public pricing through ParksApiService', () => {
    const fixture: ComponentFixture<ParkPricingPageComponent> = createComponent();
    const text: string = (fixture.nativeElement as HTMLElement).textContent ?? '';

    expect(parksApiService.getParkPricing).toHaveBeenCalledWith('park-1', expect.any(Object));
    expect(text).toContain('Adulte');
    expect(text).toContain('En ligne');
    expect(text).toContain('49');
    expect(text).toContain('Tarifs indicatifs');
    expect(text).toContain('Billets datés uniquement.');
    expect(seoService.applyParkPricingSeo).toHaveBeenCalledWith(
      'Bellewaerde', 'fr', expect.any(String), 1, null,
      '/fr/park/park-1/bellewaerde/pricing',
    );
  });

  it('does not request current pricing for a non-operating park', () => {
    parksApiService.getParkDetailSummary.mockReturnValue(of(createSummary('Planned')));

    const fixture: ComponentFixture<ParkPricingPageComponent> = createComponent();

    expect(parksApiService.getParkPricing).not.toHaveBeenCalled();
    expect(ssrHttpStatusService.setNotFound).toHaveBeenCalled();
    expect(seoService.applyParkUnavailableFeatureSeo).toHaveBeenCalledWith(
      expect.objectContaining({ status: 'Planned' }), 'pricing', 'fr',
      expect.any(String), null, '/fr/park/park-1/bellewaerde',
    );
    expect((fixture.nativeElement as HTMLElement).textContent).toContain(
      'Uniquement pour les parcs ouverts.',
    );
  });

  it('returns an empty noindex state when public pricing does not exist', () => {
    parksApiService.getParkPricing.mockReturnValue(throwError(() => new HttpErrorResponse({ status: 404 })));

    const fixture: ComponentFixture<ParkPricingPageComponent> = createComponent();

    expect(ssrHttpStatusService.setNotFound).toHaveBeenCalled();
    expect(seoService.applyParkPricingSeo).toHaveBeenCalledWith(
      'Bellewaerde', 'fr', expect.any(String), 0, null,
      '/fr/park/park-1/bellewaerde/pricing',
    );
    expect((fixture.nativeElement as HTMLElement).textContent).toContain('Aucun tarif disponible');
  });

  function createComponent(): ComponentFixture<ParkPricingPageComponent> {
    const fixture: ComponentFixture<ParkPricingPageComponent> = TestBed.createComponent(ParkPricingPageComponent);
    fixture.detectChanges();
    return fixture;
  }
});

function createSummary(status: Park['status'] = 'Operating'): ParkDetailSummary {
  return {
    park: {
      id: 'park-1', name: 'Bellewaerde', countryCode: 'BE', latitude: 50.845,
      longitude: 2.945, isVisible: true, status,
    },
    mainImage: null,
    references: {},
    stats: {
      totalItems: 0, zoneCount: 0, attractionCount: 0, restaurantCount: 0,
      showCount: 0, shopCount: 0, hotelCount: 0, countsByCategory: {},
    },
  };
}

function createPricing(): ParkPricing {
  return {
    parkId: 'park-1', currencyCode: 'EUR', sourceUrl: 'https://example.com/prices',
    purchaseUrl: 'https://example.com/tickets', lastVerifiedAtUtc: '2026-08-01T12:00:00Z',
    notes: [
      { languageCode: 'en', value: 'Dated tickets only.' },
      { languageCode: 'fr', value: 'Billets datés uniquement.' },
    ],
    admissionOffers: [{
      code: 'adult', audienceCategory: 'adult',
      labels: [{ languageCode: 'fr', value: 'Adulte' }],
      onlinePrice: { mode: 'Fixed', amount: 49 }, gatePrice: null,
      validFrom: '2026-01-01', validTo: '2026-12-31',
      purchaseUrl: null, conditions: [], sortOrder: 0,
    }],
    annualPasses: [],
    parkingOffers: [],
  };
}
