import { ChangeDetectionStrategy, Component, DestroyRef, OnInit, effect, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, ParamMap, Router, RouterLink } from '@angular/router';

import { TranslationService } from '@app/services/translation.service';
import { SeoService } from '@core/seo/seo.service';
import { ImageDisplayComponent } from '@shared/components/image-display/image-display.component';
import { PageStateComponent } from '@shared/components/page-state/page-state.component';
import { resolveLanguageFromActivatedRoute } from '@shared/utils/routing/route-language.utils';
import { HistoryTimelineEventViewModel, HistoryTimelinePageViewModel } from '../models/history-view.model';
import { HistoryTimelineStateFacade } from '../state/history-timeline-state.facade';

interface HistoryTimelinePageCopy {
  article: string;
  articleAriaPrefix: string;
  back: string;
  events: string;
  includeParkItems: string;
  kicker: string;
  latest: string;
  sourcePlural: string;
  sourceSingular: string;
  start: string;
  summaryAria: string;
  timelineAria: string;
}

const HISTORY_TIMELINE_PAGE_COPY: Record<string, HistoryTimelinePageCopy> = {
  fr: {
    article: 'Article',
    articleAriaPrefix: 'Lire l’article',
    back: 'Retour',
    events: 'Événements',
    includeParkItems: 'Inclure les éléments du parc',
    kicker: 'Histoire',
    latest: 'Dernier jalon',
    sourcePlural: 'sources',
    sourceSingular: 'source',
    start: 'Début',
    summaryAria: 'Résumé de la timeline',
    timelineAria: 'Timeline historique'
  },
  en: {
    article: 'Article',
    articleAriaPrefix: 'Read article',
    back: 'Back',
    events: 'Events',
    includeParkItems: 'Include park items',
    kicker: 'History',
    latest: 'Latest',
    sourcePlural: 'sources',
    sourceSingular: 'source',
    start: 'Start',
    summaryAria: 'Timeline summary',
    timelineAria: 'History timeline'
  },
  de: {
    article: 'Artikel',
    articleAriaPrefix: 'Artikel lesen',
    back: 'Zurück',
    events: 'Ereignisse',
    includeParkItems: 'Elemente des Parks einblenden',
    kicker: 'Geschichte',
    latest: 'Letzter Meilenstein',
    sourcePlural: 'Quellen',
    sourceSingular: 'Quelle',
    start: 'Beginn',
    summaryAria: 'Zusammenfassung der Zeitleiste',
    timelineAria: 'Historische Zeitleiste'
  },
  nl: {
    article: 'Artikel',
    articleAriaPrefix: 'Artikel lezen',
    back: 'Terug',
    events: 'Gebeurtenissen',
    includeParkItems: 'Parkitems tonen',
    kicker: 'Geschiedenis',
    latest: 'Laatste mijlpaal',
    sourcePlural: 'bronnen',
    sourceSingular: 'bron',
    start: 'Begin',
    summaryAria: 'Samenvatting van de tijdlijn',
    timelineAria: 'Historische tijdlijn'
  },
  it: {
    article: 'Articolo',
    articleAriaPrefix: 'Leggi l’articolo',
    back: 'Indietro',
    events: 'Eventi',
    includeParkItems: 'Mostra gli elementi del parco',
    kicker: 'Storia',
    latest: 'Ultima tappa',
    sourcePlural: 'fonti',
    sourceSingular: 'fonte',
    start: 'Inizio',
    summaryAria: 'Riepilogo della timeline',
    timelineAria: 'Timeline storica'
  },
  es: {
    article: 'Artículo',
    articleAriaPrefix: 'Leer el artículo',
    back: 'Volver',
    events: 'Eventos',
    includeParkItems: 'Mostrar elementos del parque',
    kicker: 'Historia',
    latest: 'Último hito',
    sourcePlural: 'fuentes',
    sourceSingular: 'fuente',
    start: 'Inicio',
    summaryAria: 'Resumen de la cronología',
    timelineAria: 'Cronología histórica'
  },
  pl: {
    article: 'Artykuł',
    articleAriaPrefix: 'Przeczytaj artykuł',
    back: 'Wróć',
    events: 'Wydarzenia',
    includeParkItems: 'Pokaż elementy parku',
    kicker: 'Historia',
    latest: 'Ostatni punkt',
    sourcePlural: 'źródła',
    sourceSingular: 'źródło',
    start: 'Początek',
    summaryAria: 'Podsumowanie osi czasu',
    timelineAria: 'Historyczna oś czasu'
  },
  pt: {
    article: 'Artigo',
    articleAriaPrefix: 'Ler o artigo',
    back: 'Voltar',
    events: 'Eventos',
    includeParkItems: 'Mostrar itens do parque',
    kicker: 'História',
    latest: 'Marco mais recente',
    sourcePlural: 'fontes',
    sourceSingular: 'fonte',
    start: 'Início',
    summaryAria: 'Resumo da linha do tempo',
    timelineAria: 'Linha do tempo histórica'
  }
};

@Component({
  selector: 'app-history-timeline-page',
  templateUrl: './history-timeline-page.component.html',
  styleUrls: ['./history-timeline-page.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [HistoryTimelineStateFacade],
  imports: [PageStateComponent, ImageDisplayComponent, RouterLink]
})
export class HistoryTimelinePageComponent implements OnInit {
  protected readonly state = this.stateFacade.state;
  protected readonly timeline = this.stateFacade.timeline;
  protected readonly includeParkItems = this.stateFacade.includeParkItems;
  protected readonly currentLanguage = signal<string>('en');

  private readonly destroyRef: DestroyRef = inject(DestroyRef);

  constructor(
    private readonly route: ActivatedRoute,
    private readonly router: Router,
    private readonly translationService: TranslationService,
    private readonly stateFacade: HistoryTimelineStateFacade,
    private readonly seoService: SeoService
  ) {
    effect((): void => {
      const currentTimeline: HistoryTimelinePageViewModel | null = this.timeline();

      if (!currentTimeline) {
        return;
      }

      this.seoService.applyHistoryTimelineSeo(currentTimeline, this.currentLanguage(), this.router.url);
    });
  }

  ngOnInit(): void {
    const initialLanguage: string = resolveLanguageFromActivatedRoute(this.route, this.translationService.getCurrentLang() || 'en');
    this.currentLanguage.set(initialLanguage);
    this.stateFacade.setCurrentLanguage(initialLanguage);

    this.route.paramMap.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((params: ParamMap): void => {
      const parkItemId: string | null = params.get('itemId');
      const parkId: string | null = params.get('id');

      if (parkItemId) {
        this.stateFacade.loadParkItemTimeline(parkItemId);
        return;
      }

      if (parkId) {
        this.stateFacade.loadParkTimeline(parkId, this.includeParkItems());
      }
    });

    this.translationService.languageChanged.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((language: string): void => {
      this.currentLanguage.set(language);
      this.stateFacade.setCurrentLanguage(language);
    });
  }

  protected toggleParkItems(): void {
    this.stateFacade.setIncludeParkItems(!this.includeParkItems());
  }

  protected backLink(timeline: HistoryTimelinePageViewModel): string[] {
    if (timeline.parkItem && timeline.park) {
      return ['/', this.currentLanguage(), 'park', timeline.park.id ?? '', this.slugOrFallback(timeline.park.name, 'park'), 'item', timeline.parkItem.id ?? '', this.slugOrFallback(timeline.parkItem.name, 'item')];
    }

    if (timeline.park) {
      return ['/', this.currentLanguage(), 'park', timeline.park.id ?? '', this.slugOrFallback(timeline.park.name, 'park')];
    }

    return ['/', this.currentLanguage(), 'parks'];
  }

  protected yearTicks(timeline: HistoryTimelinePageViewModel): number[] {
    const range: number = Math.max(1, timeline.yearEnd - timeline.yearStart);
    const rawStep: number = Math.max(1, Math.ceil(range / 5));
    const step: number = rawStep <= 2 ? rawStep : Math.ceil(rawStep / 5) * 5;
    const years: number[] = [];

    for (let year: number = timeline.yearStart; year <= timeline.yearEnd; year += step) {
      years.push(year);
    }

    if (!years.includes(timeline.yearEnd)) {
      years.push(timeline.yearEnd);
    }

    return years;
  }

  protected eventTrackBy(_index: number, event: HistoryTimelineEventViewModel): string {
    return event.id;
  }

  protected articleAriaLabel(event: HistoryTimelineEventViewModel): string {
    return `${this.resolveCopy().articleAriaPrefix} ${event.title}`;
  }

  protected includeParkItemsLabel(): string {
    return this.resolveCopy().includeParkItems;
  }

  protected backLabel(): string {
    return this.resolveCopy().back;
  }

  protected kickerLabel(): string {
    return this.resolveCopy().kicker;
  }

  protected eventsLabel(): string {
    return this.resolveCopy().events;
  }

  protected startLabel(): string {
    return this.resolveCopy().start;
  }

  protected latestLabel(): string {
    return this.resolveCopy().latest;
  }

  protected articleLabel(): string {
    return this.resolveCopy().article;
  }

  protected summaryAriaLabel(): string {
    return this.resolveCopy().summaryAria;
  }

  protected timelineAriaLabel(): string {
    return this.resolveCopy().timelineAria;
  }

  protected sourceLabel(event: HistoryTimelineEventViewModel): string {
    const copy: HistoryTimelinePageCopy = this.resolveCopy();
    const label: string = event.sourceCount === 1 ? copy.sourceSingular : copy.sourcePlural;
    return `${event.sourceCount} ${label}`;
  }

  private resolveCopy(): HistoryTimelinePageCopy {
    return HISTORY_TIMELINE_PAGE_COPY[this.currentLanguage()] ?? HISTORY_TIMELINE_PAGE_COPY['en'];
  }

  private slugOrFallback(value: string | null | undefined, fallback: string): string {
    const normalizedValue: string = value?.trim().toLowerCase() ?? '';
    const slug: string = normalizedValue
      .normalize('NFD')
      .replace(/[\u0300-\u036f]/g, '')
      .replace(/[^a-z0-9]+/g, '-')
      .replace(/^-+|-+$/g, '');

    return slug || fallback;
  }
}
