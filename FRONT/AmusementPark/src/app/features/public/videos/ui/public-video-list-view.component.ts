import { ChangeDetectionStrategy, Component, EventEmitter, Input, OnChanges, Output, SimpleChanges } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';

import { ImageDisplayComponent } from '@shared/components/image-display/image-display.component';
import { PageStateComponent } from '@shared/components/page-state/page-state.component';
import { ScreenState } from '@shared/models/contracts';
import { VideoType } from '@app/models/videos/video-type';
import { UiButtonDirective, UiChipComponent, UiKickerComponent, UiSurfaceDirective } from '@ui/primitives';
import {
  PublicVideoCardViewModel,
  PublicVideoFilterState,
  PublicVideoSelectOption
} from '../models/public-video-view.model';

export interface PublicVideoBackLink {
  routerLink: string[] | null;
  labelKey: string;
  labelParams?: Record<string, string | number | null | undefined>;
  iconClass: string;
  variant: 'primary' | 'ghost' | 'soft';
}

@Component({
  selector: 'app-public-video-list-view',
  templateUrl: './public-video-list-view.component.html',
  styleUrls: ['./public-video-list-view.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    ImageDisplayComponent,
    PageStateComponent,
    RouterLink,
    TranslateModule,
    UiButtonDirective,
    UiChipComponent,
    UiKickerComponent,
    UiSurfaceDirective
  ]
})
export class PublicVideoListViewComponent implements OnChanges {
  @Input() state: ScreenState<unknown, string> | null = null;
  @Input() videos: PublicVideoCardViewModel[] = [];
  @Input() totalVideos: number = 0;
  @Input() canLoadMore: boolean = false;
  @Input() loadingMore: boolean = false;
  @Input() filters: PublicVideoFilterState = { type: null, tagId: null, creatorName: '' };
  @Input() typeOptions: PublicVideoSelectOption[] = [];
  @Input() tagOptions: PublicVideoSelectOption[] = [];
  @Input() kickerLabelKey: string = 'videos.list.kicker';
  @Input() titleKey: string = 'videos.list.title';
  @Input() titleParams: Record<string, string | number | null | undefined> = {};
  @Input() subtitleKey: string = 'videos.list.subtitle';
  @Input() subtitleParams: Record<string, string | number | null | undefined> = {};
  @Input() loadingTitleKey: string = 'videos.list.loadingTitle';
  @Input() loadingMessageKey: string = 'videos.list.loadingMessage';
  @Input() errorTitleKey: string = 'videos.list.errorTitle';
  @Input() errorMessageKey: string = 'videos.list.errorMessage';
  @Input() emptyTitleKey: string = 'videos.list.emptyTitle';
  @Input() emptyMessageKey: string = 'videos.list.emptyMessage';
  @Input() backLinks: PublicVideoBackLink[] = [];

  @Output() filtersChanged: EventEmitter<PublicVideoFilterState> = new EventEmitter<PublicVideoFilterState>();
  @Output() loadMoreClicked: EventEmitter<void> = new EventEmitter<void>();

  protected readonly thumbnailResponsiveWidths: readonly number[] = [320, 480, 640, 800];
  protected creatorNameDraft: string = '';

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['filters']) {
      this.creatorNameDraft = this.filters.creatorName;
    }
  }

  onTypeChanged(event: Event): void {
    this.emitFilters({
      ...this.filters,
      type: this.readVideoTypeValue(event)
    });
  }

  onTagChanged(event: Event): void {
    this.emitFilters({
      ...this.filters,
      tagId: this.readSelectValue(event)
    });
  }

  onCreatorInput(event: Event): void {
    const target: HTMLInputElement | null = event.target instanceof HTMLInputElement ? event.target : null;
    this.creatorNameDraft = target?.value ?? '';
  }

  applyCreatorFilter(event: Event): void {
    event.preventDefault();
    this.emitFilters({
      ...this.filters,
      creatorName: this.creatorNameDraft.trim()
    });
  }

  clearFilters(): void {
    this.creatorNameDraft = '';
    this.emitFilters({ type: null, tagId: null, creatorName: '' });
  }

  loadMore(): void {
    this.loadMoreClicked.emit();
  }

  private emitFilters(filters: PublicVideoFilterState): void {
    this.filtersChanged.emit(filters);
  }

  private readSelectValue(event: Event): string | null {
    const target: HTMLSelectElement | null = event.target instanceof HTMLSelectElement ? event.target : null;
    const value: string = target?.value?.trim() ?? '';
    return value.length > 0 ? value : null;
  }

  private readVideoTypeValue(event: Event): VideoType | null {
    const value: string | null = this.readSelectValue(event);

    if (!value) {
      return null;
    }

    return Object.values(VideoType).includes(value as VideoType) ? value as VideoType : null;
  }
}
