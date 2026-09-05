import { ParkOfficialMap } from '@app/models/parks/park-map-items';
import { environment } from '../../../../../environments/environment';
import { resolveLocalizedText } from '@shared/utils/localization';
import { ParkOfficialMapViewModel } from '../models/park-official-map-view.model';

export function mapParkOfficialMapsToViewModels(
  officialMaps: readonly ParkOfficialMap[] | null | undefined,
  language: string
): ParkOfficialMapViewModel[] {
  return (officialMaps ?? [])
    .filter((officialMap: ParkOfficialMap) => Number.isInteger(officialMap.year) && officialMap.year >= 1800)
    .map((officialMap: ParkOfficialMap) => {
      const title: string = resolveLocalizedText(officialMap.titles, language, '').trim();
      const alternativeText: string = resolveLocalizedText(officialMap.alternativeTexts, language, title).trim();
      return {
        id: officialMap.id,
        year: officialMap.year,
        format: officialMap.format,
        documentUrl: resolveDocumentUrl(officialMap.documentUrl),
        previewImageUrl: normalizeExternalUrl(officialMap.previewImageUrl),
        sourcePageUrl: normalizeExternalUrl(officialMap.sourcePageUrl),
        languageCode: normalizeOptionalText(officialMap.languageCode)?.toUpperCase() ?? null,
        title: title || null,
        alternativeText,
        originalFileName: normalizeOptionalText(officialMap.originalFileName),
        fileSizeLabel: formatFileSize(officialMap.sizeInBytes, language),
        isImage: officialMap.format === 'Image',
        isPdf: officialMap.format === 'Pdf'
      };
    })
    .filter((officialMap: ParkOfficialMapViewModel) => officialMap.documentUrl.length > 0)
    .sort((left: ParkOfficialMapViewModel, right: ParkOfficialMapViewModel) =>
      right.year - left.year
      || (left.languageCode ?? '').localeCompare(right.languageCode ?? '')
      || left.id.localeCompare(right.id));
}

function resolveDocumentUrl(value: string | null | undefined): string {
  const normalized: string | null = normalizeOptionalText(value);
  if (!normalized) {
    return '';
  }

  if (/^https?:\/\//i.test(normalized)) {
    return normalized;
  }

  return `${environment.apiBaseUrl.replace(/\/+$/, '')}/${normalized.replace(/^\/+/, '')}`;
}

function normalizeExternalUrl(value: string | null | undefined): string | null {
  const normalized: string | null = normalizeOptionalText(value);
  return normalized && /^https?:\/\//i.test(normalized) ? normalized : null;
}

function normalizeOptionalText(value: string | null | undefined): string | null {
  const normalized: string = value?.trim() ?? '';
  return normalized || null;
}

function formatFileSize(value: number | null | undefined, language: string): string | null {
  if (!value || !Number.isFinite(value) || value <= 0) {
    return null;
  }

  const units: readonly string[] = language.toLowerCase().startsWith('fr')
    ? ['o', 'Ko', 'Mo', 'Go']
    : ['B', 'KB', 'MB', 'GB'];
  const unitIndex: number = Math.min(Math.floor(Math.log(value) / Math.log(1024)), units.length - 1);
  const amount: number = value / Math.pow(1024, unitIndex);
  return `${new Intl.NumberFormat(language || 'en', { maximumFractionDigits: unitIndex === 0 ? 0 : 1 }).format(amount)} ${units[unitIndex]}`;
}
