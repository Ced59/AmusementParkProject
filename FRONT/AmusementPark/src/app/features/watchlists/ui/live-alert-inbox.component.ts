import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, DestroyRef, OnInit, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';

import { LiveAlertNotification, LiveAlertSubscription, LiveAlertType } from '@app/models/watchlists/live-alert.model';
import { TranslationService } from '@app/services/translation.service';
import { ImageDisplayComponent } from '@shared/components/image-display/image-display.component';
import { SafeExternalUrlPipe } from '@shared/pipes';
import { buildPublicParkItemRouteCommands } from '@shared/utils/routing/public-detail-route.helpers';
import { UiButtonDirective, UiChipComponent, UiSurfaceDirective } from '@ui/primitives';
import { LiveAlertInboxFacade } from '../state/live-alert-inbox.facade';

@Component({
  selector: 'app-live-alert-inbox',
  templateUrl: './live-alert-inbox.component.html',
  styleUrl: './live-alert-inbox.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [LiveAlertInboxFacade],
  imports: [
    DatePipe,
    RouterLink,
    TranslateModule,
    ImageDisplayComponent,
    SafeExternalUrlPipe,
    UiButtonDirective,
    UiChipComponent,
    UiSurfaceDirective
  ]
})
export class LiveAlertInboxComponent implements OnInit {
  protected readonly currentLang = signal<string>('en');
  protected readonly imageWidths: readonly number[] = [120, 180, 240];

  constructor(
    protected readonly facade: LiveAlertInboxFacade,
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

  protected typeLabelKey(type: LiveAlertType): string {
    return `liveAlerts.types.${type}.notification`;
  }

  protected subscriptionLabelKey(subscription: LiveAlertSubscription): string {
    return `liveAlerts.types.${subscription.type}.active`;
  }

  protected routeFor(notification: LiveAlertNotification): string[] | null {
    if (!notification.targetName || !notification.parkName) {
      return null;
    }
    return buildPublicParkItemRouteCommands({
      language: this.currentLang(),
      parkId: notification.parkId,
      parkName: notification.parkName,
      itemId: notification.targetId,
      itemName: notification.targetName
    });
  }
}
