import { HttpTestingController } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';

import { AttractionManufacturer } from '@app/models/parks/attraction-manufacturer';
import { provideCommonTestDependencies } from '@app/testing/common-test-providers';
import { environment } from '../../../environments/environment';
import { ManufacturersApiService } from './manufacturers-api.service';

describe('ManufacturersApiService', () => {
  let service: ManufacturersApiService;
  let httpTestingController: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: provideCommonTestDependencies() });
    service = TestBed.inject(ManufacturersApiService);
    httpTestingController = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpTestingController.verify();
  });

  it('does not request hidden manufacturers for public pages by default', () => {
    service.getAttractionManufacturersPage(2, 12, ' ride ').subscribe();

    const request = httpTestingController.expectOne((candidate) =>
      candidate.url === `${environment.apiBaseUrl}attraction-manufacturers`
        && candidate.params.get('page') === '2'
        && candidate.params.get('size') === '12'
        && candidate.params.get('search') === 'ride'
        && !candidate.params.has('includeHidden'));

    request.flush({
      data: [],
      pagination: { currentPage: 2, itemsPerPage: 12, totalItems: 0, totalPages: 1 }
    });
  });

  it('requests hidden manufacturers when admin calls includeHidden', () => {
    const results: AttractionManufacturer[] = [];
    service.getAttractionManufacturerById('manufacturer-1', true).subscribe((manufacturer: AttractionManufacturer) => {
      results.push(manufacturer);
    });

    const request = httpTestingController.expectOne((candidate) =>
      candidate.url === `${environment.apiBaseUrl}attraction-manufacturers/manufacturer-1`
        && candidate.params.get('includeHidden') === 'true');

    request.flush({ id: 'manufacturer-1', name: 'Mack Rides', isVisible: false });

    expect(results).toEqual([{ id: 'manufacturer-1', name: 'Mack Rides', isVisible: false } as AttractionManufacturer]);
  });
});
