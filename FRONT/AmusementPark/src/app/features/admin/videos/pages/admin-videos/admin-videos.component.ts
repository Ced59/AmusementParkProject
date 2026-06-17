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
import { LANGUAGES, LanguageOption } from '@shared/models/localization';
import { PageStateComponent } from '@shared/components/page-state/page-state.component';

type VideoSortField = 'created' | 'updated' | 'title' | 'published';
type VideoSortDirection = 'asc' | 'desc';

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
  protected readonly languages: readonly LanguageOption[] = LANGUAGES;

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

  protected updateLanguageFilter(value: string): void {
    this.stateFacade.updateQuery({ languageCode: value || null });
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
        labels: this.languages.map((language: LanguageOption) => ({ languageCode: language.value, value: slug })),
        descriptions: [],
      }));
      this.newTagSlug = '';
      this.reload();
    } catch {
      this.stateFacade.setError();
    }
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

  protected updateSelectedLanguage(value: string): void {
    this.stateFacade.updateSelectedVideo({ languageCodes: this.toLanguageCodes(value) });
  }

  protected getSelectedLanguage(video: VideoDto): string {
    return video.languageCodes?.[0] ?? 'all';
  }

  protected updateSelectedDuration(value: string | number): void {
    this.stateFacade.updateSelectedVideo({ durationSeconds: this.toNullableNumber(value) });
  }

  protected updateSelectedLocalizedField(field: 'titles' | 'descriptions', languageCode: string, value: string): void {
    const selectedVideo: VideoDto | null = this.selectedVideo();

    if (!selectedVideo) {
      return;
    }

    const values: LocalizedItemDto<string>[] = this.upsertLocalizedValue(selectedVideo[field] ?? [], languageCode, value);
    this.stateFacade.updateSelectedVideo({ [field]: values } as Partial<VideoDto>);
  }

  protected getLocalizedField(video: VideoDto, field: 'titles' | 'descriptions', languageCode: string): string {
    return video[field]?.find((item: LocalizedItemDto<string>) => item.languageCode === languageCode)?.value ?? '';
  }

  protected getThumbnailPathOrUrl(video: VideoDto): string | null {
    return video.thumbnailImageId?.trim() || video.thumbnailUrl?.trim() || null;
  }

  protected getTagSlug(tagId: string): string {
    return this.tags().find((tag: VideoTagDto) => tag.id === tagId)?.slug ?? tagId;
  }

  protected formatProviderViewCount(video: VideoDto): string {
    const providerViewCount: number | null | undefined = video.externalMetadata?.providerViewCount;

    if (providerViewCount === null || providerViewCount === undefined || !Number.isFinite(providerViewCount) || providerViewCount < 0) {
      return '-';
    }

    return new Intl.NumberFormat('fr-FR').format(Math.round(providerViewCount));
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

  private applyMetadataToSelectedVideo(metadata: ResolvedVideoMetadataDto): void {
    this.stateFacade.updateSelectedVideo({
      title: metadata.title || this.selectedVideo()?.title || '',
      description: metadata.description || this.selectedVideo()?.description || null,
      creatorName: metadata.creatorName || this.selectedVideo()?.creatorName || null,
      creatorUrl: metadata.creatorUrl || metadata.providerChannelUrl || this.selectedVideo()?.creatorUrl || null,
      thumbnailUrl: metadata.thumbnailUrl || this.selectedVideo()?.thumbnailUrl || null,
      durationSeconds: metadata.durationSeconds ?? this.selectedVideo()?.durationSeconds ?? null,
      publishedAtUtc: metadata.publishedAtUtc || this.selectedVideo()?.publishedAtUtc || null,
      titles: this.applyMetadataToLocalizedValues(this.selectedVideo()?.titles ?? [], metadata.title),
      descriptions: this.applyMetadataToLocalizedValues(this.selectedVideo()?.descriptions ?? [], metadata.description),
    });
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
      languageCodes: video.languageCodes ?? [],
      titles: video.titles ?? [],
      descriptions: video.descriptions ?? [],
      tagIds: video.tagIds ?? [],
      isPublished: video.isPublished,
    };
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

  private applyMetadataToLocalizedValues(values: LocalizedItemDto<string>[], value: string | null | undefined): LocalizedItemDto<string>[] {
    const normalizedValue: string = value?.trim() ?? '';

    if (!normalizedValue) {
      return values;
    }

    return this.languages.reduce((accumulator: LocalizedItemDto<string>[], language: LanguageOption) => {
      const existingValue: string = accumulator.find((item: LocalizedItemDto<string>) => item.languageCode === language.value)?.value?.trim() ?? '';
      return existingValue
        ? accumulator
        : this.upsertLocalizedValue(accumulator, language.value, normalizedValue);
    }, values);
  }

  private upsertLocalizedValue(values: LocalizedItemDto<string>[], languageCode: string, value: string): LocalizedItemDto<string>[] {
    const normalizedLanguageCode: string = languageCode.trim().toLowerCase();
    const others: LocalizedItemDto<string>[] = values.filter((item: LocalizedItemDto<string>) => item.languageCode !== normalizedLanguageCode);

    if (!value.trim()) {
      return others;
    }

    return [
      ...others,
      {
        languageCode: normalizedLanguageCode,
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

  private toLanguageCodes(languageCode: string): string[] {
    const normalizedLanguageCode: string = languageCode.trim().toLowerCase();
    return normalizedLanguageCode === 'all' || normalizedLanguageCode.length === 0
      ? []
      : [normalizedLanguageCode];
  }
}
