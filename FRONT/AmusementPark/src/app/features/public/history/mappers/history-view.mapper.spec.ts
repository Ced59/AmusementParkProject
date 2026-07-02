import { HistoryArticle, HistoryEvent, HistoryTimeline } from '@app/models/history/history.models';
import { Park } from '@app/models/parks/park';
import { mapHistoryArticleToViewModel, mapHistoryTimelineToViewModel } from './history-view.mapper';

describe('history-view.mapper', () => {
  it('uses a readable timeline title fallback instead of the technical key', () => {
    const timeline: HistoryTimeline = {
      entityType: 'Park',
      park: { id: 'park-1', name: 'Mirapolis' } as Park,
      parkItem: null,
      hasParkItemTimelineEvents: false,
      includedParkItems: [],
      events: [
        {
          event: createHistoryEvent(),
          contextPark: null,
          parkItem: null,
          mainImage: null
        }
      ]
    };

    const viewModel = mapHistoryTimelineToViewModel(timeline, 'en');

    expect(viewModel.events[0].title).toBe('Opening - Mirapolis');
    expect(viewModel.events[0].title).not.toBe('mirapolis-opening-1987');
  });

  it('shows park item controls from the backend availability flag and marks first events by year', () => {
    const timeline: HistoryTimeline = {
      entityType: 'Park',
      park: { id: 'park-1', name: 'Mirapolis' } as Park,
      parkItem: null,
      hasParkItemTimelineEvents: true,
      includedParkItems: [],
      events: [
        {
          event: createHistoryEvent({ id: 'event-1987-a', key: 'opening', year: 1987 }),
          contextPark: null,
          parkItem: null,
          mainImage: null
        },
        {
          event: createHistoryEvent({ id: 'event-1987-b', key: 'operator', year: 1987, month: 7, day: null }),
          contextPark: null,
          parkItem: null,
          mainImage: null
        },
        {
          event: createHistoryEvent({ id: 'event-1988', key: 'item-opening', year: 1988, month: null, day: null }),
          contextPark: null,
          parkItem: null,
          mainImage: null
        }
      ]
    };

    const viewModel = mapHistoryTimelineToViewModel(timeline, 'en');

    expect(viewModel.showParkItemControls).toBeTrue();
    expect(viewModel.events.map(event => event.isFirstInYear)).toEqual([true, false, true]);
  });

  it('filters empty article blocks and uses a readable article title fallback', () => {
    const article: HistoryArticle = {
      event: createHistoryEvent({
        article: {
          slug: 'opening-article',
          titles: [],
          subtitles: [],
          summaries: [],
          mainImageId: null,
          isPublished: true,
          sources: [],
          blocks: [
            {
              id: 'empty-block',
              type: 'Paragraph',
              sortOrder: 1,
              texts: [],
              imageId: null,
              imageIds: [],
              captions: []
            },
            {
              id: 'content-block',
              type: 'Paragraph',
              sortOrder: 2,
              texts: [{ languageCode: 'en', value: 'Mirapolis opens to visitors.' }],
              imageId: null,
              imageIds: [],
              captions: []
            }
          ]
        }
      }),
      park: { id: 'park-1', name: 'Mirapolis' } as Park,
      parkItem: null,
      contextPark: null,
      mainImage: null
    };

    const viewModel = mapHistoryArticleToViewModel(article, 'en');

    expect(viewModel).not.toBeNull();
    expect(viewModel!.title).toBe('Opening - Mirapolis');
    expect(viewModel!.blocks.map(block => block.id)).toEqual(['content-block']);
  });

  it('builds a canonical article path from resolved owner data', () => {
    const article: HistoryArticle = {
      event: createHistoryEvent({
        article: {
          slug: 'opening-article',
          titles: [{ languageCode: 'en', value: 'Opening day' }],
          subtitles: [],
          summaries: [],
          mainImageId: null,
          isPublished: true,
          sources: [],
          blocks: []
        }
      }),
      park: { id: 'park-1', name: 'Mirapolis' } as Park,
      parkItem: null,
      contextPark: null,
      mainImage: null
    };

    const viewModel = mapHistoryArticleToViewModel(article, 'en');

    expect(viewModel).not.toBeNull();
    expect(viewModel!.canonicalPath).toBe('/en/park/park-1/mirapolis/history/event-1/opening-article');
  });

  it('keeps gallery image ids on article blocks', () => {
    const article: HistoryArticle = {
      event: createHistoryEvent({
        article: {
          slug: 'gallery-article',
          titles: [{ languageCode: 'en', value: 'Gallery' }],
          subtitles: [],
          summaries: [],
          mainImageId: null,
          isPublished: true,
          sources: [],
          blocks: [
            {
              id: 'gallery-block',
              type: 'Gallery',
              sortOrder: 1,
              texts: [],
              imageId: null,
              imageIds: ['image-1', 'image-2'],
              captions: [{ languageCode: 'en', value: 'Opening photos' }]
            }
          ]
        }
      }),
      park: { id: 'park-1', name: 'Mirapolis' } as Park,
      parkItem: null,
      contextPark: null,
      mainImage: null
    };

    const viewModel = mapHistoryArticleToViewModel(article, 'en');

    expect(viewModel).not.toBeNull();
    expect(viewModel!.blocks[0].imageIds).toEqual(['image-1', 'image-2']);
    expect(viewModel!.blocks[0].caption).toBe('Opening photos');
  });
});

function createHistoryEvent(overrides: Partial<HistoryEvent> = {}): HistoryEvent {
  return {
    id: 'event-1',
    key: 'mirapolis-opening-1987',
    entityType: 'Park',
    ownerId: 'park-1',
    parkId: 'park-1',
    parkItemId: null,
    contextParkId: null,
    year: 1987,
    month: 5,
    day: 20,
    datePrecision: 'Day',
    eventType: 'Opening',
    isMajor: true,
    isVisible: true,
    slug: null,
    titles: [],
    summaries: [],
    mainImageId: null,
    previousName: null,
    newName: null,
    previousLogoImageId: null,
    newLogoImageId: null,
    previousOperatorId: null,
    newOperatorId: null,
    locationLabel: null,
    relatedParkIds: [],
    relatedParkItemIds: [],
    sources: [],
    article: null,
    createdAtUtc: '2026-06-30T00:00:00Z',
    updatedAtUtc: '2026-06-30T00:00:00Z',
    ...overrides
  };
}
