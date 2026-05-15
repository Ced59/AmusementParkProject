import { ScreenState } from '@shared/models/contracts/screen-state.model';

export function createLoadingScreenState<TData, TError = string>(data?: TData): ScreenState<TData, TError> {
  return {
    kind: 'loading',
    data
  };
}

export function createReadyScreenState<TData, TError = string>(data: TData): ScreenState<TData, TError> {
  return {
    kind: 'ready',
    data
  };
}

export function createEmptyScreenState<TData, TError = string>(data?: TData): ScreenState<TData, TError> {
  return {
    kind: 'empty',
    data
  };
}

export function createErrorScreenState<TData, TError = string>(error: TError, data?: TData): ScreenState<TData, TError> {
  return {
    kind: 'error',
    error,
    data
  };
}
