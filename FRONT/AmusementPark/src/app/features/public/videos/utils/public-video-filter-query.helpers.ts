import { ParamMap } from '@angular/router';

import { VideoType } from '@app/models/videos/video-type';
import { PublicVideoFilterState } from '../models/public-video-view.model';

export function parsePublicVideoFilters(queryParamMap: ParamMap): PublicVideoFilterState {
  return {
    type: parseVideoType(queryParamMap.get('type')),
    tagId: normalizeOptionalString(queryParamMap.get('tagId')),
    creatorName: normalizeOptionalString(queryParamMap.get('creatorName')) ?? ''
  };
}

export function buildPublicVideoFilterQueryParams(filters: PublicVideoFilterState): Record<string, string | null> {
  return {
    type: filters.type ?? null,
    tagId: normalizeOptionalString(filters.tagId),
    creatorName: normalizeOptionalString(filters.creatorName)
  };
}

export function buildPublicVideoFilterKey(filters: PublicVideoFilterState): string {
  return [
    filters.type ?? '',
    normalizeOptionalString(filters.tagId) ?? '',
    normalizeOptionalString(filters.creatorName) ?? ''
  ].join('|');
}

function parseVideoType(value: string | null | undefined): VideoType | null {
  const normalizedValue: string | null = normalizeOptionalString(value);

  if (!normalizedValue) {
    return null;
  }

  return Object.values(VideoType).includes(normalizedValue as VideoType) ? normalizedValue as VideoType : null;
}

function normalizeOptionalString(value: string | null | undefined): string | null {
  const normalized: string = value?.trim() ?? '';
  return normalized.length > 0 ? normalized : null;
}
