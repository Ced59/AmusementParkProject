import { ChangeDetectionStrategy, Component, DestroyRef, OnInit, Signal, effect, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Data, Router, RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { TranslateModule, TranslateService } from '@ngx-translate/core';

import {
  PublicHistoricalAmbiguity,
  PublicHistoricalAttribute,
  PublicHistoricalSubjectSnapshot,
  PublicParkHistoricalSnapshot
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
import { HistoryTimelinePageViewModel } from '../models/history-view.model';
import { ParkHistoryBreadcrumbSeoService } from '../state/park-history-breadcrumb-seo.service';
import { ParkHistoryExplorerStateFacade } from '../state/park-history-explorer-state.facade';
import {
  isValidHistoricalSnapshotDate,
  normalizeHistoricalSnapshotDay,
  resolveHistoricalSnapshotDays
} from '../utils/history-snapshot-date-selection';
import {
  buildPublicHistoricalAmbiguityTrackKey,
  usesCurrentHistoricalSubjectNameFallback
} from '../utils/historical-subject-display';
import {
  PARK_HISTORY_EXPLORER_ROUTE_DATA_KEY,
  ResolvedParkHistoricalSnapshotRouteData
} from '../state/park-history-explorer.resolver';

@Component({
  selector: 'app-park-historical-snapshot-page',
  templateUrl: './park-historical-snapshot-page.component.html',
  styleUrls: ['./park-history-explorer.shared.scss', './park-historical-snapshot-page.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [ParkHistoryExplorerStateFacade],
  imports: [FormsModule, PageStateComponent, RouterLink, TranslateModule]
})
export class ParkHistoricalSnapshotPageComponent implements OnInit {
  protected readonly state = this.stateFacade.snapshotState;
  protected readonly snapshot: Signal<PublicParkHistoricalSnapshot | undefined> = this.stateFacade.snapshot;
  protected readonly parkIdentity = this.stateFacade.parkIdentity;
  protected readonly knownOpenSubjects = this.stateFacade.knownOpenSubjects;
  protected readonly possiblyOpenSubjects = this.stateFacade.possiblyOpenSubjects;
  protected readonly uncertainSubjects = this.stateFacade.uncertainSubjects;
  protected readonly zones = this.stateFacade.zones;
  protected readonly currentLanguage = signal<string>('en');
  protected readonly months: number[] = Array.from({ length: 12 }, (_value: unknown, index: number): number => index + 1);
  protected readonly ambiguityKey = buildPublicHistoricalAmbiguityTrackKey;

  protected selectedYear = new Date().getUTCFullYear();
  protected selectedMonth: number | null = null;
  protected selectedDay: number | null = null;

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
      const currentSnapshot: PublicParkHistoricalSnapshot | undefined = this.snapshot();
      if (currentSnapshot) {
        this.applySeo(currentSnapshot);
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
        const resolved: ResolvedParkHistoricalSnapshotRouteData | undefined = data[PARK_HISTORY_EXPLORER_ROUTE_DATA_KEY];
        this.selectedYear = resolved?.year ?? this.selectedYear;
        this.selectedMonth = resolved?.month ?? null;
        this.selectedDay = resolved?.day ?? null;
        this.stateFacade.setResolvedSnapshot(resolved?.snapshot ?? null);
      });

    this.translationService.languageChanged
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((language: string): void => {
        this.currentLanguage.set(language);
        const currentSnapshot: PublicParkHistoricalSnapshot | undefined = this.snapshot();
        if (currentSnapshot) {
          this.applySeo(currentSnapshot);
        }
      });
  }

  protected openSelectedDate(snapshot: PublicParkHistoricalSnapshot): void {
    const year: number = Number(this.selectedYear);
    const month: number | null = this.selectedMonth ? Number(this.selectedMonth) : null;
    const day: number | null = this.selectedDay ? Number(this.selectedDay) : null;
    if (!isValidHistoricalSnapshotDate(year, month, day)) {
      return;
    }

    this.router.navigate(
      [...this.historyLink(snapshot), String(year)],
      { queryParams: { month, day } }
    );
  }

  protected updateSelectedMonth(value: number | null): void {
    this.selectedMonth = value === null ? null : Number(value);
    this.selectedDay = normalizeHistoricalSnapshotDay(
      Number(this.selectedYear),
      this.selectedMonth,
      this.selectedDay
    );
  }

  protected updateSelectedYear(value: number): void {
    this.selectedYear = Number(value);
    this.selectedDay = normalizeHistoricalSnapshotDay(
      this.selectedYear,
      this.selectedMonth,
      this.selectedDay
    );
  }

  protected availableDays(): number[] {
    return resolveHistoricalSnapshotDays(Number(this.selectedYear), this.selectedMonth);
  }

  protected parkLink(snapshot: PublicParkHistoricalSnapshot): string[] {
    return buildPublicParkRouteCommands({
      language: this.currentLanguage(),
      parkId: snapshot.parkId,
      parkName: snapshot.parkName
    }) ?? [];
  }

  protected historyLink(snapshot: PublicParkHistoricalSnapshot): string[] {
    return buildPublicParkHistoryRouteCommands({
      language: this.currentLanguage(),
      parkId: snapshot.parkId,
      parkName: snapshot.parkName
    }) ?? [];
  }

  protected usesCurrentNameFallback(
    subject: PublicHistoricalSubjectSnapshot | PublicHistoricalAmbiguity
  ): boolean {
    return usesCurrentHistoricalSubjectNameFallback(subject);
  }

  protected stateLabel(subject: PublicHistoricalSubjectSnapshot): string {
    return this.enumLabel('operationalStates', subject.operationalState, subject.operationalState);
  }

  protected stateClass(subject: PublicHistoricalSubjectSnapshot): string {
    return `history-badge--${subject.operationalState.toLowerCase()}`;
  }

  protected attributeLabel(attribute: PublicHistoricalAttribute): string {
    return this.enumLabel('attributes', attribute.kind, attribute.kind);
  }

  protected visibleAttributes(subject: PublicHistoricalSubjectSnapshot): PublicHistoricalAttribute[] {
    return subject.attributes.filter((attribute: PublicHistoricalAttribute): boolean =>
      attribute.kind !== 'Name'
      && attribute.isDisplayResolved
      && (!!attribute.displayValue || attribute.displayCandidates.length > 0)
    );
  }

  protected attributeValue(attribute: PublicHistoricalAttribute): string {
    return attribute.displayValue ?? attribute.displayCandidates.join(' · ');
  }

  protected monthLabel(month: number): string {
    return new Intl.DateTimeFormat(this.currentLanguage(), { month: 'long', timeZone: 'UTC' })
      .format(new Date(Date.UTC(2024, month - 1, 1)));
  }

  protected coverageStatus(snapshot: PublicParkHistoricalSnapshot): string {
    return this.enumLabel('coverageStatuses', snapshot.coverage.status, snapshot.coverage.status);
  }

  protected coveragePercent(value: number): string {
    return new Intl.NumberFormat(this.currentLanguage(), { maximumFractionDigits: 0 }).format(value);
  }

  protected totalSupportingSources(snapshot: PublicParkHistoricalSnapshot): number {
    return snapshot.subjects.reduce(
      (total: number, subject: PublicHistoricalSubjectSnapshot): number => total + subject.supportingSourceCount,
      0
    );
  }

  protected subjectKey(subject: PublicHistoricalSubjectSnapshot): string {
    return `${subject.subjectType}:${subject.subjectId}`;
  }

  private enumLabel(group: string, value: string, fallback: string): string {
    const key: string = `history.explorer.${group}.${value}`;
    const translated: string = this.translateService.instant(key);
    return translated === key ? fallback : translated;
  }

  private applySeo(snapshot: PublicParkHistoricalSnapshot): void {
    const historyCommands: string[] = this.historyLink(snapshot);
    const canonicalPath: string = buildPublicRoutePath([
      ...historyCommands,
      String(snapshot.requestedInstant.year)
    ]) ?? '/';
    this.seoService.applyHistoryTimelineSeo(
      this.toSeoViewModel(snapshot),
      this.currentLanguage(),
      this.router.url,
      canonicalPath
    );
    this.breadcrumbSeoService.apply(
      snapshot.parkId,
      snapshot.parkName,
      historyCommands[4],
      this.currentLanguage(),
      canonicalPath,
      snapshot.requestedInstant.year
    );
  }

  private toSeoViewModel(snapshot: PublicParkHistoricalSnapshot): HistoryTimelinePageViewModel {
    const park: Park = { id: snapshot.parkId, name: snapshot.parkName } as Park;
    return {
      entityType: 'Park',
      title: this.translateService.instant('history.explorer.snapshotTitle', {
        park: snapshot.parkName,
        year: snapshot.requestedInstant.year
      }),
      subtitle: this.translateService.instant('history.explorer.snapshotDescription', {
        year: snapshot.requestedInstant.year
      }),
      ownerName: snapshot.parkName,
      park,
      parkItem: null,
      includedParkItems: [],
      showParkItemControls: false,
      events: [],
      pagination: {
        currentPage: 1,
        itemsPerPage: 1,
        totalItems: 0,
        totalPages: 1
      },
      pageRanges: [],
      yearStart: snapshot.requestedInstant.year,
      yearEnd: snapshot.requestedInstant.year
    };
  }
}
