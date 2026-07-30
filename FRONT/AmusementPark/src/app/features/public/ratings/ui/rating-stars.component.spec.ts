import { signal, WritableSignal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { TranslateService } from '@ngx-translate/core';

import { RatingSummary } from '@app/models/ratings/rating.models';
import {
  COMMON_TEST_IMPORTS,
  provideCommonTestDependencies,
} from '@app/testing/common-test-providers';
import { PublicRatingStateFacade } from '../state/public-rating-state.facade';
import { RatingStarsComponent } from './rating-stars.component';

class FakePublicRatingStateFacade {
  readonly summary: WritableSignal<RatingSummary | null> = signal<RatingSummary | null>({
    targetType: 'ParkItem',
    targetId: 'item-1',
    ratingCount: 2,
    averageRating: 4,
    bayesianScore: 3.5,
    rank: 4,
  });
  readonly saving: WritableSignal<boolean> = signal<boolean>(false);
  readonly messageKey: WritableSignal<string | null> = signal<string | null>(null);
  readonly userRatingValue: WritableSignal<number | null> = signal<number | null>(3.5);
  readonly configure = vi.fn();
  readonly rate = vi.fn();
  readonly removeRating = vi.fn();
}

describe('RatingStarsComponent', () => {
  let fixture: ComponentFixture<RatingStarsComponent>;
  let facade: FakePublicRatingStateFacade;

  afterEach(() => {
    vi.restoreAllMocks();
  });

  beforeEach(async () => {
    facade = new FakePublicRatingStateFacade();

    await TestBed.configureTestingModule({
      imports: [...COMMON_TEST_IMPORTS, RatingStarsComponent],
      providers: provideCommonTestDependencies(),
    })
      .overrideComponent(RatingStarsComponent, {
        set: {
          providers: [
            {
              provide: PublicRatingStateFacade,
              useValue: facade,
            },
          ],
        },
      })
      .compileComponents();

    const translateService: TranslateService = TestBed.inject(TranslateService);
    translateService.setTranslation('fr', {
      ratings: {
        stars: {
          yourRating: 'Ta note : {{value}}/5',
          clearRating: 'Effacer ma note',
          clearRatingConfirm: 'Veux-tu vraiment effacer ta note ?',
          prompt: 'Choisis ta note',
          rankLabel: 'Classé #{{rank}}',
        },
      },
      publicCounts: {
        averageRating: {
          one: 'Note moyenne {{value}} sur 5',
          other: 'Note moyenne {{value}} sur 5',
        },
        rating: {
          one: '{{count}} note',
          other: '{{count}} notes',
        },
      },
    });
    translateService.use('fr');

    fixture = TestBed.createComponent(RatingStarsComponent);
    fixture.componentRef.setInput('targetType', 'ParkItem');
    fixture.componentRef.setInput('targetId', 'item-1');
    fixture.detectChanges();
  });

  it('shows the exact personal rating and offers to clear it', () => {
    const confirmSpy = vi.spyOn(globalThis, 'confirm').mockReturnValue(true);
    const message: HTMLElement | null =
      fixture.nativeElement.querySelector('.rating-stars__message');
    const clearButton: HTMLButtonElement | null =
      fixture.nativeElement.querySelector('.rating-stars__clear');

    expect(message?.textContent).toContain('Ta note : 3,5/5');
    expect(clearButton?.textContent?.trim()).toBe('Effacer ma note');

    clearButton?.click();

    expect(confirmSpy).toHaveBeenCalledWith(
      'Veux-tu vraiment effacer ta note ?',
    );
    expect(facade.removeRating).toHaveBeenCalledTimes(1);
  });

  it('shows the target place when it belongs to a ranking', () => {
    const rank: HTMLElement | null =
      fixture.nativeElement.querySelector('.rating-stars__rank');

    expect(rank?.textContent?.trim()).toBe('Classé #4');
  });

  it('keeps the rating when removal is not confirmed', () => {
    vi.spyOn(globalThis, 'confirm').mockReturnValue(false);
    const clearButton: HTMLButtonElement | null =
      fixture.nativeElement.querySelector('.rating-stars__clear');

    clearButton?.click();

    expect(facade.removeRating).not.toHaveBeenCalled();
  });

  it('formats the public and personal ratings with the active locale', () => {
    const translateService: TranslateService = TestBed.inject(TranslateService);
    translateService.setTranslation('en', {
      ratings: {
        stars: {
          averageLabel: 'Average rating',
          yourRating: 'Your rating: {{value}}/5',
          clearRating: 'Clear my rating',
        },
      },
      publicCounts: {
        averageRating: {
          one: 'Average rating {{value}} out of 5',
          other: 'Average rating {{value}} out of 5',
        },
        rating: {
          one: '{{count}} rating',
          other: '{{count}} ratings',
        },
      },
    });
    translateService.use('en');
    fixture.detectChanges();

    const average: HTMLElement | null =
      fixture.nativeElement.querySelector('.rating-stars__average');
    const message: HTMLElement | null =
      fixture.nativeElement.querySelector('.rating-stars__message');

    expect(average?.textContent?.trim()).toBe('4.0');
    expect(message?.textContent).toContain('Your rating: 3.5/5');
  });

  it('fills the interactive stars from the personal rating rather than the community average', () => {
    const stars: NodeListOf<HTMLElement> =
      fixture.nativeElement.querySelectorAll('.rating-stars__star');

    expect(stars[0]?.style.getPropertyValue('--fill')).toBe('100%');
    expect(stars[2]?.style.getPropertyValue('--fill')).toBe('100%');
    expect(stars[3]?.style.getPropertyValue('--fill')).toBe('50%');
    expect(stars[4]?.style.getPropertyValue('--fill')).toBe('0%');
  });

  it('keeps the personal control empty when the visitor has not rated the target', () => {
    facade.userRatingValue.set(null);
    fixture.detectChanges();

    const message: HTMLElement | null =
      fixture.nativeElement.querySelector('.rating-stars__message');
    const clearButton: HTMLButtonElement | null =
      fixture.nativeElement.querySelector('.rating-stars__clear');
    const stars: NodeListOf<HTMLElement> =
      fixture.nativeElement.querySelectorAll('.rating-stars__star');

    expect(message?.textContent?.trim()).toBe('Choisis ta note');
    expect(clearButton).toBeNull();
    expect(
      Array.from(stars).map((star: HTMLElement): string =>
        star.style.getPropertyValue('--fill'),
      ),
    ).toEqual(['0%', '0%', '0%', '0%', '0%']);
  });
});
