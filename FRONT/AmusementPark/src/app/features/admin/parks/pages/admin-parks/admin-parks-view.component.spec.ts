import { signal } from '@angular/core';
import { By } from '@angular/platform-browser';
import { ComponentFixture, TestBed } from '@angular/core/testing';

import { COMMON_TEST_IMPORTS, provideCommonTestDependencies } from '@app/testing/common-test-providers';
import { Park } from '@app/models/parks/park';
import { AdminReviewStatus } from '@app/models/admin/admin-review-status';
import { ParkAudienceClassificationFilter } from '@app/models/parks/park-audience-classification';
import { ParkOpeningHoursAdminFilter } from '@app/models/parks/park-opening-hours';
import { ParkType } from '@app/models/parks/park-type';
import { ParkAdminListSortDirection, ParkAdminListSortField } from '@data-access/parks/parks-api-endpoints';
import { Table } from '@shared/ui/primitives/table';
import { AdminParksViewComponent } from './admin-parks-view.component';

interface AdminParksViewTestApi {
  onMobileSortChanged(value: string): void;
  getSortValue(): string;
}

describe('AdminParksViewComponent', () => {
  let component: AdminParksViewComponent;
  let fixture: ComponentFixture<AdminParksViewComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [...COMMON_TEST_IMPORTS, AdminParksViewComponent],
      providers: provideCommonTestDependencies(),
    }).compileComponents();

    fixture = TestBed.createComponent(AdminParksViewComponent);
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
      parkLongitude: '2.3',
      parkDataCompletenessScore: '',
      parkDataQualityLevel: '',
      parkDataCompletenessEarnedPoints: '',
      parkDataCompletenessMaxPoints: ''
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

    testApi.onMobileSortChanged('dataCompletenessScore:asc');

    expect(received[1]).toEqual({ sortField: 'dataCompletenessScore', sortDirection: 'asc' });
  });

  it('binds the table paginator offset to the current page', () => {
    component.parks = signal<Park[]>([]);
    component.loading = signal<boolean>(false);
    component.totalRecords = signal<number>(30);
    component.pageSize = signal<number>(10);
    component.currentPage = signal<number>(3);
    component.searchQuery = signal<string>('');
    component.visibilityFilter = signal<boolean | null>(null);
    component.adminReviewStatusFilter = signal<AdminReviewStatus | null>(null);
    component.typeFilter = signal<ParkType | null>(null);
    component.audienceClassificationFilter = signal<ParkAudienceClassificationFilter | null>(null);
    component.countryCodeFilter = signal<string>('');
    component.validCoordinatesFilter = signal<boolean | null>(null);
    component.openingHoursFilter = signal<ParkOpeningHoursAdminFilter>('all');
    component.sortField = signal<ParkAdminListSortField>('default');
    component.sortDirection = signal<ParkAdminListSortDirection>('asc');
    component.sortOrder = signal<1 | -1>(1);
    component.selectedParkIds = signal<string[]>([]);
    component.selectedCount = signal<number>(0);
    component.canShowHeaderTotal = signal<boolean>(true);
    component.canClearSearch = signal<boolean>(false);

    fixture.detectChanges();

    const table: Table = fixture.debugElement.query(By.directive(Table)).componentInstance as Table;

    expect(table.first).toBe(20);
  });
});
