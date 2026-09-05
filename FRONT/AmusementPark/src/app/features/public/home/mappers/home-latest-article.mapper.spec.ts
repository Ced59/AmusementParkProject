import { HomeLatestArticleModel } from '@app/models/home/home-latest-article.model';
import { NaturalTextTruncatorService } from '@shared/services/text/natural-text-truncator.service';
import { mapHomeLatestArticleToCardModel } from './home-latest-article.mapper';

describe('mapHomeLatestArticleToCardModel', () => {
  const truncator: NaturalTextTruncatorService = new NaturalTextTruncatorService();

  it('localizes and naturally truncates a park item article card', () => {
    const article: HomeLatestArticleModel = createArticle({
      entityType: 'ParkItem',
      summaries: [{ languageCode: 'fr', value: `<p>${'Une histoire détaillée avec du contexte. '.repeat(8)}</p>` }]
    });

    const card = mapHomeLatestArticleToCardModel(article, 'fr', truncator, 0);

    expect(card.title).toBe('Une grande nouveauté');
    expect(card.description?.length).toBeLessThanOrEqual(142);
    expect(card.description?.endsWith('…')).toBe(true);
    expect(card.contextLabel).toBe('Attraction test • Parc test');
    expect(card.detailLink?.join('/')).toContain('/fr/park/park-1/parc-test/item/item-1/attraction-test/history/event-1/article-test');
  });

  it('builds a park article link and falls back to the park name for an empty title', () => {
    const article: HomeLatestArticleModel = createArticle({
      entityType: 'Park',
      parkItemId: null,
      parkItemName: null,
      titles: []
    });

    const card = mapHomeLatestArticleToCardModel(article, 'en', truncator, 1);

    expect(card.title).toBe('Parc test');
    expect(card.detailLink?.join('/')).toContain('/en/park/park-1/parc-test/history/event-1/article-test');
    expect(card.tone).toBe('purple');
  });
});

function createArticle(overrides: Partial<HomeLatestArticleModel>): HomeLatestArticleModel {
  return {
    eventId: 'event-1',
    entityType: 'Park',
    parkId: 'park-1',
    parkName: 'Parc test',
    parkItemId: 'item-1',
    parkItemName: 'Attraction test',
    slug: 'article-test',
    titles: [{ languageCode: 'fr', value: 'Une grande nouveauté' }],
    summaries: [{ languageCode: 'fr', value: 'Résumé' }],
    mainImageId: 'image-1',
    ...overrides
  };
}
