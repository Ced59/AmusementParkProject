import { TestBed } from '@angular/core/testing';
import { Observable, of } from 'rxjs';

import {
  SharePublicationPreview,
  SharePublicationPreviewRequest,
  SharePublicationPublishRequest,
  SharePublicationSettings
} from '@app/models/sharing/share-publication.models';
import { ToastMessageService } from '@app/services/messages/toast-message.service';
import { TranslateService } from '@ngx-translate/core';
import { VISIT_RECAP_SHARE_PORT, VisitRecapSharePort } from './visit-recap-share-state-data.ports';
import { VisitRecapShareStateFacade } from './visit-recap-share-state.facade';

describe('VisitRecapShareStateFacade', () => {
  let facade: VisitRecapShareStateFacade;
  let previewRequests: SharePublicationPreviewRequest[];
  let publishRequests: SharePublicationPublishRequest[];
  let settings: SharePublicationSettings;

  beforeEach(() => {
    previewRequests = [];
    publishRequests = [];
    settings = { isPublic: false, includedFields: [] };
    const port: VisitRecapSharePort = {
      getSettings: (_visitId: string): Observable<SharePublicationSettings> => of(settings),
      preview: (request: SharePublicationPreviewRequest): Observable<SharePublicationPreview> => {
        previewRequests.push(request);
        return of(createPreview(request));
      },
      publish: (request: SharePublicationPublishRequest): Observable<SharePublicationSettings> => {
        publishRequests.push(request);
        return of({
          isPublic: true,
          shareId: 'opaque-share-id',
          publishedAtUtc: '2026-09-07T08:00:00Z',
          policySchemaVersion: 1,
          datePrecision: request.approvedDatePrecision,
          includedFields: request.approvedIncludedFields
        });
      },
      revoke: (_visitId: string): Observable<SharePublicationSettings> => of({ isPublic: false, includedFields: [] })
    };
    TestBed.configureTestingModule({
      providers: [
        VisitRecapShareStateFacade,
        { provide: VISIT_RECAP_SHARE_PORT, useValue: port },
        { provide: ToastMessageService, useValue: { add: vi.fn() } },
        { provide: TranslateService, useValue: { instant: (key: string): string => key } }
      ]
    });
    facade = TestBed.inject(VisitRecapShareStateFacade);
  });

  it('requires a new exact preview after every privacy or item-selection change', () => {
    facade.load('visit-1');
    facade.openEditor('Day');
    facade.setDatePrecision('Month');
    facade.setRatingsIncluded(true);
    facade.setCaptionIncluded(true);
    facade.setPublicCaption('  A public memory only  ');

    facade.preparePreview('visit-1');

    expect(facade.canPublish()).toBe(true);
    expect(previewRequests[0]).toEqual({
      publicationType: 'VisitRecap',
      sourceId: 'visit-1',
      datePrecision: 'Month',
      includedFields: ['RideCount', 'TemporalRatings', 'PublicCaption'],
      visitRecap: {
        selectedParkItemIds: null,
        publicCaption: 'A public memory only'
      }
    });

    facade.toggleItem('item-a', false);

    expect(facade.preview()).toBeNull();
    expect(facade.canPublish()).toBe(false);
    expect(facade.isItemSelected('item-a')).toBe(false);
    facade.preparePreview('visit-1');
    expect(previewRequests[1].visitRecap?.selectedParkItemIds).toEqual([]);
    expect(facade.candidateItems().map(item => item.parkItemId)).toEqual(['item-a']);

    facade.publish('visit-1');

    expect(publishRequests).toHaveLength(1);
    expect(publishRequests[0].approvalToken).toBe('approved-preview');
    expect(publishRequests[0].visitRecap).toEqual({
      selectedParkItemIds: [],
      publicCaption: 'A public memory only'
    });
    expect(facade.settings()?.shareId).toBe('opaque-share-id');
  });
});

function createPreview(request: SharePublicationPreviewRequest): SharePublicationPreview {
  const selectedIds: string[] | null | undefined = request.visitRecap?.selectedParkItemIds;
  const containsItem: boolean = selectedIds === null || selectedIds === undefined || selectedIds.includes('item-a');
  return {
    publicationType: 'VisitRecap',
    sourceVersion: 12,
    approvalToken: 'approved-preview',
    contentPolicy: {
      schemaVersion: 1,
      datePrecision: request.datePrecision,
      includedFields: request.includedFields
    },
    visitRecap: {
      parkId: 'park-1',
      parkName: 'Denain Évasion',
      date: { year: 2026, month: 7, precision: 'Month', isApproximate: false },
      distinctItemCount: 1,
      totalRideCount: 1,
      categories: ['Attraction'],
      items: containsItem
        ? [{
          parkItemId: 'item-a',
          name: 'Le Galion',
          category: 'Attraction',
          rideCount: 1,
          averageRating: 4.5,
          isMissed: false
        }]
        : [],
      publicCaption: request.visitRecap?.publicCaption,
      hasHiddenDate: false,
      hasIncompleteRatings: false,
      hasIncompleteItems: false
    }
  };
}
