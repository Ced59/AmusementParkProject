import { buildEntitySlug } from '@shared/utils/display/park-presentation.helpers';
import { resolveLocalizedText } from '@shared/utils/localization/localized-text.helpers';
import {
  buildPublicParkHistoryArticleRouteCommands,
  buildPublicParkHistoryRouteCommands,
  buildPublicParkItemHistoryArticleRouteCommands,
  buildPublicParkItemHistoryRouteCommands,
  buildPublicRoutePath
} from '@shared/utils/routing/public-detail-route.helpers';
import { HistoryArticle, HistoryTimeline, HistoryTimelineEvent } from '@app/models/history/history.models';
import { Park } from '@app/models/parks/park';
import { ParkItem } from '@app/models/parks/park-item';
import {
  HistoryArticleBlockViewModel,
  HistoryArticlePageViewModel,
  HistoryTimelineEventViewModel,
  HistoryTimelinePageViewModel
} from '../models/history-view.model';
import { resolveHistoryEventTypeLabel } from '../utils/history-event-labels';

interface HistoryTimelineMapperCopy {
  itemName: string;
  parkName: string;
  title: (ownerName: string) => string;
  subtitle: (eventCount: number, targetName: string, ownerName: string) => string;
}

const HISTORY_TIMELINE_MAPPER_COPY: Record<string, HistoryTimelineMapperCopy> = {
  fr: {
    itemName: 'cet élément',
    parkName: 'ce parc',
    title: (ownerName: string): string => `Histoire de ${ownerName}`,
    subtitle: (eventCount: number, targetName: string): string => `${eventCount} jalon${eventCount > 1 ? 's' : ''} pour suivre l’évolution de ${targetName}.`
  },
  en: {
    itemName: 'this item',
    parkName: 'this park',
    title: (ownerName: string): string => `${ownerName} history`,
    subtitle: (eventCount: number, _targetName: string, ownerName: string): string => `${eventCount} milestone${eventCount === 1 ? '' : 's'} tracing the story of ${ownerName}.`
  },
  de: {
    itemName: 'dieses Element',
    parkName: 'dieser Park',
    title: (ownerName: string): string => `Geschichte von ${ownerName}`,
    subtitle: (eventCount: number, targetName: string): string => `${eventCount} Meilenstein${eventCount === 1 ? '' : 'e'}, um die Entwicklung von ${targetName} nachzuverfolgen.`
  },
  nl: {
    itemName: 'dit item',
    parkName: 'dit park',
    title: (ownerName: string): string => `Geschiedenis van ${ownerName}`,
    subtitle: (eventCount: number, targetName: string): string => `${eventCount} mijlpaal${eventCount === 1 ? '' : 'en'} om de evolutie van ${targetName} te volgen.`
  },
  it: {
    itemName: 'questo elemento',
    parkName: 'questo parco',
    title: (ownerName: string): string => `Storia di ${ownerName}`,
    subtitle: (eventCount: number, targetName: string): string => `${eventCount} tappa${eventCount === 1 ? '' : 'e'} per seguire l’evoluzione di ${targetName}.`
  },
  es: {
    itemName: 'este elemento',
    parkName: 'este parque',
    title: (ownerName: string): string => `Historia de ${ownerName}`,
    subtitle: (eventCount: number, targetName: string): string => `${eventCount} hito${eventCount === 1 ? '' : 's'} para seguir la evolución de ${targetName}.`
  },
  pl: {
    itemName: 'ten element',
    parkName: 'ten park',
    title: (ownerName: string): string => `Historia ${ownerName}`,
    subtitle: (eventCount: number, targetName: string): string => `${eventCount} punktów na osi czasu, żeby śledzić rozwój: ${targetName}.`
  },
  pt: {
    itemName: 'este item',
    parkName: 'este parque',
    title: (ownerName: string): string => `História de ${ownerName}`,
    subtitle: (eventCount: number, targetName: string): string => `${eventCount} marco${eventCount === 1 ? '' : 's'} para acompanhar a evolução de ${targetName}.`
  }
};

export function mapHistoryTimelineToViewModel(timeline: HistoryTimeline, language: string): HistoryTimelinePageViewModel {
  const ownerName: string = timeline.parkItem?.name ?? timeline.park?.name ?? resolveFallbackOwnerName(timeline.entityType, language);
  const events: HistoryTimelineEventViewModel[] = timeline.events
    .map((entry: HistoryTimelineEvent): HistoryTimelineEventViewModel => mapTimelineEventToViewModel(entry, timeline, language))
    .sort((left: HistoryTimelineEventViewModel, right: HistoryTimelineEventViewModel): number => {
      if (left.year !== right.year) {
        return left.year - right.year;
      }

      if ((left.month ?? 0) !== (right.month ?? 0)) {
        return (left.month ?? 0) - (right.month ?? 0);
      }

      return (left.day ?? 0) - (right.day ?? 0);
    });
  const years: number[] = events.map((event: HistoryTimelineEventViewModel) => event.year);
  const yearStart: number = years.length > 0 ? Math.min(...years) : new Date().getFullYear();
  const yearEnd: number = years.length > 0 ? Math.max(...years) : yearStart;
  const range: number = Math.max(1, yearEnd - yearStart);
  const seenYears = new Set<number>();

  for (const event of events) {
    event.positionPercent = ((event.year - yearStart) / range) * 100;
    event.isFirstInYear = !seenYears.has(event.year);
    seenYears.add(event.year);
  }

  const hasParkItemTimelineEvents: boolean = timeline.hasParkItemTimelineEvents === true || (timeline.includedParkItems?.length ?? 0) > 0;

  return {
    entityType: timeline.entityType,
    title: resolveTimelineTitle(ownerName, timeline.entityType, language),
    subtitle: resolveTimelineSubtitle(ownerName, timeline.entityType, events.length, language),
    ownerName,
    park: timeline.park ?? null,
    parkItem: timeline.parkItem ?? null,
    includedParkItems: timeline.includedParkItems ?? [],
    showParkItemControls: timeline.entityType === 'Park' && hasParkItemTimelineEvents,
    events,
    yearStart,
    yearEnd
  };
}

export function mapHistoryArticleToViewModel(article: HistoryArticle, language: string): HistoryArticlePageViewModel | null {
  const content = article.event.article;

  if (!content) {
    return null;
  }

  const ownerName: string = article.parkItem?.name ?? article.park?.name ?? resolveFallbackOwnerName(article.event.entityType, language);
  const eventTypeLabel: string = resolveHistoryEventTypeLabel(article.event.eventType, language);
  const fallbackTitle: string = resolveHistoryEventTitle(article.event, language, eventTypeLabel, ownerName);
  const title: string = resolveLocalizedText(content.titles, language, fallbackTitle);
  const eventSummary: string = resolveLocalizedText(article.event.summaries, language, '');
  const summary: string = resolveLocalizedText(content.summaries, language, eventSummary);
  const subtitle: string = resolveLocalizedText(content.subtitles, language, '');

  return {
    title,
    subtitle,
    summary,
    dateLabel: formatHistoryDate(article.event.year, article.event.month ?? null, article.event.day ?? null, article.event.datePrecision, language),
    eventTypeLabel,
    ownerName,
    park: article.park ?? null,
    parkItem: article.parkItem ?? null,
    contextPark: article.contextPark ?? null,
    mainImageId: content.mainImageId ?? article.event.mainImageId ?? null,
    mainImage: article.mainImage ?? null,
    blocks: (content.blocks ?? [])
      .slice()
      .sort((left, right): number => left.sortOrder - right.sortOrder)
      .map((block, index): HistoryArticleBlockViewModel => ({
        id: block.id ?? `block-${index}`,
        type: block.type,
        sortOrder: block.sortOrder,
        headingLevel: normalizeHeadingLevel(block.headingLevel),
        text: resolveLocalizedText(block.texts, language, ''),
        imageId: block.imageId ?? null,
        imageIds: block.imageIds ?? [],
        caption: resolveLocalizedText(block.captions, language, ''),
        source: block
      }))
      .filter(isDisplayableArticleBlock),
    sources: [...(content.sources ?? []), ...(article.event.sources ?? [])],
    timelineLink: buildArticleTimelineLink(article, language),
    canonicalPath: buildArticleCanonicalPath(article, title, language),
    event: article.event,
    article: content
  };
}

function mapTimelineEventToViewModel(entry: HistoryTimelineEvent, timeline: HistoryTimeline, language: string): HistoryTimelineEventViewModel {
  const event = entry.event;
  const park: Park | null = timeline.park ?? entry.contextPark ?? null;
  const parkItem: ParkItem | null = entry.parkItem ?? timeline.parkItem ?? null;
  const ownerName: string = event.entityType === 'ParkItem'
    ? parkItem?.name ?? resolveFallbackOwnerName('ParkItem', language)
    : park?.name ?? resolveFallbackOwnerName('Park', language);
  const eventTypeLabel: string = resolveHistoryEventTypeLabel(event.eventType, language);
  const eventTitle: string = resolveHistoryEventTitle(event, language, eventTypeLabel, ownerName);

  return {
    id: event.id ?? event.key,
    key: event.key,
    title: eventTitle,
    summary: resolveLocalizedText(event.summaries, language, ''),
    dateLabel: formatHistoryDate(event.year, event.month ?? null, event.day ?? null, event.datePrecision, language),
    year: event.year,
    month: event.month ?? null,
    day: event.day ?? null,
    eventType: event.eventType,
    eventTypeLabel,
    entityType: event.entityType,
    isMajor: event.isMajor,
    ownerName,
    contextParkName: entry.contextPark?.name ?? null,
    parkItemName: parkItem?.name ?? null,
    mainImageId: event.article?.mainImageId ?? event.mainImageId ?? null,
    mainImage: entry.mainImage ?? null,
    articleLink: buildTimelineArticleLink(event.id ?? null, eventTitle, timeline, entry, language),
    sourceCount: event.sources?.length ?? 0,
    positionPercent: 0,
    isFirstInYear: false
  };
}

function resolveHistoryEventTitle(event: HistoryTimelineEvent['event'], language: string, eventTypeLabel: string, ownerName: string): string {
  const fallbackTitle: string = ownerName.trim().length > 0
    ? `${eventTypeLabel} - ${ownerName}`
    : eventTypeLabel;
  return resolveLocalizedText(event.titles, language, fallbackTitle);
}

function isDisplayableArticleBlock(block: HistoryArticleBlockViewModel): boolean {
  const hasText: boolean = block.text.trim().length > 0;
  const hasImage: boolean = !!block.imageId || block.imageIds.length > 0;
  const hasCaption: boolean = block.caption.trim().length > 0;

  return hasText || hasImage || hasCaption;
}

function buildTimelineArticleLink(
  eventId: string | null,
  eventTitle: string,
  timeline: HistoryTimeline,
  entry: HistoryTimelineEvent,
  language: string
): string[] | null {
  if (!eventId || !entry.event.isMajor || !entry.event.article) {
    return null;
  }

  if (entry.event.entityType === 'ParkItem') {
    const park: Park | null = entry.contextPark ?? timeline.park ?? null;
    const item: ParkItem | null = entry.parkItem ?? timeline.parkItem ?? null;

    return buildPublicParkItemHistoryArticleRouteCommands({
      language,
      parkId: park?.id ?? entry.event.contextParkId ?? entry.event.parkId,
      parkName: park?.name ?? entry.event.contextParkId ?? 'park',
      itemId: item?.id ?? entry.event.parkItemId,
      itemName: item?.name ?? 'item',
      eventId,
      eventTitle: entry.event.article.slug ?? eventTitle
    });
  }

  const park: Park | null = timeline.park ?? entry.contextPark ?? null;
  return buildPublicParkHistoryArticleRouteCommands({
    language,
    parkId: park?.id ?? entry.event.parkId,
    parkName: park?.name ?? 'park',
    eventId,
    eventTitle: entry.event.article.slug ?? eventTitle
  });
}

function buildArticleTimelineLink(article: HistoryArticle, language: string): string[] | null {
  if (article.event.entityType === 'ParkItem') {
    return buildPublicParkItemHistoryRouteCommands({
      language,
      parkId: article.contextPark?.id ?? article.park?.id ?? article.event.contextParkId ?? article.event.parkId,
      parkName: article.contextPark?.name ?? article.park?.name ?? 'park',
      itemId: article.parkItem?.id ?? article.event.parkItemId,
      itemName: article.parkItem?.name ?? 'item'
    });
  }

  return buildPublicParkHistoryRouteCommands({
    language,
    parkId: article.park?.id ?? article.event.parkId,
    parkName: article.park?.name ?? 'park'
  });
}

function buildArticleCanonicalPath(article: HistoryArticle, eventTitle: string, language: string): string | null {
  return buildPublicRoutePath(buildArticleCanonicalLink(article, eventTitle, language));
}

function buildArticleCanonicalLink(article: HistoryArticle, eventTitle: string, language: string): string[] | null {
  const resolvedEventTitle: string = article.event.article?.slug ?? eventTitle;
  const eventId: string | null = article.event.id ?? null;

  if (article.event.entityType === 'ParkItem') {
    return buildPublicParkItemHistoryArticleRouteCommands({
      language,
      parkId: article.contextPark?.id ?? article.park?.id ?? article.event.contextParkId ?? article.event.parkId,
      parkName: article.contextPark?.name ?? article.park?.name ?? 'park',
      itemId: article.parkItem?.id ?? article.event.parkItemId,
      itemName: article.parkItem?.name ?? 'item',
      eventId,
      eventTitle: resolvedEventTitle
    });
  }

  return buildPublicParkHistoryArticleRouteCommands({
    language,
    parkId: article.park?.id ?? article.contextPark?.id ?? article.event.parkId ?? article.event.contextParkId,
    parkName: article.park?.name ?? article.contextPark?.name ?? 'park',
    eventId,
    eventTitle: resolvedEventTitle
  });
}

function formatHistoryDate(year: number, month: number | null, day: number | null, precision: string | null | undefined, language: string): string {
  if (precision === 'Day' && month && day) {
    return new Intl.DateTimeFormat(language, { year: 'numeric', month: 'long', day: 'numeric' }).format(new Date(Date.UTC(year, month - 1, day)));
  }

  if ((precision === 'Month' || precision === 'Day') && month) {
    return new Intl.DateTimeFormat(language, { year: 'numeric', month: 'long' }).format(new Date(Date.UTC(year, month - 1, 1)));
  }

  return String(year);
}

function resolveTimelineTitle(ownerName: string, entityType: string, language: string): string {
  return resolveMapperCopy(language).title(ownerName);
}

function resolveTimelineSubtitle(ownerName: string, entityType: string, eventCount: number, language: string): string {
  const copy: HistoryTimelineMapperCopy = resolveMapperCopy(language);
  const target: string = entityType === 'ParkItem' ? copy.itemName : copy.parkName;
  return copy.subtitle(eventCount, target, ownerName);
}

function resolveFallbackOwnerName(entityType: string, language: string): string {
  const copy: HistoryTimelineMapperCopy = resolveMapperCopy(language);
  return entityType === 'ParkItem' ? copy.itemName : copy.parkName;
}

function resolveMapperCopy(language: string): HistoryTimelineMapperCopy {
  return HISTORY_TIMELINE_MAPPER_COPY[language] ?? HISTORY_TIMELINE_MAPPER_COPY['en'];
}

function normalizeHeadingLevel(value: number | null | undefined): number {
  if (value === 3 || value === 4) {
    return value;
  }

  return 2;
}

export function buildHistoryEventSlug(title: string): string {
  return buildEntitySlug(title, 'history');
}
