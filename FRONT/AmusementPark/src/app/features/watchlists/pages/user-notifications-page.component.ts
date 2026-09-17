import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, DestroyRef, OnInit, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Router, RouterLink } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';

import { UserNotification } from '@app/models/watchlists/user-notification.model';
import { FactualEventType } from '@app/models/watchlists/watch-subscription.model';
import { TranslationService } from '@app/services/translation.service';
import { ImageDisplayComponent } from '@shared/components/image-display/image-display.component';
import { PaginationComponent } from '@shared/components/pagination/pagination.component';
import { SafeExternalUrlPipe } from '@shared/pipes';
import { PaginatorState } from '@shared/ui/primitives/paginator';
import {
  buildPublicParkItemRouteCommands,
  buildPublicParkRouteCommands
} from '@shared/utils/routing/public-detail-route.helpers';
import { UiButtonDirective, UiChipComponent, UiKickerComponent, UiSurfaceDirective } from '@ui/primitives';
import { UserNotificationPresenter } from '../services/user-notification.presenter';
import { UserNotificationsFacade } from '../state/user-notifications.facade';

@Component({
  selector: 'app-user-notifications-page',
  templateUrl: './user-notifications-page.component.html',
  styleUrl: './user-notifications-page.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [UserNotificationsFacade, UserNotificationPresenter],
  imports: [
    DatePipe,
    RouterLink,
    TranslateModule,
    ImageDisplayComponent,
    PaginationComponent,
    SafeExternalUrlPipe,
    UiButtonDirective,
    UiChipComponent,
    UiKickerComponent,
    UiSurfaceDirective
  ]
})
export class UserNotificationsPageComponent implements OnInit {
  protected readonly currentLang = signal<string>('en');
  protected readonly imageWidths: readonly number[] = [120, 180, 240, 360];
  protected readonly eventTypes: readonly FactualEventType[] = [
    'OpeningCalendarPublished', 'OpeningCalendarChanged', 'SeasonOpeningConfirmed',
    'SeasonClosingConfirmed', 'ParkTemporaryClosureConfirmed', 'ParkPermanentClosureConfirmed',
    'ParkReopeningConfirmed', 'ParkNameChanged', 'OperatorChanged', 'TicketPricePublishedOrChanged',
    'MajorDataCompletionImproved', 'AttractionAnnouncedOfficially', 'OpeningDateConfirmed',
    'OpeningDateChanged', 'OpenedConfirmed', 'TemporarilyClosedConfirmed', 'ReopenedConfirmed',
    'PermanentClosureConfirmed', 'Renamed', 'MajorRestrictionChanged', 'LocationOrCategoryCorrected',
    'HistoryPublished', 'MajorHistoryUpdate', 'VerifiedSourceAdded', 'CorrectionAfterUserReport'
  ];

  constructor(
    protected readonly facade: UserNotificationsFacade,
    protected readonly presenter: UserNotificationPresenter,
    private readonly router: Router,
    translationService: TranslationService,
    destroyRef: DestroyRef
  ) {
    this.currentLang.set(translationService.getCurrentLang() || 'en');
    translationService.languageChanged
      .pipe(takeUntilDestroyed(destroyRef))
      .subscribe((language: string): void => this.currentLang.set(language || 'en'));
  }

  ngOnInit(): void {
    this.facade.load();
  }

  protected routeFor(notification: UserNotification): string[] | null {
    if (!notification.target.name) {
      return null;
    }
    if (notification.target.type === 'Park') {
      return buildPublicParkRouteCommands({
        language: this.currentLang(),
        parkId: notification.target.targetId,
        parkName: notification.target.name
      });
    }
    return buildPublicParkItemRouteCommands({
      language: this.currentLang(),
      parkId: notification.target.parkId,
      parkName: notification.target.parentParkName,
      itemId: notification.target.targetId,
      itemName: notification.target.name
    });
  }

  protected onParkChanged(event: Event): void {
    this.facade.setPark((event.target as HTMLSelectElement).value);
  }

  protected onEventTypeChanged(event: Event): void {
    this.facade.setEventType((event.target as HTMLSelectElement).value);
  }

  protected onPageChanged(event: PaginatorState): void {
    const rows: number = event.rows ?? this.facade.pagination.itemsPerPage;
    const first: number = event.first ?? 0;
    this.facade.load(Math.floor(first / Math.max(1, rows)) + 1);
  }

  protected openNotification(notification: UserNotification, route: string[]): void {
    this.facade.markRead(
      notification,
      (): void => {
        void this.router.navigate(route);
      }
    );
  }
}
