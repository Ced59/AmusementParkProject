import { ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { Observable, of, Subject } from 'rxjs';

import {
  UserParkItemRatingRanking,
  UserParkItemRatingRankingsPage,
  UserParkRatingRanking,
  UserParkRatingRankingsPage,
  UserRating,
  UserRatingListItem,
  UserRatingStats,
  UserRatingUpsertRequest,
  UserRankingShareSettings
} from '@app/models/ratings/rating.models';
import { COMMON_TEST_IMPORTS, provideCommonTestDependencies } from '@app/testing/common-test-providers';
import {
  SharePublicationPreview,
  SharePublicationPreviewRequest,
  SharePublicationPublishRequest,
  SharePublicationSettings
} from '@app/models/sharing/share-publication.models';
import { DEFAULT_PAGINATION } from '@shared/models/contracts';
import { PublicSharePanelComponent } from '@ui/sharing/public-share-panel/public-share-panel.component';
import { GlobalRatingSuggestionViewModel } from '../passport/models/global-rating-suggestion-view.models';
import { PROFILE_RATINGS_PORT, ProfileRatingsPort } from './profile-ratings-state-data.ports';
import { ProfileRatingsPanelComponent } from './profile-ratings-panel.component';
import { USER_RANKING_SHARE_PORT, UserRankingSharePort } from './user-ranking-share-state-data.ports';

class FakeProfileRatingsPort implements ProfileRatingsPort {
  readonly upsertCalls: UserRatingUpsertRequest[] = [];
  parkItemResponse: Subject<UserParkItemRatingRankingsPage> | null = null;
  readonly parkItemCalls: Array<{
    page: number;
    category: string;
    type: string | null;
    search: string | null;
    targetId: string | null;
  }> = [];
  readonly parkRankings: UserParkRatingRanking[] = [
    {
      rank: 1,
      parkId: 'park-1',
      parkName: 'Phantasialand',
      ratingCount: 2,
      averageRating: 4.5,
      parkRating: createRatingListItem('rating-park-1', 'Park', 'park-1', 'Phantasialand', 5, null),
      categories: [
        {
          parkItemCategory: 'Attraction',
          averageRating: 4,
          items: [
            createRatingListItem('rating-item-1', 'ParkItem', 'item-1', 'Taron', 4, 'Attraction')
          ]
        }
      ]
    }
  ];
  readonly parkItemRankings: UserParkItemRatingRanking[] = [
    {
      rank: 1,
      rating: createRatingListItem('rating-item-1', 'ParkItem', 'item-1', 'Taron', 4, 'Attraction')
    }
  ];

  getMyParkRankings(
    _page: number,
    _size: number,
    _search: string | null,
    _targetId: string | null
  ): Observable<UserParkRatingRankingsPage> {
    return of({
      items: this.parkRankings,
      pagination: {
        ...DEFAULT_PAGINATION,
        currentPage: 1,
        itemsPerPage: 10,
        totalItems: this.parkRankings.length,
        totalPages: 1
      }
    });
  }

  getMyParkItemRankings(
    page: number,
    _size: number,
    category: string,
    type: string | null,
    search: string | null,
    targetId: string | null
  ): Observable<UserParkItemRatingRankingsPage> {
    this.parkItemCalls.push({ page, category, type, search, targetId });
    if (this.parkItemResponse) {
      return this.parkItemResponse;
    }

    return of({
      items: this.parkItemRankings,
      pagination: {
        ...DEFAULT_PAGINATION,
        currentPage: page,
        itemsPerPage: 10,
        totalItems: search ? 20 : this.parkItemRankings.length,
        totalPages: search ? 2 : 1
      }
    });
  }

  getMyRatingStats(): Observable<UserRatingStats> {
    return of(createStats());
  }

  upsertRating(request: UserRatingUpsertRequest): Observable<UserRating> {
    this.upsertCalls.push(request);
    const ratings: UserRatingListItem[] = [
      this.parkRankings[0].parkRating!,
      ...this.parkRankings[0].categories.flatMap(category => category.items)
    ];
    const rating: UserRatingListItem | undefined = ratings.find((item: UserRatingListItem): boolean => {
      return item.targetType === request.targetType && item.targetId === request.targetId;
    });

    return of({
      id: rating?.id ?? 'rating-1',
      targetType: request.targetType,
      targetId: request.targetId,
      parkId: rating?.parkId ?? 'park-1',
      parkItemCategory: rating?.parkItemCategory ?? null,
      parkItemType: rating?.parkItemType ?? null,
      value: request.value,
      createdAtUtc: '2026-06-19T10:00:00Z',
      updatedAtUtc: '2026-06-19T11:00:00Z',
      summary: {
        targetType: request.targetType,
        targetId: request.targetId,
        ratingCount: 2,
        averageRating: request.value,
        bayesianScore: request.value
      }
    });
  }
}

class FakeUserRankingSharePort implements UserRankingSharePort {
  readonly visibilityCalls: boolean[] = [];
  readonly previewCalls: SharePublicationPreviewRequest[] = [];
  readonly publishCalls: SharePublicationPublishRequest[] = [];
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
      shareId: 'opaque-share-id',
      publishedAtUtc: '2026-09-07T08:00:00Z',
      policySchemaVersion: request.approvedPolicySchemaVersion,
      datePrecision: request.approvedDatePrecision,
      includedFields: request.approvedIncludedFields
    };
    return this.publishResponse ?? of(this.settings as SharePublicationSettings);
  }
}

describe('ProfileRatingsPanelComponent', () => {
  let fixture: ComponentFixture<ProfileRatingsPanelComponent>;
  let port: FakeProfileRatingsPort;
  let sharePort: FakeUserRankingSharePort;

  beforeEach(async () => {
    port = new FakeProfileRatingsPort();
    sharePort = new FakeUserRankingSharePort();

    await TestBed.configureTestingModule({
      imports: [...COMMON_TEST_IMPORTS, ProfileRatingsPanelComponent],
      providers: [
        ...provideCommonTestDependencies(),
        { provide: PROFILE_RATINGS_PORT, useValue: port },
        { provide: USER_RANKING_SHARE_PORT, useValue: sharePort },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(ProfileRatingsPanelComponent);
  });

  it('renders direct park ratings as park metrics instead of nested park sections', () => {
    fixture.detectChanges();

    const tree: HTMLElement | null = fixture.nativeElement.querySelector('app-rating-tree');
    const text: string = tree?.textContent ?? '';

    expect(text).toContain('ratings.rankings.parkSignal');
    expect(text).toContain('ratings.rankings.itemsSignal');
    expect(text).toContain('ratings.categories.Attraction');
    expect(text).not.toContain('ratings.targetTypes.Park');
  });

  it('updates an already displayed rating from inline stars', () => {
    fixture.detectChanges();

    const buttons: NodeListOf<HTMLButtonElement> = fixture.nativeElement.querySelectorAll('.rating-tree__items .rating-tree__star-hit--right');
    buttons[2]?.click();

    expect(port.upsertCalls).toEqual([
      { targetType: 'ParkItem', targetId: 'item-1', value: 3 }
    ]);
  });

  it('refreshes and hides an invalidated public share after a rating edit', () => {
    sharePort.settings = {
      isPublic: true,
      shareId: 'opaque-share-id',
      publishedAtUtc: '2026-09-07T08:00:00Z',
      includedFields: ['GlobalRatings']
    };
    sharePort.refreshedSettings = {
      isPublic: false,
      shareId: null,
      publishedAtUtc: null,
      includedFields: ['GlobalRatings']
    };
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('app-public-share-panel')).not.toBeNull();

    const buttons: NodeListOf<HTMLButtonElement> =
      fixture.nativeElement.querySelectorAll('.rating-tree__items .rating-tree__star-hit--right');
    buttons[2]?.click();
    fixture.detectChanges();

    expect(sharePort.settingsCalls).toBe(2);
    expect(fixture.nativeElement.querySelector('app-public-share-panel')).toBeNull();
  });

  it('discards a publish response completed after a rating edit', () => {
    const pendingPublish: Subject<SharePublicationSettings> =
      new Subject<SharePublicationSettings>();
    sharePort.publishResponse = pendingPublish;
    sharePort.refreshedSettings = {
      isPublic: false,
      shareId: null,
      publishedAtUtc: null,
      includedFields: ['GlobalRatings']
    };
    fixture.detectChanges();

    const openButton: HTMLButtonElement = fixture.nativeElement.querySelector(
      '.ranking-share__actions button',
    );
    openButton.click();
    fixture.detectChanges();
    const previewButton: HTMLButtonElement = fixture.nativeElement.querySelector(
      '.ranking-share-editor__actions button:last-child',
    );
    previewButton.click();
    fixture.detectChanges();
    const confirmButton: HTMLButtonElement = fixture.nativeElement.querySelector(
      '.ranking-share-editor__actions button:last-child',
    );
    confirmButton.click();
    fixture.detectChanges();

    const ratingButtons: NodeListOf<HTMLButtonElement> =
      fixture.nativeElement.querySelectorAll('.rating-tree__items .rating-tree__star-hit--right');
    ratingButtons[2]?.click();
    fixture.detectChanges();
    pendingPublish.next({
      isPublic: true,
      shareId: 'stale-share-id',
      publishedAtUtc: '2026-09-07T08:00:00Z',
      includedFields: ['GlobalRatings']
    });
    fixture.detectChanges();

    expect(sharePort.settingsCalls).toBe(2);
    expect(fixture.nativeElement.querySelector('app-public-share-panel')).toBeNull();
  });

  it('shows a flat attraction ranking with its place and parent park', () => {
    fixture.detectChanges();

    const filterButtons: NodeListOf<HTMLButtonElement> = fixture.nativeElement.querySelectorAll('.profile-ratings__filters button');
    filterButtons[1]?.click();
    fixture.detectChanges();

    const list: HTMLElement | null = fixture.nativeElement.querySelector('app-rating-ranking-list');
    expect(list?.textContent).toContain('#1');
    expect(list?.textContent).toContain('Taron');
    expect(list?.textContent).toContain('Phantasialand');
    expect(fixture.nativeElement.querySelector('app-rating-tree')).toBeNull();
  });

  it('opens coaster and flat ride rankings from the quick filters', () => {
    fixture.detectChanges();

    const categoryButtons: NodeListOf<HTMLButtonElement> =
      fixture.nativeElement.querySelectorAll('.profile-ratings__filters button');
    categoryButtons[1]?.click();
    fixture.detectChanges();

    const quickFilterButtons: NodeListOf<HTMLButtonElement> =
      fixture.nativeElement.querySelectorAll('.profile-ratings__quick-filters button');
    quickFilterButtons[1]?.click();
    quickFilterButtons[2]?.click();

    expect(port.parkItemCalls.slice(-2)).toEqual([
      { page: 1, category: 'Attraction', type: 'RollerCoaster', search: null, targetId: null },
      { page: 1, category: 'Attraction', type: 'FlatRide', search: null, targetId: null }
    ]);
  });

  it('exposes every park item category as a direct ranking filter', () => {
    fixture.detectChanges();

    const categoryButtons: NodeListOf<HTMLButtonElement> =
      fixture.nativeElement.querySelectorAll('.profile-ratings__filters button');

    expect(categoryButtons).toHaveLength(10);
    expect(fixture.nativeElement.textContent).toContain('ratings.categories.Animal');
    expect(fixture.nativeElement.textContent).toContain('ratings.categories.Show');
    expect(fixture.nativeElement.textContent).toContain('ratings.categories.Transport');
  });

  it('hides the previous result count while a new ranking is loading', () => {
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('.profile-ratings__result-count')).not.toBeNull();
    port.parkItemResponse = new Subject<UserParkItemRatingRankingsPage>();

    const categoryButtons: NodeListOf<HTMLButtonElement> =
      fixture.nativeElement.querySelectorAll('.profile-ratings__filters button');
    categoryButtons[1]?.click();
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('.profile-ratings__result-count')).toBeNull();
  });

  it('loads the next personal park item search page with the active search term', () => {
    fixture.detectChanges();

    const filterButtons: NodeListOf<HTMLButtonElement> =
      fixture.nativeElement.querySelectorAll('.profile-ratings__filters button');
    filterButtons[1]?.click();
    fixture.detectChanges();

    const searchInput: HTMLInputElement =
      fixture.nativeElement.querySelector('.profile-ratings__search input');
    searchInput.value = ' ride ';
    searchInput.dispatchEvent(new Event('input'));
    const searchButton: HTMLButtonElement =
      fixture.nativeElement.querySelector('.profile-ratings__search button');
    searchButton.click();
    fixture.detectChanges();

    const loadMoreButton: HTMLButtonElement =
      fixture.nativeElement.querySelector('.profile-ratings__more button');
    expect(loadMoreButton).toBeTruthy();
    loadMoreButton.click();
    fixture.detectChanges();

    expect(port.parkItemCalls.at(-1)).toEqual({
      page: 2,
      category: 'Attraction',
      type: null,
      search: 'ride',
      targetId: null
    });
  });

  it('opens the matching personal ranking without changing a suggested rating automatically', () => {
    fixture.detectChanges();
    const suggestion: GlobalRatingSuggestionViewModel = {
      id: 'ParkItem:item-1',
      targetType: 'ParkItem',
      targetId: 'item-1',
      presentedAtUtc: '2026-09-04T10:00:00Z',
      targetName: 'Taron',
      parkName: 'Phantasialand',
      parkItemCategory: 'Attraction',
      currentGlobalRatingLabel: '4.5',
      latestObservationRatingLabel: '3',
      recentAverageLabel: '3.25',
      historicalMedianLabel: '4',
      newObservationCount: 2,
      recentObservationCount: 2,
      reasonKey: 'passportRatingSuggestions.reasons.lower'
    };

    (fixture.componentInstance as unknown as {
      reviewSuggestion(value: GlobalRatingSuggestionViewModel): void;
    }).reviewSuggestion(suggestion);

    expect(port.parkItemCalls.at(-1)).toEqual({
      page: 1,
      category: 'Attraction',
      type: null,
      search: 'Taron',
      targetId: 'item-1'
    });
    expect(port.upsertCalls).toEqual([]);
  });

  it('previews the exact privacy selection before publishing and revoking the ranking', () => {
    fixture.detectChanges();

    const publishButton: HTMLButtonElement = fixture.nativeElement.querySelector(
      '.ranking-share__actions button',
    );
    publishButton.click();
    fixture.detectChanges();

    const nameCheckbox: HTMLInputElement = fixture.nativeElement.querySelector(
      '.ranking-share-choice input',
    );
    nameCheckbox.click();
    const previewButton: HTMLButtonElement = fixture.nativeElement.querySelector(
      '.ranking-share-editor__actions button:last-child',
    );
    previewButton.click();
    fixture.detectChanges();

    expect(sharePort.previewCalls[0]?.includedFields).toEqual(['GlobalRatings']);
    expect(fixture.nativeElement.querySelector('.ranking-share-preview__sample-notice')).not.toBeNull();
    const confirmButton: HTMLButtonElement = fixture.nativeElement.querySelector(
      '.ranking-share-editor__actions button:last-child',
    );
    confirmButton.click();
    fixture.detectChanges();

    expect(sharePort.publishCalls[0]).toEqual({
      publicationType: 'PersonalRanking',
      sourceId: null,
      approvedSourceVersion: 12,
      approvedPolicySchemaVersion: 1,
      approvedDatePrecision: 'Hidden',
      approvedIncludedFields: ['GlobalRatings'],
      approvalToken: 'approved-preview'
    });
    const sharedLink: HTMLAnchorElement | null = fixture.nativeElement.querySelector(
      '.ranking-share__actions a',
    );
    expect(sharedLink?.getAttribute('href')).toBe('/en/rankings/shared/opaque-share-id');
    const sharePanel: PublicSharePanelComponent = fixture.debugElement
      .query(By.directive(PublicSharePanelComponent))
      .componentInstance as PublicSharePanelComponent;
    expect(sharePanel.targetTitle).not.toContain('Camille');
    expect(sharePanel.textParams['name']).not.toBe('Camille');

    const actionButtons: NodeListOf<HTMLButtonElement> = fixture.nativeElement.querySelectorAll(
      '.ranking-share__actions button',
    );
    const revokeButton: HTMLButtonElement = actionButtons[1];
    revokeButton.click();
    fixture.detectChanges();

    expect(sharePort.visibilityCalls).toEqual([false]);
    expect(fixture.nativeElement.querySelector('app-public-share-panel')).toBeNull();
  });

  it('discards an in-flight preview when the privacy selection changes', () => {
    const pendingPreview: Subject<SharePublicationPreview> = new Subject<SharePublicationPreview>();
    sharePort.previewResponse = pendingPreview;
    fixture.detectChanges();

    const publishButton: HTMLButtonElement = fixture.nativeElement.querySelector(
      '.ranking-share__actions button',
    );
    publishButton.click();
    fixture.detectChanges();

    const previewButton: HTMLButtonElement = fixture.nativeElement.querySelector(
      '.ranking-share-editor__actions button:last-child',
    );
    previewButton.click();
    fixture.detectChanges();

    const nameCheckbox: HTMLInputElement = fixture.nativeElement.querySelector(
      '.ranking-share-choice input',
    );
    nameCheckbox.click();
    pendingPreview.next({
      publicationType: 'PersonalRanking',
      sourceVersion: 12,
      approvalToken: 'stale-approved-preview',
      contentPolicy: {
        schemaVersion: 1,
        datePrecision: 'Hidden',
        includedFields: ['PublicDisplayName', 'GlobalRatings']
      },
      personalRanking: {
        displayName: 'Camille',
        avatarUrl: null,
        statistics: createStats(),
        ratings: [],
        isTruncated: false
      }
    });
    fixture.detectChanges();

    expect(sharePort.previewCalls[0]?.includedFields).toEqual(['PublicDisplayName', 'GlobalRatings']);
    expect(fixture.nativeElement.querySelector('.ranking-share-preview')).toBeNull();
    expect(sharePort.publishCalls).toEqual([]);
  });

  it('locks privacy choices while publishing an approved preview', () => {
    sharePort.publishResponse = new Subject<SharePublicationSettings>();
    fixture.detectChanges();

    const publishButton: HTMLButtonElement = fixture.nativeElement.querySelector(
      '.ranking-share__actions button',
    );
    publishButton.click();
    fixture.detectChanges();

    const previewButton: HTMLButtonElement = fixture.nativeElement.querySelector(
      '.ranking-share-editor__actions button:last-child',
    );
    previewButton.click();
    fixture.detectChanges();

    const confirmButton: HTMLButtonElement = fixture.nativeElement.querySelector(
      '.ranking-share-editor__actions button:last-child',
    );
    confirmButton.click();
    fixture.detectChanges();

    const nameCheckbox: HTMLInputElement = fixture.nativeElement.querySelector(
      '.ranking-share-choice input',
    );
    expect(nameCheckbox.disabled).toBe(true);
    expect(sharePort.publishCalls).toHaveLength(1);
  });
});

function createRatingListItem(
  id: string,
  targetType: 'Park' | 'ParkItem',
  targetId: string,
  targetName: string,
  value: number,
  category: string | null
): UserRatingListItem {
  return {
    id,
    targetType,
    targetId,
    targetName,
    parkId: 'park-1',
    parkName: 'Phantasialand',
    parkItemCategory: category,
    parkItemType: null,
    value,
    updatedAtUtc: '2026-06-19T10:00:00Z',
    summary: {
      targetType,
      targetId,
      ratingCount: 2,
      averageRating: value,
      bayesianScore: value
    }
  };
}

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
