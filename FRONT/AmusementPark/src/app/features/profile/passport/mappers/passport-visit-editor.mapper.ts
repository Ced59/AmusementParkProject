import {
  CreatePassportRideOccurrenceBatchItem,
  PassportRideOccurrence,
  PassportVisitRideTargetEvaluation,
  UpdatePassportRideOccurrenceRequest
} from '@app/models/passport/passport-ride-occurrence.models';
import { ParkItem } from '@app/models/parks/park-item';
import { ParkZone } from '@app/models/parks/park-zone';
import { resolveLocalizedValue } from '@shared/utils/localization';
import {
  PassportAttractionSelectionDraft,
  PassportOccurrenceEditDraft,
  PassportVisitEditorAttraction,
  PassportVisitEditorZone
} from '../models/passport-visit-editor.models';

const historicalStatuses: ReadonlySet<string> = new Set<string>(['ClosedDefinitively', 'Removed']);

export function mapParkItemToVisitEditorAttraction(
  item: ParkItem,
  evaluation: PassportVisitRideTargetEvaluation | null = null
): PassportVisitEditorAttraction | null {
  const id: string = item.id?.trim() ?? '';
  const name: string = item.name?.trim() ?? '';
  if (!id || !name || item.category !== 'Attraction') {
    return null;
  }

  const lifecycleStatus: string | null = item.attractionDetails?.status?.trim() || null;
  return {
    id,
    name,
    mainImageId: item.mainImageId?.trim() || null,
    zoneId: item.zoneId?.trim() || null,
    lifecycleStatus,
    isHistorical: lifecycleStatus !== null && historicalStatuses.has(lifecycleStatus),
    historicalConsistency: evaluation?.historicalConsistency ?? 'Unverified',
    openingDate: evaluation?.openingDate ?? null,
    closingDate: evaluation?.closingDate ?? null
  };
}

export function mapParkZoneToVisitEditorZone(zone: ParkZone, language: string): PassportVisitEditorZone | null {
  const id: string = zone.id?.trim() ?? '';
  const name: string = resolveLocalizedValue(zone.names, language)?.trim() || zone.name?.trim() || '';
  return id && name ? { id, name } : null;
}

export function createAttractionSelection(
  attraction: PassportVisitEditorAttraction
): PassportAttractionSelectionDraft {
  return {
    parkItemId: attraction.id,
    attractionName: attraction.name,
    status: 'Completed',
    count: 1,
    localTime: '',
    isApproximate: false,
    privateNote: '',
    confirmHistoricalConflict: false,
    historicalConsistency: attraction.historicalConsistency,
    openingDate: attraction.openingDate,
    closingDate: attraction.closingDate
  };
}

export function mapAttractionSelectionToRequest(
  selection: PassportAttractionSelectionDraft,
  acceptsLocalTime: boolean
): CreatePassportRideOccurrenceBatchItem {
  return {
    parkItemId: selection.parkItemId,
    moment: {
      localTime: acceptsLocalTime ? normalizeTimeForApi(selection.localTime) : null,
      isApproximate: acceptsLocalTime && selection.localTime.length > 0 && selection.isApproximate
    },
    status: selection.status,
    privateNote: normalizeOptionalText(selection.privateNote),
    confirmHistoricalConflict: selection.confirmHistoricalConflict,
    count: normalizeCount(selection.count)
  };
}

export function mapOccurrenceToEditDraft(occurrence: PassportRideOccurrence): PassportOccurrenceEditDraft {
  return {
    status: occurrence.status,
    localTime: normalizeTimeForInput(occurrence.moment.localTime),
    isApproximate: occurrence.moment.isApproximate,
    privateNote: occurrence.privateNote ?? '',
    confirmHistoricalConflict: occurrence.historicalConflictConfirmed ?? false
  };
}

export function mapOccurrenceEditToRequest(
  occurrence: PassportRideOccurrence,
  draft: PassportOccurrenceEditDraft,
  acceptsLocalTime: boolean
): UpdatePassportRideOccurrenceRequest {
  return {
    expectedVersion: occurrence.version,
    moment: {
      localTime: acceptsLocalTime ? normalizeTimeForApi(draft.localTime) : null,
      isApproximate: acceptsLocalTime && draft.localTime.length > 0 && draft.isApproximate
    },
    status: draft.status,
    privateNote: normalizeOptionalText(draft.privateNote),
    confirmHistoricalConflict: draft.confirmHistoricalConflict
  };
}

export function normalizeCount(value: number): number {
  const integerValue: number = Number.isFinite(value) ? Math.trunc(value) : 1;
  return Math.min(100, Math.max(1, integerValue));
}

export function normalizeTimeForApi(value: string | null | undefined): string | null {
  const normalized: string = value?.trim() ?? '';
  if (!/^([01]\d|2[0-3]):[0-5]\d$/.test(normalized)) {
    return null;
  }

  return `${normalized}:00`;
}

export function normalizeTimeForInput(value: string | null | undefined): string {
  const normalized: string = value?.trim() ?? '';
  const match: RegExpMatchArray | null = normalized.match(/^([01]\d|2[0-3]):([0-5]\d)/);
  return match ? `${match[1]}:${match[2]}` : '';
}

function normalizeOptionalText(value: string | null | undefined): string | null {
  const normalized: string = value?.trim() ?? '';
  return normalized.length > 0 ? normalized : null;
}
