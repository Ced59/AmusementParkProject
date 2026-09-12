import { TestBed } from '@angular/core/testing';
import { Observable, Subject, of } from 'rxjs';

import {
  PassportProfileShareSelection,
  SharePublicationPreview,
  SharePublicationPreviewRequest,
  SharePublicationPublishRequest,
  SharePublicationSettings
} from '@app/models/sharing/share-publication.models';
import { ToastMessageService } from '@app/services/messages/toast-message.service';
import { TranslateService } from '@ngx-translate/core';
import { PASSPORT_PROFILE_SHARE_PORT, PassportProfileSharePort } from './passport-profile-share-state-data.ports';
import { PassportProfileShareStateFacade } from './passport-profile-share-state.facade';

describe('PassportProfileShareStateFacade', () => {
  it('restores the private selection and publishes only the exact preview', () => {
    const previewRequests: SharePublicationPreviewRequest[] = [];
    const publishRequests: SharePublicationPublishRequest[] = [];
    const settings: SharePublicationSettings = { isPublic: false, includedFields: [] };
    const selection: PassportProfileShareSelection = {
      years: [{ year: 2026, visitCount: 2 }, { year: 2025, visitCount: 1 }],
      parks: [{ parkId: 'park-1', name: 'Denain Évasion', countryCode: 'FR', visitCount: 2 }],
      ratings: [{ selectionKey: 'rating-1', parkId: 'park-1', name: 'Le Galion', parkName: 'Denain Évasion', rating: 4.5 }],
      maximumSelectedYears: 100,
      maximumSelectedParks: 250,
      savedSelectedYears: [2026],
      savedSelectedParkIds: ['park-1'],
      savedSelectedRatingKeys: ['rating-1'],
      savedPublicCaption: 'Mon histoire',
      savedVisibility: 'Unlisted',
      savedAllowsComparisons: true,
      hasSavedSnapshot: true
    };
    const port: PassportProfileSharePort = {
      getSettings: (): Observable<SharePublicationSettings> => of(settings),
      getSelection: (): Observable<PassportProfileShareSelection> => of(selection),
      preview: (request: SharePublicationPreviewRequest): Observable<SharePublicationPreview> => {
        previewRequests.push(request);
        return of(createPreview(request));
      },
      publish: (request: SharePublicationPublishRequest): Observable<SharePublicationSettings> => {
        publishRequests.push(request);
        return of({ isPublic: true, shareId: 'opaque-share-id', includedFields: request.approvedIncludedFields });
      },
      revoke: (): Observable<SharePublicationSettings> => of({ isPublic: false, includedFields: [] })
    };
    TestBed.configureTestingModule({ providers: [
      PassportProfileShareStateFacade,
      { provide: PASSPORT_PROFILE_SHARE_PORT, useValue: port },
      { provide: ToastMessageService, useValue: { add: vi.fn() } },
      { provide: TranslateService, useValue: { instant: (key: string): string => key } }
    ] });
    const facade: PassportProfileShareStateFacade = TestBed.inject(PassportProfileShareStateFacade);

    facade.load();
    facade.toggleField('GlobalRatings');
    facade.toggleField('PublicCaption');
    facade.previewPublication();
    facade.publish();

    expect(facade.selectedYears()).toEqual([2026]);
    expect(previewRequests[0].passportProfile).toEqual({
      selectedYears: [2026],
      selectedParkIds: ['park-1'],
      selectedRatingKeys: ['rating-1'],
      publicCaption: 'Mon histoire',
      visibility: 'Unlisted',
      allowsComparisons: true
    });
    expect(publishRequests[0].approvalToken).toBe('approved-preview');
    expect(publishRequests[0].passportProfile).toEqual(previewRequests[0].passportProfile);
    expect(facade.settings()?.shareId).toBe('opaque-share-id');

    facade.previewPublication();
    expect(facade.canPublish()).toBe(true);
    facade.revoke();

    expect(facade.canPublish()).toBe(false);
  });

  it('filters obsolete saved choices and clears hidden ranking choices', () => {
    const previewRequests: SharePublicationPreviewRequest[] = [];
    const selection: PassportProfileShareSelection = {
      years: [{ year: 2026, visitCount: 2 }],
      parks: [{ parkId: 'park-current', name: 'Parc actuel', countryCode: 'FR', visitCount: 2 }],
      ratings: [{ selectionKey: 'rating-current', parkId: 'park-current', name: 'Attraction actuelle', parkName: 'Parc actuel', rating: 4.5 }],
      maximumSelectedYears: 100,
      maximumSelectedParks: 250,
      savedSelectedYears: [2025, 2026],
      savedSelectedParkIds: ['park-hidden', 'park-current'],
      savedSelectedRatingKeys: ['rating-hidden', 'rating-current'],
      savedVisibility: 'Unlisted',
      savedAllowsComparisons: false,
      hasSavedSnapshot: true
    };
    const port: PassportProfileSharePort = {
      getSettings: (): Observable<SharePublicationSettings> => of({
        isPublic: true,
        policySchemaVersion: 1,
        includedFields: ['GlobalRatings']
      }),
      getSelection: (): Observable<PassportProfileShareSelection> => of(selection),
      preview: (request: SharePublicationPreviewRequest): Observable<SharePublicationPreview> => {
        previewRequests.push(request);
        return of(createPreview(request));
      },
      publish: (): Observable<SharePublicationSettings> => of({ isPublic: true, includedFields: [] }),
      revoke: (): Observable<SharePublicationSettings> => of({ isPublic: false, includedFields: [] })
    };
    TestBed.configureTestingModule({ providers: [
      PassportProfileShareStateFacade,
      { provide: PASSPORT_PROFILE_SHARE_PORT, useValue: port },
      { provide: ToastMessageService, useValue: { add: vi.fn() } },
      { provide: TranslateService, useValue: { instant: (key: string): string => key } }
    ] });
    const facade: PassportProfileShareStateFacade = TestBed.inject(PassportProfileShareStateFacade);

    facade.load();

    expect(facade.selectedYears()).toEqual([2026]);
    expect(facade.selectedParkIds()).toEqual(['park-current']);
    expect(facade.selectedRatingKeys()).toEqual(['rating-current']);

    facade.toggleField('GlobalRatings');
    facade.previewPublication();

    expect(facade.selectedRatingKeys()).toEqual([]);
    expect(previewRequests[0].passportProfile?.selectedRatingKeys).toEqual([]);
  });

  it('settles an in-flight publication when the editor invalidates its preview', () => {
    const publishResponse: Subject<SharePublicationSettings> = new Subject<SharePublicationSettings>();
    const selection: PassportProfileShareSelection = {
      years: [{ year: 2026, visitCount: 1 }],
      parks: [{ parkId: 'park-1', name: 'Parc', countryCode: 'FR', visitCount: 1 }],
      ratings: [],
      maximumSelectedYears: 100,
      maximumSelectedParks: 250,
      savedVisibility: 'Unlisted',
      savedAllowsComparisons: false,
      hasSavedSnapshot: false
    };
    const port: PassportProfileSharePort = {
      getSettings: (): Observable<SharePublicationSettings> => of({ isPublic: false, includedFields: [] }),
      getSelection: (): Observable<PassportProfileShareSelection> => of(selection),
      preview: (request: SharePublicationPreviewRequest): Observable<SharePublicationPreview> =>
        of(createPreview(request)),
      publish: (): Observable<SharePublicationSettings> => publishResponse,
      revoke: (): Observable<SharePublicationSettings> => of({ isPublic: false, includedFields: [] })
    };
    TestBed.configureTestingModule({ providers: [
      PassportProfileShareStateFacade,
      { provide: PASSPORT_PROFILE_SHARE_PORT, useValue: port },
      { provide: ToastMessageService, useValue: { add: vi.fn() } },
      { provide: TranslateService, useValue: { instant: (key: string): string => key } }
    ] });
    const facade: PassportProfileShareStateFacade = TestBed.inject(PassportProfileShareStateFacade);

    facade.load();
    facade.previewPublication();
    facade.publish();
    facade.setVisibility('Public');
    publishResponse.next({ isPublic: true, shareId: 'published', includedFields: [] });

    expect(facade.saving()).toBe(false);
    expect(facade.settings()?.shareId).toBe('published');
  });

  it('bounds new default year and park selections to the server contract', () => {
    const parks = Array.from({ length: 4 }, (_, index) => ({
      parkId: `park-${index + 1}`,
      name: `Parc ${index + 1}`,
      visitCount: 1
    }));
    const selection: PassportProfileShareSelection = {
      years: [2026, 2025, 2024, 2023].map((year) => ({ year, visitCount: 1 })),
      parks,
      ratings: [],
      maximumSelectedYears: 2,
      maximumSelectedParks: 2,
      savedVisibility: 'Unlisted',
      savedAllowsComparisons: false,
      hasSavedSnapshot: false
    };
    const port: PassportProfileSharePort = {
      getSettings: (): Observable<SharePublicationSettings> => of({ isPublic: false, includedFields: [] }),
      getSelection: (): Observable<PassportProfileShareSelection> => of(selection),
      preview: (request: SharePublicationPreviewRequest): Observable<SharePublicationPreview> =>
        of(createPreview(request)),
      publish: (): Observable<SharePublicationSettings> => of({ isPublic: true, includedFields: [] }),
      revoke: (): Observable<SharePublicationSettings> => of({ isPublic: false, includedFields: [] })
    };
    TestBed.configureTestingModule({ providers: [
      PassportProfileShareStateFacade,
      { provide: PASSPORT_PROFILE_SHARE_PORT, useValue: port },
      { provide: ToastMessageService, useValue: { add: vi.fn() } },
      { provide: TranslateService, useValue: { instant: (key: string): string => key } }
    ] });
    const facade: PassportProfileShareStateFacade = TestBed.inject(PassportProfileShareStateFacade);

    facade.load();

    expect(facade.selectedYears()).toEqual([2026, 2025]);
    expect(facade.selectedParkIds()).toEqual(['park-1', 'park-2']);

    facade.toggleYear(2024);
    expect(facade.selectedYears()).toEqual([2026, 2025]);
    facade.toggleYear(2026);
    facade.toggleYear(2024);
    expect(facade.selectedYears()).toEqual([2025, 2024]);

    facade.togglePark('park-3');
    expect(facade.selectedParkIds()).toEqual(['park-1', 'park-2']);

    facade.togglePark('park-1');
    facade.togglePark('park-3');
    expect(facade.selectedParkIds()).toEqual(['park-2', 'park-3']);
  });

  it('removes selected ratings when their park is deselected', () => {
    const selection: PassportProfileShareSelection = {
      years: [{ year: 2026, visitCount: 1 }],
      parks: [
        { parkId: 'park-1', name: 'Parc 1', visitCount: 1 },
        { parkId: 'park-2', name: 'Parc 2', visitCount: 1 }
      ],
      ratings: [
        { selectionKey: 'rating-1', parkId: 'park-1', name: 'Attraction 1', rating: 4.5 },
        { selectionKey: 'rating-2', parkId: 'park-2', name: 'Attraction 2', rating: 4 }
      ],
      maximumSelectedYears: 100,
      maximumSelectedParks: 250,
      savedSelectedYears: [2026],
      savedSelectedParkIds: ['park-1', 'park-2'],
      savedSelectedRatingKeys: ['rating-1', 'rating-2'],
      savedVisibility: 'Unlisted',
      savedAllowsComparisons: false,
      hasSavedSnapshot: true
    };
    const port: PassportProfileSharePort = {
      getSettings: (): Observable<SharePublicationSettings> => of({
        isPublic: true,
        policySchemaVersion: 1,
        includedFields: ['GlobalRatings']
      }),
      getSelection: (): Observable<PassportProfileShareSelection> => of(selection),
      preview: (request: SharePublicationPreviewRequest): Observable<SharePublicationPreview> =>
        of(createPreview(request)),
      publish: (): Observable<SharePublicationSettings> => of({ isPublic: true, includedFields: [] }),
      revoke: (): Observable<SharePublicationSettings> => of({ isPublic: false, includedFields: [] })
    };
    TestBed.configureTestingModule({ providers: [
      PassportProfileShareStateFacade,
      { provide: PASSPORT_PROFILE_SHARE_PORT, useValue: port },
      { provide: ToastMessageService, useValue: { add: vi.fn() } },
      { provide: TranslateService, useValue: { instant: (key: string): string => key } }
    ] });
    const facade: PassportProfileShareStateFacade = TestBed.inject(PassportProfileShareStateFacade);

    facade.load();
    facade.togglePark('park-1');

    expect(facade.selectedParkIds()).toEqual(['park-2']);
    expect(facade.selectedRatingKeys()).toEqual(['rating-2']);

    facade.toggleRating('rating-1');

    expect(facade.canSelectRating('rating-1')).toBe(false);
    expect(facade.selectedRatingKeys()).toEqual(['rating-2']);
  });
});

function createPreview(request: SharePublicationPreviewRequest): SharePublicationPreview {
  return {
    publicationType: 'PassportProfile',
    sourceVersion: 12,
    approvalToken: 'approved-preview',
    contentPolicy: { schemaVersion: 1, datePrecision: 'Year', includedFields: request.includedFields },
    passportProfile: {
      displayName: 'Alex', visibility: 'Unlisted', allowsComparisons: true,
      parkCount: 1, visitCount: 2, totalRideCount: 4, distinctItemCount: 2,
      countries: [], years: [], parks: [], personalRanking: [], missedItems: [],
      hasIncompleteCatalog: false, calculationVersion: 'passport-profile-v1', isEmpty: false
    }
  };
}
