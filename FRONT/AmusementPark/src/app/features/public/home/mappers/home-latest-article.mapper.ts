import { HomeLatestArticleCardModel } from '@app/models/home/home-latest-article-card.model';
import { HomeLatestArticleModel } from '@app/models/home/home-latest-article.model';
import { NaturalTextTruncatorService } from '@shared/services/text/natural-text-truncator.service';
import { resolveLocalizedValue, stripHtml } from '@shared/utils/localization';
import {
  buildPublicParkHistoryArticleRouteCommands,
  buildPublicParkItemHistoryArticleRouteCommands
} from '@shared/utils/routing/public-detail-route.helpers';

type LatestArticleTone = HomeLatestArticleCardModel['tone'];

const TONES: readonly LatestArticleTone[] = ['sky', 'purple', 'primary'];
const ARTICLE_DESCRIPTION_MAX_LENGTH: number = 142;

export function mapHomeLatestArticleToCardModel(
  article: HomeLatestArticleModel,
  currentLanguage: string,
  truncator: NaturalTextTruncatorService,
  index: number
): HomeLatestArticleCardModel {
  const fallbackTitle: string = normalizeOptionalString(article.parkItemName)
    ?? normalizeOptionalString(article.parkName)
    ?? '';
  const localizedTitle: string = stripHtml(resolveLocalizedValue(article.titles, currentLanguage) ?? null) ?? '';
  const title: string = normalizeOptionalString(localizedTitle) ?? fallbackTitle;
  const plainDescription: string = stripHtml(resolveLocalizedValue(article.summaries, currentLanguage) ?? null) ?? '';

  return {
    id: normalizeOptionalString(article.eventId),
    title,
    description: truncator.truncate(plainDescription, { maxLength: ARTICLE_DESCRIPTION_MAX_LENGTH }),
    contextLabel: buildContextLabel(article),
    mainImageId: normalizeOptionalString(article.mainImageId),
    detailLink: buildDetailLink(article, title, currentLanguage),
    tone: TONES[index % TONES.length]
  };
}

function buildContextLabel(article: HomeLatestArticleModel): string | null {
  const parts: string[] = [article.parkItemName, article.parkName]
    .map((value: string | null) => normalizeOptionalString(value))
    .filter((value: string | null): value is string => value !== null);

  return parts.length > 0 ? parts.join(' • ') : null;
}

function buildDetailLink(article: HomeLatestArticleModel, title: string, currentLanguage: string): string[] | null {
  const parkId: string | null = normalizeOptionalString(article.parkId);
  const parkName: string = normalizeOptionalString(article.parkName) ?? parkId ?? 'park';
  const eventTitle: string = normalizeOptionalString(article.slug) ?? title;

  if (article.entityType === 'ParkItem') {
    const parkItemId: string | null = normalizeOptionalString(article.parkItemId);
    const parkItemName: string = normalizeOptionalString(article.parkItemName) ?? parkItemId ?? 'item';

    return buildPublicParkItemHistoryArticleRouteCommands({
      language: currentLanguage,
      parkId,
      parkName,
      itemId: parkItemId,
      itemName: parkItemName,
      eventId: article.eventId,
      eventTitle
    });
  }

  return buildPublicParkHistoryArticleRouteCommands({
    language: currentLanguage,
    parkId,
    parkName,
    eventId: article.eventId,
    eventTitle
  });
}

function normalizeOptionalString(value: string | null | undefined): string | null {
  const normalizedValue: string = value?.trim() ?? '';
  return normalizedValue.length > 0 ? normalizedValue : null;
}
