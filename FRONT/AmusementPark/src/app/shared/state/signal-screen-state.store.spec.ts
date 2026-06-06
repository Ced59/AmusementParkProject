import { SignalScreenStateStore } from './signal-screen-state.store';

describe('SignalScreenStateStore', () => {
  let store: SignalScreenStateStore<string[], string>;

  beforeEach(() => {
    store = new SignalScreenStateStore<string[], string>();
  });

  it('starts in loading state', () => {
    expect(store.kind()).toBe('loading');
    expect(store.isLoading()).toBeTrue();
    expect(store.isReady()).toBeFalse();
  });

  it('transitions to ready with data', () => {
    store.setReady(['a']);

    expect(store.state()).toEqual({ kind: 'ready', data: ['a'] });
    expect(store.data()).toEqual(['a']);
    expect(store.isReady()).toBeTrue();
  });

  it('transitions to empty while preserving optional data', () => {
    store.setEmpty([]);

    expect(store.kind()).toBe('empty');
    expect(store.data()).toEqual([]);
    expect(store.isEmpty()).toBeTrue();
  });

  it('transitions to error with optional stale data', () => {
    store.setError('boom', ['old']);

    expect(store.error()).toBe('boom');
    expect(store.data()).toEqual(['old']);
    expect(store.isError()).toBeTrue();
  });

  it('can go back to loading and replace data', () => {
    store.setReady(['a']);
    store.setLoading(['cached']);

    expect(store.kind()).toBe('loading');
    expect(store.data()).toEqual(['cached']);
  });
});
