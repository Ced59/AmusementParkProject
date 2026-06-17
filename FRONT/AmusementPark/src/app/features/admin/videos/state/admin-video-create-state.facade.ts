import { DestroyRef, Inject, Injectable, Signal, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { firstValueFrom } from 'rxjs';

import { ResolvedVideoMetadataDto } from '@app/models/videos/resolved-video-metadata-dto';
import { VideoDto } from '@app/models/videos/video-dto';
import { VideoTagDto } from '@app/models/videos/video-tag-dto';
import { VideoWriteRequest } from '@app/models/videos/video-write-request';

import {
  ADMIN_VIDEO_CREATE_VIDEOS_API_SERVICE_PORT,
  AdminVideoCreateVideosApiServicePort
} from './admin-video-create-state-data.ports';

@Injectable()
export class AdminVideoCreateStateFacade {
  private readonly tagsSignal = signal<VideoTagDto[]>([]);
  private readonly tagsLoadingSignal = signal(false);
  private readonly resolvingMetadataSignal = signal(false);
  private readonly creatingSignal = signal(false);
  private readonly errorKeySignal = signal<string | null>(null);
  private readonly tagsErrorKeySignal = signal<string | null>(null);
  private readonly successKeySignal = signal<string | null>(null);

  public readonly tags: Signal<VideoTagDto[]> = this.tagsSignal.asReadonly();
  public readonly tagsLoading: Signal<boolean> = this.tagsLoadingSignal.asReadonly();
  public readonly resolvingMetadata: Signal<boolean> = this.resolvingMetadataSignal.asReadonly();
  public readonly creating: Signal<boolean> = this.creatingSignal.asReadonly();
  public readonly errorKey: Signal<string | null> = this.errorKeySignal.asReadonly();
  public readonly tagsErrorKey: Signal<string | null> = this.tagsErrorKeySignal.asReadonly();
  public readonly successKey: Signal<string | null> = this.successKeySignal.asReadonly();

  constructor(
    @Inject(ADMIN_VIDEO_CREATE_VIDEOS_API_SERVICE_PORT) private readonly videosPort: AdminVideoCreateVideosApiServicePort,
    private readonly destroyRef: DestroyRef
  ) {
  }

  loadTags(): void {
    this.tagsLoadingSignal.set(true);
    this.tagsErrorKeySignal.set(null);

    this.videosPort.getVideoTags()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (tags: VideoTagDto[]): void => {
          this.tagsSignal.set(tags);
          this.tagsLoadingSignal.set(false);
          this.tagsErrorKeySignal.set(null);
        },
        error: (error: unknown): void => {
          console.error('Error loading admin video tags', error);
          this.tagsSignal.set([]);
          this.tagsLoadingSignal.set(false);
          this.tagsErrorKeySignal.set('admin.videos.tagsLoadError');
        }
      });
  }

  async resolveMetadata(videoUrl: string): Promise<ResolvedVideoMetadataDto | null> {
    const normalizedUrl: string = videoUrl.trim();

    if (!normalizedUrl) {
      return null;
    }

    this.clearMessages();
    this.resolvingMetadataSignal.set(true);

    try {
      return await firstValueFrom(this.videosPort.resolveVideoMetadata(normalizedUrl));
    } catch (error: unknown) {
      console.error('Error resolving admin video metadata', error);
      this.errorKeySignal.set('admin.videos.metadataError');
      return null;
    } finally {
      this.resolvingMetadataSignal.set(false);
    }
  }

  async createVideo(request: VideoWriteRequest): Promise<VideoDto | null> {
    this.clearMessages();
    this.creatingSignal.set(true);

    try {
      const createdVideo: VideoDto = await firstValueFrom(this.videosPort.createVideo(request));
      this.successKeySignal.set('admin.videos.createSuccess');
      return createdVideo;
    } catch (error: unknown) {
      console.error('Error creating admin video', error);
      this.errorKeySignal.set('admin.videos.createError');
      return null;
    } finally {
      this.creatingSignal.set(false);
    }
  }

  clearMessages(): void {
    this.errorKeySignal.set(null);
    this.successKeySignal.set(null);
  }
}
