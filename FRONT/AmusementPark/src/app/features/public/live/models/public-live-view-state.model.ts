import { PublicLiveTarget } from '@app/models/live-data/public-live.models';

export type PublicLiveDisplayMode = 'park' | 'item';
export type PublicLiveViewKind = 'idle' | 'loading' | 'ready' | 'error' | 'disabled';

export interface PublicLiveViewState {
  readonly kind: PublicLiveViewKind;
  readonly mode: PublicLiveDisplayMode | null;
  readonly target: PublicLiveTarget | null;
  readonly items: readonly PublicLiveTarget[];
  readonly isRefreshing: boolean;
  readonly isOnline: boolean;
  readonly refreshFailed: boolean;
  readonly lastSuccessfulRefreshUtc: string | null;
}

export const INITIAL_PUBLIC_LIVE_VIEW_STATE: PublicLiveViewState = {
  kind: 'idle',
  mode: null,
  target: null,
  items: [],
  isRefreshing: false,
  isOnline: true,
  refreshFailed: false,
  lastSuccessfulRefreshUtc: null
};
