import { ChangeDetectionStrategy, Component, DestroyRef, OnInit, effect, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, ParamMap, Router, RouterLink } from '@angular/router';

import { TranslationService } from '@app/services/translation.service';
import { SeoService } from '@core/seo/seo.service';
import { ImageDisplayComponent } from '@shared/components/image-display/image-display.component';
import { PageStateComponent } from '@shared/components/page-state/page-state.component';
import { resolveLanguageFromActivatedRoute } from '@shared/utils/routing/route-language.utils';
import { HistoryArticleBlockViewModel, HistoryArticlePageViewModel } from '../models/history-view.model';
import { HistoryArticleStateFacade } from '../state/history-article-state.facade';

interface HistoryArticlePageCopy {
  back: string;
  sources: string;
}

const HISTORY_ARTICLE_PAGE_COPY: Record<string, HistoryArticlePageCopy> = {
  fr: { back: 'Retour à la timeline', sources: 'Sources' },
  en: { back: 'Back to timeline', sources: 'Sources' },
  de: { back: 'Zurück zur Zeitleiste', sources: 'Quellen' },
  nl: { back: 'Terug naar de tijdlijn', sources: 'Bronnen' },
  it: { back: 'Torna alla timeline', sources: 'Fonti' },
  es: { back: 'Volver a la cronología', sources: 'Fuentes' },
  pl: { back: 'Wróć do osi czasu', sources: 'Źródła' },
  pt: { back: 'Voltar à linha do tempo', sources: 'Fontes' }
};

@Component({
  selector: 'app-history-article-page',
  templateUrl: './history-article-page.component.html',
  styleUrls: ['./history-article-page.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [HistoryArticleStateFacade],
  imports: [PageStateComponent, ImageDisplayComponent, RouterLink]
})
export class HistoryArticlePageComponent implements OnInit {
  protected readonly state = this.stateFacade.state;
  protected readonly article = this.stateFacade.article;
  protected readonly currentLanguage = signal<string>('en');

  private readonly destroyRef: DestroyRef = inject(DestroyRef);

  constructor(
    private readonly route: ActivatedRoute,
    private readonly router: Router,
    private readonly translationService: TranslationService,
    private readonly stateFacade: HistoryArticleStateFacade,
    private readonly seoService: SeoService
  ) {
    effect((): void => {
      const currentArticle: HistoryArticlePageViewModel | null = this.article();

      if (!currentArticle) {
        return;
      }

      this.seoService.applyHistoryArticleSeo(currentArticle, this.currentLanguage(), this.router.url);
    });
  }

  ngOnInit(): void {
    const initialLanguage: string = resolveLanguageFromActivatedRoute(this.route, this.translationService.getCurrentLang() || 'en');
    this.currentLanguage.set(initialLanguage);
    this.stateFacade.setCurrentLanguage(initialLanguage);

    this.route.paramMap.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((params: ParamMap): void => {
      const eventId: string | null = params.get('eventId');

      if (!eventId) {
        return;
      }

      this.stateFacade.loadArticle(eventId);
    });

    this.translationService.languageChanged.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((language: string): void => {
      this.currentLanguage.set(language);
      this.stateFacade.setCurrentLanguage(language);
    });
  }

  protected isHeading(block: HistoryArticleBlockViewModel): boolean {
    return block.type === 'Heading';
  }

  protected isImage(block: HistoryArticleBlockViewModel): boolean {
    return block.type === 'Image' || block.type === 'Gallery';
  }

  protected isQuote(block: HistoryArticleBlockViewModel): boolean {
    return block.type === 'Quote';
  }

  protected isFactBox(block: HistoryArticleBlockViewModel): boolean {
    return block.type === 'FactBox';
  }

  protected backLabel(): string {
    return this.resolveCopy().back;
  }

  protected sourcesLabel(): string {
    return this.resolveCopy().sources;
  }

  private resolveCopy(): HistoryArticlePageCopy {
    return HISTORY_ARTICLE_PAGE_COPY[this.currentLanguage()] ?? HISTORY_ARTICLE_PAGE_COPY['en'];
  }
}
