import { Observable, of, Subject } from 'rxjs';

import { UserRatingStats, UserRankingShareSettings } from '@app/models/ratings/rating.models';

import { SharePublicationPreview, SharePublicationPreviewRequest, SharePublicationPublishRequest, SharePublicationSettings } from '@app/models/sharing/share-publication.models';

import { UserRankingSharePort } from '../../user-ranking-share-state-data.ports';

function createStats(): UserRatingStats {
  return {
    totalRatings: 2,
    averageRating: 4.5,
    highestRating: 5,
    lowestRating: 4,
    byPark: [
      { key: 'park-1', label: 'Phantasialand', count: 2, averageRating: 4.5 }
    ],
    byTargetType: [
      { key: 'Park', label: 'Parcs', count: 1, averageRating: 5 },
      { key: 'ParkItem', label: 'Lieux', count: 1, averageRating: 4 }
    ],
    byParkItemCategory: [
      { key: 'Attraction', label: 'Attractions', count: 1, averageRating: 4 }
    ]
  };
}

export class FakeUserRankingSharePort implements UserRankingSharePort {
  readonly visibilityCalls: boolean[] = [];
  readonly previewCalls: SharePublicationPreviewRequest[] = [];
  readonly publishCalls: SharePublicationPublishRequest[] = [];
  readonly rotatedPublicationIds: string[] = [];
  readonly revokedPublicationIds: string[] = [];
  previewResponse: Subject<SharePublicationPreview> | null = null;
  publishResponse: Subject<SharePublicationSettings> | null = null;
  settingsCalls: number = 0;
  refreshedSettings: UserRankingShareSettings | null = null;
  settings: UserRankingShareSettings = {
    isPublic: false,
    shareId: null,
    publishedAtUtc: null,
  };

  getMyShareSettings(): Observable<UserRankingShareSettings> {
    this.settingsCalls++;
    return of(this.settingsCalls > 1 && this.refreshedSettings
      ? this.refreshedSettings
      : this.settings);
  }

  setMyShareVisibility(isPublic: boolean): Observable<UserRankingShareSettings> {
    this.visibilityCalls.push(isPublic);
    this.settings = isPublic
      ? {
        isPublic: true,
        shareId: 'opaque-share-id',
        publishedAtUtc: '2026-08-20T18:00:00Z',
      }
      : {
        isPublic: false,
        shareId: null,
        publishedAtUtc: null,
      };
    return of(this.settings);
  }

  preview(request: SharePublicationPreviewRequest): Observable<SharePublicationPreview> {
    this.previewCalls.push(request);
    const preview: SharePublicationPreview = {
      publicationType: 'PersonalRanking',
      sourceVersion: 12,
      approvalToken: 'approved-preview',
      contentPolicy: {
        schemaVersion: 1,
        datePrecision: 'Hidden',
        includedFields: request.includedFields
      },
      personalRanking: {
        displayName: request.includedFields.includes('PublicDisplayName') ? 'Camille' : null,
        avatarUrl: null,
        statistics: createStats(),
        ratings: [],
        isTruncated: false
      }
    };
    return this.previewResponse ?? of(preview);
  }

  publish(request: SharePublicationPublishRequest): Observable<SharePublicationSettings> {
    this.publishCalls.push(request);
    this.settings = {
      isPublic: true,
      publicationId: 'publication-1',
      shareId: 'opaque-share-id',
      publishedAtUtc: '2026-09-07T08:00:00Z',
      policySchemaVersion: request.approvedPolicySchemaVersion,
      datePrecision: request.approvedDatePrecision,
      includedFields: request.approvedIncludedFields
    };
    return this.publishResponse ?? of(this.settings as SharePublicationSettings);
  }

  rotate(publicationId: string): Observable<SharePublicationSettings> {
    this.rotatedPublicationIds.push(publicationId);
    return of(this.settings as SharePublicationSettings);
  }

  revoke(publicationId: string): Observable<SharePublicationSettings> {
    this.revokedPublicationIds.push(publicationId);
    this.settings = {
      isPublic: false,
      shareId: null,
      publishedAtUtc: null,
      publicationId
    };
    return of(this.settings as SharePublicationSettings);
  }
}
