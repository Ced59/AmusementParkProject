import { HttpTestingController } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';

import { VideoOwnerType } from '@app/models/videos/video-owner-type';
import { VideoType } from '@app/models/videos/video-type';
import { VideoWriteRequest } from '@app/models/videos/video-write-request';
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
      languageCode: 'fr',
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
    expect(request.request.params.get('languageCode')).toBe('fr');
    expect(request.request.params.get('sortBy')).toBe('published');
    expect(request.request.params.get('sortDirection')).toBe('desc');
    request.flush({ data: [{ id: 'video-1' }] });
  });

  it('gets a video detail and public tags', () => {
    service.getVideoById('video-1', {}, 'fr').subscribe((video) => {
      expect(video.id).toBe('video-1');
    });

    const videoRequest = httpTestingController.expectOne((candidate) => candidate.url === `${environment.apiBaseUrl}videos/video-1`);
    expect(videoRequest.request.method).toBe('GET');
    expect(videoRequest.request.params.get('languageCode')).toBe('fr');
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

  it('resolves metadata and writes videos through admin endpoints', () => {
    const requestBody: VideoWriteRequest = {
      originalUrl: 'https://www.youtube.com/watch?v=abcdefghijk',
      ownerType: VideoOwnerType.PARK,
      ownerId: 'park-1',
      type: VideoType.ON_RIDE,
      title: 'Ride video',
      description: null,
      creatorName: 'Creator',
      creatorUrl: null,
      thumbnailUrl: null,
      durationSeconds: 120,
      publishedAtUtc: null,
      languageCodes: ['fr'],
      titles: [],
      descriptions: [],
      tagIds: ['tag-1'],
      isPublished: true
    };

    service.resolveVideoMetadata('https://www.youtube.com/watch?v=abcdefghijk').subscribe((metadata) => {
      expect(metadata.externalId).toBe('abcdefghijk');
    });

    const metadataRequest = httpTestingController.expectOne((candidate) => candidate.url === `${environment.apiBaseUrl}videos/resolve-metadata`);
    expect(metadataRequest.request.method).toBe('GET');
    expect(metadataRequest.request.params.get('url')).toBe('https://www.youtube.com/watch?v=abcdefghijk');
    metadataRequest.flush({ externalId: 'abcdefghijk' });

    service.createVideo(requestBody).subscribe((video) => {
      expect(video.id).toBe('video-1');
    });

    const createRequest = httpTestingController.expectOne(`${environment.apiBaseUrl}videos`);
    expect(createRequest.request.method).toBe('POST');
    expect(createRequest.request.body).toEqual(requestBody);
    createRequest.flush({ id: 'video-1' });

    service.updateVideo('video-1', requestBody).subscribe((video) => {
      expect(video.id).toBe('video-1');
    });

    const updateRequest = httpTestingController.expectOne(`${environment.apiBaseUrl}videos/video-1`);
    expect(updateRequest.request.method).toBe('PUT');
    expect(updateRequest.request.body).toEqual(requestBody);
    updateRequest.flush({ id: 'video-1' });

    service.deleteVideo('video-1').subscribe((deleted) => {
      expect(deleted).toBeTrue();
    });

    const deleteRequest = httpTestingController.expectOne(`${environment.apiBaseUrl}videos/video-1`);
    expect(deleteRequest.request.method).toBe('DELETE');
    deleteRequest.flush(true);
  });

  it('creates and updates video tags through admin endpoints', () => {
    service.createVideoTag({
      slug: 'official',
      labels: [{ languageCode: 'fr', value: 'official' }],
      descriptions: []
    }).subscribe((tag) => {
      expect(tag.slug).toBe('official');
    });

    const createRequest = httpTestingController.expectOne(`${environment.apiBaseUrl}videos/tags`);
    expect(createRequest.request.method).toBe('POST');
    createRequest.flush({ id: 'tag-1', slug: 'official' });

    service.updateVideoTag('tag-1', {
      slug: 'official',
      labels: [{ languageCode: 'fr', value: 'official' }],
      descriptions: [],
      isActive: true
    }).subscribe((tag) => {
      expect(tag.id).toBe('tag-1');
    });

    const updateRequest = httpTestingController.expectOne(`${environment.apiBaseUrl}videos/tags/tag-1`);
    expect(updateRequest.request.method).toBe('PUT');
    updateRequest.flush({ id: 'tag-1', slug: 'official' });
  });
});
