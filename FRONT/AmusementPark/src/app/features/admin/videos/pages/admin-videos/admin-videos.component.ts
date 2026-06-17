import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { TranslateModule } from '@ngx-translate/core';
import { firstValueFrom } from 'rxjs';

import { LocalizedItemDto } from '@app/models/shared/localized-item-dto';
import { ResolvedVideoMetadataDto } from '@app/models/videos/resolved-video-metadata-dto';
import { VideoDto } from '@app/models/videos/video-dto';
import { VideoHostingProvider } from '@app/models/videos/video-hosting-provider';
import { VideoOwnerType } from '@app/models/videos/video-owner-type';
import { VideoSearchQuery } from '@app/models/videos/video-search-query';
import { VideoTagDto } from '@app/models/videos/video-tag-dto';
import { VideoType } from '@app/models/videos/video-type';
import { VideoWriteRequest } from '@app/models/videos/video-write-request';
import { AdminVideosStateFacade } from '@features/admin/videos/state/admin-videos-state.facade';
import { ImageDisplayComponent } from '@shared/components/image-display/image-display.component';
import { PageStateComponent } from '@shared/components/page-state/page-state.component';

type VideoSortField = 'created' | 'updated' | 'title' | 'published';
type VideoSortDirection = 'asc' | 'desc';

interface AdminVideoDraft {
  originalUrl: string;
  ownerType: VideoOwnerType;
  ownerId: string;
  type: VideoType;
  title: string;
  description: string;
  creatorName: string;
  creatorUrl: string;
  thumbnailUrl: string;
  durationSeconds: number | null;
  publishedAtUtc: string;
  titleFr: string;
  descriptionFr: string;
  tagIds: string[];
  isPublished: boolean;
}

@Component({
  selector: 'app-admin-videos',
  standalone: true,
  imports: [CommonModule, FormsModule, TranslateModule, PageStateComponent, ImageDisplayComponent],
  templateUrl: './admin-videos.component.html',
  styleUrl: './admin-videos.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [AdminVideosStateFacade],
})
export class AdminVideosComponent implements OnInit {
  protected readonly state = this.stateFacade.state;
  protected readonly videos = this.stateFacade.videos;
  protected readonly tags = this.stateFacade.tags;
  protected readonly selectedVideo = this.stateFacade.selectedVideo;
  protected readonly query = this.stateFacade.query;
  protected readonly pagination = this.stateFacade.pagination;
  protected readonly operationErrorKey = this.stateFacade.operationErrorKey;
  protected readonly providers: VideoHostingProvider[] = [VideoHostingProvider.YOUTUBE, VideoHostingProvider.DAILYMOTION, VideoHostingProvider.VIMEO, VideoHostingProvider.OTHER];
  protected readonly ownerTypes: VideoOwnerType[] = [VideoOwnerType.PARK, VideoOwnerType.PARK_ITEM];
  protected readonly videoTypes: VideoType[] = [
    VideoType.ON_RIDE,
    VideoType.OFF_RIDE,
    VideoType.WALKTHROUGH,
    VideoType.ADVERTISEMENT,
    VideoType.DOCUMENTARY,
    VideoType.REVIEW,
    VideoType.NEWS,
    VideoType.EVENT,
    VideoType.INTERVIEW,
    VideoType.OTHER,
  ];
  protected readonly sortFields: VideoSortField[] = ['created', 'updated', 'title', 'published'];
  protected readonly pageSizes: number[] = [12, 24, 48, 96];
  protected readonly defaultLanguageCode: string = 'fr';

  protected draft: AdminVideoDraft = this.createEmptyDraft();
  protected resolvedMetadata: ResolvedVideoMetadataDto | null = null;
  protected selectedResolvedMetadata: ResolvedVideoMetadataDto | null = null;
  protected newTagSlug: string = '';

  constructor(private readonly stateFacade: AdminVideosStateFacade) {
  }

  ngOnInit(): void {
    this.reload();
  }

  protected reload(): void {
    this.stateFacade.reload();
  }

  protected applyQuery(): void {
    this.stateFacade.applyQuery();
  }

  protected clearFilters(): void {
    this.stateFacade.clearFilters();
  }

  protected updateSearch(value: string): void {
    this.stateFacade.updateQuery({ search: value || null });
  }

  protected updateProvider(value: string): void {
    this.stateFacade.updateQuery({ hostingProvider: value ? value as VideoHostingProvider : null });
  }

  protected updateOwnerTypeFilter(value: string): void {
    this.stateFacade.updateQuery({ ownerType: value ? value as VideoOwnerType : null });
  }

  protected updateOwnerIdFilter(value: string): void {
    this.stateFacade.updateQuery({ ownerId: value || null });
  }

  protected updateTypeFilter(value: string): void {
    this.stateFacade.updateQuery({ type: value ? value as VideoType : null });
  }

  protected updateTagFilter(value: string): void {
    this.stateFacade.updateQuery({ tagId: value || null });
  }

  protected updateCreatorFilter(value: string): void {
    this.stateFacade.updateQuery({ creatorName: value || null });
  }

  protected updatePublishedFilter(value: string): void {
    this.stateFacade.updateQuery({ isPublished: this.parseBooleanFilter(value) });
  }

  protected updateSortBy(value: string): void {
    this.stateFacade.updateQuery({ sortBy: value as VideoSortField }, false);
  }

  protected updateSortDirection(value: string): void {
    this.stateFacade.updateQuery({ sortDirection: value as VideoSortDirection }, false);
  }

  protected changePage(page: number): void {
    const totalPages: number = this.pagination().totalPages || 1;
    const targetPage: number = Math.min(Math.max(1, page), totalPages);
    this.stateFacade.changePage(targetPage);
  }

  protected changePageSize(size: string): void {
    this.stateFacade.changePageSize(Number(size));
  }

  protected selectVideo(video: VideoDto): void {
    this.selectedResolvedMetadata = null;
    this.stateFacade.selectVideo(video);
  }

  protected async resolveDraftMetadata(): Promise<void> {
    const originalUrl: string = this.draft.originalUrl.trim();

    if (!originalUrl) {
      return;
    }

    try {
      const metadata: ResolvedVideoMetadataDto = await firstValueFrom(this.stateFacade.resolveVideoMetadata(originalUrl));
      this.resolvedMetadata = metadata;
      this.applyMetadataToDraft(metadata);
    } catch {
      this.stateFacade.setError();
    }
  }

  protected async createVideo(): Promise<void> {
    if (!this.canSubmitDraft()) {
      return;
    }

    try {
      await firstValueFrom(this.stateFacade.createVideo(this.buildVideoRequestFromDraft(this.draft)));
      this.draft = this.createEmptyDraft();
      this.resolvedMetadata = null;
      this.reload();
    } catch {
      this.stateFacade.setError();
    }
  }

  protected async resolveSelectedMetadata(): Promise<void> {
    const selectedVideo: VideoDto | null = this.selectedVideo();
    const originalUrl: string = selectedVideo?.originalUrl?.trim() ?? '';

    if (!selectedVideo || !originalUrl) {
      return;
    }

    try {
      const metadata: ResolvedVideoMetadataDto = await firstValueFrom(this.stateFacade.resolveVideoMetadata(originalUrl));
      this.selectedResolvedMetadata = metadata;
      this.applyMetadataToSelectedVideo(metadata);
    } catch {
      this.stateFacade.setError();
    }
  }

  protected async saveVideo(): Promise<void> {
    const selectedVideo: VideoDto | null = this.selectedVideo();

    if (!selectedVideo) {
      return;
    }

    try {
      await firstValueFrom(this.stateFacade.updateVideo(selectedVideo.id, this.buildVideoRequestFromVideo(selectedVideo)));
      this.selectedResolvedMetadata = null;
      this.reload();
    } catch {
      this.stateFacade.setError();
    }
  }

  protected async deleteVideo(): Promise<void> {
    const selectedVideo: VideoDto | null = this.selectedVideo();

    if (!selectedVideo) {
      return;
    }

    try {
      await firstValueFrom(this.stateFacade.deleteVideo(selectedVideo.id));
      this.selectedResolvedMetadata = null;
      this.reload();
    } catch {
      this.stateFacade.setError();
    }
  }

  protected async createTag(): Promise<void> {
    const slug: string = this.newTagSlug.trim().toLowerCase();

    if (!slug) {
      return;
    }

    try {
      await firstValueFrom(this.stateFacade.createTag({
        slug,
        labels: [{ languageCode: this.defaultLanguageCode, value: slug }],
        descriptions: [],
      }));
      this.newTagSlug = '';
      this.reload();
    } catch {
      this.stateFacade.setError();
    }
  }

  protected toggleDraftTag(tagId: string, checked: boolean): void {
    this.draft.tagIds = this.toggleTagId(this.draft.tagIds, tagId, checked);
  }

  protected toggleSelectedTag(tagId: string, checked: boolean): void {
    this.stateFacade.toggleTag(tagId, checked);
  }

  protected updateSelectedVideo(patch: Partial<VideoDto>): void {
    this.stateFacade.updateSelectedVideo(patch);
  }

  protected updateSelectedOwnerType(value: string): void {
    this.stateFacade.updateSelectedVideo({ ownerType: value as VideoOwnerType });
  }

  protected updateSelectedType(value: string): void {
    this.stateFacade.updateSelectedVideo({ type: value as VideoType });
  }

  protected updateSelectedDuration(value: string | number): void {
    this.stateFacade.updateSelectedVideo({ durationSeconds: this.toNullableNumber(value) });
  }

  protected updateSelectedLocalizedField(field: 'titles' | 'descriptions', value: string): void {
    const selectedVideo: VideoDto | null = this.selectedVideo();

    if (!selectedVideo) {
      return;
    }

    const values: LocalizedItemDto<string>[] = this.upsertLocalizedValue(selectedVideo[field] ?? [], value);
    this.stateFacade.updateSelectedVideo({ [field]: values } as Partial<VideoDto>);
  }

  protected getLocalizedField(video: VideoDto, field: 'titles' | 'descriptions'): string {
    return video[field]?.find((item: LocalizedItemDto<string>) => item.languageCode === this.defaultLanguageCode)?.value ?? '';
  }

  protected getThumbnailPathOrUrl(video: VideoDto): string | null {
    return video.thumbnailImageId?.trim() || video.thumbnailUrl?.trim() || null;
  }

  protected getTagSlug(tagId: string): string {
    return this.tags().find((tag: VideoTagDto) => tag.id === tagId)?.slug ?? tagId;
  }

  protected formatDuration(value: number | null | undefined): string {
    if (value === null || value === undefined || value <= 0) {
      return '-';
    }

    const totalSeconds: number = Math.round(value);
    const minutes: number = Math.floor(totalSeconds / 60);
    const seconds: number = totalSeconds % 60;

    return `${minutes}:${seconds.toString().padStart(2, '0')}`;
  }

  protected formatDate(value: string | null | undefined): string {
    if (!value) {
      return '-';
    }

    return new Intl.DateTimeFormat('fr-FR', {
      day: '2-digit',
      month: '2-digit',
      year: 'numeric',
      hour: '2-digit',
      minute: '2-digit',
    }).format(new Date(value));
  }

  protected trackById(_: number, item: { id: string }): string {
    return item.id;
  }

  private createEmptyDraft(): AdminVideoDraft {
    return {
      originalUrl: '',
      ownerType: VideoOwnerType.PARK,
      ownerId: '',
      type: VideoType.ON_RIDE,
      title: '',
      description: '',
      creatorName: '',
      creatorUrl: '',
      thumbnailUrl: '',
      durationSeconds: null,
      publishedAtUtc: '',
      titleFr: '',
      descriptionFr: '',
      tagIds: [],
      isPublished: true,
    };
  }

  private applyMetadataToDraft(metadata: ResolvedVideoMetadataDto): void {
    this.draft.originalUrl = metadata.originalUrl || this.draft.originalUrl;
    this.draft.title = metadata.title || this.draft.title;
    this.draft.description = metadata.description || this.draft.description;
    this.draft.creatorName = metadata.creatorName || this.draft.creatorName;
    this.draft.creatorUrl = metadata.creatorUrl || metadata.providerChannelUrl || this.draft.creatorUrl;
    this.draft.thumbnailUrl = metadata.thumbnailUrl || this.draft.thumbnailUrl;
    this.draft.durationSeconds = metadata.durationSeconds ?? this.draft.durationSeconds;
    this.draft.publishedAtUtc = metadata.publishedAtUtc || this.draft.publishedAtUtc;
    this.draft.titleFr = this.draft.titleFr || metadata.title || '';
    this.draft.descriptionFr = this.draft.descriptionFr || metadata.description || '';
  }

  private applyMetadataToSelectedVideo(metadata: ResolvedVideoMetadataDto): void {
    this.stateFacade.updateSelectedVideo({
      title: metadata.title || this.selectedVideo()?.title || '',
      description: metadata.description || this.selectedVideo()?.description || null,
      creatorName: metadata.creatorName || this.selectedVideo()?.creatorName || null,
      creatorUrl: metadata.creatorUrl || metadata.providerChannelUrl || this.selectedVideo()?.creatorUrl || null,
      thumbnailUrl: metadata.thumbnailUrl || this.selectedVideo()?.thumbnailUrl || null,
      durationSeconds: metadata.durationSeconds ?? this.selectedVideo()?.durationSeconds ?? null,
      publishedAtUtc: metadata.publishedAtUtc || this.selectedVideo()?.publishedAtUtc || null,
    });
  }

  private buildVideoRequestFromDraft(draft: AdminVideoDraft): VideoWriteRequest {
    return {
      originalUrl: draft.originalUrl.trim(),
      ownerType: draft.ownerType,
      ownerId: draft.ownerId.trim(),
      type: draft.type,
      title: this.normalizeOptionalString(draft.title),
      description: this.normalizeOptionalString(draft.description),
      creatorName: this.normalizeOptionalString(draft.creatorName),
      creatorUrl: this.normalizeOptionalString(draft.creatorUrl),
      thumbnailUrl: this.normalizeOptionalString(draft.thumbnailUrl),
      durationSeconds: draft.durationSeconds,
      publishedAtUtc: this.normalizeOptionalString(draft.publishedAtUtc),
      titles: this.upsertLocalizedValue([], draft.titleFr),
      descriptions: this.upsertLocalizedValue([], draft.descriptionFr),
      tagIds: draft.tagIds,
      isPublished: draft.isPublished,
    };
  }

  private buildVideoRequestFromVideo(video: VideoDto): VideoWriteRequest {
    return {
      originalUrl: video.originalUrl.trim(),
      ownerType: video.ownerType,
      ownerId: video.ownerId?.trim() ?? '',
      type: video.type,
      title: this.normalizeOptionalString(video.title),
      description: this.normalizeOptionalString(video.description),
      creatorName: this.normalizeOptionalString(video.creatorName),
      creatorUrl: this.normalizeOptionalString(video.creatorUrl),
      thumbnailUrl: this.normalizeOptionalString(video.thumbnailUrl),
      durationSeconds: video.durationSeconds ?? null,
      publishedAtUtc: this.normalizeOptionalString(video.publishedAtUtc),
      titles: video.titles ?? [],
      descriptions: video.descriptions ?? [],
      tagIds: video.tagIds ?? [],
      isPublished: video.isPublished,
    };
  }

  protected canSubmitDraft(): boolean {
    return !!this.draft.originalUrl.trim() && !!this.draft.ownerId.trim() && this.ownerTypes.includes(this.draft.ownerType);
  }

  private parseBooleanFilter(value: string): boolean | null {
    if (value === 'true') {
      return true;
    }

    if (value === 'false') {
      return false;
    }

    return null;
  }

  private toggleTagId(currentTagIds: string[], tagId: string, checked: boolean): string[] {
    const tagIds: Set<string> = new Set(currentTagIds);

    if (checked) {
      tagIds.add(tagId);
    } else {
      tagIds.delete(tagId);
    }

    return Array.from(tagIds);
  }

  private upsertLocalizedValue(values: LocalizedItemDto<string>[], value: string): LocalizedItemDto<string>[] {
    const others: LocalizedItemDto<string>[] = values.filter((item: LocalizedItemDto<string>) => item.languageCode !== this.defaultLanguageCode);

    if (!value.trim()) {
      return others;
    }

    return [
      ...others,
      {
        languageCode: this.defaultLanguageCode,
        value: value.trim(),
      },
    ];
  }

  private normalizeOptionalString(value: string | null | undefined): string | null {
    const normalizedValue: string = value?.trim() ?? '';
    return normalizedValue.length > 0 ? normalizedValue : null;
  }

  private toNullableNumber(value: string | number): number | null {
    if (value === null || value === undefined || String(value).trim() === '') {
      return null;
    }

    const numericValue: number = Number(value);
    return Number.isFinite(numericValue) ? numericValue : null;
  }
}
