import { TestBed } from '@angular/core/testing';
import { Observable, of } from 'rxjs';

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
      ratings: [{ selectionKey: 'rating-1', name: 'Le Galion', parkName: 'Denain Évasion', rating: 4.5 }],
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
