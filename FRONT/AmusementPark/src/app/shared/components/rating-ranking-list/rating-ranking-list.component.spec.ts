import { ComponentFixture, TestBed } from '@angular/core/testing';

import {
  COMMON_TEST_IMPORTS,
  provideCommonTestDependencies,
} from '@app/testing/common-test-providers';
import {
  RatingRankingListComponent,
  RatingRankingListRatingChange,
} from './rating-ranking-list.component';

describe('RatingRankingListComponent', () => {
  let fixture: ComponentFixture<RatingRankingListComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [...COMMON_TEST_IMPORTS, RatingRankingListComponent],
      providers: provideCommonTestDependencies(),
    }).compileComponents();

    fixture = TestBed.createComponent(RatingRankingListComponent);
    fixture.componentRef.setInput('items', [
      {
        id: 'item-1',
        rank: 3,
        name: 'Taron',
        score: 4.5,
        ratingCount: 12,
        route: ['/fr/parcs/park-1/attractions/item-1'],
        parkName: 'Phantasialand',
        parkRoute: ['/fr/parcs/park-1'],
        editable: {
          ratingId: 'rating-1',
          saving: false,
        },
      },
    ]);
    fixture.detectChanges();
  });

  it('renders the item place with its parent park underneath', () => {
    const item: HTMLElement | null =
      fixture.nativeElement.querySelector('.rating-ranking-list__item');

    expect(item?.textContent).toContain('#3');
    expect(item?.textContent).toContain('Taron');
    expect(item?.textContent).toContain('Phantasialand');
  });

  it('emits an inline personal rating change', () => {
    const changes: RatingRankingListRatingChange[] = [];
    fixture.componentInstance.ratingChange.subscribe(
      (change: RatingRankingListRatingChange): void => {
        changes.push(change);
      },
    );

    const scoreButtons: NodeListOf<HTMLButtonElement> =
      fixture.nativeElement.querySelectorAll('.rating-ranking-list__star-hit--right');
    scoreButtons[3]?.click();

    expect(changes).toEqual([{ ratingId: 'rating-1', value: 4 }]);
  });

  it('does not render a misleading place when the item has no public rank', () => {
    fixture.componentRef.setInput('items', [
      {
        id: 'item-unranked',
        rank: null,
        name: 'Provisional attraction',
        score: 4.5,
        ratingCount: 2,
        route: null,
        parkName: 'Demo Park',
        parkRoute: null,
      },
    ]);
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('.rating-ranking-list__rank')).toBeNull();
    expect(fixture.nativeElement.textContent).not.toContain('#null');
  });

  it('renders optional evidence for a public ranking item', () => {
    fixture.componentRef.setInput('items', [
      {
        id: 'item-evidence',
        rank: 8,
        name: 'Evidence attraction',
        score: 4.4,
        ratingCount: 41,
        route: null,
        parkName: 'Demo Park',
        parkRoute: null,
        evidence: {
          evidence: {
            level: 'Established',
            isEligibleForMainRanking: true,
            nextThreshold: 50,
          },
          uniqueContributorCount: 38,
          ratingObservationCount: 41,
          targetType: 'ParkItem',
          rank: 8,
          methodologyVersion: 'ratings-2026-01',
          eligibilityThreshold: 10,
        },
      },
    ]);
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('.rating-ranking-list__rank')?.textContent)
      .toContain('#8');
    expect(fixture.nativeElement.querySelector('app-rating-evidence')).not.toBeNull();
  });
});
