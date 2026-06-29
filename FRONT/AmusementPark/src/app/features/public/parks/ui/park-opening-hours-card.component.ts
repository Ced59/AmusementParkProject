import { isPlatformBrowser } from '@angular/common';
import { ChangeDetectionStrategy, Component, Inject, Input, OnDestroy, PLATFORM_ID, computed, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';
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
}

@Component({
  selector: 'app-park-opening-hours-card',
  templateUrl: './park-opening-hours-card.component.html',
  styleUrls: ['./park-opening-hours-card.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, TranslateModule]
})
export class ParkOpeningHoursCardComponent implements OnDestroy {
  @Input() calendar: ParkOpeningHoursCalendar | null = null;
  @Input() state: ScreenState<unknown, string> | null = null;
  @Input() openingHoursLink: string[] | null = null;

  protected readonly now = signal(new Date());
  private readonly timerId: ReturnType<typeof setInterval> | null;

  protected readonly status = computed<ParkOpeningHoursStatus>(() => this.resolveStatus(this.calendar, this.now()));

  constructor(@Inject(PLATFORM_ID) platformId: object) {
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
    return !!this.calendar?.days?.length;
  }

  get isLoading(): boolean {
    return this.state?.kind === 'loading';
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
      return { isOpen: false, activeRange: null, today: null };
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

  private resolveParkNow(timeZoneId: string, now: Date): ParkOpeningHoursNow {
    const parts: Intl.DateTimeFormatPart[] = new Intl.DateTimeFormat('en-CA', {
      timeZone: timeZoneId || undefined,
      year: 'numeric',
      month: '2-digit',
      day: '2-digit',
      hour: '2-digit',
      minute: '2-digit',
      hourCycle: 'h23'
    }).formatToParts(now);
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

  private toMinutes(value: string): number {
    const parts: number[] = value.split(':').map((part: string) => Number(part));
    return ((parts[0] || 0) * 60) + (parts[1] || 0);
  }
}
