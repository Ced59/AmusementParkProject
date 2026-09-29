import { PublicLiveHistory } from '@app/models/live-data/public-live.models';

export type PublicLiveHistoryViewKind = 'idle' | 'loading' | 'ready' | 'error' | 'disabled';

export interface PublicLiveHistoryViewState {
  readonly kind: PublicLiveHistoryViewKind;
  readonly history: PublicLiveHistory | null;
}

export const INITIAL_PUBLIC_LIVE_HISTORY_VIEW_STATE: PublicLiveHistoryViewState = {
  kind: 'idle',
  history: null
};
