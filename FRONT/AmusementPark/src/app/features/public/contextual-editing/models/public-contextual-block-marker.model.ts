export type PublicContextualBlockType =
  | 'park.hero'
  | 'park.description'
  | 'park.images'
  | 'park.location'
  | 'park.practical'
  | 'parkItem.description'
  | 'parkItem.images'
  | 'parkItem.location'
  | 'reference.manufacturer';

export interface PublicContextualBlockMarker {
  readonly type: PublicContextualBlockType;
  readonly parkId?: string | null;
  readonly parkItemId?: string | null;
  readonly manufacturerId?: string | null;
  readonly contextLabel?: string | null;
  readonly languageCode?: string | null;
  readonly locationFallbackCenter?: readonly [number, number] | null;
  readonly parkGraphUpsertDraftJson?: string | null;
  readonly parkGraphUpsertFileName?: string | null;
}
