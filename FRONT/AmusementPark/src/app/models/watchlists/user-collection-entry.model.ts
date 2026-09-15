export type UserCollectionTargetType = 'Park' | 'ParkItem';
export type UserCollectionKind = 'Favorite' | 'WantToVisit' | 'WantToExperience' | 'Planned';
export type UserCollectionTargetStatus = 'Unknown' | 'Available' | 'TemporarilyClosed' | 'PermanentlyClosed';

export interface UserCollectionEntry {
  entryId: string;
  targetType: UserCollectionTargetType;
  targetId: string;
  kind: UserCollectionKind;
  targetStatus: UserCollectionTargetStatus;
  targetName: string | null;
  parentParkId: string | null;
  parentParkName: string | null;
  mainImageId: string | null;
  privateNote: string | null;
  priority: number | null;
  preferredStartsOn: string | null;
  preferredEndsOn: string | null;
  createdAtUtc: string;
  updatedAtUtc: string;
  version: number;
}
