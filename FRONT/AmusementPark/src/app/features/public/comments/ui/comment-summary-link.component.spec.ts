import { signal, WritableSignal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { TranslateService } from '@ngx-translate/core';

import { CommentSummary } from '@app/models/comments/comment.models';
import {
  COMMON_TEST_IMPORTS,
  provideCommonTestDependencies,
} from '@app/testing/common-test-providers';
import { CommentSummaryStateFacade } from '../state/comment-summary-state.facade';
import { CommentSummaryLinkComponent } from './comment-summary-link.component';

class FakeCommentSummaryStateFacade {
  readonly summary: WritableSignal<CommentSummary | null> = signal<CommentSummary | null>({
    targetType: 'Park',
    targetId: 'park-1',
    commentCount: 0,
    languageCode: 'fr',
    languageCommentCount: 0,
    officialComment: null,
  });
  readonly canWrite: WritableSignal<boolean> = signal<boolean>(true);
  readonly initializeAuthorAccess = vi.fn();
  readonly load = vi.fn();
}

describe('CommentSummaryLinkComponent', () => {
  let fixture: ComponentFixture<CommentSummaryLinkComponent>;
  let facade: FakeCommentSummaryStateFacade;

  beforeEach(async () => {
    facade = new FakeCommentSummaryStateFacade();

    await TestBed.configureTestingModule({
      imports: [...COMMON_TEST_IMPORTS, CommentSummaryLinkComponent],
      providers: provideCommonTestDependencies(),
    })
      .overrideComponent(CommentSummaryLinkComponent, {
        set: {
          providers: [
            {
              provide: CommentSummaryStateFacade,
              useValue: facade,
            },
          ],
        },
      })
      .compileComponents();

    const translateService: TranslateService = TestBed.inject(TranslateService);
    translateService.setTranslation('fr', {
      comments: {
        summary: {
          createFirst: 'Écrire le premier commentaire',
          currentLanguage: 'Dans ta langue actuelle : {{language}}',
          otherLanguages: 'Disponible uniquement dans d’autres langues',
          count: {
            one: '{{count}} commentaire',
            other: '{{count}} commentaires',
          },
          allLanguagesCount: {
            one: '{{count}} commentaire, toutes langues confondues',
            other: '{{count}} commentaires, toutes langues confondues',
          },
        },
      },
    });
    translateService.use('fr');

    fixture = TestBed.createComponent(CommentSummaryLinkComponent);
    fixture.componentRef.setInput('targetType', 'Park');
    fixture.componentRef.setInput('targetId', 'park-1');
    fixture.componentRef.setInput(
      'commentsLink',
      ['/', 'fr', 'park', 'park-1', 'parc-test', 'comments'],
    );
    fixture.componentRef.setInput('currentLanguage', 'fr');
    fixture.detectChanges();
  });

  it('shows the first-comment link to an authorized author when there are no comments', () => {
    const link: HTMLAnchorElement | null =
      fixture.nativeElement.querySelector('.comment-summary-link--create');

    expect(link?.textContent?.trim()).toBe('Écrire le premier commentaire');
    expect(link?.getAttribute('href')).toBe(
      '/fr/park/park-1/parc-test/comments',
    );
    expect(facade.initializeAuthorAccess).toHaveBeenCalledTimes(1);
    expect(facade.load).toHaveBeenCalledWith('Park', 'park-1', 'fr');
  });

  it('keeps the empty comment thread hidden from visitors without author access', () => {
    facade.canWrite.set(false);
    fixture.detectChanges();

    expect(
      fixture.nativeElement.querySelector('.comment-summary-link'),
    ).toBeNull();
  });

  it('states when comments are available in the current language', () => {
    facade.summary.set({
      targetType: 'Park',
      targetId: 'park-1',
      commentCount: 3,
      languageCode: 'fr',
      languageCommentCount: 2,
      officialComment: null,
    });
    fixture.detectChanges();

    const text: string = fixture.nativeElement.textContent;
    expect(text).toContain('2 commentaires');
    expect(text).toContain('Dans ta langue actuelle : Français');
    expect(text).toContain('3 commentaires, toutes langues confondues');
  });

  it('states when comments only exist in other languages', () => {
    facade.summary.set({
      targetType: 'Park',
      targetId: 'park-1',
      commentCount: 1,
      languageCode: 'fr',
      languageCommentCount: 0,
      officialComment: null,
    });
    fixture.detectChanges();

    const text: string = fixture.nativeElement.textContent;
    expect(text).toContain('1 commentaire');
    expect(text).toContain('Disponible uniquement dans d’autres langues');
  });
});
