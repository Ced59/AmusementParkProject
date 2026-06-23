import { TestBed } from '@angular/core/testing';
import { of } from 'rxjs';

import { ImageCategory } from '@app/models/images/image-category';
import { ImageOwnerType } from '@app/models/images/image-owner-type';
import { AttractionManufacturer } from '@app/models/parks/attraction-manufacturer';
import { ParkItemAdminRow } from '@app/models/parks/park-item-admin-row';
import { ApiResponse } from '@app/models/shared/api_reponse';
import { SsrHttpStatusService } from '@core/ssr/ssr-http-status.service';
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
  ParkReferenceDetailStateParkOperatorsApiServicePort
} from './park-reference-detail-state-data.ports';
import { ParkReferenceDetailStateFacade } from './park-reference-detail-state.facade';

describe('ParkReferenceDetailStateFacade', () => {
  let imagesApiService: jasmine.SpyObj<ParkReferenceDetailStateImagesApiServicePort>;
  let manufacturersApiService: jasmine.SpyObj<ParkReferenceDetailStateManufacturersApiServicePort>;
  let parkItemsApiService: jasmine.SpyObj<ParkReferenceDetailStateParkItemsApiServicePort>;
  let facade: ParkReferenceDetailStateFacade;

  beforeEach(() => {
    imagesApiService = jasmine.createSpyObj<ParkReferenceDetailStateImagesApiServicePort>('ImagesApiService', ['getImages']);
    manufacturersApiService = jasmine.createSpyObj<ParkReferenceDetailStateManufacturersApiServicePort>('ManufacturersApiService', ['getAttractionManufacturerById']);
    parkItemsApiService = jasmine.createSpyObj<ParkReferenceDetailStateParkItemsApiServicePort>('ParkItemsApiService', ['getParkItemsPaginated']);
    const parkFoundersApiService: jasmine.SpyObj<ParkReferenceDetailStateParkFoundersApiServicePort> = jasmine.createSpyObj<ParkReferenceDetailStateParkFoundersApiServicePort>('ParkFoundersApiService', ['getParkFounderById']);
    const parkOperatorsApiService: jasmine.SpyObj<ParkReferenceDetailStateParkOperatorsApiServicePort> = jasmine.createSpyObj<ParkReferenceDetailStateParkOperatorsApiServicePort>('ParkOperatorsApiService', ['getParkOperatorById']);
    const ssrHttpStatusService: jasmine.SpyObj<SsrHttpStatusService> = jasmine.createSpyObj<SsrHttpStatusService>('SsrHttpStatusService', ['setNotFound', 'setStatus']);

    imagesApiService.getImages.and.returnValue(of([]));
    manufacturersApiService.getAttractionManufacturerById.and.returnValue(of(buildManufacturer()));
    parkItemsApiService.getParkItemsPaginated.and.returnValue(of(buildAttractionsResponse([buildAttraction('ride-1', 'Ride 1')], 1, 12, 18)));

    TestBed.configureTestingModule({
      providers: [
        ParkReferenceDetailStateFacade,
        { provide: PARK_REFERENCE_DETAIL_STATE_IMAGES_API_SERVICE_PORT, useValue: imagesApiService },
        { provide: PARK_REFERENCE_DETAIL_STATE_MANUFACTURERS_API_SERVICE_PORT, useValue: manufacturersApiService },
        { provide: PARK_REFERENCE_DETAIL_STATE_PARK_ITEMS_API_SERVICE_PORT, useValue: parkItemsApiService },
        { provide: PARK_REFERENCE_DETAIL_STATE_PARK_FOUNDERS_API_SERVICE_PORT, useValue: parkFoundersApiService },
        { provide: PARK_REFERENCE_DETAIL_STATE_PARK_OPERATORS_API_SERVICE_PORT, useValue: parkOperatorsApiService },
        { provide: SsrHttpStatusService, useValue: ssrHttpStatusService }
      ]
    });

    facade = TestBed.inject(ParkReferenceDetailStateFacade);
  });

  it('loads manufacturer attractions as a visible paged list', () => {
    facade.loadReference('manufacturer', 'manufacturer-1');

    expect(manufacturersApiService.getAttractionManufacturerById).toHaveBeenCalledWith('manufacturer-1');
    expect(imagesApiService.getImages).toHaveBeenCalledWith(ImageOwnerType.ATTRACTION_MANUFACTURER, 'manufacturer-1', ImageCategory.MANUFACTURER, 1, 100, jasmine.any(Object));
    expect(parkItemsApiService.getParkItemsPaginated).toHaveBeenCalledWith(1, 12, null, null, {
      manufacturerId: 'manufacturer-1',
      isVisible: true,
      category: 'Attraction'
    }, null, jasmine.any(Object));
    expect(facade.reference()?.heroLogoImageId).toBe('logo-1');
    expect(facade.reference()?.attractions.map((attraction) => attraction.name)).toEqual(['Ride 1']);
    expect(facade.reference()?.attractionsPagination?.totalItems).toBe(18);
  });

  it('loads another manufacturer attractions page without reloading the reference identity', () => {
    facade.loadReference('manufacturer', 'manufacturer-1');
    manufacturersApiService.getAttractionManufacturerById.calls.reset();
    imagesApiService.getImages.calls.reset();
    parkItemsApiService.getParkItemsPaginated.calls.reset();
    parkItemsApiService.getParkItemsPaginated.and.returnValue(of(buildAttractionsResponse([buildAttraction('ride-2', 'Ride 2')], 2, 6, 18)));

    facade.loadManufacturerAttractionsPage(2, 6);

    expect(manufacturersApiService.getAttractionManufacturerById).not.toHaveBeenCalled();
    expect(imagesApiService.getImages).not.toHaveBeenCalled();
    expect(parkItemsApiService.getParkItemsPaginated).toHaveBeenCalledWith(2, 6, null, null, {
      manufacturerId: 'manufacturer-1',
      isVisible: true,
      category: 'Attraction'
    }, null, jasmine.any(Object));
    expect(facade.reference()?.attractions.map((attraction) => attraction.name)).toEqual(['Ride 2']);
    expect(facade.reference()?.attractionsPagination?.currentPage).toBe(2);
  });
});

function buildManufacturer(): AttractionManufacturer {
  return {
    id: 'manufacturer-1',
    name: 'Mack Rides',
    biography: [],
    currentLogoImageId: ' logo-1 '
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
    adminReviewStatus: 'Validated'
  };
}

function buildAttractionsResponse(items: ParkItemAdminRow[], currentPage: number, itemsPerPage: number, totalItems: number): ApiResponse<ParkItemAdminRow> {
  return {
    data: items,
    pagination: {
      currentPage,
      itemsPerPage,
      totalItems,
      totalPages: Math.ceil(totalItems / itemsPerPage)
    }
  };
}
