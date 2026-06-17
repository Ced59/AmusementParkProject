import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, EventEmitter, Input, OnChanges, OnInit, Output, SimpleChanges } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { TranslateModule } from '@ngx-translate/core';

import { LocalizedItemDto } from '@app/models/shared/localized-item-dto';
import { ResolvedVideoMetadataDto } from '@app/models/videos/resolved-video-metadata-dto';
import { VideoDto } from '@app/models/videos/video-dto';
import { VideoOwnerType } from '@app/models/videos/video-owner-type';
import { VideoType } from '@app/models/videos/video-type';
import { VideoWriteRequest } from '@app/models/videos/video-write-request';
import { ImageDisplayComponent } from '@shared/components/image-display/image-display.component';
import { LANGUAGES, LanguageOption } from '@shared/models/localization';
import { AdminVideoCreateStateFacade } from '@features/admin/videos/state/admin-video-create-state.facade';

interface AdminVideoLocalizedDraftField {
  languageCode: string;
  languageLabel: string;
  value: string;
}

interface AdminVideoCreateDraft {
  originalUrl: string;
  type: VideoType;
  languageCode: string;
  title: string;
  description: string;
  creatorName: string;
  creatorUrl: string;
  thumbnailUrl: string;
  durationSeconds: number | null;
  publishedAtUtc: string;
  titles: AdminVideoLocalizedDraftField[];
  descriptions: AdminVideoLocalizedDraftField[];
  tagIds: string[];
  isPublished: boolean;
}

@Component({
  selector: 'app-admin-video-create-panel',
  standalone: true,
  imports: [CommonModule, FormsModule, TranslateModule, ImageDisplayComponent],
  templateUrl: './admin-video-create-panel.component.html',
  styleUrl: './admin-video-create-panel.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [AdminVideoCreateStateFacade],
})
export class AdminVideoCreatePanelComponent implements OnInit, OnChanges {
  @Input({ required: true }) ownerType!: VideoOwnerType;
  @Input({ required: true }) ownerId!: string | null;
  @Input() ownerName: string | null = null;

  @Output() videoCreated: EventEmitter<VideoDto> = new EventEmitter<VideoDto>();

  protected readonly tags = this.stateFacade.tags;
  protected readonly tagsLoading = this.stateFacade.tagsLoading;
  protected readonly resolvingMetadata = this.stateFacade.resolvingMetadata;
  protected readonly creating = this.stateFacade.creating;
  protected readonly errorKey = this.stateFacade.errorKey;
  protected readonly tagsErrorKey = this.stateFacade.tagsErrorKey;
  protected readonly successKey = this.stateFacade.successKey;
  protected readonly languages: readonly LanguageOption[] = LANGUAGES;
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

  protected draft: AdminVideoCreateDraft = this.createEmptyDraft();
  protected resolvedMetadata: ResolvedVideoMetadataDto | null = null;

  private ownerKey: string = '';

  constructor(private readonly stateFacade: AdminVideoCreateStateFacade) {
  }

  ngOnInit(): void {
    this.stateFacade.loadTags();
    this.ownerKey = this.buildOwnerKey();
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (!changes['ownerId'] && !changes['ownerType']) {
      return;
    }

    const nextOwnerKey: string = this.buildOwnerKey();

    if (this.ownerKey && this.ownerKey !== nextOwnerKey) {
      this.resetDraft();
    }

    this.ownerKey = nextOwnerKey;
  }

  protected async resolveDraftMetadata(): Promise<void> {
    const metadata: ResolvedVideoMetadataDto | null = await this.stateFacade.resolveMetadata(this.draft.originalUrl);

    if (!metadata) {
      return;
    }

    this.resolvedMetadata = metadata;
    this.applyMetadataToDraft(metadata);
  }

  protected async createVideo(): Promise<void> {
    const ownerId: string = this.ownerId?.trim() ?? '';

    if (!this.canSubmitDraft() || !ownerId) {
      return;
    }

    const createdVideo: VideoDto | null = await this.stateFacade.createVideo(this.buildVideoRequestFromDraft(ownerId));

    if (!createdVideo) {
      return;
    }

    this.videoCreated.emit(createdVideo);
    this.resetDraft(false);
  }

  protected toggleDraftTag(tagId: string, checked: boolean): void {
    const tagIds: Set<string> = new Set(this.draft.tagIds);

    if (checked) {
      tagIds.add(tagId);
    } else {
      tagIds.delete(tagId);
    }

    this.draft.tagIds = Array.from(tagIds);
  }

  protected canSubmitDraft(): boolean {
    return !!this.ownerId?.trim() && !!this.draft.originalUrl.trim();
  }

  protected trackById(_: number, item: { id: string }): string {
    return item.id;
  }

  private resetDraft(clearMessages: boolean = true): void {
    this.draft = this.createEmptyDraft();
    this.resolvedMetadata = null;

    if (clearMessages) {
      this.stateFacade.clearMessages();
    }
  }

  private createEmptyDraft(): AdminVideoCreateDraft {
    return {
      originalUrl: '',
      type: VideoType.ON_RIDE,
      languageCode: 'all',
      title: '',
      description: '',
      creatorName: '',
      creatorUrl: '',
      thumbnailUrl: '',
      durationSeconds: null,
      publishedAtUtc: '',
      titles: this.createLocalizedDraftFields(),
      descriptions: this.createLocalizedDraftFields(),
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
    this.draft.languageCode = this.resolveMetadataLanguageCode(metadata.detectedLanguageCode);
    this.draft.titles = this.applyMetadataToLocalizedDraftFields(this.draft.titles, metadata.title);
    this.draft.descriptions = this.applyMetadataToLocalizedDraftFields(this.draft.descriptions, metadata.description);
  }

  private buildVideoRequestFromDraft(ownerId: string): VideoWriteRequest {
    return {
      originalUrl: this.draft.originalUrl.trim(),
      ownerType: this.ownerType,
      ownerId,
      type: this.draft.type,
      title: this.normalizeOptionalString(this.draft.title),
      description: this.normalizeOptionalString(this.draft.description),
      creatorName: this.normalizeOptionalString(this.draft.creatorName),
      creatorUrl: this.normalizeOptionalString(this.draft.creatorUrl),
      thumbnailUrl: this.normalizeOptionalString(this.draft.thumbnailUrl),
      durationSeconds: this.draft.durationSeconds,
      publishedAtUtc: this.normalizeOptionalString(this.draft.publishedAtUtc),
      languageCodes: this.toLanguageCodes(this.draft.languageCode),
      titles: this.mapLocalizedDraftFields(this.draft.titles),
      descriptions: this.mapLocalizedDraftFields(this.draft.descriptions),
      tagIds: this.draft.tagIds,
      isPublished: this.draft.isPublished,
    };
  }

  private createLocalizedDraftFields(): AdminVideoLocalizedDraftField[] {
    return this.languages.map((language: LanguageOption) => ({
      languageCode: language.value,
      languageLabel: language.label,
      value: '',
    }));
  }

  private applyMetadataToLocalizedDraftFields(fields: AdminVideoLocalizedDraftField[], value: string | null | undefined): AdminVideoLocalizedDraftField[] {
    const normalizedValue: string = value?.trim() ?? '';

    if (!normalizedValue) {
      return fields;
    }

    return fields.map((field: AdminVideoLocalizedDraftField) => ({
      ...field,
      value: field.value.trim() ? field.value : normalizedValue,
    }));
  }

  private mapLocalizedDraftFields(fields: AdminVideoLocalizedDraftField[]): LocalizedItemDto<string>[] {
    return fields
      .filter((field: AdminVideoLocalizedDraftField) => field.value.trim().length > 0)
      .map((field: AdminVideoLocalizedDraftField) => ({
        languageCode: field.languageCode,
        value: field.value.trim(),
      }));
  }

  private normalizeOptionalString(value: string | null | undefined): string | null {
    const normalizedValue: string = value?.trim() ?? '';
    return normalizedValue.length > 0 ? normalizedValue : null;
  }

  private resolveMetadataLanguageCode(languageCode: string | null | undefined): string {
    const normalizedLanguageCode: string = languageCode?.trim().toLowerCase().slice(0, 2) ?? '';
    return this.languages.some((language: LanguageOption) => language.value === normalizedLanguageCode)
      ? normalizedLanguageCode
      : this.draft.languageCode;
  }

  private toLanguageCodes(languageCode: string): string[] {
    const normalizedLanguageCode: string = languageCode.trim().toLowerCase();
    return normalizedLanguageCode === 'all' || normalizedLanguageCode.length === 0
      ? []
      : [normalizedLanguageCode];
  }

  private buildOwnerKey(): string {
    return `${this.ownerType ?? ''}:${this.ownerId ?? ''}`;
  }
}
