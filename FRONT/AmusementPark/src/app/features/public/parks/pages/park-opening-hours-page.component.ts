import { ChangeDetectionStrategy, Component, DestroyRef, OnInit, effect, inject, signal } from '@angular/core';
import { ActivatedRoute, ParamMap, Router, RouterLink } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { forkJoin } from 'rxjs';
import { TranslateModule } from '@ngx-translate/core';

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
  days: ParkOpeningHoursDay[];
}

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

  private readonly destroyRef: DestroyRef = inject(DestroyRef);
  private currentParkId: string | null = null;

  constructor(
    private readonly route: ActivatedRoute,
    private readonly router: Router,
    private readonly translationService: TranslationService,
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

    this.translationService.languageChanged.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((language: string) => {
      this.currentLanguage.set(language);
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
    const groupsByKey: Map<string, ParkOpeningHoursMonthGroup> = new Map<string, ParkOpeningHoursMonthGroup>();
    for (const day of days) {
      const key: string = day.localDate.slice(0, 7);
      let group: ParkOpeningHoursMonthGroup | undefined = groupsByKey.get(key);
      if (!group) {
        group = {
          key,
          label: this.formatMonth(day.localDate),
          days: []
        };
        groupsByKey.set(key, group);
      }

      group.days.push(day);
    }

    return [...groupsByKey.values()];
  }

  formatDate(value: string): string {
    return new Intl.DateTimeFormat(this.currentLanguage(), {
      weekday: 'long',
      month: 'long',
      day: 'numeric'
    }).format(new Date(`${value}T12:00:00`));
  }

  formatShortDate(value: string): string {
    return new Intl.DateTimeFormat(this.currentLanguage(), {
      weekday: 'short',
      day: 'numeric'
    }).format(new Date(`${value}T12:00:00`));
  }

  formatRange(range: ParkOpeningHoursTimeRange): string {
    return `${range.opensAt} - ${range.closesAt}${range.closesNextDay ? ' (+1)' : ''}`;
  }

  formatRanges(day: ParkOpeningHoursDay): string {
    if (day.isClosed || day.timeRanges.length === 0) {
      return '';
    }

    return day.timeRanges
      .map((range: ParkOpeningHoursTimeRange): string => this.formatRange(range))
      .join(' / ');
  }

  dayTone(day: ParkOpeningHoursDay): 'closed' | 'open' {
    return day.isClosed || day.timeRanges.length === 0 ? 'closed' : 'open';
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
