export type ScreenStateKind = 'loading' | 'ready' | 'empty' | 'error';

export interface ScreenState<TData = void, TError = string> {
  kind: ScreenStateKind;
  data?: TData;
  error?: TError;
}
