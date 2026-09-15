import { UserCollectionKind, UserCollectionTargetType } from '@app/models/watchlists/user-collection-entry.model';

export const USER_COLLECTIONS_API_ENDPOINTS = {
  collection: 'me/collections',
  entry: (
    targetType: UserCollectionTargetType,
    targetId: string,
    kind: UserCollectionKind
  ): string => `me/collections/${encodeURIComponent(targetType)}/${encodeURIComponent(targetId)}/${encodeURIComponent(kind)}`
} as const;
