import { ChangeDetectionStrategy, Component, DestroyRef, OnInit, effect, inject, signal } from '@angular/core';
import { ActivatedRoute, ParamMap, Router, RouterLink } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { forkJoin } from 'rxjs';
import { TranslateModule, TranslateService as NgxTranslateService } from '@ngx-translate/core';

import { Park } from '@app/models/parks/park';
import { ParkDetailSummary } from '@app/models/parks/park-detail-summary';
import { ParkOpeningHoursCalendar, ParkOpeningHoursDay, ParkOpeningHoursTimeRange } from '@app/models/parks/park-opening-hours';
import { ParksApiService } from '@data-access/parks/parks-api.service';
import { SeoService } from '@core/seo/seo.service';
import { SsrHttpStatusService } from '@core/ssr/ssr-http-status.service';
import { anonymousHttpOptions } from '@core/http/auth/anonymous-http-options';
import { hasHttpStatus } from '@core/http/http-error-status.helpers';
import { TranslationService } from '@app/services/translation.service';
import { PageStateComponent } from '@shared/components/page-state/page-state.component';
import { SignalScreenStateStore } from '@shared/state/signal-screen-state.store';
import { resolveLanguageFromActivatedRoute } from '@shared/utils/routing/route-language.utils';
import {
  buildPublicParkOpeningHoursRouteCommands,
  buildPublicParkRouteCommands,
  buildPublicRoutePath
} from '@shared/utils/routing/public-detail-route.helpers';
import { SafeExternalUrlPipe } from '@shared/pipes';
import { UiButtonDirective } from '@ui/primitives';

interface ParkOpeningHoursPageData {
  park: Park;
  parkImageId: string | null;
  calendar: ParkOpeningHoursCalendar;
}

interface ParkOpeningHoursMonthGroup {
  key: string;
  label: string;
  openDays: number;
  closedDays: number;
  weeks: ParkOpeningHoursCalendarWeek[];
}

interface ParkOpeningHoursCalendarWeek {
  key: string;
  cells: ParkOpeningHoursCalendarCell[];
}

interface ParkOpeningHoursCalendarCell {
  key: string;
  localDate: string | null;
  dayNumber: number | null;
  day: ParkOpeningHoursDay | null;
  tone: ParkOpeningHoursDayTone;
  isToday: boolean;
}

type ParkOpeningHoursDayTone = 'empty' | 'closed' | 'short' | 'standard' | 'long';

@Component({
  selector: 'app-park-opening-hours-page',
  templateUrl: './park-opening-hours-page.component.html',
  styleUrls: ['./park-opening-hours-page.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [PageStateComponent, RouterLink, TranslateModule, SafeExternalUrlPipe, UiButtonDirective]
})
export class ParkOpeningHoursPageComponent implements OnInit {
  private readonly stateStore = new SignalScreenStateStore<ParkOpeningHoursPageData>();

  protected readonly state = this.stateStore.state;
  protected readonly currentLanguage = signal<string>('en');
  protected readonly detailLink = signal<string[] | null>(null);
  protected readonly weekDayLabels = signal<string[]>([]);

  private readonly destroyRef: DestroyRef = inject(DestroyRef);
  private currentParkId: string | null = null;
  private monthGroupsCache: {
    days: ParkOpeningHoursDay[];
    language: string;
    groups: ParkOpeningHoursMonthGroup[];
  } | null = null;

  constructor(
    private readonly route: ActivatedRoute,
    private readonly router: Router,
    private readonly translationService: TranslationService,
    private readonly ngxTranslateService: NgxTranslateService,
    private readonly parksApiService: ParksApiService,
    private readonly seoService: SeoService,
    private readonly ssrHttpStatusService: SsrHttpStatusService
  ) {
    effect((): void => {
      const currentData: ParkOpeningHoursPageData | undefined = this.stateStore.data();
      if (!currentData) {
        return;
      }

      const routeTarget = {
        language: this.currentLanguage(),
        parkId: currentData.park.id,
        parkName: currentData.park.name
      };

      this.detailLink.set(buildPublicParkRouteCommands(routeTarget));
      this.seoService.applyParkOpeningHoursSeo(
        currentData.park.name ?? 'Park',
        this.currentLanguage(),
        this.router.url,
        currentData.calendar.days.length,
        currentData.parkImageId,
        buildPublicRoutePath(buildPublicParkOpeningHoursRouteCommands(routeTarget)));
    });
  }

  ngOnInit(): void {
    const initialLanguage: string = resolveLanguageFromActivatedRoute(this.route, this.translationService.getCurrentLang() || 'en');
    this.currentLanguage.set(initialLanguage);
    this.weekDayLabels.set(this.buildWeekDayLabels(initialLanguage));

    this.translationService.languageChanged.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((language: string) => {
      this.currentLanguage.set(language);
      this.weekDayLabels.set(this.buildWeekDayLabels(language));
    });

    this.route.paramMap.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((params: ParamMap) => {
      const parkId: string | null = params.get('id');
      if (!parkId || parkId === this.currentParkId) {
        return;
      }

      this.currentParkId = parkId;
      this.loadOpeningHoursPage(parkId);
    });
  }

  monthGroups(days: ParkOpeningHoursDay[]): ParkOpeningHoursMonthGroup[] {
    const language: string = this.currentLanguage();
    if (this.monthGroupsCache?.days === days && this.monthGroupsCache.language === language) {
      return this.monthGroupsCache.groups;
    }

    const groupsByKey: Map<string, ParkOpeningHoursDay[]> = new Map<string, ParkOpeningHoursDay[]>();
    for (const day of days) {
      const key: string = day.localDate.slice(0, 7);
      const groupDays: ParkOpeningHoursDay[] = groupsByKey.get(key) ?? [];
      groupDays.push(day);
      groupsByKey.set(key, groupDays);
    }

    const groups: ParkOpeningHoursMonthGroup[] = [...groupsByKey.entries()]
      .sort(([firstKey]: [string, ParkOpeningHoursDay[]], [secondKey]: [string, ParkOpeningHoursDay[]]): number => firstKey.localeCompare(secondKey))
      .map(([key, groupDays]: [string, ParkOpeningHoursDay[]]): ParkOpeningHoursMonthGroup => this.buildMonthGroup(key, groupDays));
    this.monthGroupsCache = {
      days,
      language,
      groups
    };

    return groups;
  }

  monthSummaryKey(group: ParkOpeningHoursMonthGroup): string {
    if (group.openDays === 0) {
      return group.closedDays <= 1 ? 'parkOpeningHours.page.monthClosedOne' : 'parkOpeningHours.page.monthClosedMany';
    }

    if (group.closedDays === 0) {
      return group.openDays <= 1 ? 'parkOpeningHours.page.monthOpenOne' : 'parkOpeningHours.page.monthOpenMany';
    }

    return 'parkOpeningHours.page.monthMixed';
  }

  formatDate(value: string): string {
    return new Intl.DateTimeFormat(this.currentLanguage(), {
      weekday: 'long',
      year: 'numeric',
      month: 'long',
      day: 'numeric'
    }).format(new Date(`${value}T12:00:00`));
  }

  formatRange(range: ParkOpeningHoursTimeRange): string {
    return `${range.opensAt} - ${range.closesAt}${range.closesNextDay ? ' (+1)' : ''}`;
  }

  formatRanges(day: ParkOpeningHoursDay | null): string {
    if (!day || this.isClosed(day)) {
      return '';
    }

    return day.timeRanges
      .map((range: ParkOpeningHoursTimeRange): string => this.formatRange(range))
      .join(' / ');
  }

  formatCompactRanges(day: ParkOpeningHoursDay | null): string {
    if (!day || this.isClosed(day)) {
      return '';
    }

    return day.timeRanges
      .map((range: ParkOpeningHoursTimeRange): string => this.formatCompactRange(range))
      .join(' / ');
  }

  publicDayNote(day: ParkOpeningHoursDay | null): string | null {
    if (!day || this.isClosed(day)) {
      return null;
    }

    const note: string | null = (day.label || day.reason || '').trim() || null;
    if (!note || this.isTechnicalPublicNote(note)) {
      return null;
    }

    return note;
  }

  dayTone(day: ParkOpeningHoursDay | null): ParkOpeningHoursDayTone {
    if (!day) {
      return 'empty';
    }

    if (this.isClosed(day)) {
      return 'closed';
    }

    const durationMinutes: number = this.resolveOpeningDurationMinutes(day);
    if (durationMinutes < 6 * 60) {
      return 'short';
    }

    if (durationMinutes > 9 * 60) {
      return 'long';
    }

    return 'standard';
  }

  cellAriaLabel(cell: ParkOpeningHoursCalendarCell): string | null {
    if (!cell.localDate) {
      return null;
    }

    if (!cell.day) {
      return this.formatDate(cell.localDate);
    }

    const status: string = this.ngxTranslateService.instant(this.isClosed(cell.day)
      ? 'parkOpeningHours.page.closed'
      : 'parkOpeningHours.page.open');
    const ranges: string = this.formatRanges(cell.day);
    return ranges.length > 0 ? `${this.formatDate(cell.localDate)}, ${status}, ${ranges}` : `${this.formatDate(cell.localDate)}, ${status}`;
  }

  private buildMonthGroup(key: string, days: ParkOpeningHoursDay[]): ParkOpeningHoursMonthGroup {
    const [yearText, monthText] = key.split('-');
    const year: number = Number.parseInt(yearText, 10);
    const monthIndex: number = Number.parseInt(monthText, 10) - 1;
    const daysByDate: Map<string, ParkOpeningHoursDay> = new Map<string, ParkOpeningHoursDay>(
      days.map((day: ParkOpeningHoursDay): [string, ParkOpeningHoursDay] => [day.localDate, day])
    );
    const daysInMonth: number = new Date(year, monthIndex + 1, 0).getDate();
    const firstDayOffset: number = this.resolveMondayFirstOffset(new Date(year, monthIndex, 1, 12, 0, 0).getDay());
    const cells: ParkOpeningHoursCalendarCell[] = [];

    for (let index: number = 0; index < firstDayOffset; index += 1) {
      cells.push(this.createEmptyCell(`${key}-empty-start-${index}`));
    }

    for (let dayNumber: number = 1; dayNumber <= daysInMonth; dayNumber += 1) {
      const localDate: string = `${key}-${dayNumber.toString().padStart(2, '0')}`;
      cells.push(this.createDayCell(localDate, dayNumber, daysByDate.get(localDate) ?? null));
    }

    while (cells.length % 7 !== 0) {
      cells.push(this.createEmptyCell(`${key}-empty-end-${cells.length}`));
    }

    const weeks: ParkOpeningHoursCalendarWeek[] = [];
    for (let index: number = 0; index < cells.length; index += 7) {
      weeks.push({
        key: `${key}-week-${index / 7}`,
        cells: cells.slice(index, index + 7)
      });
    }

    return {
      key,
      label: this.formatMonth(`${key}-01`),
      openDays: days.filter((day: ParkOpeningHoursDay): boolean => !this.isClosed(day)).length,
      closedDays: days.filter((day: ParkOpeningHoursDay): boolean => this.isClosed(day)).length,
      weeks
    };
  }

  private createEmptyCell(key: string): ParkOpeningHoursCalendarCell {
    return {
      key,
      localDate: null,
      dayNumber: null,
      day: null,
      tone: 'empty',
      isToday: false
    };
  }

  private createDayCell(localDate: string, dayNumber: number, day: ParkOpeningHoursDay | null): ParkOpeningHoursCalendarCell {
    return {
      key: localDate,
      localDate,
      dayNumber,
      day,
      tone: this.dayTone(day),
      isToday: localDate === this.resolveTodayLocalDate()
    };
  }

  private buildWeekDayLabels(language: string): string[] {
    const monday: Date = new Date('2026-01-05T12:00:00');
    return Array.from({ length: 7 }, (_: unknown, index: number): string => {
      const date: Date = new Date(monday);
      date.setDate(monday.getDate() + index);
      return new Intl.DateTimeFormat(language, { weekday: 'short' }).format(date);
    });
  }

  private formatCompactRange(range: ParkOpeningHoursTimeRange): string {
    return `${range.opensAt}-${range.closesAt}${range.closesNextDay ? '+1' : ''}`;
  }

  private isClosed(day: ParkOpeningHoursDay): boolean {
    return day.isClosed || day.timeRanges.length === 0;
  }

  private resolveOpeningDurationMinutes(day: ParkOpeningHoursDay): number {
    return day.timeRanges.reduce((total: number, range: ParkOpeningHoursTimeRange): number => {
      const opensAt: number = this.toAbsoluteMinutes(range.opensAt, false);
      const closesAt: number = this.toAbsoluteMinutes(range.closesAt, range.closesNextDay);
      return total + Math.max(0, closesAt - opensAt);
    }, 0);
  }

  private toAbsoluteMinutes(value: string, nextDay: boolean): number {
    const [hoursText, minutesText] = value.split(':');
    const minutes: number = (Number.parseInt(hoursText, 10) * 60) + Number.parseInt(minutesText, 10);
    return nextDay ? minutes + (24 * 60) : minutes;
  }

  private resolveMondayFirstOffset(day: number): number {
    return (day + 6) % 7;
  }

  private resolveTodayLocalDate(): string {
    const today: Date = new Date();
    const year: number = today.getFullYear();
    const month: string = (today.getMonth() + 1).toString().padStart(2, '0');
    const day: string = today.getDate().toString().padStart(2, '0');
    return `${year}-${month}-${day}`;
  }

  private isTechnicalPublicNote(note: string): boolean {
    const normalizedNote: string = note
      .normalize('NFD')
      .replace(/[\u0300-\u036f]/g, '')
      .toLowerCase();
    const technicalFragments: string[] = [
      'regle de couverture',
      'couverture',
      'dates non definies',
      'priorite',
      'rule',
      'coverage',
      'undefined date',
      'priority'
    ];

    return technicalFragments.some((fragment: string): boolean => normalizedNote.includes(fragment));
  }

  private loadOpeningHoursPage(parkId: string): void {
    const previousData: ParkOpeningHoursPageData | undefined = this.stateStore.data();
    this.stateStore.setLoading(previousData);

    forkJoin({
      summary: this.parksApiService.getParkDetailSummary(parkId, anonymousHttpOptions()),
      calendar: this.parksApiService.getParkOpeningHours(parkId, null, null, anonymousHttpOptions())
    }).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (data: { summary: ParkDetailSummary; calendar: ParkOpeningHoursCalendar }) => {
        const pageData: ParkOpeningHoursPageData = {
          park: data.summary.park,
          parkImageId: data.summary.mainImage?.id ?? null,
          calendar: data.calendar
        };

        if (!pageData.calendar.days || pageData.calendar.days.length === 0) {
          this.ssrHttpStatusService.setNotFound();
          this.stateStore.setEmpty(pageData);
          return;
        }

        this.stateStore.setReady(pageData);
      },
      error: (error: unknown) => {
        console.error('Error loading park opening hours page', error);

        if (hasHttpStatus(error, 404)) {
          this.ssrHttpStatusService.setNotFound();
        }

        this.stateStore.setError('parkOpeningHours.page.errorMessage', previousData);
      }
    });
  }

  private formatMonth(value: string): string {
    return new Intl.DateTimeFormat(this.currentLanguage(), {
      month: 'long',
      year: 'numeric'
    }).format(new Date(`${value}T12:00:00`));
  }
}
