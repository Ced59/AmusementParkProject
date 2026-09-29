import { PublicLiveForecast } from '@app/models/live-data/public-live.models';

export type PublicLiveForecastViewKind = 'idle' | 'loading' | 'ready' | 'unavailable';

export interface PublicLiveForecastViewState {
  readonly kind: PublicLiveForecastViewKind;
  readonly forecast: PublicLiveForecast | null;
}

export const INITIAL_PUBLIC_LIVE_FORECAST_VIEW_STATE: PublicLiveForecastViewState = {
  kind: 'idle',
  forecast: null
};
