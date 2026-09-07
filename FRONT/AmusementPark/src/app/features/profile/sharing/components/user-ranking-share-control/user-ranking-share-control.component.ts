import { ChangeDetectionStrategy, Component, Input, OnInit, Signal, computed, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslateModule, TranslateService } from '@ngx-translate/core';

import { UserRankingShareSettings } from '@app/models/ratings/rating.models';
import { PersonalRankingSharePreview, SharePublicationPreview } from '@app/models/sharing/share-publication.models';
import { PublicSharePanelComponent } from '@ui/sharing/public-share-panel/public-share-panel.component';
import { UiButtonDirective } from '@ui/primitives';
import { UserRankingShareStateFacade } from '../../../ratings/user-ranking-share-state.facade';

@Component({
  selector: 'app-user-ranking-share-control',
  templateUrl: './user-ranking-share-control.component.html',
  styleUrl: './user-ranking-share-control.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [PublicSharePanelComponent, RouterLink, TranslateModule, UiButtonDirective]
})
export class UserRankingShareControlComponent implements OnInit {
  @Input() displayName: string = '';

  @Input()
  set currentLang(value: string) {
    this.currentLangSignal.set(value.trim() || 'en');
  }

  protected readonly settings: Signal<UserRankingShareSettings | null> = this.stateFacade.settings;
  protected readonly loading: Signal<boolean> = this.stateFacade.loading;
  protected readonly saving: Signal<boolean> = this.stateFacade.saving;
  protected readonly error: Signal<boolean> = this.stateFacade.error;
  protected readonly editorOpen: Signal<boolean> = this.stateFacade.editorOpen;
  protected readonly includeDisplayName: Signal<boolean> = this.stateFacade.includeDisplayName;
  protected readonly preview: Signal<SharePublicationPreview | null> = this.stateFacade.preview;
  protected readonly previewing: Signal<boolean> = this.stateFacade.previewing;
  protected readonly previewError: Signal<boolean> = this.stateFacade.previewError;
  protected readonly canPublishPreview: Signal<boolean> = this.stateFacade.canPublishPreview;
  protected readonly personalRankingPreview: Signal<PersonalRankingSharePreview | null> = computed(() => {
    return this.preview()?.personalRanking ?? null;
  });
  protected readonly sharedRankingPath: Signal<string | null> = computed(() => {
    const shareId: string = this.settings()?.shareId?.trim() ?? '';
    return shareId.length > 0
      ? `/${this.currentLangSignal()}/rankings/shared/${encodeURIComponent(shareId)}`
      : null;
  });
  protected readonly sharedDisplayName: Signal<string> = computed(() => {
    this.currentLangSignal();
    const includedFields: readonly string[] = this.settings()?.includedFields ?? [];
    if (includedFields.includes('PublicDisplayName')) {
      return this.displayName.trim() || 'Amusement Parks';
    }

    return this.translateService.instant('ratings.share.editor.anonymousProfile') as string;
  });

  private readonly currentLangSignal = signal<string>('en');

  constructor(
    private readonly stateFacade: UserRankingShareStateFacade,
    private readonly translateService: TranslateService
  ) {
  }

  ngOnInit(): void {
    this.stateFacade.load();
  }

  protected openEditor(): void {
    this.stateFacade.openEditor();
  }

  protected closeEditor(): void {
    this.stateFacade.closeEditor();
  }

  protected toggleDisplayName(event: Event): void {
    this.stateFacade.setDisplayNameIncluded((event.target as HTMLInputElement).checked);
  }

  protected preparePreview(): void {
    this.stateFacade.preparePreview();
  }

  protected publishPreview(): void {
    this.stateFacade.publishApprovedPreview();
  }

  protected makePrivate(): void {
    this.stateFacade.setPublic(false);
  }

  protected formatRating(value: number | null | undefined): string {
    const rating: number = Number(value ?? 0);
    return rating > 0
      ? new Intl.NumberFormat(this.currentLangSignal(), {
        minimumFractionDigits: 1,
        maximumFractionDigits: 1
      }).format(rating)
      : '–';
  }

  protected previewSampleCount(preview: PersonalRankingSharePreview): number {
    return Math.min(preview.ratings.length, 3);
  }

  protected previewTotalCount(preview: PersonalRankingSharePreview): number {
    const statisticsTotal: number = Number(preview.statistics?.totalRatings ?? 0);
    return Math.max(statisticsTotal, preview.ratings.length);
  }

  protected previewHasAdditionalRatings(preview: PersonalRankingSharePreview): boolean {
    return this.previewTotalCount(preview) > this.previewSampleCount(preview);
  }
}
