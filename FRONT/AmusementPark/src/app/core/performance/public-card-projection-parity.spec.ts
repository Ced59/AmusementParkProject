import { HomeFeaturedParkModel } from '@app/models/home/home-featured-park.model';
import { HomeLatestArticleModel } from '@app/models/home/home-latest-article.model';
import { Park } from '@app/models/parks/park';
import { AttractionManufacturer } from '@app/models/parks/attraction-manufacturer';
import { ManufacturersPageComponent } from '@features/public/manufacturers/pages/manufacturers-page.component';
import { mapHomeFeaturedParkToCardModel } from '@features/public/home/mappers/home-featured-park.mapper';
import { mapHomeLatestArticleToCardModel } from '@features/public/home/mappers/home-latest-article.mapper';
import { NaturalTextTruncatorService } from '@shared/services/text/natural-text-truncator.service';
import { CountryDisplayService } from '@shared/services/countries/country-display.service';
import { mapParkToCardModel } from '@shared/utils/mapping';

const languages = ['en', 'fr', 'de', 'nl', 'it', 'es', 'pl', 'pt'];
const prefix = 'a'.repeat(100);
const full = [{ languageCode: 'fr', value: `<p>${prefix}, ${'b'.repeat(100)}</p>` }, { languageCode: 'en', value: '<p>1 <<b>2> 3</p>' }];
const summaries = (ellipsis: string) => [{ languageCode: 'fr', value: prefix + ellipsis }, { languageCode: 'en', value: '1 2> 3' }];

describe('public card transport equivalence', () => {
  const truncator = new NaturalTextTruncatorService();
  const countries = { resolveLocalizedCountryName: () => 'France' } as unknown as CountryDisplayService;

  it.each(languages)('preserves catalog and home cards, fallback and literal text in %s', language => {
    const park: Park = { id: 'park', name: 'Parc', latitude: 48, longitude: 2, descriptions: full, currentLogoImageId: 'logo' };
    expect(mapParkToCardModel({ ...park, descriptions: [], shortDescriptions: summaries('...') }, language, countries, truncator))
      .toEqual(mapParkToCardModel(park, language, countries, truncator));
    const home: HomeFeaturedParkModel = { id: 'park', name: 'Parc', countryCode: 'FR', type: null, latitude: 48, longitude: 2, city: 'Paris', currentLogoImageId: 'logo', isManualFeatured: true, isSponsoredFeatured: true, countsByCategory: [{ category: 'Attraction', count: 7 }], descriptions: full };
    expect(mapHomeFeaturedParkToCardModel({ ...home, descriptions: [], shortDescriptions: summaries('…') }, language, truncator, 1, countries))
      .toEqual(mapHomeFeaturedParkToCardModel(home, language, truncator, 1, countries));
    const article: HomeLatestArticleModel = { eventId: 'event', entityType: 'Park', parkId: 'park', parkName: 'Parc', parkItemId: null, parkItemName: null, slug: 'story', titles: [{ languageCode: 'fr', value: 'Histoire' }], summaries: full, mainImageId: 'photo' };
    expect(mapHomeLatestArticleToCardModel({ ...article, summaries: [], shortSummaries: summaries('…') }, language, truncator, 0))
      .toEqual(mapHomeLatestArticleToCardModel(article, language, truncator, 0));
  });

  it.each(languages)('preserves the manufacturer preview and its exact rich-text fallback in %s', language => {
    const preview = (ManufacturersPageComponent.prototype as unknown as { biographyPreview: (source: AttractionManufacturer) => string | null }).biographyPreview;
    const context = { currentLang: () => language };
    const source: AttractionManufacturer = { name: 'Maker', biography: full };
    const compact = [{ languageCode: 'fr', value: `${prefix}, ${'b'.repeat(75)}...` }, { languageCode: 'en', value: '1 2> 3' }];
    expect(preview.call(context, { ...source, biography: [], biographyPreviews: compact })).toEqual(preview.call(context, source));
    const imageOnly: AttractionManufacturer = { name: 'Maker', biography: [{ languageCode: 'fr', value: '<img src="x">' }, { languageCode: 'en', value: 'English' }] };
    expect(preview.call(context, { ...imageOnly, biography: [], biographyPreviews: [{ languageCode: 'fr', value: null! }, { languageCode: 'en', value: 'English' }] })).toEqual(preview.call(context, imageOnly));
  });
});
