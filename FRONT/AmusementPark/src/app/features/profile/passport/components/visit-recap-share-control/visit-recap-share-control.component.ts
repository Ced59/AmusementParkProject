import { ChangeDetectionStrategy, Component, Input, OnChanges, Signal, SimpleChanges, computed, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';

import {
  SharePublicationSettings,
  VisitRecapShareItem,
  VisitRecapSharePreview
} from '@app/models/sharing/share-publication.models';
import { PublicSharePanelComponent } from '@ui/sharing/public-share-panel/public-share-panel.component';
import { UiButtonDirective } from '@ui/primitives';
import { VisitRecapShareStateFacade } from '../../state/visit-recap-share-state.facade';

@Component({
  selector: 'app-visit-recap-share-control',
  templateUrl: './visit-recap-share-control.component.html',
  styleUrl: './visit-recap-share-control.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [VisitRecapShareStateFacade],
  imports: [PublicSharePanelComponent, RouterLink, TranslateModule, UiButtonDirective]
})
export class VisitRecapShareControlComponent implements OnChanges {
  @Input() visitId: string = '';
  @Input() sourceDatePrecision: string = 'Day';

  @Input()
  set currentLang(value: string) {
    this.currentLangSignal.set(value.trim() || 'en');
  }

  protected readonly settings: Signal<SharePublicationSettings | null> = this.facade.settings;
  protected readonly loading: Signal<boolean> = this.facade.loading;
  protected readonly saving: Signal<boolean> = this.facade.saving;
  protected readonly previewing: Signal<boolean> = this.facade.previewing;
  protected readonly candidatesLoading: Signal<boolean> = this.facade.candidatesLoading;
  protected readonly error: Signal<boolean> = this.facade.error;
  protected readonly editorOpen: Signal<boolean> = this.facade.editorOpen;
  protected readonly datePrecision: Signal<string> = this.facade.datePrecision;
  protected readonly includeRideCount: Signal<boolean> = this.facade.includeRideCount;
  protected readonly includeRatings: Signal<boolean> = this.facade.includeRatings;
  protected readonly includeMissedItems: Signal<boolean> = this.facade.includeMissedItems;
  protected readonly includeCaption: Signal<boolean> = this.facade.includeCaption;
  protected readonly publicCaption: Signal<string> = this.facade.publicCaption;
  protected readonly candidateItems: Signal<VisitRecapShareItem[]> = this.facade.candidateItems;
  protected readonly candidateTotal: Signal<number> = this.facade.candidateTotal;
  protected readonly candidatesTruncated: Signal<boolean> = this.facade.candidatesTruncated;
  protected readonly recap: Signal<VisitRecapSharePreview | null> = this.facade.visitRecap;
  protected readonly canPublish: Signal<boolean> = this.facade.canPublish;
  protected readonly publicPath: Signal<string | null> = computed(() => {
    const shareId: string = this.settings()?.shareId?.trim() ?? '';
    return shareId.length > 0
      ? `/${this.currentLangSignal()}/passport/shared/visits/${encodeURIComponent(shareId)}`
      : null;
  });
  protected readonly datePrecisionOptions: Signal<string[]> = computed(() => {
    const values: string[] = ['Hidden', 'Year', 'Month', 'Day'];
    const sourceIndex: number = values.indexOf(this.sourceDatePrecision);
    return values.slice(0, sourceIndex >= 0 ? sourceIndex + 1 : 1);
  });

  private readonly currentLangSignal = signal<string>('en');

  constructor(protected readonly facade: VisitRecapShareStateFacade) {
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['visitId'] && this.visitId.trim().length > 0) {
      this.facade.load(this.visitId);
    }
  }

  protected openEditor(): void {
    this.facade.openEditor(this.sourceDatePrecision);
  }

  protected updateDatePrecision(event: Event): void {
    this.facade.setDatePrecision(this.eventValue(event));
  }

  protected toggleRideCount(event: Event): void {
    this.facade.setRideCountIncluded(this.eventChecked(event));
  }

  protected toggleRatings(event: Event): void {
    this.facade.setRatingsIncluded(this.eventChecked(event));
  }

  protected toggleMissedItems(event: Event): void {
    this.facade.setMissedItemsIncluded(this.eventChecked(event));
  }

  protected toggleCaption(event: Event): void {
    this.facade.setCaptionIncluded(this.eventChecked(event));
  }

  protected updateCaption(event: Event): void {
    this.facade.setPublicCaption(this.eventValue(event));
  }

  protected toggleItem(parkItemId: string, event: Event): void {
    this.facade.toggleItem(parkItemId, this.eventChecked(event));
  }

  protected preparePreview(): void {
    this.facade.preparePreview(this.visitId);
  }

  protected publish(): void {
    this.facade.publish(this.visitId);
  }

  protected revoke(): void {
    this.facade.revoke(this.visitId);
  }

  protected closeEditor(): void {
    this.facade.closeEditor();
  }

  protected formatRating(value: number | null | undefined): string {
    return value == null
      ? '–'
      : new Intl.NumberFormat(this.currentLangSignal(), {
        minimumFractionDigits: 1,
        maximumFractionDigits: 1
      }).format(value);
  }

  protected formatPublicDate(recap: VisitRecapSharePreview): string {
    if (!recap.date) {
      return '';
    }

    const date: Date = new Date(Date.UTC(recap.date.year, (recap.date.month ?? 1) - 1, recap.date.day ?? 1));
    const options: Intl.DateTimeFormatOptions = recap.date.precision === 'Year'
      ? { year: 'numeric', timeZone: 'UTC' }
      : recap.date.precision === 'Month'
        ? { month: 'long', year: 'numeric', timeZone: 'UTC' }
        : { day: 'numeric', month: 'long', year: 'numeric', timeZone: 'UTC' };
    return new Intl.DateTimeFormat(this.currentLangSignal(), options).format(date);
  }

  protected itemCategoryKey(item: VisitRecapShareItem): string {
    return item.category ? `ratings.categories.${item.category}` : 'visitRecapShare.preview.unknownCategory';
  }

  private eventValue(event: Event): string {
    return event.target instanceof HTMLInputElement
      || event.target instanceof HTMLSelectElement
      || event.target instanceof HTMLTextAreaElement
      ? event.target.value
      : '';
  }

  private eventChecked(event: Event): boolean {
    return event.target instanceof HTMLInputElement && event.target.checked;
  }
}
