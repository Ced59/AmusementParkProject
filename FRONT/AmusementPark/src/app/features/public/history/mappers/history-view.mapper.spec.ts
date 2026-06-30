import { HistoryArticle, HistoryEvent, HistoryTimeline } from '@app/models/history/history.models';
import { Park } from '@app/models/parks/park';
import { mapHistoryArticleToViewModel, mapHistoryTimelineToViewModel } from './history-view.mapper';

describe('history-view.mapper', () => {
  it('uses a readable timeline title fallback instead of the technical key', () => {
    const timeline: HistoryTimeline = {
      entityType: 'Park',
      park: { id: 'park-1', name: 'Mirapolis' } as Park,
      parkItem: null,
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
