import { ParkPriceValue, ParkPricingMode } from '@app/models/parks/park-pricing';
import { LocalizedItem } from '@app/models/shared/localized-item';

export interface ParkPriceFormattingLabels {
  from: string;
  upTo: string;
  dynamic: string;
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
