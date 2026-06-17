import { HttpTestingController } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';

import { VideoOwnerType } from '@app/models/videos/video-owner-type';
import { VideoType } from '@app/models/videos/video-type';
import { environment } from '../../../environments/environment';
import { provideCommonTestDependencies } from '@app/testing/common-test-providers';
import { VideosApiService } from './videos-api.service';

describe('VideosApiService', () => {
  let service: VideosApiService;
  let httpTestingController: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: provideCommonTestDependencies() });
    service = TestBed.inject(VideosApiService);
    httpTestingController = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpTestingController.verify();
  });

  it('gets public videos with owner and filter query params', () => {
    service.getVideosPage({
      page: 2,
      size: 12,
      ownerType: VideoOwnerType.PARK,
      ownerId: 'park-1',
      type: VideoType.ON_RIDE,
      tagId: 'official',
      creatorName: 'creator',
      sortBy: 'published',
      sortDirection: 'desc'
    }).subscribe((page) => {
      expect(page.items).toEqual([{ id: 'video-1' } as never]);
    });

    const request = httpTestingController.expectOne((candidate) => candidate.url === `${environment.apiBaseUrl}videos`);
    expect(request.request.method).toBe('GET');
    expect(request.request.params.get('page')).toBe('2');
    expect(request.request.params.get('size')).toBe('12');
    expect(request.request.params.get('ownerType')).toBe('PARK');
    expect(request.request.params.get('ownerId')).toBe('park-1');
    expect(request.request.params.get('type')).toBe('ON_RIDE');
    expect(request.request.params.get('tagId')).toBe('official');
    expect(request.request.params.get('creatorName')).toBe('creator');
    expect(request.request.params.get('sortBy')).toBe('published');
    expect(request.request.params.get('sortDirection')).toBe('desc');
    request.flush({ data: [{ id: 'video-1' }] });
  });

  it('gets a video detail and public tags', () => {
    service.getVideoById('video-1').subscribe((video) => {
      expect(video.id).toBe('video-1');
    });

    const videoRequest = httpTestingController.expectOne(`${environment.apiBaseUrl}videos/video-1`);
    expect(videoRequest.request.method).toBe('GET');
    videoRequest.flush({ id: 'video-1' });

    service.getVideoTags().subscribe((tags) => {
      expect(tags).toEqual([{ id: 'tag-1', slug: 'official' } as never]);
    });

    const tagsRequest = httpTestingController.expectOne((candidate) => candidate.url === `${environment.apiBaseUrl}videos/tags`);
    expect(tagsRequest.request.method).toBe('GET');
    expect(tagsRequest.request.params.get('page')).toBe('1');
    expect(tagsRequest.request.params.get('size')).toBe('100');
    tagsRequest.flush({ data: [{ id: 'tag-1', slug: 'official' }] });
  });
});
