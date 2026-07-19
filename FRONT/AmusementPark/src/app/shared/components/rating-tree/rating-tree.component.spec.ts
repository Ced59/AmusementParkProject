import { ComponentFixture, TestBed } from '@angular/core/testing';
import { TranslateService } from '@ngx-translate/core';

import { COMMON_TEST_IMPORTS, provideCommonTestDependencies } from '@app/testing/common-test-providers';
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

    const parkDetails: HTMLDetailsElement | null = fixture.nativeElement.querySelector('.rating-tree__park');
    const sectionDetails: HTMLDetailsElement | null = fixture.nativeElement.querySelector('.rating-tree__section');

    expect(parkDetails).not.toBeNull();
    expect(sectionDetails).not.toBeNull();
    expect(parkDetails?.open).toBeFalse();
    expect(sectionDetails?.open).toBeFalse();
  });

  it('shows the same expand action on parks and sections', () => {
    fixture.componentRef.setInput('parks', [createPark()]);
    fixture.detectChanges();

    const actions: NodeListOf<HTMLElement> = fixture.nativeElement.querySelectorAll('.rating-tree__toggle-closed');

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
            other: '{{count}} personal ratings'
          }
        }
      }
    });
    translateService.use('en');
    fixture.componentRef.setInput('ratingCountLabelKey', 'ratings.profile.ratingCount');
    fixture.componentRef.setInput('parks', [{ ...createPark(), ratingCount: 1 }]);
    fixture.detectChanges();

    const countLabel: HTMLElement | null = fixture.nativeElement.querySelector('.rating-tree__park-main span');

    expect(countLabel?.textContent?.trim()).toBe('1 personal rating');
  });

  it('emits rating changes when an editable star is selected', () => {
    const changes: Array<{ ratingId: string; value: number }> = [];
    component.ratingChange.subscribe((change: { ratingId: string; value: number }): void => {
      changes.push(change);
    });
    fixture.componentRef.setInput('parks', [createPark()]);
    fixture.detectChanges();

    const buttons: NodeListOf<HTMLButtonElement> = fixture.nativeElement.querySelectorAll('.rating-tree__items .rating-tree__star-hit--right');
    buttons[3]?.click();

    expect(changes).toEqual([{ ratingId: 'rating-item-1', value: 4 }]);
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
    metrics: [{ labelKey: 'ratings.rankings.parkSignal', value: 5 }],
    sections: [
      {
        id: 'Attraction',
        titleKey: 'ratings.categories.Attraction',
        score: 4.3,
        items: [
          {
            id: 'item-1',
            name: 'Taron',
            score: 5,
            route: ['/parks', 'park-1', 'items', 'item-1'],
            secondaryLabelKey: 'ratings.profile.communityAverage',
            secondaryScore: 4.8,
            editable: {
              ratingId: 'rating-item-1',
              saving: false
            }
          }
        ]
      }
    ]
  };
}
