import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';

import { COMMON_TEST_IMPORTS, provideCommonTestDependencies } from '@app/testing/common-test-providers';
import { Park } from '@app/models/parks/park';
import { ParkAdminListSortDirection, ParkAdminListSortField } from '@data-access/parks/parks-api-endpoints';
import { AdminParksViewComponent } from './admin-parks-view.component';

interface AdminParksViewTestApi {
  onMobileSortChanged(value: string): void;
  getSortValue(): string;
}

describe('AdminParksViewComponent', () => {
  let component: AdminParksViewComponent;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [...COMMON_TEST_IMPORTS, AdminParksViewComponent],
      providers: provideCommonTestDependencies(),
    }).compileComponents();

    const fixture = TestBed.createComponent(AdminParksViewComponent);
    component = fixture.componentInstance;
  });

  it('builds the park JSON export shortcut query params from the row data', () => {
    const park: Park = {
      id: 'park-1',
      name: 'Shortcut Park',
      countryCode: 'FR',
      city: 'Paris',
      latitude: 48.5,
      longitude: 2.3
    };

    expect(component.getParkGraphExportQueryParams(park)).toEqual({
      parkId: 'park-1',
      parkName: 'Shortcut Park',
      parkCountryCode: 'FR',
      parkCity: 'Paris',
      parkLatitude: '48.5',
      parkLongitude: '2.3'
    });
  });

  it('emits the selected mobile sort option', () => {
    const received: Array<{ sortField: ParkAdminListSortField; sortDirection: ParkAdminListSortDirection }> = [];
    component.sortField = signal<ParkAdminListSortField>('parkItemsTotalCount');
    component.sortDirection = signal<ParkAdminListSortDirection>('desc');
    component.sortChanged.subscribe((event: { sortField: ParkAdminListSortField; sortDirection: ParkAdminListSortDirection }) => {
      received.push(event);
    });

    const testApi: AdminParksViewTestApi = component as unknown as AdminParksViewTestApi;

    expect(testApi.getSortValue()).toBe('parkItemsTotalCount:desc');

    testApi.onMobileSortChanged('parkItemsVisibleCount:desc');

    expect(received).toEqual([{ sortField: 'parkItemsVisibleCount', sortDirection: 'desc' }]);
  });
});
