import {
  ParkAdmissionPriceOffer,
  ParkAnnualPassOffer,
  ParkCreditOffer,
  ParkParkingPriceOffer,
  ParkPriceValue,
  ParkPricing,
  ParkPricingMode,
  ParkPricingSnapshot
} from '@app/models/parks/park-pricing';
import { LocalizedItem } from '@app/models/shared/localized-item';

export interface ParkPriceFormattingLabels {
  from: string;
  upTo: string;
  dynamic: string;
}

export type ParkPricingHistoryOfferKind = 'admission' | 'credit' | 'annualPass' | 'parking';
export type ParkPricingHistoryChannel = 'onlinePrice' | 'gatePrice';

export interface ParkPricingHistoryPoint {
  year: number;
  currencyCode: string;
  onlinePrice?: ParkPriceValue | null;
  gatePrice?: ParkPriceValue | null;
}

export interface ParkPricingHistorySeries {
  key: string;
  code: string;
  kind: ParkPricingHistoryOfferKind;
  label: string;
  points: ParkPricingHistoryPoint[];
}

export function resolvePricingLocalizedText(
  items: readonly LocalizedItem<string>[] | null | undefined,
  language: string | null | undefined,
  fallback: string = ''
): string {
  if (!items?.length) {
    return fallback;
  }

  const normalizedLanguage: string = language?.trim().toLowerCase().split('-')[0] ?? 'en';
  const exact: LocalizedItem<string> | undefined = items.find(
    (item: LocalizedItem<string>): boolean =>
      item.languageCode?.toLowerCase().split('-')[0] === normalizedLanguage && Boolean(item.value?.trim())
  );
  const english: LocalizedItem<string> | undefined = items.find(
    (item: LocalizedItem<string>): boolean =>
      item.languageCode?.toLowerCase().split('-')[0] === 'en' && Boolean(item.value?.trim())
  );

  return exact?.value?.trim()
    || english?.value?.trim()
    || items.find((item: LocalizedItem<string>): boolean => Boolean(item.value?.trim()))?.value?.trim()
    || fallback;
}

export function formatParkPrice(
  value: ParkPriceValue | null | undefined,
  currencyCode: string,
  language: string,
  labels: ParkPriceFormattingLabels
): string | null {
  if (!value) {
    return null;
  }

  const formatter: Intl.NumberFormat = new Intl.NumberFormat(language || 'en', {
    style: 'currency',
    currency: currencyCode || 'EUR',
    maximumFractionDigits: 2
  });
  const mode: ParkPricingMode = value.mode;

  if (mode === 'Fixed' && value.amount !== null && value.amount !== undefined) {
    return formatter.format(value.amount);
  }

  if (mode === 'Range') {
    if (value.minimumAmount !== null && value.minimumAmount !== undefined
      && value.maximumAmount !== null && value.maximumAmount !== undefined) {
      return `${formatter.format(value.minimumAmount)} – ${formatter.format(value.maximumAmount)}`;
    }

    return null;
  }

  if (mode === 'Dynamic') {
    if (value.minimumAmount !== null && value.minimumAmount !== undefined
      && value.maximumAmount !== null && value.maximumAmount !== undefined) {
      return `${formatter.format(value.minimumAmount)} – ${formatter.format(value.maximumAmount)}`;
    }

    if (value.minimumAmount !== null && value.minimumAmount !== undefined) {
      return `${labels.from} ${formatter.format(value.minimumAmount)}`;
    }

    if (value.maximumAmount !== null && value.maximumAmount !== undefined) {
      return `${labels.upTo} ${formatter.format(value.maximumAmount)}`;
    }

    return labels.dynamic;
  }

  return null;
}

export function buildParkPricingHistorySeries(
  pricing: ParkPricing,
  language: string,
  currentYear: number,
  maximumYears: number = 5
): ParkPricingHistorySeries[] {
  const snapshots: Array<ParkPricingSnapshot & { isCurrent?: boolean }> = [
    ...(pricing.historicalSnapshots ?? []),
    {
      year: currentYear,
      currencyCode: pricing.currencyCode,
      sourceUrl: pricing.sourceUrl,
      notes: pricing.notes,
      lastVerifiedAtUtc: pricing.lastVerifiedAtUtc,
      admissionOffers: pricing.admissionOffers,
      annualPasses: pricing.annualPasses,
      parkingOffers: pricing.parkingOffers,
      creditOffers: pricing.creditOffers ?? [],
      isCurrent: true
    }
  ].sort((left, right): number => right.year - left.year);
  const pointsBySeries = new Map<string, ParkPricingHistorySeries>();

  for (const snapshot of snapshots) {
    appendAdmissionHistory(snapshot, language, pointsBySeries);
    appendCreditHistory(snapshot, language, pointsBySeries);
    appendAnnualPassHistory(snapshot, language, pointsBySeries);
    appendParkingHistory(snapshot, language, pointsBySeries);
  }

  return [...pointsBySeries.values()]
    .map((series: ParkPricingHistorySeries): ParkPricingHistorySeries => ({
      ...series,
      points: series.points
        .sort((left, right): number => left.year - right.year)
        .slice(-Math.max(2, maximumYears))
    }))
    .filter((series: ParkPricingHistorySeries): boolean => series.points.length >= 2)
    .sort((left, right): number => left.kind.localeCompare(right.kind) || left.label.localeCompare(right.label, language));
}

export function parkPriceChartAmount(value: ParkPriceValue | null | undefined): number | null {
  if (!value) {
    return null;
  }

  if (value.mode === 'Fixed') {
    return finiteAmount(value.amount);
  }

  return finiteAmount(value.minimumAmount);
}

export function hasSingleHistoryCurrency(series: ParkPricingHistorySeries): boolean {
  return new Set(series.points.map((point: ParkPricingHistoryPoint): string => point.currencyCode)).size === 1;
}

function appendAdmissionHistory(
  snapshot: ParkPricingSnapshot,
  language: string,
  seriesByKey: Map<string, ParkPricingHistorySeries>
): void {
  for (const offer of snapshot.admissionOffers) {
    appendHistoryPoint(
      snapshot,
      'admission',
      offer.code,
      resolvePricingLocalizedText(offer.labels, language, offer.code),
      offer,
      seriesByKey);
  }
}

function appendCreditHistory(
  snapshot: ParkPricingSnapshot,
  language: string,
  seriesByKey: Map<string, ParkPricingHistorySeries>
): void {
  for (const offer of snapshot.creditOffers ?? []) {
    const code: string = `${offer.unitCode}:${offer.quantity}`;
    appendCreditHistoryPoint(
      snapshot,
      code,
      resolvePricingLocalizedText(offer.labels, language, `${offer.quantity} ${offer.unitCode}`),
      offer,
      seriesByKey);
  }
}

function appendCreditHistoryPoint(
  snapshot: ParkPricingSnapshot,
  code: string,
  label: string,
  offer: ParkCreditOffer,
  seriesByKey: Map<string, ParkPricingHistorySeries>
): void {
  const key: string = `credit:${code.trim().toLowerCase()}`;
  const point: ParkPricingHistoryPoint = {
    year: snapshot.year,
    currencyCode: snapshot.currencyCode,
    onlinePrice: fixedCreditPrice(offer.prices.onlinePrice),
    gatePrice: fixedCreditPrice(offer.prices.gatePrice)
  };
  const existing: ParkPricingHistorySeries | undefined = seriesByKey.get(key);
  if (existing) {
    if (!existing.points.some((item: ParkPricingHistoryPoint): boolean => item.year === point.year)) {
      existing.points.push(point);
    }
    return;
  }

  seriesByKey.set(key, { key, code, kind: 'credit', label, points: [point] });
}

function fixedCreditPrice(amount: number | null | undefined): ParkPriceValue | null {
  return amount === null || amount === undefined ? null : { mode: 'Fixed', amount };
}

function appendAnnualPassHistory(
  snapshot: ParkPricingSnapshot,
  language: string,
  seriesByKey: Map<string, ParkPricingHistorySeries>
): void {
  for (const offer of snapshot.annualPasses) {
    appendHistoryPoint(
      snapshot,
      'annualPass',
      offer.code,
      resolvePricingLocalizedText(offer.names, language, offer.code),
      offer,
      seriesByKey);
  }
}

function appendParkingHistory(
  snapshot: ParkPricingSnapshot,
  language: string,
  seriesByKey: Map<string, ParkPricingHistorySeries>
): void {
  for (const offer of snapshot.parkingOffers) {
    appendHistoryPoint(
      snapshot,
      'parking',
      offer.code,
      resolvePricingLocalizedText(offer.labels, language, offer.code),
      offer,
      seriesByKey);
  }
}

function appendHistoryPoint(
  snapshot: ParkPricingSnapshot,
  kind: ParkPricingHistoryOfferKind,
  code: string,
  label: string,
  offer: ParkAdmissionPriceOffer | ParkAnnualPassOffer | ParkParkingPriceOffer,
  seriesByKey: Map<string, ParkPricingHistorySeries>
): void {
  const key: string = `${kind}:${code.trim().toLowerCase()}`;
  const existing: ParkPricingHistorySeries | undefined = seriesByKey.get(key);
  const point: ParkPricingHistoryPoint = {
    year: snapshot.year,
    currencyCode: snapshot.currencyCode,
    onlinePrice: offer.onlinePrice,
    gatePrice: offer.gatePrice
  };

  if (existing) {
    if (!existing.points.some((item: ParkPricingHistoryPoint): boolean => item.year === point.year)) {
      existing.points.push(point);
    }
    return;
  }

  seriesByKey.set(key, {
    key,
    code,
    kind,
    label,
    points: [point]
  });
}

function finiteAmount(value: number | null | undefined): number | null {
  return value !== null && value !== undefined && Number.isFinite(value) ? value : null;
}
