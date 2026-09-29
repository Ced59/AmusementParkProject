import { ChangeDetectionStrategy, Component, EventEmitter, Input, Output, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';

import { PublicLiveQueue, PublicLiveSource, PublicLiveTarget } from '@app/models/live-data/public-live.models';
import { buildPublicParkItemRouteCommands } from '@shared/utils/routing/public-detail-route.helpers';
import { UiButtonDirective, UiChipComponent, UiKickerComponent } from '@ui/primitives';
import { PublicLiveFilter } from '../models/public-live-filter.model';
import { PublicLiveDisplayMode, PublicLiveViewState } from '../models/public-live-view-state.model';
import {
  filterPublicLiveTargets,
  isPublicLiveQueueWaitUsable,
  isPublicLiveTargetClosed,
  resolvePublicLiveAttributionSources,
  resolvePublicLiveFreshnessReference,
  resolvePublicLiveStatusLabelKey,
  resolvePublicLiveTone,
  resolvePublicLiveWaitMinutes
} from '../utils/public-live-view.helpers';

@Component({
  selector: 'app-public-live-panel',
  templateUrl: './public-live-panel.component.html',
  styleUrls: ['./public-live-panel.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, TranslateModule, UiButtonDirective, UiChipComponent, UiKickerComponent]
})
export class PublicLivePanelComponent {
  @Input({ required: true }) state!: PublicLiveViewState;
  @Input({ required: true }) mode!: PublicLiveDisplayMode;
  @Input() currentLanguage: string = 'en';
  @Input() timeZoneId: string | null = null;
  @Output() refreshClicked: EventEmitter<void> = new EventEmitter<void>();

  protected readonly selectedFilter = signal<PublicLiveFilter>('all');
  protected readonly filters: readonly PublicLiveFilter[] = ['all', 'available', 'shortWait', 'closed', 'unknown'];

  refresh(): void {
    this.refreshClicked.emit();
  }

  selectFilter(filter: PublicLiveFilter): void {
    this.selectedFilter.set(filter);
  }

  protected visibleItems(): readonly PublicLiveTarget[] {
    return filterPublicLiveTargets(this.state.items, this.selectedFilter());
  }

  protected statusLabelKey(target: PublicLiveTarget): string {
    return resolvePublicLiveStatusLabelKey(target);
  }

  protected statusTone(target: PublicLiveTarget): 'open' | 'closed' | 'limited' | 'unknown' {
    return resolvePublicLiveTone(target);
  }

  protected confidenceLabelKey(target: PublicLiveTarget): string | null {
    return target.confidence ? `liveData.confidence.${target.confidence}` : null;
  }

  protected waitMinutes(target: PublicLiveTarget): number | null {
    return resolvePublicLiveWaitMinutes(target);
  }

  protected isClosed(target: PublicLiveTarget): boolean {
    return isPublicLiveTargetClosed(target);
  }

  protected queueLabelKey(queue: PublicLiveQueue): string {
    return `liveData.queue.${queue.kind.charAt(0).toLowerCase()}${queue.kind.slice(1)}`;
  }

  protected queueAvailabilityLabelKey(queue: PublicLiveQueue): string {
    return `liveData.queueAvailability.${queue.availability.charAt(0).toLowerCase()}${queue.availability.slice(1)}`;
  }

  protected queueHasUsableWait(queue: PublicLiveQueue): boolean {
    return isPublicLiveQueueWaitUsable(queue);
  }

  protected isReturnQueue(queue: PublicLiveQueue): boolean {
    return queue.kind === 'ReturnTime' || queue.kind === 'PaidReturnTime';
  }

  protected isBoardingGroupQueue(queue: PublicLiveQueue): boolean {
    return queue.kind === 'BoardingGroup';
  }

  protected formatQueueTime(value: string | null): string | null {
    if (!value) {
      return null;
    }

    const date: Date = new Date(value);
    if (Number.isNaN(date.getTime())) {
      return null;
    }

    try {
      const hasParkTimeZone: boolean = Boolean(this.timeZoneId?.trim());
      const formatted: string = new Intl.DateTimeFormat(this.currentLanguage, {
        timeZone: hasParkTimeZone ? this.timeZoneId! : 'UTC',
        hour: '2-digit',
        minute: '2-digit'
      }).format(date);
      return hasParkTimeZone ? formatted : `${formatted} UTC`;
    } catch {
      return null;
    }
  }

  protected formatQueuePrice(queue: PublicLiveQueue): string | null {
    if (queue.priceMinorUnits === null || !queue.currencyCode) {
      return null;
    }

    try {
      const formatter: Intl.NumberFormat = new Intl.NumberFormat(this.currentLanguage, {
        style: 'currency',
        currency: queue.currencyCode
      });
      const minorUnitDigits: number = formatter.resolvedOptions().maximumFractionDigits ?? 2;
      return formatter.format(queue.priceMinorUnits / (10 ** minorUnitDigits));
    } catch {
      return null;
    }
  }

  protected attributionSources(target: PublicLiveTarget): readonly PublicLiveSource[] {
    return resolvePublicLiveAttributionSources(target, this.mode === 'park' ? this.visibleItems() : []);
  }

  protected freshnessReference(target: PublicLiveTarget): PublicLiveTarget {
    return resolvePublicLiveFreshnessReference(target, this.mode === 'park' ? this.visibleItems() : []);
  }

  protected ageLabelKey(target: PublicLiveTarget): string {
    const ageSeconds: number | null = target.ageSeconds;
    if (ageSeconds === null) {
      return 'liveData.age.unknown';
    }

    if (ageSeconds < 60) {
      return 'liveData.age.justNow';
    }

    if (ageSeconds < 3_600) {
      return 'liveData.age.minutes';
    }

    return 'liveData.age.hours';
  }

  protected ageParameters(target: PublicLiveTarget): { value: number } {
    const ageSeconds: number = Math.max(0, target.ageSeconds ?? 0);
    return {
      value: ageSeconds < 3_600
        ? Math.max(1, Math.floor(ageSeconds / 60))
        : Math.max(1, Math.floor(ageSeconds / 3_600))
    };
  }

  protected localTime(): string | null {
    if (!this.timeZoneId) {
      return null;
    }

    try {
      return new Intl.DateTimeFormat(this.currentLanguage, {
        timeZone: this.timeZoneId,
        hour: '2-digit',
        minute: '2-digit'
      }).format(new Date());
    } catch {
      return null;
    }
  }

  protected itemRoute(target: PublicLiveTarget): string[] | null {
    return buildPublicParkItemRouteCommands({
      language: this.currentLanguage,
      parkId: target.parkId,
      parkName: target.parkDisplayName,
      itemId: target.targetId,
      itemName: target.displayName
    });
  }
}
