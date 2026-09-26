import {
  PublicHistoricalTimelineEntry,
  PublicParkHistoricalTimeline
} from '@app/models/history/public-park-history.models';
import { resolveLocalizedText } from '@shared/utils/localization/localized-text.helpers';
import {
  buildPublicParkHistoryArticleRouteCommands,
  buildPublicParkItemHistoryArticleRouteCommands
} from '@shared/utils/routing/public-detail-route.helpers';

export function buildCanonicalHistoryNarrativeLink(
  entry: PublicHistoricalTimelineEntry,
  timeline: PublicParkHistoricalTimeline,
  language: string
): string[] | null {
  if (!entry.narrative) {
    return null;
  }

  const eventTitle: string = entry.narrative.slug
    ?? resolveLocalizedText(entry.narrative.titles, language, entry.subjectLabel);
  if (entry.subjectType === 'ParkItem') {
    return buildPublicParkItemHistoryArticleRouteCommands({
      language,
      parkId: timeline.parkId,
      parkName: timeline.parkName,
      itemId: entry.subjectId,
      itemName: entry.currentSubjectName ?? entry.subjectLabel,
      eventId: entry.narrative.eventId,
      eventTitle
    });
  }

  if (entry.subjectType !== 'Park') {
    return null;
  }

  return buildPublicParkHistoryArticleRouteCommands({
    language,
    parkId: timeline.parkId,
    parkName: timeline.parkName,
    eventId: entry.narrative.eventId,
    eventTitle
  });
}
