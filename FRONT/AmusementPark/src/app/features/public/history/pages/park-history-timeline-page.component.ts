import { ChangeDetectionStrategy, Component, DestroyRef, OnInit, Signal, effect, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Data, Router, RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { TranslateModule, TranslateService } from '@ngx-translate/core';

import {
  PublicHistoricalTimelineEntry,
  PublicParkHistoricalTimeline
} from '@app/models/history/public-park-history.models';
import { Park } from '@app/models/parks/park';
import { TranslationService } from '@app/services/translation.service';
import { SeoService } from '@core/seo/seo.service';
import { PageStateComponent } from '@shared/components/page-state/page-state.component';
import {
  buildPublicParkHistoryRouteCommands,
  buildPublicParkRouteCommands,
  buildPublicRoutePath
} from '@shared/utils/routing/public-detail-route.helpers';
import { resolveLanguageFromActivatedRoute } from '@shared/utils/routing/route-language.utils';
import { PublicSharePanelComponent } from '@ui/sharing/public-share-panel/public-share-panel.component';
import { HistoryTimelineEventViewModel, HistoryTimelinePageRangeViewModel, HistoryTimelinePageViewModel } from '../models/history-view.model';
import { ParkHistoryBreadcrumbSeoService } from '../state/park-history-breadcrumb-seo.service';
import { ParkHistoryExplorerStateFacade } from '../state/park-history-explorer-state.facade';
import { resolveHistoryEventTypeLabel } from '../utils/history-event-labels';
import { formatPublicHistoricalPeriod } from '../utils/historical-period-label';
import { resolveDisplayedHistoryPageCount } from '../utils/history-pagination-display';
import { buildCanonicalHistoryNarrativeLink } from '../utils/history-narrative-link';
import {
  PARK_HISTORY_EXPLORER_ROUTE_DATA_KEY,
  ResolvedParkHistoryTimelineRouteData
} from '../state/park-history-explorer.resolver';

@Component({
  selector: 'app-park-history-timeline-page',
  templateUrl: './park-history-timeline-page.component.html',
  styleUrls: ['./park-history-explorer.shared.scss', './park-history-timeline-page.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [ParkHistoryExplorerStateFacade],
  imports: [FormsModule, PageStateComponent, PublicSharePanelComponent, RouterLink, TranslateModule]
})
export class ParkHistoryTimelinePageComponent implements OnInit {
  protected readonly state = this.stateFacade.timelineState;
  protected readonly timeline: Signal<PublicParkHistoricalTimeline | undefined> = this.stateFacade.timeline;
  protected readonly currentLanguage = signal<string>('en');
  protected requestedYear: number | null = null;

  private readonly currentPage = signal<number>(1);
  constructor(
    private readonly route: ActivatedRoute,
    private readonly router: Router,
    private readonly stateFacade: ParkHistoryExplorerStateFacade,
    private readonly translationService: TranslationService,
    private readonly translateService: TranslateService,
    private readonly seoService: SeoService,
    private readonly breadcrumbSeoService: ParkHistoryBreadcrumbSeoService,
    private readonly destroyRef: DestroyRef
  ) {
    effect((): void => {
      const currentTimeline: PublicParkHistoricalTimeline | undefined = this.timeline();
      if (currentTimeline) {
        this.applySeo(currentTimeline);
      }
    });
  }

  ngOnInit(): void {
    const initialLanguage: string = resolveLanguageFromActivatedRoute(
      this.route,
      this.translationService.getCurrentLang() || 'en'
    );
    this.currentLanguage.set(initialLanguage);

    this.route.data
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((data: Data): void => {
        const resolved: ResolvedParkHistoryTimelineRouteData | undefined = data[PARK_HISTORY_EXPLORER_ROUTE_DATA_KEY];
        this.currentPage.set(resolved?.page ?? 1);
        this.stateFacade.setResolvedTimeline(resolved?.timeline ?? null);
        this.requestedYear = this.latestYear(resolved?.timeline ?? null);
      });

    this.translationService.languageChanged
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((language: string): void => {
        this.currentLanguage.set(language);
        const currentTimeline: PublicParkHistoricalTimeline | undefined = this.timeline();
        if (currentTimeline) {
          this.applySeo(currentTimeline);
        }
      });
  }

  protected openYear(): void {
    const currentTimeline: PublicParkHistoricalTimeline | undefined = this.timeline();
    const year: number = Number(this.requestedYear);
    if (!currentTimeline || !Number.isInteger(year) || year < 1000 || year > 9999) {
      return;
    }

    this.router.navigate([...this.historyBaseLink(currentTimeline), String(year)]);
  }

  protected parkLink(timeline: PublicParkHistoricalTimeline): string[] {
    return buildPublicParkRouteCommands({
      language: this.currentLanguage(),
      parkId: timeline.parkId,
      parkName: timeline.parkName
    }) ?? [];
  }

  protected historyLink(timeline: PublicParkHistoricalTimeline): string[] {
    return buildPublicParkHistoryRouteCommands({
      language: this.currentLanguage(),
      parkId: timeline.parkId,
      parkName: timeline.parkName
    }) ?? [];
  }

  protected narrativeLink(entry: PublicHistoricalTimelineEntry, timeline: PublicParkHistoricalTimeline): string[] | null {
    return buildCanonicalHistoryNarrativeLink(entry, timeline, this.currentLanguage());
  }

  protected displayedPageCount(timeline: PublicParkHistoricalTimeline): number {
    return resolveDisplayedHistoryPageCount(timeline.pagination.totalPages);
  }

  protected pageLink(timeline: PublicParkHistoricalTimeline, page: number): string[] {
    return page <= 1
      ? this.historyBaseLink(timeline)
      : [...this.historyBaseLink(timeline), 'page', String(page)];
  }

  protected previousPage(timeline: PublicParkHistoricalTimeline): string[] {
    return this.pageLink(timeline, Math.max(1, timeline.pagination.currentPage - 1));
  }

  protected nextPage(timeline: PublicParkHistoricalTimeline): string[] {
    return this.pageLink(timeline, Math.min(timeline.pagination.totalPages, timeline.pagination.currentPage + 1));
  }

  protected eventDate(entry: PublicHistoricalTimelineEntry): string {
    return formatPublicHistoricalPeriod(
      entry.period,
      this.currentLanguage(),
      (key: string, parameters?: Record<string, string>): string => this.translateService.instant(key, parameters)
    );
  }

  protected factTypeLabel(entry: PublicHistoricalTimelineEntry): string {
    return entry.factType === 'Other' && entry.otherTypeLabel
      ? entry.otherTypeLabel
      : resolveHistoryEventTypeLabel(entry.factType, this.currentLanguage());
  }

  protected evidenceLabel(entry: PublicHistoricalTimelineEntry): string {
    return this.enumLabel('evidence', entry.evidenceState, entry.evidenceState);
  }

  protected evidenceClass(entry: PublicHistoricalTimelineEntry): string {
    return `history-badge--${entry.evidenceState.toLowerCase()}`;
  }

  protected uncertainty(entry: PublicHistoricalTimelineEntry): string | null {
    const language: string = this.currentLanguage();
    return entry.uncertaintyExplanations.find((text) => text.languageCode === language)?.value
      ?? entry.uncertaintyExplanations.find((text) => text.languageCode === 'en')?.value
      ?? entry.uncertaintyExplanations[0]?.value
      ?? null;
  }

  protected hasTransition(entry: PublicHistoricalTimelineEntry): boolean {
    return !!entry.previousDisplayValue || !!entry.nextDisplayValue;
  }

  protected eventKey(entry: PublicHistoricalTimelineEntry, index: number): string {
    return `${entry.subjectType}:${entry.subjectId}:${entry.factType}:${entry.period.start?.year ?? 'unknown'}:${index}`;
  }

  private historyBaseLink(timeline: PublicParkHistoricalTimeline): string[] {
    return this.historyLink(timeline);
  }

  private latestYear(timeline: PublicParkHistoricalTimeline | null): number | null {
    const years: number[] = (timeline?.events ?? [])
      .flatMap((event: PublicHistoricalTimelineEntry) => [event.period.start?.year, event.period.end?.year])
      .filter((year): year is number => Number.isInteger(year));
    return years.length > 0 ? Math.max(...years) : new Date().getUTCFullYear();
  }

  private enumLabel(group: string, value: string, fallback: string): string {
    const key: string = `history.explorer.${group}.${value}`;
    const translated: string = this.translateService.instant(key);
    return translated === key ? fallback : translated;
  }

  private applySeo(timeline: PublicParkHistoricalTimeline): void {
    const canonicalCommands: string[] = this.historyLink(timeline);
    const canonicalPath: string = this.currentPage() <= 1
      ? buildPublicRoutePath(canonicalCommands) ?? '/'
      : buildPublicRoutePath([...canonicalCommands, 'page', String(this.currentPage())]) ?? '/';
    this.seoService.applyHistoryTimelineSeo(
      this.toSeoViewModel(timeline),
      this.currentLanguage(),
      this.router.url,
      canonicalPath
    );
    this.breadcrumbSeoService.apply(
      timeline.parkId,
      timeline.parkName,
      canonicalCommands[4],
      this.currentLanguage(),
      canonicalPath
    );
  }

  private toSeoViewModel(timeline: PublicParkHistoricalTimeline): HistoryTimelinePageViewModel {
    const events: HistoryTimelineEventViewModel[] = timeline.events.map(
      (entry: PublicHistoricalTimelineEntry, index: number): HistoryTimelineEventViewModel => ({
        id: this.eventKey(entry, index),
        key: this.eventKey(entry, index),
        title: entry.subjectLabel,
        summary: this.uncertainty(entry) ?? '',
        dateLabel: this.eventDate(entry),
        year: entry.period.start?.year ?? entry.period.end?.year ?? 1,
        month: entry.period.start?.month ?? entry.period.end?.month ?? null,
        day: entry.period.start?.day ?? entry.period.end?.day ?? null,
        eventType: entry.factType,
        eventTypeLabel: this.factTypeLabel(entry),
        entityType: 'Park',
        isMajor: entry.importance === 'Major',
        ownerName: timeline.parkName,
        contextParkName: timeline.parkName,
        parkItemName: entry.subjectType === 'ParkItem' ? entry.subjectLabel : null,
        mainImageId: null,
        mainImage: null,
        articleLink: null,
        sourceCount: entry.sources.length,
        positionPercent: 0,
        isFirstInYear: index === 0
          || (timeline.events[index - 1]?.period.start?.year ?? timeline.events[index - 1]?.period.end?.year ?? 1)
            !== (entry.period.start?.year ?? entry.period.end?.year ?? 1)
      })
    );
    const years: number[] = events.map((event: HistoryTimelineEventViewModel): number => event.year).filter((year: number): boolean => year > 1);
    const pageRange: HistoryTimelinePageRangeViewModel = {
      page: timeline.pagination.currentPage,
      label: years.length > 0 ? `${Math.min(...years)}–${Math.max(...years)}` : String(timeline.pagination.currentPage),
      startYear: years.length > 0 ? Math.min(...years) : 1,
      endYear: years.length > 0 ? Math.max(...years) : 1,
      eventCount: events.length,
      isCurrent: true
    };
    const park: Park = { id: timeline.parkId, name: timeline.parkName } as Park;
    return {
      entityType: 'Park',
      title: this.translateService.instant('history.explorer.timelineTitle', { park: timeline.parkName }),
      subtitle: this.translateService.instant('history.explorer.timelineDescription'),
      ownerName: timeline.parkName,
      park,
      parkItem: null,
      includedParkItems: [],
      showParkItemControls: false,
      events,
      pagination: timeline.pagination,
      pageRanges: [pageRange],
      yearStart: pageRange.startYear,
      yearEnd: pageRange.endYear
    };
  }
}
