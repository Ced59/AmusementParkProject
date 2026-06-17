import { TestBed } from '@angular/core/testing';
import { Observable, of, throwError } from 'rxjs';

import { ResolvedVideoMetadataDto } from '@app/models/videos/resolved-video-metadata-dto';
import { VideoDto } from '@app/models/videos/video-dto';
import { VideoHostingProvider } from '@app/models/videos/video-hosting-provider';
import { VideoOwnerType } from '@app/models/videos/video-owner-type';
import { VideoTagDto } from '@app/models/videos/video-tag-dto';
import { VideoType } from '@app/models/videos/video-type';
import { VideoWriteRequest } from '@app/models/videos/video-write-request';

import {
  ADMIN_VIDEO_CREATE_VIDEOS_API_SERVICE_PORT,
  AdminVideoCreateVideosApiServicePort
} from './admin-video-create-state-data.ports';
import { AdminVideoCreateStateFacade } from './admin-video-create-state.facade';

class FakeVideosPort implements AdminVideoCreateVideosApiServicePort {
  public tagsResponse$: Observable<VideoTagDto[]> = of([createTag('official')]);
  public metadataResponse$: Observable<ResolvedVideoMetadataDto> = of(createMetadata());
  public createResponse$: Observable<VideoDto> = of(createVideo('video-1'));
  public lastCreateRequest: VideoWriteRequest | null = null;

  getVideoTags(): Observable<VideoTagDto[]> {
    return this.tagsResponse$;
  }

  resolveVideoMetadata(videoUrl: string): Observable<ResolvedVideoMetadataDto> {
    return this.metadataResponse$;
  }

  createVideo(request: VideoWriteRequest): Observable<VideoDto> {
    this.lastCreateRequest = request;
    return this.createResponse$;
  }
}

function createTag(id: string): VideoTagDto {
  return {
    id,
    slug: id,
    labels: [],
    descriptions: [],
    isActive: true,
    createdAt: '2026-01-01T00:00:00Z',
    updatedAt: '2026-01-01T00:00:00Z',
  };
}

function createMetadata(): ResolvedVideoMetadataDto {
  return {
    hostingProvider: VideoHostingProvider.YOUTUBE,
    originalUrl: 'https://www.youtube.com/watch?v=abcdefghijk',
    canonicalUrl: 'https://www.youtube.com/watch?v=abcdefghijk',
    title: 'Onride test',
  };
}

function createVideo(id: string): VideoDto {
  return {
    id,
    hostingProvider: VideoHostingProvider.YOUTUBE,
    ownerType: VideoOwnerType.PARK,
    ownerId: 'park-1',
    type: VideoType.ON_RIDE,
    originalUrl: 'https://www.youtube.com/watch?v=abcdefghijk',
    canonicalUrl: 'https://www.youtube.com/watch?v=abcdefghijk',
    embedUrl: 'https://www.youtube.com/embed/abcdefghijk',
    externalId: 'abcdefghijk',
    title: 'Onride test',
    descriptions: [],
    titles: [],
    languageCodes: [],
    tagIds: [],
    externalMetadata: {},
    isPublished: true,
    createdAt: '2026-01-01T00:00:00Z',
    updatedAt: '2026-01-01T00:00:00Z',
  };
}

describe('AdminVideoCreateStateFacade', () => {
  let facade: AdminVideoCreateStateFacade;
  let port: FakeVideosPort;

  beforeEach(() => {
    port = new FakeVideosPort();

    TestBed.configureTestingModule({
      providers: [
        AdminVideoCreateStateFacade,
        { provide: ADMIN_VIDEO_CREATE_VIDEOS_API_SERVICE_PORT, useValue: port },
      ],
    });

    facade = TestBed.inject(AdminVideoCreateStateFacade);
  });

  it('loads available video tags for contextual creation', () => {
    facade.loadTags();

    expect(facade.tags().map((tag: VideoTagDto) => tag.id)).toEqual(['official']);
    expect(facade.errorKey()).toBeNull();
  });

  it('keeps contextual creation available when tags fail to load', () => {
    port.tagsResponse$ = throwError(() => new Error('network'));

    facade.loadTags();

    expect(facade.tags()).toEqual([]);
    expect(facade.errorKey()).toBe('admin.videos.tagsLoadError');
  });

  it('resolves metadata and creates a video request', async () => {
    const metadata: ResolvedVideoMetadataDto | null = await facade.resolveMetadata(' https://www.youtube.com/watch?v=abcdefghijk ');

    expect(metadata?.title).toBe('Onride test');

    const createdVideo: VideoDto | null = await facade.createVideo({
      originalUrl: 'https://www.youtube.com/watch?v=abcdefghijk',
      ownerType: VideoOwnerType.PARK,
      ownerId: 'park-1',
      type: VideoType.ON_RIDE,
      title: 'Onride test',
      description: null,
      creatorName: null,
      creatorUrl: null,
      thumbnailUrl: null,
      durationSeconds: null,
      publishedAtUtc: null,
      languageCodes: ['fr'],
      titles: [],
      descriptions: [],
      tagIds: [],
      isPublished: true,
    });

    expect(createdVideo?.id).toBe('video-1');
    expect(port.lastCreateRequest?.ownerId).toBe('park-1');
    expect(facade.successKey()).toBe('admin.videos.createSuccess');
  });

  it('reports a creation error without throwing to the component', async () => {
    port.createResponse$ = throwError(() => new Error('api'));

    const createdVideo: VideoDto | null = await facade.createVideo({
      originalUrl: 'https://www.youtube.com/watch?v=abcdefghijk',
      ownerType: VideoOwnerType.PARK,
      ownerId: 'park-1',
      type: VideoType.ON_RIDE,
      title: null,
      description: null,
      creatorName: null,
      creatorUrl: null,
      thumbnailUrl: null,
      durationSeconds: null,
      publishedAtUtc: null,
      languageCodes: [],
      titles: [],
      descriptions: [],
      tagIds: [],
      isPublished: true,
    });

    expect(createdVideo).toBeNull();
    expect(facade.errorKey()).toBe('admin.videos.createError');
  });
});
