import { ComponentFixture, TestBed } from '@angular/core/testing';
import { TranslateService } from '@ngx-translate/core';

import {
  COMMON_TEST_IMPORTS,
  provideCommonTestDependencies,
} from '@app/testing/common-test-providers';
import { RatingTreeComponent, RatingTreePark } from './rating-tree.component';

describe('RatingTreeComponent', () => {
  let component: RatingTreeComponent;
  let fixture: ComponentFixture<RatingTreeComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [...COMMON_TEST_IMPORTS, RatingTreeComponent],
      providers: provideCommonTestDependencies(),
    }).compileComponents();

    fixture = TestBed.createComponent(RatingTreeComponent);
    component = fixture.componentInstance;
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('renders parks and sections collapsed by default', () => {
    fixture.componentRef.setInput('parks', [createPark()]);
    fixture.detectChanges();

    const parkDetails: HTMLDetailsElement | null =
      fixture.nativeElement.querySelector('.rating-tree__park');
    const sectionDetails: HTMLDetailsElement | null =
      fixture.nativeElement.querySelector('.rating-tree__section');

    expect(parkDetails).not.toBeNull();
    expect(sectionDetails).not.toBeNull();
    expect(parkDetails?.open).toBe(false);
    expect(sectionDetails?.open).toBe(false);
  });

  it('shows the same expand action on parks and sections', () => {
    fixture.componentRef.setInput('parks', [createPark()]);
    fixture.detectChanges();

    const actions: NodeListOf<HTMLElement> =
      fixture.nativeElement.querySelectorAll('.rating-tree__toggle-closed');

    expect(actions.length).toBe(2);
    expect(actions[0].textContent).toContain('ratings.tree.detailAction');
    expect(actions[1].textContent).toContain('ratings.tree.detailAction');
  });

  it('pluralizes the rating count with the label family supplied by the caller', () => {
    const translateService: TranslateService = TestBed.inject(TranslateService);
    translateService.setTranslation('en', {
      ratings: {
        profile: {
          ratingCount: {
            one: '{{count}} personal rating',
            other: '{{count}} personal ratings',
          },
        },
      },
    });
    translateService.use('en');
    fixture.componentRef.setInput(
      'ratingCountLabelKey',
      'ratings.profile.ratingCount',
    );
    fixture.componentRef.setInput('parks', [
      { ...createPark(), ratingCount: 1 },
    ]);
    fixture.detectChanges();

    const countLabel: HTMLElement | null = fixture.nativeElement.querySelector(
      '.rating-tree__park-main span',
    );

    expect(countLabel?.textContent?.trim()).toBe('1 personal rating');
  });

  it('shows rating counts for park metrics, sections, and items when provided', () => {
    const translateService: TranslateService = TestBed.inject(TranslateService);
    translateService.setTranslation('en', {
      ratings: {
        rankings: {
          ratingCount: {
            one: '{{count}} rating',
            other: '{{count}} ratings',
          },
        },
      },
    });
    translateService.use('en');
    fixture.componentRef.setInput('parks', [createPark()]);
    fixture.detectChanges();

    const metricCount: HTMLElement | null = fixture.nativeElement.querySelector(
      '.rating-tree__metric-count',
    );
    const sectionCount: HTMLElement | null =
      fixture.nativeElement.querySelector(
        '.rating-tree__section-main .rating-tree__rating-count',
      );
    const itemCount: HTMLElement | null = fixture.nativeElement.querySelector(
      '.rating-tree__item-main .rating-tree__rating-count',
    );

    expect(metricCount?.textContent?.trim()).toBe('2 ratings');
    expect(sectionCount?.textContent?.trim()).toBe('6 ratings');
    expect(itemCount?.textContent?.trim()).toBe('3 ratings');
  });

  it('renders a filled star layer with the score proportion', () => {
    fixture.componentRef.setInput('parks', [createPark()]);
    fixture.detectChanges();

    const stars: NodeListOf<HTMLElement> =
      fixture.nativeElement.querySelectorAll(
        '.rating-tree__park-summary .rating-tree__star',
      );
    const filledStars: NodeListOf<HTMLElement> =
      fixture.nativeElement.querySelectorAll(
        '.rating-tree__park-summary .rating-tree__star-filled',
      );

    expect(filledStars.length).toBe(5);
    expect(stars[0]?.style.getPropertyValue('--fill')).toBe('100%');
    expect(stars[3]?.style.getPropertyValue('--fill')).toBe('100%');
    expect(
      Number.parseFloat(stars[4]?.style.getPropertyValue('--fill') ?? '0'),
    ).toBeCloseTo(30);
  });

  it('emits rating changes when an editable star is selected', () => {
    const changes: Array<{
      ratingId: string;
      value: number;
    }> = [];
    component.ratingChange.subscribe(
      (change: { ratingId: string; value: number }): void => {
        changes.push(change);
      },
    );
    fixture.componentRef.setInput('parks', [createPark()]);
    fixture.detectChanges();

    const buttons: NodeListOf<HTMLButtonElement> =
      fixture.nativeElement.querySelectorAll(
        '.rating-tree__items .rating-tree__star-hit--right',
      );
    buttons[3]?.click();

    expect(changes).toEqual([{ ratingId: 'rating-item-1', value: 4 }]);
  });

  it('renders evidence without requiring it for existing tree consumers', () => {
    fixture.componentRef.setInput('parks', [
      {
        ...createPark(),
        rank: null,
        evidence: {
          evidence: {
            level: 'Provisional',
            isEligibleForMainRanking: false,
            nextThreshold: 10,
          },
          uniqueContributorCount: 7,
          ratingObservationCount: 9,
          targetType: 'Park',
          rank: null,
          methodologyVersion: 'ratings-2026-01',
          eligibilityThreshold: 10,
        },
      },
    ]);
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('.rating-tree__rank')).toBeNull();
    expect(
      fixture.nativeElement.querySelectorAll('app-rating-evidence'),
    ).toHaveLength(2);
  });
});

function createPark(): RatingTreePark {
  return {
    id: 'park-1',
    rank: 1,
    name: 'Phantasialand',
    score: 4.3,
    ratingCount: 8,
    route: ['/parks', 'park-1'],
    metrics: [
      {
        labelKey: 'ratings.rankings.parkSignal',
        value: 5,
        ratingCount: 2,
      },
    ],
    sections: [
      {
        id: 'Attraction',
        titleKey: 'ratings.categories.Attraction',
        score: 4.3,
        ratingCount: 6,
        items: [
          {
            id: 'item-1',
            name: 'Taron',
            score: 5,
            ratingCount: 3,
            route: ['/parks', 'park-1', 'items', 'item-1'],
            secondaryLabelKey: 'ratings.profile.communityAverage',
            secondaryScore: 4.8,
            editable: {
              ratingId: 'rating-item-1',
              saving: false,
            },
          },
        ],
      },
    ],
  };
}
