import { VideoDto } from '@app/models/videos/video-dto';
import { VideoHostingProvider } from '@app/models/videos/video-hosting-provider';
import { VideoOwnerType } from '@app/models/videos/video-owner-type';
import { VideoTagDto } from '@app/models/videos/video-tag-dto';
import { VideoType } from '@app/models/videos/video-type';
import { buildPublicVideoCards, buildPublicVideoNavigation, buildVideoTagOptions } from './public-video.mapper';

describe('public video mapper', () => {
  it('maps localized video labels, tags and thumbnail image ids', () => {
    const video: VideoDto = createVideo({
      title: 'Fallback',
      titles: [{ languageCode: 'fr', value: 'Onride FR' }],
      descriptions: [{ languageCode: 'fr', value: '<p>Description FR</p>' }],
      thumbnailImageId: 'thumb-1',
      thumbnailUrl: 'https://img.example/thumb.jpg',
      durationSeconds: 125,
      tagIds: ['official']
    });
    const tags: VideoTagDto[] = [{
      id: 'official',
      slug: 'official-amusementparks-fun',
      labels: [{ languageCode: 'fr', value: 'Officiel' }],
      descriptions: [],
      isActive: true,
      createdAt: '',
      updatedAt: ''
    }];

    const cards = buildPublicVideoCards([video], tags, 'fr', () => ['/', 'fr', 'video']);

    expect(cards[0].title).toBe('Onride FR');
    expect(cards[0].description).toBe('Description FR');
    expect(cards[0].thumbnailPathOrUrl).toBe('thumb-1');
    expect(cards[0].durationLabel).toBe('2:05');
    expect(cards[0].tags[0].label).toBe('Officiel');
  });

  it('builds previous and next navigation around the current video', () => {
    const videos: VideoDto[] = [
      createVideo({ id: 'v1', title: 'First' }),
      createVideo({ id: 'v2', title: 'Second' }),
      createVideo({ id: 'v3', title: 'Third' })
    ];

    const navigation = buildPublicVideoNavigation(videos, 'v2', (video: VideoDto) => ['/', video.id]);

    expect(navigation.previous?.title).toBe('First');
    expect(navigation.previous?.routerLink).toEqual(['/', 'v1']);
    expect(navigation.next?.title).toBe('Third');
    expect(navigation.next?.routerLink).toEqual(['/', 'v3']);
  });

  it('keeps only active tags in filter options', () => {
    const tags: VideoTagDto[] = [
      { id: 'active', slug: 'active', labels: [{ languageCode: 'en', value: 'Active' }], descriptions: [], isActive: true, createdAt: '', updatedAt: '' },
      { id: 'hidden', slug: 'hidden', labels: [], descriptions: [], isActive: false, createdAt: '', updatedAt: '' }
    ];

    expect(buildVideoTagOptions(tags, 'en')).toEqual([{ value: 'active', label: 'Active' }]);
  });
});

function createVideo(overrides: Partial<VideoDto> = {}): VideoDto {
  return {
    id: 'video-1',
    hostingProvider: VideoHostingProvider.YOUTUBE,
    ownerType: VideoOwnerType.PARK,
    ownerId: 'park-1',
    type: VideoType.ON_RIDE,
    originalUrl: 'https://www.youtube.com/watch?v=abc',
    canonicalUrl: 'https://www.youtube.com/watch?v=abc',
    embedUrl: 'https://www.youtube.com/embed/abc',
    externalId: 'abc',
    title: 'Video',
    description: null,
    creatorName: 'Creator',
    creatorUrl: null,
    thumbnailUrl: null,
    thumbnailImageId: null,
    durationSeconds: null,
    publishedAtUtc: null,
    languageCodes: [],
    titles: [],
    descriptions: [],
    tagIds: [],
    externalMetadata: {},
    isPublished: true,
    createdAt: '',
    updatedAt: '',
    ...overrides
  };
}
