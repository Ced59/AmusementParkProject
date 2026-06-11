import { TestBed } from '@angular/core/testing';
import { Observable, of, throwError } from 'rxjs';

import { ImageCategory } from '@app/models/images/image-category';
import { ImageOwnerType } from '@app/models/images/image-owner-type';
import { ParkDetailSummary } from '@app/models/parks/park-detail-summary';
import { Park } from '@app/models/parks/park';
import { SsrHttpStatusService } from '@core/ssr/ssr-http-status.service';
import { CountryDisplayService } from '@shared/services/countries/country-display.service';
import {
  PARK_DETAIL_PARKS_PORT,
  ParkDetailParksPort
} from './park-detail-data.ports';
import { ParkDetailStateFacade } from './park-detail-state.facade';

class FakeParksPort implements ParkDetailParksPort {
  public summaryResponse$: Observable<ParkDetailSummary> = of(createSummary());
  public readonly summaryCalls: string[] = [];

  getParkDetailSummary(id: string): Observable<ParkDetailSummary> {
    this.summaryCalls.push(id);
    return this.summaryResponse$;
  }
}

class FakeSsrHttpStatusService {
  public notFoundCallCount = 0;

  setNotFound(): void {
    this.notFoundCallCount += 1;
  }
}

function createPark(): Park {
  return {
    id: 'park-1',
    name: 'Bellewaerde',
    countryCode: 'BE',
    latitude: 50.845,
    longitude: 2.945,
    isVisible: true,
    founderId: 'founder-1',
    operatorId: 'operator-1',
    currentLogoImageId: 'logo-1',
    descriptions: [
      { languageCode: 'en', value: '<p>Belgian park.</p>' }
    ]
  };
}

function createSummary(totalItems: number = 3): ParkDetailSummary {
  return {
    park: createPark(),
    mainImage: {
      id: 'main-image-1',
      category: ImageCategory.PARK,
      ownerType: ImageOwnerType.PARK,
      ownerId: 'park-1',
      path: 'parks/main.jpg',
      description: 'Main park image',
      isCurrent: true,
      isPublished: true,
      width: 1200,
      height: 800,
      sizeInBytes: 1000,
      originalFileName: 'main.jpg',
      contentType: 'image/jpeg',
      geoLocation: null,
      altTexts: [],
      captions: [],
      credits: [],
      tagIds: [],
      createdAt: '2026-01-01T00:00:00Z',
      updatedAt: '2026-01-01T00:00:00Z'
    },
    references: {
      founderName: 'Founder',
      operatorName: 'Operator'
    },
    stats: {
      totalItems,
      zoneCount: totalItems > 0 ? 1 : 0,
      attractionCount: Math.max(totalItems - 1, 0),
      restaurantCount: totalItems > 0 ? 1 : 0,
      showCount: 0,
      shopCount: 0,
      hotelCount: 0,
      countsByCategory: {
        Attraction: Math.max(totalItems - 1, 0),
        Restaurant: totalItems > 0 ? 1 : 0
      }
    }
  };
}

function configureFacade(): {
  facade: ParkDetailStateFacade;
  parksPort: FakeParksPort;
  ssrStatusService: FakeSsrHttpStatusService;
} {
  const parksPort: FakeParksPort = new FakeParksPort();
  const ssrStatusService: FakeSsrHttpStatusService = new FakeSsrHttpStatusService();

  TestBed.configureTestingModule({
    providers: [
      ParkDetailStateFacade,
      { provide: PARK_DETAIL_PARKS_PORT, useValue: parksPort },
      {
        provide: CountryDisplayService,
        useValue: {
          resolveLocalizedCountryName: (): string => 'Belgique'
        }
      },
      { provide: SsrHttpStatusService, useValue: ssrStatusService }
    ]
  });

  return {
    facade: TestBed.inject(ParkDetailStateFacade),
    parksPort,
    ssrStatusService
  };
}

describe('ParkDetailStateFacade', () => {
  beforeEach(() => {
    TestBed.resetTestingModule();
  });

  it('loads the optimized park detail summary only', () => {
    const context = configureFacade();
    context.parksPort.summaryResponse$ = of(createSummary(3));

    context.facade.setCurrentLanguage('fr');
    context.facade.loadPark('park-1');

    expect(context.facade.state().kind).toBe('ready');
    expect(context.facade.park()?.name).toBe('Bellewaerde');
    expect(context.facade.park()?.countryName).toBe('Belgique');
    expect(context.facade.park()?.heroImageId).toBe('main-image-1');
    expect(context.facade.park()?.founderName).toBe('Founder');
    expect(context.facade.park()?.operatorName).toBe('Operator');
    expect(context.facade.park()?.stats[0].value).toBe(3);
    expect(context.facade.summary()?.entries.find((entry) => entry.labelKey === 'parkVisitor.summary.totalItems')?.count).toBe(3);
    expect(context.parksPort.summaryCalls).toEqual(['park-1']);
  });

  it('sets the SSR 404 status and preserves the previous data when the summary is missing', () => {
    spyOn(console, 'error');
    const context = configureFacade();
    context.facade.loadPark('park-1');
    context.parksPort.summaryResponse$ = throwError(() => ({ status: 404 }));

    context.facade.loadPark('missing-park');

    expect(context.facade.state().kind).toBe('error');
    expect(context.facade.state().error).toBe('parks.detail.errorMessage');
    expect(context.facade.park()?.id).toBe('park-1');
    expect(context.ssrStatusService.notFoundCallCount).toBe(1);
  });
});
