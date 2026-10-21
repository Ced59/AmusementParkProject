import type { MockedObject } from 'vitest';
import { TestBed } from '@angular/core/testing';
import { of } from 'rxjs';

import { ImageCategory } from '@app/models/images/image-category';
import { ImageOwnerType } from '@app/models/images/image-owner-type';
import { AttractionManufacturer } from '@app/models/parks/attraction-manufacturer';
import { ParkItemAdminRow } from '@app/models/parks/park-item-admin-row';
import { ApiResponse } from '@app/models/shared/api_reponse';
import { SsrHttpStatusService } from '@core/ssr/ssr-http-status.service';
import { CountryDisplayService } from '@shared/services/countries/country-display.service';
import { ParkReferenceKind } from '../models/park-reference-detail-view.model';
import {
  PARK_REFERENCE_DETAIL_STATE_IMAGES_API_SERVICE_PORT,
  ParkReferenceDetailStateImagesApiServicePort,
  PARK_REFERENCE_DETAIL_STATE_MANUFACTURERS_API_SERVICE_PORT,
  ParkReferenceDetailStateManufacturersApiServicePort,
  PARK_REFERENCE_DETAIL_STATE_PARK_FOUNDERS_API_SERVICE_PORT,
  ParkReferenceDetailStateParkFoundersApiServicePort,
  PARK_REFERENCE_DETAIL_STATE_PARK_ITEMS_API_SERVICE_PORT,
  ParkReferenceDetailStateParkItemsApiServicePort,
  PARK_REFERENCE_DETAIL_STATE_PARK_OPERATORS_API_SERVICE_PORT,
  ParkReferenceDetailStateParkOperatorsApiServicePort,
} from './park-reference-detail-state-data.ports';
import { ParkReferenceDetailStateFacade } from './park-reference-detail-state.facade';

describe('ParkReferenceDetailStateFacade', () => {
  let imagesApiService: MockedObject<ParkReferenceDetailStateImagesApiServicePort>;
  let manufacturersApiService: MockedObject<ParkReferenceDetailStateManufacturersApiServicePort>;
  let parkItemsApiService: MockedObject<ParkReferenceDetailStateParkItemsApiServicePort>;
  let parkFoundersApiService: MockedObject<ParkReferenceDetailStateParkFoundersApiServicePort>;
  let parkOperatorsApiService: MockedObject<ParkReferenceDetailStateParkOperatorsApiServicePort>;
  let facade: ParkReferenceDetailStateFacade;

  beforeEach(() => {
    imagesApiService = {
      getImages: vi.fn().mockName('ImagesApiService.getImages'),
    } as unknown as MockedObject<ParkReferenceDetailStateImagesApiServicePort>;
    manufacturersApiService = {
      getAttractionManufacturerById: vi
        .fn()
        .mockName('ManufacturersApiService.getAttractionManufacturerById'),
    } as unknown as MockedObject<ParkReferenceDetailStateManufacturersApiServicePort>;
    parkItemsApiService = {
      getParkItemsPaginated: vi
        .fn()
        .mockName('ParkItemsApiService.getParkItemsPaginated'),
    } as unknown as MockedObject<ParkReferenceDetailStateParkItemsApiServicePort>;
    parkFoundersApiService =
      {
        getParkFounderById: vi
          .fn()
          .mockName('ParkFoundersApiService.getParkFounderById'),
      } as unknown as MockedObject<ParkReferenceDetailStateParkFoundersApiServicePort>;
    parkOperatorsApiService =
      {
        getParkOperatorById: vi
          .fn()
          .mockName('ParkOperatorsApiService.getParkOperatorById'),
      } as unknown as MockedObject<ParkReferenceDetailStateParkOperatorsApiServicePort>;
    const ssrHttpStatusService: MockedObject<SsrHttpStatusService> = {
      setNotFound: vi.fn().mockName('SsrHttpStatusService.setNotFound'),
      setStatus: vi.fn().mockName('SsrHttpStatusService.setStatus'),
    } as unknown as MockedObject<SsrHttpStatusService>;

    imagesApiService.getImages.mockReturnValue(of([]));
    manufacturersApiService.getAttractionManufacturerById.mockReturnValue(
      of(buildManufacturer()),
    );
    parkItemsApiService.getParkItemsPaginated.mockReturnValue(
      of(
        buildAttractionsResponse(
          [buildAttraction('ride-1', 'Ride 1')],
          1,
          12,
          18,
        ),
      ),
    );

    TestBed.configureTestingModule({
      providers: [
        ParkReferenceDetailStateFacade,
        {
          provide: PARK_REFERENCE_DETAIL_STATE_IMAGES_API_SERVICE_PORT,
          useValue: imagesApiService,
        },
        {
          provide: PARK_REFERENCE_DETAIL_STATE_MANUFACTURERS_API_SERVICE_PORT,
          useValue: manufacturersApiService,
        },
        {
          provide: PARK_REFERENCE_DETAIL_STATE_PARK_ITEMS_API_SERVICE_PORT,
          useValue: parkItemsApiService,
        },
        {
          provide: PARK_REFERENCE_DETAIL_STATE_PARK_FOUNDERS_API_SERVICE_PORT,
          useValue: parkFoundersApiService,
        },
        {
          provide: PARK_REFERENCE_DETAIL_STATE_PARK_OPERATORS_API_SERVICE_PORT,
          useValue: parkOperatorsApiService,
        },
        { provide: SsrHttpStatusService, useValue: ssrHttpStatusService },
      ],
    });

    facade = TestBed.inject(ParkReferenceDetailStateFacade);
  });

  it('loads manufacturer attractions as a visible paged list', () => {
    facade.loadReference('manufacturer', 'manufacturer-1');

    expect(
      manufacturersApiService.getAttractionManufacturerById,
    ).toHaveBeenCalledWith('manufacturer-1');
    expect(imagesApiService.getImages).toHaveBeenCalledWith(
      ImageOwnerType.ATTRACTION_MANUFACTURER,
      'manufacturer-1',
      ImageCategory.MANUFACTURER,
      1,
      100,
      expect.any(Object),
    );
    expect(parkItemsApiService.getParkItemsPaginated).toHaveBeenCalledWith(
      1,
      12,
      null,
      null,
      {
        manufacturerId: 'manufacturer-1',
        isVisible: true,
        category: 'Attraction',
      },
      null,
      expect.any(Object),
    );
    expect(facade.reference()?.heroLogoImageId).toBe('logo-1');
    expect(
      facade.reference()?.attractions.map((attraction) => attraction.name),
    ).toEqual(['Ride 1']);
    expect(facade.reference()?.attractionsPagination?.totalItems).toBe(18);
  });

  it('loads another manufacturer attractions page without reloading the reference identity', () => {
    facade.loadReference('manufacturer', 'manufacturer-1');
    manufacturersApiService.getAttractionManufacturerById.mockClear();
    imagesApiService.getImages.mockClear();
    parkItemsApiService.getParkItemsPaginated.mockClear();
    parkItemsApiService.getParkItemsPaginated.mockReturnValue(
      of(
        buildAttractionsResponse(
          [buildAttraction('ride-2', 'Ride 2')],
          2,
          6,
          18,
        ),
      ),
    );

    facade.loadManufacturerAttractionsPage(2, 6);

    expect(
      manufacturersApiService.getAttractionManufacturerById,
    ).not.toHaveBeenCalled();
    expect(imagesApiService.getImages).not.toHaveBeenCalled();
    expect(parkItemsApiService.getParkItemsPaginated).toHaveBeenCalledWith(
      2,
      6,
      null,
      null,
      {
        manufacturerId: 'manufacturer-1',
        isVisible: true,
        category: 'Attraction',
      },
      null,
      expect.any(Object),
    );
    expect(
      facade.reference()?.attractions.map((attraction) => attraction.name),
    ).toEqual(['Ride 2']);
    expect(facade.reference()?.attractionsPagination?.currentPage).toBe(2);
  });

  for (const kind of ['founder', 'operator', 'manufacturer'] as const) {
    it(`localizes the ${kind} country in all supported languages without reloading`, () => {
      loadCountryReference(kind, 'IT');
      const expectedNames: Record<string, string> = {
        fr: 'Italie', en: 'Italy', de: 'Italien', nl: 'Italië',
        it: 'Italia', es: 'Italia', pt: 'Itália', pl: 'Włochy',
      };

      for (const [language, name] of Object.entries(expectedNames)) {
        facade.setCurrentLanguage(language);
        expect(countryFact(kind)).toMatchObject({ value: name, isMonospace: false });
      }

      const apiMethod = kind === 'founder' ? parkFoundersApiService.getParkFounderById
        : kind === 'operator' ? parkOperatorsApiService.getParkOperatorById
          : manufacturersApiService.getAttractionManufacturerById;
      expect(apiMethod).toHaveBeenCalledTimes(1);
    });

    it.each([null, '', 'not-a-country', 'QX'])(`omits an unresolved ${kind} country: %s`, (code) => {
      loadCountryReference(kind, code);
      expect(countryFact(kind)).toBeUndefined();
    });

    it(`never displays the ${kind} raw code when country names are unavailable`, () => {
      vi.spyOn(TestBed.inject(CountryDisplayService), 'resolveLocalizedCountryName').mockReturnValue('PT');
      loadCountryReference(kind, ' pt ');
      expect(countryFact(kind)).toBeUndefined();
    });
  }

  function loadCountryReference(kind: ParkReferenceKind, countryCode: string | null): void {
    parkFoundersApiService.getParkFounderById.mockReturnValue(of({
      id: 'reference-1', name: 'Founder', nationalityCountryCode: countryCode,
    }));
    parkOperatorsApiService.getParkOperatorById.mockReturnValue(of({
      id: 'reference-1', name: 'Operator', contactDetails: { countryCode },
    }));
    manufacturersApiService.getAttractionManufacturerById.mockReturnValue(of({
      ...buildManufacturer(), contactDetails: { countryCode },
    }));
    facade.loadReference(kind, 'reference-1');
  }

  function countryFact(kind: ParkReferenceKind) {
    const labelKey = kind === 'founder' ? 'parks.reference.facts.nationality' : 'parks.reference.facts.country';
    return facade.reference()?.facts.find((fact) => fact.labelKey === labelKey);
  }
});

function buildManufacturer(): AttractionManufacturer {
  return {
    id: 'manufacturer-1',
    name: 'Mack Rides',
    biography: [],
    currentLogoImageId: ' logo-1 ',
  };
}

function buildAttraction(id: string, name: string): ParkItemAdminRow {
  return {
    id,
    parkId: 'park-1',
    parkName: 'Test Park',
    name,
    category: 'Attraction',
    type: 'RollerCoaster',
    isVisible: true,
    adminReviewStatus: 'Validated',
  };
}

function buildAttractionsResponse(
  items: ParkItemAdminRow[],
  currentPage: number,
  itemsPerPage: number,
  totalItems: number,
): ApiResponse<ParkItemAdminRow> {
  return {
    data: items,
    pagination: {
      currentPage,
      itemsPerPage,
      totalItems,
      totalPages: Math.ceil(totalItems / itemsPerPage),
    },
  };
}
