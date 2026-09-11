import { ChangeDetectionStrategy, Component, Input, OnChanges, Signal, SimpleChanges, computed, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';

import {
  SharePublicationSettings,
  YearRecapShareHighlight,
  YearRecapSharePreview
} from '@app/models/sharing/share-publication.models';
import { PublicSharePanelComponent } from '@ui/sharing/public-share-panel/public-share-panel.component';
import { UiButtonDirective } from '@ui/primitives';
import { YearRecapShareStateFacade } from '../../state/year-recap-share-state.facade';

@Component({
  selector: 'app-year-recap-share-control',
  templateUrl: './year-recap-share-control.component.html',
  styleUrl: './year-recap-share-control.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [YearRecapShareStateFacade],
  imports: [PublicSharePanelComponent, RouterLink, TranslateModule, UiButtonDirective]
})
export class YearRecapShareControlComponent implements OnChanges {
  @Input() year: number = 0;

  @Input()
  set currentLang(value: string) {
    this.currentLangSignal.set(value.trim() || 'en');
  }

  protected readonly settings: Signal<SharePublicationSettings | null> = this.facade.settings;
  protected readonly loading: Signal<boolean> = this.facade.loading;
  protected readonly saving: Signal<boolean> = this.facade.saving;
  protected readonly previewing: Signal<boolean> = this.facade.previewing;
  protected readonly error: Signal<boolean> = this.facade.error;
  protected readonly editorOpen: Signal<boolean> = this.facade.editorOpen;
  protected readonly includeRideCount: Signal<boolean> = this.facade.includeRideCount;
  protected readonly includeRatings: Signal<boolean> = this.facade.includeRatings;
  protected readonly includeGeography: Signal<boolean> = this.facade.includeGeography;
  protected readonly includeMissedItems: Signal<boolean> = this.facade.includeMissedItems;
  protected readonly includeCaption: Signal<boolean> = this.facade.includeCaption;
  protected readonly publicCaption: Signal<string> = this.facade.publicCaption;
  protected readonly recap: Signal<YearRecapSharePreview | null> = this.facade.yearRecap;
  protected readonly canPublish: Signal<boolean> = this.facade.canPublish;
  protected readonly publicPath: Signal<string | null> = computed(() => {
    const shareId: string = this.settings()?.shareId?.trim() ?? '';
    return shareId.length > 0
      ? `/${this.currentLangSignal()}/passport/shared/years/${encodeURIComponent(shareId)}`
      : null;
  });

  private readonly currentLangSignal = signal<string>('en');

  constructor(protected readonly facade: YearRecapShareStateFacade) {
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['year'] && this.year >= 1 && this.year <= 9999) {
      this.facade.load(this.year);
    }
  }

  protected eventChecked(event: Event): boolean {
    return event.target instanceof HTMLInputElement && event.target.checked;
  }

  protected updateCaption(event: Event): void {
    this.facade.setPublicCaption(event.target instanceof HTMLTextAreaElement ? event.target.value : '');
  }

  protected formatRating(value: number | null | undefined): string {
    return value == null
      ? '–'
      : new Intl.NumberFormat(this.currentLangSignal(), {
        minimumFractionDigits: 1,
        maximumFractionDigits: 1
      }).format(value);
  }

  protected formatPercent(value: number): string {
    return new Intl.NumberFormat(this.currentLangSignal(), {
      style: 'percent',
      maximumFractionDigits: 0
    }).format(value);
  }

  protected categoryKey(category: string): string {
    return `ratings.categories.${category}`;
  }

  protected highlightDetail(highlight: YearRecapShareHighlight): string {
    return highlight.averageRating == null
      ? `${highlight.rideCount}`
      : `${this.formatRating(highlight.averageRating)} / 5 · ${highlight.ratingCount}`;
  }
}
