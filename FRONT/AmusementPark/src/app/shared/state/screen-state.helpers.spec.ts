import {
  createEmptyScreenState,
  createErrorScreenState,
  createLoadingScreenState,
  createReadyScreenState
} from './screen-state.helpers';

describe('screen state helpers', () => {
  it('creates loading states with optional data', () => {
    expect(createLoadingScreenState<number>(42)).toEqual({ kind: 'loading', data: 42 });
  });

  it('creates ready states with required data', () => {
    expect(createReadyScreenState({ id: 1 })).toEqual({ kind: 'ready', data: { id: 1 } });
  });

  it('creates empty states with optional data', () => {
    expect(createEmptyScreenState<string[]>()).toEqual({ kind: 'empty', data: undefined });
    expect(createEmptyScreenState<string[]>([])).toEqual({ kind: 'empty', data: [] });
  });

  it('creates error states with error and optional previous data', () => {
    expect(createErrorScreenState('boom', ['old'])).toEqual({ kind: 'error', error: 'boom', data: ['old'] });
  });
});
