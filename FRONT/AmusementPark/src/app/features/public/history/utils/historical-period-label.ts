import { PublicHistoricalDate, PublicHistoricalPeriod } from '@app/models/history/public-park-history.models';

export type HistoricalPeriodLabelTranslator = (
  key: string,
  parameters?: Record<string, string>
) => string;

export function formatPublicHistoricalPeriod(
  period: PublicHistoricalPeriod,
  language: string,
  translate: HistoricalPeriodLabelTranslator
): string {
  const start: PublicHistoricalDate | null = period.start ?? null;
  const end: PublicHistoricalDate | null = period.end ?? null;

  if (!start && !end) {
    return translate('history.explorer.unknownDate');
  }

  if (
    start
    && end
    && historicalDatesAreEqual(start, end)
    && period.startConfidence === period.endConfidence
  ) {
    return formatPublicHistoricalBoundary(start, period.startConfidence, language, translate);
  }

  if (start && end) {
    return translate('history.explorer.datePeriod', {
      start: formatPublicHistoricalBoundary(start, period.startConfidence, language, translate),
      end: formatPublicHistoricalBoundary(end, period.endConfidence, language, translate)
    });
  }

  if (start) {
    return translate('history.explorer.dateFrom', {
      date: formatPublicHistoricalBoundary(start, period.startConfidence, language, translate)
    });
  }

  return translate('history.explorer.dateUntil', {
    date: formatPublicHistoricalBoundary(end!, period.endConfidence, language, translate)
  });
}

function formatPublicHistoricalBoundary(
  date: PublicHistoricalDate,
  confidence: string,
  language: string,
  translate: HistoricalPeriodLabelTranslator
): string {
  const formatted: string = formatPublicHistoricalDate(date, language, translate);
  if (confidence === 'Confirmed') {
    return formatted;
  }

  const confidenceKey: string = `history.explorer.dateConfidence.${confidence}`;
  const qualified: string = translate(confidenceKey, { date: formatted });
  return qualified === confidenceKey ? formatted : qualified;
}

function formatPublicHistoricalDate(
  date: PublicHistoricalDate,
  language: string,
  translate: HistoricalPeriodLabelTranslator
): string {
  const options: Intl.DateTimeFormatOptions = date.day
    ? { day: 'numeric', month: 'long', year: 'numeric', timeZone: 'UTC' }
    : date.month
      ? { month: 'long', year: 'numeric', timeZone: 'UTC' }
      : { year: 'numeric', timeZone: 'UTC' };
  const value: Date = new Date(Date.UTC(date.year, (date.month ?? 1) - 1, date.day ?? 1));
  let formatted: string = new Intl.DateTimeFormat(language, options).format(value);

  if (date.qualifier) {
    const qualifierKey: string = `history.explorer.dateQualifiers.${date.qualifier}`;
    const qualified: string = translate(qualifierKey, { date: formatted });
    if (qualified !== qualifierKey) {
      formatted = qualified;
    }
  }

  if (date.isApproximate && date.qualifier !== 'Circa') {
    return translate('history.explorer.approximateDate', { date: formatted });
  }

  return formatted;
}

function historicalDatesAreEqual(left: PublicHistoricalDate, right: PublicHistoricalDate): boolean {
  return left.year === right.year
    && (left.month ?? null) === (right.month ?? null)
    && (left.day ?? null) === (right.day ?? null)
    && left.precision === right.precision
    && (left.isApproximate ?? false) === (right.isApproximate ?? false)
    && (left.qualifier ?? null) === (right.qualifier ?? null);
}
