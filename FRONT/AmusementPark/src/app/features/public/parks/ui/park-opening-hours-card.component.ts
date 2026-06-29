import { isPlatformBrowser } from '@angular/common';
import { ChangeDetectionStrategy, Component, Inject, Input, OnDestroy, PLATFORM_ID, computed, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { ScreenState } from '@shared/models/contracts/screen-state.model';
import { ParkOpeningHoursCalendar, ParkOpeningHoursDay, ParkOpeningHoursTimeRange } from '@app/models/parks/park-opening-hours';

interface ParkOpeningHoursNow {
  localDate: string;
  minutes: number;
}

interface ParkOpeningHoursStatus {
  isOpen: boolean;
  activeRange: ParkOpeningHoursTimeRange | null;
  today: ParkOpeningHoursDay | null;
  nextOpeningDelayLabel: string | null;
}

@Component({
  selector: 'app-park-opening-hours-card',
  templateUrl: './park-opening-hours-card.component.html',
  styleUrls: ['./park-opening-hours-card.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, TranslateModule]
})
export class ParkOpeningHoursCardComponent implements OnDestroy {
  private readonly calendarSignal = signal<ParkOpeningHoursCalendar | null>(null);
  private readonly stateSignal = signal<ScreenState<unknown, string> | null>(null);

  @Input()
  set calendar(value: ParkOpeningHoursCalendar | null) {
    this.calendarSignal.set(value);
  }

  get calendar(): ParkOpeningHoursCalendar | null {
    return this.calendarSignal();
  }

  @Input()
  set state(value: ScreenState<unknown, string> | null) {
    this.stateSignal.set(value);
  }

  get state(): ScreenState<unknown, string> | null {
    return this.stateSignal();
  }

  @Input() openingHoursLink: string[] | null = null;

  protected readonly now = signal(new Date());
  private readonly timerId: ReturnType<typeof setInterval> | null;

  protected readonly status = computed<ParkOpeningHoursStatus>(() => this.resolveStatus(this.calendarSignal(), this.now()));

  constructor(
    @Inject(PLATFORM_ID) platformId: object,
    private readonly translateService: TranslateService
  ) {
    this.timerId = isPlatformBrowser(platformId)
      ? setInterval((): void => this.now.set(new Date()), 60000)
      : null;
  }

  ngOnDestroy(): void {
    if (this.timerId) {
      clearInterval(this.timerId);
    }
  }

  get hasCalendar(): boolean {
    return !!this.calendarSignal()?.days?.length;
  }

  get isLoading(): boolean {
    return this.stateSignal()?.kind === 'loading';
  }

  protected formatRanges(day: ParkOpeningHoursDay | null): string {
    if (!day || day.isClosed || day.timeRanges.length === 0) {
      return '';
    }

    return day.timeRanges
      .map((range: ParkOpeningHoursTimeRange): string => this.formatRange(range))
      .join(' / ');
  }

  protected formatRange(range: ParkOpeningHoursTimeRange): string {
    return `${range.opensAt} - ${range.closesAt}${range.closesNextDay ? ' (+1)' : ''}`;
  }

  protected visitorTimeZone(): string {
    return Intl.DateTimeFormat().resolvedOptions().timeZone || 'local';
  }

  private resolveStatus(calendar: ParkOpeningHoursCalendar | null, now: Date): ParkOpeningHoursStatus {
    if (!calendar?.days?.length) {
      return { isOpen: false, activeRange: null, today: null, nextOpeningDelayLabel: null };
    }

    const parkNow: ParkOpeningHoursNow = this.resolveParkNow(calendar.timeZoneId, now);
    const today: ParkOpeningHoursDay | null = calendar.days.find((day: ParkOpeningHoursDay) => day.localDate === parkNow.localDate) ?? null;
    const previousDay: ParkOpeningHoursDay | null = calendar.days.find((day: ParkOpeningHoursDay) => day.localDate === this.previousLocalDate(parkNow.localDate)) ?? null;
    const activeTodayRange: ParkOpeningHoursTimeRange | null = this.findActiveRange(today, parkNow.minutes, false);
    const activePreviousRange: ParkOpeningHoursTimeRange | null = this.findActiveRange(previousDay, parkNow.minutes + 1440, true);
    const activeRange: ParkOpeningHoursTimeRange | null = activeTodayRange ?? activePreviousRange;

    return {
      isOpen: activeRange !== null,
      activeRange,
      today,
      nextOpeningDelayLabel: activeRange ? null : this.formatDelay(this.findNextOpeningDelayMinutes(calendar.days, parkNow)),
    };
  }

  private findActiveRange(day: ParkOpeningHoursDay | null, currentMinutes: number, requireNextDay: boolean): ParkOpeningHoursTimeRange | null {
    if (!day || day.isClosed) {
      return null;
    }

    return day.timeRanges.find((range: ParkOpeningHoursTimeRange) => {
      if (requireNextDay && !range.closesNextDay) {
        return false;
      }

      const opensAt: number = this.toMinutes(range.opensAt);
      const closesAt: number = this.toMinutes(range.closesAt) + (range.closesNextDay ? 1440 : 0);
      return currentMinutes >= opensAt && currentMinutes < closesAt;
    }) ?? null;
  }

  private findNextOpeningDelayMinutes(days: ParkOpeningHoursDay[], parkNow: ParkOpeningHoursNow): number | null {
    const sortedDays: ParkOpeningHoursDay[] = [...days].sort((first: ParkOpeningHoursDay, second: ParkOpeningHoursDay): number => {
      return first.localDate.localeCompare(second.localDate);
    });
    let nextDelayMinutes: number | null = null;

    for (const day of sortedDays) {
      if (day.isClosed || day.timeRanges.length === 0) {
        continue;
      }

      const dayOffset: number = this.diffLocalDatesInDays(parkNow.localDate, day.localDate);
      if (dayOffset < 0) {
        continue;
      }

      for (const range of day.timeRanges) {
        const delayMinutes: number = (dayOffset * 1440) + this.toMinutes(range.opensAt) - parkNow.minutes;
        if (delayMinutes <= 0) {
          continue;
        }

        if (nextDelayMinutes === null || delayMinutes < nextDelayMinutes) {
          nextDelayMinutes = delayMinutes;
        }
      }
    }

    return nextDelayMinutes;
  }

  private formatDelay(delayMinutes: number | null): string | null {
    if (delayMinutes === null) {
      return null;
    }

    const totalMinutes: number = Math.max(1, Math.ceil(delayMinutes));
    const days: number = Math.floor(totalMinutes / 1440);
    const hours: number = Math.floor((totalMinutes % 1440) / 60);
    const minutes: number = totalMinutes % 60;
    const language: string = this.translateService.currentLang || this.translateService.defaultLang || 'en';
    const formatter: Intl.NumberFormat = new Intl.NumberFormat(language);

    if (days > 0) {
      const dayUnit: string = language === 'fr' ? (days === 1 ? 'jour' : 'jours') : 'd';
      const dayText: string = `${formatter.format(days)} ${dayUnit}`;
      return hours > 0 ? `${dayText} ${formatter.format(hours)} h` : dayText;
    }

    if (hours > 0) {
      const hourText: string = `${formatter.format(hours)} h`;
      return minutes > 0 && hours < 3 ? `${hourText} ${formatter.format(minutes)} min` : hourText;
    }

    return `${formatter.format(minutes)} min`;
  }

  private resolveParkNow(timeZoneId: string, now: Date): ParkOpeningHoursNow {
    let parts: Intl.DateTimeFormatPart[];
    try {
      parts = new Intl.DateTimeFormat('en-CA', {
        timeZone: timeZoneId || undefined,
        year: 'numeric',
        month: '2-digit',
        day: '2-digit',
        hour: '2-digit',
        minute: '2-digit',
        hourCycle: 'h23'
      }).formatToParts(now);
    } catch {
      parts = new Intl.DateTimeFormat('en-CA', {
        year: 'numeric',
        month: '2-digit',
        day: '2-digit',
        hour: '2-digit',
        minute: '2-digit',
        hourCycle: 'h23'
      }).formatToParts(now);
    }

    const valueByType: Record<string, string> = Object.fromEntries(parts.map((part: Intl.DateTimeFormatPart) => [part.type, part.value]));
    const hour: number = Number(valueByType['hour'] ?? '0');
    const minute: number = Number(valueByType['minute'] ?? '0');

    return {
      localDate: `${valueByType['year']}-${valueByType['month']}-${valueByType['day']}`,
      minutes: (hour * 60) + minute,
    };
  }

  private previousLocalDate(localDate: string): string {
    const parts: number[] = localDate.split('-').map((part: string) => Number(part));
    const date: Date = new Date(Date.UTC(parts[0], parts[1] - 1, parts[2]));
    date.setUTCDate(date.getUTCDate() - 1);
    return date.toISOString().slice(0, 10);
  }

  private diffLocalDatesInDays(fromLocalDate: string, toLocalDate: string): number {
    const fromParts: number[] = fromLocalDate.split('-').map((part: string) => Number(part));
    const toParts: number[] = toLocalDate.split('-').map((part: string) => Number(part));
    const fromTime: number = Date.UTC(fromParts[0], fromParts[1] - 1, fromParts[2]);
    const toTime: number = Date.UTC(toParts[0], toParts[1] - 1, toParts[2]);
    return Math.round((toTime - fromTime) / 86400000);
  }

  private toMinutes(value: string): number {
    const parts: number[] = value.split(':').map((part: string) => Number(part));
    return ((parts[0] || 0) * 60) + (parts[1] || 0);
  }
}
