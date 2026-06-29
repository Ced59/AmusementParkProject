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

interface ParkOpeningHoursSelectedDayDetails {
  localDate: string;
  day: ParkOpeningHoursDay | null;
  isToday: boolean;
  status: 'open' | 'closed' | 'notDefined';
  note: string | null;
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
  protected readonly selectedMonthKey = signal<string>(this.resolveCurrentMonthKey(null));
  protected readonly selectedDayLocalDate = signal<string | null>(null);
  protected readonly monthLoading = signal<boolean>(false);
  protected readonly monthLoadFailed = signal<boolean>(false);

  private readonly destroyRef: DestroyRef = inject(DestroyRef);
  private readonly monthCalendarCache = new Map<string, ParkOpeningHoursCalendar>();
  private currentParkId: string | null = null;
  private monthLoadSequence = 0;
  private monthGroupCache: {
    days: ParkOpeningHoursDay[];
    language: string;
    monthKey: string;
    todayLocalDate: string;
    group: ParkOpeningHoursMonthGroup;
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

  monthGroup(calendar: ParkOpeningHoursCalendar): ParkOpeningHoursMonthGroup {
    const monthKey: string = this.resolveCalendarMonthKey(calendar);
    const language: string = this.currentLanguage();
    const todayLocalDate: string = this.resolveTodayLocalDate(calendar.timeZoneId);
    const monthDays: ParkOpeningHoursDay[] = calendar.days.filter((day: ParkOpeningHoursDay): boolean => day.localDate.startsWith(monthKey));

    if (
      this.monthGroupCache?.days === calendar.days
      && this.monthGroupCache.language === language
      && this.monthGroupCache.monthKey === monthKey
      && this.monthGroupCache.todayLocalDate === todayLocalDate
    ) {
      return this.monthGroupCache.group;
    }

    const group: ParkOpeningHoursMonthGroup = this.buildMonthGroup(monthKey, monthDays, todayLocalDate);
    this.monthGroupCache = {
      days: calendar.days,
      language,
      monthKey,
      todayLocalDate,
      group
    };

    return group;
  }

  previousMonthKey(calendar: ParkOpeningHoursCalendar): string | null {
    const previousMonthKey: string = this.addMonths(this.resolveCalendarMonthKey(calendar), -1);
    return this.isMonthWithinCoverage(previousMonthKey, calendar.firstDate, calendar.lastDate) ? previousMonthKey : null;
  }

  nextMonthKey(calendar: ParkOpeningHoursCalendar): string | null {
    const nextMonthKey: string = this.addMonths(this.resolveCalendarMonthKey(calendar), 1);
    return this.isMonthWithinCoverage(nextMonthKey, calendar.firstDate, calendar.lastDate) ? nextMonthKey : null;
  }

  currentMonthKey(calendar: ParkOpeningHoursCalendar): string | null {
    return this.clampMonthKey(this.resolveCurrentMonthKey(calendar.timeZoneId), calendar.firstDate, calendar.lastDate);
  }

  canGoToCurrentMonth(calendar: ParkOpeningHoursCalendar): boolean {
    const currentMonthKey: string | null = this.currentMonthKey(calendar);
    return currentMonthKey !== null && currentMonthKey !== this.resolveCalendarMonthKey(calendar);
  }

  showPreviousMonth(data: ParkOpeningHoursPageData): void {
    const monthKey: string | null = this.previousMonthKey(data.calendar);
    if (monthKey) {
      this.activateMonth(data, monthKey);
    }
  }

  showNextMonth(data: ParkOpeningHoursPageData): void {
    const monthKey: string | null = this.nextMonthKey(data.calendar);
    if (monthKey) {
      this.activateMonth(data, monthKey);
    }
  }

  showCurrentMonth(data: ParkOpeningHoursPageData): void {
    const monthKey: string | null = this.currentMonthKey(data.calendar);
    if (monthKey) {
      this.activateMonth(data, monthKey);
    }
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
    return `${range.opensAt} - ${range.closesAt}${range.closesNextDay ? ` (${this.nextDaySuffix()})` : ''}`;
  }

  formatLastAdmission(range: ParkOpeningHoursTimeRange): string {
    if (!range.lastAdmissionAt) {
      return '';
    }

    return `${range.lastAdmissionAt}${range.lastAdmissionNextDay ? ` (${this.nextDaySuffix()})` : ''}`;
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
    if (!day) {
      return null;
    }

    const note: string | null = (day.label || day.reason || '').trim() || null;
    if (!note || this.isTechnicalPublicNote(note)) {
      return null;
    }

    return note;
  }

  hasPublicDayNote(day: ParkOpeningHoursDay | null): boolean {
    return this.publicDayNote(day) !== null;
  }

  selectDay(cell: ParkOpeningHoursCalendarCell): void {
    if (!cell.localDate) {
      return;
    }

    this.selectedDayLocalDate.set(cell.localDate);
  }

  isSelectedCell(cell: ParkOpeningHoursCalendarCell): boolean {
    return !!cell.localDate && cell.localDate === this.selectedDayLocalDate();
  }

  selectedDayDetails(calendar: ParkOpeningHoursCalendar): ParkOpeningHoursSelectedDayDetails {
    const localDate: string = this.resolveSelectedDayLocalDate(calendar);
    const day: ParkOpeningHoursDay | null = calendar.days.find((candidate: ParkOpeningHoursDay): boolean => candidate.localDate === localDate) ?? null;
    const status: 'open' | 'closed' | 'notDefined' = day === null
      ? 'notDefined'
      : this.isClosed(day)
        ? 'closed'
        : 'open';

    return {
      localDate,
      day,
      isToday: localDate === this.resolveTodayLocalDate(calendar.timeZoneId),
      status,
      note: this.publicDayNote(day)
    };
  }

  selectedDayStatusLabelKey(details: ParkOpeningHoursSelectedDayDetails): string {
    if (details.status === 'open') {
      return 'parkOpeningHours.page.open';
    }

    if (details.status === 'closed') {
      return 'parkOpeningHours.page.closed';
    }

    return 'parkOpeningHours.page.notDefined';
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
    const information: string = this.hasPublicDayNote(cell.day)
      ? this.ngxTranslateService.instant('parkOpeningHours.page.dayHasInformation')
      : '';

    return [this.formatDate(cell.localDate), status, ranges, information]
      .filter((part: string): boolean => part.length > 0)
      .join(', ');
  }

  private buildMonthGroup(monthKey: string, days: ParkOpeningHoursDay[], todayLocalDate: string): ParkOpeningHoursMonthGroup {
    const [yearText, monthText] = monthKey.split('-');
    const year: number = Number.parseInt(yearText, 10);
    const monthIndex: number = Number.parseInt(monthText, 10) - 1;
    const daysByDate: Map<string, ParkOpeningHoursDay> = new Map<string, ParkOpeningHoursDay>(
      days.map((day: ParkOpeningHoursDay): [string, ParkOpeningHoursDay] => [day.localDate, day])
    );
    const daysInMonth: number = new Date(Date.UTC(year, monthIndex + 1, 0)).getUTCDate();
    const firstDayOffset: number = this.resolveMondayFirstOffset(new Date(Date.UTC(year, monthIndex, 1, 12, 0, 0)).getUTCDay());
    const cells: ParkOpeningHoursCalendarCell[] = [];

    for (let index: number = 0; index < firstDayOffset; index += 1) {
      cells.push(this.createEmptyCell(`${monthKey}-empty-start-${index}`));
    }

    for (let dayNumber: number = 1; dayNumber <= daysInMonth; dayNumber += 1) {
      const localDate: string = `${monthKey}-${dayNumber.toString().padStart(2, '0')}`;
      cells.push(this.createDayCell(localDate, dayNumber, daysByDate.get(localDate) ?? null, todayLocalDate));
    }

    while (cells.length % 7 !== 0) {
      cells.push(this.createEmptyCell(`${monthKey}-empty-end-${cells.length}`));
    }

    const weeks: ParkOpeningHoursCalendarWeek[] = [];
    for (let index: number = 0; index < cells.length; index += 7) {
      weeks.push({
        key: `${monthKey}-week-${index / 7}`,
        cells: cells.slice(index, index + 7)
      });
    }

    return {
      key: monthKey,
      label: this.formatMonth(`${monthKey}-01`),
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

  private createDayCell(localDate: string, dayNumber: number, day: ParkOpeningHoursDay | null, todayLocalDate: string): ParkOpeningHoursCalendarCell {
    return {
      key: localDate,
      localDate,
      dayNumber,
      day,
      tone: this.dayTone(day),
      isToday: localDate === todayLocalDate
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

  private nextDaySuffix(): string {
    return this.ngxTranslateService.instant('parkOpeningHours.page.nextDaySuffix');
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

  private resolveTodayLocalDate(timeZoneId: string | null | undefined): string {
    const valueByType: Record<string, string> = this.resolveDateParts(timeZoneId, new Date());
    return `${valueByType['year']}-${valueByType['month']}-${valueByType['day']}`;
  }

  private resolveCurrentMonthKey(timeZoneId: string | null | undefined): string {
    return this.resolveTodayLocalDate(timeZoneId).slice(0, 7);
  }

  private resolveDateParts(timeZoneId: string | null | undefined, date: Date): Record<string, string> {
    let parts: Intl.DateTimeFormatPart[];
    try {
      parts = new Intl.DateTimeFormat('en-CA', {
        timeZone: timeZoneId || undefined,
        year: 'numeric',
        month: '2-digit',
        day: '2-digit'
      }).formatToParts(date);
    } catch {
      parts = new Intl.DateTimeFormat('en-CA', {
        year: 'numeric',
        month: '2-digit',
        day: '2-digit'
      }).formatToParts(date);
    }

    return Object.fromEntries(parts.map((part: Intl.DateTimeFormatPart) => [part.type, part.value]));
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
    const requestedMonthKey: string = this.resolveCurrentMonthKey(null);
    const requestedRange: { from: string; to: string } = this.resolveMonthRange(requestedMonthKey);

    this.monthCalendarCache.clear();
    this.monthGroupCache = null;
    this.monthLoadSequence += 1;
    this.monthLoading.set(false);
    this.monthLoadFailed.set(false);
    this.selectedMonthKey.set(requestedMonthKey);
    this.selectedDayLocalDate.set(null);
    this.stateStore.setLoading(previousData);

    forkJoin({
      summary: this.parksApiService.getParkDetailSummary(parkId, anonymousHttpOptions()),
      calendar: this.parksApiService.getParkOpeningHours(parkId, requestedRange.from, requestedRange.to, anonymousHttpOptions())
    }).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (data: { summary: ParkDetailSummary; calendar: ParkOpeningHoursCalendar }) => {
        const pageData: ParkOpeningHoursPageData = {
          park: data.summary.park,
          parkImageId: data.summary.mainImage?.id ?? null,
          calendar: data.calendar
        };

        if (!this.hasOpeningHoursCoverage(pageData.calendar)) {
          this.ssrHttpStatusService.setNotFound();
          this.stateStore.setEmpty(pageData);
          return;
        }

        this.cacheCalendar(requestedMonthKey, pageData.calendar);

        const preferredMonthKey: string | null = this.resolvePreferredInitialMonthKey(pageData.calendar, requestedMonthKey);
        if (preferredMonthKey && preferredMonthKey !== requestedMonthKey) {
          this.selectedMonthKey.set(preferredMonthKey);
          this.loadInitialMonth(pageData, preferredMonthKey);
          return;
        }

        this.selectedMonthKey.set(requestedMonthKey);
        this.selectDefaultDay(pageData.calendar);
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

  private loadInitialMonth(pageData: ParkOpeningHoursPageData, monthKey: string): void {
    const cachedCalendar: ParkOpeningHoursCalendar | undefined = this.monthCalendarCache.get(monthKey);
    if (cachedCalendar) {
      this.selectDefaultDay(cachedCalendar);
      this.stateStore.setReady({ ...pageData, calendar: cachedCalendar });
      return;
    }

    const parkId: string | null = pageData.park.id?.trim() ?? null;
    if (!parkId) {
      this.stateStore.setReady(pageData);
      return;
    }

    this.monthLoading.set(true);
    this.loadMonthCalendar(parkId, monthKey, pageData, true);
  }

  private activateMonth(data: ParkOpeningHoursPageData, monthKey: string): void {
    const normalizedMonthKey: string | null = this.clampMonthKey(monthKey, data.calendar.firstDate, data.calendar.lastDate);
    const parkId: string | null = data.park.id?.trim() ?? null;
    if (!normalizedMonthKey || !parkId || normalizedMonthKey === this.resolveCalendarMonthKey(data.calendar)) {
      return;
    }

    this.selectedMonthKey.set(normalizedMonthKey);
    this.monthLoadFailed.set(false);

    const cachedCalendar: ParkOpeningHoursCalendar | undefined = this.monthCalendarCache.get(normalizedMonthKey);
    if (cachedCalendar) {
      this.selectDefaultDay(cachedCalendar);
      this.stateStore.setReady({ ...data, calendar: cachedCalendar });
      return;
    }

    this.monthLoading.set(true);
    this.loadMonthCalendar(parkId, normalizedMonthKey, data, false);
  }

  private loadMonthCalendar(parkId: string, monthKey: string, pageData: ParkOpeningHoursPageData, initialLoad: boolean): void {
    const requestId: number = ++this.monthLoadSequence;
    const range: { from: string; to: string } = this.resolveMonthRange(monthKey);

    this.parksApiService.getParkOpeningHours(parkId, range.from, range.to, anonymousHttpOptions())
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (calendar: ParkOpeningHoursCalendar) => {
          if (requestId !== this.monthLoadSequence) {
            return;
          }

          this.cacheCalendar(monthKey, calendar);
          this.monthLoading.set(false);
          this.monthLoadFailed.set(false);
          this.selectDefaultDay(calendar);
          this.stateStore.setReady({ ...pageData, calendar });
        },
        error: (error: unknown) => {
          if (requestId !== this.monthLoadSequence) {
            return;
          }

          console.error('Error loading park opening hours month', error);
          this.monthLoading.set(false);
          this.monthLoadFailed.set(true);

          if (initialLoad) {
            this.stateStore.setError('parkOpeningHours.page.errorMessage', pageData);
          }
        }
      });
  }

  private hasOpeningHoursCoverage(calendar: ParkOpeningHoursCalendar): boolean {
    return !!calendar.firstDate || !!calendar.lastDate || calendar.days.length > 0;
  }

  private resolvePreferredInitialMonthKey(calendar: ParkOpeningHoursCalendar, fallbackMonthKey: string): string | null {
    const parkCurrentMonthKey: string = this.resolveCurrentMonthKey(calendar.timeZoneId);
    return this.clampMonthKey(parkCurrentMonthKey, calendar.firstDate, calendar.lastDate)
      ?? this.clampMonthKey(fallbackMonthKey, calendar.firstDate, calendar.lastDate)
      ?? fallbackMonthKey;
  }

  private resolveCalendarMonthKey(calendar: ParkOpeningHoursCalendar): string {
    const selectedMonthKey: string = this.selectedMonthKey();
    const fromMonthKey: string = calendar.fromDate.slice(0, 7);
    const toMonthKey: string = calendar.toDate.slice(0, 7);
    if (selectedMonthKey >= fromMonthKey && selectedMonthKey <= toMonthKey) {
      return selectedMonthKey;
    }

    return fromMonthKey;
  }

  private resolveSelectedDayLocalDate(calendar: ParkOpeningHoursCalendar): string {
    const selectedDayLocalDate: string | null = this.selectedDayLocalDate();
    const monthKey: string = this.resolveCalendarMonthKey(calendar);
    if (selectedDayLocalDate && selectedDayLocalDate.startsWith(monthKey)) {
      return selectedDayLocalDate;
    }

    return this.resolveDefaultDayLocalDate(calendar);
  }

  private selectDefaultDay(calendar: ParkOpeningHoursCalendar): void {
    const monthKey: string = this.resolveCalendarMonthKey(calendar);
    const selectedDayLocalDate: string | null = this.selectedDayLocalDate();
    if (selectedDayLocalDate && selectedDayLocalDate.startsWith(monthKey)) {
      return;
    }

    this.selectedDayLocalDate.set(this.resolveDefaultDayLocalDate(calendar));
  }

  private resolveDefaultDayLocalDate(calendar: ParkOpeningHoursCalendar): string {
    const monthKey: string = this.resolveCalendarMonthKey(calendar);
    const todayLocalDate: string = this.resolveTodayLocalDate(calendar.timeZoneId);
    if (todayLocalDate.startsWith(monthKey)) {
      return todayLocalDate;
    }

    return calendar.days.find((day: ParkOpeningHoursDay): boolean => day.localDate.startsWith(monthKey))?.localDate
      ?? `${monthKey}-01`;
  }

  private cacheCalendar(monthKey: string, calendar: ParkOpeningHoursCalendar): void {
    this.monthCalendarCache.set(monthKey, calendar);
  }

  private resolveMonthRange(monthKey: string): { from: string; to: string } {
    const [year, month] = this.parseMonthKey(monthKey);
    const lastDay: number = new Date(Date.UTC(year, month, 0)).getUTCDate();

    return {
      from: `${monthKey}-01`,
      to: `${monthKey}-${lastDay.toString().padStart(2, '0')}`
    };
  }

  private addMonths(monthKey: string, offset: number): string {
    const [year, month] = this.parseMonthKey(monthKey);
    const date: Date = new Date(Date.UTC(year, month - 1 + offset, 1, 12, 0, 0));
    return `${date.getUTCFullYear()}-${(date.getUTCMonth() + 1).toString().padStart(2, '0')}`;
  }

  private clampMonthKey(monthKey: string, firstDate: string | null | undefined, lastDate: string | null | undefined): string | null {
    const firstMonthKey: string | null = firstDate ? firstDate.slice(0, 7) : null;
    const lastMonthKey: string | null = lastDate ? lastDate.slice(0, 7) : null;

    if (firstMonthKey && monthKey < firstMonthKey) {
      return firstMonthKey;
    }

    if (lastMonthKey && monthKey > lastMonthKey) {
      return lastMonthKey;
    }

    return monthKey;
  }

  private isMonthWithinCoverage(monthKey: string, firstDate: string | null | undefined, lastDate: string | null | undefined): boolean {
    const firstMonthKey: string | null = firstDate ? firstDate.slice(0, 7) : null;
    const lastMonthKey: string | null = lastDate ? lastDate.slice(0, 7) : null;

    if (firstMonthKey && monthKey < firstMonthKey) {
      return false;
    }

    if (lastMonthKey && monthKey > lastMonthKey) {
      return false;
    }

    return firstMonthKey !== null || lastMonthKey !== null;
  }

  private parseMonthKey(monthKey: string): [number, number] {
    const [yearText, monthText] = monthKey.split('-');
    return [Number.parseInt(yearText, 10), Number.parseInt(monthText, 10)];
  }

  private formatMonth(value: string): string {
    return new Intl.DateTimeFormat(this.currentLanguage(), {
      month: 'long',
      year: 'numeric'
    }).format(new Date(`${value}T12:00:00`));
  }
}
