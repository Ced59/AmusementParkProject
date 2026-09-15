import { HttpTestingController } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';

import { UserCollectionEntry } from '@app/models/watchlists/user-collection-entry.model';
import { provideCommonTestDependencies } from '@app/testing/common-test-providers';
import { environment } from '../../../environments/environment';
import { UserCollectionsApiService } from './user-collections-api.service';

describe('UserCollectionsApiService', () => {
  let service: UserCollectionsApiService;
  let httpTestingController: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: provideCommonTestDependencies() });
    service = TestBed.inject(UserCollectionsApiService);
    httpTestingController = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpTestingController.verify());

  it('uses owner-scoped filtered and idempotent endpoints', () => {
    const entry: UserCollectionEntry = buildEntry();

    service.listMine('Park', 'park/1').subscribe();
    const list = httpTestingController.expectOne((request) =>
      request.url === `${environment.apiBaseUrl}me/collections`
      && request.params.get('targetType') === 'Park'
      && request.params.get('targetId') === 'park/1'
    );
    expect(list.request.method).toBe('GET');
    list.flush([entry]);

    service.add('Park', 'park/1', 'Favorite').subscribe();
    const addition = httpTestingController.expectOne(
      `${environment.apiBaseUrl}me/collections/Park/park%2F1/Favorite`
    );
    expect(addition.request.method).toBe('PUT');
    addition.flush(entry);

    service.delete('Park', 'park/1', 'Favorite').subscribe();
    const deletion = httpTestingController.expectOne(
      `${environment.apiBaseUrl}me/collections/Park/park%2F1/Favorite`
    );
    expect(deletion.request.method).toBe('DELETE');
    deletion.flush(null);
  });
});

function buildEntry(): UserCollectionEntry {
  return {
    entryId: 'entry-1',
    targetType: 'Park',
    targetId: 'park/1',
    kind: 'Favorite',
    targetStatus: 'Available',
    targetName: 'Europa-Park',
    parentParkId: null,
    parentParkName: null,
    mainImageId: 'image-1',
    privateNote: null,
    priority: null,
    preferredStartsOn: null,
    preferredEndsOn: null,
    createdAtUtc: '2026-09-15T10:00:00Z',
    updatedAtUtc: '2026-09-15T10:00:00Z',
    version: 1
  };
}
