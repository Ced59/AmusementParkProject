import { EventEmitter } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, ParamMap, convertToParamMap } from '@angular/router';
import { Observable, of } from 'rxjs';

import {
  SharedUserRankingProfile,
  UserParkItemRatingRankingsPage,
  UserParkRatingRankingsPage,
} from '@app/models/ratings/rating.models';
import { AuthService } from '@app/services/auth/auth.service';
import { ModalService } from '@app/services/modal/modal.service';
import { TranslationService } from '@app/services/translation.service';
import { SeoService } from '@core/seo/seo.service';
import { SsrHttpStatusService } from '@core/ssr/ssr-http-status.service';
import {
  COMMON_TEST_IMPORTS,
  provideCommonTestDependencies,
} from '@app/testing/common-test-providers';
import { DEFAULT_PAGINATION } from '@shared/models/contracts';
import {
  SHARED_USER_RANKINGS_PORT,
  SharedUserRankingsPort,
} from '../state/shared-user-rankings-state-data.ports';
import { SharedUserRankingsPageComponent } from './shared-user-rankings-page.component';

class FakeSharedRankingsPagePort implements SharedUserRankingsPort {
  readonly itemCalls: Array<{ category: string; type: string | null }> = [];

  getSharedProfile(_shareId: string): Observable<SharedUserRankingProfile> {
    return of({
      displayName: 'Camille',
      publishedAtUtc: '2026-08-20T18:00:00Z',
      isOwner: false,
      stats: {
        totalRatings: 2,
        averageRating: 4.5,
        highestRating: 5,
        lowestRating: 4,
        byPark: [],
        byTargetType: [],
        byParkItemCategory: [],
      },
    });
  }

  getSharedParkRankings(
    _shareId: string,
    _page: number,
    _size: number,
    _search: string | null,
  ): Observable<UserParkRatingRankingsPage> {
    return of({
      items: [{
        rank: 1,
        parkId: 'park-1',
        parkName: 'Phantasialand',
        ratingCount: 2,
        averageRating: 4.5,
        parkRating: null,
        categories: [{
          parkItemCategory: 'Attraction',
          averageRating: 4.5,
          items: [createRating()],
        }],
      }],
      pagination: {
        ...DEFAULT_PAGINATION,
        currentPage: 1,
        itemsPerPage: 10,
        totalItems: 1,
        totalPages: 1,
      },
    });
  }

  getSharedParkItemRankings(
    _shareId: string,
    _page: number,
    _size: number,
    category: string,
    type: string | null,
    _search: string | null,
  ): Observable<UserParkItemRatingRankingsPage> {
    this.itemCalls.push({ category, type });
    return of({
      items: [{ rank: 1, rating: createRating() }],
      pagination: {
        ...DEFAULT_PAGINATION,
        currentPage: 1,
        itemsPerPage: 10,
        totalItems: 1,
        totalPages: 1,
      },
    });
  }
}

describe('SharedUserRankingsPageComponent', () => {
  let fixture: ComponentFixture<SharedUserRankingsPageComponent>;
  let port: FakeSharedRankingsPagePort;
  let routeSnapshot: { paramMap: ParamMap; queryParamMap: ParamMap };
  let loggedIn: boolean;
  let openModal: ReturnType<typeof vi.fn>;

  beforeEach(async () => {
    port = new FakeSharedRankingsPagePort();
    loggedIn = false;
    openModal = vi.fn();
    routeSnapshot = {
      paramMap: convertToParamMap({ lang: 'fr', shareId: 'opaque-share-id' }),
      queryParamMap: convertToParamMap({}),
    };

    await TestBed.configureTestingModule({
      imports: [...COMMON_TEST_IMPORTS, SharedUserRankingsPageComponent],
      providers: [
        ...provideCommonTestDependencies(),
        {
          provide: ActivatedRoute,
          useValue: { snapshot: routeSnapshot, parent: null },
        },
        { provide: SHARED_USER_RANKINGS_PORT, useValue: port },
        { provide: AuthService, useValue: { isLoggedIn: () => loggedIn } },
        { provide: ModalService, useValue: { openModal } },
        {
          provide: TranslationService,
          useValue: {
            getCurrentLang: () => 'fr',
            languageChanged: new EventEmitter<string>(),
          },
        },
        {
          provide: SeoService,
          useValue: {
            applyRouteDefaults: vi.fn(),
            applySharedUserRankingSeo: vi.fn(),
            applyNotFoundSeo: vi.fn(),
          },
        },
        { provide: SsrHttpStatusService, useValue: { setNotFound: vi.fn() } },
      ],
    }).compileComponents();
  });

  it('renders another user ranking without editable stars and offers a gentle account invitation', () => {
    fixture = TestBed.createComponent(SharedUserRankingsPageComponent);
    fixture.detectChanges();

    const host: HTMLElement = fixture.nativeElement as HTMLElement;
    expect(host.textContent).toContain('ratings.share.cta.anonymousTitle');
    expect(host.querySelectorAll('.rating-tree__star-hit')).toHaveLength(0);

    const accountButton: HTMLButtonElement | null = host.querySelector(
      '.shared-user-rankings__cta button',
    );
    accountButton?.click();
    expect(openModal).toHaveBeenCalledWith('loginModal');
  });

  it('links a signed-in visitor to their own ranking without editing the shared one', () => {
    loggedIn = true;
    fixture = TestBed.createComponent(SharedUserRankingsPageComponent);
    fixture.detectChanges();

    const host: HTMLElement = fixture.nativeElement as HTMLElement;
    expect(host.textContent).toContain('ratings.share.cta.memberTitle');
    const profileLink: HTMLAnchorElement | null = host.querySelector(
      '.shared-user-rankings__cta a',
    );
    expect(profileLink?.getAttribute('href')).toBe('/fr/profile?tab=ratings');
    expect(host.querySelectorAll('.rating-tree__star-hit')).toHaveLength(0);
  });

  it('restores a shared category and attraction type from the URL', () => {
    routeSnapshot.queryParamMap = convertToParamMap({
      category: 'Attraction',
      type: 'FlatRide',
    });
    fixture = TestBed.createComponent(SharedUserRankingsPageComponent);
    fixture.detectChanges();

    expect(port.itemCalls).toEqual([
      { category: 'Attraction', type: 'FlatRide' },
    ]);
    expect((fixture.nativeElement as HTMLElement).textContent).toContain(
      'parkExplorer.types.flatRide',
    );
  });
});

function createRating() {
  return {
    id: 'rating-1',
    targetType: 'ParkItem' as const,
    targetId: 'item-1',
    targetName: 'Talocan',
    parkId: 'park-1',
    parkName: 'Phantasialand',
    parkItemCategory: 'Attraction',
    parkItemType: 'FlatRide',
    value: 4.5,
    updatedAtUtc: '2026-08-20T18:00:00Z',
    summary: {
      targetType: 'ParkItem' as const,
      targetId: 'item-1',
      ratingCount: 1,
      averageRating: 4.5,
      bayesianScore: 4,
    },
  };
}
