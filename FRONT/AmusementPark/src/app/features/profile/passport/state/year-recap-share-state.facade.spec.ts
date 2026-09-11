import { TestBed } from '@angular/core/testing';
import { Observable, Subject, of } from 'rxjs';

import {
  SharePublicationPreview,
  SharePublicationPreviewRequest,
  SharePublicationPublishRequest,
  SharePublicationSettings,
  YearRecapShareSelection
} from '@app/models/sharing/share-publication.models';
import { ToastMessageService } from '@app/services/messages/toast-message.service';
import { TranslateService } from '@ngx-translate/core';
import { YEAR_RECAP_SHARE_PORT, YearRecapSharePort } from './year-recap-share-state-data.ports';
import { YearRecapShareStateFacade } from './year-recap-share-state.facade';

describe('YearRecapShareStateFacade', () => {
  let facade: YearRecapShareStateFacade;
  let settings: SharePublicationSettings;
  let selection: YearRecapShareSelection;
  let previewRequests: SharePublicationPreviewRequest[];
  let publishRequests: SharePublicationPublishRequest[];
  let publishResponse: Observable<SharePublicationSettings>;
  let previewIsEmpty: boolean;

  beforeEach(() => {
    settings = { isPublic: false, includedFields: [] };
    selection = { hasSavedSnapshot: false };
    previewRequests = [];
    publishRequests = [];
    previewIsEmpty = false;
    publishResponse = of({
      isPublic: true,
      shareId: 'opaque-share-id',
      policySchemaVersion: 1,
      datePrecision: 'Year',
      includedFields: ['RideCount', 'GeographicStatistics']
    });
    const port: YearRecapSharePort = {
      getSettings: (_year: number): Observable<SharePublicationSettings> => of(settings),
      getSelection: (_year: number): Observable<YearRecapShareSelection> => of(selection),
      preview: (request: SharePublicationPreviewRequest): Observable<SharePublicationPreview> => {
        previewRequests.push(request);
        return of(createPreview(request, previewIsEmpty));
      },
      publish: (request: SharePublicationPublishRequest): Observable<SharePublicationSettings> => {
        publishRequests.push(request);
        return publishResponse;
      },
      revoke: (_year: number): Observable<SharePublicationSettings> => of({ isPublic: false, includedFields: [] })
    };
    TestBed.configureTestingModule({
      providers: [
        YearRecapShareStateFacade,
        { provide: YEAR_RECAP_SHARE_PORT, useValue: port },
        { provide: ToastMessageService, useValue: { add: vi.fn() } },
        { provide: TranslateService, useValue: { instant: (key: string): string => key } }
      ]
    });
    facade = TestBed.inject(YearRecapShareStateFacade);
  });

  it('restores the approved policy and public caption when editing an existing share', () => {
    settings = {
      isPublic: false,
      policySchemaVersion: 1,
      datePrecision: 'Year',
      includedFields: ['TemporalRatings', 'PublicCaption']
    };
    selection = {
      savedPublicCaption: 'Souvenir déjà publié',
      hasSavedSnapshot: true
    };

    facade.load(2026);
    facade.openEditor();

    expect(facade.includeRideCount()).toBe(false);
    expect(facade.includeGeography()).toBe(false);
    expect(facade.includeRatings()).toBe(true);
    expect(facade.includeCaption()).toBe(true);
    expect(facade.publicCaption()).toBe('Souvenir déjà publié');
  });

  it('requires an exact non-empty preview and freezes its choices while publishing', () => {
    const pending: Subject<SharePublicationSettings> = new Subject<SharePublicationSettings>();
    publishResponse = pending.asObservable();
    facade.load(2026);
    facade.openEditor();
    facade.setRatingsIncluded(true);
    facade.setCaptionIncluded(true);
    facade.setPublicCaption('  Une année mémorable  ');

    facade.preparePreview(2026);

    expect(facade.canPublish()).toBe(true);
    expect(previewRequests[0]).toEqual({
      publicationType: 'YearRecap',
      sourceId: '2026',
      datePrecision: 'Year',
      includedFields: ['RideCount', 'TemporalRatings', 'GeographicStatistics', 'PublicCaption'],
      yearRecap: { publicCaption: 'Une année mémorable' }
    });

    facade.publish(2026);
    facade.setPublicCaption('Texte modifié trop tard');

    expect(facade.saving()).toBe(true);
    expect(facade.publicCaption()).toBe('  Une année mémorable  ');
    expect(publishRequests[0].approvalToken).toBe('approved-preview');
    expect(publishRequests[0].yearRecap?.publicCaption).toBe('Une année mémorable');
  });

  it('does not allow publication when the selected year has no completed visit', () => {
    previewIsEmpty = true;
    facade.load(2026);
    facade.openEditor();
    facade.preparePreview(2026);

    expect(facade.yearRecap()?.isEmpty).toBe(true);
    expect(facade.canPublish()).toBe(false);
  });
});

function createPreview(
  request: SharePublicationPreviewRequest,
  isEmpty: boolean = false
): SharePublicationPreview {
  return {
    publicationType: 'YearRecap',
    sourceVersion: 12,
    approvalToken: 'approved-preview',
    contentPolicy: {
      schemaVersion: 1,
      datePrecision: request.datePrecision,
      includedFields: request.includedFields
    },
    yearRecap: {
      year: 2026,
      parkCount: isEmpty ? 0 : 1,
      visitCount: isEmpty ? 0 : 2,
      approximateVisitCount: 0,
      approximateVisitRate: 0,
      totalRideCount: isEmpty ? 0 : 4,
      distinctItemCount: isEmpty ? 0 : 2,
      categories: isEmpty ? [] : ['Attraction'],
      mostVisitedParks: [],
      nowClosedItems: [],
      publicCaption: request.yearRecap?.publicCaption,
      hasIncompleteCatalog: false,
      calculationVersion: 'passport-year-recap-v1',
      isEmpty
    }
  };
}
